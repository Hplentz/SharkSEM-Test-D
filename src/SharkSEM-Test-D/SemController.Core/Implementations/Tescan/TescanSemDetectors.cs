namespace SemController.Core.Implementations.Tescan;

public class TescanSemDetectors
{
    private readonly TescanSemController _controller;
    
    internal TescanSemDetectors(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<string> EnumDetectorsAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("DtEnumDetectors", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeStringInternal(response, ref offset);
        }
        return string.Empty;
    }
    
    public async Task<int> GetChannelCountAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("DtGetChannels", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return 0;
    }
    
    public async Task<int> GetSelectedDetectorAsync(int channel, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(channel);
        byte[] response = await _controller.SendCommandInternalAsync("DtGetSelected", body, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return -1;
    }
    
    public async Task SelectDetectorAsync(int channel, int detector, CancellationToken cancellationToken = default)
    {
        List<byte> body = new List<byte>();
        body.AddRange(TescanSemController.EncodeIntInternal(channel));
        body.AddRange(TescanSemController.EncodeIntInternal(detector));
        await _controller.SendCommandNoResponseInternalAsync("DtSelect", body.ToArray(), cancellationToken);
    }
    
    public async Task<(int enabled, int bpp)> GetChannelEnabledAsync(int channel, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(channel);
        byte[] response = await _controller.SendCommandInternalAsync("DtGetEnabled", body, cancellationToken);
        if (response.Length >= 8)
        {
            int enabled = TescanSemController.DecodeIntInternal(response, 0);
            int bpp = TescanSemController.DecodeIntInternal(response, 4);
            return (enabled, bpp);
        }
        return (0, 0);
    }
    
    public async Task EnableChannelAsync(int channel, bool enable, int bpp = 8, CancellationToken cancellationToken = default)
    {
        List<byte> body = new List<byte>();
        body.AddRange(TescanSemController.EncodeIntInternal(channel));
        body.AddRange(TescanSemController.EncodeIntInternal(enable ? 1 : 0));
        body.AddRange(TescanSemController.EncodeIntInternal(bpp));
        await _controller.SendCommandNoResponseInternalAsync("DtEnable", body.ToArray(), cancellationToken);
    }
    
    public async Task AutoSignalAsync(int channel, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(channel);
        await _controller.SendCommandWithWaitInternalAsync("DtAutoSignal", body, TescanSemController.WaitFlagOpticsInternal | TescanSemController.WaitFlagAutoInternal, cancellationToken);
    }
}
