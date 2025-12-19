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
}
