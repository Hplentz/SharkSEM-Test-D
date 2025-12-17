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
    
    public async Task SetSpotSizeAsync(double spotSize, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeFloatInternal(spotSize);
        await _controller.SendCommandNoResponseInternalAsync("SetSpotSize", body, cancellationToken);
    }
}
