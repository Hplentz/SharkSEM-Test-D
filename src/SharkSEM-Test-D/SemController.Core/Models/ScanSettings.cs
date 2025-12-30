// =============================================================================
// ScanSettings.cs - Image Acquisition Configuration
// =============================================================================
// Configures parameters for image acquisition including resolution,
// dwell time, frame averaging, and region of interest.
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Configuration for image acquisition operations.
/// </summary>
public class ScanSettings
{
    /// <summary>Image width in pixels.</summary>
    public int Width { get; set; } = 1024;
    
    /// <summary>Image height in pixels.</summary>
    public int Height { get; set; } = 768;
    
    /// <summary>Dwell time per pixel in microseconds (affects signal-to-noise).</summary>
    public double DwellTimeUs { get; set; } = 1.0;
    
    /// <summary>Number of frames to average (higher = less noise but slower).</summary>
    public int FrameCount { get; set; } = 1;
    
    /// <summary>Array of detector channel indices to acquire from.</summary>
    public int[] Channels { get; set; } = new[] { 0 };
    
    // Region of Interest (ROI) - for sub-area scanning
    // Values of 0 indicate full-frame acquisition
    
    /// <summary>Left edge of ROI in pixels (0 = use full width).</summary>
    public int Left { get; set; } = 0;
    
    /// <summary>Top edge of ROI in pixels (0 = use full height).</summary>
    public int Top { get; set; } = 0;
    
    /// <summary>Right edge of ROI in pixels (0 = use full width).</summary>
    public int Right { get; set; } = 0;
    
    /// <summary>Bottom edge of ROI in pixels (0 = use full height).</summary>
    public int Bottom { get; set; } = 0;
}
