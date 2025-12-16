namespace SemController.Core.Models;

public class StageLimits
{
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
    public double MinZ { get; set; }
    public double MaxZ { get; set; }
    public double MinRotation { get; set; }
    public double MaxRotation { get; set; }
    public double MinTiltX { get; set; }
    public double MaxTiltX { get; set; }
    public double? MinTiltY { get; set; }
    public double? MaxTiltY { get; set; }
}
