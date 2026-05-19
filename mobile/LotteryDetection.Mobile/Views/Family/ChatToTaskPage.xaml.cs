using LotteryDetection.Mobile.Services.Dialogs;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using LotteryDetection.Mobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.Family;

public partial class ChatToTaskPage : ContentPage
{
    public ChatToTaskPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Home";
        var ai = MauiProgram.Services?.GetService<IAIService>() ?? MockAIService.Instance;
        BindingContext = new ChatToTaskViewModel(NavigationService.Default, ai);
    }

    private ChatToTaskViewModel? ViewModel => BindingContext as ChatToTaskViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel != null) await ViewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (ViewModel != null)
            ViewModel.ShowMakeTaskSheet = false;
    }

    private async void OnPickOwnerTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null) return;
        var options = await ResolveOwnerOptionsAsync();
        var picked = await AppDialog.ShowActionSheetAsync("Owner", "Cancel", options);
        if (string.IsNullOrEmpty(picked)) return;
        ViewModel.SetMakeTaskOwner(picked);
    }

    private async Task<string[]> ResolveOwnerOptionsAsync()
    {
        var cache = MauiProgram.Services?.GetService<IFamilyMemberCache>();
        if (cache == null) return new[] { "Home" };
        try
        {
            var members = await cache.GetMembersAsync();
            var names = members
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .Select(m => m.Name!)
                .ToList();
            if (names.Count == 0) return new[] { "Home" };
            names.Add("Home");
            return names.ToArray();
        }
        catch
        {
            return new[] { "Home" };
        }
    }

    private async void OnPickWhenTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null) return;
        var current = ViewModel.MakeTaskWhen;
        var dateLabel = await AppDialog.ShowActionSheetAsync("When", "Cancel",
            "In 1 hour", "In 3 hours", "Tonight 7 PM", "Tomorrow 9 AM", "This weekend 10 AM");
        if (string.IsNullOrEmpty(dateLabel)) return;

        var when = dateLabel switch
        {
            "In 1 hour" => DateTime.Now.AddHours(1),
            "In 3 hours" => DateTime.Now.AddHours(3),
            "Tonight 7 PM" => DateTime.Today.AddHours(19),
            "Tomorrow 9 AM" => DateTime.Today.AddDays(1).AddHours(9),
            "This weekend 10 AM" => DateTime.Today.AddDays(((int)DayOfWeek.Saturday - (int)DateTime.Today.DayOfWeek + 7) % 7).AddHours(10),
            _ => current
        };
        ViewModel.SetMakeTaskWhen(when);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (ViewModel?.ShowMakeTaskSheet == true)
        {
            ViewModel.ShowMakeTaskSheet = false;
            return;
        }

        await NavigationService.Default.NavigateBackAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }
}
