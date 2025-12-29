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
            var client = _getClient();
            var state = client.Vacuum.ChamberState;
            
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
            var client = _getClient();
            
            try
            {
                var vacuum = client.Vacuum;
                if (vacuum == null)
                    return 0.0;
                
                var pressureProperty = vacuum.ChamberPressure;
                if (pressureProperty == null)
                    return 0.0;
                
                return pressureProperty.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPressureAsync error: {ex.Message}");
                return 0.0;
            }
        }, cancellationToken);
    }

    public async Task<(double Pressure, string RawState, string DebugInfo)> GetPressureWithDebugAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            var debugInfo = new System.Text.StringBuilder();
            double pressure = 0.0;
            string rawState = "";
            
            try
            {
                var vacuum = client.Vacuum;
                debugInfo.AppendLine($"Vacuum object: {vacuum?.GetType().FullName ?? "null"}");
                
                if (vacuum != null)
                {
                    rawState = vacuum.ChamberState ?? "null";
                    debugInfo.AppendLine($"ChamberState: {rawState}");
                    
                    var pressureProperty = vacuum.ChamberPressure;
                    debugInfo.AppendLine($"ChamberPressure type: {pressureProperty?.GetType().FullName ?? "null"}");
                    
                    if (pressureProperty != null)
                    {
                        pressure = pressureProperty.Value;
                        debugInfo.AppendLine($"ChamberPressure.Value: {pressure}");
                    }
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error: {ex.GetType().Name}: {ex.Message}");
            }
            
            return (pressure, rawState, debugInfo.ToString());
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
