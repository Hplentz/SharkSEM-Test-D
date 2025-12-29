using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemBeam
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    internal ThermoSemBeam(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    public async Task<BeamState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var client = _getClient();
                var hv = client.Beams.ElectronBeam.HighVoltage.Value;
                return hv > 0 ? BeamState.On : BeamState.Off;
            }
            catch
            {
                return BeamState.Unknown;
            }
        }, cancellationToken);
    }

    public async Task TurnOnAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Beams.ElectronBeam.TurnOn();
        }, cancellationToken);
    }

    public async Task<bool> WaitForOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = await GetStateAsync(cancellationToken);
            if (state == BeamState.On)
                return true;
            await Task.Delay(500, cancellationToken);
        }
        return false;
    }

    public async Task TurnOffAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Beams.ElectronBeam.TurnOff();
        }, cancellationToken);
    }

    public async Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            return client.Beams.ElectronBeam.HighVoltage.Value;
        }, cancellationToken);
    }

    public async Task SetHighVoltageAsync(double voltage, bool waitForCompletion = false, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var client = _getClient();
            client.Beams.ElectronBeam.HighVoltage.Value = voltage;
        }, cancellationToken);
        
        if (waitForCompletion)
        {
            await Task.Delay(2000, cancellationToken);
        }
    }

    public async Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            
            try
            {
                var electronBeam = client.Beams.ElectronBeam;
                if (electronBeam == null)
                    return 0.0;
                
                var emissionProperty = electronBeam.EmissionCurrent;
                if (emissionProperty == null)
                    return 0.0;
                
                return emissionProperty.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetEmissionCurrentAsync error: {ex.Message}");
                return 0.0;
            }
        }, cancellationToken);
    }

    public async Task<(double Current, string DebugInfo)> GetEmissionCurrentWithDebugAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            var debugInfo = new System.Text.StringBuilder();
            double current = 0.0;
            
            try
            {
                var beams = client.Beams;
                debugInfo.AppendLine($"Beams object: {beams?.GetType().FullName ?? "null"}");
                
                if (beams != null)
                {
                    var electronBeam = beams.ElectronBeam;
                    debugInfo.AppendLine($"ElectronBeam object: {electronBeam?.GetType().FullName ?? "null"}");
                    
                    if (electronBeam != null)
                    {
                        var emissionProperty = electronBeam.EmissionCurrent;
                        debugInfo.AppendLine($"EmissionCurrent type: {emissionProperty?.GetType().FullName ?? "null"}");
                        
                        if (emissionProperty != null)
                        {
                            current = emissionProperty.Value;
                            debugInfo.AppendLine($"EmissionCurrent.Value: {current}");
                            debugInfo.AppendLine($"EmissionCurrent.Value (µA): {current * 1e6}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error: {ex.GetType().Name}: {ex.Message}");
            }
            
            return (current, debugInfo.ToString());
        }, cancellationToken);
    }

    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(BlankerMode.Auto);
    }

    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}
