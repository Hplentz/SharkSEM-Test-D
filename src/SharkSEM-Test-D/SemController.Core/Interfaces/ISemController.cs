// =============================================================================
// ISemController.cs - Unified SEM Controller Interface
// =============================================================================
// This interface defines the vendor-agnostic API for controlling Scanning
// Electron Microscopes (SEMs). All vendor-specific implementations (TESCAN,
// Thermo Fisher, Mock) implement this interface, enabling application code
// to work with any supported SEM using identical method calls.
//
// Design Philosophy:
// - All operations are asynchronous (async/await) for responsive UI integration
// - CancellationToken support enables graceful operation cancellation
// - Implements IDisposable for proper resource cleanup on connection close
// - Methods are grouped by functional domain (vacuum, stage, beam, optics, etc.)
//
// Unit Conventions (standardized across all implementations):
// - Stage positions: millimeters (mm)
// - Working distance: millimeters (mm)  
// - View field: micrometers (µm)
// - High voltage: Volts (V)
// - Emission current: Amperes (A)
// - Tilt/Rotation angles: Degrees (°)
// - Pressure: Pascals (Pa)
//
// Usage:
//   ISemController sem = SemControllerFactory.Create(settings);
//   await sem.ConnectAsync();
//   var info = await sem.GetMicroscopeInfoAsync();
//   // ... perform operations ...
//   sem.Dispose();
// =============================================================================

using SemController.Core.Models;

namespace SemController.Core.Interfaces;

/// <summary>
/// Unified interface for controlling Scanning Electron Microscopes.
/// Provides vendor-agnostic access to all SEM subsystems including
/// vacuum, stage, beam, optics, and imaging.
/// </summary>
public interface ISemController : IDisposable
{
    // -------------------------------------------------------------------------
    // Connection Management
    // -------------------------------------------------------------------------
    
    /// <summary>Returns true if currently connected to the microscope.</summary>
    bool IsConnected { get; }
    
    /// <summary>Establishes connection to the microscope using configured settings.</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Gracefully disconnects from the microscope, releasing resources.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Microscope Information
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Retrieves microscope identification and version information.
    /// Useful for logging, diagnostics, and verifying connection.
    /// </summary>
    Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Vacuum System Control
    // -------------------------------------------------------------------------
    // The vacuum system must reach appropriate levels before beam operations.
    // Status checks should be performed before enabling the electron beam.
    
    /// <summary>Gets the current vacuum system status (pumping, vented, ready, etc.).</summary>
    Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Gets pressure reading from specified gauge in Pascals.</summary>
    Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default);
    
    /// <summary>Gets the current vacuum operating mode (High Vacuum, Low Vacuum, etc.).</summary>
    Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Initiates chamber pump-down sequence.</summary>
    Task PumpAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Initiates chamber venting to atmospheric pressure.</summary>
    Task VentAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Electron Beam Control
    // -------------------------------------------------------------------------
    // Beam operations require appropriate vacuum levels. The beam should be
    // turned off before venting or opening the chamber.
    
    /// <summary>Gets current electron beam state (on, off, standby).</summary>
    Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Enables the electron beam (requires vacuum ready).</summary>
    Task BeamOnAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enables beam and waits for it to stabilize.
    /// Returns true if beam is ready within timeout, false otherwise.
    /// </summary>
    Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default);
    
    /// <summary>Disables the electron beam.</summary>
    Task BeamOffAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // High Voltage and Emission
    // -------------------------------------------------------------------------
    
    /// <summary>Gets current accelerating voltage in Volts.</summary>
    Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets accelerating voltage in Volts.
    /// When waitForCompletion is true, blocks until voltage stabilizes.
    /// </summary>
    Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default);
    
    /// <summary>Gets current emission current in Amperes.</summary>
    Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Stage Control
    // -------------------------------------------------------------------------
    // Stage positions use millimeters for X, Y, Z and degrees for tilt/rotation.
    // Always check stage limits before commanding large movements.
    
    /// <summary>Gets current stage position (X, Y, Z in mm; tilt, rotation in degrees).</summary>
    Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves stage to absolute position.
    /// When waitForCompletion is true, blocks until movement completes.
    /// </summary>
    Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves stage by relative delta from current position.
    /// When waitForCompletion is true, blocks until movement completes.
    /// </summary>
    Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default);
    
    /// <summary>Returns true if stage is currently in motion.</summary>
    Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Immediately stops all stage motion.</summary>
    Task StopStageAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Gets stage travel limits for all axes.</summary>
    Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Initiates stage calibration/homing sequence.</summary>
    Task CalibrateStageAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Returns true if stage has been calibrated since power-on.</summary>
    Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Electron Optics
    // -------------------------------------------------------------------------
    
    /// <summary>Gets current view field (field of view) in micrometers.</summary>
    Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Sets view field in micrometers (controls magnification).</summary>
    Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default);
    
    /// <summary>Gets current working distance in millimeters.</summary>
    Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Sets working distance in millimeters.</summary>
    Task SetWorkingDistanceAsync(double workingDistance, CancellationToken cancellationToken = default);
    
    /// <summary>Gets current focus value (vendor-specific units).</summary>
    Task<double> GetFocusAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Sets focus value (vendor-specific units).</summary>
    Task SetFocusAsync(double focus, CancellationToken cancellationToken = default);
    
    /// <summary>Initiates automatic focus procedure.</summary>
    Task AutoFocusAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Scanning Control
    // -------------------------------------------------------------------------
    
    /// <summary>Gets current scan speed setting (higher = faster but noisier).</summary>
    Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Sets scan speed (range is vendor-specific).</summary>
    Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default);
    
    /// <summary>Gets current beam blanker mode.</summary>
    Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Sets beam blanker mode (auto, manual, external).</summary>
    Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Image Acquisition
    // -------------------------------------------------------------------------
    // Image acquisition requires beam on and appropriate scan settings.
    // Multiple detectors can be acquired simultaneously depending on hardware.
    
    /// <summary>
    /// Acquires images from one or more detectors according to settings.
    /// Returns array of SemImage objects containing pixel data and metadata.
    /// </summary>
    Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Acquires single image from specified detector channel.
    /// Simplified interface for quick single-detector acquisition.
    /// </summary>
    Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default);
    
    /// <summary>Stops any active scanning/acquisition operation.</summary>
    Task StopScanAsync(CancellationToken cancellationToken = default);
    
    // -------------------------------------------------------------------------
    // Additional Optics
    // -------------------------------------------------------------------------
    
    /// <summary>Gets current spot size (probe diameter, vendor-specific units).</summary>
    Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default);
}
