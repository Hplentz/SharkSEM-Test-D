// =============================================================================
// TescanSemHighVoltage.cs - TESCAN High Voltage and Beam Control
// =============================================================================
// Manages electron beam and high voltage operations for TESCAN microscopes.
// Handles beam on/off, voltage setting, and emission current monitoring.
//
// SharkSEM Commands Used:
// - HVGetBeam: Returns beam state as integer
// - HVBeamOn: Turns beam on (uses wait flags for completion)
// - HVBeamOff: Turns beam off
// - HVGetVoltage: Returns current accelerating voltage
// - HVSetVoltage: Sets accelerating voltage (with async flag)
// - HVGetEmission: Returns emission current in microamps
//
// Unit Conventions:
// - Voltage in Volts
// - Emission current returned by protocol in microamps, converted to Amperes
// =============================================================================

using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// High voltage and electron beam control sub-module for TESCAN SEMs.
/// Handles beam state, accelerating voltage, and emission monitoring.
/// </summary>
public class TescanSemHighVoltage
{
    private readonly TescanSemController _controller;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemHighVoltage(TescanSemController controller)
    {
        _controller = controller;
    }
    
    /// <summary>
    /// Gets current beam state (Off, On, Transitioning, Unknown).
    /// </summary>
    public async Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("HVGetBeam", null, cancellationToken);
        if (response.Length >= 4)
        {
            int state = TescanSemController.DecodeIntInternal(response, 0);
            return (BeamState)state;
        }
        return BeamState.Unknown;
    }
    
    /// <summary>
    /// Turns on the electron beam.
    /// Uses WaitFlagOptics and WaitFlagAuto to wait for beam stabilization.
    /// </summary>
    public async Task BeamOnAsync(CancellationToken cancellationToken = default)
    {
        // Wait flags ensure command doesn't return until beam is stable
        await _controller.SendCommandWithWaitInternalAsync("HVBeamOn", null, TescanSemController.WaitFlagOpticsInternal | TescanSemController.WaitFlagAutoInternal, cancellationToken);
    }
    
    /// <summary>
    /// Waits for beam to reach On state with timeout.
    /// Polls beam state every 200ms until On, Off/Unknown (failure), or timeout.
    /// </summary>
    public async Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            BeamState state = await GetBeamStateAsync(cancellationToken);
            if (state == BeamState.On)
                return true;
            if (state == BeamState.Off || state == BeamState.Unknown)
                return false; // Failed to turn on
            await Task.Delay(200, cancellationToken);
        }
        return false;
    }
    
    /// <summary>
    /// Turns off the electron beam.
    /// </summary>
    public async Task BeamOffAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("HVBeamOff", null, cancellationToken);
    }
    
    /// <summary>
    /// Gets current accelerating voltage in Volts.
    /// </summary>
    public async Task<double> GetVoltageAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("HVGetVoltage", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    /// <summary>
    /// Sets accelerating voltage in Volts.
    /// </summary>
    /// <param name="voltage">Target voltage in Volts.</param>
    /// <param name="waitForCompletion">
    /// If true (asyncFlag=0), command blocks until voltage stabilizes.
    /// If false (asyncFlag=1), command returns immediately.
    /// </param>
    public async Task SetVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        // asyncFlag: 0 = wait for completion, 1 = return immediately
        int asyncFlag = waitForCompletion ? 0 : 1;
        List<byte> body = new List<byte>();
        body.AddRange(TescanSemController.EncodeFloatInternal(voltage));
        body.AddRange(TescanSemController.EncodeIntInternal(asyncFlag));
        await _controller.SendCommandNoResponseInternalAsync("HVSetVoltage", body.ToArray(), cancellationToken);
    }
    
    /// <summary>
    /// Gets current emission current in Amperes.
    /// SharkSEM returns microamps; this method converts to Amperes.
    /// </summary>
    public async Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("HVGetEmission", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            double emissionMicroAmps = TescanSemController.DecodeFloatInternal(response, ref offset);
            return emissionMicroAmps * 1e-6; // Convert µA to A
        }
        return double.NaN;
    }
}
