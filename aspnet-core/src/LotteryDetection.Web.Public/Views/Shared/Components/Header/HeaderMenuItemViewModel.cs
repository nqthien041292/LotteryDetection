using Abp.Application.Navigation;

namespace LotteryDetection.Web.Public.Views.Shared.Components.Header;

public class HeaderMenuItemViewModel
{
    public HeaderMenuItemViewModel(
        UserMenuItem menuItem,
        int currentLevel,
        string currentPageName)
    {
        MenuItem = menuItem;
        CurrentLevel = currentLevel;
        CurrentPageName = currentPageName;
    }

    public UserMenuItem MenuItem { get; set; }

    public int CurrentLevel { get; set; }

    public string CurrentPageName { get; set; }
}