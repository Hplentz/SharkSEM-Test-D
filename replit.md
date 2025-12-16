# SharkSEM-Test-B (SEM Controller Library)

## Overview
A C# library providing a unified interface for controlling Scanning Electron Microscopes (SEMs). The library abstracts vendor-specific APIs behind a common interface (`ISemController`), allowing application code to work with any supported microscope without modification.

## Current State
- **Completed**: Core library with ISemController interface, TescanSemController (SharkSEM API), MockSemController for testing
- **Working**: Demo application runs successfully showing all major features

## Project Structure
```
src/SharkSEM-Test-B/
├── SemController.sln              # Solution file
├── SemController.Core/            # Main library
│   ├── Interfaces/                # ISemController, ISemConnection
│   ├── Models/                    # StagePosition, ScanSettings, SemImage, etc.
│   ├── Implementations/           # TescanSemController, MockSemController
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
