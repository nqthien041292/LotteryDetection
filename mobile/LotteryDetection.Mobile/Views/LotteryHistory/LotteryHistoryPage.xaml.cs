using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;

namespace LotteryDetection.Mobile.Views.LotteryHistory;

public partial class LotteryHistoryPage : ContentPage
{
    private LotteryHistoryViewModel? vm;
    private volatile bool _isPageActive;

    public LotteryHistoryPage()
    {
        InitializeComponent();

        var service = MauiProgram.Services?.GetService<ILotteryHistoryService>();
        if (service != null)
        {
            vm = new LotteryHistoryViewModel(service, NavigationService.Default);
            BindingContext = vm;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageActive = true;

        if (vm != null)
        {
            await vm.InitializeAsync();
        }

        // Khởi chạy hoạt ảnh bập bồng, xoay nhẹ cho Rương Kho Báu ở Header
        _ = StartHeaderChestAnimationLoop();

        // Khởi chạy hoạt ảnh nhịp thở co giãn mời gọi bấm cho nút Tiếp Tục Dò Vé dưới cùng
        _ = StartContinueButtonPulseLoop();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isPageActive = false;
    }

    private async Task StartHeaderChestAnimationLoop()
    {
        // Hoạt ảnh bập bồng và xoay lắc lư nhẹ nhàng cho Rương Kho Báu Header
        while (_isPageActive && this.Handler != null && HeaderChest != null)
        {
            // Di chuyển lên và xoay nhẹ sang phải
            _ = HeaderChest.TranslateTo(0, -3, 1300, Easing.SinInOut);
            await HeaderChest.RotateTo(4, 1300, Easing.SinInOut);

            if (!_isPageActive || this.Handler == null || HeaderChest == null) break;

            // Di chuyển xuống và xoay nhẹ sang trái
            _ = HeaderChest.TranslateTo(0, 3, 1300, Easing.SinInOut);
            await HeaderChest.RotateTo(-4, 1300, Easing.SinInOut);
        }

        // Reset khi rời trang để bảo toàn hiệu năng
        if (HeaderChest != null)
        {
            _ = HeaderChest.TranslateTo(0, 0, 100);
            _ = HeaderChest.RotateTo(0, 100);
        }
    }

    private async Task StartContinueButtonPulseLoop()
    {
        // Hoạt ảnh co giãn (pulse animation) nhịp thở thu hút sự chú ý của người dùng
        while (_isPageActive && this.Handler != null && ContinueButton != null)
        {
            _ = ContinueButton.ScaleTo(1.03, 1000, Easing.SinInOut);
            await Task.Delay(1000);

            if (!_isPageActive || this.Handler == null || ContinueButton == null) break;

            _ = ContinueButton.ScaleTo(0.97, 1000, Easing.SinInOut);
            await Task.Delay(1000);
        }

        // Reset khi rời trang
        if (ContinueButton != null)
        {
            _ = ContinueButton.ScaleTo(1.0, 100);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
