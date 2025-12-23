namespace SemController.Core.Models;

public class ScanSpeed
{
    public int Index { get; set; }
    public double DwellTimeMicroseconds { get; set; }

    public ScanSpeed(int index, double dwellTimeMicroseconds)
    {
        Index = index;
        DwellTimeMicroseconds = dwellTimeMicroseconds;
    }

    public override string ToString() => $"Speed {Index}: {DwellTimeMicroseconds:F1} µs/pixel";
}
