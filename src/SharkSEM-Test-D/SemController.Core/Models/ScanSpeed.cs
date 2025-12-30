// =============================================================================
// ScanSpeed.cs - Scan Speed Definition
// =============================================================================
// Represents a predefined scan speed setting with its index and dwell time.
// Used for UI display and scan speed selection.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Represents a scan speed setting with its dwell time.
/// Higher index typically means faster scanning but noisier images.
/// </summary>
public class ScanSpeed
{
    /// <summary>Speed setting index (vendor-specific range).</summary>
    public int Index { get; set; }
    
    /// <summary>Pixel dwell time in microseconds for this speed setting.</summary>
    public double DwellTimeMicroseconds { get; set; }

    /// <summary>Creates a scan speed definition.</summary>
    /// <param name="index">Speed setting index.</param>
    /// <param name="dwellTimeMicroseconds">Dwell time in microseconds.</param>
    public ScanSpeed(int index, double dwellTimeMicroseconds)
    {
        Index = index;
        DwellTimeMicroseconds = dwellTimeMicroseconds;
    }

    /// <summary>Returns formatted string for display.</summary>
    public override string ToString() => $"Speed {Index}: {DwellTimeMicroseconds:F1} µs/pixel";
}
