using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Family;

public partial class CalendarPage : ContentPage
{
    public CalendarPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Home";

        var calendar = MauiProgram.Services?.GetService<ICalendarService>();
        var memberCache = MauiProgram.Services?.GetService<IFamilyMemberCache>();
        if (calendar != null)
            BindingContext = new CalendarViewModel(NavigationService.Default, calendar, memberCache);
    }

    private CalendarViewModel? ViewModel => BindingContext as CalendarViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel != null) await ViewModel.InitializeAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateToLotteryCaptureAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }
}