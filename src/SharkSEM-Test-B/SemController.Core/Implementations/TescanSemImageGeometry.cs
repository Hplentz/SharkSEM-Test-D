using SemController.Core.Models;

namespace SemController.Core.Implementations;

public class TescanSemImageGeometry
{
    private readonly TescanSemController _controller;
    
    internal TescanSemImageGeometry(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<List<ImageGeometry>> EnumGeometriesAsync(CancellationToken cancellationToken = default)
    {
        var geometries = new List<ImageGeometry>();
        var response = await _controller.SendCommandInternalAsync("EnumGeometries", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            var geoMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            foreach (var line in geoMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].StartsWith("geom.") && parts[0].EndsWith(".name"))
                {
                    var indexStr = parts[0].Replace("geom.", "").Replace(".name", "");
                    if (int.TryParse(indexStr, out var index))
                    {
                        geometries.Add(new ImageGeometry(index, parts[1].Trim()));
                    }
                }
            }
        }
        return geometries;
    }
    
    public async Task<(double x, double y)> GetGeometryAsync(int index, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(index);
        var response = await _controller.SendCommandInternalAsync("GetGeometry", body, cancellationToken);
        if (response.Length >= 8)
        {
            int offset = 0;
            var x = TescanSemController.DecodeFloatInternal(response, ref offset);
            var y = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (x, y);
        }
        return (double.NaN, double.NaN);
    }
    
    public async Task SetGeometryAsync(int index, double x, double y, CancellationToken cancellationToken = default)
    {
        var body = new byte[TescanSemController.EncodeIntInternal(index).Length + 
                           TescanSemController.EncodeFloatInternal(x).Length + 
                           TescanSemController.EncodeFloatInternal(y).Length];
        int offset = 0;
        Buffer.BlockCopy(TescanSemController.EncodeIntInternal(index), 0, body, offset, 4);
        offset += 4;
        var xBytes = TescanSemController.EncodeFloatInternal(x);
        Buffer.BlockCopy(xBytes, 0, body, offset, xBytes.Length);
        offset += xBytes.Length;
        var yBytes = TescanSemController.EncodeFloatInternal(y);
        Buffer.BlockCopy(yBytes, 0, body, offset, yBytes.Length);
        await _controller.SendCommandNoResponseInternalAsync("SetGeometry", body, cancellationToken);
    }
    
    public async Task<(double x, double y)> GetImageShiftAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("GetImageShift", null, cancellationToken);
        if (response.Length >= 8)
        {
            int offset = 0;
            var x = TescanSemController.DecodeFloatInternal(response, ref offset);
            var y = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (x, y);
        }
        return (double.NaN, double.NaN);
    }
    
    public async Task SetImageShiftAsync(double x, double y, CancellationToken cancellationToken = default)
    {
        var xBytes = TescanSemController.EncodeFloatInternal(x);
        var yBytes = TescanSemController.EncodeFloatInternal(y);
        var body = new byte[xBytes.Length + yBytes.Length];
        Buffer.BlockCopy(xBytes, 0, body, 0, xBytes.Length);
        Buffer.BlockCopy(yBytes, 0, body, xBytes.Length, yBytes.Length);
        await _controller.SendCommandNoResponseInternalAsync("SetImageShift", body, cancellationToken);
    }
    
    public async Task<List<Centering>> EnumCenteringsAsync(CancellationToken cancellationToken = default)
    {
        var centerings = new List<Centering>();
        var response = await _controller.SendCommandInternalAsync("EnumCenterings", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            var centeringMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            foreach (var line in centeringMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].StartsWith("cen.") && parts[0].EndsWith(".name"))
                {
                    var indexStr = parts[0].Replace("cen.", "").Replace(".name", "");
                    if (int.TryParse(indexStr, out var index))
                    {
                        centerings.Add(new Centering(index, parts[1].Trim()));
                    }
                }
            }
        }
        return centerings;
    }
    
    public async Task<(double x, double y)> GetCenteringAsync(int index, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(index);
        var response = await _controller.SendCommandInternalAsync("GetCentering", body, cancellationToken);
        if (response.Length >= 8)
        {
            int offset = 0;
            var x = TescanSemController.DecodeFloatInternal(response, ref offset);
            var y = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (x, y);
        }
        return (double.NaN, double.NaN);
    }
    
    public async Task SetCenteringAsync(int index, double x, double y, CancellationToken cancellationToken = default)
    {
        var body = new byte[TescanSemController.EncodeIntInternal(index).Length + 
                           TescanSemController.EncodeFloatInternal(x).Length + 
                           TescanSemController.EncodeFloatInternal(y).Length];
        int offset = 0;
        Buffer.BlockCopy(TescanSemController.EncodeIntInternal(index), 0, body, offset, 4);
        offset += 4;
        var xBytes = TescanSemController.EncodeFloatInternal(x);
        Buffer.BlockCopy(xBytes, 0, body, offset, xBytes.Length);
        offset += xBytes.Length;
        var yBytes = TescanSemController.EncodeFloatInternal(y);
        Buffer.BlockCopy(yBytes, 0, body, offset, yBytes.Length);
        await _controller.SendCommandNoResponseInternalAsync("SetCentering", body, cancellationToken);
    }
}
