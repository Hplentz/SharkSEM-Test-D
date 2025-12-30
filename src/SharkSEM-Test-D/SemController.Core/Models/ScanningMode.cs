// =============================================================================
// ScanningMode.cs - Scanning Mode Definition
// =============================================================================
// Represents a scanning mode available on the microscope.
// Examples: Resolution, Depth, Field, etc.
// Used primarily with TESCAN microscopes.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Represents a scanning mode (e.g., Resolution, Depth, Field).
/// Immutable record type for simple value semantics.
/// </summary>
/// <param name="Index">Mode index used in commands.</param>
/// <param name="Name">Human-readable mode name.</param>
public record ScanningMode(int Index, string Name)
{
    /// <summary>Returns formatted string for display.</summary>
    public override string ToString() => $"[{Index}] {Name}";
}
