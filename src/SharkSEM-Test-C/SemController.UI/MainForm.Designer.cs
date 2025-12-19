namespace SemController.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Connection Group
        grpConnection = new GroupBox();
        btnConnect = new Button();
        btnRefresh = new Button();
        lblConnectionStatus = new Label();

        grpConnection.SuspendLayout();
        grpConnection.Controls.Add(btnConnect);
        grpConnection.Controls.Add(btnRefresh);
        grpConnection.Controls.Add(lblConnectionStatus);
        grpConnection.Location = new Point(12, 12);
        grpConnection.Name = "grpConnection";
        grpConnection.Size = new Size(300, 80);
        grpConnection.Text = "Connection";

        btnConnect.Location = new Point(10, 25);
        btnConnect.Size = new Size(80, 28);
        btnConnect.Text = "Connect";
        btnConnect.Click += BtnConnect_Click;

        btnRefresh.Location = new Point(100, 25);
        btnRefresh.Size = new Size(80, 28);
        btnRefresh.Text = "Refresh";
        btnRefresh.Click += BtnRefresh_Click;

        lblConnectionStatus.Location = new Point(10, 56);
        lblConnectionStatus.Size = new Size(280, 18);
        lblConnectionStatus.Text = "Status: Disconnected";

        grpConnection.ResumeLayout(false);

        // Microscope Info Group
        grpMicroscopeInfo = new GroupBox();
        lblManufacturer = new Label();
        lblModel = new Label();
        lblSerial = new Label();
        lblSoftwareVersion = new Label();
        lblProtocolVersion = new Label();

        grpMicroscopeInfo.SuspendLayout();
        grpMicroscopeInfo.Location = new Point(12, 98);
        grpMicroscopeInfo.Size = new Size(300, 130);
        grpMicroscopeInfo.Text = "Microscope Info";

        lblManufacturer.Location = new Point(10, 22);
        lblManufacturer.Size = new Size(280, 18);
        lblManufacturer.Text = "Manufacturer: --";

        lblModel.Location = new Point(10, 42);
        lblModel.Size = new Size(280, 18);
        lblModel.Text = "Model: --";

        lblSerial.Location = new Point(10, 62);
        lblSerial.Size = new Size(280, 18);
        lblSerial.Text = "Serial: --";

        lblSoftwareVersion.Location = new Point(10, 82);
        lblSoftwareVersion.Size = new Size(280, 18);
        lblSoftwareVersion.Text = "Software: --";

        lblProtocolVersion.Location = new Point(10, 102);
        lblProtocolVersion.Size = new Size(280, 18);
        lblProtocolVersion.Text = "Protocol: --";

        grpMicroscopeInfo.Controls.Add(lblManufacturer);
        grpMicroscopeInfo.Controls.Add(lblModel);
        grpMicroscopeInfo.Controls.Add(lblSerial);
        grpMicroscopeInfo.Controls.Add(lblSoftwareVersion);
        grpMicroscopeInfo.Controls.Add(lblProtocolVersion);
        grpMicroscopeInfo.ResumeLayout(false);

        // Vacuum Group
        grpVacuum = new GroupBox();
        lblVacuumStatus = new Label();
        lblChamberPressure = new Label();

        grpVacuum.SuspendLayout();
        grpVacuum.Location = new Point(12, 234);
        grpVacuum.Size = new Size(300, 70);
        grpVacuum.Text = "Vacuum";

        lblVacuumStatus.Location = new Point(10, 22);
        lblVacuumStatus.Size = new Size(280, 18);
        lblVacuumStatus.Text = "Status: --";

        lblChamberPressure.Location = new Point(10, 44);
        lblChamberPressure.Size = new Size(280, 18);
        lblChamberPressure.Text = "Pressure: -- Pa";

        grpVacuum.Controls.Add(lblVacuumStatus);
        grpVacuum.Controls.Add(lblChamberPressure);
        grpVacuum.ResumeLayout(false);

        // Detector Group
        grpDetector = new GroupBox();
        cboDetectors = new ComboBox();

        grpDetector.SuspendLayout();
        grpDetector.Location = new Point(12, 310);
        grpDetector.Size = new Size(300, 55);
        grpDetector.Text = "Detector Selection";

        cboDetectors.Location = new Point(10, 22);
        cboDetectors.Size = new Size(280, 23);
        cboDetectors.DropDownStyle = ComboBoxStyle.DropDownList;
        cboDetectors.SelectedIndexChanged += CboDetectors_SelectedIndexChanged;

        grpDetector.Controls.Add(cboDetectors);
        grpDetector.ResumeLayout(false);

        // Beam Group
        grpBeam = new GroupBox();
        lblBeamState = new Label();
        var lblHVLabel = new Label();
        numHighVoltage = new NumericUpDown();
        var lblHVUnit = new Label();
        var lblPCLabel = new Label();
        cboPCIndexes = new ComboBox();
        var lblBeamCurrentLabel = new Label();
        numBeamCurrent = new NumericUpDown();
        var lblBeamCurrentUnit = new Label();
        lblAbsorbedCurrent = new Label();
        var lblWDLabel = new Label();
        numWorkingDistance = new NumericUpDown();
        var lblWDUnit = new Label();

        grpBeam.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numHighVoltage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numBeamCurrent).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numWorkingDistance).BeginInit();

        grpBeam.Location = new Point(320, 12);
        grpBeam.Size = new Size(320, 200);
        grpBeam.Text = "Beam";

        lblBeamState.Location = new Point(10, 22);
        lblBeamState.Size = new Size(300, 18);
        lblBeamState.Text = "Beam State: --";

        lblHVLabel.Location = new Point(10, 48);
        lblHVLabel.Size = new Size(90, 23);
        lblHVLabel.Text = "High Voltage:";
        lblHVLabel.TextAlign = ContentAlignment.MiddleLeft;

        numHighVoltage.Location = new Point(105, 48);
        numHighVoltage.Size = new Size(100, 23);
        numHighVoltage.Minimum = 0;
        numHighVoltage.Maximum = 30000;
        numHighVoltage.DecimalPlaces = 0;
        numHighVoltage.Leave += NumHighVoltage_Leave;

        lblHVUnit.Location = new Point(210, 48);
        lblHVUnit.Size = new Size(30, 23);
        lblHVUnit.Text = "V";
        lblHVUnit.TextAlign = ContentAlignment.MiddleLeft;

        lblPCLabel.Location = new Point(10, 78);
        lblPCLabel.Size = new Size(90, 23);
        lblPCLabel.Text = "PC Index:";
        lblPCLabel.TextAlign = ContentAlignment.MiddleLeft;

        cboPCIndexes.Location = new Point(105, 78);
        cboPCIndexes.Size = new Size(200, 23);
        cboPCIndexes.DropDownStyle = ComboBoxStyle.DropDownList;
        cboPCIndexes.SelectedIndexChanged += CboPCIndexes_SelectedIndexChanged;

        lblBeamCurrentLabel.Location = new Point(10, 108);
        lblBeamCurrentLabel.Size = new Size(90, 23);
        lblBeamCurrentLabel.Text = "Beam Current:";
        lblBeamCurrentLabel.TextAlign = ContentAlignment.MiddleLeft;

        numBeamCurrent.Location = new Point(105, 108);
        numBeamCurrent.Size = new Size(100, 23);
        numBeamCurrent.Minimum = 0;
        numBeamCurrent.Maximum = 100000;
        numBeamCurrent.DecimalPlaces = 2;
        numBeamCurrent.Leave += NumBeamCurrent_Leave;

        lblBeamCurrentUnit.Location = new Point(210, 108);
        lblBeamCurrentUnit.Size = new Size(30, 23);
        lblBeamCurrentUnit.Text = "pA";
        lblBeamCurrentUnit.TextAlign = ContentAlignment.MiddleLeft;

        lblAbsorbedCurrent.Location = new Point(10, 138);
        lblAbsorbedCurrent.Size = new Size(300, 18);
        lblAbsorbedCurrent.Text = "Absorbed Current: -- pA";

        lblWDLabel.Location = new Point(10, 164);
        lblWDLabel.Size = new Size(90, 23);
        lblWDLabel.Text = "Working Dist:";
        lblWDLabel.TextAlign = ContentAlignment.MiddleLeft;

        numWorkingDistance.Location = new Point(105, 164);
        numWorkingDistance.Size = new Size(100, 23);
        numWorkingDistance.Minimum = 0;
        numWorkingDistance.Maximum = 100;
        numWorkingDistance.DecimalPlaces = 3;
        numWorkingDistance.Increment = 0.1m;
        numWorkingDistance.Leave += NumWorkingDistance_Leave;

        lblWDUnit.Location = new Point(210, 164);
        lblWDUnit.Size = new Size(30, 23);
        lblWDUnit.Text = "mm";
        lblWDUnit.TextAlign = ContentAlignment.MiddleLeft;

        grpBeam.Controls.Add(lblBeamState);
        grpBeam.Controls.Add(lblHVLabel);
        grpBeam.Controls.Add(numHighVoltage);
        grpBeam.Controls.Add(lblHVUnit);
        grpBeam.Controls.Add(lblPCLabel);
        grpBeam.Controls.Add(cboPCIndexes);
        grpBeam.Controls.Add(lblBeamCurrentLabel);
        grpBeam.Controls.Add(numBeamCurrent);
        grpBeam.Controls.Add(lblBeamCurrentUnit);
        grpBeam.Controls.Add(lblAbsorbedCurrent);
        grpBeam.Controls.Add(lblWDLabel);
        grpBeam.Controls.Add(numWorkingDistance);
        grpBeam.Controls.Add(lblWDUnit);

        ((System.ComponentModel.ISupportInitialize)numHighVoltage).EndInit();
        ((System.ComponentModel.ISupportInitialize)numBeamCurrent).EndInit();
        ((System.ComponentModel.ISupportInitialize)numWorkingDistance).EndInit();
        grpBeam.ResumeLayout(false);

        // Scanning Modes Group
        grpScanningModes = new GroupBox();
        cboScanningModes = new ComboBox();
        lblPivotPosition = new Label();

        grpScanningModes.SuspendLayout();
        grpScanningModes.Location = new Point(320, 218);
        grpScanningModes.Size = new Size(320, 80);
        grpScanningModes.Text = "Scanning Modes";

        cboScanningModes.Location = new Point(10, 22);
        cboScanningModes.Size = new Size(300, 23);
        cboScanningModes.DropDownStyle = ComboBoxStyle.DropDownList;
        cboScanningModes.SelectedIndexChanged += CboScanningModes_SelectedIndexChanged;

        lblPivotPosition.Location = new Point(10, 52);
        lblPivotPosition.Size = new Size(300, 18);
        lblPivotPosition.Text = "Pivot Position: -- mm";

        grpScanningModes.Controls.Add(cboScanningModes);
        grpScanningModes.Controls.Add(lblPivotPosition);
        grpScanningModes.ResumeLayout(false);

        // Geometries Group
        grpGeometries = new GroupBox();
        var lblImageShiftLabel = new Label();
        var lblImageShiftX = new Label();
        numImageShiftX = new NumericUpDown();
        var lblImageShiftY = new Label();
        numImageShiftY = new NumericUpDown();
        lblImageShiftRangeX = new Label();
        lblImageShiftRangeY = new Label();
        var lblImageRotLabel = new Label();
        numImageRotation = new NumericUpDown();
        lblImageRotRange = new Label();

        grpGeometries.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numImageShiftX).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numImageShiftY).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numImageRotation).BeginInit();

        grpGeometries.Location = new Point(320, 304);
        grpGeometries.Size = new Size(360, 130);
        grpGeometries.Text = "Geometries";

        lblImageShiftLabel.Location = new Point(10, 22);
        lblImageShiftLabel.Size = new Size(80, 18);
        lblImageShiftLabel.Text = "Image Shift:";

        lblImageShiftX.Location = new Point(10, 44);
        lblImageShiftX.Size = new Size(20, 23);
        lblImageShiftX.Text = "X:";
        lblImageShiftX.TextAlign = ContentAlignment.MiddleLeft;

        numImageShiftX.Location = new Point(30, 44);
        numImageShiftX.Size = new Size(80, 23);
        numImageShiftX.Minimum = -1;
        numImageShiftX.Maximum = 1;
        numImageShiftX.DecimalPlaces = 6;
        numImageShiftX.Increment = 0.001m;
        numImageShiftX.Leave += NumImageShiftX_Leave;

        lblImageShiftY.Location = new Point(120, 44);
        lblImageShiftY.Size = new Size(20, 23);
        lblImageShiftY.Text = "Y:";
        lblImageShiftY.TextAlign = ContentAlignment.MiddleLeft;

        numImageShiftY.Location = new Point(140, 44);
        numImageShiftY.Size = new Size(80, 23);
        numImageShiftY.Minimum = -1;
        numImageShiftY.Maximum = 1;
        numImageShiftY.DecimalPlaces = 6;
        numImageShiftY.Increment = 0.001m;
        numImageShiftY.Leave += NumImageShiftY_Leave;

        lblImageShiftRangeX.Location = new Point(10, 70);
        lblImageShiftRangeX.Size = new Size(170, 16);
        lblImageShiftRangeX.Text = "X range: -- to --";
        lblImageShiftRangeX.Font = new Font(this.Font.FontFamily, 8f);

        lblImageShiftRangeY.Location = new Point(185, 70);
        lblImageShiftRangeY.Size = new Size(170, 16);
        lblImageShiftRangeY.Text = "Y range: -- to --";
        lblImageShiftRangeY.Font = new Font(this.Font.FontFamily, 8f);

        lblImageRotLabel.Location = new Point(10, 90);
        lblImageRotLabel.Size = new Size(100, 18);
        lblImageRotLabel.Text = "Image Rotation:";

        numImageRotation.Location = new Point(110, 88);
        numImageRotation.Size = new Size(80, 23);
        numImageRotation.Minimum = -360;
        numImageRotation.Maximum = 360;
        numImageRotation.DecimalPlaces = 2;
        numImageRotation.Increment = 1m;
        numImageRotation.Leave += NumImageRotation_Leave;

        lblImageRotRange.Location = new Point(200, 90);
        lblImageRotRange.Size = new Size(110, 18);
        lblImageRotRange.Text = "Range: -- to -- deg";
        lblImageRotRange.Font = new Font(this.Font.FontFamily, 8f);

        grpGeometries.Controls.Add(lblImageShiftLabel);
        grpGeometries.Controls.Add(lblImageShiftX);
        grpGeometries.Controls.Add(numImageShiftX);
        grpGeometries.Controls.Add(lblImageShiftY);
        grpGeometries.Controls.Add(numImageShiftY);
        grpGeometries.Controls.Add(lblImageShiftRangeX);
        grpGeometries.Controls.Add(lblImageShiftRangeY);
        grpGeometries.Controls.Add(lblImageRotLabel);
        grpGeometries.Controls.Add(numImageRotation);
        grpGeometries.Controls.Add(lblImageRotRange);

        ((System.ComponentModel.ISupportInitialize)numImageShiftX).EndInit();
        ((System.ComponentModel.ISupportInitialize)numImageShiftY).EndInit();
        ((System.ComponentModel.ISupportInitialize)numImageRotation).EndInit();
        grpGeometries.ResumeLayout(false);

        // Stage Position Group
        grpStage = new GroupBox();
        lblStageCalibrated = new Label();
        lblStageBusy = new Label();
        var lblStageX = new Label();
        numStageX = new NumericUpDown();
        var lblStageY = new Label();
        numStageY = new NumericUpDown();
        var lblStageZ = new Label();
        numStageZ = new NumericUpDown();
        var lblStageR = new Label();
        numStageR = new NumericUpDown();
        var lblStageTx = new Label();
        numStageTx = new NumericUpDown();

        grpStage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numStageX).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numStageY).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numStageZ).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numStageR).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numStageTx).BeginInit();

        grpStage.Location = new Point(12, 370);
        grpStage.Size = new Size(300, 160);
        grpStage.Text = "Stage Position";

        lblStageCalibrated.Location = new Point(10, 22);
        lblStageCalibrated.Size = new Size(140, 18);
        lblStageCalibrated.Text = "Calibrated: --";

        lblStageBusy.Location = new Point(155, 22);
        lblStageBusy.Size = new Size(135, 18);
        lblStageBusy.Text = "Busy: --";

        lblStageX.Location = new Point(10, 48);
        lblStageX.Size = new Size(50, 23);
        lblStageX.Text = "X (mm):";
        lblStageX.TextAlign = ContentAlignment.MiddleLeft;

        numStageX.Location = new Point(60, 48);
        numStageX.Size = new Size(80, 23);
        numStageX.Minimum = -1000;
        numStageX.Maximum = 1000;
        numStageX.DecimalPlaces = 3;
        numStageX.Increment = 0.1m;
        numStageX.Leave += NumStageX_Leave;

        lblStageY.Location = new Point(150, 48);
        lblStageY.Size = new Size(50, 23);
        lblStageY.Text = "Y (mm):";
        lblStageY.TextAlign = ContentAlignment.MiddleLeft;

        numStageY.Location = new Point(200, 48);
        numStageY.Size = new Size(80, 23);
        numStageY.Minimum = -1000;
        numStageY.Maximum = 1000;
        numStageY.DecimalPlaces = 3;
        numStageY.Increment = 0.1m;
        numStageY.Leave += NumStageY_Leave;

        lblStageZ.Location = new Point(10, 78);
        lblStageZ.Size = new Size(50, 23);
        lblStageZ.Text = "Z (mm):";
        lblStageZ.TextAlign = ContentAlignment.MiddleLeft;

        numStageZ.Location = new Point(60, 78);
        numStageZ.Size = new Size(80, 23);
        numStageZ.Minimum = -1000;
        numStageZ.Maximum = 1000;
        numStageZ.DecimalPlaces = 3;
        numStageZ.Increment = 0.1m;
        numStageZ.Leave += NumStageZ_Leave;

        lblStageR.Location = new Point(150, 78);
        lblStageR.Size = new Size(50, 23);
        lblStageR.Text = "R (deg):";
        lblStageR.TextAlign = ContentAlignment.MiddleLeft;

        numStageR.Location = new Point(200, 78);
        numStageR.Size = new Size(80, 23);
        numStageR.Minimum = -360;
        numStageR.Maximum = 360;
        numStageR.DecimalPlaces = 2;
        numStageR.Increment = 1m;
        numStageR.Leave += NumStageR_Leave;

        lblStageTx.Location = new Point(10, 108);
        lblStageTx.Size = new Size(50, 23);
        lblStageTx.Text = "Tx (deg):";
        lblStageTx.TextAlign = ContentAlignment.MiddleLeft;

        numStageTx.Location = new Point(60, 108);
        numStageTx.Size = new Size(80, 23);
        numStageTx.Minimum = -90;
        numStageTx.Maximum = 90;
        numStageTx.DecimalPlaces = 2;
        numStageTx.Increment = 1m;
        numStageTx.Leave += NumStageTx_Leave;

        grpStage.Controls.Add(lblStageCalibrated);
        grpStage.Controls.Add(lblStageBusy);
        grpStage.Controls.Add(lblStageX);
        grpStage.Controls.Add(numStageX);
        grpStage.Controls.Add(lblStageY);
        grpStage.Controls.Add(numStageY);
        grpStage.Controls.Add(lblStageZ);
        grpStage.Controls.Add(numStageZ);
        grpStage.Controls.Add(lblStageR);
        grpStage.Controls.Add(numStageR);
        grpStage.Controls.Add(lblStageTx);
        grpStage.Controls.Add(numStageTx);

        ((System.ComponentModel.ISupportInitialize)numStageX).EndInit();
        ((System.ComponentModel.ISupportInitialize)numStageY).EndInit();
        ((System.ComponentModel.ISupportInitialize)numStageZ).EndInit();
        ((System.ComponentModel.ISupportInitialize)numStageR).EndInit();
        ((System.ComponentModel.ISupportInitialize)numStageTx).EndInit();
        grpStage.ResumeLayout(false);

        // View Field Group
        grpViewField = new GroupBox();
        var lblVFLabel = new Label();
        numViewField = new NumericUpDown();
        var lblVFUnit = new Label();

        grpViewField.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numViewField).BeginInit();

        grpViewField.Location = new Point(12, 536);
        grpViewField.Size = new Size(300, 55);
        grpViewField.Text = "View Field Width";

        lblVFLabel.Location = new Point(10, 22);
        lblVFLabel.Size = new Size(80, 23);
        lblVFLabel.Text = "View Field:";
        lblVFLabel.TextAlign = ContentAlignment.MiddleLeft;

        numViewField.Location = new Point(95, 22);
        numViewField.Size = new Size(100, 23);
        numViewField.Minimum = 0;
        numViewField.Maximum = 100000;
        numViewField.DecimalPlaces = 3;
        numViewField.Increment = 1m;
        numViewField.Leave += NumViewField_Leave;

        lblVFUnit.Location = new Point(200, 22);
        lblVFUnit.Size = new Size(30, 23);
        lblVFUnit.Text = "µm";
        lblVFUnit.TextAlign = ContentAlignment.MiddleLeft;

        grpViewField.Controls.Add(lblVFLabel);
        grpViewField.Controls.Add(numViewField);
        grpViewField.Controls.Add(lblVFUnit);

        ((System.ComponentModel.ISupportInitialize)numViewField).EndInit();
        grpViewField.ResumeLayout(false);

        // Scanning Group
        grpScanning = new GroupBox();
        var lblSpeedLabel = new Label();
        cboScanSpeeds = new ComboBox();
        lblDwellTime = new Label();
        btnAcquireImage = new Button();
        picImage = new PictureBox();

        grpScanning.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picImage).BeginInit();

        grpScanning.Location = new Point(650, 12);
        grpScanning.Size = new Size(420, 580);
        grpScanning.Text = "Scanning";

        lblSpeedLabel.Location = new Point(10, 22);
        lblSpeedLabel.Size = new Size(80, 23);
        lblSpeedLabel.Text = "Scan Speed:";
        lblSpeedLabel.TextAlign = ContentAlignment.MiddleLeft;

        cboScanSpeeds.Location = new Point(95, 22);
        cboScanSpeeds.Size = new Size(150, 23);
        cboScanSpeeds.DropDownStyle = ComboBoxStyle.DropDownList;
        cboScanSpeeds.SelectedIndexChanged += CboScanSpeeds_SelectedIndexChanged;

        lblDwellTime.Location = new Point(255, 22);
        lblDwellTime.Size = new Size(150, 23);
        lblDwellTime.Text = "-- µs/pixel";
        lblDwellTime.TextAlign = ContentAlignment.MiddleLeft;

        btnAcquireImage.Location = new Point(10, 55);
        btnAcquireImage.Size = new Size(120, 28);
        btnAcquireImage.Text = "Acquire Image";
        btnAcquireImage.Click += BtnAcquireImage_Click;

        picImage.Location = new Point(10, 90);
        picImage.Size = new Size(400, 400);
        picImage.BorderStyle = BorderStyle.FixedSingle;
        picImage.SizeMode = PictureBoxSizeMode.Zoom;
        picImage.BackColor = Color.Black;

        grpScanning.Controls.Add(lblSpeedLabel);
        grpScanning.Controls.Add(cboScanSpeeds);
        grpScanning.Controls.Add(lblDwellTime);
        grpScanning.Controls.Add(btnAcquireImage);
        grpScanning.Controls.Add(picImage);

        ((System.ComponentModel.ISupportInitialize)picImage).EndInit();
        grpScanning.ResumeLayout(false);

        // Main Form
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1085, 605);
        this.Controls.Add(grpConnection);
        this.Controls.Add(grpMicroscopeInfo);
        this.Controls.Add(grpVacuum);
        this.Controls.Add(grpDetector);
        this.Controls.Add(grpBeam);
        this.Controls.Add(grpScanningModes);
        this.Controls.Add(grpGeometries);
        this.Controls.Add(grpStage);
        this.Controls.Add(grpViewField);
        this.Controls.Add(grpScanning);
        this.Name = "MainForm";
        this.Text = "SEM Controller";
        this.FormClosing += MainForm_FormClosing;

        this.ResumeLayout(false);
    }

    private GroupBox grpConnection;
    private Button btnConnect;
    private Button btnRefresh;
    private Label lblConnectionStatus;

    private GroupBox grpMicroscopeInfo;
    private Label lblManufacturer;
    private Label lblModel;
    private Label lblSerial;
    private Label lblSoftwareVersion;
    private Label lblProtocolVersion;

    private GroupBox grpVacuum;
    private Label lblVacuumStatus;
    private Label lblChamberPressure;

    private GroupBox grpDetector;
    private ComboBox cboDetectors;

    private GroupBox grpBeam;
    private Label lblBeamState;
    private NumericUpDown numHighVoltage;
    private ComboBox cboPCIndexes;
    private NumericUpDown numBeamCurrent;
    private Label lblAbsorbedCurrent;
    private NumericUpDown numWorkingDistance;

    private GroupBox grpScanningModes;
    private ComboBox cboScanningModes;
    private Label lblPivotPosition;

    private GroupBox grpGeometries;
    private NumericUpDown numImageShiftX;
    private NumericUpDown numImageShiftY;
    private Label lblImageShiftRangeX;
    private Label lblImageShiftRangeY;
    private NumericUpDown numImageRotation;
    private Label lblImageRotRange;

    private GroupBox grpStage;
    private Label lblStageCalibrated;
    private Label lblStageBusy;
    private NumericUpDown numStageX;
    private NumericUpDown numStageY;
    private NumericUpDown numStageZ;
    private NumericUpDown numStageR;
    private NumericUpDown numStageTx;

    private GroupBox grpViewField;
    private NumericUpDown numViewField;

    private GroupBox grpScanning;
    private ComboBox cboScanSpeeds;
    private Label lblDwellTime;
    private Button btnAcquireImage;
    private PictureBox picImage;
}
