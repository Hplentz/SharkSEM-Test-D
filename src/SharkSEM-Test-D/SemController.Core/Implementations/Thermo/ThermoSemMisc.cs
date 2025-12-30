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
                
                string[] modelPaths = new[]
                {
                    "service.Beams.Electron.MicroscopeName",
                    "service.Instrument.Name",
                    "service.Instrument.Type",
                    "service.Configuration.Name",
                    "service.Configuration.MicroscopeName",
                    "service.MicroscopeName",
                    "service.InstrumentName"
                };
                
                try { info.Model = service.Beams.Electron.MicroscopeName; } catch { }
                if (info.Model == "SEM")
                {
                    try { info.Model = service.Instrument.Name; } catch { }
                }
                if (info.Model == "SEM")
                {
                    try { info.Model = service.Instrument.Type; } catch { }
                }
                if (info.Model == "SEM")
                {
                    try { info.Model = service.Configuration.Name; } catch { }
                }
                if (info.Model == "SEM")
                {
                    try { info.Model = service.Configuration.MicroscopeName; } catch { }
                }
                if (info.Model == "SEM")
                {
                    try { info.Model = service.MicroscopeName; } catch { }
                }
                if (info.Model == "SEM")
                {
                    try { info.Model = service.InstrumentName; } catch { }
                }
                
                try { info.SerialNumber = service.Instrument.SerialNumber; } catch { }
                if (info.SerialNumber == "Unknown")
                {
                    try { info.SerialNumber = service.Configuration.SerialNumber; } catch { }
                }
                if (info.SerialNumber == "Unknown")
                {
                    try { info.SerialNumber = service.SerialNumber; } catch { }
                }
                
                try { info.SoftwareVersion = service.Instrument.SoftwareVersion; } catch { }
                if (info.SoftwareVersion == "Unknown")
                {
                    try { info.SoftwareVersion = service.Configuration.SoftwareVersion; } catch { }
                }
                if (info.SoftwareVersion == "Unknown")
                {
                    try { info.SoftwareVersion = service.SoftwareVersion; } catch { }
                }
                if (info.SoftwareVersion == "Unknown")
                {
                    try { info.SoftwareVersion = service.Version; } catch { }
                }
            }
            catch
            {
            }

            return info;
        }, cancellationToken);
    }
}
