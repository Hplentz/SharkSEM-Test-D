// =============================================================================
// TescanSemImageGeometry.cs - TESCAN Image Geometry and Centering
// =============================================================================
// Manages image geometry (resolution presets), image shift, and optical
// centering for TESCAN microscopes.
//
// Concepts:
// - Geometry: Predefined image resolution settings (e.g., 1024x768, 2048x1536)
// - Image Shift: Electrical beam deflection to move image without stage
// - Centering: Optical alignment adjustments (crossover, wobble, rotation, etc.)
//
// SharkSEM uses shortened prefixes in property maps:
// - geom.N.name for geometries
// - cen.N.name for centerings
//
// SharkSEM Commands Used:
// - EnumGeometries: Lists available geometry presets
// - GetGeometry/SetGeometry: Current geometry values
// - GetGeomLimits: Min/max for geometry settings
// - GetImageShift/SetImageShift: Beam deflection offset
// - EnumCenterings: Lists available centering modes
// - GetCentering/SetCentering: Centering adjustment values
// =============================================================================

using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Image geometry and optical centering sub-module for TESCAN SEMs.
/// Handles resolution presets, image shift, and alignment adjustments.
/// </summary>
public class TescanSemImageGeometry
{
    private readonly TescanSemController _controller;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemImageGeometry(TescanSemController controller)
    {
        _controller = controller;
    }
    
    // -------------------------------------------------------------------------
    // Image Geometry (Resolution Presets)
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Enumerates available image geometry presets.
    /// Parses property-map response with geom.N.name format.
    /// </summary>
    public async Task<List<ImageGeometry>> EnumGeometriesAsync(CancellationToken cancellationToken = default)
    {
        List<ImageGeometry> geometries = new List<ImageGeometry>();
        byte[] response = await _controller.SendCommandInternalAsync("EnumGeometries", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            string geoMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            // Parse property-map format: geom.N.name=Description
            foreach (string line in geoMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].StartsWith("geom.") && parts[0].EndsWith(".name"))
                {
                    string indexStr = parts[0].Replace("geom.", "").Replace(".name", "");
                    if (int.TryParse(indexStr, out int index))
                    {
                        geometries.Add(new ImageGeometry(index, parts[1].Trim()));
                    }
                }
            }
        }
        return geometries;
    }
    
    /// <summary>
    /// Gets geometry values (width, height) for specified preset index.
    /// </summary>
    public async Task<(double x, double y)> GetGeometryAsync(int index, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(index);
        byte[] response = await _controller.SendCommandInternalAsync("GetGeometry", body, cancellationToken);
        if (response.Length >= 8)
        {
            int offset = 0;
            double x = TescanSemController.DecodeFloatInternal(response, ref offset);
            double y = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (x, y);
        }
        return (double.NaN, double.NaN);
    }
    
    /// <summary>
    /// Gets geometry limits (min/max width and height) for specified preset.
    /// </summary>
    public async Task<(double minX, double maxX, double minY, double maxY)> GetGeomLimitsAsync(int index, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(index);
        byte[] response = await _controller.SendCommandInternalAsync("GetGeomLimits", body, cancellationToken);
        if (response.Length >= 20)
        {
            int offset = 0;
            int result = TescanSemController.DecodeIntInternal(response, offset);
            offset += 4;
            double minX = TescanSemController.DecodeFloatInternal(response, ref offset);
            double maxX = TescanSemController.DecodeFloatInternal(response, ref offset);
            double minY = TescanSemController.DecodeFloatInternal(response, ref offset);
            double maxY = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (minX, maxX, minY, maxY);
        }
        return (double.NaN, double.NaN, double.NaN, double.NaN);
    }
    
    /// <summary>
    /// Sets geometry values for specified preset index.
    /// </summary>
    public async Task SetGeometryAsync(int index, double x, double y, CancellationToken cancellationToken = default)
    {
        byte[] body = new byte[TescanSemController.EncodeIntInternal(index).Length + 
                           TescanSemController.EncodeFloatInternal(x).Length + 
                           TescanSemController.EncodeFloatInternal(y).Length];
        int offset = 0;
        Buffer.BlockCopy(TescanSemController.EncodeIntInternal(index), 0, body, offset, 4);
        offset += 4;
        byte[] xBytes = TescanSemController.EncodeFloatInternal(x);
        Buffer.BlockCopy(xBytes, 0, body, offset, xBytes.Length);
        offset += xBytes.Length;
        byte[] yBytes = TescanSemController.EncodeFloatInternal(y);
        Buffer.BlockCopy(yBytes, 0, body, offset, yBytes.Length);
        await _controller.SendCommandNoResponseInternalAsync("SetGeometry", body, cancellationToken);
    }
    
    // -------------------------------------------------------------------------
    // Image Shift (Beam Deflection)
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Gets current image shift (beam deflection offset).
    /// Values in relative units (typically -1 to +1 range).
    /// </summary>
    public async Task<(double x, double y)> GetImageShiftAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetImageShift", null, cancellationToken);
        if (response.Length >= 8)
        {
            int offset = 0;
            double x = TescanSemController.DecodeFloatInternal(response, ref offset);
            double y = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (x, y);
        }
        return (double.NaN, double.NaN);
    }
    
    /// <summary>
    /// Sets image shift (beam deflection offset).
    /// </summary>
    public async Task SetImageShiftAsync(double x, double y, CancellationToken cancellationToken = default)
    {
        byte[] xBytes = TescanSemController.EncodeFloatInternal(x);
        byte[] yBytes = TescanSemController.EncodeFloatInternal(y);
        byte[] body = new byte[xBytes.Length + yBytes.Length];
        Buffer.BlockCopy(xBytes, 0, body, 0, xBytes.Length);
        Buffer.BlockCopy(yBytes, 0, body, xBytes.Length, yBytes.Length);
        await _controller.SendCommandNoResponseInternalAsync("SetImageShift", body, cancellationToken);
    }
    
    // -------------------------------------------------------------------------
    // Optical Centering
    // -------------------------------------------------------------------------
    // TESCAN microscopes have multiple centering modes for optical alignment.
    // Property maps use shortened prefix: cen.N.name
    
    /// <summary>
    /// Enumerates available optical centering modes.
    /// Parses property-map with cen.N.name format.
    /// </summary>
    public async Task<List<Centering>> EnumCenteringsAsync(CancellationToken cancellationToken = default)
    {
        List<Centering> centerings = new List<Centering>();
        byte[] response = await _controller.SendCommandInternalAsync("EnumCenterings", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            string centeringMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            // Parse property-map format: cen.N.name=CenteringName
            foreach (string line in centeringMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].StartsWith("cen.") && parts[0].EndsWith(".name"))
                {
                    string indexStr = parts[0].Replace("cen.", "").Replace(".name", "");
                    if (int.TryParse(indexStr, out int index))
                    {
                        centerings.Add(new Centering(index, parts[1].Trim()));
                    }
                }
            }
        }
        return centerings;
    }
    
    /// <summary>
    /// Gets centering adjustment values for specified mode.
    /// </summary>
    public async Task<(double x, double y)> GetCenteringAsync(int index, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(index);
        byte[] response = await _controller.SendCommandInternalAsync("GetCentering", body, cancellationToken);
        if (response.Length >= 8)
        {
            int offset = 0;
            double x = TescanSemController.DecodeFloatInternal(response, ref offset);
            double y = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (x, y);
        }
        return (double.NaN, double.NaN);
    }
    
    /// <summary>
    /// Sets centering adjustment values for specified mode.
    /// </summary>
    public async Task SetCenteringAsync(int index, double x, double y, CancellationToken cancellationToken = default)
    {
        byte[] body = new byte[TescanSemController.EncodeIntInternal(index).Length + 
                           TescanSemController.EncodeFloatInternal(x).Length + 
                           TescanSemController.EncodeFloatInternal(y).Length];
        int offset = 0;
        Buffer.BlockCopy(TescanSemController.EncodeIntInternal(index), 0, body, offset, 4);
        offset += 4;
        byte[] xBytes = TescanSemController.EncodeFloatInternal(x);
        Buffer.BlockCopy(xBytes, 0, body, offset, xBytes.Length);
        offset += xBytes.Length;
        byte[] yBytes = TescanSemController.EncodeFloatInternal(y);
        Buffer.BlockCopy(yBytes, 0, body, offset, yBytes.Length);
        await _controller.SendCommandNoResponseInternalAsync("SetCentering", body, cancellationToken);
    }
}
