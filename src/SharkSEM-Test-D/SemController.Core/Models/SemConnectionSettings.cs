// =============================================================================
// SemConnectionSettings.cs - Connection Configuration
// =============================================================================
// Contains all settings needed to establish a connection to a microscope.
// Used by SemControllerFactory to create appropriate controller instances.
// Includes factory methods for common configurations.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Connection settings for establishing SEM communication.
/// Pass to SemControllerFactory.Create() to instantiate the appropriate controller.
/// </summary>
public class SemConnectionSettings
{
    /// <summary>SEM vendor type (determines which controller implementation to use).</summary>
    public SemType Type { get; set; } = SemType.Tescan;
    
    /// <summary>
    /// Hostname or IP address.
    /// TESCAN: microscope PC hostname/IP.
    /// Thermo: typically "localhost" when running on microscope PC.
    /// </summary>
    public string Host { get; set; } = "localhost";
    
    /// <summary>
    /// TCP port number (TESCAN only).
    /// Default 8300 for control channel, 8301 for data channel.
    /// </summary>
    public int Port { get; set; } = 8300;
    
    /// <summary>Communication timeout in seconds.</summary>
    public double TimeoutSeconds { get; set; } = 30.0;
    
    /// <summary>Creates settings with default values.</summary>
    public SemConnectionSettings() { }
    
    /// <summary>Creates settings with all values specified.</summary>
    public SemConnectionSettings(SemType type, string host, int port, double timeoutSeconds = 30.0)
    {
        Type = type;
        Host = host;
        Port = port;
        TimeoutSeconds = timeoutSeconds;
    }
    
    /// <summary>Factory method for TESCAN connection settings.</summary>
    /// <param name="host">Microscope PC hostname or IP.</param>
    /// <param name="port">Control channel port (default 8300).</param>
    public static SemConnectionSettings Tescan(string host, int port = 8300) =>
        new SemConnectionSettings(SemType.Tescan, host, port);
    
    /// <summary>Factory method for Mock controller (no hardware required).</summary>
    public static SemConnectionSettings Mock() =>
        new SemConnectionSettings(SemType.Mock, "mock", 0);
}
