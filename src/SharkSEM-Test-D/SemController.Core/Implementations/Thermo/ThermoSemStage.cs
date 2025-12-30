// =============================================================================
// ThermoSemStage.cs - Thermo Specimen Stage Control
// =============================================================================
// Manages specimen stage operations for Thermo Fisher microscopes including
// position queries, absolute/relative movement, and home/calibration.
//
// Unit Conversion:
// - AutoScript uses meters for position, radians for angles
// - ISemController interface uses millimeters and degrees
// - This module handles all conversions transparently
// =============================================================================

using AutoScript.Clients;
using AutoScript.Libraries.SdbMicroscope.Structures;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

/// <summary>
/// Specimen stage control sub-module for Thermo Fisher SEMs.
/// Handles position queries, movement, and calibration.
/// Converts between AutoScript units (meters/radians) and interface units (mm/degrees).
/// </summary>
public class ThermoSemStage
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    /// <summary>
    /// Internal constructor - instantiated by ThermoSemController.
    /// </summary>
    internal ThermoSemStage(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    /// <summary>
    /// Gets current stage position.
    /// Converts AutoScript meters/radians to mm/degrees.
    /// </summary>
    public async Task<Models.StagePosition> GetPositionAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            AutoScript.Libraries.SdbMicroscope.Structures.StagePosition pos = _getClient().Specimen.Stage.CurrentPosition;
            
            // Convert from meters to millimeters (linear axes)
            // Convert from radians to degrees (angular axes)
            return new Models.StagePosition
            {
                X = (pos.X ?? 0) * 1000.0,           // m -> mm
                Y = (pos.Y ?? 0) * 1000.0,           // m -> mm
                Z = (pos.Z ?? 0) * 1000.0,           // m -> mm
                Rotation = (pos.R ?? 0) * (180.0 / Math.PI),  // rad -> deg
                TiltX = (pos.T ?? 0) * (180.0 / Math.PI)      // rad -> deg
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Moves stage to absolute position.
    /// Converts mm/degrees to meters/radians for AutoScript.
    /// </summary>
    public async Task MoveAbsoluteAsync(Models.StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Convert from mm/degrees to meters/radians for AutoScript
            AutoScript.Libraries.SdbMicroscope.Structures.StagePosition targetPos = new AutoScript.Libraries.SdbMicroscope.Structures.StagePosition
            {
                X = position.X / 1000.0,                        // mm -> m
                Y = position.Y / 1000.0,                        // mm -> m
                Z = (position.Z ?? 0) / 1000.0,                 // mm -> m
                R = (position.Rotation ?? 0) * (Math.PI / 180.0), // deg -> rad
                T = (position.TiltX ?? 0) * (Math.PI / 180.0)     // deg -> rad
            };
            _getClient().Specimen.Stage.AbsoluteMove(targetPos);
        }, cancellationToken);
    }

    /// <summary>
    /// Moves stage by relative delta from current position.
    /// </summary>
    public async Task MoveRelativeAsync(Models.StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Convert delta from mm/degrees to meters/radians
            AutoScript.Libraries.SdbMicroscope.Structures.StagePosition deltaPos = new AutoScript.Libraries.SdbMicroscope.Structures.StagePosition
            {
                X = delta.X / 1000.0,
                Y = delta.Y / 1000.0,
                Z = (delta.Z ?? 0) / 1000.0,
                R = (delta.Rotation ?? 0) * (Math.PI / 180.0),
                T = (delta.TiltX ?? 0) * (Math.PI / 180.0)
            };
            _getClient().Specimen.Stage.RelativeMove(deltaPos);
        }, cancellationToken);
    }

    /// <summary>
    /// Checks if stage is currently moving.
    /// Currently returns false (motion detection not yet implemented).
    /// </summary>
    public async Task<bool> IsMovingAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Query actual motion state from AutoScript
        return await Task.FromResult(false);
    }

    /// <summary>
    /// Stops stage motion immediately.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Specimen.Stage.Stop();
        }, cancellationToken);
    }

    /// <summary>
    /// Gets stage travel limits.
    /// Currently returns typical default limits (actual limits not yet queried).
    /// </summary>
    public async Task<StageLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Query actual limits from AutoScript Stage.Limits property
        return await Task.FromResult(new StageLimits
        {
            MinX = -50, MaxX = 50,       // mm
            MinY = -50, MaxY = 50,       // mm
            MinZ = 0, MaxZ = 50,         // mm
            MinRotation = -180, MaxRotation = 180,  // degrees
            MinTiltX = -10, MaxTiltX = 60          // degrees
        });
    }

    /// <summary>
    /// Initiates stage home/calibration sequence.
    /// </summary>
    public async Task CalibrateAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Specimen.Stage.Home();
        }, cancellationToken);
    }

    /// <summary>
    /// Checks if stage has been homed/calibrated.
    /// </summary>
    public async Task<bool> IsCalibratedAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _getClient().Specimen.Stage.IsHomed;
        }, cancellationToken);
    }
}
