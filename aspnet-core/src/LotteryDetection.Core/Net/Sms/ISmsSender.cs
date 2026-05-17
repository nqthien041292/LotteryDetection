using System.Threading.Tasks;

namespace LotteryDetection.Net.Sms;

public interface ISmsSender
{
    Task SendAsync(string number, string message);
}