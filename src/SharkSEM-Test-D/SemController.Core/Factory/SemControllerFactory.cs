using SemController.Core.Implementations;
using SemController.Core.Implementations.Tescan;
using SemController.Core.Implementations.Thermo;
using SemController.Core.Interfaces;
using SemController.Core.Models;

namespace SemController.Core.Factory;

public static class SemControllerFactory
{
    public static ISemController Create(SemConnectionSettings settings)
    {
        return settings.Type switch
        {
            SemType.Tescan => new TescanSemController(settings),
            SemType.Thermo => new ThermoSemController(settings.Host, settings.Port),
            SemType.Mock => new MockSemController(),
            SemType.Custom => throw new NotSupportedException("Custom SEM type requires direct implementation"),
            _ => throw new ArgumentOutOfRangeException(nameof(settings.Type), settings.Type, "Unknown SEM type")
        };
    }
    
    public static ISemController CreateTescan(string host, int port = 8300, double timeoutSeconds = 30.0)
    {
        return new TescanSemController(host, port, timeoutSeconds);
    }
    
    public static ISemController CreateThermo(string host = "localhost", int port = 7520)
    {
        return new ThermoSemController(host, port);
    }
    
    public static ISemController CreateMock()
    {
        return new MockSemController();
    }
    
    public static async Task<ISemController> CreateAndConnectAsync(SemConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        ISemController controller = Create(settings);
        await controller.ConnectAsync(cancellationToken);
        return controller;
    }
    
    public static async Task<ISemController> CreateAndConnectTescanAsync(string host, int port = 8300, double timeoutSeconds = 30.0, CancellationToken cancellationToken = default)
    {
        ISemController controller = CreateTescan(host, port, timeoutSeconds);
        await controller.ConnectAsync(cancellationToken);
        return controller;
    }
    
    public static async Task<ISemController> CreateAndConnectThermoAsync(string host = "localhost", int port = 7520, CancellationToken cancellationToken = default)
    {
        ISemController controller = CreateThermo(host, port);
        await controller.ConnectAsync(cancellationToken);
        return controller;
    }
}
