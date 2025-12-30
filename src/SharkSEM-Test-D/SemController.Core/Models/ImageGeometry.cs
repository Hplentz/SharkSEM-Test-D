// =============================================================================
// ImageGeometry.cs - Image Geometry/Resolution Preset
// =============================================================================
// Represents a predefined image resolution setting available on the microscope.
// Examples: 256x192, 512x384, 1024x768, 2048x1536, etc.
// Used primarily with TESCAN microscopes.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Represents an image geometry (resolution) preset.
/// Immutable record type for simple value semantics.
/// </summary>
/// <param name="Index">Geometry index used in commands.</param>
/// <param name="Name">Resolution description (e.g., "1024x768").</param>
public record ImageGeometry(int Index, string Name);
