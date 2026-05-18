using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetectionMobile.Views.Family.Invitation;

public partial class AcceptInvitationPage : ContentPage
{
    public AcceptInvitationPage()
    {
        InitializeComponent();

        var familyService = MauiProgram.Services?.GetService<IFamilyService>();
        if (familyService != null)
            BindingContext = new AcceptInvitationViewModel(NavigationService.Default, familyService);
    }

    private AcceptInvitationViewModel? ViewModel => BindingContext as AcceptInvitationViewModel;

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
