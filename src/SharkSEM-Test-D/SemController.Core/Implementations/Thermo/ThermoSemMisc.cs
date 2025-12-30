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
                dynamic service = _getClient().Service;
                
                try
                {
                    info.Model = service.Microscope.Name;
                }
                catch
                {
                    try
                    {
                        info.Model = service.Name;
                    }
                    catch { }
                }
                
                try
                {
                    info.SerialNumber = service.Microscope.SerialNumber;
                }
                catch
                {
                    try
                    {
                        info.SerialNumber = service.SerialNumber;
                    }
                    catch { }
                }
                
                try
                {
                    info.SoftwareVersion = service.Microscope.Version;
                }
                catch
                {
                    try
                    {
                        info.SoftwareVersion = service.Version;
                    }
                    catch { }
                }
            }
            catch
            {
            }

            return info;
        }, cancellationToken);
    }
}
