# SharkSEM-Test-D (SEM Controller Library)

## Overview
This C# library provides a unified, vendor-agnostic interface (`ISemController`) for controlling Scanning Electron Microscopes (SEMs). Its primary purpose is to abstract away vendor-specific APIs, allowing application code to control various SEMs (TESCAN, Thermo Fisher Scientific) using a single, consistent API. This enables seamless integration and interchangeability of SEM equipment in automated workflows. The project aims to simplify SEM control for researchers and developers, reducing the complexity of interacting with diverse microscope systems.

## User Preferences
I prefer simple language and detailed explanations. I want iterative development with frequent check-ins. Ask before making major architectural changes or introducing new external dependencies. Do not make changes to the `SemController.Core/Implementations/Thermo/lib/` folder. Provide progress updates before long-running operations so I know the system isn't stuck.

## System Architecture
The system is built around a core `ISemController` interface, providing a comprehensive set of operations for SEM control, including connection management, vacuum, stage, beam, optics, and imaging. A `SemControllerFactory` pattern is used to instantiate vendor-specific controllers. The architecture is modular, with both `TescanSemController` and `ThermoSemController` broken down into sub-objects (e.g., `sem.Stage`, `sem.Vacuum`, `sem.Beam`) for organized access. All operations are asynchronous (`async/await`) and controllers implement `IDisposable` for resource management.

**Project Structure:**
```
src/SharkSEM-Test-D/
├── SemController.sln
├── SemController.Core/
│   ├── Interfaces/ISemController.cs
│   ├── Models/
│   ├── Implementations/
│   │   ├── Tescan/
│   │   │   ├── TescanSemController.cs
│   │   │   ├── TescanSemStage.cs, TescanSemVacuum.cs, etc.
│   │   ├── Thermo/
│   │   │   ├── ThermoSemController.cs
│   │   │   ├── ThermoSemVacuum.cs, ThermoSemBeam.cs, ThermoSemStage.cs
│   │   │   ├── ThermoSemOptics.cs, ThermoSemScanning.cs, ThermoSemMisc.cs
│   │   │   └── lib/ (AutoScript DLLs)
│   │   └── MockSemController.cs
│   └── Factory/SemControllerFactory.cs
├── SemController.Tescan.Example/
├── SemController.Thermo.Example/
└── SemController.Tescan.UI/
```

**Key Features:**
- **Interface Abstraction**: `ISemController` unifies control across different SEM vendors.
- **Vendor Support**:
    - **TESCAN**: Implemented via SharkSEM protocol (TCP-based, binary message format).
    - **Thermo Fisher Scientific**: Implemented via AutoScript C# API (COM-based).
    - **Mock**: For testing and development.
- **UI/UX**: A WinForms UI application (`SemController.Tescan.UI`) provides a visual control interface for TESCAN SEMs, displaying microscope info, vacuum, detectors, beam, scanning modes, geometries, stage, and view field, with editable controls.
- **SharkSEM Protocol Handling**: Includes precise binary message header and body encoding, handling 4-byte alignment, protocol version checking, and dedicated data channel management for image acquisition.
- **Unit Conventions**: Standardized units for stage positions (mm), working distance (mm), view field (µm), high voltage (Volts), emission current (Amperes), and tilt/rotation (Degrees).
- **Image Acquisition Workflow**: Detailed steps for detector configuration, channel selection, enabling, signal optimization, and image capture via a dedicated data channel.

## Important Implementation Notes

**Thermo AutoScript API - Use `var` for COM objects**: When accessing AutoScript service properties (e.g., `_getClient().Service`, `service.System`), you MUST use `var` instead of explicit `dynamic` declarations. Using `dynamic` explicitly causes RuntimeBinderException because it loses type information needed for COM property resolution. This is critical for ThermoSemMisc.cs and any code accessing the AutoScript API.

## Recent Changes (December 2025)

**Live Hardware Testing & Bug Fixes - Verified on Gen3 (V2.x.x) and Gen4 (V3.x.x) SEMs**

Critical bugs fixed during live SEM testing:

1. **GetIAbsorbed hang**: Fixed by sending `null` body instead of integer 0. Protocol expects no body for this command.

2. **Protocol version parsing**: Older protocols (V2.x.x) may return device info concatenated with version string (e.g., "2.0.210300USB\VID_..."). Fixed to extract only version digits.

3. **Command version checking**: Added `CommandMinVersions` dictionary to skip unsupported commands gracefully instead of hanging. Key commands added in V2.0.22 (not available in V2.0.21):
   - `StgGetLimits` - Get stage travel limits
   - `StgGetMotorized` - Check motorized axes
   - `GetGeomLimits` - Get geometry parameter limits

4. **Version requirements verified from API manuals**:
   - V2.0.22 manual for Gen3 SEMs
   - V3.x.x manual for Gen4 SEMs

**Enhanced Protocol Documentation in TescanSemController.cs and TescanSemScanning.cs**

Key files now include detailed ASCII diagrams and protocol specifications:

- **SharkSEM Message Header Format**: 32-byte layout with offset/size/field tables
- **Body Data Encoding**: Integer, float (ASCII-encoded!), string formats with examples
- **Data Channel Setup**: Critical bind→register→connect sequence explained
- **ScScanXY Command**: Request/response body format for image acquisition
- **ScData Message Format**: Data channel pixel data structure
- **Wait Flags**: WaitFlagOptics, WaitFlagAuto usage and importance

**Source Code Documentation Complete**
All 35+ source files now include comprehensive documentation:
- File headers with protocol/architecture overviews
- Class-level summaries describing purpose and responsibilities  
- Method documentation explaining functionality and parameters
- Unit conversion notes where applicable
- Critical implementation details for protocol handling

Documentation standard follows: No line-by-line commenting. Focus on what and why, making code maintainable when modified outside Replit.

## Recent Changes (January 2026)

**Comprehensive Exception Handling Implementation**

Added robust exception handling across all TESCAN modules with detailed source comments explaining error scenarios and recovery strategies:

1. **ConnectAsync**: Catches SocketException with proper cleanup, re-throws descriptive InvalidOperationException with troubleshooting guidance (check power, SharkSEM server, firewall).

2. **EnsureDataChannelInternalAsync**: Data channel setup errors trigger CloseDataChannel() cleanup to allow retry. Reports specific failure stage (bind, register, or connect).

3. **SendCommandInternalAsync/NoResponse**: Detects connection loss during read/write, marks controller disconnected via MarkDisconnected() helper, throws IOException with command name and context.

4. **TescanSemStage.WaitForMovementAsync**: Timeout errors include current stage position when available. Communication errors during wait are caught and reported with position context.

5. **TescanSemVacuum**: PumpAsync/VentAsync include safety warnings (venting while beam on = potential gun damage) and wrap errors with helpful messages.

6. **TescanSemHighVoltage.BeamOnAsync**: Documents vacuum prerequisites per API manual. SetVoltageAsync includes voltage limits guidance.

7. **TescanSemScanning.AcquireImagesAsync**: Validates parameters upfront, decodes ScScanXY error codes (-1/-2/-3), ensures GUI scanning is re-enabled in finally block even on errors.

**Design Decisions**:
- Library re-throws descriptive exceptions instead of calling Environment.Exit
- IOException for network/communication failures
- InvalidOperationException for state/prerequisite errors  
- TimeoutException for operations that exceed time limits
- MarkDisconnected() centralizes connection cleanup
- Error messages include actionable troubleshooting guidance

**Startup Project**: SemController.Tescan.Example set as default startup project in solution file.

## External Dependencies
- **TESCAN SharkSEM Protocol**: Custom TCP-based binary protocol (default port 8300 for control, 8301 for data).
- **Thermo Fisher Scientific AutoScript API**: COM-based C# API, requiring specific DLLs (`SemController.Core/Implementations/Thermo/lib/`).
- **.NET 8.0**: Core framework.