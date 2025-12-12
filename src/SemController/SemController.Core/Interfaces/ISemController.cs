using SemController.Core.Models;

namespace SemController.Core.Interfaces;

public interface ISemController : IDisposable
{
    bool IsConnected { get; }
    
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    
    Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default);
    
    Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default);
    Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default);
    Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default);
    Task PumpAsync(CancellationToken cancellationToken = default);
    Task VentAsync(CancellationToken cancellationToken = default);
    
    Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default);
    Task BeamOnAsync(CancellationToken cancellationToken = default);
    Task BeamOffAsync(CancellationToken cancellationToken = default);
    
    Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default);
    Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default);
    
    Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default);
    
    Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default);
    Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default);
    Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default);
    Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default);
    Task StopStageAsync(CancellationToken cancellationToken = default);
    Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default);
    Task CalibrateStageAsync(CancellationToken cancellationToken = default);
    Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default);
    
    Task<double> GetMagnificationAsync(CancellationToken cancellationToken = default);
    Task SetMagnificationAsync(double magnification, CancellationToken cancellationToken = default);
    
    Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default);
    Task SetWorkingDistanceAsync(double workingDistance, CancellationToken cancellationToken = default);
    
    Task<double> GetFocusAsync(CancellationToken cancellationToken = default);
    Task SetFocusAsync(double focus, CancellationToken cancellationToken = default);
    Task AutoFocusAsync(CancellationToken cancellationToken = default);
    
    Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default);
    Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default);
    Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default);
    Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default);
    
    Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default);
    Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default);
    Task StopScanAsync(CancellationToken cancellationToken = default);
    
    Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default);
    Task SetSpotSizeAsync(double spotSize, CancellationToken cancellationToken = default);
}
