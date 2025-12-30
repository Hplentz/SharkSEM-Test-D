// =============================================================================
// TescanSemElectronOptics.cs - TESCAN Electron Optics Control
// =============================================================================
// Manages electron optics for TESCAN microscopes including view field,
// working distance, focus, spot size, beam current, and scanning modes.
//
// Unit Conventions:
// - View field: mm in protocol, converted to µm for interface
// - Working distance: mm (no conversion)
// - Beam current: picoamps (pA)
// - Spot size: vendor-specific units
//
// SharkSEM Commands Used:
// - GetViewField/SetViewField: View field in mm
// - GetWD/SetWD: Working distance in mm
// - AutoWD: Automatic focus
// - GetSpotSize: Current spot size
// - GetBeamCurrent/SetBeamCurrent: Beam current in pA (requires WaitFlagOptics)
// - SMEnumModes/SMGetMode/SMSetMode: Scanning mode control
// - SMGetPivotPos: Pivot position for dynamic focus
// - GetIAbsorbed: Absorbed current measurement
// - EnumPCIndexes/GetPCIndex/SetPCIndex: Probe current index control
// =============================================================================

using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Electron optics control sub-module for TESCAN SEMs.
/// Handles view field, working distance, focus, spot size, and beam current.
/// </summary>
public class TescanSemElectronOptics
{
    private readonly TescanSemController _controller;
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemElectronOptics(TescanSemController controller)
    {
        _controller = controller;
    }
    
    /// <summary>
    /// Gets current view field (horizontal field of view) in micrometers.
    /// SharkSEM returns mm; this method converts to µm.
    /// </summary>
    public async Task<double> GetViewFieldAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetViewField", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            double viewFieldMm = TescanSemController.DecodeFloatInternal(response, ref offset);
            return viewFieldMm * 1000.0; // mm -> µm
        }
        return double.NaN;
    }
    
    /// <summary>
    /// Sets view field in micrometers.
    /// Converts µm to mm for SharkSEM protocol.
    /// </summary>
    public async Task SetViewFieldAsync(double viewFieldMicrons, CancellationToken cancellationToken = default)
    {
        double viewFieldMm = viewFieldMicrons / 1000.0; // µm -> mm
        byte[] body = TescanSemController.EncodeFloatInternal(viewFieldMm);
        await _controller.SendCommandNoResponseInternalAsync("SetViewField", body, cancellationToken);
    }
    
    /// <summary>
    /// Gets current working distance in millimeters.
    /// </summary>
    public async Task<double> GetWorkingDistanceAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetWD", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    /// <summary>
    /// Sets working distance in millimeters.
    /// </summary>
    public async Task SetWorkingDistanceAsync(double workingDistanceMm, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeFloatInternal(workingDistanceMm);
        await _controller.SendCommandNoResponseInternalAsync("SetWD", body, cancellationToken);
    }
    
    /// <summary>
    /// Gets current focus (equivalent to working distance for TESCAN).
    /// </summary>
    public async Task<double> GetFocusAsync(CancellationToken cancellationToken = default)
    {
        return await GetWorkingDistanceAsync(cancellationToken);
    }
    
    /// <summary>
    /// Sets focus (equivalent to working distance for TESCAN).
    /// </summary>
    public async Task SetFocusAsync(double focus, CancellationToken cancellationToken = default)
    {
        await SetWorkingDistanceAsync(focus, cancellationToken);
    }
    
    /// <summary>
    /// Runs automatic working distance (autofocus) procedure.
    /// Uses wait flags to block until complete.
    /// </summary>
    public async Task AutoFocusAsync(CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(0); // Standard autofocus mode
        await _controller.SendCommandWithWaitInternalAsync("AutoWD", body, TescanSemController.WaitFlagOpticsInternal | TescanSemController.WaitFlagAutoInternal, cancellationToken);
    }
    
    /// <summary>
    /// Gets current spot size (probe diameter) in vendor-specific units.
    /// </summary>
    public async Task<double> GetSpotSizeAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetSpotSize", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    /// <summary>
    /// Gets current beam current in picoamps (pA).
    /// </summary>
    public async Task<double> GetBeamCurrentAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetBeamCurrent", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    /// <summary>
    /// Sets beam current in picoamps (pA).
    /// CRITICAL: Uses WaitFlagOptics to ensure optics stabilize before returning.
    /// </summary>
    public async Task SetBeamCurrentAsync(double beamCurrentPicoamps, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeFloatInternal(beamCurrentPicoamps);
        await _controller.SendCommandWithWaitInternalAsync("SetBeamCurrent", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    /// <summary>
    /// Enumerates available probe current indices.
    /// Returns property-map string describing available PC settings.
    /// </summary>
    public async Task<string> EnumPCIndexesAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("EnumPCIndexes", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeStringInternal(response, ref offset);
        }
        return string.Empty;
    }
    
    /// <summary>
    /// Gets current probe current index.
    /// </summary>
    public async Task<int> GetPCIndexAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetPCIndex", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return -1;
    }
    
    /// <summary>
    /// Sets probe current index.
    /// Uses WaitFlagOptics to wait for optics stabilization.
    /// </summary>
    public async Task SetPCIndexAsync(int index, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(index);
        await _controller.SendCommandWithWaitInternalAsync("SetPCIndex", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    /// <summary>
    /// Gets absorbed current measurement from Faraday cup in picoamps.
    /// SharkSEM Command: GetIAbsorbed (no body)
    /// Response: Float (ASCII-encoded absorbed current value)
    /// </summary>
    public async Task<double> GetAbsorbedCurrentAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("GetIAbsorbed", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            return TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        return double.NaN;
    }
    
    // -------------------------------------------------------------------------
    // Scanning Mode Control
    // -------------------------------------------------------------------------
    // TESCAN microscopes support multiple scanning modes (Resolution, Depth, Field, etc.)
    // Response format uses property-map strings with pattern: mode.N.name=ModeName
    
    /// <summary>
    /// Enumerates available scanning modes.
    /// Parses property-map response to extract mode index and name.
    /// </summary>
    public async Task<List<ScanningMode>> EnumScanningModesAsync(CancellationToken cancellationToken = default)
    {
        List<ScanningMode> modes = new List<ScanningMode>();
        byte[] response = await _controller.SendCommandInternalAsync("SMEnumModes", null, cancellationToken);
        if (response.Length > 0)
        {
            int offset = 0;
            string modeMap = TescanSemController.DecodeStringInternal(response, ref offset);
            
            // Parse property-map format: mode.N.name=ModeName
            foreach (string line in modeMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].StartsWith("mode.") && parts[0].EndsWith(".name"))
                {
                    string indexStr = parts[0].Replace("mode.", "").Replace(".name", "");
                    if (int.TryParse(indexStr, out int index))
                    {
                        modes.Add(new ScanningMode(index, parts[1].Trim()));
                    }
                }
            }
        }
        return modes;
    }
    
    /// <summary>
    /// Gets current scanning mode index.
    /// </summary>
    public async Task<int> GetScanningModeAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("SMGetMode", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0);
        }
        return -1;
    }
    
    /// <summary>
    /// Sets scanning mode by index.
    /// Uses WaitFlagOptics to wait for mode change to complete.
    /// </summary>
    public async Task SetScanningModeAsync(int modeIndex, CancellationToken cancellationToken = default)
    {
        byte[] body = TescanSemController.EncodeIntInternal(modeIndex);
        await _controller.SendCommandWithWaitInternalAsync("SMSetMode", body, TescanSemController.WaitFlagOpticsInternal, cancellationToken);
    }
    
    /// <summary>
    /// Gets pivot position for dynamic focus in mm.
    /// Returns tuple with result code and pivot position.
    /// </summary>
    public async Task<(int result, double pivotPositionMm)> GetPivotPositionAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("SMGetPivotPos", null, cancellationToken);
        if (response.Length >= 8)
        {
            int result = TescanSemController.DecodeIntInternal(response, 0);
            int offset = 4;
            double pivotPos = TescanSemController.DecodeFloatInternal(response, ref offset);
            return (result, pivotPos);
        }
        return (-1, double.NaN);
    }
}
