namespace SemController.Core.Models;

public class StagePosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Rotation { get; set; }
    public double TiltX { get; set; }
    public double? TiltY { get; set; }

    public StagePosition() { }

    public StagePosition(double x, double y, double z = 0, double rotation = 0, double tiltX = 0, double? tiltY = null)
    {
        X = x;
        Y = y;
        Z = z;
        Rotation = rotation;
        TiltX = tiltX;
        TiltY = tiltY;
    }

    public override string ToString() =>
        $"X={X:F6}, Y={Y:F6}, Z={Z:F6}, R={Rotation:F2}, Tx={TiltX:F2}, Ty={TiltY?.ToString("F2") ?? "N/A"}";
}
