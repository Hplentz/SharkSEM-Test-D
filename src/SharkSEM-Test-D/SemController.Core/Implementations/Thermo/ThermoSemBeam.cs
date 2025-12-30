// =============================================================================
// ThermoSemBeam.cs - Thermo Electron Beam Control
// =============================================================================
// Manages electron beam operations for Thermo Fisher microscopes including
// beam on/off, high voltage control, and emission current monitoring.
//
// All methods wrap synchronous AutoScript calls in Task.Run() for async
// compatibility.
// =============================================================================

using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

/// <summary>
/// Electron beam control sub-module for Thermo Fisher SEMs.
/// Handles beam state, high voltage, and emission current.
/// </summary>
public class ThermoSemBeam
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    /// <summary>
    /// Internal constructor - instantiated by ThermoSemController.
    /// </summary>
    internal ThermoSemBeam(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    /// <summary>
    /// Gets current beam state by checking if high voltage is applied.
    /// Returns On if HV > 0, Off if HV = 0, Unknown on error.
    /// </summary>
    public async Task<BeamState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                SdbMicroscopeClient client = _getClient();
                double hv = client.Beams.ElectronBeam.HighVoltage.Value;
                // Infer beam state from high voltage value
                return hv > 0 ? BeamState.On : BeamState.Off;
            }
            catch
            {
                return BeamState.Unknown;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Turns on the electron beam.
    /// </summary>
    public async Task TurnOnAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Beams.ElectronBeam.TurnOn();
        }, cancellationToken);
    }

    /// <summary>
    /// Turns on beam and waits for it to be ready.
    /// Polls beam state every 500ms until On or timeout.
    /// </summary>
    /// <param name="timeoutMs">Maximum wait time in milliseconds.</param>
    /// <returns>True if beam is on within timeout, false otherwise.</returns>
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

    /// <summary>
    /// Turns off the electron beam.
    /// </summary>
    public async Task TurnOffAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Beams.ElectronBeam.TurnOff();
        }, cancellationToken);
    }

    /// <summary>
    /// Gets current accelerating voltage in Volts.
    /// </summary>
    public async Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            return client.Beams.ElectronBeam.HighVoltage.Value;
        }, cancellationToken);
    }

    /// <summary>
    /// Sets accelerating voltage in Volts.
    /// </summary>
    /// <param name="voltage">Target voltage in Volts.</param>
    /// <param name="waitForCompletion">If true, waits for voltage to stabilize.</param>
    public async Task SetHighVoltageAsync(double voltage, bool waitForCompletion = false, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            // AutoScript uses property setter for voltage changes
            client.Beams.ElectronBeam.HighVoltage.Value = voltage;
        }, cancellationToken);
        
        // Simple delay-based wait for stabilization
        // TODO: Could poll actual voltage until it matches target
        if (waitForCompletion)
        {
            await Task.Delay(2000, cancellationToken);
        }
    }

    /// <summary>
    /// Gets current emission current in Amperes.
    /// </summary>
    public async Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            return client.Beams.ElectronBeam.EmissionCurrent.Value;
        }, cancellationToken);
    }

    /// <summary>
    /// Gets blanker mode (currently returns Auto as default).
    /// </summary>
    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual blanker mode query when API path is confirmed
        return await Task.FromResult(BlankerMode.Auto);
    }

    /// <summary>
    /// Sets blanker mode (not yet implemented for Thermo).
    /// </summary>
    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        // TODO: Implement blanker mode control when API path is confirmed
        await Task.CompletedTask;
    }
}
