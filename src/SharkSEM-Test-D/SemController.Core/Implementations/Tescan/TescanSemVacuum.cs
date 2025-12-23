using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

public class TescanSemVacuum
{
    private readonly TescanSemController _controller;
    
    internal TescanSemVacuum(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<VacuumStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("VacGetStatus", null, cancellationToken);
        if (response.Length >= 4)
        {
            var status = TescanSemController.DecodeIntInternal(response, 0);
            return (VacuumStatus)status;
        }
        return VacuumStatus.Error;
    }
    
    public async Task<double> GetPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal((int)gauge);
        var response = await _controller.SendCommandInternalAsync("VacGetPressure", body, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task<VacuumMode> GetModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("VacGetVPMode", null, cancellationToken);
        if (response.Length >= 4)
        {
            var mode = TescanSemController.DecodeIntInternal(response, 0);
            return (VacuumMode)mode;
        }
        return VacuumMode.Unknown;
    }
    
    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("VacPump", null, cancellationToken);
    }
    
    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("VacVent", null, cancellationToken);
    }
}
