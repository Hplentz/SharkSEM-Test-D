// =============================================================================
// TescanSemScanning.cs - TESCAN Image Acquisition
// =============================================================================
// Handles image acquisition and scanning control for TESCAN microscopes.
// Manages scan speeds, blanker modes, and the complete image capture workflow.
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ IMAGE ACQUISITION WORKFLOW                                                  │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │ 1. SETUP DATA CHANNEL (EnsureDataChannelInternalAsync)                      │
// │    - Bind local socket to available port                                    │
// │    - Register port with SEM via TcpRegDataPort                              │
// │    - Connect to SEM data port (control port + 1)                            │
// │                                                                             │
// │ 2. CONFIGURE DETECTORS (DtEnable for each channel)                          │
// │    - Enable detector channels that will receive signal                      │
// │    - Set bits per pixel (8 or 16)                                           │
// │                                                                             │
// │ 3. TAKE CONTROL FROM GUI                                                    │
// │    - GUISetScanning(0) - Disable live view on microscope PC                 │
// │    - ScStopScan - Stop any active scanning                                  │
// │    - This prevents conflicts between GUI and remote acquisition             │
// │                                                                             │
// │ 4. EXECUTE SCAN (ScScanXY)                                                  │
// │    - Send scan parameters (resolution, ROI, frame count)                    │
// │    - Server returns frame ID (negative = error)                             │
// │    - Scan begins immediately; data flows to data channel                    │
// │                                                                             │
// │ 5. RECEIVE IMAGE DATA (ReadAllImagesFromDataChannelAsync)                   │
// │    - Read ScData messages from data channel                                 │
// │    - Each message contains a chunk of pixels for one channel                │
// │    - Accumulate chunks until complete image received                        │
// │                                                                             │
// │ 6. RESTORE GUI CONTROL                                                      │
// │    - GUISetScanning(1) - Re-enable live view                                │
// │    - Important: Do this even on failure (finally block)                     │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ ScScanXY COMMAND FORMAT                                                     │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │ Request Body (8 integers = 32 bytes):                                       │
// │   [0] Mode      - 0 = single frame, 1 = continuous                          │
// │   [1] Width     - Image width in pixels                                     │
// │   [2] Height    - Image height in pixels                                    │
// │   [3] Left      - ROI left edge (0 = full frame)                            │
// │   [4] Top       - ROI top edge (0 = full frame)                             │
// │   [5] Right     - ROI right edge (Width-1 = full frame)                     │
// │   [6] Bottom    - ROI bottom edge (Height-1 = full frame)                   │
// │   [7] Frames    - Number of frames to acquire                               │
// │                                                                             │
// │ Response Body:                                                              │
// │   [0] FrameId   - Positive = success (frame identifier)                     │
// │                   Negative = error code                                     │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ ScData MESSAGE FORMAT (Data Channel)                                        │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │ Header: Standard 32-byte SharkSEM header with command "ScData"              │
// │                                                                             │
// │ Body Layout:                                                                │
// │   Offset   Size   Field       Description                                   │
// │   ──────   ────   ─────       ───────────                                   │
// │   0        4      Reserved    Unknown/unused                                │
// │   4        4      Channel     Detector channel number (0, 1, 2...)          │
// │   8        4      PixelIndex  Starting pixel position for this chunk        │
// │   12       4      BPP         Bits per pixel (8 or 16)                      │
// │   16       4      DataSize    Number of bytes of pixel data                 │
// │   20       N      PixelData   Raw pixel values                              │
// │                                                                             │
// │ Data arrives progressively:                                                 │
// │   - Multiple ScData messages per channel                                    │
// │   - PixelIndex indicates where chunk belongs in final image                 │
// │   - Chunks usually arrive in order but may have gaps/retransmissions        │
// │   - Complete when PixelIndex + DataSize = Width × Height                    │
// │                                                                             │
// │ Multi-channel acquisition:                                                  │
// │   - Messages for different channels are interleaved                         │
// │   - Channel field identifies which buffer to write to                       │
// │   - All channels must complete before returning                             │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ SCAN SPEED PROPERTY MAP FORMAT                                              │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │ ScEnumSpeeds returns a property map string with this format:                │
// │                                                                             │
// │   speed.0.dwell=0.1                                                         │
// │   speed.1.dwell=0.2                                                         │
// │   speed.2.dwell=0.4                                                         │
// │   speed.3.dwell=0.8                                                         │
// │   ...                                                                       │
// │                                                                             │
// │ Each line: speed.<index>.dwell=<microseconds>                               │
// │ - Index is used with ScSetSpeed                                             │
// │ - Dwell time in microseconds per pixel                                      │
// │                                                                             │
// │ Note: ScEnumSpeeds requires protocol version 3.1.14 or later.               │
// │ Older systems must use ScGetSpeed/ScSetSpeed with known indices.            │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
// =============================================================================

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Image acquisition and scan control sub-module for TESCAN SEMs.
/// Handles scan speeds, blanker control, and the complete image capture workflow.
/// </summary>
public class TescanSemScanning
{
    private readonly TescanSemController _controller;
    /// <summary>Cached scan speeds to avoid repeated enumeration.</summary>
    private List<ScanSpeed>? _cachedSpeeds;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemScanning(TescanSemController controller)
    {
        _controller = controller;
    }
    
    // =========================================================================
    // Scan Speed Management
    // =========================================================================
    // TESCAN microscopes have discrete speed settings with associated dwell times.
    // The dwell time (microseconds per pixel) determines image quality vs. speed.
    // Longer dwell = better signal-to-noise, slower acquisition.
    
    /// <summary>
    /// Enumerates available scan speeds with their dwell times.
    /// 
    /// Parses the ScEnumSpeeds property map response:
    ///   "speed.0.dwell=0.1\nspeed.1.dwell=0.2\n..."
    /// 
    /// Results are cached to avoid repeated queries. Use forceRefresh=true
    /// if speeds may have changed (rare - usually only after SEM restart).
    /// 
    /// Note: Requires protocol version 3.1.14+. Earlier versions must use
    /// ScGetSpeed/ScSetSpeed with known index values.
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
            
            // Parse property-map: "speed.N.dwell=X.X" lines
            // Using regex to handle various formatting
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
            
            // Sort by index for consistent ordering
            speeds.Sort((a, b) => a.Index.CompareTo(b.Index));
        }
        
        _cachedSpeeds = speeds;
        return speeds;
    }
    
    /// <summary>
    /// Clears cached speed list.
    /// Call if speeds might have changed (e.g., after SEM software restart).
    /// </summary>
    public void ClearSpeedCache()
    {
        _cachedSpeeds = null;
    }
    
    /// <summary>
    /// Finds the speed index closest to the target dwell time.
    /// Useful when you want a specific acquisition time rather than a specific index.
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
    /// Sets scan speed to the closest available match for target dwell time.
    /// </summary>
    /// <returns>True if speed was set, false if no speeds available.</returns>
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
    /// SharkSEM Command: ScGetSpeed (no body)
    /// Response: [4 bytes: speed index as int32]
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
    /// SharkSEM Command: ScSetSpeed
    /// Body: [4 bytes: speed index as int32]
    /// No response (fire-and-forget).
    /// </summary>
    public async Task SetSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(speed);
        await _controller.SendCommandNoResponseInternalAsync("ScSetSpeed", body, cancellationToken);
    }
    
    // =========================================================================
    // Blanker Control
    // =========================================================================
    // The beam blanker deflects the electron beam away from the sample.
    // Used to prevent sample damage/charging when not actively scanning.
    
    /// <summary>
    /// Gets current blanker mode.
    /// SharkSEM Command: ScGetBlanker (no body)
    /// Response: [4 bytes: mode as int32] - 0=Off, 1=On, 2=Auto
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
    /// SharkSEM Command: ScSetBlanker
    /// Body: [4 bytes: mode as int32] - 0=Off, 1=On, 2=Auto
    /// </summary>
    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal((int)mode);
        await _controller.SendCommandNoResponseInternalAsync("ScSetBlanker", body, cancellationToken);
    }
    
    /// <summary>
    /// Stops any active scan operation.
    /// SharkSEM Command: ScStopScan (no body, no response)
    /// 
    /// Important: Call this before starting a new acquisition to ensure
    /// the scanning system is in a known state.
    /// </summary>
    public async Task StopScanAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("ScStopScan", null, cancellationToken);
    }
    
    /// <summary>
    /// Enables or disables GUI scanning (live view on microscope PC).
    /// 
    /// SharkSEM Command: GUISetScanning
    /// Body: [4 bytes: 0=disable, 1=enable]
    /// 
    /// CRITICAL: Must disable GUI scanning before remote acquisition!
    /// If GUI is actively scanning, our ScScanXY may conflict or fail.
    /// Always re-enable in finally block to restore operator control.
    /// </summary>
    public async Task SetGuiScanningAsync(bool enable, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(enable ? 1 : 0);
        await _controller.SendCommandNoResponseInternalAsync("GUISetScanning", body, cancellationToken);
    }
    
    // =========================================================================
    // Image Acquisition
    // =========================================================================
    
    /// <summary>
    /// Acquires images from specified detector channels.
    /// Implements the complete SharkSEM acquisition workflow.
    /// 
    /// Workflow:
    /// 1. Ensure data channel is established (for receiving image data)
    /// 2. Enable each requested detector channel with DtEnable
    /// 3. Disable GUI scanning to prevent conflicts
    /// 4. Stop any active scan
    /// 5. Send ScScanXY command with resolution and ROI
    /// 6. Read ScData messages from data channel until all channels complete
    /// 7. Re-enable GUI scanning (even on failure)
    /// 
    /// The data channel must be set up before calling ScScanXY, otherwise
    /// the image data has nowhere to go and will be lost.
    /// </summary>
    /// <param name="settings">Scan parameters (resolution, channels, ROI).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of acquired images, one per channel.</returns>
    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        // Step 1: Ensure data channel is established
        // This performs the critical bind → register → connect sequence
        await _controller.EnsureDataChannelInternalAsync(cancellationToken);
        
        // Step 2: Enable each requested detector channel
        // DtEnable parameters: [channel][enabled][bpp]
        foreach (int channel in settings.Channels)
        {
            List<byte> enableBody = new List<byte>();
            enableBody.AddRange(TescanSemController.EncodeIntInternal(channel));
            enableBody.AddRange(TescanSemController.EncodeIntInternal(1)); // Enable = 1
            enableBody.AddRange(TescanSemController.EncodeIntInternal(8)); // 8 bits per pixel
            await _controller.SendCommandNoResponseInternalAsync("DtEnable", enableBody.ToArray(), cancellationToken);
        }
        
        // Step 3 & 4: Take control from GUI
        await SetGuiScanningAsync(false, cancellationToken);
        await StopScanAsync(cancellationToken);
        
        try
        {
            // Calculate ROI bounds
            // If Right/Bottom are 0, use full frame (Width-1, Height-1)
            int right = settings.Right > 0 ? settings.Right : (settings.Width - 1);
            int bottom = settings.Bottom > 0 ? settings.Bottom : (settings.Height - 1);
            
            // Step 5: Build and send ScScanXY command
            // Format: [mode][width][height][left][top][right][bottom][frames]
            List<byte> scanBody = new List<byte>();
            scanBody.AddRange(TescanSemController.EncodeIntInternal(0));              // Mode 0 = single frame
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Width));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Height));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Left));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(settings.Top));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(right));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(bottom));
            scanBody.AddRange(TescanSemController.EncodeIntInternal(1));              // 1 frame
            
            // Execute scan - returns frame ID (negative = error)
            byte[] scanResult = await _controller.SendCommandInternalAsync("ScScanXY", scanBody.ToArray(), cancellationToken);
            if (scanResult.Length >= 4)
            {
                int scannedFrameId = TescanSemController.DecodeIntInternal(scanResult, 0);
                if (scannedFrameId < 0)
                {
                    throw new InvalidOperationException($"ScScanXY failed with error code: {scannedFrameId}");
                }
            }
            
            // Step 6: Read image data from data channel
            // ScData messages arrive progressively with pixel chunks
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
            // Step 7: ALWAYS restore GUI scanning control
            // This is critical - leaving GUI scanning disabled would lock the operator out
            await SetGuiScanningAsync(true, cancellationToken);
        }
    }
    
    /// <summary>
    /// Acquires a single image from specified channel.
    /// Convenience wrapper around AcquireImagesAsync for single-channel acquisition.
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
    /// 
    /// ScData Message Body Format:
    /// ┌────────────────────────────────────────────────────────────────────┐
    /// │ Offset   Size   Field       Description                            │
    /// ├────────┼──────┼────────────┼──────────────────────────────────────┤
    /// │ 0        4      Reserved    Unknown/unused                         │
    /// │ 4        4      Channel     Detector channel number                │
    /// │ 8        4      PixelIndex  Starting pixel for this chunk          │
    /// │ 12       4      BPP         Bits per pixel (8 or 16)               │
    /// │ 16       4      DataSize    Bytes of pixel data in this chunk      │
    /// │ 20       N      PixelData   Raw pixel values                       │
    /// └────────────────────────────────────────────────────────────────────┘
    /// 
    /// Handling notes:
    /// - Messages may arrive for different channels interleaved
    /// - PixelIndex tells us where to place data in the buffer
    /// - We handle retransmissions (lower index than expected)
    /// - We skip gaps (higher index than expected) to avoid corruption
    /// - Complete when all bytes received for all channels
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
        // Use 3x normal timeout since image acquisition can be slow
        TimeSpan timeout = TimeSpan.FromSeconds(_controller.TimeoutSeconds * 3);
        DateTime startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            // Read next message from data channel
            TescanSemController.DataChannelMessage? message = await _controller.ReadDataChannelMessageInternalAsync(cancellationToken);
            if (message == null)
                break;
            
            // Extract command name from header (first 16 bytes, null-terminated)
            string commandName = Encoding.ASCII.GetString(message.Header, 0, TescanSemController.CommandNameSizeInternal).TrimEnd('\0');
            
            // Only process ScData messages with valid body
            if (commandName == "ScData" && message.Body.Length >= 20)
            {
                // Parse ScData body fields
                int msgChannel = BitConverter.ToInt32(message.Body, 4);      // Offset 4: channel
                uint argIndex = BitConverter.ToUInt32(message.Body, 8);      // Offset 8: pixel index
                int argBpp = BitConverter.ToInt32(message.Body, 12);         // Offset 12: bits per pixel
                uint argDataSize = BitConverter.ToUInt32(message.Body, 16);  // Offset 16: data size
                
                // Skip if not a channel we're interested in
                if (!imageByChannel.ContainsKey(msgChannel))
                    continue;
                    
                // Skip non-8-bit data (16-bit would need different buffer handling)
                if (argBpp != 8)
                    continue;
                
                byte[] buffer = imageByChannel[msgChannel];
                int currentSize = bytesReceivedByChannel[msgChannel];
                
                // Handle retransmission (index < expected = re-send of earlier data)
                if (argIndex < currentSize)
                {
                    // Reset position to handle retransmission
                    currentSize = (int)argIndex;
                }
                
                // Skip out-of-order data (gap in sequence would leave holes)
                if (argIndex > currentSize)
                    continue;
                
                // Copy pixel data to buffer at correct position
                int dataOffset = 20;  // Pixel data starts at byte 20
                int copyLen = Math.Min((int)argDataSize, buffer.Length - (int)argIndex);
                
                if (copyLen > 0 && dataOffset + argDataSize <= message.Body.Length)
                {
                    Array.Copy(message.Body, dataOffset, buffer, (int)argIndex, copyLen);
                    bytesReceivedByChannel[msgChannel] = (int)argIndex + copyLen;
                }
            }
            
            // Check if all channels have complete images
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
        
        // Return images in channel order (same order as input)
        List<byte[]> results = new List<byte[]>();
        foreach (int ch in channels)
        {
            results.Add(imageByChannel[ch]);
        }
        
        return results;
    }
}
