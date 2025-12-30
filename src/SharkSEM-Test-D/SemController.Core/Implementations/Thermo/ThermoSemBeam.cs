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
                SdbMicroscopeClient client = _getClient();
                double hv = client.Beams.ElectronBeam.HighVoltage.Value;
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
        DateTime startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeamState state = await GetStateAsync(cancellationToken);
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
            SdbMicroscopeClient client = _getClient();
            return client.Beams.ElectronBeam.HighVoltage.Value;
        }, cancellationToken);
    }

    public async Task SetHighVoltageAsync(double voltage, bool waitForCompletion = false, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
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
            SdbMicroscopeClient client = _getClient();
            return client.Beams.ElectronBeam.EmissionCurrent.Value;
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
