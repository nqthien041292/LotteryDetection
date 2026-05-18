namespace LotteryDetectionMobile.Views.Components;

public class TabSelectedEventArgs : EventArgs
{
    public TabSelectedEventArgs(string tabKey)
    {
        TabKey = tabKey;
    }

    public string TabKey { get; }
}