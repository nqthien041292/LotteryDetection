using System.Diagnostics;
using LotteryDetection.Mobile.Models.Help;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockHelpTicketService : IHelpTicketService
{
    public static IHelpTicketService Instance { get; } = new MockHelpTicketService();

    public Task<bool> SubmitAsync(HelpTicket ticket)
    {
        Debug.WriteLine($"[MockHelpTicketService] {ticket.Topic} @ {ticket.SubmittedAt:O}: {ticket.Message}");
        return Task.FromResult(true);
    }
}
