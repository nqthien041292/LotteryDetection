using System.ComponentModel;
using LotteryDetectionMobile.Services.Navigation;
using LotteryDetectionMobile.ViewModel;
using LotteryDetectionMobile.Views.Components;

namespace LotteryDetectionMobile.Views.Family;

public partial class TaskDetailPage : ContentPage, IQueryAttributable
{
    public TaskDetailPage()
    {
        InitializeComponent();
        BottomBar.SelectedTab = "Task";
    }

    private TaskDetailViewModel? ViewModel => BindingContext as TaskDetailViewModel;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (ViewModel == null) return;
        if (query.TryGetValue("TaskId", out var idObj) && idObj is string id) ViewModel.TaskId = id;
        if (query.TryGetValue("EditMode", out var editObj) && editObj is bool editMode && editMode)
            ViewModel.StartInEditMode = true;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel == null) return;

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Defer the data load to the next dispatcher tick so the page can finish
        // its initial layout (with the skeleton already showing thanks to the
        // ViewModel's default IsLoading=true). Without this, the synchronous part
        // of InitializeAsync — including the Task-property notifications that fan
        // out to every binding in the ScrollView — runs inside the same frame as
        // the Shell transition, which is what makes the screen appear to "stall"
        // for a moment after the user taps.
        Dispatcher.Dispatch(async () =>
        {
            await ViewModel.InitializeAsync(ViewModel.TaskId);
            if (ViewModel.StartInEditMode)
            {
                ViewModel.EditCommand.Execute(null);
                ViewModel.StartInEditMode = false;
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (ViewModel != null)
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TaskDetailViewModel.Task)
            or nameof(TaskDetailViewModel.IsCompleted)
            or nameof(TaskDetailViewModel.IsCompleting)
            or nameof(TaskDetailViewModel.ErrorMessage)
            or nameof(TaskDetailViewModel.IsEditing)
            or nameof(TaskDetailViewModel.IsSaving)
            or nameof(TaskDetailViewModel.IsDeleting)
            or nameof(TaskDetailViewModel.IsAddingToBoard))
            UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        var vm = ViewModel;
        if (vm == null) return;

        var isEditing = vm.IsEditing;

        TitleLabel.IsVisible = !isEditing;
        TitleEntry.IsVisible = isEditing;
        AssigneeLabel.IsVisible = !isEditing;
        AssigneeEntry.IsVisible = isEditing;
        DueDateLabel.IsVisible = !isEditing;
        DueDateEntry.IsVisible = isEditing;
        PriorityChip.IsVisible = !isEditing;
        PriorityEntry.IsVisible = isEditing;
        CategoryEntry.IsVisible = isEditing;
        LocationLabel.IsVisible = !isEditing && !string.IsNullOrEmpty(vm.Task?.Location);
        LocationEntry.IsVisible = isEditing;

        NotesCard.IsVisible = isEditing || !string.IsNullOrEmpty(vm.Task?.Notes);
        NotesLabel.IsVisible = !isEditing;
        NotesEditor.IsVisible = isEditing;

        TranscriptCard.IsVisible = !isEditing && !string.IsNullOrEmpty(vm.Task?.Transcript);

        EditButton.IsVisible = !isEditing && !vm.IsCompleted;
        EditActions.IsVisible = isEditing;
        SaveButton.Text = vm.IsSaving ? "Saving…" : "Save";
        CompleteButton.IsVisible = !isEditing && !vm.IsCompleted;
        CompleteButton.Text = vm.IsCompleting ? "Completing…" : "Mark complete";
        DeleteButton.IsVisible = !isEditing;
        DeleteButton.Text = vm.IsDeleting ? "Deleting…" : "Delete task";

        // Once the voice task has been promoted into a FamilyTask, the button doesn't need to
        // act anymore — relabel it as a passive "View on board" entry point.
        var status = vm.Task?.Status ?? string.Empty;
        var isOnBoard = string.Equals(status, "TaskCreated", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);
        AddToBoardButton.Text = vm.IsAddingToBoard
            ? "Adding…"
            : isOnBoard ? "View on board" : "Add to board";
        AddToBoardButton.IsEnabled = !vm.IsAddingToBoard;

        ErrorLabel.IsVisible = !string.IsNullOrEmpty(vm.ErrorMessage);

        // Reflect the priority on the chip
        if (!string.IsNullOrEmpty(vm.Task?.Priority))
        {
            var p = vm.Task.Priority.ToLowerInvariant();
            PriorityChip.Text = p switch
            {
                "high" => "High",
                "low" => "Low",
                _ => "Medium"
            };
            PriorityChip.ChipColor = p switch
            {
                "high" => "high",
                "low" => "low",
                _ => "med"
            };
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await NavigationService.Default.NavigateBackAsync();
    }

    private async void OnNewRecordingClicked(object? sender, EventArgs e)
    {
        await NavigationService.Default.NavigateToVoiceCaptureAsync();
    }

    private async void OnAddToBoardClicked(object? sender, EventArgs e)
    {
        var vm = ViewModel;
        if (vm == null) { await NavigationService.Default.NavigateToDashboardAsync(); return; }

        // If user is in edit mode, save first so the latest fields land on the board.
        if (vm.IsEditing)
        {
            await vm.SaveChangesAsync();
            if (!string.IsNullOrEmpty(vm.ErrorMessage)) return;
        }

        // Promote the voice task into a FamilyTask on the backend, then navigate to Home.
        await vm.AddToBoardAsync();
    }

    private async void OnTabSelected(object sender, TabSelectedEventArgs e)
    {
        if (ViewModel == null) return;
        await ViewModel.OnTabSelectedAsync(e.TabKey);
    }
}
