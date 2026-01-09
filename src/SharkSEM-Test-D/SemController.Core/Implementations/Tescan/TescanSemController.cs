// =============================================================================
// TescanSemController.cs - Main TESCAN SEM Controller
// =============================================================================
// Implements the ISemController interface for TESCAN Scanning Electron Microscopes
// using the SharkSEM protocol (TCP-based binary message format).
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ SHARKSEM PROTOCOL OVERVIEW                                                  │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │ SharkSEM uses a simple request/response protocol over TCP:                  │
// │                                                                             │
// │   Client                                Server (SEM)                        │
// │     │                                      │                                │
// │     │──── [32-byte Header] ───────────────>│                                │
// │     │──── [Body: 0..N bytes] ─────────────>│                                │
// │     │                                      │                                │
// │     │<─── [32-byte Header] ────────────────│                                │
// │     │<─── [Body: 0..N bytes] ──────────────│                                │
// │     │                                      │                                │
// │                                                                             │
// │ Two TCP connections are used:                                               │
// │   - Control Channel (port 8300): Commands and responses                     │
// │   - Data Channel (port 8301): Image data during acquisition                 │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ MESSAGE HEADER FORMAT (32 bytes)                                            │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │   Offset   Size   Field         Description                                 │
// │   ──────   ────   ─────         ───────────                                 │
// │   0        16     CommandName   ASCII command name, null-padded             │
// │   16       4      BodySize      Size of body in bytes (uint32, LE)          │
// │   20       4      MessageId     Unique message ID (uint32, LE)              │
// │   24       2      Flags         Bit flags (uint16, LE)                      │
// │   26       2      Queue         Queue ID for priority (uint16, LE)          │
// │   28       4      Reserved      Unused, set to 0                            │
// │                                                                             │
// │   Flag Values:                                                              │
// │     0x0001 = FlagSendResponse  - Request a response from server             │
// │     0x0100 = WaitFlagScan      - Wait for scan to complete                  │
// │     0x0200 = WaitFlagStage     - Wait for stage movement to complete        │
// │     0x0400 = WaitFlagOptics    - Wait for optics to stabilize               │
// │     0x0800 = WaitFlagAuto      - Wait for auto-procedure to complete        │
// │                                                                             │
// │   Wait flags cause the server to delay response until the operation         │
// │   is complete. This is CRITICAL for commands like SetBeamCurrent where      │
// │   the optics need time to stabilize.                                        │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ BODY DATA ENCODING                                                          │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │ All body data must be 4-byte aligned. The Pad4() function handles this.     │
// │                                                                             │
// │ INTEGER (4 bytes):                                                          │
// │   - Direct little-endian 32-bit signed integer                              │
// │   - Example: 42 → [0x2A, 0x00, 0x00, 0x00]                                  │
// │                                                                             │
// │ FLOAT/DOUBLE (variable, 4-byte aligned):                                    │
// │   - NOT IEEE binary format! Encoded as null-terminated ASCII string         │
// │   - Format: [4-byte length][ASCII string with null][padding to 4-byte]      │
// │   - Example: 3.14159 →                                                      │
// │       [0x08, 0x00, 0x00, 0x00] (length = 8, includes null terminator)       │
// │       [0x33, 0x2E, 0x31, 0x34, 0x31, 0x35, 0x39, 0x00] ("3.14159\0")        │
// │   - Padding added to reach next 4-byte boundary                             │
// │                                                                             │
// │ STRING (variable, 4-byte aligned):                                          │
// │   - Same format as float: [4-byte length][ASCII string with null][padding]  │
// │   - Length includes the null terminator                                     │
// │                                                                             │
// │ PROPERTY MAPS:                                                              │
// │   - Many enumeration commands return property maps as strings               │
// │   - Format: "prefix.N.property=value\n" (one line per property)             │
// │   - Example: "mode.0.name=Resolution\nmode.1.name=Depth\n"                  │
// │   - IMPORTANT: Prefixes are shortened (geom., cen., mode.) not full words   │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
//
// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ DATA CHANNEL SETUP (Image Acquisition)                                      │
// ├─────────────────────────────────────────────────────────────────────────────┤
// │                                                                             │
// │ The data channel receives image data during acquisition. Setup sequence:    │
// │                                                                             │
// │   1. BIND: Create TcpClient and bind to local port (any available)          │
// │      _dataClient.Client.Bind(IPEndPoint(IPAddress.Any, 0))                  │
// │                                                                             │
// │   2. REGISTER: Tell server which port we're listening on                    │
// │      Send "TcpRegDataPort" command with local port number                   │
// │                                                                             │
// │   3. CONNECT: Connect to server's data port (control port + 1)              │
// │      _dataClient.ConnectAsync(_host, _port + 1)                             │
// │                                                                             │
// │   ORDER IS CRITICAL! If you connect before registering, or register         │
// │   before binding, the data channel will not work.                           │
// │                                                                             │
// │   Image data arrives as "ScData" messages with progressive pixel chunks:    │
// │     Offset 0-3:  Unknown (reserved)                                         │
// │     Offset 4-7:  Channel number                                             │
// │     Offset 8-11: Pixel index (where this chunk starts)                      │
// │     Offset 12-15: Bits per pixel (8 or 16)                                  │
// │     Offset 16-19: Data size (number of bytes in this chunk)                 │
// │     Offset 20+:  Pixel data                                                 │
// │                                                                             │
// └─────────────────────────────────────────────────────────────────────────────┘
//
// Architecture:
// - Uses composition pattern with sub-modules for organized functionality:
//   Stage, Vacuum, HighVoltage, Optics, Scanning, Detectors, ImageGeometry, Misc
// - All sub-modules receive reference to this controller for protocol access
// - Control channel on port 8300 (configurable), data channel on port+1
//
// =============================================================================

using System.Globalization;
using System.Net.Sockets;
using System.Text;
using SemController.Core.Interfaces;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Main controller for TESCAN SEMs implementing the SharkSEM protocol.
/// Manages TCP connections, message encoding/decoding, and coordinates sub-modules.
/// </summary>
public class TescanSemController : ISemController
{
    // =========================================================================
    // Connection State
    // =========================================================================
    
    private readonly string _host;
    private readonly int _port;
    private readonly double _timeoutSeconds;
    
    /// <summary>Control channel TCP client (port 8300 by default).</summary>
    private TcpClient? _client;
    /// <summary>Control channel network stream for reading/writing.</summary>
    private NetworkStream? _stream;
    
    /// <summary>Data channel TCP client (port 8301 by default).</summary>
    private TcpClient? _dataClient;
    /// <summary>Data channel network stream for receiving image data.</summary>
    private NetworkStream? _dataStream;
    
    private bool _disposed;
    /// <summary>Auto-incrementing message ID included in each request header.</summary>
    private uint _messageId;
    /// <summary>Tracks whether data channel has been registered with TcpRegDataPort.</summary>
    private bool _dataChannelRegistered;
    
    // =========================================================================
    // Protocol Constants
    // =========================================================================
    
    /// <summary>Culture-invariant number formatting for float encoding.</summary>
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    
    /// <summary>
    /// SharkSEM message header size: 32 bytes.
    /// Layout: [16 bytes command][4 bytes body size][4 bytes msg ID][2 bytes flags][2 bytes queue][4 bytes reserved]
    /// </summary>
    private const int HeaderSize = 32;
    
    /// <summary>Maximum command name length in header (including null terminator).</summary>
    internal const int CommandNameSizeInternal = 16;
    
    /// <summary>
    /// Flag bit 0x0001: Request server to send a response.
    /// If not set, server executes command but sends no response (fire-and-forget).
    /// </summary>
    private const ushort FlagSendResponse = 0x0001;
    
    // -------------------------------------------------------------------------
    // Wait Flags - Cause server to delay response until operation completes
    // -------------------------------------------------------------------------
    
    /// <summary>Wait for scanning operation to complete before responding.</summary>
    private const ushort WaitFlagScan = 0x0100;
    
    /// <summary>Wait for stage movement to complete before responding.</summary>
    private const ushort WaitFlagStage = 0x0200;
    
    /// <summary>
    /// Wait for electron optics to stabilize before responding.
    /// CRITICAL for SetBeamCurrent - without this, subsequent operations may fail.
    /// </summary>
    internal const ushort WaitFlagOpticsInternal = 0x0400;
    
    /// <summary>Wait for automatic procedure (AutoWD, AutoSignal) to complete.</summary>
    internal const ushort WaitFlagAutoInternal = 0x0800;
    
    // =========================================================================
    // Connection Status
    // =========================================================================
    
    /// <summary>Returns true if control channel TCP connection is active.</summary>
    public bool IsConnected => _client?.Connected ?? false;
    
    /// <summary>Timeout value exposed to sub-modules for data channel operations.</summary>
    internal double TimeoutSeconds => _timeoutSeconds;
    
    /// <summary>Protocol version string as returned by TcpGetVersion (e.g., "3.2.20").</summary>
    public string ProtocolVersionString { get; private set; } = "";
    
    /// <summary>Parsed protocol version for comparison against command requirements.</summary>
    public Version? ProtocolVersion { get; private set; }
    
    // =========================================================================
    // Protocol Version Checking
    // =========================================================================
    // Different SharkSEM commands were introduced in different protocol versions.
    // This dictionary maps command names to their minimum required version.
    // Before sending a command, we check if the connected SEM supports it.
    
    private static readonly Dictionary<string, Version> CommandMinVersions = new()
    {
        // TCP/Connection commands
        ["TcpGetVersion"] = new Version(1, 0, 5),
        ["TcpGetModel"] = new Version(3, 2, 20),
        ["TcpGetDevice"] = new Version(2, 0, 3),
        ["TcpGetSWVersion"] = new Version(2, 0, 9),
        ["TcpRegDataPort"] = new Version(1, 0, 0),
        
        // Vacuum commands
        ["VacGetStatus"] = new Version(1, 0, 0),
        ["VacGetPressure"] = new Version(1, 0, 0),
        ["VacGetVPMode"] = new Version(1, 0, 0),
        ["VacPump"] = new Version(1, 0, 0),
        ["VacVent"] = new Version(1, 0, 0),
        
        // High Voltage / Beam commands
        ["HVGetBeam"] = new Version(1, 0, 0),
        ["HVBeamOn"] = new Version(1, 0, 0),
        ["HVBeamOff"] = new Version(1, 0, 0),
        ["HVGetVoltage"] = new Version(1, 0, 0),
        ["HVSetVoltage"] = new Version(1, 0, 0),
        ["HVGetEmission"] = new Version(1, 0, 0),
        
        // Stage commands
        ["StgGetPosition"] = new Version(1, 0, 0),
        ["StgMoveTo"] = new Version(1, 0, 0),
        ["StgMove"] = new Version(1, 0, 0),
        ["StgIsBusy"] = new Version(1, 0, 0),
        ["StgStop"] = new Version(1, 0, 0),
        ["StgGetLimits"] = new Version(2, 0, 22),    // Added in 2.0.22, not in 2.0.21!
        ["StgGetMotorized"] = new Version(2, 0, 22), // Added in 2.0.22
        ["StgCalibrate"] = new Version(1, 0, 0),
        ["StgIsCalibrated"] = new Version(1, 0, 0),
        
        // Electron Optics commands
        ["GetViewField"] = new Version(1, 0, 0),
        ["SetViewField"] = new Version(1, 0, 0),
        ["GetWD"] = new Version(1, 0, 0),
        ["SetWD"] = new Version(1, 0, 0),
        ["AutoWD"] = new Version(1, 0, 0),
        ["GetSpotSize"] = new Version(1, 0, 0),
        ["GetIAbsorbed"] = new Version(1, 0, 0),
        
        // Scanning Mode commands
        ["SMEnumModes"] = new Version(1, 0, 0),
        ["SMGetMode"] = new Version(1, 0, 0),
        ["SMSetMode"] = new Version(1, 0, 0),
        ["SMGetPivotPos"] = new Version(2, 0, 22),
        
        // Scanning commands
        ["ScGetSpeed"] = new Version(1, 0, 0),
        ["ScSetSpeed"] = new Version(1, 0, 0),
        ["ScGetBlanker"] = new Version(1, 0, 0),
        ["ScSetBlanker"] = new Version(1, 0, 0),
        ["ScStopScan"] = new Version(1, 0, 0),
        ["ScScanXY"] = new Version(1, 0, 0),
        ["ScEnumSpeeds"] = new Version(3, 1, 14),  // Added in later version
        
        // GUI commands
        ["GUIGetScanning"] = new Version(1, 0, 5),
        ["GUISetScanning"] = new Version(1, 0, 5),
        ["GUIGetCurrDets"] = new Version(3, 2, 20),
        ["GUIResetLUT"] = new Version(3, 2, 20),
        ["GUISetLiveAS"] = new Version(3, 2, 20),
        
        // Detector commands
        ["DtEnumDetectors"] = new Version(1, 0, 0),
        ["DtGetChannels"] = new Version(1, 0, 0),
        ["DtGetSelected"] = new Version(1, 0, 0),
        ["DtSelect"] = new Version(1, 0, 0),
        ["DtGetEnabled"] = new Version(1, 0, 11),
        ["DtEnable"] = new Version(1, 0, 0),
        ["DtAutoSignal"] = new Version(1, 0, 11),
        ["DtGetSESuitable"] = new Version(3, 1, 20),
        ["DtStateEnum"] = new Version(3, 1, 1),
        ["DtStateGet"] = new Version(3, 1, 1),
        ["DtStateSet"] = new Version(3, 1, 1),
        
        // Image Geometry commands - per SharkSEM API v2.0.22 manual
        ["EnumGeometries"] = new Version(1, 0, 5),
        ["GetGeometry"] = new Version(1, 0, 5),
        ["SetGeometry"] = new Version(1, 0, 5),
        ["GetGeomLimits"] = new Version(2, 0, 22),   // Added in 2.0.22, not in 2.0.21!
        ["GetImageShift"] = new Version(1, 0, 0),
        ["SetImageShift"] = new Version(1, 0, 0),
        ["EnumCenterings"] = new Version(1, 0, 5),
        ["GetCentering"] = new Version(1, 0, 5),
        ["SetCentering"] = new Version(1, 0, 5),
    };
    
    /// <summary>
    /// Checks if the connected SEM's protocol version supports a given command.
    /// Returns true if supported (or version unknown), false if definitely unsupported.
    /// Logs warning when command is skipped due to version mismatch.
    /// </summary>
    internal bool CheckVersionSupport(string commandName)
    {
        // If we don't know the protocol version, assume command is supported
        if (ProtocolVersion == null)
        {
            return true;
        }
        
        if (CommandMinVersions.TryGetValue(commandName, out Version? minVersion))
        {
            if (ProtocolVersion < minVersion)
            {
                Console.WriteLine($"[Version Check] Command '{commandName}' requires protocol version {minVersion} or later, but current version is {ProtocolVersionString}. Skipping call.");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Queries and parses the protocol version from the SEM.
    /// Called during ConnectAsync to enable version checking for subsequent commands.
    /// 
    /// Note: Older protocol versions may return additional data (like device info)
    /// concatenated in the response. We extract only the version portion which
    /// matches the pattern "X.Y.Z" (digits and dots only).
    /// </summary>
    private async Task FetchProtocolVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            // skipVersionCheck=true because we can't check version before we know version!
            byte[] response = await SendCommandInternalAsync("TcpGetVersion", null, cancellationToken, skipVersionCheck: true);
            if (response.Length > 0)
            {
                int offset = 0;
                string rawVersionString = DecodeStringInternal(response, ref offset);
                
                // Extract only the version portion (digits and dots)
                // Older protocols may append device info like "2.0.210300USB\VID_..."
                // We want just "2.0.21"
                int endIndex = 0;
                for (int i = 0; i < rawVersionString.Length; i++)
                {
                    char c = rawVersionString[i];
                    if (char.IsDigit(c) || c == '.')
                    {
                        endIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                
                ProtocolVersionString = endIndex > 0 ? rawVersionString.Substring(0, endIndex) : rawVersionString;
                
                // Parse version string like "3.2.20" into Version object
                string[] parts = ProtocolVersionString.Split('.');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0], out int major) &&
                    int.TryParse(parts[1], out int minor) &&
                    int.TryParse(parts[2], out int build))
                {
                    ProtocolVersion = new Version(major, minor, build);
                }
                else if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out major) &&
                    int.TryParse(parts[1], out minor))
                {
                    ProtocolVersion = new Version(major, minor, 0);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Could not retrieve protocol version: {ex.Message}");
        }
    }
    
    // =========================================================================
    // Sub-Module Properties
    // =========================================================================
    // Each sub-module handles a specific functional area of the SEM.
    // They receive a reference to this controller to access protocol methods.
    
    public TescanSemStage Stage { get; }
    public TescanSemDetectors Detectors { get; }
    public TescanSemHighVoltage HighVoltage { get; }
    public TescanSemElectronOptics Optics { get; }
    public TescanSemScanning Scanning { get; }
    public TescanSemVacuum Vacuum { get; }
    public TescanSemMisc Misc { get; }
    public TescanSemImageGeometry ImageGeometry { get; }
    
    // =========================================================================
    // Constructor
    // =========================================================================
    
    /// <summary>
    /// Creates a new TESCAN SEM controller.
    /// </summary>
    /// <param name="host">IP address or hostname of the SEM.</param>
    /// <param name="port">Control channel port (default 8300). Data channel is port+1.</param>
    /// <param name="timeoutSeconds">TCP read/write timeout in seconds.</param>
    public TescanSemController(string host, int port = 8300, double timeoutSeconds = 30.0)
    {
        _host = host;
        _port = port;
        _timeoutSeconds = timeoutSeconds;
        
        // Initialize all sub-modules with reference to this controller
        Stage = new TescanSemStage(this);
        Detectors = new TescanSemDetectors(this);
        HighVoltage = new TescanSemHighVoltage(this);
        Optics = new TescanSemElectronOptics(this);
        Scanning = new TescanSemScanning(this);
        Vacuum = new TescanSemVacuum(this);
        Misc = new TescanSemMisc(this);
        ImageGeometry = new TescanSemImageGeometry(this);
    }
    
    public TescanSemController(SemConnectionSettings settings)
        : this(settings.Host, settings.Port, settings.TimeoutSeconds)
    {
    }
    
    // =========================================================================
    // Connection Management
    // =========================================================================
    
    /// <summary>
    /// Establishes TCP connection to the SEM control channel.
    /// 
    /// Protocol flow:
    /// 1. Create TcpClient with configured timeouts
    /// 2. Connect to host:port (default 8300)
    /// 3. Immediately query TcpGetVersion to:
    ///    - Verify connection is working
    ///    - Determine protocol version for command compatibility checking
    /// 
    /// The data channel (for image acquisition) is NOT established here.
    /// It's set up on-demand by EnsureDataChannelInternalAsync when needed.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        
        // Create TCP client with configured timeouts
        _client = new TcpClient();
        _client.ReceiveTimeout = (int)(_timeoutSeconds * 1000);
        _client.SendTimeout = (int)(_timeoutSeconds * 1000);

        // Connect to control channel with comprehensive exception handling
        // Network failures are common: wrong IP, firewall blocking, SEM not running SharkSEM server
        try
        {
            await _client.ConnectAsync(_host, _port, cancellationToken);
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // Network-level failure (connection refused, host unreachable, etc.)
            // Clean up the TcpClient before re-throwing
            _client?.Dispose();
            _client = null;
            throw new InvalidOperationException(
                $"Network error connecting to TESCAN SEM at {_host}:{_port}. " +
                $"Ensure the microscope is powered on, SharkSEM server is running, " +
                $"and no firewall is blocking the connection. Socket error: {ex.SocketErrorCode}", ex);
        }
        catch (OperationCanceledException)
        {
            // Connection was cancelled (e.g., timeout or user cancellation)
            // Clean up and let cancellation propagate normally
            _client?.Dispose();
            _client = null;
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected error - clean up and re-throw with context
            _client?.Dispose();
            _client = null;
            throw new InvalidOperationException(
                $"Failed to connect to TESCAN SEM at {_host}:{_port}: {ex.Message}", ex);
        }
        
        _stream = _client.GetStream();
        
        // Reset data channel state
        _dataChannelRegistered = false;
        
        // Query protocol version for command compatibility checking
        await FetchProtocolVersionAsync(cancellationToken);
        
        if (ProtocolVersion != null)
        {
            Console.WriteLine($"Connected to SEM with protocol version {ProtocolVersionString}");
        }
    }
    
    /// <summary>
    /// Establishes the data channel for image acquisition.
    /// 
    /// CRITICAL: The setup sequence MUST be performed in this exact order:
    /// 
    /// 1. BIND - Create TcpClient and bind to a local port
    ///    _dataClient.Client.Bind(IPEndPoint(IPAddress.Any, 0))
    ///    The OS assigns an available port. We need to know this port number.
    /// 
    /// 2. REGISTER - Tell the SEM which port we're listening on
    ///    Send "TcpRegDataPort" command with our local port number.
    ///    This tells the SEM where to send image data.
    /// 
    /// 3. CONNECT - Connect to the SEM's data port
    ///    Connect to host:(control_port + 1), typically 8301.
    ///    Now the bidirectional data channel is established.
    /// 
    /// Why this order matters:
    /// - If you connect before registering, the SEM doesn't know your port
    /// - If you register before binding, you don't know what port to register
    /// - The SEM uses your registered port to identify your data connection
    /// 
    /// This method is idempotent - safe to call multiple times.
    /// </summary>
    internal async Task EnsureDataChannelInternalAsync(CancellationToken cancellationToken)
    {
        // Already connected and registered? Nothing to do.
        if (_dataClient?.Connected == true && _dataChannelRegistered)
            return;
        
        // Data channel setup requires precise sequence: bind → register → connect
        // Any failure requires cleanup to allow retry
        try
        {
            // Step 1: Create TCP client and bind to local port
            _dataClient = new TcpClient();
            _dataClient.ReceiveTimeout = (int)(_timeoutSeconds * 1000);
            _dataClient.SendTimeout = (int)(_timeoutSeconds * 1000);
            
            // Bind to any available local port (OS assigns one)
            _dataClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0));
            
            // Get the port number that was assigned
            int localPort = ((System.Net.IPEndPoint)_dataClient.Client.LocalEndPoint!).Port;
            
            // Step 2: Register our port with the SEM via control channel
            // This tells the SEM where to send image data
            byte[] regBody = EncodeIntInternal(localPort);
            await SendCommandInternalAsync("TcpRegDataPort", regBody, cancellationToken);
            
            // Step 3: Connect to SEM's data port (control port + 1)
            await _dataClient.ConnectAsync(_host, _port + 1, cancellationToken);
            _dataStream = _dataClient.GetStream();
            
            _dataChannelRegistered = true;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // Network error during data channel setup - clean up and report
            CloseDataChannel();
            throw new InvalidOperationException(
                $"Failed to establish data channel to TESCAN SEM at {_host}:{_port + 1}. " +
                $"Socket error: {ex.SocketErrorCode}. Image acquisition will not be available.", ex);
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested - clean up and propagate
            CloseDataChannel();
            throw;
        }
        catch (IOException ex)
        {
            // I/O error during TcpRegDataPort command
            CloseDataChannel();
            throw new InvalidOperationException(
                $"Communication error while registering data port with SEM: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Closes and cleans up the data channel resources.
    /// Called on data channel errors to allow retry.
    /// </summary>
    private void CloseDataChannel()
    {
        _dataStream?.Close();
        _dataClient?.Close();
        _dataStream = null;
        _dataClient = null;
        _dataChannelRegistered = false;
    }
    
    /// <summary>
    /// Disconnects from the SEM, closing both control and data channels.
    /// </summary>
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        // Close data channel first
        _dataStream?.Close();
        _dataClient?.Close();
        _dataStream = null;
        _dataClient = null;
        _dataChannelRegistered = false;
        
        // Close control channel
        _stream?.Close();
        _client?.Close();
        _stream = null;
        _client = null;
        return Task.CompletedTask;
    }
    
    // =========================================================================
    // Message Header Construction
    // =========================================================================
    
    /// <summary>
    /// Builds a 32-byte SharkSEM message header.
    /// 
    /// Header Layout (32 bytes total):
    /// ┌──────────────────────────────────────────────────────────────────────┐
    /// │ Offset │ Size │ Field       │ Description                            │
    /// ├────────┼──────┼─────────────┼────────────────────────────────────────┤
    /// │ 0      │ 16   │ CommandName │ ASCII, null-terminated, null-padded    │
    /// │ 16     │ 4    │ BodySize    │ uint32, little-endian                  │
    /// │ 20     │ 4    │ MessageId   │ uint32, little-endian, auto-increment  │
    /// │ 24     │ 2    │ Flags       │ uint16, little-endian                  │
    /// │ 26     │ 2    │ Queue       │ uint16, little-endian (usually 0)      │
    /// │ 28     │ 4    │ Reserved    │ Set to 0                               │
    /// └──────────────────────────────────────────────────────────────────────┘
    /// 
    /// The MessageId is automatically incremented for each message sent.
    /// This allows matching responses to requests (though we typically
    /// process them synchronously anyway).
    /// </summary>
    /// <param name="command">Command name (max 15 chars).</param>
    /// <param name="bodySize">Size of body that follows header.</param>
    /// <param name="flags">Flag bits (FlagSendResponse, WaitFlags, etc.).</param>
    /// <param name="queue">Queue ID for priority (usually 0).</param>
    /// <returns>32-byte header ready to send.</returns>
    private byte[] BuildHeader(string command, uint bodySize, ushort flags = FlagSendResponse, ushort queue = 0)
    {
        byte[] header = new byte[HeaderSize];
        
        // Bytes 0-15: Command name (ASCII, null-padded)
        byte[] cmdBytes = Encoding.ASCII.GetBytes(command);
        int cmdLen = Math.Min(cmdBytes.Length, CommandNameSizeInternal - 1);
        Array.Copy(cmdBytes, 0, header, 0, cmdLen);
        // Remaining bytes already 0 from array initialization
        
        // Bytes 16-19: Body size (uint32, little-endian)
        BitConverter.GetBytes(bodySize).CopyTo(header, 16);
        
        // Bytes 20-23: Message ID (uint32, little-endian, auto-increment)
        BitConverter.GetBytes(++_messageId).CopyTo(header, 20);
        
        // Bytes 24-25: Flags (uint16, little-endian)
        BitConverter.GetBytes(flags).CopyTo(header, 24);
        
        // Bytes 26-27: Queue (uint16, little-endian)
        BitConverter.GetBytes(queue).CopyTo(header, 26);
        
        // Bytes 28-31: Reserved (already 0)
        
        return header;
    }
    
    // =========================================================================
    // Data Encoding Utilities
    // =========================================================================
    
    /// <summary>
    /// Pads a size up to the next 4-byte boundary.
    /// SharkSEM requires all body data to be 4-byte aligned.
    /// 
    /// Examples:
    ///   Pad4(1) = 4
    ///   Pad4(4) = 4
    ///   Pad4(5) = 8
    ///   Pad4(8) = 8
    /// </summary>
    private static int Pad4(int size) => (size + 3) & ~3;
    
    /// <summary>
    /// Encodes a 32-bit signed integer for SharkSEM body.
    /// Direct little-endian encoding, 4 bytes.
    /// </summary>
    internal static byte[] EncodeIntInternal(int value)
    {
        return BitConverter.GetBytes(value);
    }
    
    /// <summary>
    /// Encodes a 32-bit unsigned integer for SharkSEM body.
    /// Direct little-endian encoding, 4 bytes.
    /// </summary>
    internal static byte[] EncodeUIntInternal(uint value)
    {
        return BitConverter.GetBytes(value);
    }
    
    /// <summary>
    /// Encodes a floating-point value for SharkSEM body.
    /// 
    /// IMPORTANT: SharkSEM does NOT use IEEE binary float encoding!
    /// Instead, floats are encoded as null-terminated ASCII strings:
    /// 
    /// Format:
    /// ┌─────────────────────────────────────────────────────────────────────┐
    /// │ [4 bytes: length] [N bytes: ASCII string with null] [padding]      │
    /// └─────────────────────────────────────────────────────────────────────┘
    /// 
    /// Example: Encoding 3.14159
    ///   1. Convert to string: "3.14159"
    ///   2. Add null terminator: "3.14159\0" (8 bytes)
    ///   3. Length prefix: 0x08 0x00 0x00 0x00 (8 as uint32 LE)
    ///   4. Total: 4 + 8 = 12 bytes (already 4-byte aligned)
    /// 
    /// This encoding preserves precision and avoids IEEE binary float
    /// portability issues, at the cost of more bytes per value.
    /// </summary>
    internal static byte[] EncodeFloatInternal(double value)
    {
        // Convert to string with null terminator
        string str = value.ToString("G", InvariantCulture) + '\0';
        byte[] strBytes = Encoding.ASCII.GetBytes(str);
        
        // Calculate padded size (4-byte length prefix + string + padding)
        int paddedSize = Pad4(4 + strBytes.Length);
        byte[] result = new byte[paddedSize];
        
        // Write length prefix (includes null terminator in count)
        BitConverter.GetBytes((uint)strBytes.Length).CopyTo(result, 0);
        
        // Write string bytes (including null terminator)
        Array.Copy(strBytes, 0, result, 4, strBytes.Length);
        
        // Padding bytes already 0 from array initialization
        return result;
    }
    
    /// <summary>
    /// Encodes a string value for SharkSEM body.
    /// Same format as EncodeFloatInternal: [length][string+null][padding].
    /// </summary>
    internal static byte[] EncodeStringInternal(string value)
    {
        string str = value + '\0';
        byte[] strBytes = Encoding.ASCII.GetBytes(str);
        int paddedSize = Pad4(4 + strBytes.Length);
        byte[] result = new byte[paddedSize];
        BitConverter.GetBytes((uint)strBytes.Length).CopyTo(result, 0);
        Array.Copy(strBytes, 0, result, 4, strBytes.Length);
        return result;
    }
    
    // =========================================================================
    // Command Sending Methods
    // =========================================================================
    
    /// <summary>
    /// Sends a command and waits for response.
    /// 
    /// Protocol flow:
    /// 1. Check if command is supported by current protocol version
    /// 2. Build 32-byte header with FlagSendResponse set
    /// 3. Send header + body over TCP
    /// 4. Read 32-byte response header
    /// 5. Read response body (size from header bytes 16-19)
    /// 6. Return response body bytes
    /// 
    /// Thread safety: NOT thread-safe. Assumes single-threaded command flow.
    /// TCP reads use a loop to handle partial reads (network fragmentation).
    /// </summary>
    /// <param name="command">SharkSEM command name.</param>
    /// <param name="body">Optional body bytes (already encoded).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="skipVersionCheck">Skip version check (used for TcpGetVersion itself).</param>
    /// <returns>Response body bytes (may be empty).</returns>
    internal async Task<byte[]> SendCommandInternalAsync(string command, byte[]? body, CancellationToken cancellationToken, bool skipVersionCheck = false)
    {
        // Version check (skip for bootstrap commands like TcpGetVersion)
        if (!skipVersionCheck && !CheckVersionSupport(command))
            return Array.Empty<byte>();
        
        // Verify connection is still valid before attempting command
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope. Call ConnectAsync first.");
        
        try
        {
            // Build and send request
            uint bodySize = (uint)(body?.Length ?? 0);
            byte[] header = BuildHeader(command, bodySize);
            
            await _stream.WriteAsync(header, cancellationToken);
            if (body != null && body.Length > 0)
            {
                await _stream.WriteAsync(body, cancellationToken);
            }
            
            // Read response header (loop to handle partial reads)
            byte[] responseHeader = new byte[HeaderSize];
            int bytesRead = 0;
            while (bytesRead < HeaderSize)
            {
                int read = await _stream.ReadAsync(responseHeader.AsMemory(bytesRead, HeaderSize - bytesRead), cancellationToken);
                if (read == 0)
                {
                    // Connection was closed unexpectedly - mark as disconnected
                    MarkDisconnected();
                    throw new IOException($"Connection to SEM closed unexpectedly while executing '{command}'. " +
                        "The microscope may have been restarted or the network connection was lost.");
                }
                bytesRead += read;
            }
            
            // Extract body size from response header (bytes 16-19)
            uint responseBodySize = BitConverter.ToUInt32(responseHeader, 16);
            
            if (responseBodySize == 0)
                return Array.Empty<byte>();
            
            // Read response body (loop to handle partial reads)
            byte[] responseBody = new byte[responseBodySize];
            bytesRead = 0;
            while (bytesRead < responseBodySize)
            {
                int read = await _stream.ReadAsync(responseBody.AsMemory(bytesRead, (int)responseBodySize - bytesRead), cancellationToken);
                if (read == 0)
                {
                    // Connection was closed unexpectedly during body read
                    MarkDisconnected();
                    throw new IOException($"Connection to SEM closed unexpectedly while reading response for '{command}'. " +
                        "Received {bytesRead} of {responseBodySize} bytes.");
                }
                bytesRead += read;
            }
            
            return responseBody;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // Network-level error during command execution
            MarkDisconnected();
            throw new IOException($"Network error during command '{command}': {ex.SocketErrorCode}. " +
                "The connection has been lost.", ex);
        }
        catch (ObjectDisposedException)
        {
            // Stream was disposed (connection already closed)
            MarkDisconnected();
            throw new InvalidOperationException($"Cannot execute command '{command}': connection has been closed.");
        }
    }
    
    /// <summary>
    /// Marks the controller as disconnected and cleans up resources.
    /// Called when communication errors indicate the connection is no longer valid.
    /// </summary>
    private void MarkDisconnected()
    {
        _stream = null;
        _client?.Close();
        _client = null;
        CloseDataChannel();
    }
    
    /// <summary>
    /// Sends a command without expecting a response (fire-and-forget).
    /// 
    /// Used for commands where we don't need confirmation, like:
    /// - VacPump, VacVent (initiate long-running operations)
    /// - GUISetScanning (toggle GUI state)
    /// 
    /// Sets flags=0 (no FlagSendResponse) so server doesn't send response.
    /// This is faster but provides no confirmation of success.
    /// </summary>
    internal async Task<bool> SendCommandNoResponseInternalAsync(string command, byte[]? body, CancellationToken cancellationToken)
    {
        if (!CheckVersionSupport(command))
            return false;
        
        // Verify connection is still valid before attempting command
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope. Call ConnectAsync first.");
        
        try
        {
            uint bodySize = (uint)(body?.Length ?? 0);
            // Note: flags=0 means no response requested
            byte[] header = BuildHeader(command, bodySize, flags: 0);
            
            await _stream.WriteAsync(header, cancellationToken);
            if (body != null && body.Length > 0)
            {
                await _stream.WriteAsync(body, cancellationToken);
            }
            return true;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            // Network-level error during command send
            MarkDisconnected();
            throw new IOException($"Network error sending command '{command}': {ex.SocketErrorCode}. " +
                "The connection has been lost.", ex);
        }
        catch (ObjectDisposedException)
        {
            // Stream was disposed (connection already closed)
            MarkDisconnected();
            throw new InvalidOperationException($"Cannot execute command '{command}': connection has been closed.");
        }
    }
    
    /// <summary>
    /// Sends a command with wait flags and waits for response.
    /// 
    /// Wait flags cause the SEM to delay its response until the operation
    /// is complete. This is CRITICAL for operations that need time:
    /// 
    /// - WaitFlagOptics (0x0400): Used with SetBeamCurrent
    ///   Without this, beam current may not have stabilized when next
    ///   command executes, causing incorrect readings or failures.
    /// 
    /// - WaitFlagAuto (0x0800): Used with AutoWD, AutoSignal
    ///   These are lengthy automatic procedures that may take seconds.
    /// 
    /// - WaitFlagStage (0x0200): Used with stage movements
    ///   Though we typically poll StgIsBusy instead.
    /// 
    /// The server holds the response until all flagged operations complete.
    /// </summary>
    internal async Task<byte[]> SendCommandWithWaitInternalAsync(string command, byte[]? body, ushort waitFlags, CancellationToken cancellationToken)
    {
        if (!CheckVersionSupport(command))
            return Array.Empty<byte>();
        
        if (_stream == null)
            throw new InvalidOperationException("Not connected to microscope");
        
        uint bodySize = (uint)(body?.Length ?? 0);
        // Combine FlagSendResponse with wait flags
        ushort flags = (ushort)(FlagSendResponse | waitFlags);
        byte[] header = BuildHeader(command, bodySize, flags);
        
        await _stream.WriteAsync(header, cancellationToken);
        if (body != null && body.Length > 0)
        {
            await _stream.WriteAsync(body, cancellationToken);
        }
        
        // Read response (may take a long time with wait flags!)
        byte[] responseHeader = new byte[HeaderSize];
        int bytesRead = 0;
        while (bytesRead < HeaderSize)
        {
            int read = await _stream.ReadAsync(responseHeader.AsMemory(bytesRead, HeaderSize - bytesRead), cancellationToken);
            if (read == 0) throw new IOException("Connection closed by server");
            bytesRead += read;
        }
        
        uint responseBodySize = BitConverter.ToUInt32(responseHeader, 16);
        if (responseBodySize == 0)
            return Array.Empty<byte>();
        
        byte[] responseBody = new byte[responseBodySize];
        bytesRead = 0;
        while (bytesRead < (int)responseBodySize)
        {
            int read = await _stream.ReadAsync(responseBody.AsMemory(bytesRead, (int)responseBodySize - bytesRead), cancellationToken);
            if (read == 0) throw new IOException("Connection closed by server");
            bytesRead += read;
        }
        
        return responseBody;
    }
    
    // =========================================================================
    // Data Decoding Utilities
    // =========================================================================
    
    /// <summary>
    /// Decodes a 32-bit signed integer from response body.
    /// Direct little-endian decoding.
    /// </summary>
    internal static int DecodeIntInternal(byte[] body, int offset)
    {
        return BitConverter.ToInt32(body, offset);
    }
    
    /// <summary>
    /// Decodes a floating-point value from response body.
    /// 
    /// Reverses the encoding from EncodeFloatInternal:
    /// 1. Read 4-byte length prefix
    /// 2. Read ASCII string (length-1 to exclude null)
    /// 3. Parse string as double
    /// 4. Advance offset past padded length
    /// 
    /// The offset parameter is ref to allow sequential decoding of
    /// multiple values from the same body.
    /// </summary>
    internal static double DecodeFloatInternal(byte[] body, ref int offset)
    {
        // Read length prefix
        uint strLen = BitConverter.ToUInt32(body, offset);
        offset += 4;
        
        // Read string (excluding null terminator)
        string str = Encoding.ASCII.GetString(body, offset, (int)strLen - 1);
        
        // Advance past padded string
        offset += Pad4((int)strLen);
        
        return double.Parse(str, NumberStyles.Float, InvariantCulture);
    }
    
    /// <summary>
    /// Decodes a string value from response body.
    /// Same format as DecodeFloatInternal.
    /// </summary>
    internal static string DecodeStringInternal(byte[] body, ref int offset)
    {
        uint strLen = BitConverter.ToUInt32(body, offset);
        offset += 4;
        string str = Encoding.ASCII.GetString(body, offset, (int)strLen - 1);
        offset += Pad4((int)strLen);
        return str;
    }
    
    // =========================================================================
    // Data Channel Message Handling
    // =========================================================================
    
    /// <summary>
    /// Container for a message read from the data channel.
    /// Used by ReadDataChannelMessageInternalAsync.
    /// </summary>
    internal class DataChannelMessage
    {
        public byte[] Header { get; set; } = Array.Empty<byte>();
        public byte[] Body { get; set; } = Array.Empty<byte>();
    }
    
    /// <summary>
    /// Reads a single message from the data channel.
    /// 
    /// Data channel messages use the same 32-byte header format as control
    /// channel, but typically contain image data ("ScData" command).
    /// 
    /// ScData message body format:
    /// ┌────────────────────────────────────────────────────────────────────┐
    /// │ Offset │ Size │ Field      │ Description                          │
    /// ├────────┼──────┼────────────┼──────────────────────────────────────┤
    /// │ 0      │ 4    │ Unknown    │ Reserved/unused                      │
    /// │ 4      │ 4    │ Channel    │ Detector channel number              │
    /// │ 8      │ 4    │ PixelIndex │ Starting pixel index for this chunk  │
    /// │ 12     │ 4    │ BPP        │ Bits per pixel (8 or 16)             │
    /// │ 16     │ 4    │ DataSize   │ Number of pixel bytes in this chunk  │
    /// │ 20     │ N    │ PixelData  │ Raw pixel values                     │
    /// └────────────────────────────────────────────────────────────────────┘
    /// 
    /// Image data arrives in multiple chunks. The PixelIndex indicates where
    /// each chunk belongs in the final image. Chunks may arrive out of order.
    /// </summary>
    internal async Task<DataChannelMessage?> ReadDataChannelMessageInternalAsync(CancellationToken cancellationToken)
    {
        if (_dataStream == null)
            return null;
        
        try
        {
            // Read 32-byte header
            byte[] header = new byte[HeaderSize];
            int bytesRead = 0;
            while (bytesRead < HeaderSize)
            {
                int read = await _dataStream.ReadAsync(header.AsMemory(bytesRead, HeaderSize - bytesRead), cancellationToken);
                if (read == 0) return null; // Connection closed
                bytesRead += read;
            }
            
            // Extract body size from header
            uint bodySize = BitConverter.ToUInt32(header, 16);
            
            // Read body
            byte[] body;
            if (bodySize > 0)
            {
                body = new byte[bodySize];
                bytesRead = 0;
                while (bytesRead < bodySize)
                {
                    int read = await _dataStream.ReadAsync(body.AsMemory(bytesRead, (int)bodySize - bytesRead), cancellationToken);
                    if (read == 0) return null; // Connection closed
                    bytesRead += read;
                }
            }
            else
            {
                body = Array.Empty<byte>();
            }
            
            return new DataChannelMessage { Header = header, Body = body };
        }
        catch
        {
            return null;
        }
    }
    
    // =========================================================================
    // ISemController Implementation - Delegates to Sub-Modules
    // =========================================================================
    // These methods provide backward compatibility with ISemController interface
    // by delegating to the appropriate sub-module.
    
    #region ISemController Backward Compatibility - Delegates to sub-classes
    
    public Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
        => Misc.GetMicroscopeInfoAsync(cancellationToken);
    
    public Task<VacuumStatus> GetVacuumStatusAsync(CancellationToken cancellationToken = default)
        => Vacuum.GetStatusAsync(cancellationToken);
    
    public Task<double> GetVacuumPressureAsync(VacuumGauge gauge = VacuumGauge.Chamber, CancellationToken cancellationToken = default)
        => Vacuum.GetPressureAsync(gauge, cancellationToken);
    
    public Task<VacuumMode> GetVacuumModeAsync(CancellationToken cancellationToken = default)
        => Vacuum.GetModeAsync(cancellationToken);
    
    public Task PumpAsync(CancellationToken cancellationToken = default)
        => Vacuum.PumpAsync(cancellationToken);
    
    public Task VentAsync(CancellationToken cancellationToken = default)
        => Vacuum.VentAsync(cancellationToken);
    
    public Task<BeamState> GetBeamStateAsync(CancellationToken cancellationToken = default)
        => HighVoltage.GetBeamStateAsync(cancellationToken);
    
    public Task BeamOnAsync(CancellationToken cancellationToken = default)
        => HighVoltage.BeamOnAsync(cancellationToken);
    
    public Task<bool> WaitForBeamOnAsync(int timeoutMs = 30000, CancellationToken cancellationToken = default)
        => HighVoltage.WaitForBeamOnAsync(timeoutMs, cancellationToken);
    
    public Task BeamOffAsync(CancellationToken cancellationToken = default)
        => HighVoltage.BeamOffAsync(cancellationToken);
    
    public Task<double> GetHighVoltageAsync(CancellationToken cancellationToken = default)
        => HighVoltage.GetVoltageAsync(cancellationToken);
    
    public Task SetHighVoltageAsync(double voltage, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => HighVoltage.SetVoltageAsync(voltage, waitForCompletion, cancellationToken);
    
    public Task<double> GetEmissionCurrentAsync(CancellationToken cancellationToken = default)
        => HighVoltage.GetEmissionCurrentAsync(cancellationToken);
    
    public Task<StagePosition> GetStagePositionAsync(CancellationToken cancellationToken = default)
        => Stage.GetPositionAsync(cancellationToken);
    
    public Task MoveStageAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => Stage.MoveToAsync(position, waitForCompletion, cancellationToken);
    
    public Task MoveStageRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
        => Stage.MoveRelativeAsync(delta, waitForCompletion, cancellationToken);
    
    public Task<bool> IsStageMovingAsync(CancellationToken cancellationToken = default)
        => Stage.IsMovingAsync(cancellationToken);
    
    public Task StopStageAsync(CancellationToken cancellationToken = default)
        => Stage.StopAsync(cancellationToken);
    
    public Task<StageLimits> GetStageLimitsAsync(CancellationToken cancellationToken = default)
        => Stage.GetLimitsAsync(cancellationToken);
    
    public Task CalibrateStageAsync(CancellationToken cancellationToken = default)
        => Stage.CalibrateAsync(cancellationToken);
    
    public Task<bool> IsStageCallibratedAsync(CancellationToken cancellationToken = default)
        => Stage.IsCallibratedAsync(cancellationToken);
    
    public Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
        => Optics.GetViewFieldAsync(cancellationToken);
    
    public Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
        => Optics.SetViewFieldAsync(viewFieldMicrons, cancellationToken);
    
    public Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
        => Optics.GetWorkingDistanceAsync(cancellationToken);
    
    public Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
        => Optics.SetWorkingDistanceAsync(workingDistanceMm, cancellationToken);
    
    public Task<double> GetFocusAsync(CancellationToken cancellationToken = default)
        => Optics.GetFocusAsync(cancellationToken);
    
    public Task SetFocusAsync(double focus, CancellationToken cancellationToken = default)
        => Optics.SetFocusAsync(focus, cancellationToken);
    
    public Task AutoFocusAsync(CancellationToken cancellationToken = default)
        => Optics.AutoFocusAsync(cancellationToken);
    
    public Task<int> GetScanSpeedAsync(CancellationToken cancellationToken = default)
        => Scanning.GetSpeedAsync(cancellationToken);
    
    public Task SetScanSpeedAsync(int speed, CancellationToken cancellationToken = default)
        => Scanning.SetSpeedAsync(speed, cancellationToken);
    
    public Task<BlankerMode> GetBlankerModeAsync(CancellationToken cancellationToken = default)
        => Scanning.GetBlankerModeAsync(cancellationToken);
    
    public Task SetBlankerModeAsync(BlankerMode mode, CancellationToken cancellationToken = default)
        => Scanning.SetBlankerModeAsync(mode, cancellationToken);
    
    public Task<string> EnumDetectorsAsync(CancellationToken cancellationToken = default)
        => Detectors.EnumDetectorsAsync(cancellationToken);
    
    public Task<int> GetChannelCountAsync(CancellationToken cancellationToken = default)
        => Detectors.GetChannelCountAsync(cancellationToken);
    
    public Task<int> GetSelectedDetectorAsync(int channel, CancellationToken cancellationToken = default)
        => Detectors.GetSelectedDetectorAsync(channel, cancellationToken);
    
    public Task SelectDetectorAsync(int channel, int detector, CancellationToken cancellationToken = default)
        => Detectors.SelectDetectorAsync(channel, detector, cancellationToken);
    
    public Task<(int enabled, int bpp)> GetChannelEnabledAsync(int channel, CancellationToken cancellationToken = default)
        => Detectors.GetChannelEnabledAsync(channel, cancellationToken);
    
    public Task EnableChannelAsync(int channel, bool enable, int bpp = 8, CancellationToken cancellationToken = default)
        => Detectors.EnableChannelAsync(channel, enable, bpp, cancellationToken);
    
    public Task AutoSignalAsync(int channel, CancellationToken cancellationToken = default)
        => Detectors.AutoSignalAsync(channel, cancellationToken);
    
    public Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
        => Scanning.AcquireImagesAsync(settings, cancellationToken);
    
    public Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
        => Scanning.AcquireSingleImageAsync(channel, width, height, cancellationToken);
    
    public Task StopScanAsync(CancellationToken cancellationToken = default)
        => Scanning.StopScanAsync(cancellationToken);
    
    public Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
        => Optics.GetSpotSizeAsync(cancellationToken);
    
    #endregion
    
    // =========================================================================
    // IDisposable Implementation
    // =========================================================================
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _dataStream?.Dispose();
            _dataClient?.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
        }
        
        _disposed = true;
    }
}
