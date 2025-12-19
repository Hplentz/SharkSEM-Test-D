using SemController.Core.Models;

namespace SemController.Core.Models;

public class SemConnectionSettings
{
    public SemType Type { get; set; } = SemType.Tescan;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8300;
    public double TimeoutSeconds { get; set; } = 30.0;
    
    public SemConnectionSettings() { }
    
    public SemConnectionSettings(SemType type, string host, int port, double timeoutSeconds = 30.0)
    {
        Type = type;
        Host = host;
        Port = port;
        TimeoutSeconds = timeoutSeconds;
    }
    
    public static SemConnectionSettings Tescan(string host, int port = 8300) =>
        new SemConnectionSettings(SemType.Tescan, host, port);
    
    public static SemConnectionSettings Mock() =>
        new SemConnectionSettings(SemType.Mock, "mock", 0);
}
