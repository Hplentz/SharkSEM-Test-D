// =============================================================================
// Enums.cs - Core Enumeration Types for SEM Control
// =============================================================================
// Defines standardized enumeration values used throughout the library.
// These enums provide vendor-agnostic status codes and mode settings
// that implementations must map to/from their native values.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Vacuum system status values.
/// Implementations map vendor-specific states to these standardized values.
/// </summary>
public enum VacuumStatus
{
    Error = -1,        // Vacuum system fault or error condition
    Ready = 0,         // Vacuum at operational levels, ready for beam
    Pumping = 1,       // Pump-down in progress
    Venting = 2,       // Venting to atmosphere in progress
    VacuumOff = 3,     // Vacuum system powered off
    ChamberOpen = 4    // Chamber door/lid is open
}

/// <summary>
/// Electron beam state values.
/// </summary>
public enum BeamState
{
    Unknown = -1,       // State cannot be determined
    Off = 0,            // Beam disabled
    On = 1,             // Beam enabled and stable
    Transitioning = 1000 // Beam turning on/off, not yet stable
}

/// <summary>
/// Beam blanker operating modes.
/// The blanker deflects the beam to prevent sample exposure during moves.
/// </summary>
public enum BlankerMode
{
    Off = 0,   // Blanker disabled (beam always on target)
    On = 1,    // Blanker enabled (beam deflected away)
    Auto = 2   // Automatic blanking during stage moves, scan retrace, etc.
}

/// <summary>
/// Vacuum gauge identifiers for pressure readings.
/// Not all gauges exist on all microscope configurations.
/// </summary>
public enum VacuumGauge
{
    Chamber = 0,    // Main specimen chamber gauge
    TmpGauge = 1,   // Turbo molecular pump backing gauge
    SemGun = 2,     // SEM gun column gauge
    FibColumn = 3,  // FIB column gauge (dual-beam only)
    FibGun = 4,     // FIB gun gauge (dual-beam only)
    SemColumn = 5,  // SEM column gauge
    XeValve = 6     // Xenon valve area gauge (plasma FIB)
}

/// <summary>
/// Vacuum operating mode (affects chamber pressure level).
/// </summary>
public enum VacuumMode
{
    Unknown = -1,        // Mode cannot be determined
    HighVacuum = 0,      // Standard high vacuum operation (~10^-3 Pa)
    VariablePressure = 1 // Variable pressure/low vacuum mode for charging samples
}

/// <summary>
/// Supported SEM vendor types for factory instantiation.
/// </summary>
public enum SemType
{
    Tescan,  // TESCAN microscopes using SharkSEM protocol
    Thermo,  // Thermo Fisher Scientific using AutoScript API
    Mock,    // Mock implementation for testing without hardware
    Custom   // User-defined custom implementation
}
