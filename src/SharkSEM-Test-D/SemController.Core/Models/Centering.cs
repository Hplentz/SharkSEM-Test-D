// =============================================================================
// Centering.cs - Centering Mode Definition
// =============================================================================
// Represents a centering mode for optical alignment operations.
// Examples: Crossover, Wobble, Rotation, etc.
// Used primarily with TESCAN microscopes.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Represents an optical centering mode for alignment procedures.
/// Immutable record type for simple value semantics.
/// </summary>
/// <param name="Index">Centering mode index used in commands.</param>
/// <param name="Name">Human-readable centering mode name.</param>
public record Centering(int Index, string Name);
