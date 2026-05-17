using System.Collections.Generic;

namespace LotteryDetection.DashboardCustomization.Definitions;

public class DashboardDefinition
{
    public DashboardDefinition(string name, List<string> availableWidgets)
    {
        Name = name;
        AvailableWidgets = availableWidgets;
    }

    public string Name { get; set; }

    public List<string> AvailableWidgets { get; set; }
}