using SemController.Core.Factory;
using SemController.Core.Models;
using SemController.Core.Implementations.Thermo;

Console.WriteLine("=== Thermo Fisher Scientific SEM Controller Demo ===\n");

Console.WriteLine("This demo tests the Thermo Fisher SEM controller via AutoScript API.");
Console.WriteLine("The library provides the same ISemController interface for all SEM vendors.\n");

var host = "localhost";
var port = 7520;
var debugMode = false;

if (args.Length >= 1)
    host = args[0];
if (args.Length >= 2 && int.TryParse(args[1], out var parsedPort))
    port = parsedPort;
if (args.Contains("--debug") || args.Contains("-d"))
    debugMode = true;

Console.WriteLine($"--- Connecting to Thermo Fisher SEM at {host}:{port} ---");
if (debugMode)
    Console.WriteLine("(Debug mode enabled)\n");
else
    Console.WriteLine("(Use --debug for detailed API diagnostics)\n");

try
{
    using var sem = new ThermoSemController(host, port);
    await sem.ConnectAsync();
    Console.WriteLine("Connected to Thermo Fisher SEM");
    
    var info = await sem.GetMicroscopeInfoAsync();
    Console.WriteLine($"\nMicroscope Info:");
    Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
    Console.WriteLine($"  Model: {info.Model}");
    Console.WriteLine($"  Serial: {info.SerialNumber}");
    Console.WriteLine($"  Software: {info.SoftwareVersion}");
    Console.WriteLine($"  Protocol: {info.ProtocolVersion}");
    
    Console.WriteLine("\n--- Vacuum Status ---");
    var vacStatus = await sem.GetVacuumStatusAsync();
    Console.WriteLine($"Vacuum Status: {vacStatus}");
    
    try
    {
        if (debugMode)
        {
            var (pressure, rawState, debugInfo) = await sem.Vacuum.GetPressureWithDebugAsync();
            Console.WriteLine($"Chamber Pressure: {pressure:E2} Pa");
            Console.WriteLine($"  Raw State: {rawState}");
            Console.WriteLine($"  Debug Info:\n{debugInfo}");
        }
        else
        {
            var pressure = await sem.GetVacuumPressureAsync(VacuumGauge.Chamber);
            Console.WriteLine($"Chamber Pressure: {pressure:E2} Pa");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chamber Pressure: (unavailable - {ex.Message})");
    }
    
    Console.WriteLine("\n--- Beam Status ---");
    var beamState = await sem.GetBeamStateAsync();
    Console.WriteLine($"Beam State: {beamState}");
    
    try
    {
        var hv = await sem.GetHighVoltageAsync();
        Console.WriteLine($"High Voltage: {hv:F0} V");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"High Voltage: (unavailable - {ex.Message})");
    }
    
    try
    {
        if (debugMode)
        {
            var (current, debugInfo) = await sem.Beam.GetEmissionCurrentWithDebugAsync();
            var emissionMicroAmps = current * 1e6;
            Console.WriteLine($"Emission Current: {current:E3} A ({emissionMicroAmps:F1} µA)");
            Console.WriteLine($"  Debug Info:\n{debugInfo}");
        }
        else
        {
            var emission = await sem.GetEmissionCurrentAsync();
            var emissionMicroAmps = emission * 1e6;
            Console.WriteLine($"Emission Current: {emission:E3} A ({emissionMicroAmps:F1} µA)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Emission Current: (unavailable - {ex.Message})");
    }
    
    Console.WriteLine("\n--- Electron Optics ---");
    try
    {
        if (debugMode)
        {
            var (wdMm, debugInfo) = await sem.Optics.GetWorkingDistanceWithDebugAsync();
            Console.WriteLine($"Working Distance: {wdMm:F3} mm");
            Console.WriteLine($"  Debug Info:\n{debugInfo}");
        }
        else
        {
            var wd = await sem.GetWorkingDistanceAsync();
            Console.WriteLine($"Working Distance: {wd:F3} mm");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Working Distance: (unavailable - {ex.Message})");
    }
    
    try
    {
        var vf = await sem.GetViewFieldAsync();
        var mag = await sem.Optics.GetMagnificationAsync();
        Console.WriteLine($"View Field: {vf:F1} µm");
        Console.WriteLine($"Magnification: {mag:F0}X");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"View Field: (unavailable - {ex.Message})");
    }
    
    Console.WriteLine("\n--- Stage Position ---");
    try
    {
        var pos = await sem.GetStagePositionAsync();
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
        var isHomed = await sem.IsStageCallibratedAsync();
        Console.WriteLine($"Stage Homed: {isHomed}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Stage Homed: (unavailable - {ex.Message})");
    }
    
    Console.WriteLine("\n--- Image Acquisition Test ---");
    Console.WriteLine("Acquiring single image...");
    try
    {
        var image = await sem.AcquireSingleImageAsync(0, 1024, 768);
        Console.WriteLine($"Image acquired: {image.Width}x{image.Height}, {image.BitsPerPixel} bpp");
        Console.WriteLine($"Data size: {image.Data?.Length ?? 0} bytes");
        
        Console.WriteLine("\nSaving image to C:\\Temp...");
        var savedPath = await sem.AcquireAndSaveImageAsync(@"C:\Temp");
        Console.WriteLine($"Image saved to: {savedPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Image acquisition failed: {ex.Message}");
    }
    
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
    Console.WriteLine($"\nUsage: SemController.Thermo.Example [host] [port] [--debug]");
    Console.WriteLine($"  Example: SemController.Thermo.Example 192.168.1.100 7520");
    Console.WriteLine($"  Example: SemController.Thermo.Example 192.168.1.100 7520 --debug");
}

Console.WriteLine("\n=== Demo Complete ===");
