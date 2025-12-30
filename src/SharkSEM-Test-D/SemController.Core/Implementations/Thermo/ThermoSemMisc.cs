// =============================================================================
// ThermoSemMisc.cs - Thermo Miscellaneous Functions
// =============================================================================
// Handles miscellaneous operations for Thermo Fisher microscopes,
// primarily microscope information retrieval.
//
// CRITICAL: When accessing AutoScript service properties, you MUST use 'var'
// instead of explicit 'dynamic' declarations. Using 'dynamic' explicitly causes
// RuntimeBinderException because it loses type information needed for COM
// property resolution.
//
// Example:
//   CORRECT:   var service = _getClient().Service;
//   WRONG:     dynamic service = _getClient().Service;
// =============================================================================

using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

/// <summary>
/// Miscellaneous functions sub-module for Thermo Fisher SEMs.
/// Currently handles microscope identification information.
/// </summary>
public class ThermoSemMisc
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    /// <summary>
    /// Internal constructor - instantiated by ThermoSemController.
    /// </summary>
    internal ThermoSemMisc(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    /// <summary>
    /// Retrieves microscope identification and version information.
    /// Accesses the AutoScript service.System object for microscope details.
    /// 
    /// IMPORTANT: Must use 'var' (not 'dynamic') when accessing service properties
    /// to preserve COM type information required for property resolution.
    /// </summary>
    public async Task<MicroscopeInfo> GetMicroscopeInfoAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            // Create info with default values (used as fallback if properties fail)
            MicroscopeInfo info = new MicroscopeInfo
            {
                Manufacturer = "Thermo Fisher Scientific",
                Model = "SEM",
                SerialNumber = "Unknown",
                SoftwareVersion = "Unknown",
                ProtocolVersion = "AutoScript"
            };

            try
            {
                // CRITICAL: Use 'var' here, NOT 'dynamic'
                // Using 'dynamic' loses COM type information and causes RuntimeBinderException
                var service = _getClient().Service;
                var system = service.System;
                
                // Each property access is wrapped in try-catch because
                // some may timeout or be unavailable on certain models
                try
                {
                    info.Model = system.Name;
                }
                catch (Exception ex) when (ex.Message.Contains("timed out") || ex is TimeoutException)
                {
                    info.Model = "SEM (timeout)";
                }
                catch
                {
                    // Keep default value on other errors
                }
                
                try
                {
                    info.SerialNumber = system.SerialNumber;
                }
                catch (Exception ex) when (ex.Message.Contains("timed out") || ex is TimeoutException)
                {
                    info.SerialNumber = "Timeout";
                }
                catch
                {
                }
                
                try
                {
                    info.SoftwareVersion = system.Version;
                }
                catch (Exception ex) when (ex.Message.Contains("timed out") || ex is TimeoutException)
                {
                    info.SoftwareVersion = "Timeout";
                }
                catch
                {
                }
            }
            catch
            {
                // Service or System access failed entirely - return defaults
            }

            return info;
        }, cancellationToken);
    }
}
