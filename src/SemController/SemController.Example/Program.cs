using SemController.Core.Factory;
using SemController.Core.Models;
using SemController.Core.Interfaces;

Console.WriteLine("=== SEM Controller Library Demo ===\n");

Console.WriteLine("This library provides a unified interface for controlling Scanning Electron Microscopes.");
Console.WriteLine("It abstracts different SEM manufacturers behind a common interface.\n");

Console.WriteLine("Supported SEM Types:");
Console.WriteLine("  - TESCAN (via SharkSEM protocol)");
Console.WriteLine("  - Mock/Simulator (for testing without hardware)\n");

Console.WriteLine("--- Running Mock SEM Demo ---\n");

using (ISemController sem = SemControllerFactory.CreateMock())
{
    await sem.ConnectAsync();
    Console.WriteLine("Connected to mock SEM");
    
    var info = await sem.GetMicroscopeInfoAsync();
    Console.WriteLine($"\nMicroscope Info:");
    Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
    Console.WriteLine($"  Model: {info.Model}");
    Console.WriteLine($"  Serial: {info.SerialNumber}");
    Console.WriteLine($"  Software: {info.SoftwareVersion}");
    Console.WriteLine($"  Protocol: {info.ProtocolVersion}");
    
    var vacStatus = await sem.GetVacuumStatusAsync();
    Console.WriteLine($"\nVacuum Status: {vacStatus}");
    
    var pressure = await sem.GetVacuumPressureAsync(VacuumGauge.Chamber);
    Console.WriteLine($"Chamber Pressure: {pressure:E2} Pa");
    
    var beamState = await sem.GetBeamStateAsync();
    Console.WriteLine($"\nBeam State: {beamState}");
    
    Console.WriteLine("\nTurning beam ON...");
    await sem.BeamOnAsync();
    beamState = await sem.GetBeamStateAsync();
    Console.WriteLine($"Beam State: {beamState}");
    
    var hv = await sem.GetHighVoltageAsync();
    Console.WriteLine($"\nHigh Voltage: {hv / 1000:F1} kV");
    
    Console.WriteLine("Setting HV to 20 kV...");
    await sem.SetHighVoltageAsync(20000);
    hv = await sem.GetHighVoltageAsync();
    Console.WriteLine($"High Voltage: {hv / 1000:F1} kV");
    
    var emission = await sem.GetEmissionCurrentAsync();
    Console.WriteLine($"Emission Current: {emission * 1e6:F1} uA");
    
    var stagePos = await sem.GetStagePositionAsync();
    Console.WriteLine($"\nStage Position: {stagePos}");
    
    var limits = await sem.GetStageLimitsAsync();
    Console.WriteLine($"Stage X Range: {limits.MinX * 1000:F1} to {limits.MaxX * 1000:F1} mm");
    Console.WriteLine($"Stage Y Range: {limits.MinY * 1000:F1} to {limits.MaxY * 1000:F1} mm");
    Console.WriteLine($"Stage Z Range: {limits.MinZ * 1000:F1} to {limits.MaxZ * 1000:F1} mm");
    
    Console.WriteLine("\nMoving stage to X=10mm, Y=5mm...");
    await sem.MoveStageAsync(new StagePosition(0.010, 0.005));
    stagePos = await sem.GetStagePositionAsync();
    Console.WriteLine($"New Position: {stagePos}");
    
    var mag = await sem.GetMagnificationAsync();
    Console.WriteLine($"\nMagnification: {mag}x");
    
    Console.WriteLine("Setting magnification to 5000x...");
    await sem.SetMagnificationAsync(5000);
    mag = await sem.GetMagnificationAsync();
    Console.WriteLine($"Magnification: {mag}x");
    
    var wd = await sem.GetWorkingDistanceAsync();
    Console.WriteLine($"\nWorking Distance: {wd * 1000:F2} mm");
    
    Console.WriteLine("\nRunning AutoFocus...");
    await sem.AutoFocusAsync();
    wd = await sem.GetWorkingDistanceAsync();
    Console.WriteLine($"Working Distance: {wd * 1000:F2} mm");
    
    Console.WriteLine("\nAcquiring sample image (256x256)...");
    var image = await sem.AcquireSingleImageAsync(0, 256, 256);
    Console.WriteLine($"Image acquired: {image.Width}x{image.Height}, {image.Data.Length} bytes, Channel {image.Channel}");
    
    await sem.BeamOffAsync();
    Console.WriteLine("\nBeam turned OFF");
    
    await sem.DisconnectAsync();
    Console.WriteLine("Disconnected\n");
}

Console.WriteLine("--- TESCAN Connection Example (not executed) ---\n");
Console.WriteLine("To connect to a real TESCAN SEM, use:");
Console.WriteLine("");
Console.WriteLine("  var settings = SemConnectionSettings.Tescan(\"192.168.1.100\", 8300);");
Console.WriteLine("  using var sem = await SemControllerFactory.CreateAndConnectAsync(settings);");
Console.WriteLine("");
Console.WriteLine("  // Or directly:");
Console.WriteLine("  using var sem = SemControllerFactory.CreateTescan(\"192.168.1.100\");");
Console.WriteLine("  await sem.ConnectAsync();");
Console.WriteLine("");
Console.WriteLine("The same ISemController interface is used regardless of microscope type.\n");

Console.WriteLine("=== Demo Complete ===");
