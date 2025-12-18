using SemController.Core.Factory;
using SemController.Core.Models;
using SemController.Core.Interfaces;
using SemController.Core.Implementations;
using System.Drawing;
using System.Drawing.Imaging;

Console.WriteLine("=== SEM Controller Library Demo ===\n");

Console.WriteLine("This library provides a unified interface for controlling Scanning Electron Microscopes.");
Console.WriteLine("It abstracts different SEM manufacturers behind a common interface.\n");

Console.WriteLine("Supported SEM Types:");
Console.WriteLine("  - TESCAN (via SharkSEM protocol)");
Console.WriteLine("  - Mock/Simulator (for testing without hardware)\n");

Console.WriteLine("--- Connecting to TESCAN SEM at 127.0.0.1 ---\n");

using (var sem = new TescanSemController("127.0.0.1"))
{
    await sem.ConnectAsync();
    Console.WriteLine("Connected to TESCAN SEM");
    
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
    
    Console.WriteLine("\n--- Detector Configuration ---");
    var detectorsStr = await sem.EnumDetectorsAsync();
    Console.WriteLine($"Available Detectors: {detectorsStr}");
    
    var channelCount = await sem.GetChannelCountAsync();
    Console.WriteLine($"Number of Channels: {channelCount}");
    
    for (int ch = 0; ch < Math.Min(channelCount, 4); ch++)
    {
        var selectedDet = await sem.GetSelectedDetectorAsync(ch);
        var (enabled, bpp) = await sem.GetChannelEnabledAsync(ch);
        Console.WriteLine($"  Channel {ch}: Detector={selectedDet}, Enabled={enabled}, BPP={bpp}");
    }
    
    var detectorNames = detectorsStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
    int bseDetectorIndex = -1;
    for (int i = 0; i < detectorNames.Length; i++)
    {
        if (detectorNames[i].Contains("BSE", StringComparison.OrdinalIgnoreCase) ||
            detectorNames[i].Contains("Back", StringComparison.OrdinalIgnoreCase))
        {
            bseDetectorIndex = i;
            Console.WriteLine($"\nFound BSE detector at index {i}: {detectorNames[i]}");
            break;
        }
    }
    
    int imageChannel = 0;
    if (bseDetectorIndex >= 0)
    {
        Console.WriteLine($"Selecting BSE detector (index {bseDetectorIndex}) for channel {imageChannel}...");
        await sem.SelectDetectorAsync(imageChannel, bseDetectorIndex);
    }
    
    Console.WriteLine($"Enabling channel {imageChannel} with 8-bit depth...");
    await sem.EnableChannelAsync(imageChannel, true, 8);
    
    var (en, bp) = await sem.GetChannelEnabledAsync(imageChannel);
    Console.WriteLine($"Channel {imageChannel} status: Enabled={en}, BPP={bp}");
    
    var beamState = await sem.GetBeamStateAsync();
    Console.WriteLine($"\nBeam State: {beamState}");
    
    Console.WriteLine("\nTurning beam ON...");
    await sem.BeamOnAsync();
    beamState = await sem.GetBeamStateAsync();
    Console.WriteLine($"Beam State: {beamState}");
    
    if (beamState == BeamState.Transitioning)
    {
        Console.WriteLine("Waiting for beam to stabilize...");
        var beamReady = await sem.WaitForBeamOnAsync(30000);
        beamState = await sem.GetBeamStateAsync();
        Console.WriteLine($"Beam State: {beamState} (ready: {beamReady})");
    }
    
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
    Console.WriteLine($"Stage X Range: {limits.MinX:F1} to {limits.MaxX:F1} mm");
    Console.WriteLine($"Stage Y Range: {limits.MinY:F1} to {limits.MaxY:F1} mm");
    Console.WriteLine($"Stage Z Range: {limits.MinZ:F1} to {limits.MaxZ:F1} mm");
    
    var isCalibrated = await sem.Stage.IsCallibratedAsync();
    var isBusy = await sem.Stage.IsMovingAsync();
    Console.WriteLine($"\nStage Calibrated: {isCalibrated}, Stage Busy: {isBusy}");
    
    if (!isCalibrated)
    {
        Console.WriteLine("WARNING: Stage is not calibrated - StgMoveTo will be ignored!");
        Console.WriteLine("To calibrate, call: await sem.Stage.CalibrateAsync();");
    }
    
    Console.WriteLine("\nMoving stage to X=10mm, Y=5mm...");
    await sem.MoveStageAsync(new StagePosition(10.0, 5.0));
    stagePos = await sem.GetStagePositionAsync();
    Console.WriteLine($"New Position: {stagePos}");
    
    var viewField = await sem.GetViewFieldAsync();
    Console.WriteLine($"\nView Field: {viewField:F1} um");
    
    Console.WriteLine("Setting view field to 1000 um...");
    await sem.SetViewFieldAsync(1000);
    viewField = await sem.GetViewFieldAsync();
    Console.WriteLine($"View Field: {viewField:F1} um");
    
    var wd = await sem.GetWorkingDistanceAsync();
    Console.WriteLine($"\nWorking Distance: {wd:F2} mm");
    
    Console.WriteLine("\nRunning AutoFocus...");
    await sem.AutoFocusAsync();
    wd = await sem.GetWorkingDistanceAsync();
    Console.WriteLine($"Working Distance: {wd:F2} mm");
    
    Console.WriteLine($"\nRunning AutoSignal on channel {imageChannel}...");
    await sem.AutoSignalAsync(imageChannel);
    
    Console.WriteLine($"\nAcquiring image (256x256) on channel {imageChannel}...");
    var image = await sem.AcquireSingleImageAsync(imageChannel, 256, 256);
    Console.WriteLine($"Image acquired: {image.Width}x{image.Height}, {image.Data.Length} bytes, Channel {image.Channel}");
    
    if (image.Data.Length > 0)
    {
        var minVal = image.Data.Min();
        var maxVal = image.Data.Max();
        var avgVal = image.Data.Average(b => (double)b);
        Console.WriteLine($"Image statistics: Min={minVal}, Max={maxVal}, Avg={avgVal:F1}");
    }
    
    var outputPath = Path.Combine(@"C:\Temp", $"SEM_Image_{DateTime.Now:yyyyMMdd_HHmmss}.png");
    try
    {
        Directory.CreateDirectory(@"C:\Temp");
        using var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed);
        var palette = bitmap.Palette;
        for (int i = 0; i < 256; i++)
        {
            palette.Entries[i] = Color.FromArgb(i, i, i);
        }
        bitmap.Palette = palette;
        
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), 
            ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
        for (int y = 0; y < image.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(image.Data, y * image.Width, 
                bitmapData.Scan0 + y * bitmapData.Stride, image.Width);
        }
        bitmap.UnlockBits(bitmapData);
        
        bitmap.Save(outputPath, ImageFormat.Png);
        Console.WriteLine($"Image saved to: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not save image: {ex.Message}");
    }
    
    Console.WriteLine("\nBeam left ON for inspection");
    
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
