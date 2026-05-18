using LotteryDetectionMobile.Services.Dialogs;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Voice;

public partial class TaskPreviewPage : ContentPage
{
    public TaskPreviewPage()
    {
        InitializeComponent();
    }

    private TaskPreviewViewModel? ViewModel => BindingContext as TaskPreviewViewModel;

    private async void OnPickOwnerTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null) return;
        var options = await ResolveOwnerOptionsAsync();
        var picked = await AppDialog.ShowActionSheetAsync("Owner", "Cancel", options);
        if (string.IsNullOrEmpty(picked)) return;
        ViewModel.SetOwner(picked);
    }

    private static async Task<string[]> ResolveOwnerOptionsAsync()
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

    private async void OnPickCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null) return;
        var picked = await AppDialog.ShowActionSheetAsync("Category", "Cancel",
            TaskPreviewViewModel.CategoryOptions);
        if (string.IsNullOrEmpty(picked)) return;
        ViewModel.SetCategory(picked);
    }

    private async void OnPickWhenTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null) return;
        var picked = await AppDialog.ShowActionSheetAsync("When", "Cancel",
            "In 1 hour", "Tonight 7 PM", "Tomorrow 9 AM", "This weekend 10 AM");
        if (string.IsNullOrEmpty(picked)) return;

        var when = picked switch
        {
            "In 1 hour" => DateTime.Now.AddHours(1),
            "Tonight 7 PM" => DateTime.Today.AddHours(19),
            "Tomorrow 9 AM" => DateTime.Today.AddDays(1).AddHours(9),
            "This weekend 10 AM" => DateTime.Today.AddDays(((int)DayOfWeek.Saturday - (int)DateTime.Today.DayOfWeek + 7) % 7).AddHours(10),
            _ => ViewModel.When
        };
        ViewModel.SetWhen(when);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
