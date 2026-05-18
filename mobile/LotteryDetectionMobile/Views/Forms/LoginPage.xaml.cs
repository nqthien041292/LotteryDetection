using LotteryDetectionMobile.ViewModel;

namespace LotteryDetectionMobile.Views.Forms;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

        // Resolve ViewModel from DI to ensure singleton IAuthService is used
        BindingContext = IPlatformApplication.Current!.Services.GetRequiredService<LoginPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Always check on appear: handles first show, navigation transitions, and the
        // case where the user returns from interactive browser auth on Android, where
        // the SignInAsync continuation may not reliably navigate by itself.
        if (BindingContext is LoginPageViewModel viewModel)
            await viewModel.OnPageAppearingAsync();
    }
}