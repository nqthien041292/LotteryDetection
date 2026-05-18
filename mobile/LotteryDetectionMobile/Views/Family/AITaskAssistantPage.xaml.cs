using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Family;

public partial class AITaskAssistantPage : ContentPage
{
    public AITaskAssistantPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Home";

        var aiService = MauiProgram.Services?.GetService<IAIService>();
        if (aiService != null)
            BindingContext = new AITaskAssistantViewModel(NavigationService.Default, aiService);
    }

    private AITaskAssistantViewModel? ViewModel => BindingContext as AITaskAssistantViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel != null)
            await ViewModel.InitializeAsync();
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
}
