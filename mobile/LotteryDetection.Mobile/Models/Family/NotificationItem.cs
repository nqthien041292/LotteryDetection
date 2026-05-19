using System.ComponentModel;

namespace LotteryDetection.Mobile.Models.Family;

public class NotificationItem : INotifyPropertyChanged
{
    private bool isUnread;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string IconBackground { get; set; } = "#E5E7EB";

    public bool IsUnread
    {
        get => isUnread;
        set
        {
            if (isUnread == value) return;
            isUnread = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUnread)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
