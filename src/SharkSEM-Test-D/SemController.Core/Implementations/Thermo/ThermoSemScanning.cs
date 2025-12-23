using AutoScript.Clients;
using SemController.Core.Models;

namespace SemController.Core.Implementations.Thermo;

public class ThermoSemScanning
{
    private readonly Func<SdbMicroscopeClient> _getClient;

    internal ThermoSemScanning(Func<SdbMicroscopeClient> getClient)
    {
        _getClient = getClient;
    }

    public async Task<int> GetSpeedAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(1);
    }

    public async Task SetSpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task<SemImage[]> AcquireImagesAsync(ScanSettings settings, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var frame = _getClient().Imaging.GrabFrame();
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

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _getClient().Imaging.StopAcquisition();
        }, cancellationToken);
    }
}
