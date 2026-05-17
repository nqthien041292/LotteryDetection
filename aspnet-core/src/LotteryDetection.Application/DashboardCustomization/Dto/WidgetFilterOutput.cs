namespace LotteryDetection.DashboardCustomization.Dto;

public class WidgetFilterOutput
{
    public WidgetFilterOutput(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; }
}