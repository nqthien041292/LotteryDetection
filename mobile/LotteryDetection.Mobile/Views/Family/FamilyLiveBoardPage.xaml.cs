using System.Windows.Input;
using LotteryDetection.Mobile.Models.Board;
using LotteryDetection.Mobile.Services.Dialogs;
using LotteryDetection.Mobile.Services.Interfaces;
using LotteryDetection.Mobile.Services.Mock;
using LotteryDetection.Mobile.Services.Navigation;
using LotteryDetection.Mobile.ViewModel;
using LotteryDetection.Mobile.Views.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LotteryDetection.Mobile.Views.Family;

public partial class FamilyLiveBoardPage : ContentPage
{
    public FamilyLiveBoardPage()
    {
        InitializeComponent();
        BindingContext = new FamilyLiveBoardViewModel(
            NavigationService.Default,
            GetService<ITaskService>() ?? MockTaskService.Instance,
            GetService<IFamilyMemberCache>());
        BottomBar.SelectedTab = "Home";
        LongPressMoveCommand = new Command<BoardCard>(async card => await PromptMoveAsync(card));
    }

    public ICommand LongPressMoveCommand { get; }

    private FamilyLiveBoardViewModel? ViewModel => BindingContext as FamilyLiveBoardViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel != null) await ViewModel.InitializeAsync();
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

    private async Task PromptMoveAsync(BoardCard? card)
    {
        if (card == null || ViewModel == null) return;
        var options = new List<string>();
        if (card.Status != "todo") options.Add("Move to To do");
        if (card.Status != "doing") options.Add("Move to In progress");
        if (card.Status != "done") options.Add("Move to Done");

        var choice = await AppDialog.ShowActionSheetAsync(card.Title, "Cancel", options.ToArray());
        if (string.IsNullOrEmpty(choice)) return;

        var target = choice switch
        {
            "Move to To do" => "todo",
            "Move to In progress" => "doing",
            "Move to Done" => "done",
            _ => null
        };
        if (target != null) await ViewModel.MoveCardAsync(card, target);
    }

    private static T? GetService<T>() where T : class
    {
        return MauiProgram.Services?.GetService<T>()
               ?? Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
    }
}
