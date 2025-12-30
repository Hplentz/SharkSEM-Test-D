// =============================================================================
// MockSemController.cs - Hardware Simulator for Testing
// =============================================================================
// Provides a fully functional ISemController implementation without requiring
// actual microscope hardware. Useful for:
//   - UI development and testing without microscope access
//   - Unit testing of application logic
//   - Demonstration and training purposes
//
// Behavior:
//   - Maintains internal state for all SEM parameters
//   - Simulates realistic delays for operations (pump, stage move, etc.)
//   - Generates random noise patterns for image acquisition
//   - Returns plausible values with small random variations
// =============================================================================

using SemController.Core.Interfaces;
using SemController.Core.Models;

namespace SemController.Core.Implementations;

/// <summary>
/// Mock SEM controller for testing and development without hardware.
/// Simulates all ISemController operations with realistic behavior.
/// </summary>
public class MockSemController : ISemController
{
    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------
    // These fields maintain the simulated microscope state between calls.
    
    private bool _isConnected;
    private StagePosition _currentPosition = new StagePosition(0, 0, 0.015, 0, 0);
    private VacuumStatus _vacuumStatus = VacuumStatus.Ready;
    private BeamState _beamState = BeamState.Off;
    private double _highVoltage = 15000;        // Volts
    private double _workingDistance = 0.010;    // mm (10mm working distance)
    private double _viewFieldMicrons = 100;     // µm
    private int _scanSpeed = 5;
    private BlankerMode _blankerMode = BlankerMode.Off;
    private double _spotSize = 3.0;
    private bool _isStageMoving;
    private readonly Random _random = new Random(); // For simulating noise/variation
    
    // -------------------------------------------------------------------------
    // Connection Management
    // -------------------------------------------------------------------------
    
    public bool IsConnected => _isConnected;
    
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }
    
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        return Task.CompletedTask;
    }
    
    // -------------------------------------------------------------------------
    // Microscope Information
    // -------------------------------------------------------------------------
    
    public Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MicroscopeInfo
        {
            Manufacturer = "MockSEM",
            Model = "SIMULATOR-1000",
            SerialNumber = "MOCK123456",
            SoftwareVersion = "1.0.0",
            ProtocolVersion = "3.2.20"
        });
    }
    
    // -------------------------------------------------------------------------
    // Vacuum System
    // -------------------------------------------------------------------------
    // Simulates pump-down and venting with realistic delays.
    
    public Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_vacuumStatus);
    
    public Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        // Return realistic base pressures for different gauges with small random variation
        double basePressure = gauge switch
        {
            VacuumGauge.Chamber => 1e-3,   // ~10^-3 Pa for chamber
            VacuumGauge.SemGun => 1e-7,    // ~10^-7 Pa for gun (much better vacuum)
            VacuumGauge.SemColumn => 1e-5, // ~10^-5 Pa for column
            _ => 1e-4
        };
        return Task.FromResult(basePressure * (1 + _random.NextDouble() * 0.1));
    }
    
    public Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(VacuumMode.HighVacuum);
    
    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        _vacuumStatus = VacuumStatus.Pumping;
        await Task.Delay(1000, cancellationToken); // Simulate pump-down time
        _vacuumStatus = VacuumStatus.Ready;
    }
    
    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        _vacuumStatus = VacuumStatus.Venting;
        await Task.Delay(1000, cancellationToken); // Simulate venting time
        _vacuumStatus = VacuumStatus.ChamberOpen;
    }
    
    // -------------------------------------------------------------------------
    // Electron Beam Control
    // -------------------------------------------------------------------------
    
    public Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_beamState);
    
    public Task BeamOnAsync(CancellationToken cancellationToken = default)
    {
        _beamState = BeamState.On;
        return Task.CompletedTask;
    }
    
    public Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_beamState == BeamState.On);
    }
    
    public Task BeamOffAsync(CancellationToken cancellationToken = default)
    {
        _beamState = BeamState.Off;
        return Task.CompletedTask;
    }
    
    // -------------------------------------------------------------------------
    // High Voltage and Emission
    // -------------------------------------------------------------------------
    
    public Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_highVoltage);
    
    public Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        // Clamp to typical SEM voltage range
        _highVoltage = Math.Clamp(voltage, 200, 30000);
        return Task.CompletedTask;
    }
    
    public Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default) =>
        // Return ~100µA when beam is on, 0 when off, with small random variation
        Task.FromResult(_beamState == BeamState.On ? 100e-6 + _random.NextDouble() * 10e-6 : 0.0);
    
    // -------------------------------------------------------------------------
    // Stage Control
    // -------------------------------------------------------------------------
    // Simulates stage movement with delays for realistic behavior.
    
    public Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new StagePosition(_currentPosition.X, _currentPosition.Y, _currentPosition.Z, 
            _currentPosition.Rotation, _currentPosition.TiltX, _currentPosition.TiltY));
    
    public async Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        _isStageMoving = true;
        
        if (waitForCompletion)
        {
            await Task.Delay(500, cancellationToken); // Simulate movement time
        }
        
        _currentPosition = position;
        _isStageMoving = false;
    }
    
    public async Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        // Calculate new absolute position from current position + delta
        StagePosition newPosition = new StagePosition(
            _currentPosition.X + delta.X,
            _currentPosition.Y + delta.Y,
            _currentPosition.Z + delta.Z,
            _currentPosition.Rotation + delta.Rotation,
            _currentPosition.TiltX + delta.TiltX,
            _currentPosition.TiltY.HasValue && delta.TiltY.HasValue 
                ? _currentPosition.TiltY + delta.TiltY 
                : _currentPosition.TiltY ?? delta.TiltY
        );
        
        await MoveStageAsync(newPosition, waitForCompletion, cancellationToken);
    }
    
    public Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_isStageMoving);
    
    public Task StopStageAsync(CancellationToken cancellationToken = default)
    {
        _isStageMoving = false;
        return Task.CompletedTask;
    }
    
    public Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default)
    {
        // Return typical stage limits (in mm for linear, degrees for angular)
        return Task.FromResult(new StageLimits
        {
            MinX = -0.065, MaxX = 0.065,       // ±65mm X travel
            MinY = -0.065, MaxY = 0.065,       // ±65mm Y travel
            MinZ = 0, MaxZ = 0.040,            // 0-40mm Z travel
            MinRotation = -180, MaxRotation = 180,  // Full rotation
            MinTiltX = -10, MaxTiltX = 60,     // -10° to +60° tilt
            MinTiltY = -10, MaxTiltY = 10      // ±10° compustage tilt
        });
    }
    
    public Task CalibrateStageAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    
    public Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    
    // -------------------------------------------------------------------------
    // Electron Optics
    // -------------------------------------------------------------------------
    
    public Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_viewFieldMicrons);
    
    public Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        // Clamp to typical view field range
        _viewFieldMicrons = Math.Clamp(viewFieldMicrons, 1, 100000);
        return Task.CompletedTask;
    }
    
    public Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_workingDistance);
    
    public Task SetWorkingDistanceAsync(double workingDistance, CancellationToken cancellationToken = default)
    {
        _workingDistance = workingDistance;
        return Task.CompletedTask;
    }
    
    public Task<double> GetFocusAsync(CancellationToken cancellationToken = default) =>
        GetWorkingDistanceAsync(cancellationToken);
    
    public Task SetFocusAsync(double focus, CancellationToken cancellationToken = default) =>
        SetWorkingDistanceAsync(focus, cancellationToken);
    
    public async Task AutoFocusAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken); // Simulate autofocus time
        // Set to near 10mm with small random variation
        _workingDistance = 0.010 + _random.NextDouble() * 0.001;
    }
    
    // -------------------------------------------------------------------------
    // Scanning Control
    // -------------------------------------------------------------------------
    
    public Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_scanSpeed);
    
    public Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        _scanSpeed = Math.Clamp(speed, 1, 10);
        return Task.CompletedTask;
    }
    
    public Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_blankerMode);
    
    public Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        _blankerMode = mode;
        return Task.CompletedTask;
    }
    
    // -------------------------------------------------------------------------
    // Image Acquisition
    // -------------------------------------------------------------------------
    // Generates random noise patterns resembling SEM images.
    
    public Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        List<SemImage> images = new List<SemImage>();
        
        foreach (int channel in settings.Channels)
        {
            byte[] data = new byte[settings.Width * settings.Height];
            _random.NextBytes(data);
            
            // Reduce contrast to make it look more like a real SEM image (less pure noise)
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(128 + (data[i] - 128) * 0.3);
            }
            
            images.Add(new SemImage(settings.Width, settings.Height, data, channel));
        }
        
        return Task.FromResult(images.ToArray());
    }
    
    public async Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
    {
        SemImage[] images = await AcquireImagesAsync(new ScanSettings
        {
            Width = width,
            Height = height,
            Channels = new[] { channel }
        }, cancellationToken);
        
        return images.FirstOrDefault() ?? new SemImage(width, height, Array.Empty<byte>(), channel);
    }
    
    public Task StopScanAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    
    // -------------------------------------------------------------------------
    // Additional Optics
    // -------------------------------------------------------------------------
    
    public Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_spotSize);
    
    // -------------------------------------------------------------------------
    // Resource Cleanup
    // -------------------------------------------------------------------------
    
    public void Dispose() { } // No resources to release in mock
}
