using LotteryDetectionMobile.Models.Help;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface IHelpTicketService
{
    Task<bool> SubmitAsync(HelpTicket ticket);
}
