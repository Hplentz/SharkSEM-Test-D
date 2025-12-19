namespace SemController.Core.Models;

public class SemImage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Channel { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int BitsPerPixel { get; set; } = 8;
    public DateTime CaptureTime { get; set; } = DateTime.UtcNow;
    public string? Header { get; set; }

    public SemImage() { }

    public SemImage(int width, int height, byte[] data, int channel = 0, int bitsPerPixel = 8)
    {
        Width = width;
        Height = height;
        Data = data;
        Channel = channel;
        BitsPerPixel = bitsPerPixel;
    }
}
