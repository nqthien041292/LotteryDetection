using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Family;

public partial class NotificationsPage : ContentPage
{
    private SwipeView? _openSwipeView;

    public NotificationsPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Home";

        var notifService = MauiProgram.Services?.GetService<INotificationService>();
        if (notifService != null)
            BindingContext = new NotificationsViewModel(NavigationService.Default, notifService);
    }

    private NotificationsViewModel? ViewModel => BindingContext as NotificationsViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel != null) await ViewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CloseOpenSwipe();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }

    private void OnNotificationSwipeStarted(object sender, SwipeStartedEventArgs e)
    {
        if (sender is SwipeView swipeView)
            _openSwipeView = swipeView;
    }

    private void OnNotificationSwipeEnded(object sender, SwipeEndedEventArgs e)
    {
        if (ReferenceEquals(_openSwipeView, sender))
            _openSwipeView = null;
    }

    private async void OnNotificationSwipeInvoked(object sender, EventArgs e)
    {
        CloseOpenSwipe();
        await Task.Delay(180);

        if (ViewModel == null || sender is not SwipeItem { CommandParameter: NotificationItem item } swipeItem)
            return;

        if (string.Equals(swipeItem.Text, "Dismiss", StringComparison.OrdinalIgnoreCase))
            ViewModel.DismissCommand.Execute(item);
        else
            ViewModel.MarkReadCommand.Execute(item);
    }

    private void CloseOpenSwipe()
    {
        _openSwipeView?.Close();
        _openSwipeView = null;
    }
}
