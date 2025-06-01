namespace Horizon.Modules.QRCodeGen;

public static class QRCodeHelper
{
    public static readonly QRCodeGenerator SingletonQrGenerator = new();
    public static Task<byte[]> GenerateQRCodeFromUrlAsync(string url)
    {
        try
        {
            //Create raw qr code data

            QRCodeData qrCodeData = SingletonQrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);

            //Create byte/raw bitmap qr code
            BitmapByteQRCode qrCodeBmp = new(qrCodeData);

            return Task.FromResult(qrCodeBmp.GetGraphic(20));
        }
        catch
        {
            return null;
        }
    }

    public static async Task<BitmapImage> ConvertBitmapBytesToImage(byte[] bytes)
    {
        var image = new BitmapImage();
        using (InMemoryRandomAccessStream stream = new())
        {
            using (DataWriter writer = new(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
            }
            await image.SetSourceAsync(stream);
        }
        return image;
    }
}
