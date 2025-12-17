using System.Text;
using SemController.Core.Models;

namespace SemController.Core.Implementations;

public class TescanSemScanning
{
    private readonly TescanSemController _controller;
    
    internal TescanSemScanning(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("ScGetSpeed", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return 0;
    }
    
    public async Task SetSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(speed);
        await _controller.SendCommandNoResponseInternalAsync("ScSetSpeed", body, cancellationToken);
    }
    
    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("ScGetBlanker", null, cancellationToken);
        if (response.Length >= 4)
        {
            return (BlankerMode)TescanSemController.DecodeIntInternal(response, 0);
        }
        return BlankerMode.Off;
    }
    
    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal((int)mode);
        await _controller.SendCommandNoResponseInternalAsync("ScSetBlanker", body, cancellationToken);
    }
    
    public async Task StopScanAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("ScStopScan", null, cancellationToken);
    }
    
    public async Task SetGuiScanningAsync(bool enable, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(enable ? 1 : 0);
        await _controller.SendCommandNoResponseInternalAsync("GUISetScanning", body, cancellationToken);
    }
    
    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        await _controller.EnsureDataChannelInternalAsync(cancellationToken);
        
        foreach (var channel in settings.Channels)
        {
            var enableBody = new List<byte>();
            enableBody.AddRange(TescanSemController.EncodeIntInternal(channel));
            enableBody.AddRange(TescanSemController.EncodeIntInternal(1));
            enableBody.AddRange(TescanSemController.EncodeIntInternal(8));
            await _controller.SendCommandNoResponseInternalAsync("DtEnable", enableBody.ToArray(), cancellationToken);
        }
        
        await SetGuiScanningAsync(false, cancellationToken);
        await StopScanAsync(cancellationToken);
        
        try
        {
            var right = settings.Right > 0 ? settings.Right : (settings.Width - 1);
            var bottom = settings.Bottom > 0 ? settings.Bottom : (settings.Height - 1);
            
            var scanBody = new List<byte>();
            scanBody.AddRange(TescanSemController.EncodeIntInternal(0));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Width));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Height));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Left));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Top));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(right));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(bottom));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(1));
            
            var scanResult = await _controller.SendCommandInternalAsync("ScScanXY", scanBody.ToArray(), cancellationToken);
            if (scanResult.Length >= 4)
            {
                var scannedFrameId = TescanSemController.DecodeIntInternal(scanResult, 0);
                if (scannedFrameId < 0)
                {
                    throw new InvalidOperationException($"ScScanXY failed with error code: {scannedFrameId}");
                }
            }
            
            var imageSize = settings.Width * settings.Height;
            var imageDataList = await ReadAllImagesFromDataChannelAsync(settings.Channels, imageSize, cancellationToken);
            
            var images = new List<SemImage>();
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
        var settings = new ScanSettings
        {
            Width = width,
            Height = height,
            Channels = new[] { channel }
        };
        
        var images = await AcquireImagesAsync(settings, cancellationToken);
        return images.Length > 0 ? images[0] : new SemImage(width, height, Array.Empty<byte>(), channel);
    }
    
    private async Task<List<byte[]>> ReadAllImagesFromDataChannelAsync(int[] channels, int imageSizePerChannel, CancellationToken cancellationToken)
    {
        var imageByChannel = new Dictionary<int, byte[]>();
        var bytesReceivedByChannel = new Dictionary<int, int>();
        
        foreach (var ch in channels)
        {
            imageByChannel[ch] = new byte[imageSizePerChannel];
            bytesReceivedByChannel[ch] = 0;
        }
        
        var timeout = TimeSpan.FromSeconds(_controller.TimeoutSeconds * 3);
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var message = await _controller.ReadDataChannelMessageInternalAsync(cancellationToken);
            if (message == null)
                break;
            
            var commandName = Encoding.ASCII.GetString(message.Header, 0, TescanSemController.CommandNameSizeInternal).TrimEnd('\0');
            
            if (commandName == "ScData" && message.Body.Length >= 20)
            {
                var msgChannel = BitConverter.ToInt32(message.Body, 4);
                var argIndex = BitConverter.ToUInt32(message.Body, 8);
                var argBpp = BitConverter.ToInt32(message.Body, 12);
                var argDataSize = BitConverter.ToUInt32(message.Body, 16);
                
                if (!imageByChannel.ContainsKey(msgChannel))
                    continue;
                    
                if (argBpp != 8)
                    continue;
                
                var buffer = imageByChannel[msgChannel];
                var currentSize = bytesReceivedByChannel[msgChannel];
                
                if (argIndex < currentSize)
                {
                    currentSize = (int)argIndex;
                }
                
                if (argIndex > currentSize)
                    continue;
                
                int dataOffset = 20;
                var copyLen = Math.Min((int)argDataSize, buffer.Length - (int)argIndex);
                
                if (copyLen > 0 && dataOffset + argDataSize <= message.Body.Length)
                {
                    Array.Copy(message.Body, dataOffset, buffer, (int)argIndex, copyLen);
                    bytesReceivedByChannel[msgChannel] = (int)argIndex + copyLen;
                }
            }
            
            bool allComplete = true;
            foreach (var ch in channels)
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
        
        var results = new List<byte[]>();
        foreach (var ch in channels)
        {
            results.Add(imageByChannel[ch]);
        }
        
        return results;
    }
}
