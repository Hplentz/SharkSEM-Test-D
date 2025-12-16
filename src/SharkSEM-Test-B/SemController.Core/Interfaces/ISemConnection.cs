namespace SemController.Core.Interfaces;

public interface ISemConnection : IDisposable
{
    string Host { get; }
    int Port { get; }
    bool IsConnected { get; }
    double TimeoutSeconds { get; set; }
    
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
