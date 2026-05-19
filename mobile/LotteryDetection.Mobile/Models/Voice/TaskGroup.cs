using System.Collections.ObjectModel;

namespace LotteryDetection.Mobile.Models.Voice;

/// <summary>
///     Groups tasks by a date-based label (e.g., "Today", "Overdue", "Completed") for CollectionView display.
/// </summary>
public class TaskGroup : ObservableCollection<VoiceTaskListItem>
{
    public TaskGroup(string name, IEnumerable<VoiceTaskListItem> items) : base(items)
    {
        Name = name;
    }

    public string Name { get; }
}