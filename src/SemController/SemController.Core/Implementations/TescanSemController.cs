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
    private bool _disposed;
    
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
    }
    
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _stream?.Close();
        _client?.Close();
        _stream = null;
        _client = null;
        return Task.CompletedTask;
    }
    
    private async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope");
        
        var request = Encoding.ASCII.GetBytes(command + "\r\n");
        await _stream.WriteAsync(request, cancellationToken);
        
        var buffer = new byte[8192];
        var response = new StringBuilder();
        
        do
        {
            var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;
            response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
        } while (_stream.DataAvailable);
        
        return response.ToString().Trim();
    }
    
    private async Task SendCommandNoResponseAsync(string command, CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope");
        
        var request = Encoding.ASCII.GetBytes(command + "\r\n");
        await _stream.WriteAsync(request, cancellationToken);
    }
    
    public async Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        var info = new MicroscopeInfo { Manufacturer = "TESCAN" };
        
        try
        {
            var model = await SendCommandAsync("TcpGetModel", cancellationToken);
            info.Model = model;
        }
        catch { }
        
        try
        {
            var serial = await SendCommandAsync("TcpGetDevice", cancellationToken);
            info.SerialNumber = serial;
        }
        catch { }
        
        try
        {
            var swVersion = await SendCommandAsync("TcpGetSWVersion", cancellationToken);
            info.SoftwareVersion = swVersion;
        }
        catch { }
        
        try
        {
            var protocolVersion = await SendCommandAsync("TcpGetVersion", cancellationToken);
            info.ProtocolVersion = protocolVersion;
        }
        catch { }
        
        return info;
    }
    
    public async Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("VacGetStatus", cancellationToken);
        if (int.TryParse(response, out var status))
            return (VacuumStatus)status;
        return VacuumStatus.Error;
    }
    
    public async Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync($"VacGetPressure {(int)gauge}", cancellationToken);
        if (double.TryParse(response, out var pressure))
            return pressure;
        return double.NaN;
    }
    
    public async Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("VacGetVPMode", cancellationToken);
        if (int.TryParse(response, out var mode))
            return (VacuumMode)mode;
        return VacuumMode.Unknown;
    }
    
    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("VacPump", cancellationToken);
    }
    
    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("VacVent", cancellationToken);
    }
    
    public async Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("HVGetBeam", cancellationToken);
        if (int.TryParse(response, out var state))
            return (BeamState)state;
        return BeamState.Unknown;
    }
    
    public async Task BeamOnAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("HVBeamOn", cancellationToken);
    }
    
    public async Task BeamOffAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("HVBeamOff", cancellationToken);
    }
    
    public async Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("HVGetVoltage", cancellationToken);
        if (double.TryParse(response, out var voltage))
            return voltage;
        return double.NaN;
    }
    
    public async Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var asyncFlag = waitForCompletion ? 0 : 1;
        await SendCommandNoResponseAsync($"HVSetVoltage {voltage} {asyncFlag}", cancellationToken);
    }
    
    public async Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("HVGetEmission", cancellationToken);
        if (double.TryParse(response, out var emission))
            return emission;
        return double.NaN;
    }
    
    public async Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("StgGetPosition", cancellationToken);
        var parts = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var position = new StagePosition();
        if (parts.Length >= 1 && double.TryParse(parts[0], out var x)) position.X = x;
        if (parts.Length >= 2 && double.TryParse(parts[1], out var y)) position.Y = y;
        if (parts.Length >= 3 && double.TryParse(parts[2], out var z)) position.Z = z;
        if (parts.Length >= 4 && double.TryParse(parts[3], out var rot)) position.Rotation = rot;
        if (parts.Length >= 5 && double.TryParse(parts[4], out var tiltX)) position.TiltX = tiltX;
        if (parts.Length >= 6 && double.TryParse(parts[5], out var tiltY)) position.TiltY = tiltY;
        
        return position;
    }
    
    public async Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var command = position.TiltY.HasValue
            ? $"StgMoveTo {position.X} {position.Y} {position.Z} {position.Rotation} {position.TiltX} {position.TiltY}"
            : $"StgMoveTo {position.X} {position.Y} {position.Z} {position.Rotation} {position.TiltX}";
        
        await SendCommandNoResponseAsync(command, cancellationToken);
        
        if (waitForCompletion)
        {
            while (await IsStageMovingAsync(cancellationToken))
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }
    
    public async Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var command = delta.TiltY.HasValue
            ? $"StgMove {delta.X} {delta.Y} {delta.Z} {delta.Rotation} {delta.TiltX} {delta.TiltY}"
            : $"StgMove {delta.X} {delta.Y} {delta.Z} {delta.Rotation} {delta.TiltX}";
        
        await SendCommandNoResponseAsync(command, cancellationToken);
        
        if (waitForCompletion)
        {
            while (await IsStageMovingAsync(cancellationToken))
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }
    
    public async Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("StgIsBusy", cancellationToken);
        return response == "1";
    }
    
    public async Task StopStageAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("StgStop", cancellationToken);
    }
    
    public async Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("StgGetLimits 0", cancellationToken);
        var parts = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var limits = new StageLimits();
        if (parts.Length >= 2)
        {
            double.TryParse(parts[0], out var minX);
            double.TryParse(parts[1], out var maxX);
            limits.MinX = minX;
            limits.MaxX = maxX;
        }
        if (parts.Length >= 4)
        {
            double.TryParse(parts[2], out var minY);
            double.TryParse(parts[3], out var maxY);
            limits.MinY = minY;
            limits.MaxY = maxY;
        }
        if (parts.Length >= 6)
        {
            double.TryParse(parts[4], out var minZ);
            double.TryParse(parts[5], out var maxZ);
            limits.MinZ = minZ;
            limits.MaxZ = maxZ;
        }
        if (parts.Length >= 8)
        {
            double.TryParse(parts[6], out var minRot);
            double.TryParse(parts[7], out var maxRot);
            limits.MinRotation = minRot;
            limits.MaxRotation = maxRot;
        }
        if (parts.Length >= 10)
        {
            double.TryParse(parts[8], out var minTiltX);
            double.TryParse(parts[9], out var maxTiltX);
            limits.MinTiltX = minTiltX;
            limits.MaxTiltX = maxTiltX;
        }
        if (parts.Length >= 12)
        {
            double.TryParse(parts[10], out var minTiltY);
            double.TryParse(parts[11], out var maxTiltY);
            limits.MinTiltY = minTiltY;
            limits.MaxTiltY = maxTiltY;
        }
        
        return limits;
    }
    
    public async Task CalibrateStageAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync("StgCalibrate", cancellationToken);
    }
    
    public async Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("StgIsCalibrated", cancellationToken);
        return response == "1";
    }
    
    public async Task<double> GetMagnificationAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("OpGetViewField", cancellationToken);
        if (double.TryParse(response, out var viewField))
            return 1.0 / viewField;
        return double.NaN;
    }
    
    public async Task SetMagnificationAsync(double magnification, CancellationToken cancellationToken = default)
    {
        var viewField = 1.0 / magnification;
        await SendCommandNoResponseAsync($"OpSetViewField {viewField}", cancellationToken);
    }
    
    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("OpGetWD", cancellationToken);
        if (double.TryParse(response, out var wd))
            return wd;
        return double.NaN;
    }
    
    public async Task SetWorkingDistanceAsync(double workingDistance, CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync($"OpSetWD {workingDistance}", cancellationToken);
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
        await SendCommandNoResponseAsync("AutoWD 0", cancellationToken);
    }
    
    public async Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("ScGetSpeed", cancellationToken);
        if (int.TryParse(response, out var speed))
            return speed;
        return 0;
    }
    
    public async Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync($"ScSetSpeed {speed}", cancellationToken);
    }
    
    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("ScGetBlanker", cancellationToken);
        if (int.TryParse(response, out var mode))
            return (BlankerMode)mode;
        return BlankerMode.Off;
    }
    
    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync($"ScSetBlanker {(int)mode}", cancellationToken);
    }
    
    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        var channelList = string.Join(" ", settings.Channels);
        
        var right = settings.Right > 0 ? settings.Right : settings.Width;
        var bottom = settings.Bottom > 0 ? settings.Bottom : settings.Height;
        
        await SendCommandAsync(
            $"ScScanXY 0 {settings.Width} {settings.Height} {settings.Left} {settings.Top} {right} {bottom} 1 {(uint)(settings.DwellTimeUs * 1000)}",
            cancellationToken);
        
        var response = await SendCommandAsync(
            $"FetchImage {channelList} {settings.Width} {settings.Height}",
            cancellationToken);
        
        var images = new List<SemImage>();
        var imageSize = settings.Width * settings.Height;
        var responseBytes = Encoding.ASCII.GetBytes(response);
        
        for (int i = 0; i < settings.Channels.Length; i++)
        {
            var start = i * imageSize;
            if (start + imageSize <= responseBytes.Length)
            {
                var imageData = new byte[imageSize];
                Array.Copy(responseBytes, start, imageData, 0, imageSize);
                images.Add(new SemImage(settings.Width, settings.Height, imageData, settings.Channels[i]));
            }
        }
        
        return images.ToArray();
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
        await SendCommandNoResponseAsync("ScStopScan", cancellationToken);
    }
    
    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync("OpGetSpotSize", cancellationToken);
        if (double.TryParse(response, out var spotSize))
            return spotSize;
        return double.NaN;
    }
    
    public async Task SetSpotSizeAsync(double spotSize, CancellationToken cancellationToken = default)
    {
        await SendCommandNoResponseAsync($"OpSetSpotSize {spotSize}", cancellationToken);
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
            _stream?.Dispose();
            _client?.Dispose();
        }
        
        _disposed = true;
    }
}
