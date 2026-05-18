using System;
using System.Collections.Generic;
using LotteryDetectionMobile.Models.Family;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface IAIService
{
    Task<string> BuildTaskPreviewAsync(string transcript);
    Task<IEnumerable<string>> GetPromptSuggestionsAsync();
    Task<IEnumerable<AssistantSuggestion>> GetSuggestionsAsync();
    Task<IEnumerable<TaskItem>> GetAssistantDraftTasksAsync(string context);
    Task<string> SummarizeChatAsync(string context);
    Task<IReadOnlyList<Guid>> ConfirmDraftTasksAsync(IEnumerable<TaskItem> drafts); // Phase 7
}