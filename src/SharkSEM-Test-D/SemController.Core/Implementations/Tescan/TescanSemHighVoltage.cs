using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

public class TescanSemHighVoltage
{
    private readonly TescanSemController _controller;
    
    internal TescanSemHighVoltage(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("HVGetBeam", null, cancellationToken);
        if (response.Length >= 4)
        {
            var state = TescanSemController.DecodeIntInternal(response, 0);
            return (BeamState)state;
        }
        return BeamState.Unknown;
    }
    
    public async Task BeamOnAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandWithWaitInternalAsync("HVBeamOn", null, TescanSemController.WaitFlagOpticsInternal | TescanSemController.WaitFlagAutoInternal, cancellationToken);
    }
    
    public async Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var state = await GetBeamStateAsync(cancellationToken);
            if (state == BeamState.On)
                return true;
            if (state == BeamState.Off || state == BeamState.Unknown)
                return false;
            await Task.Delay(200, cancellationToken);
        }
        return false;
    }
    
    public async Task BeamOffAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("HVBeamOff", null, cancellationToken);
    }
    
    public async Task<double> GetVoltageAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("HVGetVoltage", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var asyncFlag = waitForCompletion ? 0 : 1;
        var body = new List<byte>();
        body.AddRange(TescanSemController.EncodeFloatInternal(voltage));
        body.AddRange(TescanSemController.EncodeIntInternal(asyncFlag));
        await _controller.SendCommandNoResponseInternalAsync("HVSetVoltage", body.ToArray(), cancellationToken);
    }
    
    public async Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("HVGetEmission", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            var emissionMicroAmps = TescanSemController.DecodeFloatInternal(response, ref offset);
            return emissionMicroAmps * 1e-6;
        }
        return double.NaN;
    }
}
