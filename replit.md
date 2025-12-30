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

**Source Code Documentation Complete**
All 35+ source files now include comprehensive documentation:
- File headers with protocol/architecture overviews
- Class-level summaries describing purpose and responsibilities  
- Method documentation explaining functionality and parameters
- Unit conversion notes where applicable
- Critical implementation details for protocol handling

Documentation standard follows: No line-by-line commenting. Focus on what and why, making code maintainable when modified outside Replit.

## External Dependencies
- **TESCAN SharkSEM Protocol**: Custom TCP-based binary protocol (default port 8300 for control, 8301 for data).
- **Thermo Fisher Scientific AutoScript API**: COM-based C# API, requiring specific DLLs (`SemController.Core/Implementations/Thermo/lib/`).
- **.NET 8.0**: Core framework.