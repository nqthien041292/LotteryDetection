using System.Collections.ObjectModel;

namespace LotteryDetectionMobile.Models.Board;

/// <summary>One Kanban column: id (todo / doing / done), label, accent dot, observable cards.</summary>
public sealed class KanbanColumn
{
    public string Id { get; init; } = "todo";
    public string Label { get; init; } = string.Empty;
    public string DotColor { get; init; } = "#94A3BE";
    public ObservableCollection<BoardCard> Cards { get; } = new();
    public int Count => Cards.Count;
}
