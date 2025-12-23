using AutoScript.Clients;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemOptics
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    internal ThermoSemOptics(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    public async Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _getClient().Beams.ElectronBeam.HorizontalFieldWidth.Value * 1e6;
        }, cancellationToken);
    }

    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Beams.ElectronBeam.HorizontalFieldWidth.Value = viewFieldMicrons / 1e6;
        }, cancellationToken);
    }

    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _getClient().Beams.ElectronBeam.WorkingDistance.Value * 1000.0;
        }, cancellationToken);
    }

    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Beams.ElectronBeam.WorkingDistance.Value = workingDistanceMm / 1000.0;
        }, cancellationToken);
    }

    public async Task<double> GetFocusAsync(CancellationToken cancellationToken = default)
    {
        return await GetWorkingDistanceAsync(cancellationToken);
    }

    public async Task SetFocusAsync(double focus, CancellationToken cancellationToken = default)
    {
        await SetWorkingDistanceAsync(focus, cancellationToken);
    }

    public async Task AutoFocusAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().AutoFunctions.RunAutoFocus();
        }, cancellationToken);
    }

    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _getClient().Beams.ElectronBeam.Scanning.Spot.Value;
        }, cancellationToken);
    }
}
