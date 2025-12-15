using System.Globalization;
using System.Net.Sockets;
using System.Text;
using SemController.Core.Interfaces;
using SemController.Core.Models;

namespace SemController.Core.Implementations;

public class TescanSemController : ISemController
{
    private readonly string _host;
    private readonly int _port;
    private readonly double _timeoutSeconds;
    private readonly TimeSpan _stageMovementTimeout = TimeSpan.FromMinutes(5);
    
    private TcpClient? _client;
    private NetworkStream? _stream;
    private TcpClient? _dataClient;
    private NetworkStream? _dataStream;
    private bool _disposed;
    private uint _messageId;
    private bool _dataChannelRegistered;
    
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    
    private const int HeaderSize = 32;
    private const int CommandNameSize = 16;
    private const ushort FlagSendResponse = 0x0001;
    
    private const ushort WaitFlagScan = 0x0100;
    private const ushort WaitFlagStage = 0x0200;
    private const ushort WaitFlagOptics = 0x0400;
    private const ushort WaitFlagAuto = 0x0800;
    
    public bool IsConnected => _client?.Connected ?? false;
    
    public TescanSemController(string host, int port = 8300, double timeoutSeconds = 30.0)
    {
        _host = host;
        _port = port;
        _timeoutSeconds = timeoutSeconds;
    }
    
    public TescanSemController(SemConnectionSettings settings)
        : this(settings.Host, settings.Port, settings.TimeoutSeconds)
    {
    }
    
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        
        _client = new TcpClient();
        _client.ReceiveTimeout = (int)(_timeoutSeconds * 1000);
        _client.SendTimeout = (int)(_timeoutSeconds * 1000);
        
        await _client.ConnectAsync(_host, _port, cancellationToken);
        _stream = _client.GetStream();
        
        _dataChannelRegistered = false;
    }
    
    private async Task EnsureDataChannelAsync(CancellationToken cancellationToken)
    {
        if (_dataClient?.Connected == true && _dataChannelRegistered)
            return;
        
        _dataClient = new TcpClient();
        _dataClient.ReceiveTimeout = (int)(_timeoutSeconds * 1000);
        _dataClient.SendTimeout = (int)(_timeoutSeconds * 1000);
        
        await _dataClient.ConnectAsync(_host, _port + 1, cancellationToken);
        _dataStream = _dataClient.GetStream();
        
        var localPort = ((System.Net.IPEndPoint)_dataClient.Client.LocalEndPoint!).Port;
        var regBody = EncodeInt(localPort);
        await SendCommandAsync("TcpRegDataPort", regBody, cancellationToken);
        
        _dataChannelRegistered = true;
    }
    
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _dataStream?.Close();
        _dataClient?.Close();
        _dataStream = null;
        _dataClient = null;
        _dataChannelRegistered = false;
        
        _stream?.Close();
        _client?.Close();
        _stream = null;
        _client = null;
        return Task.CompletedTask;
    }
    
    private byte[] BuildHeader(string command, uint bodySize, ushort flags = FlagSendResponse, ushort queue = 0)
    {
        var header = new byte[HeaderSize];
        
        var cmdBytes = Encoding.ASCII.GetBytes(command);
        var cmdLen = Math.Min(cmdBytes.Length, CommandNameSize - 1);
        Array.Copy(cmdBytes, 0, header, 0, cmdLen);
        
        BitConverter.GetBytes(bodySize).CopyTo(header, 16);
        BitConverter.GetBytes(++_messageId).CopyTo(header, 20);
        BitConverter.GetBytes(flags).CopyTo(header, 24);
        BitConverter.GetBytes(queue).CopyTo(header, 26);
        
        return header;
    }
    
    private static int Pad4(int size) => (size + 3) & ~3;
    
    private static byte[] EncodeInt(int value)
    {
        return BitConverter.GetBytes(value);
    }
    
    private static byte[] EncodeUInt(uint value)
    {
        return BitConverter.GetBytes(value);
    }
    
    private static byte[] EncodeFloat(double value)
    {
        var str = value.ToString("G", InvariantCulture) + '\0';
        var strBytes = Encoding.ASCII.GetBytes(str);
        var paddedSize = Pad4(4 + strBytes.Length);
        var result = new byte[paddedSize];
        BitConverter.GetBytes((uint)strBytes.Length).CopyTo(result, 0);
        Array.Copy(strBytes, 0, result, 4, strBytes.Length);
        return result;
    }
    
    private static byte[] EncodeString(string value)
    {
        var str = value + '\0';
        var strBytes = Encoding.ASCII.GetBytes(str);
        var paddedSize = Pad4(4 + strBytes.Length);
        var result = new byte[paddedSize];
        BitConverter.GetBytes((uint)strBytes.Length).CopyTo(result, 0);
        Array.Copy(strBytes, 0, result, 4, strBytes.Length);
        return result;
    }
    
    private async Task<byte[]> SendCommandAsync(string command, byte[]? body, CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope");
        
        var bodySize = (uint)(body?.Length ?? 0);
        var header = BuildHeader(command, bodySize);
        
        await _stream.WriteAsync(header, cancellationToken);
        if (body != null && body.Length > 0)
        {
            await _stream.WriteAsync(body, cancellationToken);
        }
        
        var responseHeader = new byte[HeaderSize];
        var bytesRead = 0;
        while (bytesRead < HeaderSize)
        {
            var read = await _stream.ReadAsync(responseHeader.AsMemory(bytesRead, HeaderSize - bytesRead), cancellationToken);
            if (read == 0) throw new IOException("Connection closed by server");
            bytesRead += read;
        }
        
        var responseBodySize = BitConverter.ToUInt32(responseHeader, 16);
        
        if (responseBodySize == 0)
            return Array.Empty<byte>();
        
        var responseBody = new byte[responseBodySize];
        bytesRead = 0;
        while (bytesRead < responseBodySize)
        {
            var read = await _stream.ReadAsync(responseBody.AsMemory(bytesRead, (int)responseBodySize - bytesRead), cancellationToken);
            if (read == 0) throw new IOException("Connection closed by server");
            bytesRead += read;
        }
        
        return responseBody;
    }
    
    private async Task SendCommandNoResponseAsync(string command, byte[]? body, CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope");
        
        var bodySize = (uint)(body?.Length ?? 0);
        var header = BuildHeader(command, bodySize, flags: 0);
        
        await _stream.WriteAsync(header, cancellationToken);
        if (body != null && body.Length > 0)
        {
            await _stream.WriteAsync(body, cancellationToken);
        }
    }
    
    private async Task<byte[]> SendCommandWithWaitAsync(string command, byte[]? body, ushort waitFlags, CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope");
        
        var bodySize = (uint)(body?.Length ?? 0);
        var flags = (ushort)(FlagSendResponse | waitFlags);
        var header = BuildHeader(command, bodySize, flags);
        
        await _stream.WriteAsync(header, cancellationToken);
        if (body != null && body.Length > 0)
        {
            await _stream.WriteAsync(body, cancellationToken);
        }
        
        var responseHeader = new byte[HeaderSize];
        var bytesRead = 0;
        while (bytesRead < HeaderSize)
        {
            var read = await _stream.ReadAsync(responseHeader.AsMemory(bytesRead, HeaderSize - bytesRead), cancellationToken);
            if (read == 0) throw new IOException("Connection closed by server");
            bytesRead += read;
        }
        
        var responseBodySize = BitConverter.ToUInt32(responseHeader, 16);
        if (responseBodySize == 0)
            return Array.Empty<byte>();
        
        var responseBody = new byte[responseBodySize];
        bytesRead = 0;
        while (bytesRead < (int)responseBodySize)
        {
            var read = await _stream.ReadAsync(responseBody.AsMemory(bytesRead, (int)responseBodySize - bytesRead), cancellationToken);
            if (read == 0) throw new IOException("Connection closed by server");
            bytesRead += read;
        }
        
        return responseBody;
    }
    
    private static int DecodeInt(byte[] body, int offset)
    {
        return BitConverter.ToInt32(body, offset);
    }
    
    private static double DecodeFloat(byte[] body, ref int offset)
    {
        var strLen = BitConverter.ToUInt32(body, offset);
        offset += 4;
        var str = Encoding.ASCII.GetString(body, offset, (int)strLen - 1);
        offset += Pad4((int)strLen);
        return double.Parse(str, NumberStyles.Float, InvariantCulture);
    }
    
    private static string DecodeString(byte[] body, ref int offset)
    {
        var strLen = BitConverter.ToUInt32(body, offset);
        offset += 4;
        var str = Encoding.ASCII.GetString(body, offset, (int)strLen - 1);
        offset += Pad4((int)strLen);
        return str;
    }
    
    public async Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        var info = new MicroscopeInfo { Manufacturer = "TESCAN" };
        
        try
        {
            var response = await SendCommandAsync("TcpGetModel", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.Model = DecodeString(response, ref offset);
            }
        }
        catch { }
        
        try
        {
            var response = await SendCommandAsync("TcpGetDevice", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.SerialNumber = DecodeString(response, ref offset);
            }
        }
        catch { }
        
        try
        {
            var response = await SendCommandAsync("TcpGetSWVersion", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.SoftwareVersion = DecodeString(response, ref offset);
            }
        }
        catch { }
        
        try
        {
            var response = await SendCommandAsync("TcpGetVersion", null, cancellationToken);
            if (response.Length > 0)
            {
                int offset = 0;
                info.ProtocolVersion = DecodeString(response, ref offset);
            }
        }
        catch { }
        
        return info;
    }
    
    public async Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("VacGetStatus", null, cancellationToken);
        if (response.Length >= 4)
        {
            var status = DecodeInt(response, 0);
            return (VacuumStatus)status;
        }
        return VacuumStatus.Error;
    }
    
    public async Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        var body = EncodeInt((int)gauge);
        var response = await SendCommandAsync("VacGetPressure", body, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return DecodeFloat(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("VacGetVPMode", null, cancellationToken);
        if (response.Length >= 4)
        {
            var mode = DecodeInt(response, 0);
            return (VacuumMode)mode;
        }
        return VacuumMode.Unknown;
    }
    
    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("VacPump", null, cancellationToken);
    }
    
    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("VacVent", null, cancellationToken);
    }
    
    public async Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("HVGetBeam", null, cancellationToken);
        if (response.Length >= 4)
        {
            var state = DecodeInt(response, 0);
            return (BeamState)state;
        }
        return BeamState.Unknown;
    }
    
    public async Task BeamOnAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandWithWaitAsync("HVBeamOn", null, WaitFlagOptics | WaitFlagAuto, cancellationToken);
    }
    
    public async Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var state = await GetBeamStateAsync(cancellationToken);
            if (state == BeamState.On)
                return true;
            if (state == BeamState.Off || state == BeamState.Unknown)
                return false;
            await Task.Delay(200, cancellationToken);
        }
        return false;
    }
    
    public async Task BeamOffAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("HVBeamOff", null, cancellationToken);
    }
    
    public async Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("HVGetVoltage", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return DecodeFloat(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var asyncFlag = waitForCompletion ? 0 : 1;
        var body = new List<byte>();
        body.AddRange(EncodeFloat(voltage));
        body.AddRange(EncodeInt(asyncFlag));
        await SendCommandNoResponseAsync("HVSetVoltage", body.ToArray(), cancellationToken);
    }
    
    public async Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("HVGetEmission", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            var emissionMicroAmps = DecodeFloat(response, ref offset);
            return emissionMicroAmps * 1e-6;
        }
        return double.NaN;
    }
    
    public async Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("StgGetPosition", null, cancellationToken);
        var position = new StagePosition();
        
        if (response.Length > 0)
        {
            int offset = 0;
            if (offset < response.Length) position.X = DecodeFloat(response, ref offset);
            if (offset < response.Length) position.Y = DecodeFloat(response, ref offset);
            if (offset < response.Length) position.Z = DecodeFloat(response, ref offset);
            if (offset < response.Length) position.Rotation = DecodeFloat(response, ref offset);
            if (offset < response.Length) position.TiltX = DecodeFloat(response, ref offset);
            if (offset < response.Length) position.TiltY = DecodeFloat(response, ref offset);
        }
        
        return position;
    }
    
    public async Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var body = new List<byte>();
        body.AddRange(EncodeFloat(position.X));
        body.AddRange(EncodeFloat(position.Y));
        body.AddRange(EncodeFloat(position.Z));
        body.AddRange(EncodeFloat(position.Rotation));
        body.AddRange(EncodeFloat(position.TiltX));
        if (position.TiltY.HasValue)
        {
            body.AddRange(EncodeFloat(position.TiltY.Value));
        }
        
        await SendCommandNoResponseAsync("StgMoveTo", body.ToArray(), cancellationToken);
        
        if (waitForCompletion)
        {
            await WaitForStageMovementAsync(cancellationToken);
        }
    }
    
    public async Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var body = new List<byte>();
        body.AddRange(EncodeFloat(delta.X));
        body.AddRange(EncodeFloat(delta.Y));
        body.AddRange(EncodeFloat(delta.Z));
        body.AddRange(EncodeFloat(delta.Rotation));
        body.AddRange(EncodeFloat(delta.TiltX));
        if (delta.TiltY.HasValue)
        {
            body.AddRange(EncodeFloat(delta.TiltY.Value));
        }
        
        await SendCommandNoResponseAsync("StgMove", body.ToArray(), cancellationToken);
        
        if (waitForCompletion)
        {
            await WaitForStageMovementAsync(cancellationToken);
        }
    }
    
    private async Task WaitForStageMovementAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        while (await IsStageMovingAsync(cancellationToken))
        {
            if (DateTime.UtcNow - startTime > _stageMovementTimeout)
            {
                throw new TimeoutException($"Stage movement timed out after {_stageMovementTimeout.TotalMinutes} minutes");
            }
            await Task.Delay(100, cancellationToken);
        }
    }
    
    public async Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("StgIsBusy", null, cancellationToken);
        if (response.Length >= 4)
        {
            return DecodeInt(response, 0) != 0;
        }
        return false;
    }
    
    public async Task StopStageAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("StgStop", null, cancellationToken);
    }
    
    public async Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default)
    {
        var body = EncodeInt(0);
        var response = await SendCommandAsync("StgGetLimits", body, cancellationToken);
        
        var limits = new StageLimits();
        if (response.Length > 0)
        {
            int offset = 0;
            if (offset < response.Length) limits.MinX = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MaxX = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MinY = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MaxY = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MinZ = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MaxZ = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MinRotation = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MaxRotation = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MinTiltX = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MaxTiltX = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MinTiltY = DecodeFloat(response, ref offset);
            if (offset < response.Length) limits.MaxTiltY = DecodeFloat(response, ref offset);
        }
        
        return limits;
    }
    
    public async Task CalibrateStageAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("StgCalibrate", null, cancellationToken);
    }
    
    public async Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("StgIsCalibrated", null, cancellationToken);
        if (response.Length >= 4)
        {
            return DecodeInt(response, 0) != 0;
        }
        return false;
    }
    
    public async Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("GetViewField", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            var viewFieldMm = DecodeFloat(response, ref offset);
            return viewFieldMm * 1000.0;
        }
        return double.NaN;
    }
    
    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        var viewFieldMm = viewFieldMicrons / 1000.0;
        var body = EncodeFloat(viewFieldMm);
        await SendCommandNoResponseAsync("SetViewField", body, cancellationToken);
    }
    
    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("GetWD", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return DecodeFloat(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        var body = EncodeFloat(workingDistanceMm);
        await SendCommandNoResponseAsync("SetWD", body, cancellationToken);
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
        var body = EncodeInt(0);
        await SendCommandWithWaitAsync("AutoWD", body, WaitFlagOptics | WaitFlagAuto, cancellationToken);
    }
    
    public async Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("ScGetSpeed", null, cancellationToken);
        if (response.Length >= 4)
        {
            return DecodeInt(response, 0);
        }
        return 0;
    }
    
    public async Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        var body = EncodeInt(speed);
        await SendCommandNoResponseAsync("ScSetSpeed", body, cancellationToken);
    }
    
    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("ScGetBlanker", null, cancellationToken);
        if (response.Length >= 4)
        {
            return (BlankerMode)DecodeInt(response, 0);
        }
        return BlankerMode.Off;
    }
    
    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        var body = EncodeInt((int)mode);
        await SendCommandNoResponseAsync("ScSetBlanker", body, cancellationToken);
    }
    
    public async Task<string> EnumDetectorsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("DtEnumDetectors", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return DecodeString(response, ref offset);
        }
        return string.Empty;
    }
    
    public async Task<int> GetChannelCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("DtGetChannels", null, cancellationToken);
        if (response.Length >= 4)
        {
            return DecodeInt(response, 0);
        }
        return 0;
    }
    
    public async Task<int> GetSelectedDetectorAsync(int channel, CancellationToken cancellationToken = default)
    {
        var body = EncodeInt(channel);
        var response = await SendCommandAsync("DtGetSelected", body, cancellationToken);
        if (response.Length >= 4)
        {
            return DecodeInt(response, 0);
        }
        return -1;
    }
    
    public async Task SelectDetectorAsync(int channel, int detector, CancellationToken cancellationToken = default)
    {
        var body = new List<byte>();
        body.AddRange(EncodeInt(channel));
        body.AddRange(EncodeInt(detector));
        await SendCommandNoResponseAsync("DtSelect", body.ToArray(), cancellationToken);
    }
    
    public async Task<(int enabled, int bpp)> GetChannelEnabledAsync(int channel, CancellationToken cancellationToken = default)
    {
        var body = EncodeInt(channel);
        var response = await SendCommandAsync("DtGetEnabled", body, cancellationToken);
        if (response.Length >= 8)
        {
            var enabled = DecodeInt(response, 0);
            var bpp = DecodeInt(response, 4);
            return (enabled, bpp);
        }
        return (0, 0);
    }
    
    public async Task EnableChannelAsync(int channel, bool enable, int bpp = 8, CancellationToken cancellationToken = default)
    {
        var body = new List<byte>();
        body.AddRange(EncodeInt(channel));
        body.AddRange(EncodeInt(enable ? 1 : 0));
        body.AddRange(EncodeInt(bpp));
        await SendCommandNoResponseAsync("DtEnable", body.ToArray(), cancellationToken);
    }
    
    public async Task AutoSignalAsync(int channel, CancellationToken cancellationToken = default)
    {
        var body = EncodeInt(channel);
        await SendCommandWithWaitAsync("DtAutoSignal", body, WaitFlagOptics | WaitFlagAuto, cancellationToken);
    }
    
    public async Task ScStopScanAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("ScStopScan", null, cancellationToken);
    }
    
    public async Task SetGuiScanningAsync(bool enable, CancellationToken cancellationToken = default)
    {
        var body = EncodeInt(enable ? 1 : 0);
        await SendCommandNoResponseAsync("GUISetScanning", body, cancellationToken);
    }
    
    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        await EnsureDataChannelAsync(cancellationToken);
        
        foreach (var channel in settings.Channels)
        {
            var enableBody = new List<byte>();
            enableBody.AddRange(EncodeInt(channel));
            enableBody.AddRange(EncodeInt(1));
            enableBody.AddRange(EncodeInt(8));
            await SendCommandNoResponseAsync("DtEnable", enableBody.ToArray(), cancellationToken);
        }
        
        await SetGuiScanningAsync(false, cancellationToken);
        await ScStopScanAsync(cancellationToken);
        
        try
        {
            var right = settings.Right > 0 ? settings.Right : settings.Width;
            var bottom = settings.Bottom > 0 ? settings.Bottom : settings.Height;
            
            var scanBody = new List<byte>();
            scanBody.AddRange(EncodeInt(0));
            scanBody.AddRange(EncodeInt(settings.Width));
            scanBody.AddRange(EncodeInt(settings.Height));
            scanBody.AddRange(EncodeInt(settings.Left));
            scanBody.AddRange(EncodeInt(settings.Top));
            scanBody.AddRange(EncodeInt(right));
            scanBody.AddRange(EncodeInt(bottom));
            scanBody.AddRange(EncodeInt(1));
            
            var scanResult = await SendCommandAsync("ScScanXY", scanBody.ToArray(), cancellationToken);
            if (scanResult.Length >= 4)
            {
                var frameId = DecodeInt(scanResult, 0);
                if (frameId < 0)
                {
                    throw new InvalidOperationException($"ScScanXY failed with error code: {frameId}");
                }
            }
            
            var channelCount = settings.Channels.Length;
            var imageSize = settings.Width * settings.Height;
            
            var imageDataList = await ReadAllImagesFromDataChannelAsync(settings.Channels, imageSize, cancellationToken);
            
            var images = new List<SemImage>();
            for (int i = 0; i < channelCount && i < imageDataList.Count; i++)
            {
                if (imageDataList[i].Length > 0)
                {
                    images.Add(new SemImage(settings.Width, settings.Height, imageDataList[i], settings.Channels[i]));
                }
            }
            
            return images.ToArray();
        }
        finally
        {
            await SetGuiScanningAsync(true, cancellationToken);
        }
    }
    
    private static List<byte[]> ParseFetchImageResponse(byte[] response, int channelCount, int width, int height)
    {
        var results = new List<byte[]>();
        int offset = 0;
        
        for (int i = 0; i < channelCount && offset < response.Length; i++)
        {
            if (offset + 4 > response.Length) break;
            
            var imageSize = BitConverter.ToInt32(response, offset);
            offset += 4;
            
            if (imageSize <= 0)
                imageSize = width * height;
            
            if (offset + imageSize > response.Length)
            {
                var remaining = response.Length - offset;
                var imageData = new byte[remaining];
                Array.Copy(response, offset, imageData, 0, remaining);
                results.Add(imageData);
                break;
            }
            
            var data = new byte[imageSize];
            Array.Copy(response, offset, data, 0, imageSize);
            results.Add(data);
            offset += imageSize;
        }
        
        if (results.Count == 0 && response.Length >= width * height)
        {
            results.Add(response);
        }
        
        return results;
    }
    
    private async Task<List<byte[]>> ReadAllImagesFromDataChannelAsync(int[] channels, int imageSizePerChannel, CancellationToken cancellationToken)
    {
        var imageByChannel = new Dictionary<int, byte[]>();
        var bytesReceivedByChannel = new Dictionary<int, int>();
        
        foreach (var ch in channels)
        {
            imageByChannel[ch] = new byte[imageSizePerChannel];
            bytesReceivedByChannel[ch] = 0;
        }
        
        var timeout = TimeSpan.FromSeconds(_timeoutSeconds * 3);
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var message = await ReadDataChannelMessageAsync(cancellationToken);
            if (message == null)
                break;
            
            var commandName = Encoding.ASCII.GetString(message.Header, 0, CommandNameSize).TrimEnd('\0');
            
            if (commandName == "ScData" && message.Body.Length >= 20)
            {
                var frameId = BitConverter.ToUInt32(message.Body, 0);
                var msgChannel = BitConverter.ToInt32(message.Body, 4);
                var argIndex = BitConverter.ToUInt32(message.Body, 8);
                var argBpp = BitConverter.ToInt32(message.Body, 12);
                var argDataSize = BitConverter.ToUInt32(message.Body, 16);
                
                if (!imageByChannel.ContainsKey(msgChannel))
                    continue;
                    
                if (argBpp != 8)
                    continue;
                
                var buffer = imageByChannel[msgChannel];
                var currentSize = bytesReceivedByChannel[msgChannel];
                
                if (argIndex < currentSize)
                {
                    currentSize = (int)argIndex;
                }
                
                if (argIndex > currentSize)
                    continue;
                
                int dataOffset = 20;
                var copyLen = Math.Min((int)argDataSize, buffer.Length - (int)argIndex);
                
                if (copyLen > 0 && dataOffset + argDataSize <= message.Body.Length)
                {
                    Array.Copy(message.Body, dataOffset, buffer, (int)argIndex, copyLen);
                    bytesReceivedByChannel[msgChannel] = (int)argIndex + copyLen;
                }
            }
            
            bool allComplete = true;
            foreach (var ch in channels)
            {
                if (bytesReceivedByChannel[ch] < imageSizePerChannel)
                {
                    allComplete = false;
                    break;
                }
            }
            if (allComplete)
                break;
        }
        
        var results = new List<byte[]>();
        foreach (var ch in channels)
        {
            results.Add(imageByChannel[ch]);
        }
        
        return results;
    }
    
    private class DataChannelMessage
    {
        public byte[] Header { get; set; } = Array.Empty<byte>();
        public byte[] Body { get; set; } = Array.Empty<byte>();
    }
    
    private async Task<DataChannelMessage?> ReadDataChannelMessageAsync(CancellationToken cancellationToken)
    {
        if (_dataStream == null)
            return null;
        
        try
        {
            var header = new byte[HeaderSize];
            var bytesRead = 0;
            while (bytesRead < HeaderSize)
            {
                var read = await _dataStream.ReadAsync(header.AsMemory(bytesRead, HeaderSize - bytesRead), cancellationToken);
                if (read == 0) return null;
                bytesRead += read;
            }
            
            var bodySize = BitConverter.ToUInt32(header, 16);
            
            byte[] body;
            if (bodySize > 0)
            {
                body = new byte[bodySize];
                bytesRead = 0;
                while (bytesRead < bodySize)
                {
                    var read = await _dataStream.ReadAsync(body.AsMemory(bytesRead, (int)bodySize - bytesRead), cancellationToken);
                    if (read == 0) return null;
                    bytesRead += read;
                }
            }
            else
            {
                body = Array.Empty<byte>();
            }
            
            return new DataChannelMessage { Header = header, Body = body };
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
    {
        var settings = new ScanSettings
        {
            Width = width,
            Height = height,
            Channels = new[] { channel }
        };
        
        var images = await AcquireImagesAsync(settings, cancellationToken);
        return images.Length > 0 ? images[0] : new SemImage(width, height, Array.Empty<byte>(), channel);
    }
    
    public async Task StopScanAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("ScStopScan", null, cancellationToken);
    }
    
    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("GetSpotSize", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return DecodeFloat(response, ref offset);
        }
        return double.NaN;
    }
    
    public async Task SetSpotSizeAsync(double spotSize, CancellationToken cancellationToken = default)
    {
        var body = EncodeFloat(spotSize);
        await SendCommandNoResponseAsync("SetSpotSize", body, cancellationToken);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _dataStream?.Dispose();
            _dataClient?.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
        }
        
        _disposed = true;
    }
}
