using System;
using System.Collections.Generic;
using System.Linq;

namespace LotteryDetection.Lottery;

public static class LotteryMatcher
{
    public static void Match(TicketAnalysis ticket, LotteryDrawResult drawResult)
    {
        if (drawResult == null || drawResult.Prizes == null || !drawResult.Prizes.Any() || string.IsNullOrEmpty(ticket.TicketNumber))
        {
            return;
        }

        var ticketNum = ticket.TicketNumber.Trim();
        bool isWinner = false;
        string matchedPrize = null;
        decimal? prizeAmount = null;

        // XSMN/XSMT typically check from Special to Eighth, matching the last N digits.
        // Special: 6 digits (match all 6)
        // First: 5 digits (match last 5)
        // Second: 5 digits (match last 5)
        // Third: 5 digits (match last 5)
        // Fourth: 5 digits (match last 5)
        // Fifth: 4 digits (match last 4)
        // Sixth: 4 digits (match last 4)
        // Seventh: 3 digits (match last 3)
        // Eighth: 2 digits (match last 2)

        var rules = new Dictionary<string, (int digits, decimal amount)>
        {
            {"Special", (6, 2000000000)},
            {"First", (5, 30000000)},
            {"Second", (5, 15000000)},
            {"Third", (5, 10000000)},
            {"Fourth", (5, 3000000)},
            {"Fifth", (4, 1000000)},
            {"Sixth", (4, 400000)},
            {"Seventh", (3, 200000)},
            {"Eighth", (2, 100000)}
        };

        // Check from highest to lowest prize
        foreach (var rule in rules)
        {
            if (drawResult.Prizes.TryGetValue(rule.Key, out var winningNumbers))
            {
                foreach (var winningNum in winningNumbers)
                {
                    if (ticketNum.EndsWith(winningNum))
                    {
                        isWinner = true;
                        matchedPrize = rule.Key;
                        prizeAmount = rule.Value.amount;
                        break;
                    }
                }
            }
            if (isWinner) break;
        }

        ticket.IsWinner = isWinner;
        ticket.MatchedPrize = matchedPrize;
        ticket.PrizeAmount = prizeAmount;
    }
}
