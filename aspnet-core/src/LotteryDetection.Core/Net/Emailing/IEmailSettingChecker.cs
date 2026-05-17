using System.Threading.Tasks;

namespace LotteryDetection.Net.Emailing;

public interface IEmailSettingsChecker
{
    bool EmailSettingsValid();

    Task<bool> EmailSettingsValidAsync();
}

