using SemController.Core.Models;

namespace SemController.Core.Implementations;

public class TescanSemMisc
{
    private readonly TescanSemController _controller;
    
    internal TescanSemMisc(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        var info = new MicroscopeInfo { Manufacturer = "TESCAN" };
        
        try
        {
            var response = await _controller.SendCommandInternalAsync("TcpGetModel", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.Model = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        try
        {
            var response = await _controller.SendCommandInternalAsync("TcpGetDevice", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.SerialNumber = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        try
        {
            var response = await _controller.SendCommandInternalAsync("TcpGetSWVersion", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.SoftwareVersion = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        try
        {
            var response = await _controller.SendCommandInternalAsync("TcpGetVersion", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.ProtocolVersion = TescanSemController.DecodeStringInternal(response, ref offset);
            }
        }
        catch { }
        
        return info;
    }
}
