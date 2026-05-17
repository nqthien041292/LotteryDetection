using System;
using Abp.Dependency;
using QRCoder;

namespace LotteryDetection.Authorization.QrLogin;

public class QrLoginImageService : ITransientDependency
{
    public string GenerateSetupCode(string connectionId, string sessionId)
    {
        var data = $"{connectionId}|{sessionId}";

        using (var qrGenerator = new QRCodeGenerator())
        using (var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
        using (var qrCode = new PngByteQRCode(qrCodeData))
        {
            var qrCodeImage = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
        }
    }
}