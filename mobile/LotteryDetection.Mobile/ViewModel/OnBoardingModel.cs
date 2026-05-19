namespace LotteryDetection.Mobile.ViewModel;

public class OnBoardingModel
{
    public OnBoardingModel(string image, string title, string description)
    {
        Image = image;
        Title = title;
        Description = description;
    }

    public string? Image { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }
}