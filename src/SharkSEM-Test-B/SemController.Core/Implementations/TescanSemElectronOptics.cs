using SemController.Core.Models;

namespace SemController.Core.Implementations;

public class TescanSemElectronOptics
{
    private readonly TescanSemController _controller;
    
    internal TescanSemElectronOptics(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("GetViewField", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            var viewFieldMm = TescanSemController.DecodeFloatInternal(response, ref offset);
            return viewFieldMm * 1000.0;
        }
        return double.NaN;
    }
    
    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        var viewFieldMm = viewFieldMicrons / 1000.0;
        var body = TescanSemController.EncodeFloatInternal(viewFieldMm);
        await _controller.SendCommandNoResponseInternalAsync("SetViewField", body, cancellationToken);
    }
    
    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("GetWD", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeFloatInternal(workingDistanceMm);
        await _controller.SendCommandNoResponseInternalAsync("SetWD", body, cancellationToken);
    }
    
    public async Task<double> GetFocusAsync(CancellationToken cancellationToken = default)
    {
        return await GetWorkingDistanceAsync(cancellationToken);
    }
    
    public async Task SetFocusAsync(double focus, CancellationToken cancellationToken = default)
    {
        await SetWorkingDistanceAsync(focus, cancellationToken);
    }
    
    public async Task AutoFocusAsync(CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(0);
        await _controller.SendCommandWithWaitInternalAsync("AutoWD", body, TescanSemController.WaitFlagOpticsInternal | TescanSemController.WaitFlagAutoInternal, cancellationToken);
    }
    
    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("GetSpotSize", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task<double> GetBeamCurrentAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("GetBeamCurrent", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetBeamCurrentAsync(double beamCurrentPicoamps, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeFloatInternal(beamCurrentPicoamps);
        await _controller.SendCommandWithWaitInternalAsync("SetBeamCurrent", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    public async Task<string> EnumPCIndexesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("EnumPCIndexes", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeStringInternal(response, ref offset);
        }
        return string.Empty;
    }
    
    public async Task<double> GetAbsorbedCurrentAsync(CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(0);
        var response = await _controller.SendCommandInternalAsync("GetIAbsorbed", body, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task<List<ScanningMode>> EnumScanningModesAsync(CancellationToken cancellationToken = default)
    {
        var modes = new List<ScanningMode>();
        var response = await _controller.SendCommandInternalAsync("SMEnumModes", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            var modeMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            foreach (var line in modeMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].StartsWith("mode.") && parts[0].EndsWith(".name"))
                {
                    var indexStr = parts[0].Replace("mode.", "").Replace(".name", "");
                    if (int.TryParse(indexStr, out var index))
                    {
                        modes.Add(new ScanningMode(index, parts[1].Trim()));
                    }
                }
            }
        }
        return modes;
    }
    
    public async Task<int> GetScanningModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("SMGetMode", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return -1;
    }
    
    public async Task SetScanningModeAsync(int modeIndex, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(modeIndex);
        await _controller.SendCommandWithWaitInternalAsync("SMSetMode", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    public async Task<(int result, double pivotPositionMm)> GetPivotPositionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("SMGetPivotPos", null, cancellationToken);
        if (response.Length >= 8)
        {
            var result = TescanSemController.DecodeIntInternal(response, 0);
            int offset = 4;
            var pivotPos = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (result, pivotPos);
        }
        return (-1, double.NaN);
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
                if (parts.Length == 2 && parts[0].StartsWith("geometry.") && parts[0].EndsWith(".name"))
                {
                    var indexStr = parts[0].Replace("geometry.", "").Replace(".name", "");
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
                if (parts.Length == 2 && parts[0].StartsWith("centering.") && parts[0].EndsWith(".name"))
                {
                    var indexStr = parts[0].Replace("centering.", "").Replace(".name", "");
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
