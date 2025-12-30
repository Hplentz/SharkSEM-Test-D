using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

public class TescanSemScanning
{
    private readonly TescanSemController _controller;
    private List<ScanSpeed>? _cachedSpeeds;
    
    internal TescanSemScanning(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<List<ScanSpeed>> EnumSpeedsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (_cachedSpeeds != null && !forceRefresh)
        {
            return _cachedSpeeds;
        }
        
        List<ScanSpeed> speeds = new List<ScanSpeed>();
        
        byte[] response = await _controller.SendCommandInternalAsync("ScEnumSpeeds", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            string speedMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            Regex regex = new Regex(@"speed\.(\d+)\.dwell=([0-9.]+)", RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(speedMap);
            
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count >= 3)
                {
                    if (int.TryParse(match.Groups[1].Value, out int index) &&
                        double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double dwellTime))
                    {
                        speeds.Add(new ScanSpeed(index, dwellTime));
                    }
                }
            }
            
            speeds.Sort((a, b) => a.Index.CompareTo(b.Index));
        }
        
        _cachedSpeeds = speeds;
        return speeds;
    }
    
    public void ClearSpeedCache()
    {
        _cachedSpeeds = null;
    }
    
    public async Task<int?> FindSpeedIndexByDwellTimeAsync(double targetDwellTimeMicroseconds, CancellationToken cancellationToken = default)
    {
        List<ScanSpeed> speeds = await EnumSpeedsAsync(false, cancellationToken);
        
        ScanSpeed? bestMatch = null;
        double smallestDiff = double.MaxValue;
        
        foreach (ScanSpeed speed in speeds)
        {
            double diff = Math.Abs(speed.DwellTimeMicroseconds - targetDwellTimeMicroseconds);
            if (diff < smallestDiff)
            {
                smallestDiff = diff;
                bestMatch = speed;
            }
        }
        
        return bestMatch?.Index;
    }
    
    public async Task<bool> SetSpeedByDwellTimeAsync(double targetDwellTimeMicroseconds, CancellationToken cancellationToken = default)
    {
        int? speedIndex = await FindSpeedIndexByDwellTimeAsync(targetDwellTimeMicroseconds, cancellationToken);
        if (speedIndex.HasValue)
        {
            await SetSpeedAsync(speedIndex.Value, cancellationToken);
            return true;
        }
        return false;
    }
    
    public async Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("ScGetSpeed", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return 0;
    }
    
    public async Task SetSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(speed);
        await _controller.SendCommandNoResponseInternalAsync("ScSetSpeed", body, cancellationToken);
    }
    
    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("ScGetBlanker", null, cancellationToken);
        if (response.Length >= 4)
        {
            return (BlankerMode)TescanSemController.DecodeIntInternal(response, 0);
        }
        return BlankerMode.Off;
    }
    
    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal((int)mode);
        await _controller.SendCommandNoResponseInternalAsync("ScSetBlanker", body, cancellationToken);
    }
    
    public async Task StopScanAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("ScStopScan", null, cancellationToken);
    }
    
    public async Task SetGuiScanningAsync(bool enable, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(enable ? 1 : 0);
        await _controller.SendCommandNoResponseInternalAsync("GUISetScanning", body, cancellationToken);
    }
    
    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        await _controller.EnsureDataChannelInternalAsync(cancellationToken);
        
        foreach (int channel in settings.Channels)
        {
            List<byte> enableBody = new List<byte>();
            enableBody.AddRange(TescanSemController.EncodeIntInternal(channel));
            enableBody.AddRange(TescanSemController.EncodeIntInternal(1));
            enableBody.AddRange(TescanSemController.EncodeIntInternal(8));
            await _controller.SendCommandNoResponseInternalAsync("DtEnable", enableBody.ToArray(), cancellationToken);
        }
        
        await SetGuiScanningAsync(false, cancellationToken);
        await StopScanAsync(cancellationToken);
        
        try
        {
            int right = settings.Right > 0 ? settings.Right : (settings.Width - 1);
            int bottom = settings.Bottom > 0 ? settings.Bottom : (settings.Height - 1);
            
            List<byte> scanBody = new List<byte>();
            scanBody.AddRange(TescanSemController.EncodeIntInternal(0));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Width));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Height));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Left));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Top));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(right));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(bottom));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(1));
            
            byte[] scanResult = await _controller.SendCommandInternalAsync("ScScanXY", scanBody.ToArray(), cancellationToken);
            if (scanResult.Length >= 4)
            {
                int scannedFrameId = TescanSemController.DecodeIntInternal(scanResult, 0);
                if (scannedFrameId < 0)
                {
                    throw new InvalidOperationException($"ScScanXY failed with error code: {scannedFrameId}");
                }
            }
            
            int imageSize = settings.Width * settings.Height;
            List<byte[]> imageDataList = await ReadAllImagesFromDataChannelAsync(settings.Channels, imageSize, cancellationToken);
            
            List<SemImage> images = new List<SemImage>();
            for (int i = 0; i < settings.Channels.Length && i < imageDataList.Count; i++)
            {
                if (imageDataList[i].Length > 0)
                {
                    images.Add(new SemImage(settings.Width, settings.Height, imageDataList[i], settings.Channels[i]));
                }
            }
            
            return images.ToArray();
        }
        finally
        {
            await SetGuiScanningAsync(true, cancellationToken);
        }
    }
    
    public async Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
    {
        ScanSettings settings = new ScanSettings
        {
            Width = width,
            Height = height,
            Channels = new[] { channel }
        };
        
        SemImage[] images = await AcquireImagesAsync(settings, cancellationToken);
        return images.Length > 0 ? images[0] : new SemImage(width, height, Array.Empty<byte>(), channel);
    }
    
    private async Task<List<byte[]>> ReadAllImagesFromDataChannelAsync(int[] channels, int imageSizePerChannel, CancellationToken cancellationToken)
    {
        Dictionary<int, byte[]> imageByChannel = new Dictionary<int, byte[]>();
        Dictionary<int, int> bytesReceivedByChannel = new Dictionary<int, int>();
        
        foreach (int ch in channels)
        {
            imageByChannel[ch] = new byte[imageSizePerChannel];
            bytesReceivedByChannel[ch] = 0;
        }
        
        TimeSpan timeout = TimeSpan.FromSeconds(_controller.TimeoutSeconds * 3);
        DateTime startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            TescanSemController.DataChannelMessage? message = await _controller.ReadDataChannelMessageInternalAsync(cancellationToken);
            if (message == null)
                break;
            
            string commandName = Encoding.ASCII.GetString(message.Header, 0, TescanSemController.CommandNameSizeInternal).TrimEnd('\0');
            
            if (commandName == "ScData" && message.Body.Length >= 20)
            {
                int msgChannel = BitConverter.ToInt32(message.Body, 4);
                uint argIndex = BitConverter.ToUInt32(message.Body, 8);
                int argBpp = BitConverter.ToInt32(message.Body, 12);
                uint argDataSize = BitConverter.ToUInt32(message.Body, 16);
                
                if (!imageByChannel.ContainsKey(msgChannel))
                    continue;
                    
                if (argBpp != 8)
                    continue;
                
                byte[] buffer = imageByChannel[msgChannel];
                int currentSize = bytesReceivedByChannel[msgChannel];
                
                if (argIndex < currentSize)
                {
                    currentSize = (int)argIndex;
                }
                
                if (argIndex > currentSize)
                    continue;
                
                int dataOffset = 20;
                int copyLen = Math.Min((int)argDataSize, buffer.Length - (int)argIndex);
                
                if (copyLen > 0 && dataOffset + argDataSize <= message.Body.Length)
                {
                    Array.Copy(message.Body, dataOffset, buffer, (int)argIndex, copyLen);
                    bytesReceivedByChannel[msgChannel] = (int)argIndex + copyLen;
                }
            }
            
            bool allComplete = true;
            foreach (int ch in channels)
            {
                if (bytesReceivedByChannel[ch] < imageSizePerChannel)
                {
                    allComplete = false;
                    break;
                }
            }
            if (allComplete)
                break;
        }
        
        List<byte[]> results = new List<byte[]>();
        foreach (int ch in channels)
        {
            results.Add(imageByChannel[ch]);
        }
        
        return results;
    }
}
