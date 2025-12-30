// =============================================================================
// Thermo Fisher SEM Controller Example
// =============================================================================
// Demonstrates the use of ThermoSemController to connect to and control a
// Thermo Fisher Scientific Scanning Electron Microscope via AutoScript API.
//
// Usage: SemController.Thermo.Example [host] [port]
//   host - AutoScript server hostname/IP (default: localhost)
//   port - AutoScript server port (default: 7520)
//
// This example shows:
// - Connecting to a Thermo Fisher SEM
// - Retrieving microscope information
// - Reading vacuum, beam, optics, and stage status
// - Acquiring and saving images
//
// Note: Requires the Thermo Fisher SEM to be running with AutoScript enabled.
// =============================================================================

using SemController.Core.Factory;
using SemController.Core.Models;
using SemController.Core.Implementations.Thermo;

Console.WriteLine("=== Thermo Fisher Scientific SEM Controller Demo ===\n");

Console.WriteLine("This demo tests the Thermo Fisher SEM controller via AutoScript API.");
Console.WriteLine("The library provides the same ISemController interface for all SEM vendors.\n");

// Parse command-line arguments for connection settings
string host = "localhost";
int port = 7520;

if (args.Length >= 1)
    host = args[0];
if (args.Length >= 2 && int.TryParse(args[1], out int parsedPort))
    port = parsedPort;

Console.WriteLine($"--- Connecting to Thermo Fisher SEM at {host}:{port} ---\n");

try
{
    // Create controller and connect
    using ThermoSemController sem = new ThermoSemController(host, port);
    await sem.ConnectAsync();
    Console.WriteLine("Connected to Thermo Fisher SEM");
    
    // Retrieve and display microscope information
    MicroscopeInfo info = await sem.GetMicroscopeInfoAsync();
    Console.WriteLine($"\nMicroscope Info:");
    Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
    Console.WriteLine($"  Model: {info.Model}");
    Console.WriteLine($"  Serial: {info.SerialNumber}");
    Console.WriteLine($"  Software: {info.SoftwareVersion}");
    Console.WriteLine($"  Protocol: {info.ProtocolVersion}");
    
    // Query vacuum system status
    Console.WriteLine("\n--- Vacuum Status ---");
    VacuumStatus vacStatus = await sem.GetVacuumStatusAsync();
    Console.WriteLine($"Vacuum Status: {vacStatus}");
    
    try
    {
        double pressure = await sem.GetVacuumPressureAsync(VacuumGauge.Chamber);
        Console.WriteLine($"Chamber Pressure: {pressure:E2} Pa");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chamber Pressure: (unavailable - {ex.Message})");
    }
    
    // Query beam status
    Console.WriteLine("\n--- Beam Status ---");
    BeamState beamState = await sem.GetBeamStateAsync();
    Console.WriteLine($"Beam State: {beamState}");
    
    try
    {
        double hv = await sem.GetHighVoltageAsync();
        Console.WriteLine($"High Voltage: {hv:F0} V");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"High Voltage: (unavailable - {ex.Message})");
    }
    
    try
    {
        double emission = await sem.GetEmissionCurrentAsync();
        double emissionMicroAmps = emission * 1e6;
        Console.WriteLine($"Emission Current: {emission:E3} A ({emissionMicroAmps:F1} µA)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Emission Current: (unavailable - {ex.Message})");
    }
    
    // Query electron optics
    Console.WriteLine("\n--- Electron Optics ---");
    try
    {
        double wd = await sem.GetWorkingDistanceAsync();
        Console.WriteLine($"Working Distance: {wd:F3} mm");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Working Distance: (unavailable - {ex.Message})");
    }
    
    try
    {
        double vf = await sem.GetViewFieldAsync();
        double mag = await sem.Optics.GetMagnificationAsync();
        Console.WriteLine($"View Field: {vf:F1} µm");
        Console.WriteLine($"Magnification: {mag:F0}X");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"View Field: (unavailable - {ex.Message})");
    }
    
    // Query stage position
    Console.WriteLine("\n--- Stage Position ---");
    try
    {
        StagePosition pos = await sem.GetStagePositionAsync();
        Console.WriteLine($"Stage Position:");
        Console.WriteLine($"  X: {pos.X:F3} mm");
        Console.WriteLine($"  Y: {pos.Y:F3} mm");
        Console.WriteLine($"  Z: {pos.Z?.ToString("F3") ?? "N/A"} mm");
        Console.WriteLine($"  Rotation: {pos.Rotation?.ToString("F2") ?? "N/A"}°");
        Console.WriteLine($"  Tilt: {pos.TiltX?.ToString("F2") ?? "N/A"}°");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Stage Position: (unavailable - {ex.Message})");
    }
    
    try
    {
        bool isHomed = await sem.IsStageCallibratedAsync();
        Console.WriteLine($"Stage Homed: {isHomed}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Stage Homed: (unavailable - {ex.Message})");
    }
    
    // Test image acquisition
    Console.WriteLine("\n--- Image Acquisition Test ---");
    Console.WriteLine("Acquiring single image...");
    try
    {
        SemImage image = await sem.AcquireSingleImageAsync(0, 1024, 768);
        Console.WriteLine($"Image acquired: {image.Width}x{image.Height}, {image.BitsPerPixel} bpp");
        Console.WriteLine($"Data size: {image.Data?.Length ?? 0} bytes");
        
        Console.WriteLine("\nSaving image to C:\\Temp...");
        string savedPath = await sem.AcquireAndSaveImageAsync(@"C:\Temp");
        Console.WriteLine($"Image saved to: {savedPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Image acquisition failed: {ex.Message}");
    }
    
    // Disconnect
    Console.WriteLine("\n--- Disconnecting ---");
    await sem.DisconnectAsync();
    Console.WriteLine("Disconnected from Thermo Fisher SEM");
}
catch (Exception ex)
{
    Console.WriteLine($"\nConnection Error: {ex.Message}");
    Console.WriteLine($"\nMake sure:");
    Console.WriteLine($"  1. The Thermo Fisher SEM is running and accessible");
    Console.WriteLine($"  2. AutoScript server is enabled on the SEM");
    Console.WriteLine($"  3. The host ({host}) and port ({port}) are correct");
    Console.WriteLine($"\nUsage: SemController.Thermo.Example [host] [port]");
    Console.WriteLine($"  Example: SemController.Thermo.Example 192.168.1.100 7520");
}

Console.WriteLine("\n=== Demo Complete ===");
