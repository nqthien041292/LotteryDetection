using System.Collections.Generic;

namespace LotteryDetection.DashboardCustomization.Dto;

/// <summary>
///     This class stores filtered dashboard information by user
/// </summary>
public class DashboardOutput
{
    public DashboardOutput(string name, List<WidgetOutput> widgets)
    {
        Name = name;
        Widgets = widgets;
    }

    public string Name { get; set; }

    public List<WidgetOutput> Widgets { get; set; }
}