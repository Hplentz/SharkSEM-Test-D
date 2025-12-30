// =============================================================================
// SemControllerFactory.cs - Controller Factory Pattern
// =============================================================================
// Provides factory methods for creating ISemController instances.
// Abstracts away vendor-specific instantiation details, allowing application
// code to work with controllers through settings objects rather than
// knowing about specific implementation classes.
//
// Usage Patterns:
//   1. Settings-based: SemControllerFactory.Create(settings)
//   2. Vendor-specific: SemControllerFactory.CreateTescan("192.168.1.100")
//   3. Convenience: SemControllerFactory.CreateAndConnectAsync(settings)
// =============================================================================

using SemController.Core.Implementations;
using SemController.Core.Implementations.Tescan;
using SemController.Core.Implementations.Thermo;
using SemController.Core.Interfaces;
using SemController.Core.Models;

namespace SemController.Core.Factory;

/// <summary>
/// Factory for creating vendor-specific ISemController implementations.
/// Use this class instead of instantiating controllers directly.
/// </summary>
public static class SemControllerFactory
{
    /// <summary>
    /// Creates a controller based on connection settings.
    /// This is the primary factory method for configuration-driven instantiation.
    /// </summary>
    /// <param name="settings">Connection settings specifying vendor type and parameters.</param>
    /// <returns>Unconnected controller instance. Call ConnectAsync() before use.</returns>
    /// <exception cref="NotSupportedException">Thrown for Custom type requiring direct implementation.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown SemType values.</exception>
    public static ISemController Create(SemConnectionSettings settings)
    {
        // Switch expression maps SemType to appropriate controller implementation
        return settings.Type switch
        {
            SemType.Tescan => new TescanSemController(settings),
            SemType.Thermo => new ThermoSemController(settings.Host, settings.Port),
            SemType.Mock => new MockSemController(),
            SemType.Custom => throw new NotSupportedException("Custom SEM type requires direct implementation"),
            _ => throw new ArgumentOutOfRangeException(nameof(settings.Type), settings.Type, "Unknown SEM type")
        };
    }
    
    // -------------------------------------------------------------------------
    // Vendor-Specific Factory Methods
    // -------------------------------------------------------------------------
    // These provide convenient shortcuts when vendor type is known at compile time.
    
    /// <summary>Creates a TESCAN controller with specified connection parameters.</summary>
    /// <param name="host">Microscope PC hostname or IP address.</param>
    /// <param name="port">SharkSEM control channel port (default 8300).</param>
    /// <param name="timeoutSeconds">Communication timeout in seconds.</param>
    public static ISemController CreateTescan(string host, int port = 8300, double timeoutSeconds = 30.0)
    {
        return new TescanSemController(host, port, timeoutSeconds);
    }
    
    /// <summary>Creates a Thermo Fisher controller.</summary>
    /// <param name="host">AutoScript server host (typically "localhost").</param>
    /// <param name="port">AutoScript server port.</param>
    public static ISemController CreateThermo(string host = "localhost", int port = 7520)
    {
        return new ThermoSemController(host, port);
    }
    
    /// <summary>Creates a mock controller for testing without hardware.</summary>
    public static ISemController CreateMock()
    {
        return new MockSemController();
    }
    
    // -------------------------------------------------------------------------
    // Convenience Methods (Create + Connect)
    // -------------------------------------------------------------------------
    // These combine creation and connection in a single async call.
    
    /// <summary>
    /// Creates controller from settings and establishes connection.
    /// Returns ready-to-use connected controller.
    /// </summary>
    public static async Task<ISemController> CreateAndConnectAsync(SemConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        ISemController controller = Create(settings);
        await controller.ConnectAsync(cancellationToken);
        return controller;
    }
    
    /// <summary>Creates and connects to a TESCAN microscope.</summary>
    public static async Task<ISemController> CreateAndConnectTescanAsync(string host, int port = 8300, double timeoutSeconds = 30.0, CancellationToken cancellationToken = default)
    {
        ISemController controller = CreateTescan(host, port, timeoutSeconds);
        await controller.ConnectAsync(cancellationToken);
        return controller;
    }
    
    /// <summary>Creates and connects to a Thermo Fisher microscope.</summary>
    public static async Task<ISemController> CreateAndConnectThermoAsync(string host = "localhost", int port = 7520, CancellationToken cancellationToken = default)
    {
        ISemController controller = CreateThermo(host, port);
        await controller.ConnectAsync(cancellationToken);
        return controller;
    }
}
