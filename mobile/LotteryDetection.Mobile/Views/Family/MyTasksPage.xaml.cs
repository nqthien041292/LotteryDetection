using LotteryDetection.Mobile.Models.Voice;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using LotteryDetection.Mobile.Views.Components;

namespace LotteryDetection.Mobile.Views.Family;

public partial class MyTasksPage : ContentPage
{
    // Window after a swipe completes during which CollectionView selection is ignored
    // (a swipe gesture also produces a touch-up that the platform interprets as a tap on the row).
    private const int SwipeSuppressMs = 350;
    private DateTime _lastSwipeAt = DateTime.MinValue;
    private bool _isSwipeActive;
    private SwipeView? _openSwipeView;

    public MyTasksPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Task";
    }

    private MyTasksViewModel ViewModel => BindingContext as MyTasksViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BottomBar.SelectedTab = "Task";
        if (ViewModel != null)
            await ViewModel.InitializeAsync();
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

    private void OnTaskCardTapped(object? sender, object? e)
    {
        if (_isSwipeActive) return;
        if ((DateTime.UtcNow - _lastSwipeAt).TotalMilliseconds < SwipeSuppressMs) return;
        if (e is VoiceTaskListItem task)
            ViewModel?.SelectTaskCommand.Execute(task);
    }

    private void OnSwipeStarted(object sender, SwipeStartedEventArgs e)
    {
        if (sender is SwipeView swipeView)
            _openSwipeView = swipeView;
        _isSwipeActive = true;
    }

    private void OnSwipeEnded(object sender, SwipeEndedEventArgs e)
    {
        _isSwipeActive = false;
        if (ReferenceEquals(_openSwipeView, sender))
            _openSwipeView = null;
        _lastSwipeAt = DateTime.UtcNow;
    }

    private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
    {
        if (sender is Element element)
        {
            CloseOpenSwipe(FindParentSwipeView(element));
            await Task.Delay(180);
        }

        if (sender is SwipeItemView { CommandParameter: VoiceTaskListItem commandTask })
            ViewModel?.DeleteTaskCommand.Execute(commandTask);
        else if (sender is Element { BindingContext: VoiceTaskListItem contextTask })
            ViewModel?.DeleteTaskCommand.Execute(contextTask);
    }

    private static SwipeView? FindParentSwipeView(Element element)
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current is SwipeView swipeView)
                return swipeView;
            current = current.Parent;
        }

        return null;
    }

    private void CloseOpenSwipe(SwipeView? swipeView = null)
    {
        (swipeView ?? _openSwipeView)?.Close();
        _openSwipeView = null;
        _isSwipeActive = false;
        _lastSwipeAt = DateTime.UtcNow;
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel != null)
            await ViewModel.OnTabSelectedAsync(e.TabKey);
    }
}
