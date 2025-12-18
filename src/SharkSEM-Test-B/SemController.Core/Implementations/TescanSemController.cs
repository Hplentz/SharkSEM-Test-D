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
    
    private TcpClient? _client;
    private NetworkStream? _stream;
    private TcpClient? _dataClient;
    private NetworkStream? _dataStream;
    private bool _disposed;
    private uint _messageId;
    private bool _dataChannelRegistered;
    
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    
    private const int HeaderSize = 32;
    internal const int CommandNameSizeInternal = 16;
    private const ushort FlagSendResponse = 0x0001;
    
    private const ushort WaitFlagScan = 0x0100;
    private const ushort WaitFlagStage = 0x0200;
    internal const ushort WaitFlagOpticsInternal = 0x0400;
    internal const ushort WaitFlagAutoInternal = 0x0800;
    
    public bool IsConnected => _client?.Connected ?? false;
    
    internal double TimeoutSeconds => _timeoutSeconds;
    
    public string ProtocolVersionString { get; private set; } = "";
    public Version? ProtocolVersion { get; private set; }
    
    private static readonly Dictionary<string, Version> CommandMinVersions = new()
    {
        ["TcpGetVersion"] = new Version(1, 0, 5),
        ["TcpGetModel"] = new Version(3, 2, 20),
        ["TcpGetDevice"] = new Version(2, 0, 3),
        ["TcpGetSWVersion"] = new Version(2, 0, 9),
        ["TcpRegDataPort"] = new Version(1, 0, 0),
        
        ["VacGetStatus"] = new Version(1, 0, 0),
        ["VacGetPressure"] = new Version(1, 0, 0),
        ["VacGetVPMode"] = new Version(1, 0, 0),
        ["VacPump"] = new Version(1, 0, 0),
        ["VacVent"] = new Version(1, 0, 0),
        
        ["HVGetBeam"] = new Version(1, 0, 0),
        ["HVBeamOn"] = new Version(1, 0, 0),
        ["HVBeamOff"] = new Version(1, 0, 0),
        ["HVGetVoltage"] = new Version(1, 0, 0),
        ["HVSetVoltage"] = new Version(1, 0, 0),
        ["HVGetEmission"] = new Version(1, 0, 0),
        
        ["StgGetPosition"] = new Version(1, 0, 0),
        ["StgMoveTo"] = new Version(1, 0, 0),
        ["StgMove"] = new Version(1, 0, 0),
        ["StgIsBusy"] = new Version(1, 0, 0),
        ["StgStop"] = new Version(1, 0, 0),
        ["StgGetLimits"] = new Version(1, 0, 0),
        ["StgCalibrate"] = new Version(1, 0, 0),
        ["StgIsCalibrated"] = new Version(1, 0, 0),
        
        ["GetViewField"] = new Version(1, 0, 0),
        ["SetViewField"] = new Version(1, 0, 0),
        ["GetWD"] = new Version(1, 0, 0),
        ["SetWD"] = new Version(1, 0, 0),
        ["AutoWD"] = new Version(1, 0, 0),
        ["GetSpotSize"] = new Version(1, 0, 0),
        
        ["GetIAbsorbed"] = new Version(1, 0, 0),
        
        ["SMEnumModes"] = new Version(1, 0, 0),
        ["SMGetMode"] = new Version(1, 0, 0),
        ["SMSetMode"] = new Version(1, 0, 0),
        ["SMGetPivotPos"] = new Version(2, 0, 22),
        
        ["ScGetSpeed"] = new Version(1, 0, 0),
        ["ScSetSpeed"] = new Version(1, 0, 0),
        ["ScGetBlanker"] = new Version(1, 0, 0),
        ["ScSetBlanker"] = new Version(1, 0, 0),
        ["ScStopScan"] = new Version(1, 0, 0),
        ["ScScanXY"] = new Version(1, 0, 0),
        ["ScEnumSpeeds"] = new Version(3, 1, 14),
        
        ["GUIGetScanning"] = new Version(1, 0, 5),
        ["GUISetScanning"] = new Version(1, 0, 5),
        ["GUIGetCurrDets"] = new Version(3, 2, 20),
        ["GUIResetLUT"] = new Version(3, 2, 20),
        ["GUISetLiveAS"] = new Version(3, 2, 20),
        
        ["DtEnumDetectors"] = new Version(1, 0, 0),
        ["DtGetChannels"] = new Version(1, 0, 0),
        ["DtGetSelected"] = new Version(1, 0, 0),
        ["DtSelect"] = new Version(1, 0, 0),
        ["DtGetEnabled"] = new Version(1, 0, 11),
        ["DtEnable"] = new Version(1, 0, 0),
        ["DtAutoSignal"] = new Version(1, 0, 11),
        ["DtGetSESuitable"] = new Version(3, 1, 20),
        ["DtStateEnum"] = new Version(3, 1, 1),
        ["DtStateGet"] = new Version(3, 1, 1),
        ["DtStateSet"] = new Version(3, 1, 1),
    };
    
    internal bool CheckVersionSupport(string commandName)
    {
        if (ProtocolVersion == null)
        {
            return true;
        }
        
        if (CommandMinVersions.TryGetValue(commandName, out var minVersion))
        {
            if (ProtocolVersion < minVersion)
            {
                Console.WriteLine($"[Version Check] Command '{commandName}' requires protocol version {minVersion} or later, but current version is {ProtocolVersionString}. Skipping call.");
                return false;
            }
        }
        
        return true;
    }
    
    private async Task FetchProtocolVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendCommandInternalAsync("TcpGetVersion", null, cancellationToken, skipVersionCheck: true);
            if (response.Length > 0)
            {
                int offset = 0;
                ProtocolVersionString = DecodeStringInternal(response, ref offset);
                
                var parts = ProtocolVersionString.Split('.');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0], out var major) &&
                    int.TryParse(parts[1], out var minor) &&
                    int.TryParse(parts[2], out var build))
                {
                    ProtocolVersion = new Version(major, minor, build);
                }
                else if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out major) &&
                    int.TryParse(parts[1], out minor))
                {
                    ProtocolVersion = new Version(major, minor, 0);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Could not retrieve protocol version: {ex.Message}");
        }
    }
    
    public TescanSemStage Stage { get; }
    public TescanSemDetectors Detectors { get; }
    public TescanSemHighVoltage HighVoltage { get; }
    public TescanSemElectronOptics Optics { get; }
    public TescanSemScanning Scanning { get; }
    public TescanSemVacuum Vacuum { get; }
    public TescanSemMisc Misc { get; }
    public TescanSemImageGeometry ImageGeometry { get; }
    
    public TescanSemController(string host, int port = 8300, double timeoutSeconds = 30.0)
    {
        _host = host;
        _port = port;
        _timeoutSeconds = timeoutSeconds;
        
        Stage = new TescanSemStage(this);
        Detectors = new TescanSemDetectors(this);
        HighVoltage = new TescanSemHighVoltage(this);
        Optics = new TescanSemElectronOptics(this);
        Scanning = new TescanSemScanning(this);
        Vacuum = new TescanSemVacuum(this);
        Misc = new TescanSemMisc(this);
        ImageGeometry = new TescanSemImageGeometry(this);
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
        
        await FetchProtocolVersionAsync(cancellationToken);
        
        if (ProtocolVersion != null)
        {
            Console.WriteLine($"Connected to SEM with protocol version {ProtocolVersionString}");
        }
    }
    
    internal async Task EnsureDataChannelInternalAsync(CancellationToken cancellationToken)
    {
        if (_dataClient?.Connected == true && _dataChannelRegistered)
            return;
        
        _dataClient = new TcpClient();
        _dataClient.ReceiveTimeout = (int)(_timeoutSeconds * 1000);
        _dataClient.SendTimeout = (int)(_timeoutSeconds * 1000);
        
        _dataClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0));
        
        var localPort = ((System.Net.IPEndPoint)_dataClient.Client.LocalEndPoint!).Port;
        
        var regBody = EncodeIntInternal(localPort);
        await SendCommandInternalAsync("TcpRegDataPort", regBody, cancellationToken);
        
        await _dataClient.ConnectAsync(_host, _port + 1, cancellationToken);
        _dataStream = _dataClient.GetStream();
        
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
        var cmdLen = Math.Min(cmdBytes.Length, CommandNameSizeInternal - 1);
        Array.Copy(cmdBytes, 0, header, 0, cmdLen);
        
        BitConverter.GetBytes(bodySize).CopyTo(header, 16);
        BitConverter.GetBytes(++_messageId).CopyTo(header, 20);
        BitConverter.GetBytes(flags).CopyTo(header, 24);
        BitConverter.GetBytes(queue).CopyTo(header, 26);
        
        return header;
    }
    
    private static int Pad4(int size) => (size + 3) & ~3;
    
    internal static byte[] EncodeIntInternal(int value)
    {
        return BitConverter.GetBytes(value);
    }
    
    internal static byte[] EncodeUIntInternal(uint value)
    {
        return BitConverter.GetBytes(value);
    }
    
    internal static byte[] EncodeFloatInternal(double value)
    {
        var str = value.ToString("G", InvariantCulture) + '\0';
        var strBytes = Encoding.ASCII.GetBytes(str);
        var paddedSize = Pad4(4 + strBytes.Length);
        var result = new byte[paddedSize];
        BitConverter.GetBytes((uint)strBytes.Length).CopyTo(result, 0);
        Array.Copy(strBytes, 0, result, 4, strBytes.Length);
        return result;
    }
    
    internal static byte[] EncodeStringInternal(string value)
    {
        var str = value + '\0';
        var strBytes = Encoding.ASCII.GetBytes(str);
        var paddedSize = Pad4(4 + strBytes.Length);
        var result = new byte[paddedSize];
        BitConverter.GetBytes((uint)strBytes.Length).CopyTo(result, 0);
        Array.Copy(strBytes, 0, result, 4, strBytes.Length);
        return result;
    }
    
    internal async Task<byte[]> SendCommandInternalAsync(string command, byte[]? body, CancellationToken cancellationToken, bool skipVersionCheck = false)
    {
        if (!skipVersionCheck && !CheckVersionSupport(command))
            return Array.Empty<byte>();
        
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
    
    internal async Task<bool> SendCommandNoResponseInternalAsync(string command, byte[]? body, CancellationToken cancellationToken)
    {
        if (!CheckVersionSupport(command))
            return false;
        
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope");
        
        var bodySize = (uint)(body?.Length ?? 0);
        var header = BuildHeader(command, bodySize, flags: 0);
        
        await _stream.WriteAsync(header, cancellationToken);
        if (body != null && body.Length > 0)
        {
            await _stream.WriteAsync(body, cancellationToken);
        }
        return true;
    }
    
    internal async Task<byte[]> SendCommandWithWaitInternalAsync(string command, byte[]? body, ushort waitFlags, CancellationToken cancellationToken)
    {
        if (!CheckVersionSupport(command))
            return Array.Empty<byte>();
        
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
    
    internal static int DecodeIntInternal(byte[] body, int offset)
    {
        return BitConverter.ToInt32(body, offset);
    }
    
    internal static double DecodeFloatInternal(byte[] body, ref int offset)
    {
        var strLen = BitConverter.ToUInt32(body, offset);
        offset += 4;
        var str = Encoding.ASCII.GetString(body, offset, (int)strLen - 1);
        offset += Pad4((int)strLen);
        return double.Parse(str, NumberStyles.Float, InvariantCulture);
    }
    
    internal static string DecodeStringInternal(byte[] body, ref int offset)
    {
        var strLen = BitConverter.ToUInt32(body, offset);
        offset += 4;
        var str = Encoding.ASCII.GetString(body, offset, (int)strLen - 1);
        offset += Pad4((int)strLen);
        return str;
    }
    
    internal class DataChannelMessage
    {
        public byte[] Header { get; set; } = Array.Empty<byte>();
        public byte[] Body { get; set; } = Array.Empty<byte>();
    }
    
    internal async Task<DataChannelMessage?> ReadDataChannelMessageInternalAsync(CancellationToken cancellationToken)
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
    
    #region ISemController Backward Compatibility - Delegates to sub-classes
    
    public Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
        => Misc.GetMicroscopeInfoAsync(cancellationToken);
    
    public Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default)
        => Vacuum.GetStatusAsync(cancellationToken);
    
    public Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
        => Vacuum.GetPressureAsync(gauge, cancellationToken);
    
    public Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default)
        => Vacuum.GetModeAsync(cancellationToken);
    
    public Task PumpAsync(CancellationToken cancellationToken = default)
        => Vacuum.PumpAsync(cancellationToken);
    
    public Task VentAsync(CancellationToken cancellationToken = default)
        => Vacuum.VentAsync(cancellationToken);
    
    public Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
        => HighVoltage.GetBeamStateAsync(cancellationToken);
    
    public Task BeamOnAsync(CancellationToken cancellationToken = default)
        => HighVoltage.BeamOnAsync(cancellationToken);
    
    public Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
        => HighVoltage.WaitForBeamOnAsync(timeoutMs, cancellationToken);
    
    public Task BeamOffAsync(CancellationToken cancellationToken = default)
        => HighVoltage.BeamOffAsync(cancellationToken);
    
    public Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
        => HighVoltage.GetVoltageAsync(cancellationToken);
    
    public Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => HighVoltage.SetVoltageAsync(voltage, waitForCompletion, cancellationToken);
    
    public Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
        => HighVoltage.GetEmissionCurrentAsync(cancellationToken);
    
    public Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default)
        => Stage.GetPositionAsync(cancellationToken);
    
    public Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => Stage.MoveToAsync(position, waitForCompletion, cancellationToken);
    
    public Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => Stage.MoveRelativeAsync(delta, waitForCompletion, cancellationToken);
    
    public Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default)
        => Stage.IsMovingAsync(cancellationToken);
    
    public Task StopStageAsync(CancellationToken cancellationToken = default)
        => Stage.StopAsync(cancellationToken);
    
    public Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default)
        => Stage.GetLimitsAsync(cancellationToken);
    
    public Task CalibrateStageAsync(CancellationToken cancellationToken = default)
        => Stage.CalibrateAsync(cancellationToken);
    
    public Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default)
        => Stage.IsCallibratedAsync(cancellationToken);
    
    public Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
        => Optics.GetViewFieldAsync(cancellationToken);
    
    public Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
        => Optics.SetViewFieldAsync(viewFieldMicrons, cancellationToken);
    
    public Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
        => Optics.GetWorkingDistanceAsync(cancellationToken);
    
    public Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
        => Optics.SetWorkingDistanceAsync(workingDistanceMm, cancellationToken);
    
    public Task<double> GetFocusAsync(CancellationToken cancellationToken = default)
        => Optics.GetFocusAsync(cancellationToken);
    
    public Task SetFocusAsync(double focus, CancellationToken cancellationToken = default)
        => Optics.SetFocusAsync(focus, cancellationToken);
    
    public Task AutoFocusAsync(CancellationToken cancellationToken = default)
        => Optics.AutoFocusAsync(cancellationToken);
    
    public Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default)
        => Scanning.GetSpeedAsync(cancellationToken);
    
    public Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default)
        => Scanning.SetSpeedAsync(speed, cancellationToken);
    
    public Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
        => Scanning.GetBlankerModeAsync(cancellationToken);
    
    public Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
        => Scanning.SetBlankerModeAsync(mode, cancellationToken);
    
    public Task<string> EnumDetectorsAsync(CancellationToken cancellationToken = default)
        => Detectors.EnumDetectorsAsync(cancellationToken);
    
    public Task<int> GetChannelCountAsync(CancellationToken cancellationToken = default)
        => Detectors.GetChannelCountAsync(cancellationToken);
    
    public Task<int> GetSelectedDetectorAsync(int channel, CancellationToken cancellationToken = default)
        => Detectors.GetSelectedDetectorAsync(channel, cancellationToken);
    
    public Task SelectDetectorAsync(int channel, int detector, CancellationToken cancellationToken = default)
        => Detectors.SelectDetectorAsync(channel, detector, cancellationToken);
    
    public Task<(int enabled, int bpp)> GetChannelEnabledAsync(int channel, CancellationToken cancellationToken = default)
        => Detectors.GetChannelEnabledAsync(channel, cancellationToken);
    
    public Task EnableChannelAsync(int channel, bool enable, int bpp = 8, CancellationToken cancellationToken = default)
        => Detectors.EnableChannelAsync(channel, enable, bpp, cancellationToken);
    
    public Task AutoSignalAsync(int channel, CancellationToken cancellationToken = default)
        => Detectors.AutoSignalAsync(channel, cancellationToken);
    
    public Task ScStopScanAsync(CancellationToken cancellationToken = default)
        => Scanning.StopScanAsync(cancellationToken);
    
    public Task SetGuiScanningAsync(bool enable, CancellationToken cancellationToken = default)
        => Scanning.SetGuiScanningAsync(enable, cancellationToken);
    
    public Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
        => Scanning.AcquireImagesAsync(settings, cancellationToken);
    
    public Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
        => Scanning.AcquireSingleImageAsync(channel, width, height, cancellationToken);
    
    public Task StopScanAsync(CancellationToken cancellationToken = default)
        => Scanning.StopScanAsync(cancellationToken);
    
    public Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
        => Optics.GetSpotSizeAsync(cancellationToken);
    
    #endregion
    
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
