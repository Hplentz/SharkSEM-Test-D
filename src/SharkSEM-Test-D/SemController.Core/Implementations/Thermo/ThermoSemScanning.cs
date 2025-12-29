using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemScanning
{
    private readonly Func<SdbMicroscopeClient> _getClient;
    private string? _lastSavedImagePath;
    
    internal bool EnablePngStorage { get; set; } = false;

    internal ThermoSemScanning(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    public string? LastSavedImagePath => _lastSavedImagePath;

    public async Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var client = _getClient();
                var dwellTime = client.Beams.ElectronBeam.Scanning.DwellTime.Value;
                if (dwellTime <= 100e-9) return 1;
                if (dwellTime <= 300e-9) return 2;
                if (dwellTime <= 1e-6) return 3;
                if (dwellTime <= 3e-6) return 4;
                if (dwellTime <= 10e-6) return 5;
                if (dwellTime <= 30e-6) return 6;
                if (dwellTime <= 100e-6) return 7;
                return 8;
            }
            catch
            {
                return 1;
            }
        }, cancellationToken);
    }

    public async Task SetSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            try
            {
                var client = _getClient();
                double dwellTime = speed switch
                {
                    1 => 100e-9,
                    2 => 300e-9,
                    3 => 1e-6,
                    4 => 3e-6,
                    5 => 10e-6,
                    6 => 30e-6,
                    7 => 100e-6,
                    _ => 300e-6
                };
                client.Beams.ElectronBeam.Scanning.DwellTime.Value = dwellTime;
            }
            catch
            {
            }
        }, cancellationToken);
    }

    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var client = _getClient();
            var frame = client.Imaging.GrabFrame();
            var width = frame.Width;
            var height = frame.Height;
            var pixelData = frame.Data;
            
            byte[] imageData;
            if (pixelData != null && pixelData.Length > 0)
            {
                if (pixelData is byte[] byteArray)
                {
                    imageData = byteArray;
                }
                else
                {
                    imageData = new byte[width * height];
                    for (int i = 0; i < Math.Min(pixelData.Length, imageData.Length); i++)
                    {
                        imageData[i] = (byte)(pixelData.GetValue(i) ?? 0);
                    }
                }
            }
            else
            {
                imageData = new byte[width * height];
            }
            
            var semImage = new SemImage
            {
                Width = width,
                Height = height,
                BitsPerPixel = 8,
                Channel = 0,
                Data = imageData
            };
            return new[] { semImage };
        }, cancellationToken);
    }

    public async Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
    {
        var images = await AcquireImagesAsync(new ScanSettings { Width = width, Height = height }, cancellationToken);
        return images.FirstOrDefault() ?? new SemImage { Width = width, Height = height, Data = new byte[width * height] };
    }

    public async Task<string> AcquireAndSaveImageAsync(string? outputPath = null, CancellationToken cancellationToken = default)
    {
        var image = await AcquireSingleImageAsync(0, 1024, 768, cancellationToken);
        
        if (!EnablePngStorage)
        {
            return string.Empty;
        }
        
        string directory = outputPath ?? @"C:\Temp";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"SEM_Image_{timestamp}.png";
        string fullPath = Path.Combine(directory, filename);
        
        await SaveImageAsPngAsync(image, fullPath, cancellationToken);
        _lastSavedImagePath = fullPath;
        
        return fullPath;
    }

    public async Task SaveImageAsPngAsync(SemImage image, string filePath, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (image.Data == null || image.Width <= 0 || image.Height <= 0)
                throw new InvalidOperationException("Invalid image data");

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            
            WritePngFile(fileStream, image.Data, image.Width, image.Height);
        }, cancellationToken);
    }

    private void WritePngFile(Stream stream, byte[] imageData, int width, int height)
    {
        using var writer = new BinaryWriter(stream);
        
        byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        writer.Write(signature);
        
        using (var ihdr = new MemoryStream())
        {
            WriteUInt32BE(ihdr, (uint)width);
            WriteUInt32BE(ihdr, (uint)height);
            ihdr.WriteByte(8);
            ihdr.WriteByte(0);
            ihdr.WriteByte(0);
            ihdr.WriteByte(0);
            ihdr.WriteByte(0);
            WriteChunk(writer, "IHDR", ihdr.ToArray());
        }
        
        using (var zlibData = new MemoryStream())
        {
            zlibData.WriteByte(0x78);
            zlibData.WriteByte(0x9C);
            
            using (var deflatedData = new MemoryStream())
            {
                using (var deflate = new System.IO.Compression.DeflateStream(deflatedData, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
                {
                    for (int y = 0; y < height; y++)
                    {
                        deflate.WriteByte(0);
                        
                        int rowStart = y * width;
                        for (int x = 0; x < width; x++)
                        {
                            int index = rowStart + x;
                            byte pixel = index < imageData.Length ? imageData[index] : (byte)0;
                            deflate.WriteByte(pixel);
                        }
                    }
                }
                
                var compressed = deflatedData.ToArray();
                zlibData.Write(compressed, 0, compressed.Length);
            }
            
            uint adler = ComputeAdler32ForPng(imageData, width, height);
            WriteUInt32BE(zlibData, adler);
            
            WriteChunk(writer, "IDAT", zlibData.ToArray());
        }
        
        WriteChunk(writer, "IEND", Array.Empty<byte>());
    }

    private void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        WriteUInt32BE(writer.BaseStream, (uint)data.Length);
        
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        writer.Write(typeBytes);
        
        if (data.Length > 0)
            writer.Write(data);
        
        byte[] combined = new byte[4 + data.Length];
        Array.Copy(typeBytes, 0, combined, 0, 4);
        Array.Copy(data, 0, combined, 4, data.Length);
        uint crc = ComputeCrc32(combined);
        WriteUInt32BE(writer.BaseStream, crc);
    }

    private void WriteUInt32BE(Stream stream, uint value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }

    private uint ComputeAdler32ForPng(byte[] imageData, int width, int height)
    {
        uint a = 1, b = 0;
        const uint mod = 65521;

        for (int y = 0; y < height; y++)
        {
            a = (a + 0) % mod;
            b = (b + a) % mod;
            
            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                int index = rowStart + x;
                byte pixel = index < imageData.Length ? imageData[index] : (byte)0;
                a = (a + pixel) % mod;
                b = (b + a) % mod;
            }
        }

        return (b << 16) | a;
    }

    private static readonly uint[] Crc32Table = GenerateCrc32Table();

    private static uint[] GenerateCrc32Table()
    {
        uint[] table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                if ((c & 1) != 0)
                    c = 0xEDB88320 ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[n] = c;
        }
        return table;
    }

    private uint ComputeCrc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFF;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Imaging.StopAcquisition();
        }, cancellationToken);
    }
}
