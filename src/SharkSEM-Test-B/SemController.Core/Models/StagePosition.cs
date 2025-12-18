namespace SemController.Core.Models;

public class StagePosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double? Z { get; set; }
    public double? Rotation { get; set; }
    public double? TiltX { get; set; }
    public double? TiltY { get; set; }

    public StagePosition() { }

    public StagePosition(double x, double y, double? z = null, double? rotation = null, double? tiltX = null, double? tiltY = null)
    {
        X = x;
        Y = y;
        Z = z;
        Rotation = rotation;
        TiltX = tiltX;
        TiltY = tiltY;
    }

    public override string ToString() =>
        $"X={X:F6}, Y={Y:F6}, Z={Z?.ToString("F6") ?? "N/A"}, R={Rotation?.ToString("F2") ?? "N/A"}, Tx={TiltX?.ToString("F2") ?? "N/A"}, Ty={TiltY?.ToString("F2") ?? "N/A"}";
}
