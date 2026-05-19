using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;

namespace LotteryDetectionMobile.Views.LotteryCapture;

public partial class LotteryCapturePage : ContentPage
{
    private string? ticketImagePath;

    public LotteryCapturePage(LotteryCaptureViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnCaptureClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("Khong ho tro camera", "Thiet bi hien tai khong ho tro chup anh truc tiep.", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Chup ve so"
            });

            if (photo != null) await LoadTicketPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Khong chup duoc anh", ex.Message, "OK");
        }
    }

    private async void OnPickClicked(object? sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Chon anh ve so"
            });

            if (photo != null) await LoadTicketPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Khong chon duoc anh", ex.Message, "OK");
        }
    }

    private async Task LoadTicketPhotoAsync(FileResult photo)
    {
        var fileName = $"ticket-{DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(photo.FileName)}";
        var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        await using (var source = await photo.OpenReadAsync())
        await using (var destination = File.OpenWrite(localPath))
        {
            await source.CopyToAsync(destination);
        }

        ticketImagePath = localPath;
        TicketPreview.Source = ImageSource.FromFile(localPath);
        TicketPreview.IsVisible = true;
        EmptyCameraState.IsVisible = false;

        ProvinceLabel.Text = "-";
        DrawDateLabel.Text = "-";
        TicketNumberLabel.Text = "-";
        PrizeLabel.Text = "Anh da san sang. Bam AI do ve so de phan tich.";
        PrizeBanner.BackgroundColor = Color.FromArgb("#EEF4EC");
        StatusLabel.Text = "Da tai anh ve so.";
    }

    private async void OnAnalyzeClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ticketImagePath))
        {
            await DisplayAlert("Can anh ve so", "Hay chup hoac chon anh ve so truoc khi do.", "OK");
            return;
        }

        SetAnalyzing(true);
        StatusLabel.Text = "AI dang phan tich anh ve, tach day so va doi ket qua...";

        await Task.Delay(900);

        ProvinceLabel.Text = "TP. Ho Chi Minh";
        DrawDateLabel.Text = DateTime.Now.ToString("dd/MM/yyyy");
        TicketNumberLabel.Text = "834972";
        PrizeBanner.BackgroundColor = Color.FromArgb("#E7F6DE");
        PrizeLabel.Text = "Khong phat hien trung giai trong bo ket qua mau. San sang ket noi API ket qua xo so that.";
        StatusLabel.Text = "AI da phan tich xong.";

        SetAnalyzing(false);
    }

    private void SetAnalyzing(bool isAnalyzing)
    {
        AnalyzeSpinner.IsRunning = isAnalyzing;
        AnalyzeSpinner.IsVisible = isAnalyzing;
        AnalyzeButton.IsEnabled = !isAnalyzing;
        AnalyzeButton.Text = isAnalyzing ? "AI dang phan tich..." : "AI do ve so";
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }
}
