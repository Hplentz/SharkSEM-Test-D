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
            var service = _getClient().Service;
            return new MicroscopeInfo
            {
                Manufacturer = "Thermo Fisher Scientific",
                Model = service.System.Name,
                SerialNumber = service.System.SerialNumber,
                SoftwareVersion = service.System.Version,
                ProtocolVersion = "AutoScript"
            };
        }, cancellationToken);
    }
}
