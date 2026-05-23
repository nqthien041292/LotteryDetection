using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LotteryDetection.Mobile.ViewModel;

namespace LotteryDetection.Mobile.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    private bool _isPageActive;

    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = new DashboardViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageActive = true;

        // 1. Đưa các phần tử về trạng thái ẩn và dịch chuyển xuống dưới để tạo hiệu ứng staggered entrance
        HeaderTitleStack.Opacity = 0;
        HeaderTitleStack.TranslationY = -15;

        HeaderAvatarBorder.Opacity = 0;
        HeaderAvatarBorder.Scale = 0.5;

        MainScanCard.Opacity = 0;
        MainScanCard.TranslationY = 25;

        ResultsCard.Opacity = 0;
        ResultsCard.TranslationY = 25;

        HistoryCard.Opacity = 0;
        HistoryCard.TranslationY = 25;

        ProcessCard.Opacity = 0;
        ProcessCard.TranslationY = 25;

        // 2. Chạy chuỗi hiệu ứng xuất hiện tuần tự mượt mà
        await Task.Delay(80);

        // Tiêu đề và ảnh đại diện xuất hiện đồng thời
        _ = HeaderTitleStack.FadeTo(1, 350, Easing.CubicOut);
        _ = HeaderTitleStack.TranslateTo(0, 0, 350, Easing.CubicOut);

        _ = HeaderAvatarBorder.FadeTo(1, 350, Easing.CubicOut);
        _ = HeaderAvatarBorder.ScaleTo(1, 350, Easing.SpringOut);

        await Task.Delay(100);

        // Thẻ quét vé chính trượt lên và mờ dần vào
        _ = MainScanCard.FadeTo(1, 450, Easing.CubicOut);
        await MainScanCard.TranslateTo(0, 0, 450, Easing.CubicOut);

        // Kích hoạt vòng lặp nhịp thở & vẫy nhẹ của huy hiệu Thần Tài
        _ = StartMascotBreathingLoop();

        // Kích hoạt vòng lặp co giãn nhịp thở (Pulse) mời gọi bấm của Nút quét vé chính
        _ = StartButtonPulseLoop();

        // Các thẻ chức năng phụ xuất hiện tuần tự cách nhau 80ms
        _ = ResultsCard.FadeTo(1, 350, Easing.CubicOut);
        _ = ResultsCard.TranslateTo(0, 0, 350, Easing.CubicOut);

        await Task.Delay(80);

        _ = HistoryCard.FadeTo(1, 350, Easing.CubicOut);
        _ = HistoryCard.TranslateTo(0, 0, 350, Easing.CubicOut);

        // Kích hoạt vòng lặp chuyển động liên tục của Kim nguyên bảo và Rương kho báu
        _ = StartSecondaryIconsAnimationLoop();

        await Task.Delay(80);

        // Thẻ quy trình xuất hiện sau cùng
        _ = ProcessCard.FadeTo(1, 400, Easing.CubicOut);
        await ProcessCard.TranslateTo(0, 0, 400, Easing.CubicOut);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isPageActive = false; // Dừng các vòng lặp chuyển động khi rời trang để tiết kiệm hiệu năng
    }

    private async Task StartMascotBreathingLoop()
    {
        while (_isPageActive && this.Handler != null)
        {
            // Co giãn nhẹ và xoay xuôi chiều kim đồng hồ để mô phỏng nhịp thở/vẫy gọi
            _ = MascotBadge.ScaleTo(1.04, 1500, Easing.SinInOut);
            await MascotBadge.RotateTo(3, 1500, Easing.SinInOut);

            if (!_isPageActive || this.Handler == null) break;

            // Thu nhỏ nhẹ và xoay ngược chiều kim đồng hồ về vị trí cân bằng
            _ = MascotBadge.ScaleTo(0.97, 1500, Easing.SinInOut);
            await MascotBadge.RotateTo(-3, 1500, Easing.SinInOut);
        }
        
        // Đưa linh vật về vị trí tĩnh ban đầu khi rời trang
        if (MascotBadge != null)
        {
            _ = MascotBadge.RotateTo(0, 100);
            _ = MascotBadge.ScaleTo(1.0, 100);
        }
    }

    private async Task StartButtonPulseLoop()
    {
        while (_isPageActive && this.Handler != null)
        {
            // Co dãn nút bấm nhẹ nhàng (từ 1.0 lên 1.03) để kích thích thị giác
            _ = ScanButton.ScaleTo(1.03, 1000, Easing.SinInOut);
            await Task.Delay(1000);

            if (!_isPageActive || this.Handler == null) break;

            // Thu nhỏ nhẹ xuống 0.97
            _ = ScanButton.ScaleTo(0.97, 1000, Easing.SinInOut);
            await Task.Delay(1000);
        }

        // Đưa nút về trạng thái cân bằng ban đầu
        if (ScanButton != null)
        {
            _ = ScanButton.ScaleTo(1.0, 100);
        }
    }

    private async Task StartSecondaryIconsAnimationLoop()
    {
        // Chạy đồng thời chuyển động cho cả 2 icon để tạo sự sinh động
        _ = StartGoldIngotAnimationLoop();
        _ = StartTreasureChestAnimationLoop();
    }

    private async Task StartGoldIngotAnimationLoop()
    {
        while (_isPageActive && this.Handler != null)
        {
            if (ResultsIcon == null) break;

            // Nhấp nhô lên trên và xoay nhẹ sang phải
            _ = ResultsIcon.TranslateTo(0, -3, 1200, Easing.SinInOut);
            await ResultsIcon.RotateTo(4, 1200, Easing.SinInOut);

            if (!_isPageActive || this.Handler == null) break;

            // Nhấp nhô xuống dưới và xoay nhẹ sang trái
            _ = ResultsIcon.TranslateTo(0, 3, 1200, Easing.SinInOut);
            await ResultsIcon.RotateTo(-4, 1200, Easing.SinInOut);
        }

        // Đưa về trạng thái ban đầu khi rời trang
        if (ResultsIcon != null)
        {
            _ = ResultsIcon.TranslateTo(0, 0, 100);
            _ = ResultsIcon.RotateTo(0, 100);
        }
    }

    private async Task StartTreasureChestAnimationLoop()
    {
        while (_isPageActive && this.Handler != null)
        {
            if (HistoryIcon == null) break;

            // Nghiêng trái và phồng to nhẹ
            _ = HistoryIcon.ScaleTo(1.08, 1400, Easing.SinInOut);
            await HistoryIcon.RotateTo(-6, 1400, Easing.SinInOut);

            if (!_isPageActive || this.Handler == null) break;

            // Nghiêng phải và thu nhỏ nhẹ
            _ = HistoryIcon.ScaleTo(0.92, 1400, Easing.SinInOut);
            await HistoryIcon.RotateTo(6, 1400, Easing.SinInOut);
        }

        // Đưa về trạng thái ban đầu khi rời trang
        if (HistoryIcon != null)
        {
            _ = HistoryIcon.ScaleTo(1.0, 100);
            _ = HistoryIcon.RotateTo(0, 100);
        }
    }

    private async void OnScanButtonClicked(object sender, EventArgs e)
    {
        if (sender is VisualElement button)
        {
            // Phản hồi xúc giác nảy nút bấm (Tactile Press Bounce effect) cực kỳ mượt mà
            await button.ScaleTo(0.93, 85, Easing.CubicInOut);
            await button.ScaleTo(1.0, 120, Easing.SpringOut);
        }
    }
}
