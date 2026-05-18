using System.Diagnostics;
using LotteryDetectionMobile.Models.Help;
using LotteryDetectionMobile.Services.Interfaces;

namespace LotteryDetectionMobile.Services.Mock;

public class MockHelpTicketService : IHelpTicketService
{
    public static IHelpTicketService Instance { get; } = new MockHelpTicketService();

    public Task<bool> SubmitAsync(HelpTicket ticket)
    {
        Debug.WriteLine($"[MockHelpTicketService] {ticket.Topic} @ {ticket.SubmittedAt:O}: {ticket.Message}");
        return Task.FromResult(true);
    }
}
