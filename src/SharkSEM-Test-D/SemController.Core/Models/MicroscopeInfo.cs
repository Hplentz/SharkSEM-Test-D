// =============================================================================
// MicroscopeInfo.cs - Microscope Identification Data
// =============================================================================
// Contains microscope identification and version information.
// Returned by GetMicroscopeInfoAsync() for logging, diagnostics,
// and connection verification.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Microscope identification and version information.
/// Populated from vendor-specific system information queries.
/// </summary>
public class MicroscopeInfo
{
    /// <summary>Microscope manufacturer (e.g., "TESCAN", "Thermo Fisher Scientific").</summary>
    public string Manufacturer { get; set; } = string.Empty;
    
    /// <summary>Microscope model name/number (e.g., "MIRA3", "Apreo").</summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>Unique serial number for this instrument.</summary>
    public string SerialNumber { get; set; } = string.Empty;
    
    /// <summary>Control software version installed on the microscope.</summary>
    public string SoftwareVersion { get; set; } = string.Empty;
    
    /// <summary>Communication protocol version (e.g., "SharkSEM 4.4", "AutoScript").</summary>
    public string ProtocolVersion { get; set; } = string.Empty;
}
