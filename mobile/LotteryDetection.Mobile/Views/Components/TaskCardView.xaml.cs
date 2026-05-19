using System.Windows.Input;

namespace LotteryDetection.Mobile.Views.Components;

/// <summary>
///     Reusable task card component used across MyTasks, ChatToTask, LiveBoard, and AIAssistant pages.
/// </summary>
public partial class TaskCardView : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(TaskCardView));

    public static readonly BindableProperty DescriptionProperty =
        BindableProperty.Create(nameof(Description), typeof(string), typeof(TaskCardView));

    public static readonly BindableProperty OwnerProperty =
        BindableProperty.Create(nameof(Owner), typeof(string), typeof(TaskCardView));

    public static readonly BindableProperty PriorityProperty =
        BindableProperty.Create(nameof(Priority), typeof(string), typeof(TaskCardView));

    public static readonly BindableProperty DueDateProperty =
        BindableProperty.Create(nameof(DueDate), typeof(string), typeof(TaskCardView));

    public static readonly BindableProperty TappedCommandProperty =
        BindableProperty.Create(nameof(TappedCommand), typeof(ICommand), typeof(TaskCardView));

    public static readonly BindableProperty TappedCommandParameterProperty =
        BindableProperty.Create(nameof(TappedCommandParameter), typeof(object), typeof(TaskCardView));

    public TaskCardView()
    {
        InitializeComponent();
    }

    // Fires after the press animation completes. EventArgs is the card's BindingContext (the data item).
    public event EventHandler<object?>? ItemTapped;

    public async Task AnimatePressAsync()
    {
        try
        {
            await CardBorder.ScaleTo(0.96, 70, Easing.CubicOut);
            await CardBorder.ScaleTo(1.0, 130, Easing.SpringOut);
        }
        catch { /* element unloaded */ }
    }

    private async void OnCardTapped(object sender, TappedEventArgs e)
    {
        await AnimatePressAsync();
        // Fire event with the data item so the host page can respond without XAML command binding.
        ItemTapped?.Invoke(this, BindingContext);
        // Also execute any explicitly bound command (for callers that prefer the command pattern).
        var param = TappedCommandParameter ?? BindingContext;
        if (TappedCommand?.CanExecute(param) == true)
            TappedCommand.Execute(param);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Owner
    {
        get => (string)GetValue(OwnerProperty);
        set => SetValue(OwnerProperty, value);
    }

    public string Priority
    {
        get => (string)GetValue(PriorityProperty);
        set => SetValue(PriorityProperty, value);
    }

    public string DueDate
    {
        get => (string)GetValue(DueDateProperty);
        set => SetValue(DueDateProperty, value);
    }

    public ICommand TappedCommand
    {
        get => (ICommand)GetValue(TappedCommandProperty);
        set => SetValue(TappedCommandProperty, value);
    }

    public object TappedCommandParameter
    {
        get => GetValue(TappedCommandParameterProperty);
        set => SetValue(TappedCommandParameterProperty, value);
    }

    // Computed visibility helpers
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public bool HasOwner => !string.IsNullOrEmpty(Owner);
    public bool HasPriority => !string.IsNullOrEmpty(Priority);
    public bool HasDueDate => !string.IsNullOrEmpty(DueDate);

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName is nameof(Description)) OnPropertyChanged(nameof(HasDescription));
        if (propertyName is nameof(Owner)) OnPropertyChanged(nameof(HasOwner));
        if (propertyName is nameof(Priority)) OnPropertyChanged(nameof(HasPriority));
        if (propertyName is nameof(DueDate)) OnPropertyChanged(nameof(HasDueDate));
    }
}