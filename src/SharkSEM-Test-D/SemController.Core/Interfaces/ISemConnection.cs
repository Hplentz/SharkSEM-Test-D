// =============================================================================
// ISemConnection.cs - Low-Level Connection Interface
// =============================================================================
// Defines the basic connection contract for SEM communication channels.
// This interface abstracts the underlying transport layer (TCP, COM, etc.)
// used by vendor-specific implementations.
//
// Currently used by TESCAN implementation for TCP socket management.
// Thermo implementation uses COM-based AutoScript client internally.
// =============================================================================

namespace SemController.Core.Interfaces;

/// <summary>
/// Low-level connection interface for SEM communication.
/// Abstracts transport layer details (TCP, COM, etc.).
/// </summary>
public interface ISemConnection : IDisposable
{
    /// <summary>Host address or machine name for the connection.</summary>
    string Host { get; }
    
    /// <summary>Port number for TCP-based connections.</summary>
    int Port { get; }
    
    /// <summary>Returns true if connection is currently active.</summary>
    bool IsConnected { get; }
    
    /// <summary>Communication timeout in seconds.</summary>
    double TimeoutSeconds { get; set; }
    
    /// <summary>Establishes connection to the remote endpoint.</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Closes the connection and releases resources.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
