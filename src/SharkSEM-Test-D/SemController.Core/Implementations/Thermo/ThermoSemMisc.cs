using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemMisc
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    internal ThermoSemMisc(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    public async Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            MicroscopeInfo info = new MicroscopeInfo
            {
                Manufacturer = "Thermo Fisher Scientific",
                Model = "SEM",
                SerialNumber = "Unknown",
                SoftwareVersion = "Unknown",
                ProtocolVersion = "AutoScript"
            };

            try
            {
                var service = _getClient().Service;
                var system = service.System;
                
                try
                {
                    info.Model = system.Name;
                }
                catch (Exception ex) when (ex.Message.Contains("timed out") || ex is TimeoutException)
                {
                    info.Model = "SEM (timeout)";
                }
                catch
                {
                }
                
                try
                {
                    info.SerialNumber = system.SerialNumber;
                }
                catch (Exception ex) when (ex.Message.Contains("timed out") || ex is TimeoutException)
                {
                    info.SerialNumber = "Timeout";
                }
                catch
                {
                }
                
                try
                {
                    info.SoftwareVersion = system.Version;
                }
                catch (Exception ex) when (ex.Message.Contains("timed out") || ex is TimeoutException)
                {
                    info.SoftwareVersion = "Timeout";
                }
                catch
                {
                }
            }
            catch
            {
            }

            return info;
        }, cancellationToken);
    }
}
