// =============================================================================
// StagePosition.cs - Stage Position Data Structure
// =============================================================================
// Represents the position of the microscope specimen stage in 5 degrees
// of freedom. All linear dimensions are in millimeters, angles in degrees.
//
// Nullable properties allow partial position specification for relative
// movements or when certain axes are not available.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Represents specimen stage position with up to 5 degrees of freedom.
/// Units: X, Y, Z in millimeters; Rotation, TiltX, TiltY in degrees.
/// </summary>
public class StagePosition
{
    /// <summary>X-axis position in millimeters (horizontal, left-right).</summary>
    public double X { get; set; }
    
    /// <summary>Y-axis position in millimeters (horizontal, front-back).</summary>
    public double Y { get; set; }
    
    /// <summary>Z-axis position in millimeters (vertical, height). Null if unavailable.</summary>
    public double? Z { get; set; }
    
    /// <summary>Rotation angle in degrees. Null if unavailable.</summary>
    public double? Rotation { get; set; }
    
    /// <summary>Primary tilt angle in degrees. Null if unavailable.</summary>
    public double? TiltX { get; set; }
    
    /// <summary>Secondary tilt angle in degrees (compustage only). Null if unavailable.</summary>
    public double? TiltY { get; set; }

    /// <summary>Creates a new stage position with all axes at zero.</summary>
    public StagePosition() { }

    /// <summary>
    /// Creates a new stage position with specified coordinates.
    /// Null values indicate the axis should not be moved (for relative moves)
    /// or is not available (when reading position).
    /// </summary>
    public StagePosition(double x, double y, double? z = null, double? rotation = null, double? tiltX = null, double? tiltY = null)
    {
        X = x;
        Y = y;
        Z = z;
        Rotation = rotation;
        TiltX = tiltX;
        TiltY = tiltY;
    }

    /// <summary>Returns formatted string representation for logging/display.</summary>
    public override string ToString() =>
        $"X={X:F6}, Y={Y:F6}, Z={Z?.ToString("F6") ?? "N/A"}, R={Rotation?.ToString("F2") ?? "N/A"}, Tx={TiltX?.ToString("F2") ?? "N/A"}, Ty={TiltY?.ToString("F2") ?? "N/A"}";
}
