using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

public sealed class DashboardSkeleton : SkeletonViewBase
{
    public DashboardSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 14 };

        var headline = new VerticalStackLayout { Spacing = 10 };
        headline.Children.Add(Block(250, 24, 6, 0.42));
        headline.Children.Add(Block(180, 12, 4, 0.34));
        var tiles = Grid(StarColumn(), StarColumn(), StarColumn());
        tiles.ColumnSpacing = 8;
        for (var i = 0; i < 3; i++)
        {
            var tile = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Block(64, 10, 4, 0.18 + i * 0.22),
                    Block(38, 22, 6, 0.18 + i * 0.22)
                }
            };
            tiles.Add(Card(tile, 76, 14), i);
        }
        headline.Children.Add(tiles);
        stack.Children.Add(headline);

        var week = new VerticalStackLayout { Spacing = 8 };
        week.Children.Add(Block(86, 11, 4, 0.18));
        var days = Grid(StarColumn(), StarColumn(), StarColumn(), StarColumn(), StarColumn(), StarColumn(), StarColumn());
        days.ColumnSpacing = 6;
        for (var i = 0; i < 7; i++)
        {
            var day = new VerticalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    Block(24, 8, 4, 0.1 + i * 0.12),
                    Block(30, 30, 15, 0.1 + i * 0.12),
                    Block(18, 4, 2, 0.1 + i * 0.12)
                }
            };
            days.Add(day, i);
        }
        week.Children.Add(Card(days, 86, 16));
        stack.Children.Add(week);

        var quick = Grid(StarColumn(), StarColumn());
        quick.RowDefinitions.Add(FixedRow(82));
        quick.RowDefinitions.Add(FixedRow(82));
        quick.ColumnSpacing = 8;
        quick.RowSpacing = 8;
        for (var r = 0; r < 2; r++)
        for (var c = 0; c < 2; c++)
        {
            var cell = Grid(FixedColumn(34), StarColumn());
            cell.ColumnSpacing = 10;
            cell.Add(Block(34, 34, 10, 0.12 + c * 0.45), 0);
            cell.Add(TextPair(98, 74, 0.35 + c * 0.36), 1);
            quick.Add(Card(cell, 82, 14), c, r);
        }
        stack.Children.Add(quick);

        var lineup = new VerticalStackLayout { Spacing = 8 };
        lineup.Children.Add(Block(112, 11, 4, 0.22));
        for (var i = 0; i < 3; i++)
            lineup.Children.Add(TaskRow(66));
        stack.Children.Add(lineup);

        SetSkeletonContent(stack);
    }
}

public sealed class TaskListSkeleton : SkeletonViewBase
{
    public TaskListSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(ChipRow(48, 58, 94, 86));
        for (var i = 0; i < 5; i++)
            stack.Children.Add(TaskRow(84));
        SetSkeletonContent(stack);
    }
}

public sealed class BoardSkeleton : SkeletonViewBase
{
    public BoardSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(ChipRow(96, 118, 84));

        var column = new VerticalStackLayout { Spacing = 10 };
        var header = Grid(FixedColumn(10), StarColumn(), FixedColumn(34));
        header.ColumnSpacing = 8;
        header.Add(Block(8, 8, 4, 0.1), 0);
        header.Add(Block(112, 13, 4, 0.34), 1);
        header.Add(Block(24, 12, 4, 0.84), 2);
        column.Children.Add(header);
        for (var i = 0; i < 4; i++)
            column.Children.Add(TaskRow(92));
        column.Children.Add(Card(Block(110, 12, 4, 0.5), 44, 12));
        stack.Children.Add(Card(column, -1, 18, new Thickness(10, 12)));

        SetSkeletonContent(stack);
    }
}

public sealed class GamificationSkeleton : SkeletonViewBase
{
    public GamificationSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 14 };
        var hero = new VerticalStackLayout { Spacing = 14 };
        var user = Grid(FixedColumn(64), StarColumn());
        user.ColumnSpacing = 12;
        user.Add(Block(56, 56, 28, 0.1), 0);
        user.Add(TextPair(170, 112, 0.42), 1);
        hero.Children.Add(user);
        hero.Children.Add(Block(260, 8, 4, 0.5));
        var stats = Grid(StarColumn(), StarColumn(), StarColumn());
        stats.ColumnSpacing = 8;
        for (var i = 0; i < 3; i++)
            stats.Add(Card(TextPair(42, 62, 0.2 + i * 0.28), 78, 12), i);
        hero.Children.Add(stats);
        stack.Children.Add(Card(hero, -1, 20, new Thickness(20)));

        stack.Children.Add(Block(190, 11, 4, 0.3));
        for (var i = 0; i < 3; i++)
            stack.Children.Add(AvatarRow(64));

        var badges = Grid(StarColumn(), StarColumn(), StarColumn());
        badges.ColumnSpacing = 10;
        for (var i = 0; i < 3; i++)
            badges.Add(Card(new VerticalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center,
                Children = { Block(44, 44, 22, 0.2 + i * 0.3), Block(58, 10, 4, 0.2 + i * 0.3) }
            }, 112, 12), i);
        stack.Children.Add(badges);
        for (var i = 0; i < 2; i++)
            stack.Children.Add(AvatarRow(62));

        SetSkeletonContent(stack);
    }
}

public sealed class TaskDetailSkeleton : SkeletonViewBase
{
    public TaskDetailSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 14 };

        var detail = new VerticalStackLayout { Spacing = 14 };
        detail.Children.Add(Block(220, 20, 5, 0.36));
        for (var i = 0; i < 5; i++)
        {
            var row = Grid(FixedColumn(80), StarColumn());
            row.ColumnSpacing = 16;
            row.Add(Block(62, 12, 4, 0.15), 0);
            row.Add(Block(i % 2 == 0 ? 126 : 168, 14, 5, 0.72), 1);
            detail.Children.Add(row);
        }
        stack.Children.Add(Card(detail, -1, 18, new Thickness(20, 18)));
        stack.Children.Add(Card(TextPair(160, 240, 0.44), 88, 14));

        SetSkeletonContent(stack);
    }
}

public sealed class SettingsSkeleton : SkeletonViewBase
{
    public SettingsSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 14 };
        stack.Children.Add(ProfileHero());
        stack.Children.Add(Block(110, 11, 4, 0.2));
        for (var i = 0; i < 3; i++)
            stack.Children.Add(AvatarRow(66));
        stack.Children.Add(Block(104, 11, 4, 0.2));
        for (var i = 0; i < 5; i++)
            stack.Children.Add(AvatarRow(58, trailing: false));
        SetSkeletonContent(stack);
    }

    private View ProfileHero()
    {
        var grid = Grid(FixedColumn(48), StarColumn(), FixedColumn(54));
        grid.ColumnSpacing = 12;
        grid.Add(Block(48, 48, 14, 0.1), 0);
        grid.Add(TextPair(160, 210, 0.42), 1);
        grid.Add(Block(44, 26, 8, 0.88), 2);
        return Card(grid, 82, 16);
    }
}

public sealed class AdminRolesSkeleton : SkeletonViewBase
{
    public AdminRolesSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 14 };
        stack.Children.Add(AvatarRow(74));
        stack.Children.Add(Block(72, 11, 4, 0.18));
        for (var i = 0; i < 4; i++)
        {
            var row = Grid(FixedColumn(42), StarColumn(), FixedColumn(74), FixedColumn(30));
            row.ColumnSpacing = 10;
            row.Add(Block(40, 40, 20, 0.08), 0);
            row.Add(TextPair(150, 190, 0.42), 1);
            row.Add(Block(70, 28, 10, 0.72), 2);
            row.Add(Block(24, 24, 8, 0.9), 3);
            stack.Children.Add(Card(row, 68, 16));
        }
        stack.Children.Add(Block(86, 11, 4, 0.18));
        stack.Children.Add(AvatarRow(66));
        stack.Children.Add(AvatarRow(66));
        SetSkeletonContent(stack);
    }
}

public sealed class AssistantSkeleton : SkeletonViewBase
{
    public AssistantSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Block(132, 12, 4, 0.26),
                Block(230, 18, 5, 0.42),
                Block(260, 12, 4, 0.48)
            }
        }, 128, 18, new Thickness(16)));
        stack.Children.Add(ChipRow(54, 82, 68, 82));
        for (var i = 0; i < 4; i++)
            stack.Children.Add(TaskRow(90));
        stack.Children.Add(Card(TextPair(180, 245, 0.42), 94, 16));
        SetSkeletonContent(stack);
    }
}

public sealed class NotificationSkeleton : SkeletonViewBase
{
    public NotificationSkeleton()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        for (var i = 0; i < 6; i++)
        {
            var row = Grid(FixedColumn(44), StarColumn(), FixedColumn(12));
            row.ColumnSpacing = 12;
            row.Add(Block(44, 44, 12, 0.08), 0);
            row.Add(TextPair(i % 2 == 0 ? 188 : 142, 224, 0.44), 1);
            row.Add(Block(8, 8, 4, 0.92), 2);
            stack.Children.Add(Card(row, 74, 16));
        }
        SetSkeletonContent(stack);
    }
}
