using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

public class TescanSemImageGeometry
{
    private readonly TescanSemController _controller;
    
    internal TescanSemImageGeometry(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<List<ImageGeometry>> EnumGeometriesAsync(CancellationToken cancellationToken = default)
    {
        List<ImageGeometry> geometries = new List<ImageGeometry>();
        byte[] response = await _controller.SendCommandInternalAsync("EnumGeometries", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            string geoMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
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
    
    public async Task SetImageShiftAsync(double x, double y, CancellationToken cancellationToken = default)
    {
        byte[] xBytes = TescanSemController.EncodeFloatInternal(x);
        byte[] yBytes = TescanSemController.EncodeFloatInternal(y);
        byte[] body = new byte[xBytes.Length + yBytes.Length];
        Buffer.BlockCopy(xBytes, 0, body, 0, xBytes.Length);
        Buffer.BlockCopy(yBytes, 0, body, xBytes.Length, yBytes.Length);
        await _controller.SendCommandNoResponseInternalAsync("SetImageShift", body, cancellationToken);
    }
    
    public async Task<List<Centering>> EnumCenteringsAsync(CancellationToken cancellationToken = default)
    {
        List<Centering> centerings = new List<Centering>();
        byte[] response = await _controller.SendCommandInternalAsync("EnumCenterings", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            string centeringMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
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
