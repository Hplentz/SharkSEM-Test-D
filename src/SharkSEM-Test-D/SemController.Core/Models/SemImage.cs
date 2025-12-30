// =============================================================================
// SemImage.cs - Acquired Image Data Container
// =============================================================================
// Contains image data acquired from the microscope along with metadata.
// The Data array contains raw pixel values (grayscale), with BitsPerPixel
// indicating the bit depth (typically 8 or 16 bits).
// =============================================================================

namespace SemController.Core.Models;

/// <summary>
/// Container for acquired SEM image data and metadata.
/// </summary>
public class SemImage
{
    /// <summary>Image width in pixels.</summary>
    public int Width { get; set; }
    
    /// <summary>Image height in pixels.</summary>
    public int Height { get; set; }
    
    /// <summary>Detector channel number that produced this image.</summary>
    public int Channel { get; set; }
    
    /// <summary>
    /// Raw pixel data as byte array.
    /// For 8-bit: one byte per pixel.
    /// For 16-bit: two bytes per pixel (little-endian).
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    /// <summary>Bits per pixel (8 or 16).</summary>
    public int BitsPerPixel { get; set; } = 8;
    
    /// <summary>UTC timestamp when image was captured.</summary>
    public DateTime CaptureTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>Optional vendor-specific header data for debugging.</summary>
    public string? Header { get; set; }

    /// <summary>Creates an empty image container.</summary>
    public SemImage() { }

    /// <summary>
    /// Creates an image with specified dimensions and data.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="data">Raw pixel data.</param>
    /// <param name="channel">Detector channel number.</param>
    /// <param name="bitsPerPixel">Bit depth (8 or 16).</param>
    public SemImage(int width, int height, byte[] data, int channel = 0, int bitsPerPixel = 8)
    {
        Width = width;
        Height = height;
        Data = data;
        Channel = channel;
        BitsPerPixel = bitsPerPixel;
    }
}
