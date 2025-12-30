// =============================================================================
// StageLimits.cs - Stage Travel Limits
// =============================================================================
// Defines minimum and maximum travel limits for each stage axis.
// Used to validate commanded positions before moving.
// Linear units in millimeters, angular units in degrees.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Stage travel limits for all axes.
/// Applications should check positions against these limits before commanding moves.
/// </summary>
public class StageLimits
{
    /// <summary>Minimum X position in millimeters.</summary>
    public double MinX { get; set; }
    
    /// <summary>Maximum X position in millimeters.</summary>
    public double MaxX { get; set; }
    
    /// <summary>Minimum Y position in millimeters.</summary>
    public double MinY { get; set; }
    
    /// <summary>Maximum Y position in millimeters.</summary>
    public double MaxY { get; set; }
    
    /// <summary>Minimum Z position in millimeters.</summary>
    public double MinZ { get; set; }
    
    /// <summary>Maximum Z position in millimeters.</summary>
    public double MaxZ { get; set; }
    
    /// <summary>Minimum rotation angle in degrees.</summary>
    public double MinRotation { get; set; }
    
    /// <summary>Maximum rotation angle in degrees.</summary>
    public double MaxRotation { get; set; }
    
    /// <summary>Minimum primary tilt angle in degrees.</summary>
    public double MinTiltX { get; set; }
    
    /// <summary>Maximum primary tilt angle in degrees.</summary>
    public double MaxTiltX { get; set; }
    
    /// <summary>Minimum secondary tilt angle in degrees (null if not available).</summary>
    public double? MinTiltY { get; set; }
    
    /// <summary>Maximum secondary tilt angle in degrees (null if not available).</summary>
    public double? MaxTiltY { get; set; }
}
