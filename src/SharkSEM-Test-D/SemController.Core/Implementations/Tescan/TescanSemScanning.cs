// =============================================================================
// TescanSemScanning.cs - TESCAN Image Acquisition
// =============================================================================
// Handles image acquisition and scanning control for TESCAN microscopes.
// Manages scan speeds, blanker modes, and the complete image capture workflow.
//
// Image Acquisition Workflow:
// 1. Establish data channel (bind → register → connect)
// 2. Enable detector channels for acquisition
// 3. Disable GUI scanning to take exclusive control
// 4. Execute ScScanXY with resolution and ROI parameters
// 5. Read image data from data channel (ScData messages)
// 6. Re-enable GUI scanning
//
// Data Channel Protocol:
// - Separate TCP connection on port 8301 (control port + 1)
// - Must first bind to local port, then register with TcpRegDataPort
// - Image data arrives as ScData messages with header + pixel data
//
// SharkSEM Commands Used:
// - ScEnumSpeeds: Lists available scan speeds
// - ScGetSpeed/ScSetSpeed: Current speed setting
// - ScGetBlanker/ScSetBlanker: Blanker mode
// - ScStopScan: Stops active scan
// - GUISetScanning: Enables/disables microscope GUI scanning
// - ScScanXY: Initiates single-frame acquisition
// =============================================================================

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Image acquisition sub-module for TESCAN SEMs.
/// Handles scan speed control and image capture via SharkSEM protocol.
/// </summary>
public class TescanSemScanning
{
    private readonly TescanSemController _controller;
    private List<ScanSpeed>? _cachedSpeeds;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemScanning(TescanSemController controller)
    {
        _controller = controller;
    }
    
    // -------------------------------------------------------------------------
    // Scan Speed Management
    // -------------------------------------------------------------------------
    // TESCAN microscopes have discrete speed settings with associated dwell times.
    // Speeds are cached after first enumeration for performance.
    
    /// <summary>
    /// Enumerates available scan speeds with their dwell times.
    /// Results are cached; use forceRefresh to query again.
    /// </summary>
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
            
            // Parse property-map format: speed.N.dwell=X.X (dwell in microseconds)
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
    
    /// <summary>
    /// Clears cached speed list (call if speeds might have changed).
    /// </summary>
    public void ClearSpeedCache()
    {
        _cachedSpeeds = null;
    }
    
    /// <summary>
    /// Finds the speed index closest to the target dwell time.
    /// </summary>
    /// <param name="targetDwellTimeMicroseconds">Desired dwell time in µs.</param>
    /// <returns>Speed index, or null if no speeds available.</returns>
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
    
    /// <summary>
    /// Sets scan speed to the closest match for target dwell time.
    /// </summary>
    /// <returns>True if speed was set, false if no matching speed found.</returns>
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
    
    /// <summary>
    /// Gets current scan speed index.
    /// </summary>
    public async Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("ScGetSpeed", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return 0;
    }
    
    /// <summary>
    /// Sets scan speed by index.
    /// </summary>
    public async Task SetSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(speed);
        await _controller.SendCommandNoResponseInternalAsync("ScSetSpeed", body, cancellationToken);
    }
    
    // -------------------------------------------------------------------------
    // Blanker Control
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Gets current blanker mode (Off, On, Auto).
    /// </summary>
    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("ScGetBlanker", null, cancellationToken);
        if (response.Length >= 4)
        {
            return (BlankerMode)TescanSemController.DecodeIntInternal(response, 0);
        }
        return BlankerMode.Off;
    }
    
    /// <summary>
    /// Sets blanker mode.
    /// </summary>
    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal((int)mode);
        await _controller.SendCommandNoResponseInternalAsync("ScSetBlanker", body, cancellationToken);
    }
    
    /// <summary>
    /// Stops any active scan operation.
    /// </summary>
    public async Task StopScanAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("ScStopScan", null, cancellationToken);
    }
    
    /// <summary>
    /// Enables or disables GUI scanning (live view on microscope PC).
    /// Must disable before acquisition to avoid conflicts.
    /// </summary>
    public async Task SetGuiScanningAsync(bool enable, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(enable ? 1 : 0);
        await _controller.SendCommandNoResponseInternalAsync("GUISetScanning", body, cancellationToken);
    }
    
    // -------------------------------------------------------------------------
    // Image Acquisition
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Acquires images from specified channels according to settings.
    /// Implements the complete SharkSEM acquisition workflow.
    /// </summary>
    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        // Ensure data channel is established (bind → register → connect sequence)
        await _controller.EnsureDataChannelInternalAsync(cancellationToken);
        
        // Enable each requested channel for acquisition
        foreach (int channel in settings.Channels)
        {
            List<byte> enableBody = new List<byte>();
            enableBody.AddRange(TescanSemController.EncodeIntInternal(channel));
            enableBody.AddRange(TescanSemController.EncodeIntInternal(1)); // Enable
            enableBody.AddRange(TescanSemController.EncodeIntInternal(8)); // 8 bits per pixel
            await _controller.SendCommandNoResponseInternalAsync("DtEnable", enableBody.ToArray(), cancellationToken);
        }
        
        // Take control from GUI scanning
        await SetGuiScanningAsync(false, cancellationToken);
        await StopScanAsync(cancellationToken);
        
        try
        {
            // Calculate ROI bounds (0 = full frame)
            int right = settings.Right > 0 ? settings.Right : (settings.Width - 1);
            int bottom = settings.Bottom > 0 ? settings.Bottom : (settings.Height - 1);
            
            // Build ScScanXY command body
            List<byte> scanBody = new List<byte>();
            scanBody.AddRange(TescanSemController.EncodeIntInternal(0));              // Single frame mode
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Width));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Height));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Left));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Top));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(right));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(bottom));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(1));              // Number of frames
            
            // Execute scan and get frame ID
            byte[] scanResult = await _controller.SendCommandInternalAsync("ScScanXY", scanBody.ToArray(), cancellationToken);
            if (scanResult.Length >= 4)
            {
                int scannedFrameId = TescanSemController.DecodeIntInternal(scanResult, 0);
                if (scannedFrameId < 0)
                {
                    throw new InvalidOperationException($"ScScanXY failed with error code: {scannedFrameId}");
                }
            }
            
            // Read image data from data channel
            int imageSize = settings.Width * settings.Height;
            List<byte[]> imageDataList = await ReadAllImagesFromDataChannelAsync(settings.Channels, imageSize, cancellationToken);
            
            // Build SemImage array from received data
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
            // Always restore GUI scanning control
            await SetGuiScanningAsync(true, cancellationToken);
        }
    }
    
    /// <summary>
    /// Acquires a single image from specified channel.
    /// Convenience method that wraps AcquireImagesAsync.
    /// </summary>
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
    
    /// <summary>
    /// Reads image data from the SharkSEM data channel.
    /// Accumulates ScData messages until all channels have complete images.
    /// </summary>
    private async Task<List<byte[]>> ReadAllImagesFromDataChannelAsync(int[] channels, int imageSizePerChannel, CancellationToken cancellationToken)
    {
        // Initialize buffers for each channel
        Dictionary<int, byte[]> imageByChannel = new Dictionary<int, byte[]>();
        Dictionary<int, int> bytesReceivedByChannel = new Dictionary<int, int>();
        
        foreach (int ch in channels)
        {
            imageByChannel[ch] = new byte[imageSizePerChannel];
            bytesReceivedByChannel[ch] = 0;
        }
        
        // Read messages until all channels complete or timeout
        TimeSpan timeout = TimeSpan.FromSeconds(_controller.TimeoutSeconds * 3);
        DateTime startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            TescanSemController.DataChannelMessage? message = await _controller.ReadDataChannelMessageInternalAsync(cancellationToken);
            if (message == null)
                break;
            
            string commandName = Encoding.ASCII.GetString(message.Header, 0, TescanSemController.CommandNameSizeInternal).TrimEnd('\0');
            
            // ScData message format:
            // Offset 0-3: unknown
            // Offset 4-7: channel number
            // Offset 8-11: pixel index (where this data starts)
            // Offset 12-15: bits per pixel
            // Offset 16-19: data size
            // Offset 20+: pixel data
            if (commandName == "ScData" && message.Body.Length >= 20)
            {
                int msgChannel = BitConverter.ToInt32(message.Body, 4);
                uint argIndex = BitConverter.ToUInt32(message.Body, 8);
                int argBpp = BitConverter.ToInt32(message.Body, 12);
                uint argDataSize = BitConverter.ToUInt32(message.Body, 16);
                
                // Skip if not a channel we're interested in
                if (!imageByChannel.ContainsKey(msgChannel))
                    continue;
                    
                // Skip non-8-bit data (16-bit would need different handling)
                if (argBpp != 8)
                    continue;
                
                byte[] buffer = imageByChannel[msgChannel];
                int currentSize = bytesReceivedByChannel[msgChannel];
                
                // Handle potential retransmission (index < current position)
                if (argIndex < currentSize)
                {
                    currentSize = (int)argIndex;
                }
                
                // Skip out-of-order data (gap in sequence)
                if (argIndex > currentSize)
                    continue;
                
                // Copy pixel data to buffer
                int dataOffset = 20;
                int copyLen = Math.Min((int)argDataSize, buffer.Length - (int)argIndex);
                
                if (copyLen > 0 && dataOffset + argDataSize <= message.Body.Length)
                {
                    Array.Copy(message.Body, dataOffset, buffer, (int)argIndex, copyLen);
                    bytesReceivedByChannel[msgChannel] = (int)argIndex + copyLen;
                }
            }
            
            // Check if all channels are complete
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
        
        // Return images in channel order
        List<byte[]> results = new List<byte[]>();
        foreach (int ch in channels)
        {
            results.Add(imageByChannel[ch]);
        }
        
        return results;
    }
}
