using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemVacuum
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    internal ThermoSemVacuum(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    public async Task<VacuumStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            string? state = client.Vacuum.ChamberState;
            
            return state?.ToLower() switch
            {
                "pumped" => VacuumStatus.Ready,
                "vacuum" => VacuumStatus.Ready,
                "ready" => VacuumStatus.Ready,
                "vented" => VacuumStatus.VacuumOff,
                "air" => VacuumStatus.VacuumOff,
                "pumping" => VacuumStatus.Pumping,
                "venting" => VacuumStatus.Venting,
                "prevacuum" => VacuumStatus.Pumping,
                "transition" => VacuumStatus.Pumping,
                _ => VacuumStatus.Error
            };
        }, cancellationToken);
    }

    public async Task<double> GetPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            return client.Vacuum.ChamberPressure.Value;
        }, cancellationToken);
    }

    public async Task<VacuumMode> GetModeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return VacuumMode.HighVacuum;
        }, cancellationToken);
    }

    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Vacuum.Pump();
        }, cancellationToken);
    }

    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Vacuum.Vent();
        }, cancellationToken);
    }
}
