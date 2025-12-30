// =============================================================================
// ThermoSemOptics.cs - Thermo Electron Optics Control
// =============================================================================
// Manages electron optics for Thermo Fisher microscopes including view field
// (magnification), working distance, focus, and spot size.
//
// Unit Conversion:
// - AutoScript uses meters for linear dimensions
// - ISemController uses micrometers for view field, millimeters for WD
// - This module handles all conversions transparently
// =============================================================================

using AutoScript.Clients;

namespace SemController.Core.Implementations.Thermo;

/// <summary>
/// Electron optics control sub-module for Thermo Fisher SEMs.
/// Handles view field, working distance, focus, and spot size.
/// </summary>
public class ThermoSemOptics
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    /// <summary>
    /// Internal constructor - instantiated by ThermoSemController.
    /// </summary>
    internal ThermoSemOptics(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    /// <summary>
    /// Gets current horizontal field width (view field) in micrometers.
    /// Smaller values = higher magnification.
    /// </summary>
    public async Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            double hfwMeters = client.Beams.ElectronBeam.HorizontalFieldWidth.Value;
            return hfwMeters * 1e6; // Convert meters to micrometers
        }, cancellationToken);
    }

    /// <summary>
    /// Gets current magnification (calculated from view field).
    /// Based on typical SEM display width of 128mm.
    /// </summary>
    public async Task<double> GetMagnificationAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            double viewField = client.Beams.ElectronBeam.HorizontalFieldWidth.Value;
            if (viewField > 0)
            {
                // Standard magnification formula: display_width / view_field
                // 0.128m = 128mm typical SEM display width
                return 0.128 / viewField;
            }
            return 0.0;
        }, cancellationToken);
    }

    /// <summary>
    /// Sets horizontal field width (view field) in micrometers.
    /// </summary>
    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            client.Beams.ElectronBeam.HorizontalFieldWidth.Value = viewFieldMicrons / 1e6; // µm -> m
        }, cancellationToken);
    }

    /// <summary>
    /// Gets current working distance in millimeters.
    /// Working distance is the distance from final lens to specimen surface.
    /// </summary>
    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            double wdMeters = client.Beams.ElectronBeam.WorkingDistance.Value;
            return wdMeters * 1000.0; // m -> mm
        }, cancellationToken);
    }

    /// <summary>
    /// Sets working distance in millimeters.
    /// Note: This adjusts focus to achieve the specified working distance.
    /// </summary>
    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            client.Beams.ElectronBeam.WorkingDistance.Value = workingDistanceMm / 1000.0; // mm -> m
        }, cancellationToken);
    }

    /// <summary>
    /// Gets current focus (equivalent to working distance for Thermo).
    /// </summary>
    public async Task<double> GetFocusAsync(CancellationToken cancellationToken = default)
    {
        return await GetWorkingDistanceAsync(cancellationToken);
    }

    /// <summary>
    /// Sets focus (equivalent to working distance for Thermo).
    /// </summary>
    public async Task SetFocusAsync(double focus, CancellationToken cancellationToken = default)
    {
        await SetWorkingDistanceAsync(focus, cancellationToken);
    }

    /// <summary>
    /// Runs automatic focus procedure.
    /// </summary>
    public async Task AutoFocusAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().AutoFunctions.RunAutoFocus();
        }, cancellationToken);
    }

    /// <summary>
    /// Gets current spot size (probe diameter).
    /// </summary>
    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            return client.Beams.ElectronBeam.Scanning.Spot.Value;
        }, cancellationToken);
    }
}
