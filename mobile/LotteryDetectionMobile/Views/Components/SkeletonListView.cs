namespace LotteryDetectionMobile.Views.Components;

public sealed class SkeletonListView : SkeletonViewBase
{
    public static readonly BindableProperty RowsProperty = BindableProperty.Create(
        nameof(Rows), typeof(int), typeof(SkeletonListView), 4,
        propertyChanged: (b, _, _) => ((SkeletonListView)b).Rebuild());

    public static readonly BindableProperty ShowHeroProperty = BindableProperty.Create(
        nameof(ShowHero), typeof(bool), typeof(SkeletonListView), true,
        propertyChanged: (b, _, _) => ((SkeletonListView)b).Rebuild());

    public static readonly BindableProperty ShowChipsProperty = BindableProperty.Create(
        nameof(ShowChips), typeof(bool), typeof(SkeletonListView), false,
        propertyChanged: (b, _, _) => ((SkeletonListView)b).Rebuild());

    public static readonly BindableProperty CardHeightProperty = BindableProperty.Create(
        nameof(CardHeight), typeof(double), typeof(SkeletonListView), 74.0,
        propertyChanged: (b, _, _) => ((SkeletonListView)b).Rebuild());

    public SkeletonListView()
    {
        Rebuild();
    }

    public int Rows
    {
        get => (int)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public bool ShowHero
    {
        get => (bool)GetValue(ShowHeroProperty);
        set => SetValue(ShowHeroProperty, value);
    }

    public bool ShowChips
    {
        get => (bool)GetValue(ShowChipsProperty);
        set => SetValue(ShowChipsProperty, value);
    }

    public double CardHeight
    {
        get => (double)GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    private void Rebuild()
    {
        ResetSkeletonBlocks();
        var stack = new VerticalStackLayout
        {
            Spacing = 12,
            Padding = Padding
        };

        if (ShowHero)
            stack.Children.Add(AvatarRow(82));
        if (ShowChips)
            stack.Children.Add(ChipRow(64, 82, 72));

        for (var i = 0; i < Math.Max(1, Rows); i++)
            stack.Children.Add(AvatarRow(CardHeight));

        SetSkeletonContent(stack);
    }
}
