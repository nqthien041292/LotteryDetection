using System.Windows.Input;
using LotteryDetection.Mobile.Models.Board;
using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetection.Mobile.Views.Components;

/// <summary>
///     Bundle screens-calendar.jsx → BoardCard. Member-tinted bg with left dot stripe,
///     avatar+owner+when row, optional HIGH chip and LIVE pulse badge.
/// </summary>
public sealed class BoardCardView : ContentView
{
    public static readonly BindableProperty CardProperty =
        BindableProperty.Create(nameof(Card), typeof(BoardCard), typeof(BoardCardView),
            propertyChanged: OnCardChanged);

    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(BoardCardView));

    public static readonly BindableProperty LongPressCommandProperty =
        BindableProperty.Create(nameof(LongPressCommand), typeof(ICommand), typeof(BoardCardView));

    private readonly Border outer;
    private readonly Label titleLabel;
    private readonly FamilyAvatar avatar;
    private readonly Label ownerLabel;
    private readonly Label whenLabel;
    private readonly Border highChip;
    private readonly Label highLabel;
    private readonly Border liveBadge;
    private readonly Label liveLabel;
    private readonly BoxView liveDot;

    public BoardCardView()
    {
        titleLabel = new Label
        {
            FontFamily = "Geist",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            LineHeight = 1.3,
            CharacterSpacing = -0.005,
            LineBreakMode = LineBreakMode.WordWrap
        };

        avatar = new FamilyAvatar { Size = 16 };
        ownerLabel = new Label
        {
            FontFamily = "Geist",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            Opacity = 0.85
        };
        var bullet = new Label
        {
            Text = "·",
            FontFamily = "Geist",
            FontSize = 10,
            VerticalOptions = LayoutOptions.Center,
            Opacity = 0.55
        };
        whenLabel = new Label
        {
            FontFamily = "Geist",
            FontSize = 11,
            VerticalOptions = LayoutOptions.Center,
            Opacity = 0.7
        };

        highLabel = new Label
        {
            Text = "HIGH",
            FontFamily = "Geist",
            FontSize = 9.5,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 0.06,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        highChip = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(5, 1),
            BackgroundColor = Color.FromArgb("#A6FFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false,
            Content = highLabel
        };

        var metaRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 6,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(avatar, 0);
        Grid.SetColumn(ownerLabel, 1);
        Grid.SetColumn(bullet, 2);
        Grid.SetColumn(whenLabel, 3);
        Grid.SetColumn(highChip, 5);
        metaRow.Children.Add(avatar);
        metaRow.Children.Add(ownerLabel);
        metaRow.Children.Add(bullet);
        metaRow.Children.Add(whenLabel);
        metaRow.Children.Add(highChip);

        var stack = new VerticalStackLayout { Spacing = 6, Children = { titleLabel, metaRow } };

        liveDot = new BoxView { WidthRequest = 5, HeightRequest = 5, CornerRadius = 3, BackgroundColor = Color.FromArgb("#22C55E") };
        liveLabel = new Label
        {
            Text = "LIVE",
            FontFamily = "Geist",
            FontSize = 9,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 0.04,
            VerticalOptions = LayoutOptions.Center
        };
        var liveContent = new HorizontalStackLayout { Spacing = 4, Children = { liveDot, liveLabel } };
        liveBadge = new Border
        {
            StrokeThickness = 0,
            Padding = new Thickness(6, 2),
            BackgroundColor = Color.FromArgb("#99FFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 10, 10, 0),
            IsVisible = false,
            Content = liveContent
        };

        var grid = new Grid { Children = { stack, liveBadge }, Padding = new Thickness(12, 12, 12, 12) };

        outer = new Border
        {
            StrokeThickness = 0,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = grid,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Content = outer;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (Card != null && TapCommand?.CanExecute(Card) == true)
                TapCommand.Execute(Card);
        };
        outer.GestureRecognizers.Add(tap);

        // long-press via PointerGestureRecognizer fallback: 600ms hold
        var longPress = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        longPress.Tapped += (_, _) =>
        {
            if (Card != null && LongPressCommand?.CanExecute(Card) == true)
                LongPressCommand.Execute(Card);
        };
        outer.GestureRecognizers.Add(longPress);
    }

    public BoardCard? Card
    {
        get => (BoardCard?)GetValue(CardProperty);
        set => SetValue(CardProperty, value);
    }

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public ICommand? LongPressCommand
    {
        get => (ICommand?)GetValue(LongPressCommandProperty);
        set => SetValue(LongPressCommandProperty, value);
    }

    private static void OnCardChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BoardCardView v) v.Apply();
    }

    private void Apply()
    {
        if (Card == null)
        {
            IsVisible = false;
            return;
        }
        IsVisible = true;

        var bg = MemberPalette.Resolve(Card.Member, MemberPalette.Slot.Bg);
        var text = MemberPalette.Resolve(Card.Member, MemberPalette.Slot.Text);

        outer.BackgroundColor = bg;
        outer.Opacity = Card.IsDone ? 0.65 : 1;

        titleLabel.TextColor = text;
        titleLabel.Text = Card.Title;
        titleLabel.TextDecorations = Card.IsDone ? TextDecorations.Strikethrough : TextDecorations.None;

        avatar.Member = Card.Member;
        avatar.Name = Card.OwnerLabel?.Length > 0 ? Card.OwnerLabel.Substring(0, 1) : "?";

        ownerLabel.Text = Card.OwnerLabel;
        ownerLabel.TextColor = text;
        whenLabel.Text = Card.When;
        whenLabel.TextColor = text;

        highChip.IsVisible = Card.IsHigh;
        highLabel.TextColor = text;

        liveBadge.IsVisible = Card.IsLive;
        liveLabel.TextColor = text;
    }
}
