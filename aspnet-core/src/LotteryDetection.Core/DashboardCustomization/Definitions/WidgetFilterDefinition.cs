namespace LotteryDetection.DashboardCustomization.Definitions;

public class WidgetFilterDefinition
{
    public WidgetFilterDefinition(
        string id,
        string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; }
}