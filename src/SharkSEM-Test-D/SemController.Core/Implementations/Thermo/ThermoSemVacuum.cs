// =============================================================================
// ThermoSemVacuum.cs - Thermo Vacuum System Control
// =============================================================================
// Handles vacuum system operations for Thermo Fisher microscopes.
// Provides status monitoring, pressure readings, and pump/vent control.
//
// Note: All methods wrap synchronous AutoScript calls in Task.Run() to
// maintain async compatibility without blocking the calling thread.
// =============================================================================

using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

/// <summary>
/// Vacuum system control sub-module for Thermo Fisher SEMs.
/// Manages chamber vacuum state, pressure monitoring, and pump/vent operations.
/// </summary>
public class ThermoSemVacuum
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    /// <summary>
    /// Internal constructor - instantiated by ThermoSemController.
    /// </summary>
    /// <param name="getClient">Delegate that returns the connected client.</param>
    internal ThermoSemVacuum(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    /// <summary>
    /// Gets the current vacuum system status.
    /// Maps AutoScript ChamberState strings to standardized VacuumStatus enum.
    /// </summary>
    public async Task<VacuumStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            string? state = client.Vacuum.ChamberState;
            
            // Map various AutoScript state strings to our standardized enum
            // Different Thermo microscope models may report different strings
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

    /// <summary>
    /// Gets chamber pressure reading in Pascals.
    /// Note: Currently only returns chamber pressure; gauge parameter reserved for future use.
    /// </summary>
    public async Task<double> GetPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            return client.Vacuum.ChamberPressure.Value;
        }, cancellationToken);
    }

    /// <summary>
    /// Gets current vacuum operating mode.
    /// Currently returns HighVacuum as default (ESEM mode not yet implemented).
    /// </summary>
    public async Task<VacuumMode> GetModeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            // TODO: Query actual mode from AutoScript when ESEM support is added
            return VacuumMode.HighVacuum;
        }, cancellationToken);
    }

    /// <summary>
    /// Initiates chamber pump-down sequence.
    /// </summary>
    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Vacuum.Pump();
        }, cancellationToken);
    }

    /// <summary>
    /// Initiates chamber venting to atmosphere.
    /// </summary>
    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Vacuum.Vent();
        }, cancellationToken);
    }
}
