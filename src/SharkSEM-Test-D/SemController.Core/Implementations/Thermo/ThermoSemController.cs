// =============================================================================
// ThermoSemController.cs - Thermo Fisher Scientific SEM Controller
// =============================================================================
// Implements ISemController for Thermo Fisher Scientific microscopes using
// the AutoScript C# API (COM-based wrapper around Python AutoScript).
//
// Architecture:
// This controller uses a modular composition pattern where functionality is
// delegated to specialized sub-modules:
//   - ThermoSemVacuum: Vacuum system control
//   - ThermoSemBeam: Electron beam and high voltage control
//   - ThermoSemStage: Specimen stage control
//   - ThermoSemOptics: Electron optics (focus, view field, etc.)
//   - ThermoSemScanning: Image acquisition
//   - ThermoSemMisc: Microscope information and miscellaneous functions
//
// CRITICAL: When accessing AutoScript service properties, you MUST use 'var'
// instead of explicit 'dynamic' declarations. Using 'dynamic' explicitly causes
// RuntimeBinderException because it loses type information needed for COM
// property resolution. See ThermoSemMisc.cs for example.
//
// The AutoScript API is synchronous, so all async methods wrap operations
// in Task.Run() to avoid blocking the calling thread.
// =============================================================================

using SemController.Core.Interfaces;
using SemController.Core.Models;
using AutoScript.Clients;

namespace SemController.Core.Implementations.Thermo;

/// <summary>
/// ISemController implementation for Thermo Fisher Scientific SEMs.
/// Uses AutoScript API for microscope communication.
/// </summary>
public class ThermoSemController : ISemController
{
    private readonly string _host;
    private readonly int _port;
    private SdbMicroscopeClient? _client;
    private bool _disposed;

    // -------------------------------------------------------------------------
    // Sub-Module Properties
    // -------------------------------------------------------------------------
    // Each sub-module handles a specific functional domain.
    // This delegation pattern keeps code organized and testable.
    
    /// <summary>Vacuum system control (pump, vent, pressure readings).</summary>
    public ThermoSemVacuum Vacuum { get; private set; } = null!;
    
    /// <summary>Electron beam control (on/off, voltage, emission).</summary>
    public ThermoSemBeam Beam { get; private set; } = null!;
    
    /// <summary>Specimen stage control (movement, position, limits).</summary>
    public ThermoSemStage Stage { get; private set; } = null!;
    
    /// <summary>Electron optics control (focus, view field, working distance).</summary>
    public ThermoSemOptics Optics { get; private set; } = null!;
    
    /// <summary>Image acquisition and scanning control.</summary>
    public ThermoSemScanning Scanning { get; private set; } = null!;
    
    /// <summary>Miscellaneous functions (microscope info).</summary>
    public ThermoSemMisc Misc { get; private set; } = null!;

    /// <summary>Returns true if connected to the microscope.</summary>
    public bool IsConnected => _client != null;

    /// <summary>
    /// Creates a new Thermo Fisher SEM controller.
    /// </summary>
    /// <param name="host">AutoScript server host (typically "localhost").</param>
    /// <param name="port">AutoScript server port (typically 7520).</param>
    public ThermoSemController(string host = "localhost", int port = 7520)
    {
        _host = host;
        _port = port;
        InitializeSubModules();
    }

    /// <summary>
    /// Initializes all sub-modules with a delegate that provides the client.
    /// Using a delegate (Func) allows sub-modules to access the client lazily,
    /// ensuring they always get the current connected client instance.
    /// </summary>
    private void InitializeSubModules()
    {
        Vacuum = new ThermoSemVacuum(GetClient);
        Beam = new ThermoSemBeam(GetClient);
        Stage = new ThermoSemStage(GetClient);
        Optics = new ThermoSemOptics(GetClient);
        Scanning = new ThermoSemScanning(GetClient);
        Misc = new ThermoSemMisc(GetClient);
    }

    /// <summary>
    /// Gets the connected client, throwing if not connected.
    /// This is passed as a delegate to sub-modules.
    /// </summary>
    private SdbMicroscopeClient GetClient()
    {
        EnsureConnected();
        return _client!;
    }

    // -------------------------------------------------------------------------
    // Connection Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Connects to the microscope via AutoScript API.
    /// Wrapped in Task.Run() because AutoScript Connect is synchronous.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _client = new SdbMicroscopeClient();
            _client.Connect(_host, _port);
        }, cancellationToken);
    }

    /// <summary>
    /// Disconnects from the microscope.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _client?.Disconnect();
            _client = null;
        }, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // ISemController Implementation - Delegation to Sub-Modules
    // -------------------------------------------------------------------------
    // All ISemController methods delegate to the appropriate sub-module.
    // This pattern keeps the main controller class thin and maintainable.
    // Each method is a one-liner that routes to the specialized handler.

    public Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
        => Misc.GetMicroscopeInfoAsync(cancellationToken);

    // Vacuum Operations
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

    // Beam Operations
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

    /// <summary>Acquires and saves an image (convenience method).</summary>
    public Task<string> AcquireAndSaveImageAsync(string? outputPath = null, CancellationToken cancellationToken = default)
        => Scanning.AcquireAndSaveImageAsync(outputPath, cancellationToken);

    public Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
        => Beam.GetEmissionCurrentAsync(cancellationToken);

    public Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
        => Beam.GetBlankerModeAsync(cancellationToken);

    public Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
        => Beam.SetBlankerModeAsync(mode, cancellationToken);

    // Stage Operations
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

    // Optics Operations
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

    // Scanning Operations
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

    // -------------------------------------------------------------------------
    // Helper Methods
    // -------------------------------------------------------------------------

    /// <summary>Throws if not connected to the microscope.</summary>
    private void EnsureConnected()
    {
        if (_client == null)
            throw new InvalidOperationException("Not connected to microscope. Call ConnectAsync first.");
    }

    // -------------------------------------------------------------------------
    // Resource Cleanup
    // -------------------------------------------------------------------------

    /// <summary>
    /// Disposes the controller, disconnecting from the microscope.
    /// </summary>
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
