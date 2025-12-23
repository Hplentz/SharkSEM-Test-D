using SemController.Core.Models;

namespace SemController.Core.Implementations;

public class TescanSemStage
{
    private readonly TescanSemController _controller;
    private readonly TimeSpan _stageMovementTimeout = TimeSpan.FromMinutes(5);
    
    internal TescanSemStage(TescanSemController controller)
    {
        _controller = controller;
    }
    
    public async Task<StagePosition> GetPositionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("StgGetPosition", null, cancellationToken);
        var position = new StagePosition();
        
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
    
    public async Task MoveToAsync(StagePosition position, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var body = new List<byte>();
        body.AddRange(TescanSemController.EncodeFloatInternal(position.X));
        body.AddRange(TescanSemController.EncodeFloatInternal(position.Y));
        
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
    
    public async Task MoveRelativeAsync(StagePosition delta, bool waitForCompletion = true, CancellationToken cancellationToken = default)
    {
        var body = new List<byte>();
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
    
    private async Task WaitForMovementAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        while (await IsMovingAsync(cancellationToken))
        {
            if (DateTime.UtcNow - startTime > _stageMovementTimeout)
            {
                throw new TimeoutException($"Stage movement timed out after {_stageMovementTimeout.TotalMinutes} minutes");
            }
            await Task.Delay(100, cancellationToken);
        }
    }
    
    public async Task<bool> IsMovingAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("StgIsBusy", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0) != 0;
        }
        return false;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("StgStop", null, cancellationToken);
    }
    
    public async Task<StageLimits> GetLimitsAsync(CancellationToken cancellationToken = default)
    {
        var body = TescanSemController.EncodeIntInternal(0);
        var response = await _controller.SendCommandInternalAsync("StgGetLimits", body, cancellationToken);
        
        var limits = new StageLimits();
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
    
    public async Task CalibrateAsync(CancellationToken cancellationToken = default)
    {
        await _controller.SendCommandNoResponseInternalAsync("StgCalibrate", null, cancellationToken);
    }
    
    public async Task<bool> IsCallibratedAsync(CancellationToken cancellationToken = default)
    {
        var response = await _controller.SendCommandInternalAsync("StgIsCalibrated", null, cancellationToken);
        if (response.Length >= 4)
        {
            return TescanSemController.DecodeIntInternal(response, 0) != 0;
        }
        return false;
    }
}
