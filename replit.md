# SharkSEM-Test-C (SEM Controller Library)

## Overview
A C# library providing a unified interface for controlling Scanning Electron Microscopes (SEMs). The library abstracts vendor-specific APIs behind a common interface (`ISemController`), allowing application code to work with any supported microscope without modification.

## Current State
- **Completed**: Core library with ISemController interface, TescanSemController (SharkSEM API), MockSemController for testing
- **Working**: Demo application runs successfully showing all major features

## Project Structure
```
src/SharkSEM-Test-C/
├── SemController.sln              # Solution file
├── SemController.UI/              # WinForms application (startup project)
├── SemController.Core/            # Main library
│   ├── Interfaces/                # ISemController, ISemConnection
│   ├── Models/                    # StagePosition, ScanSettings, SemImage, etc.
│   ├── Implementations/           # Controller implementations (modular design)
│   │   ├── TescanSemController.cs     # Main controller (connection, protocol)
│   │   ├── TescanSemStage.cs          # Stage control (position, movement, limits)
│   │   ├── TescanSemDetectors.cs      # Detector configuration
│   │   ├── TescanSemHighVoltage.cs    # Beam & HV control
│   │   ├── TescanSemElectronOptics.cs # Focus, WD, ViewField, SpotSize, ScanningModes
│   │   ├── TescanSemImageGeometry.cs  # Image geometry, shift, centerings
│   │   ├── TescanSemScanning.cs       # Scan control & image acquisition
│   │   ├── TescanSemVacuum.cs         # Vacuum control
│   │   ├── TescanSemMisc.cs           # Miscellaneous (GetMicroscopeInfo)
│   │   └── MockSemController.cs       # Mock controller for testing
│   └── Factory/                   # SemControllerFactory
└── SemController.Example/         # Demo console application
```

## Key Interfaces

### ISemController
Main interface for controlling any SEM:
- Connection management: `ConnectAsync()`, `DisconnectAsync()`
- Vacuum: `GetVacuumStatusAsync()`, `GetVacuumPressureAsync()`, `PumpVacuumAsync()`, `VentAsync()`
- Stage: `GetStagePositionAsync()`, `MoveStageAsync()`, `GetStageLimitsAsync()`
- Beam: `BeamOnAsync()`, `BeamOffAsync()`, `GetHighVoltageAsync()`, `SetHighVoltageAsync()`
- Optics: `GetMagnificationAsync()`, `SetMagnificationAsync()`, `AutoFocusAsync()`
- Imaging: `AcquireSingleImageAsync()`, `StartScanAsync()`, `StopScanAsync()`

## Supported Microscopes
1. **TESCAN** - Via SharkSEM protocol (TCP-based, protocol version 3.2.20)
2. **Mock** - Simulated microscope for development and testing

## Usage Example
```csharp
using SemController.Core.Factory;
using SemController.Core.Interfaces;

// Create and connect
using ISemController sem = SemControllerFactory.CreateTescan("192.168.1.100");
await sem.ConnectAsync();

// Use the same interface regardless of microscope type
var info = await sem.GetMicroscopeInfoAsync();
await sem.BeamOnAsync();
await sem.SetHighVoltageAsync(15000); // 15 kV
await sem.SetMagnificationAsync(5000);
var image = await sem.AcquireSingleImageAsync(0, 1024, 768);

await sem.DisconnectAsync();
```

## Recent Changes
- 2024-12-18: Added Image Geometry commands
  - `EnumGeometriesAsync()` - Returns list of available geometry parameters with index and name
  - `GetGeometryAsync(int index)` - Returns (x, y) values for geometry parameter
  - `SetGeometryAsync(int index, double x, double y)` - Sets geometry values
  - `GetImageShiftAsync()` - Returns (x, y) image shift values
  - `SetImageShiftAsync(double x, double y)` - Sets image shift
  - `EnumCenteringsAsync()` - Returns list of centering parameters with index and name
  - `GetCenteringAsync(int index)` - Returns (x, y) centering values
  - `SetCenteringAsync(int index, double x, double y)` - Sets centering values
  - Added ImageGeometry and Centering record models

- 2024-12-18: Added Scanning Mode commands
  - `EnumScanningModesAsync()` - Returns list of available scanning modes with index and name
  - `GetScanningModeAsync()` - Returns current scanning mode index
  - `SetScanningModeAsync(int modeIndex)` - Sets scanning mode (with Wait C flag for stabilization)
  - `GetPivotPositionAsync()` - Returns (result, pivotPosition) tuple in mm
  - Added ScanningMode record model

- 2024-12-18: Added current measurement commands
  - `GetBeamCurrentAsync()` / `SetBeamCurrentAsync(double pA)` - Beam current in picoamps
  - `GetAbsorbedCurrentAsync()` - Specimen absorbed current in picoamps (requires active scanning)
  - `EnumPCIndexesAsync()` - Returns raw PC index enumeration
  - Removed `SetSpotSizeAsync()` - Command does not exist in API (SpotSize is read-only calculated value)

- 2024-12-17: Added protocol version checking
  - Protocol version fetched automatically on connect via `TcpGetVersion`
  - Each API command has a minimum required version (e.g., `TcpGetModel` requires 3.2.20)
  - If command not supported by current protocol version, logs warning to console and skips call
  - Properties exposed: `ProtocolVersionString` (e.g., "3.2.9"), `ProtocolVersion` (System.Version)
  - Example: `TcpGetModel` requires v3.2.20, so on v3.2.9 it will log a warning instead of failing

- 2024-12-17: Major refactoring - split TescanSemController into modular sub-classes
  - TescanSemStage: Stage control (position, movement, limits, calibration)
  - TescanSemDetectors: Detector configuration (enum, select, enable, auto signal)
  - TescanSemHighVoltage: Beam & HV control (on/off, voltage, emission)
  - TescanSemElectronOptics: Optics (view field, WD, focus, spot size)
  - TescanSemScanning: Scan control & image acquisition
  - TescanSemVacuum: Vacuum control (status, pump, vent)
  - TescanSemMisc: Miscellaneous (microscope info)
  - Backward compatibility maintained via delegation pattern
  - Can use either `sem.Stage.GetPositionAsync()` or `sem.GetStagePositionAsync()`

- 2024-12-16: Fixed data channel connection order for image acquisition
  - **Critical fix**: Data channel must bind → register port → connect (not connect first)
  - This matches the official Python SharkSEM library exactly
  - Order: 1) Bind socket to get local port, 2) Register port with TcpRegDataPort, 3) Connect to data port
  - Image data now received correctly via ScData chunks on data channel

- 2024-12-15: Fixed scan control handshake for remote image acquisition
  - Using GUISetScanning and ScStopScan instead of ScCtrlGui/ScCtrlManual (not available in v3.2.9)
  - Re-enables GUI control after acquisition
  - ScScanXY uses 8 integer parameters (no dwell_time)

- 2024-12-15: Fixed optics synchronization for image acquisition
  - Added wait flags support (WaitFlagOptics, WaitFlagAuto) to header protocol
  - AutoFocusAsync now blocks until optics adjustment completes
  - AutoSignalAsync now blocks until signal adjustment completes
  - This prevents "Optics Adjustment Running" during image capture

- 2024-12-15: Added detector configuration API
  - EnumDetectorsAsync, GetChannelCountAsync, SelectDetectorAsync
  - EnableChannelAsync, GetSelectedDetectorAsync, GetChannelEnabledAsync
  - AutoSignalAsync for automatic signal optimization

- 2024-12-13: Fixed chunked image acquisition via data channel
  - FetchImage now uses SendCommandNoResponseAsync (triggers data channel, doesn't return image)
  - Image data always read from data channel via ScData messages
  - ScData messages properly parsed: frame(4) + channel(4) + index(4) + bpp(4) + size(4) + data[]
  - Pixel index field used to place chunks at correct positions in image buffer
  - Pre-allocated image buffers for each channel

- 2024-12-12: Fixed unit conversions for TESCAN API
  - Stage positions/limits: API returns µm, converted to mm (÷1000)
  - Working Distance: API returns µm, converted to mm (÷1000)
  - Replaced magnification with View Field (API returns mm, displayed as µm)
  - Updated interface: GetViewFieldAsync/SetViewFieldAsync replace Get/SetMagnificationAsync

- 2024-12-12: Fixed SharkSEM binary protocol implementation
  - Rewrote TescanSemController to use correct 32-byte binary header format
  - Implemented proper encoding for integers, floats (as strings), and arrays
  - Added 4-byte alignment/padding as required by protocol spec
  - All messages now follow Message Structure from API manual

- 2024-12-12: Initial implementation complete
  - Created ISemController interface with comprehensive SEM operations
  - Implemented TescanSemController for SharkSEM API
  - Created MockSemController for testing
  - Added factory pattern for controller instantiation
  - Working demo application

## Architecture Notes
- **Factory Pattern**: Use `SemControllerFactory` to create controllers
- **Interface Abstraction**: All controllers implement `ISemController`
- **Modular Design**: TescanSemController exposes sub-objects for organized access
  - `sem.Stage` - Stage control operations
  - `sem.Detectors` - Detector configuration
  - `sem.HighVoltage` - Beam and HV control
  - `sem.Optics` - Electron optics (focus, WD, view field)
  - `sem.Scanning` - Image acquisition
  - `sem.Vacuum` - Vacuum control
  - `sem.Misc` - Miscellaneous operations
- **Async/Await**: All operations are asynchronous
- **IDisposable**: Controllers implement IDisposable for resource cleanup

## SharkSEM Protocol Details
The SharkSEM protocol uses a binary message format over TCP:

### Message Header (32 bytes)
- Bytes 0-15: Command name (null-terminated, max 15 chars)
- Bytes 16-19: Body size (uint32, little-endian)
- Bytes 20-23: Message ID (uint32)
- Bytes 24-25: Flags (uint16) - bit 0 = request response
- Bytes 26-27: Queue (uint16)
- Bytes 28-31: Reserved (zeros)

### Message Body Encoding
- **Integers**: 4 bytes, little-endian
- **Floats**: Encoded as null-terminated ASCII strings with 4-byte size prefix
- **Strings/Arrays**: 4-byte size prefix + data + padding to 4-byte alignment

## Technical Details
- .NET 8.0
- No external dependencies (pure BCL implementation)
- SharkSEM uses TCP port 8300 by default

## Unit Conventions
- **Stage positions**: Returned in **mm** (no conversion - API returns mm)
- **Stage limits X/Y/Z**: Returned in **mm** (no conversion - API returns mm)
- **Working Distance**: Returned in **mm** (no conversion - API returns mm)
- **View Field**: Returned in **µm** (API returns mm, converted ×1000)
- **High Voltage**: Volts
- **Emission Current**: Amperes (API returns µA, converted)
- **Tilt/Rotation**: Degrees (no conversion)

## Detector Configuration
Before acquiring images, detectors must be properly configured:

### Key Methods (TescanSemController)
- `EnumDetectorsAsync()` - Returns semicolon-separated list of detector names
- `GetChannelCountAsync()` - Returns number of available channels
- `SelectDetectorAsync(channel, detector)` - Assigns detector to channel
- `EnableChannelAsync(channel, enable, bpp)` - Enables channel with bit depth
- `GetSelectedDetectorAsync(channel)` - Gets currently selected detector
- `GetChannelEnabledAsync(channel)` - Gets (enabled, bpp) tuple
- `AutoSignalAsync(channel)` - Auto-adjusts signal for channel

### Image Acquisition Workflow
1. Call `EnumDetectorsAsync()` to find BSE detector index
2. Call `SelectDetectorAsync(channel, bseIndex)` to assign BSE to channel
3. Call `EnableChannelAsync(channel, true, 8)` to enable with 8-bit depth
4. Call `AutoSignalAsync(channel)` to optimize signal levels
5. Call `AcquireImagesAsync(settings)` to capture image

### Data Channel
- **Control port**: 8300 (commands and responses)
- **Data port**: 8301 (image data transfer via ScData messages)

The library automatically establishes the data channel when acquiring images:
1. Connects to port 8301 first
2. Registers the local port with TcpRegDataPort
3. Collects ScData messages after ScScanXY command
