// =============================================================================
// ThermoSemScanning.cs - Thermo Image Acquisition
// =============================================================================
// Handles image acquisition and scanning control for Thermo Fisher microscopes.
// Includes scan speed control, image capture, and PNG file saving.
//
// PNG Encoding:
// This module includes a custom PNG encoder to avoid external dependencies.
// Set EnablePngStorage = true to enable file saving (disabled by default).
// =============================================================================

using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

/// <summary>
/// Image acquisition sub-module for Thermo Fisher SEMs.
/// Handles scanning, image capture, and optional PNG file saving.
/// </summary>
public class ThermoSemScanning
{
    private readonly Func<SdbMicroscopeClient> _getClient;
    private string? _lastSavedImagePath;
    
    /// <summary>
    /// Controls whether AcquireAndSaveImageAsync actually saves files.
    /// Default is false to prevent unintended file system writes.
    /// </summary>
    internal bool EnablePngStorage { get; set; } = false;

    /// <summary>
    /// Internal constructor - instantiated by ThermoSemController.
    /// </summary>
    internal ThermoSemScanning(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    /// <summary>
    /// Gets the path of the last saved image (null if none saved).
    /// </summary>
    public string? LastSavedImagePath => _lastSavedImagePath;

    /// <summary>
    /// Gets current scan speed as an integer index (1-8).
    /// Derived from dwell time: lower dwell time = higher speed number.
    /// </summary>
    public async Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                SdbMicroscopeClient client = _getClient();
                double dwellTime = client.Beams.ElectronBeam.Scanning.DwellTime.Value;
                
                // Map dwell time to speed index (1 = fastest, 8 = slowest)
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

    /// <summary>
    /// Sets scan speed by index (1-8).
    /// Maps speed index to dwell time for AutoScript.
    /// </summary>
    public async Task SetSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            try
            {
                SdbMicroscopeClient client = _getClient();
                
                // Map speed index to dwell time (seconds)
                double dwellTime = speed switch
                {
                    1 => 100e-9,   // 100 ns
                    2 => 300e-9,   // 300 ns
                    3 => 1e-6,     // 1 µs
                    4 => 3e-6,     // 3 µs
                    5 => 10e-6,    // 10 µs
                    6 => 30e-6,    // 30 µs
                    7 => 100e-6,   // 100 µs
                    _ => 300e-6    // 300 µs (slow for high quality)
                };
                client.Beams.ElectronBeam.Scanning.DwellTime.Value = dwellTime;
            }
            catch
            {
                // Silently fail if speed cannot be set
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Acquires images according to settings.
    /// Uses AutoScript GrabFrame() to capture current view.
    /// </summary>
    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            SdbMicroscopeClient client = _getClient();
            
            // GrabFrame captures current detector view
            // Using 'dynamic' for frame because the actual return type may vary
            dynamic frame = client.Imaging.GrabFrame();
            int width = frame.Width;
            int height = frame.Height;
            Array pixelData = frame.Data;
            
            // Convert pixel data to byte array
            byte[] imageData;
            if (pixelData != null && pixelData.Length > 0)
            {
                if (pixelData is byte[] byteArray)
                {
                    imageData = byteArray;
                }
                else
                {
                    // Handle non-byte pixel formats by converting to 8-bit
                    imageData = new byte[width * height];
                    for (int i = 0; i < Math.Min(pixelData.Length, imageData.Length); i++)
                    {
                        imageData[i] = (byte)(pixelData.GetValue(i) ?? 0);
                    }
                }
            }
            else
            {
                // Return empty image if no data available
                imageData = new byte[width * height];
            }
            
            SemImage semImage = new SemImage
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

    /// <summary>
    /// Acquires a single image from specified channel.
    /// </summary>
    public async Task<SemImage> AcquireSingleImageAsync(int channel, int width, int height, CancellationToken cancellationToken = default)
    {
        SemImage[] images = await AcquireImagesAsync(new ScanSettings { Width = width, Height = height }, cancellationToken);
        return images.FirstOrDefault() ?? new SemImage { Width = width, Height = height, Data = new byte[width * height] };
    }

    /// <summary>
    /// Acquires an image and optionally saves it as PNG.
    /// Only saves if EnablePngStorage is true.
    /// </summary>
    /// <param name="outputPath">Directory to save image (defaults to C:\Temp).</param>
    /// <returns>Full path of saved file, or empty string if storage disabled.</returns>
    public async Task<string> AcquireAndSaveImageAsync(string? outputPath = null, CancellationToken cancellationToken = default)
    {
        SemImage image = await AcquireSingleImageAsync(0, 1024, 768, cancellationToken);
        
        // Only save if explicitly enabled
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

    /// <summary>
    /// Saves a SemImage as a PNG file using custom encoder.
    /// No external imaging libraries required.
    /// </summary>
    public async Task SaveImageAsPngAsync(SemImage image, string filePath, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (image.Data == null || image.Width <= 0 || image.Height <= 0)
                throw new InvalidOperationException("Invalid image data");

            using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            WritePngFile(fileStream, image.Data, image.Width, image.Height);
        }, cancellationToken);
    }

    // =========================================================================
    // Custom PNG Encoder Implementation
    // =========================================================================
    // Writes valid PNG files without external dependencies.
    // Supports 8-bit grayscale only (sufficient for SEM images).
    
    /// <summary>
    /// Writes image data as PNG to stream.
    /// </summary>
    private void WritePngFile(Stream stream, byte[] imageData, int width, int height)
    {
        using BinaryWriter writer = new BinaryWriter(stream);
        
        // PNG signature (8 bytes that identify file as PNG)
        byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        writer.Write(signature);
        
        // IHDR chunk - image header
        using (MemoryStream ihdr = new MemoryStream())
        {
            WriteUInt32BE(ihdr, (uint)width);
            WriteUInt32BE(ihdr, (uint)height);
            ihdr.WriteByte(8);   // Bit depth (8 bits per pixel)
            ihdr.WriteByte(0);   // Color type (0 = grayscale)
            ihdr.WriteByte(0);   // Compression method (0 = deflate)
            ihdr.WriteByte(0);   // Filter method (0 = adaptive)
            ihdr.WriteByte(0);   // Interlace method (0 = no interlace)
            WriteChunk(writer, "IHDR", ihdr.ToArray());
        }
        
        // IDAT chunk - image data (compressed)
        using (MemoryStream zlibData = new MemoryStream())
        {
            // zlib header
            zlibData.WriteByte(0x78);  // Compression method + flags
            zlibData.WriteByte(0x9C);  // Check bits
            
            // Deflate compressed data
            using (MemoryStream deflatedData = new MemoryStream())
            {
                using (System.IO.Compression.DeflateStream deflate = new System.IO.Compression.DeflateStream(deflatedData, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
                {
                    for (int y = 0; y < height; y++)
                    {
                        deflate.WriteByte(0); // Filter type (0 = none) for each row
                        
                        int rowStart = y * width;
                        for (int x = 0; x < width; x++)
                        {
                            int index = rowStart + x;
                            byte pixel = index < imageData.Length ? imageData[index] : (byte)0;
                            deflate.WriteByte(pixel);
                        }
                    }
                }
                
                byte[] compressed = deflatedData.ToArray();
                zlibData.Write(compressed, 0, compressed.Length);
            }
            
            // Adler-32 checksum for zlib
            uint adler = ComputeAdler32ForPng(imageData, width, height);
            WriteUInt32BE(zlibData, adler);
            
            WriteChunk(writer, "IDAT", zlibData.ToArray());
        }
        
        // IEND chunk - marks end of PNG
        WriteChunk(writer, "IEND", Array.Empty<byte>());
    }

    /// <summary>
    /// Writes a PNG chunk (length + type + data + CRC).
    /// </summary>
    private void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        WriteUInt32BE(writer.BaseStream, (uint)data.Length);
        
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        writer.Write(typeBytes);
        
        if (data.Length > 0)
            writer.Write(data);
        
        // CRC covers type + data
        byte[] combined = new byte[4 + data.Length];
        Array.Copy(typeBytes, 0, combined, 0, 4);
        Array.Copy(data, 0, combined, 4, data.Length);
        uint crc = ComputeCrc32(combined);
        WriteUInt32BE(writer.BaseStream, crc);
    }

    /// <summary>
    /// Writes a 32-bit unsigned integer in big-endian format.
    /// </summary>
    private void WriteUInt32BE(Stream stream, uint value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }

    /// <summary>
    /// Computes Adler-32 checksum for PNG image data.
    /// Must match the uncompressed scanline data format (with filter bytes).
    /// </summary>
    private uint ComputeAdler32ForPng(byte[] imageData, int width, int height)
    {
        uint a = 1, b = 0;
        const uint mod = 65521;

        for (int y = 0; y < height; y++)
        {
            // Account for filter byte (0) at start of each row
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

    // CRC-32 lookup table (generated once at class load)
    private static readonly uint[] Crc32Table = GenerateCrc32Table();

    /// <summary>
    /// Generates CRC-32 lookup table for PNG chunk checksums.
    /// </summary>
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

    /// <summary>
    /// Computes CRC-32 checksum for PNG chunk validation.
    /// </summary>
    private uint ComputeCrc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Stops any active image acquisition.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Imaging.StopAcquisition();
        }, cancellationToken);
    }
}
