using LotteryDetectionMobile.ViewModel;

namespace LotteryDetectionMobile.Views.Forms;

public partial class SplashPage : ContentPage
{
    private readonly SplashViewModel _viewModel;

    public SplashPage()
    {
        InitializeComponent();

        // Resolve from DI to share the singleton IAuthService.
        _viewModel = IPlatformApplication.Current!.Services.GetRequiredService<SplashViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            BrandStack.Opacity = 0;
            BrandStack.Scale = 0.92;
            await Task.WhenAll(
                BrandStack.FadeTo(1, 450, Easing.CubicOut),
                BrandStack.ScaleTo(1, 450, Easing.CubicOut));
        }
        catch
        {
            // Entrance animation is cosmetic — never block routing on it.
        }

        await _viewModel.RunStartupAsync();
    }
}
