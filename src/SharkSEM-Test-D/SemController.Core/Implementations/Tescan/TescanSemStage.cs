// =============================================================================
// TescanSemStage.cs - TESCAN Specimen Stage Control
// =============================================================================
// Manages specimen stage operations for TESCAN microscopes via SharkSEM protocol.
// Provides position queries, absolute/relative movement, calibration, and limits.
//
// Unit Convention:
// - All position values are in millimeters (mm) for X, Y, Z
// - Angular values in degrees for rotation and tilt
// - SharkSEM protocol uses the same units (no conversion needed)
//
// SharkSEM Commands Used:
// - StgGetPosition: Returns current X, Y, Z, R, Tx, Ty
// - StgMoveTo: Absolute movement (variable number of axes)
// - StgMove: Relative movement (variable number of axes)
// - StgIsBusy: Returns 1 if moving, 0 if stopped
// - StgStop: Emergency stop
// - StgGetLimits: Returns min/max for all axes
// - StgCalibrate: Initiates calibration/homing
// - StgIsCalibrated: Returns calibration status
// =============================================================================

using SemController.Core.Models;

namespace SemController.Core.Implementations.Tescan;

/// <summary>
/// Specimen stage control sub-module for TESCAN SEMs.
/// Handles position queries, movement, and calibration.
/// </summary>
public class TescanSemStage
{
    private readonly TescanSemController _controller;
    private readonly TimeSpan _stageMovementTimeout = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Internal constructor - instantiated by TescanSemController.
    /// </summary>
    internal TescanSemStage(TescanSemController controller)
    {
        _controller = controller;
    }
    
    /// <summary>
    /// Gets current stage position for all axes.
    /// Position values are returned in order: X, Y, Z, Rotation, TiltX, TiltY.
    /// </summary>
    public async Task<StagePosition> GetPositionAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("StgGetPosition", null, cancellationToken);
        StagePosition position = new StagePosition();
        
        // Decode position values sequentially from response
        // Each value is a string-encoded float with length prefix
        if (response.Length > 0)
        {
            int offset = 0;
            if (offset < response.Length) position.X = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) position.Y = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) position.Z = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) position.Rotation = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) position.TiltX = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) position.TiltY = TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        
        return position;
    }
    
    /// <summary>
    /// Moves stage to absolute position.
    /// Only axes with non-null values are included in the command.
    /// Axes must be specified in order (can't skip intermediate axes).
    /// </summary>
    /// <param name="position">Target position (null values = don't move that axis).</param>
    /// <param name="waitForCompletion">If true, blocks until movement completes.</param>
    public async Task MoveToAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        // Build command body with position values
        // SharkSEM expects values in order; null values terminate the list
        List<byte> body = new List<byte>();
        body.AddRange(TescanSemController.EncodeFloatInternal(position.X));
        body.AddRange(TescanSemController.EncodeFloatInternal(position.Y));
        
        // Only add optional axes if they have values
        // Must maintain order: Z, Rotation, TiltX, TiltY
        if (position.Z.HasValue)
        {
            body.AddRange(TescanSemController.EncodeFloatInternal(position.Z.Value));
            
            if (position.Rotation.HasValue)
            {
                body.AddRange(TescanSemController.EncodeFloatInternal(position.Rotation.Value));
                
                if (position.TiltX.HasValue)
                {
                    body.AddRange(TescanSemController.EncodeFloatInternal(position.TiltX.Value));
                    
                    if (position.TiltY.HasValue)
                    {
                        body.AddRange(TescanSemController.EncodeFloatInternal(position.TiltY.Value));
                    }
                }
            }
        }
        
        await _controller.SendCommandNoResponseInternalAsync("StgMoveTo", body.ToArray(), cancellationToken);
        
        if (waitForCompletion)
        {
            await WaitForMovementAsync(cancellationToken);
        }
    }
    
    /// <summary>
    /// Moves stage by relative delta from current position.
    /// Structure mirrors MoveToAsync - null values terminate axis list.
    /// </summary>
    public async Task MoveRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        List<byte> body = new List<byte>();
        body.AddRange(TescanSemController.EncodeFloatInternal(delta.X));
        body.AddRange(TescanSemController.EncodeFloatInternal(delta.Y));
        
        if (delta.Z.HasValue)
        {
            body.AddRange(TescanSemController.EncodeFloatInternal(delta.Z.Value));
            
            if (delta.Rotation.HasValue)
            {
                body.AddRange(TescanSemController.EncodeFloatInternal(delta.Rotation.Value));
                
                if (delta.TiltX.HasValue)
                {
                    body.AddRange(TescanSemController.EncodeFloatInternal(delta.TiltX.Value));
                    
                    if (delta.TiltY.HasValue)
                    {
                        body.AddRange(TescanSemController.EncodeFloatInternal(delta.TiltY.Value));
                    }
                }
            }
        }
        
        await _controller.SendCommandNoResponseInternalAsync("StgMove", body.ToArray(), cancellationToken);
        
        if (waitForCompletion)
        {
            await WaitForMovementAsync(cancellationToken);
        }
    }
    
    /// <summary>
    /// Polls IsMoving until stage stops or timeout occurs.
    /// Provides detailed error reporting including position information when possible.
    /// </summary>
    private async Task WaitForMovementAsync(CancellationToken cancellationToken)
    {
        DateTime startTime = DateTime.UtcNow;
        StagePosition? lastKnownPosition = null;
        
        try
        {
            while (await IsMovingAsync(cancellationToken))
            {
                if (DateTime.UtcNow - startTime > _stageMovementTimeout)
                {
                    // Get current position to include in error message
                    try { lastKnownPosition = await GetPositionAsync(cancellationToken); } catch { }
                    
                    string positionInfo = lastKnownPosition != null 
                        ? $" Current position: {lastKnownPosition}" 
                        : "";
                    throw new TimeoutException(
                        $"Stage movement timed out after {_stageMovementTimeout.TotalMinutes} minutes.{positionInfo} " +
                        "The stage may be stuck or an obstacle is preventing movement. " +
                        "Use StopAsync() to halt the stage.");
                }
                await Task.Delay(100, cancellationToken);
            }
        }
        catch (IOException ex)
        {
            // Communication error while waiting for movement
            // Try to get position info for error context
            try { lastKnownPosition = await GetPositionAsync(cancellationToken); } catch { }
            
            string positionInfo = lastKnownPosition != null 
                ? $" Last known position: {lastKnownPosition}" 
                : "";
            throw new InvalidOperationException(
                $"Communication lost while waiting for stage movement to complete.{positionInfo}", ex);
        }
    }
    
    /// <summary>
    /// Checks if stage is currently in motion.
    /// </summary>
    public async Task<bool> IsMovingAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("StgIsBusy", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0) != 0;
        }
        return false;
    }
    
    /// <summary>
    /// Immediately stops all stage motion.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("StgStop", null, cancellationToken);
    }
    
    /// <summary>
    /// Gets stage travel limits for all axes.
    /// Returns min/max values in mm for linear axes, degrees for angular.
    /// </summary>
    public async Task<StageLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
    {
        // Argument 0 = get limits for all axes
        byte[] body = TescanSemController.EncodeIntInternal(0);
        byte[] response = await _controller.SendCommandInternalAsync("StgGetLimits", body, cancellationToken);
        
        StageLimits limits = new StageLimits();
        if (response.Length > 0)
        {
            int offset = 0;
            if (offset < response.Length) limits.MinX = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MaxX = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MinY = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MaxY = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MinZ = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MaxZ = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MinRotation = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MaxRotation = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MinTiltX = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MaxTiltX = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MinTiltY = TescanSemController.DecodeFloatInternal(response, ref offset);
            if (offset < response.Length) limits.MaxTiltY = TescanSemController.DecodeFloatInternal(response, ref offset);
        }
        
        return limits;
    }
    
    /// <summary>
    /// Initiates stage calibration/homing sequence.
    /// Stage must be calibrated after power-on before accurate movements.
    /// Note: Calibration can take up to one minute. Use IsCallibratedAsync() to check completion.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not connected or if stage/detector noses are busy.</exception>
    public async Task CalibrateAsync(CancellationToken cancellationToken = default)
    {
        // Per SharkSEM API manual: StgCalibrate requires all stages and detector noses to be idle
        // (Wait B flag must not be set). Calibration takes up to one minute.
        try
        {
            await _controller.SendCommandNoResponseInternalAsync("StgCalibrate", null, cancellationToken);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                "Failed to initiate stage calibration. Ensure all stages and detector noses are idle " +
                "before attempting calibration.", ex);
        }
    }
    
    /// <summary>
    /// Checks if stage has been calibrated since power-on.
    /// </summary>
    public async Task<bool> IsCallibratedAsync(CancellationToken cancellationToken = default)
    {
        byte[] response = await _controller.SendCommandInternalAsync("StgIsCalibrated", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0) != 0;
        }
        return false;
    }
}
