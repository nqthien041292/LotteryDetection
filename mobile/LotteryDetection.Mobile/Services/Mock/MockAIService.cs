using System;
using System.Collections.Generic;
using LotteryDetection.Mobile.Models.Family;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockAIService : IAIService
{
    public static IAIService Instance { get; } = new MockAIService();

    public Task<string> BuildTaskPreviewAsync(string transcript)
    {
        var text = string.IsNullOrWhiteSpace(transcript)
            ? "No transcript captured yet."
            : $"Task: {transcript}\nOwner: Family board\nWhen: Today by 6 PM\nNotes: Structured via AI (mock).";
        return Task.FromResult(text);
    }

    public Task<IEnumerable<string>> GetPromptSuggestionsAsync()
    {
        var prompts = new List<string>
        {
            "Turn this chat into three actionable tasks.",
            "Who should own the grocery run this week?",
            "Summarize the chores for Saturday morning.",
            "Draft a checklist for the birthday party."
        };
        return Task.FromResult(prompts.AsEnumerable());
    }

    public Task<IEnumerable<AssistantSuggestion>> GetSuggestionsAsync()
    {
        var items = new List<AssistantSuggestion>
        {
            new()
            {
                Id = "s1", Kind = "create",
                Title = "Schedule Riley's 8-yr checkup",
                Reason = "Last visit was 13 months ago · Dr. Patel has Tue 10 AM open",
                Member = "sam", MemberName = "Sam", Priority = "med"
            },
            new()
            {
                Id = "s2", Kind = "conflict",
                Title = "Soccer pickup overlaps work meeting on Thu",
                Reason = "You're double-booked 4:30–5:30 PM · suggest reassign to Sam",
                Member = "alex", MemberName = "Alex", Priority = "high", IsConflict = true
            },
            new()
            {
                Id = "s3", Kind = "delegate",
                Title = "Hand off \"grocery run\" to Jordan",
                Reason = "Jordan is free 4–6 PM and offered to help yesterday",
                Member = "jordan", MemberName = "Jordan", Priority = "low"
            },
            new()
            {
                Id = "s4", Kind = "create",
                Title = "Buy Riley birthday gift",
                Reason = "Birthday in 8 days · last year you bought Lego on day-of",
                Member = "alex", MemberName = "Alex", Priority = "med"
            },
            new()
            {
                Id = "s5", Kind = "create",
                Title = "Pack Jordan's field trip lunch",
                Reason = "Field trip Friday · permission slip already signed",
                Member = "home", MemberName = "Shared", Priority = "low"
            }
        };
        return Task.FromResult(items.AsEnumerable());
    }

    public Task<IEnumerable<TaskItem>> GetAssistantDraftTasksAsync(string context)
    {
        var tasks = new[]
        {
            new TaskItem
            {
                Title = "Confirm birthday guest list",
                Description = "Message family thread and lock RSVP count",
                Owner = "Sam",
                DueDate = DateTime.Today.AddDays(1),
                Priority = "High",
                Status = "Planning",
                Tags = new[] { "Birthday", "Comms" },
                Points = 15
            },
            new TaskItem
            {
                Title = "Grocery run for pasta night",
                Description = "Add garlic bread + salad; aim before 6 PM",
                Owner = "Alex",
                DueDate = DateTime.Today,
                Priority = "Medium",
                Status = "In progress",
                Tags = new[] { "Food", "Dinner" },
                Points = 10
            }
        };
        return Task.FromResult(tasks.AsEnumerable());
    }

    public Task<string> SummarizeChatAsync(string context)
    {
        var summary =
            "Key asks: finalize guest list, buy groceries for pasta night, and prep science project summary. Turned into 3 tasks.";
        return Task.FromResult(summary);
    }

    // Phase 7 — no-op for XAML preview / offline fallback
    public Task<IReadOnlyList<Guid>> ConfirmDraftTasksAsync(IEnumerable<TaskItem> drafts)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }
}