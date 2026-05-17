using System.Threading.Tasks;
using Abp.Dependency;
using Microsoft.AspNetCore.SignalR;

namespace LotteryDetection.Authorization.QrLogin;

public class QrLoginHub : Hub, ITransientDependency
{
    private readonly QrLoginImageService _qrLoginImageService;
    private readonly IQrLoginManager _qrLoginManager;

    public QrLoginHub(IQrLoginManager qrLoginManager, QrLoginImageService qrLoginImageService)
    {
        _qrLoginManager = qrLoginManager;
        _qrLoginImageService = qrLoginImageService;
    }

    public async Task SetSessionId()
    {
        var sessionId = await _qrLoginManager.GenerateSessionId(Context.ConnectionId);

        var qrCodeUrl = _qrLoginImageService.GenerateSetupCode(Context.ConnectionId, sessionId);

        await Clients.Caller.SendAsync("generateQrCode", qrCodeUrl);
    }
}