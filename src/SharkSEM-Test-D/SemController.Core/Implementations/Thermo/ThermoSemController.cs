using SemController.Core.Interfaces;
using SemController.Core.Models;
using AutoScript.Clients;
using AutoScript.Libraries.SdbMicroscope.Structures;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemController : ISemController
{
    private readonly string _host;
    private readonly int _port;
    private SdbMicroscopeClient? _client;
    private bool _disposed;

    public bool IsConnected => _client != null;

    public ThermoSemController(string host = "localhost", int port = 7520)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _client = new SdbMicroscopeClient();
            _client.Connect(_host, _port);
        }, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _client?.Disconnect();
            _client = null;
        }, cancellationToken);
    }

    public async Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            var service = _client!.Service;
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

    public async Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            var state = _client!.Vacuum.ChamberState;
            return state?.ToLower() switch
            {
                "vacuum" => VacuumStatus.Ready,
                "vented" => VacuumStatus.VacuumOff,
                "pumping" => VacuumStatus.Pumping,
                "venting" => VacuumStatus.Venting,
                _ => VacuumStatus.Error
            };
        }, cancellationToken);
    }

    public async Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return _client!.Vacuum.ChamberPressure.Value;
        }, cancellationToken);
    }

    public async Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return VacuumMode.HighVacuum;
        }, cancellationToken);
    }

    public async Task PumpAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Vacuum.Pump();
        }, cancellationToken);
    }

    public async Task VentAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Vacuum.Vent();
        }, cancellationToken);
    }

    public async Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            try
            {
                var hv = _client!.Beams.ElectronBeam.HighVoltage.Value;
                return hv > 0 ? BeamState.On : BeamState.Off;
            }
            catch
            {
                return BeamState.Unknown;
            }
        }, cancellationToken);
    }

    public async Task BeamOnAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Beams.ElectronBeam.TurnOn();
        }, cancellationToken);
    }

    public async Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = await GetBeamStateAsync(cancellationToken);
            if (state == BeamState.On)
                return true;
            await Task.Delay(500, cancellationToken);
        }
        return false;
    }

    public async Task BeamOffAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Beams.ElectronBeam.TurnOff();
        }, cancellationToken);
    }

    public async Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return _client!.Beams.ElectronBeam.HighVoltage.Value;
        }, cancellationToken);
    }

    public async Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Beams.ElectronBeam.HighVoltage.Value = voltage;
        }, cancellationToken);
    }

    public async Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return _client!.Beams.ElectronBeam.EmissionCurrent.Value;
        }, cancellationToken);
    }

    public async Task<Models.StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            var pos = _client!.Specimen.Stage.CurrentPosition;
            return new Models.StagePosition
            {
                X = (pos.X ?? 0) * 1000.0,
                Y = (pos.Y ?? 0) * 1000.0,
                Z = (pos.Z ?? 0) * 1000.0,
                Rotation = (pos.R ?? 0) * (180.0 / Math.PI),
                TiltX = (pos.T ?? 0) * (180.0 / Math.PI)
            };
        }, cancellationToken);
    }

    public async Task MoveStageAsync(Models.StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            var targetPos = new AutoScript.Libraries.SdbMicroscope.Structures.StagePosition
            {
                X = position.X / 1000.0,
                Y = position.Y / 1000.0,
                Z = (position.Z ?? 0) / 1000.0,
                R = (position.Rotation ?? 0) * (Math.PI / 180.0),
                T = (position.TiltX ?? 0) * (Math.PI / 180.0)
            };
            _client!.Specimen.Stage.AbsoluteMove(targetPos);
        }, cancellationToken);
    }

    public async Task MoveStageRelativeAsync(Models.StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            var deltaPos = new AutoScript.Libraries.SdbMicroscope.Structures.StagePosition
            {
                X = delta.X / 1000.0,
                Y = delta.Y / 1000.0,
                Z = (delta.Z ?? 0) / 1000.0,
                R = (delta.Rotation ?? 0) * (Math.PI / 180.0),
                T = (delta.TiltX ?? 0) * (Math.PI / 180.0)
            };
            _client!.Specimen.Stage.RelativeMove(deltaPos);
        }, cancellationToken);
    }

    public async Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    public async Task StopStageAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Specimen.Stage.Stop();
        }, cancellationToken);
    }

    public async Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new StageLimits
        {
            MinX = -50, MaxX = 50,
            MinY = -50, MaxY = 50,
            MinZ = 0, MaxZ = 50,
            MinRotation = -180, MaxRotation = 180,
            MinTiltX = -10, MaxTiltX = 60
        });
    }

    public async Task CalibrateStageAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Specimen.Stage.Home();
        }, cancellationToken);
    }

    public async Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return _client!.Specimen.Stage.IsHomed;
        }, cancellationToken);
    }

    public async Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return _client!.Beams.ElectronBeam.HorizontalFieldWidth.Value * 1e6;
        }, cancellationToken);
    }

    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Beams.ElectronBeam.HorizontalFieldWidth.Value = viewFieldMicrons / 1e6;
        }, cancellationToken);
    }

    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return _client!.Beams.ElectronBeam.WorkingDistance.Value * 1000.0;
        }, cancellationToken);
    }

    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Beams.ElectronBeam.WorkingDistance.Value = workingDistanceMm / 1000.0;
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
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.AutoFunctions.RunAutoFocus();
        }, cancellationToken);
    }

    public async Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(1);
    }

    public async Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(BlankerMode.Auto);
    }

    public async Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            var image = _client!.Imaging.GrabFrame();
            var semImage = new SemImage
            {
                Width = image.Width,
                Height = image.Height,
                BitsPerPixel = 8,
                Channel = 0,
                Data = new byte[image.Width * image.Height]
            };
            return new[] { semImage };
        }, cancellationToken);
    }

    public async Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
    {
        var images = await AcquireImagesAsync(new ScanSettings { Width = width, Height = height }, cancellationToken);
        return images.FirstOrDefault() ?? new SemImage { Width = width, Height = height, Data = new byte[width * height] };
    }

    public async Task StopScanAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await Task.Run(() =>
        {
            _client!.Imaging.StopAcquisition();
        }, cancellationToken);
    }

    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            return _client!.Beams.ElectronBeam.Scanning.Spot.Value;
        }, cancellationToken);
    }

    private void EnsureConnected()
    {
        if (_client == null)
            throw new InvalidOperationException("Not connected to microscope. Call ConnectAsync first.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Disconnect();
            _client = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
