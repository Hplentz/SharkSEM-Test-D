// =============================================================================
// TescanSemVacuum.cs - TESCAN Vacuum System Control
// =============================================================================
// Handles vacuum system operations for TESCAN microscopes via SharkSEM protocol.
// Provides status monitoring, pressure readings for multiple gauges, and
// pump/vent control commands.
//
// SharkSEM Commands Used:
// - VacGetStatus: Returns vacuum system state as integer
// - VacGetPressure: Returns pressure in Pascals for specified gauge
// - VacGetVPMode: Returns variable pressure mode setting
// - VacPump: Initiates pump-down (no response expected)
// - VacVent: Initiates venting (no response expected)
// =============================================================================

using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Vacuum system control sub-module for TESCAN SEMs.
/// Communicates with microscope via SharkSEM protocol commands.
/// </summary>
public class TescanSemVacuum
{
    private readonly TescanSemController _controller;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemVacuum(TescanSemController controller)
    {
        _controller = controller;
    }
    
    /// <summary>
    /// Gets current vacuum system status.
    /// Returns VacuumStatus enum directly mapped from SharkSEM response.
    /// </summary>
    public async Task<VacuumStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("VacGetStatus", null, cancellationToken);
        if (response.Length >= 4)
        {
            int status = TescanSemController.DecodeIntInternal(response, 0);
            return (VacuumStatus)status;
        }
        return VacuumStatus.Error;
    }
    
    /// <summary>
    /// Gets pressure reading from specified gauge in Pascals.
    /// Different gauges monitor different parts of the vacuum system
    /// (chamber, column, gun, etc.).
    /// </summary>
    public async Task<double> GetPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal((int)gauge);
        byte[] response = await _controller.SendCommandInternalAsync("VacGetPressure", body, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    /// <summary>
    /// Gets current vacuum operating mode.
    /// Returns HighVacuum (0) or VariablePressure (1).
    /// </summary>
    public async Task<VacuumMode> GetModeAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("VacGetVPMode", null, cancellationToken);
        if (response.Length >= 4)
        {
            int mode = TescanSemController.DecodeIntInternal(response, 0);
            return (VacuumMode)mode;
        }
        return VacuumMode.Unknown;
    }
    
    /// <summary>
    /// Initiates chamber pump-down sequence.
    /// Fire-and-forget command (no response expected).
    /// 
    /// Note: This is a long-running operation. Monitor GetStatusAsync() for
    /// vacuum state changes. Pump-down time varies from seconds to minutes
    /// depending on chamber volume and current pressure.
    /// </summary>
    /// <exception cref="IOException">Thrown if communication with SEM fails.</exception>
    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _controller.SendCommandNoResponseInternalAsync("VacPump", null, cancellationToken);
        }
        catch (IOException ex)
        {
            throw new IOException(
                "Failed to initiate vacuum pump-down. " +
                "Check SEM connection and vacuum system status.", ex);
        }
    }
    
    /// <summary>
    /// Initiates chamber venting to atmosphere.
    /// Fire-and-forget command (no response expected).
    /// 
    /// CAUTION: Venting while beam is on can damage the electron gun!
    /// The SEM should automatically protect against this, but verify
    /// beam is off before venting if possible.
    /// 
    /// Note: This is a long-running operation. Monitor GetStatusAsync() for
    /// vacuum state changes.
    /// </summary>
    /// <exception cref="IOException">Thrown if communication with SEM fails.</exception>
    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _controller.SendCommandNoResponseInternalAsync("VacVent", null, cancellationToken);
        }
        catch (IOException ex)
        {
            throw new IOException(
                "Failed to initiate chamber venting. " +
                "Check SEM connection and vacuum system status.", ex);
        }
    }
}
