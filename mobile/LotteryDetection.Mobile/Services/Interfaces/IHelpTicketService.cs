using LotteryDetection.Mobile.Models.Help;

namespace LotteryDetection.Mobile.Services.Interfaces;

public interface IHelpTicketService
{
    Task<bool> SubmitAsync(HelpTicket ticket);
}
