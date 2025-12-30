using AutoScript.Clients;
using AutoScript.Libraries.SdbMicroscope.Structures;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemStage
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    internal ThermoSemStage(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    public async Task<Models.StagePosition> GetPositionAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            AutoScript.Libraries.SdbMicroscope.Structures.StagePosition pos = _getClient().Specimen.Stage.CurrentPosition;
            return new Models.StagePosition
            {
                X = (pos.X ?? 0) * 1000.0,
                Y = (pos.Y ?? 0) * 1000.0,
                Z = (pos.Z ?? 0) * 1000.0,
                Rotation = (pos.R ?? 0) * (180.0 / Math.PI),
                TiltX = (pos.T ?? 0) * (180.0 / Math.PI)
            };
        }, cancellationToken);
    }

    public async Task MoveAbsoluteAsync(Models.StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            AutoScript.Libraries.SdbMicroscope.Structures.StagePosition targetPos = new AutoScript.Libraries.SdbMicroscope.Structures.StagePosition
            {
                X = position.X / 1000.0,
                Y = position.Y / 1000.0,
                Z = (position.Z ?? 0) / 1000.0,
                R = (position.Rotation ?? 0) * (Math.PI / 180.0),
                T = (position.TiltX ?? 0) * (Math.PI / 180.0)
            };
            _getClient().Specimen.Stage.AbsoluteMove(targetPos);
        }, cancellationToken);
    }

    public async Task MoveRelativeAsync(Models.StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
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

    public async Task<bool> IsMovingAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Specimen.Stage.Stop();
        }, cancellationToken);
    }

    public async Task<StageLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new StageLimits
        {
            MinX = -50, MaxX = 50,
            MinY = -50, MaxY = 50,
            MinZ = 0, MaxZ = 50,
            MinRotation = -180, MaxRotation = 180,
            MinTiltX = -10, MaxTiltX = 60
        });
    }

    public async Task CalibrateAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Specimen.Stage.Home();
        }, cancellationToken);
    }

    public async Task<bool> IsCalibratedAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _getClient().Specimen.Stage.IsHomed;
        }, cancellationToken);
    }
}
