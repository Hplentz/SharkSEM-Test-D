using SemController.Core.Interfaces;
using SemController.Core.Models;
using AutoScript.Clients;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemController : ISemController
{
    private readonly string _host;
    private readonly int _port;
    private SdbMicroscopeClient? _client;
    private bool _disposed;

    public ThermoSemVacuum Vacuum { get; private set; } = null!;
    public ThermoSemBeam Beam { get; private set; } = null!;
    public ThermoSemStage Stage { get; private set; } = null!;
    public ThermoSemOptics Optics { get; private set; } = null!;
    public ThermoSemScanning Scanning { get; private set; } = null!;
    public ThermoSemMisc Misc { get; private set; } = null!;

    public bool IsConnected => _client != null;

    public ThermoSemController(string host = "localhost", int port = 7520)
    {
        _host = host;
        _port = port;
        InitializeSubModules();
    }

    private void InitializeSubModules()
    {
        Vacuum = new ThermoSemVacuum(GetClient);
        Beam = new ThermoSemBeam(GetClient);
        Stage = new ThermoSemStage(GetClient);
        Optics = new ThermoSemOptics(GetClient);
        Scanning = new ThermoSemScanning(GetClient);
        Misc = new ThermoSemMisc(GetClient);
    }

    private SdbMicroscopeClient GetClient()
    {
        EnsureConnected();
        return _client!;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _client = new SdbMicroscopeClient();
            _client.Connect(_host, _port);
        }, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _client?.Disconnect();
            _client = null;
        }, cancellationToken);
    }

    public Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
        => Misc.GetMicroscopeInfoAsync(cancellationToken);

    public Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default)
        => Vacuum.GetStatusAsync(cancellationToken);

    public Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
        => Vacuum.GetPressureAsync(gauge, cancellationToken);

    public Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default)
        => Vacuum.GetModeAsync(cancellationToken);

    public Task PumpAsync(CancellationToken cancellationToken = default)
        => Vacuum.PumpAsync(cancellationToken);

    public Task VentAsync(CancellationToken cancellationToken = default)
        => Vacuum.VentAsync(cancellationToken);

    public Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
        => Beam.GetStateAsync(cancellationToken);

    public Task BeamOnAsync(CancellationToken cancellationToken = default)
        => Beam.TurnOnAsync(cancellationToken);

    public Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
        => Beam.WaitForOnAsync(timeoutMs, cancellationToken);

    public Task BeamOffAsync(CancellationToken cancellationToken = default)
        => Beam.TurnOffAsync(cancellationToken);

    public Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
        => Beam.GetHighVoltageAsync(cancellationToken);

    public Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => Beam.SetHighVoltageAsync(voltage, waitForCompletion, cancellationToken);

    public Task<string> AcquireAndSaveImageAsync(string? outputPath = null, CancellationToken cancellationToken = default)
        => Scanning.AcquireAndSaveImageAsync(outputPath, cancellationToken);

    public Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
        => Beam.GetEmissionCurrentAsync(cancellationToken);

    public Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
        => Beam.GetBlankerModeAsync(cancellationToken);

    public Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
        => Beam.SetBlankerModeAsync(mode, cancellationToken);

    public Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default)
        => Stage.GetPositionAsync(cancellationToken);

    public Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => Stage.MoveAbsoluteAsync(position, waitForCompletion, cancellationToken);

    public Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => Stage.MoveRelativeAsync(delta, waitForCompletion, cancellationToken);

    public Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default)
        => Stage.IsMovingAsync(cancellationToken);

    public Task StopStageAsync(CancellationToken cancellationToken = default)
        => Stage.StopAsync(cancellationToken);

    public Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default)
        => Stage.GetLimitsAsync(cancellationToken);

    public Task CalibrateStageAsync(CancellationToken cancellationToken = default)
        => Stage.CalibrateAsync(cancellationToken);

    public Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default)
        => Stage.IsCalibratedAsync(cancellationToken);

    public Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
        => Optics.GetViewFieldAsync(cancellationToken);

    public Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
        => Optics.SetViewFieldAsync(viewFieldMicrons, cancellationToken);

    public Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
        => Optics.GetWorkingDistanceAsync(cancellationToken);

    public Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
        => Optics.SetWorkingDistanceAsync(workingDistanceMm, cancellationToken);

    public Task<double> GetFocusAsync(CancellationToken cancellationToken = default)
        => Optics.GetFocusAsync(cancellationToken);

    public Task SetFocusAsync(double focus, CancellationToken cancellationToken = default)
        => Optics.SetFocusAsync(focus, cancellationToken);

    public Task AutoFocusAsync(CancellationToken cancellationToken = default)
        => Optics.AutoFocusAsync(cancellationToken);

    public Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
        => Optics.GetSpotSizeAsync(cancellationToken);

    public Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default)
        => Scanning.GetSpeedAsync(cancellationToken);

    public Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default)
        => Scanning.SetSpeedAsync(speed, cancellationToken);

    public Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
        => Scanning.AcquireImagesAsync(settings, cancellationToken);

    public Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
        => Scanning.AcquireSingleImageAsync(channel, width, height, cancellationToken);

    public Task StopScanAsync(CancellationToken cancellationToken = default)
        => Scanning.StopAsync(cancellationToken);

    private void EnsureConnected()
    {
        if (_client == null)
            throw new InvalidOperationException("Not connected to microscope. Call ConnectAsync first.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Disconnect();
            _client = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
