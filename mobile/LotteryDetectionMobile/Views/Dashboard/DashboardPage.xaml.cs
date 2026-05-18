using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Mock;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();

        // Override the XAML-created default ViewModel with one wired up to live services from DI.
        var realtimeService = GetService<IDashboardRealtimeService>();
        var taskService = GetService<ITaskService>() ?? MockTaskService.Instance;
        var gamification = GetService<IGamificationService>();
        var authService = GetService<IAuthService>();
        var notificationService = GetService<INotificationService>();
        var memberCache = GetService<IFamilyMemberCache>();
        BindingContext = new DashboardViewModel(NavigationService.Default, taskService, realtimeService, gamification, authService, notificationService, memberCache);
        if (BottomBar != null) BottomBar.SelectedTab = "Home";
    }

    private DashboardViewModel? ViewModel => BindingContext as DashboardViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BottomBar != null) BottomBar.SelectedTab = "Home";
        if (ViewModel != null) await ViewModel.InitializeAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }

    private static T? GetService<T>() where T : class
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
    }
}
