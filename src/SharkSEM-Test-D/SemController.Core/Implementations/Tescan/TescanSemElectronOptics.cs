using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

public class TescanSemElectronOptics
{
    private readonly TescanSemController _controller;
    
    internal TescanSemElectronOptics(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetViewField", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            double viewFieldMm = TescanSemController.DecodeFloatInternal(response, ref offset);
            return viewFieldMm * 1000.0;
        }
        return double.NaN;
    }
    
    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        double viewFieldMm = viewFieldMicrons / 1000.0;
        byte[] body = TescanSemController.EncodeFloatInternal(viewFieldMm);
        await _controller.SendCommandNoResponseInternalAsync("SetViewField", body, cancellationToken);
    }
    
    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetWD", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeFloatInternal(workingDistanceMm);
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
        byte[] body = TescanSemController.EncodeIntInternal(0);
        await _controller.SendCommandWithWaitInternalAsync("AutoWD", body, TescanSemController.WaitFlagOpticsInternal | TescanSemController.WaitFlagAutoInternal, cancellationToken);
    }
    
    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetSpotSize", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task<double> GetBeamCurrentAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetBeamCurrent", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetBeamCurrentAsync(double beamCurrentPicoamps, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeFloatInternal(beamCurrentPicoamps);
        await _controller.SendCommandWithWaitInternalAsync("SetBeamCurrent", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    public async Task<string> EnumPCIndexesAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("EnumPCIndexes", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeStringInternal(response, ref offset);
        }
        return string.Empty;
    }
    
    public async Task<int> GetPCIndexAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetPCIndex", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return -1;
    }
    
    public async Task SetPCIndexAsync(int index, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(index);
        await _controller.SendCommandWithWaitInternalAsync("SetPCIndex", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    public async Task<double> GetAbsorbedCurrentAsync(CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(0);
        byte[] response = await _controller.SendCommandInternalAsync("GetIAbsorbed", body, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task<List<ScanningMode>> EnumScanningModesAsync(CancellationToken cancellationToken = default)
    {
        List<ScanningMode> modes = new List<ScanningMode>();
        byte[] response = await _controller.SendCommandInternalAsync("SMEnumModes", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            string modeMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            foreach (string line in modeMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].StartsWith("mode.") && parts[0].EndsWith(".name"))
                {
                    string indexStr = parts[0].Replace("mode.", "").Replace(".name", "");
                    if (int.TryParse(indexStr, out int index))
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
        byte[] response = await _controller.SendCommandInternalAsync("SMGetMode", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return -1;
    }
    
    public async Task SetScanningModeAsync(int modeIndex, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(modeIndex);
        await _controller.SendCommandWithWaitInternalAsync("SMSetMode", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    public async Task<(int result, double pivotPositionMm)> GetPivotPositionAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("SMGetPivotPos", null, cancellationToken);
        if (response.Length >= 8)
        {
            int result = TescanSemController.DecodeIntInternal(response, 0);
            int offset = 4;
            double pivotPos = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (result, pivotPos);
        }
        return (-1, double.NaN);
    }
}
