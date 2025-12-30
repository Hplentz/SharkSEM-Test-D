// =============================================================================
// TescanSemDetectors.cs - TESCAN Detector Control
// =============================================================================
// Manages detector configuration and selection for TESCAN microscopes.
// TESCAN SEMs have multiple detector channels that can be configured with
// different detectors (SE, BSE, etc.) for imaging.
//
// Detector concepts:
// - Channel: A numbered slot (0, 1, 2...) that receives detector signal
// - Detector: Physical detector hardware (SE, BSE, InBeam, etc.)
// - BPP: Bits per pixel for acquisition (typically 8 or 16)
//
// SharkSEM Commands Used:
// - DtEnumDetectors: Lists all available detectors
// - DtGetChannels: Returns number of channels
// - DtGetSelected: Returns which detector is assigned to a channel
// - DtSelect: Assigns a detector to a channel
// - DtGetEnabled: Returns if channel is enabled for acquisition
// - DtEnable: Enables/disables a channel for acquisition
// - DtAutoSignal: Runs automatic brightness/contrast optimization
// =============================================================================

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Detector configuration sub-module for TESCAN SEMs.
/// Handles detector enumeration, channel assignment, and auto-signal.
/// </summary>
public class TescanSemDetectors
{
    private readonly TescanSemController _controller;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemDetectors(TescanSemController controller)
    {
        _controller = controller;
    }
    
    /// <summary>
    /// Enumerates all available detectors.
    /// Returns property-map string describing detector indices and names.
    /// </summary>
    public async Task<string> EnumDetectorsAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("DtEnumDetectors", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeStringInternal(response, ref offset);
        }
        return string.Empty;
    }
    
    /// <summary>
    /// Gets total number of detector channels available.
    /// </summary>
    public async Task<int> GetChannelCountAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("DtGetChannels", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return 0;
    }
    
    /// <summary>
    /// Gets which detector is currently selected for a channel.
    /// Returns detector index (-1 if none selected).
    /// </summary>
    public async Task<int> GetSelectedDetectorAsync(int channel, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(channel);
        byte[] response = await _controller.SendCommandInternalAsync("DtGetSelected", body, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return -1;
    }
    
    /// <summary>
    /// Assigns a detector to a channel.
    /// </summary>
    /// <param name="channel">Channel number (0-based).</param>
    /// <param name="detector">Detector index from EnumDetectors.</param>
    public async Task SelectDetectorAsync(int channel, int detector, CancellationToken cancellationToken = default)
    {
        List<byte> body = new List<byte>();
        body.AddRange(TescanSemController.EncodeIntInternal(channel));
        body.AddRange(TescanSemController.EncodeIntInternal(detector));
        await _controller.SendCommandNoResponseInternalAsync("DtSelect", body.ToArray(), cancellationToken);
    }
    
    /// <summary>
    /// Gets channel enabled state and bits per pixel setting.
    /// </summary>
    /// <returns>Tuple of (enabled: 0 or 1, bpp: typically 8 or 16).</returns>
    public async Task<(int enabled, int bpp)> GetChannelEnabledAsync(int channel, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(channel);
        byte[] response = await _controller.SendCommandInternalAsync("DtGetEnabled", body, cancellationToken);
        if (response.Length >= 8)
        {
            int enabled = TescanSemController.DecodeIntInternal(response, 0);
            int bpp = TescanSemController.DecodeIntInternal(response, 4);
            return (enabled, bpp);
        }
        return (0, 0);
    }
    
    /// <summary>
    /// Enables or disables a channel for image acquisition.
    /// </summary>
    /// <param name="channel">Channel number.</param>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <param name="bpp">Bits per pixel (8 or 16).</param>
    public async Task EnableChannelAsync(int channel, bool enable, int bpp = 8, CancellationToken cancellationToken = default)
    {
        List<byte> body = new List<byte>();
        body.AddRange(TescanSemController.EncodeIntInternal(channel));
        body.AddRange(TescanSemController.EncodeIntInternal(enable ? 1 : 0));
        body.AddRange(TescanSemController.EncodeIntInternal(bpp));
        await _controller.SendCommandNoResponseInternalAsync("DtEnable", body.ToArray(), cancellationToken);
    }
    
    /// <summary>
    /// Runs automatic brightness/contrast optimization for a channel.
    /// Uses wait flags to block until optimization completes.
    /// </summary>
    public async Task AutoSignalAsync(int channel, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(channel);
        await _controller.SendCommandWithWaitInternalAsync("DtAutoSignal", body, TescanSemController.WaitFlagOpticsInternal | TescanSemController.WaitFlagAutoInternal, cancellationToken);
    }
}
