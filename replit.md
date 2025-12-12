# SEM Controller Library

## Overview
A C# library providing a unified interface for controlling Scanning Electron Microscopes (SEMs). The library abstracts vendor-specific APIs behind a common interface (`ISemController`), allowing application code to work with any supported microscope without modification.

## Current State
- **Completed**: Core library with ISemController interface, TescanSemController (SharkSEM API), MockSemController for testing
- **Working**: Demo application runs successfully showing all major features

## Project Structure
```
src/SemController/
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
- **SharkSEM Protocol**: Uses TCP connection with text-based commands

## Technical Details
- .NET 8.0
- No external dependencies (pure BCL implementation)
- SharkSEM uses TCP port 8300 by default
- All measurements in SI units (meters, volts, amperes, pascals)
