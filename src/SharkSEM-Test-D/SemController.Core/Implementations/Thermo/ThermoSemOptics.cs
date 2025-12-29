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
            var client = _getClient();
            
            try
            {
                var hfwProperty = client.Beams.ElectronBeam.HorizontalFieldWidth;
                
                if (hfwProperty == null)
                    return 0.0;
                
                var hfwMeters = hfwProperty.Value;
                return hfwMeters * 1e6;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetViewFieldAsync error: {ex.Message}");
                return 0.0;
            }
        }, cancellationToken);
    }

    public async Task<double> GetMagnificationAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            try
            {
                var viewField = client.Beams.ElectronBeam.HorizontalFieldWidth.Value;
                if (viewField > 0)
                {
                    return 0.128 / viewField;
                }
                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }, cancellationToken);
    }

    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var client = _getClient();
            client.Beams.ElectronBeam.HorizontalFieldWidth.Value = viewFieldMicrons / 1e6;
        }, cancellationToken);
    }

    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            
            try
            {
                var wdProperty = client.Beams.ElectronBeam.WorkingDistance;
                
                if (wdProperty == null)
                    return 0.0;
                
                var wdMeters = wdProperty.Value;
                return wdMeters * 1000.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetWorkingDistanceAsync error: {ex.Message}");
                return 0.0;
            }
        }, cancellationToken);
    }

    public async Task<(double WdMm, string DebugInfo)> GetWorkingDistanceWithDebugAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            var debugInfo = new System.Text.StringBuilder();
            double wdMm = 0.0;
            
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
                        var wdProperty = electronBeam.WorkingDistance;
                        debugInfo.AppendLine($"WorkingDistance type: {wdProperty?.GetType().FullName ?? "null"}");
                        
                        if (wdProperty != null)
                        {
                            var wdMeters = wdProperty.Value;
                            wdMm = wdMeters * 1000.0;
                            debugInfo.AppendLine($"WorkingDistance.Value (meters): {wdMeters}");
                            debugInfo.AppendLine($"WorkingDistance (mm): {wdMm}");
                        }
                        
                        var hfwProperty = electronBeam.HorizontalFieldWidth;
                        debugInfo.AppendLine($"HorizontalFieldWidth type: {hfwProperty?.GetType().FullName ?? "null"}");
                        
                        if (hfwProperty != null)
                        {
                            var hfwMeters = hfwProperty.Value;
                            debugInfo.AppendLine($"HorizontalFieldWidth.Value (meters): {hfwMeters}");
                            debugInfo.AppendLine($"HorizontalFieldWidth (µm): {hfwMeters * 1e6}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error: {ex.GetType().Name}: {ex.Message}");
            }
            
            return (wdMm, debugInfo.ToString());
        }, cancellationToken);
    }

    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var client = _getClient();
            client.Beams.ElectronBeam.WorkingDistance.Value = workingDistanceMm / 1000.0;
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
            var client = _getClient();
            try
            {
                return client.Beams.ElectronBeam.Scanning.Spot.Value;
            }
            catch
            {
                return 0.0;
            }
        }, cancellationToken);
    }
}
