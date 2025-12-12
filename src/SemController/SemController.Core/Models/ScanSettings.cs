namespace SemController.Core.Models;

public class ScanSettings
{
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 768;
    public double DwellTimeUs { get; set; } = 1.0;
    public int FrameCount { get; set; } = 1;
    public int[] Channels { get; set; } = new[] { 0 };
    public int Left { get; set; } = 0;
    public int Top { get; set; } = 0;
    public int Right { get; set; } = 0;
    public int Bottom { get; set; } = 0;
}
