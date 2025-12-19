using SemController.Core.Implementations;
using SemController.Core.Models;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace SemController.UI;

public partial class MainForm : Form
{
    private TescanSemController? _sem;
    private bool _isConnected = false;
    private bool _isUpdating = false;
    private List<ScanSpeed> _scanSpeeds = new();
    private List<ScanningMode> _scanningModes = new();
    private List<ImageGeometry> _geometries = new();
    private List<int> _pcIndexes = new();
    private int _imageShiftIndex = -1;
    private int _imageRotationIndex = -1;

    public MainForm()
    {
        InitializeComponent();
        SetControlsEnabled(false);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await ConnectAsync();
    }

    private void SetControlsEnabled(bool enabled)
    {
        btnRefresh.Enabled = enabled;
        grpMicroscopeInfo.Enabled = enabled;
        grpVacuum.Enabled = enabled;
        grpDetector.Enabled = enabled;
        grpBeam.Enabled = enabled;
        grpScanningModes.Enabled = enabled;
        grpGeometries.Enabled = enabled;
        grpStage.Enabled = enabled;
        grpViewField.Enabled = enabled;
        grpScanning.Enabled = enabled;
    }

    private async Task ConnectAsync()
    {
        try
        {
            lblConnectionStatus.Text = "Status: Connecting...";
            btnConnect.Enabled = false;
            Application.DoEvents();

            _sem = new TescanSemController("127.0.0.1");
            await _sem.ConnectAsync();

            _isConnected = true;
            btnConnect.Text = "Disconnect";
            lblConnectionStatus.Text = "Status: Connected";
            SetControlsEnabled(true);

            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            lblConnectionStatus.Text = $"Status: Connection failed - {ex.Message}";
            _isConnected = false;
        }
        finally
        {
            btnConnect.Enabled = true;
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            if (_sem != null)
            {
                await _sem.DisconnectAsync();
                _sem.Dispose();
                _sem = null;
            }
        }
        catch { }

        _isConnected = false;
        btnConnect.Text = "Connect";
        lblConnectionStatus.Text = "Status: Disconnected";
        SetControlsEnabled(false);
    }

    private async Task RefreshAllAsync()
    {
        if (_sem == null || !_isConnected) return;

        _isUpdating = true;
        try
        {
            await RefreshMicroscopeInfoAsync();
            await RefreshVacuumAsync();
            await RefreshDetectorsAsync();
            await RefreshBeamAsync();
            await RefreshScanningModesAsync();
            await RefreshGeometriesAsync();
            await RefreshStageAsync();
            await RefreshViewFieldAsync();
            await RefreshScanSpeedsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refreshing data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task RefreshMicroscopeInfoAsync()
    {
        if (_sem == null) return;

        try
        {
            var info = await _sem.GetMicroscopeInfoAsync();
            lblManufacturer.Text = $"Manufacturer: {info.Manufacturer}";
            lblModel.Text = $"Model: {info.Model}";
            lblSerial.Text = $"Serial: {info.SerialNumber}";
            lblSoftwareVersion.Text = $"Software: {info.SoftwareVersion}";
            lblProtocolVersion.Text = $"Protocol: {_sem.ProtocolVersionString}";
        }
        catch
        {
            lblManufacturer.Text = "Manufacturer: (error)";
        }
    }

    private async Task RefreshVacuumAsync()
    {
        if (_sem == null) return;

        try
        {
            var status = await _sem.GetVacuumStatusAsync();
            var pressure = await _sem.GetVacuumPressureAsync();
            lblVacuumStatus.Text = $"Status: {status}";
            lblChamberPressure.Text = $"Pressure: {pressure:E2} Pa";
        }
        catch
        {
            lblVacuumStatus.Text = "Status: (error)";
            lblChamberPressure.Text = "Pressure: -- Pa";
        }
    }

    private async Task RefreshDetectorsAsync()
    {
        if (_sem == null) return;

        try
        {
            cboDetectors.Items.Clear();
            var detectorsStr = await _sem.Detectors.EnumDetectorsAsync();
            if (!string.IsNullOrEmpty(detectorsStr))
            {
                var detectors = detectorsStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < detectors.Length; i++)
                {
                    cboDetectors.Items.Add($"{i}: {detectors[i].Trim()}");
                }

                var selected = await _sem.Detectors.GetSelectedDetectorAsync(0);
                if (selected >= 0 && selected < cboDetectors.Items.Count)
                {
                    cboDetectors.SelectedIndex = selected;
                }
            }
        }
        catch { }
    }

    private async Task RefreshBeamAsync()
    {
        if (_sem == null) return;

        try
        {
            var beamOn = await _sem.HighVoltage.IsBeamOnAsync();
            lblBeamState.Text = $"Beam State: {(beamOn ? "ON" : "OFF")}";

            var hv = await _sem.GetHighVoltageAsync();
            numHighVoltage.Value = (decimal)hv;

            await RefreshPCIndexesAsync();

            var beamCurrent = await _sem.Optics.GetBeamCurrentAsync();
            numBeamCurrent.Value = (decimal)beamCurrent;

            try
            {
                var absorbedCurrent = await _sem.Optics.GetAbsorbedCurrentAsync();
                lblAbsorbedCurrent.Text = absorbedCurrent >= 1000
                    ? $"Absorbed Current: {absorbedCurrent / 1000:F2} nA"
                    : $"Absorbed Current: {absorbedCurrent:F2} pA";
            }
            catch
            {
                lblAbsorbedCurrent.Text = "Absorbed Current: -- pA";
            }

            var wd = await _sem.GetWorkingDistanceAsync();
            numWorkingDistance.Value = (decimal)wd;
        }
        catch { }
    }

    private async Task RefreshPCIndexesAsync()
    {
        if (_sem == null) return;

        try
        {
            cboPCIndexes.Items.Clear();
            _pcIndexes.Clear();
            var pcStr = await _sem.Optics.EnumPCIndexesAsync();
            if (!string.IsNullOrEmpty(pcStr))
            {
                var regex = new Regex(@"pc\.(\d+)\.current=([0-9.eE+-]+)", RegexOptions.IgnoreCase);
                var matches = regex.Matches(pcStr);
                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        var index = int.Parse(match.Groups[1].Value);
                        var current = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                        var display = current >= 1000 ? $"{current / 1000:F2} nA" : $"{current:F2} pA";
                        _pcIndexes.Add(index);
                        cboPCIndexes.Items.Add($"{index}: {display}");
                    }
                }
                
                var currentPCIndex = await _sem.Optics.GetPCIndexAsync();
                for (int i = 0; i < _pcIndexes.Count; i++)
                {
                    if (_pcIndexes[i] == currentPCIndex)
                    {
                        cboPCIndexes.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        catch { }
    }

    private async Task RefreshScanningModesAsync()
    {
        if (_sem == null) return;

        try
        {
            cboScanningModes.Items.Clear();
            _scanningModes = await _sem.Optics.EnumScanningModesAsync();
            foreach (var mode in _scanningModes)
            {
                cboScanningModes.Items.Add($"{mode.Index}: {mode.Name}");
            }

            var currentMode = await _sem.Optics.GetScanningModeAsync();
            for (int i = 0; i < _scanningModes.Count; i++)
            {
                if (_scanningModes[i].Index == currentMode)
                {
                    cboScanningModes.SelectedIndex = i;
                    break;
                }
            }

            var (result, pivot) = await _sem.Optics.GetPivotPositionAsync();
            if (result == 0)
            {
                lblPivotPosition.Text = $"Pivot Position: {pivot:F3} mm";
            }
            else
            {
                lblPivotPosition.Text = "Pivot Position: --";
            }
        }
        catch { }
    }

    private async Task RefreshGeometriesAsync()
    {
        if (_sem == null) return;

        try
        {
            _geometries = await _sem.ImageGeometry.EnumGeometriesAsync();

            _imageShiftIndex = _geometries.FirstOrDefault(g => g.Name.Contains("Image Shift", StringComparison.OrdinalIgnoreCase))?.Index ?? -1;
            _imageRotationIndex = _geometries.FirstOrDefault(g => g.Name.Contains("Image Rotation", StringComparison.OrdinalIgnoreCase))?.Index ?? -1;

            if (_imageShiftIndex >= 0)
            {
                var (x, y) = await _sem.ImageGeometry.GetGeometryAsync(_imageShiftIndex);
                numImageShiftX.Value = (decimal)x;
                numImageShiftY.Value = (decimal)y;

                var (minX, maxX, minY, maxY) = await _sem.ImageGeometry.GetGeomLimitsAsync(_imageShiftIndex);
                lblImageShiftRangeX.Text = $"X range: {minX:F4} to {maxX:F4}";
                lblImageShiftRangeY.Text = $"Y range: {minY:F4} to {maxY:F4}";
            }

            if (_imageRotationIndex >= 0)
            {
                var (x, _) = await _sem.ImageGeometry.GetGeometryAsync(_imageRotationIndex);
                numImageRotation.Value = (decimal)x;

                var (minX, maxX, _, _) = await _sem.ImageGeometry.GetGeomLimitsAsync(_imageRotationIndex);
                lblImageRotRange.Text = $"Range: {minX:F0} to {maxX:F0} deg";
            }
        }
        catch { }
    }

    private async Task RefreshStageAsync()
    {
        if (_sem == null) return;

        try
        {
            var calibrated = await _sem.Stage.IsCallibratedAsync();
            var busy = await _sem.Stage.IsMovingAsync();
            lblStageCalibrated.Text = $"Calibrated: {calibrated}";
            lblStageBusy.Text = $"Busy: {busy}";

            var pos = await _sem.GetStagePositionAsync();
            numStageX.Value = (decimal)pos.X;
            numStageY.Value = (decimal)pos.Y;
            numStageZ.Value = (decimal)pos.Z;
            numStageR.Value = (decimal)pos.Rotation;
            numStageTx.Value = (decimal)pos.TiltX;
        }
        catch { }
    }

    private async Task RefreshViewFieldAsync()
    {
        if (_sem == null) return;

        try
        {
            var vf = await _sem.GetViewFieldAsync();
            numViewField.Value = (decimal)vf;
        }
        catch { }
    }

    private async Task RefreshScanSpeedsAsync()
    {
        if (_sem == null) return;

        try
        {
            cboScanSpeeds.Items.Clear();
            _scanSpeeds = await _sem.Scanning.EnumSpeedsAsync();
            foreach (var speed in _scanSpeeds)
            {
                cboScanSpeeds.Items.Add($"Speed {speed.Index}");
            }

            var currentSpeed = await _sem.GetScanSpeedAsync();
            for (int i = 0; i < _scanSpeeds.Count; i++)
            {
                if (_scanSpeeds[i].Index == currentSpeed)
                {
                    cboScanSpeeds.SelectedIndex = i;
                    lblDwellTime.Text = $"{_scanSpeeds[i].DwellTime:F2} µs/pixel";
                    break;
                }
            }
        }
        catch { }
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_isConnected)
        {
            await DisconnectAsync();
        }
        else
        {
            await ConnectAsync();
        }
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void BtnAcquireImage_Click(object? sender, EventArgs e)
    {
        if (_sem == null || !_isConnected) return;

        try
        {
            btnAcquireImage.Enabled = false;
            btnAcquireImage.Text = "Acquiring...";

            var image = await _sem.AcquireSingleImageAsync(0, 1024, 768);
            if (image != null && image.Data != null && image.Data.Length > 0)
            {
                var bitmap = CreateBitmapFromImageData(image);
                picImage.Image?.Dispose();
                picImage.Image = bitmap;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Image acquisition failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnAcquireImage.Enabled = true;
            btnAcquireImage.Text = "Acquire Image";
        }
    }

    private static Bitmap CreateBitmapFromImageData(SemImage image)
    {
        var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed);

        var palette = bitmap.Palette;
        for (int i = 0; i < 256; i++)
        {
            palette.Entries[i] = Color.FromArgb(i, i, i);
        }
        bitmap.Palette = palette;

        var bmpData = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
        try
        {
            for (int y = 0; y < image.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(image.Data, y * image.Width, bmpData.Scan0 + y * bmpData.Stride, image.Width);
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }

        return bitmap;
    }

    private async void CboDetectors_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null || cboDetectors.SelectedIndex < 0) return;
        await _sem.Detectors.SelectDetectorAsync(0, cboDetectors.SelectedIndex);
    }

    private async void CboPCIndexes_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null || cboPCIndexes.SelectedIndex < 0) return;
        if (cboPCIndexes.SelectedIndex < _pcIndexes.Count)
        {
            await _sem.Optics.SetPCIndexAsync(_pcIndexes[cboPCIndexes.SelectedIndex]);
        }
    }

    private async void CboScanningModes_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null || cboScanningModes.SelectedIndex < 0) return;
        if (cboScanningModes.SelectedIndex < _scanningModes.Count)
        {
            await _sem.Optics.SetScanningModeAsync(_scanningModes[cboScanningModes.SelectedIndex].Index);
        }
    }

    private async void CboScanSpeeds_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null || cboScanSpeeds.SelectedIndex < 0) return;
        if (cboScanSpeeds.SelectedIndex < _scanSpeeds.Count)
        {
            await _sem.SetScanSpeedAsync(_scanSpeeds[cboScanSpeeds.SelectedIndex].Index);
            lblDwellTime.Text = $"{_scanSpeeds[cboScanSpeeds.SelectedIndex].DwellTime:F2} µs/pixel";
        }
    }

    private async void NumHighVoltage_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.SetHighVoltageAsync((double)numHighVoltage.Value);
    }

    private async void NumBeamCurrent_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.Optics.SetBeamCurrentAsync((double)numBeamCurrent.Value);
    }

    private async void NumWorkingDistance_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.SetWorkingDistanceAsync((double)numWorkingDistance.Value);
    }

    private async void NumImageShiftX_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null || _imageShiftIndex < 0) return;
        await _sem.ImageGeometry.SetGeometryAsync(_imageShiftIndex, (double)numImageShiftX.Value, (double)numImageShiftY.Value);
    }

    private async void NumImageShiftY_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null || _imageShiftIndex < 0) return;
        await _sem.ImageGeometry.SetGeometryAsync(_imageShiftIndex, (double)numImageShiftX.Value, (double)numImageShiftY.Value);
    }

    private async void NumImageRotation_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null || _imageRotationIndex < 0) return;
        await _sem.ImageGeometry.SetGeometryAsync(_imageRotationIndex, (double)numImageRotation.Value, 0);
    }

    private async void NumStageX_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.MoveStageAsync(x: (double)numStageX.Value);
    }

    private async void NumStageY_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.MoveStageAsync(y: (double)numStageY.Value);
    }

    private async void NumStageZ_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.MoveStageAsync(z: (double)numStageZ.Value);
    }

    private async void NumStageR_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.MoveStageAsync(rotation: (double)numStageR.Value);
    }

    private async void NumStageTx_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.MoveStageAsync(tiltX: (double)numStageTx.Value);
    }

    private async void NumViewField_Leave(object? sender, EventArgs e)
    {
        if (_isUpdating || _sem == null) return;
        await _sem.SetViewFieldAsync((double)numViewField.Value);
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        await DisconnectAsync();
    }
}
