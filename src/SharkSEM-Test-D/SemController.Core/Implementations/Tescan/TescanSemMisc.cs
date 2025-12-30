// =============================================================================
// TescanSemMisc.cs - TESCAN Miscellaneous Functions
// =============================================================================
// Handles miscellaneous operations for TESCAN microscopes,
// primarily microscope identification and version information.
//
// SharkSEM Commands Used:
// - TcpGetModel: Returns microscope model name
// - TcpGetDevice: Returns serial number
// - TcpGetSWVersion: Returns microscope software version
// - TcpGetVersion: Returns SharkSEM protocol version
// =============================================================================

using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Miscellaneous functions sub-module for TESCAN SEMs.
/// Handles microscope identification information.
/// </summary>
public class TescanSemMisc
{
    private readonly TescanSemController _controller;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemMisc(TescanSemController controller)
    {
        _controller = controller;
    }
    
    /// <summary>
    /// Retrieves microscope identification and version information.
    /// Queries multiple commands and aggregates results.
    /// Each query is wrapped in try-catch to handle partial failures gracefully.
    /// </summary>
    public async Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        MicroscopeInfo info = new MicroscopeInfo { Manufacturer = "TESCAN" };
        
        // Query model name
        try
        {
            byte[] response = await _controller.SendCommandInternalAsync("TcpGetModel", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.Model = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        // Query serial number (device ID)
        try
        {
            byte[] response = await _controller.SendCommandInternalAsync("TcpGetDevice", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.SerialNumber = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        // Query microscope software version
        try
        {
            byte[] response = await _controller.SendCommandInternalAsync("TcpGetSWVersion", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.SoftwareVersion = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        // Query SharkSEM protocol version
        try
        {
            byte[] response = await _controller.SendCommandInternalAsync("TcpGetVersion", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.ProtocolVersion = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        return info;
    }
}
