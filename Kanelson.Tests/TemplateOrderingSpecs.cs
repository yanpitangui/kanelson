using AwesomeAssertions;
using MudBlazor;
using MudBlazor.Utilities;

namespace Kanelson.Tests;

public class TemplateOrderingSpecs
{
    private sealed class Item
    {
        public Guid Id { get; init; }
        public string Selector { get; set; } = "1";
        public int Order { get; set; }
        public int Position { get; set; }
    }

    private static List<Item> Zone2Items(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new Item { Id = Guid.NewGuid(), Selector = "2", Order = i })
            .ToList();

    // Mirrors the component's RefreshPositions: use physical list order, not Order sort
    private static void RefreshPositions(List<Item> items)
    {
        var pos = 1;
        foreach (var item in items.Where(x => x.Selector == "2"))
            item.Position = pos++;
    }

    // Mirrors the component's ItemUpdated reorder step
    private static void PhysicallyReorderByOrder(List<Item> items)
    {
        var reordered = items.OrderBy(x => x.Selector == "1" ? int.MinValue : x.Order).ToList();
        items.Clear();
        items.AddRange(reordered);
    }

    [Fact]
    public void UpdateOrder_updates_Order_property_on_items()
    {
        var items = Zone2Items(3); // Orders: 0, 1, 2
        var last = items[2];       // Order = 2

        var drop = new MudItemDropInfo<Item>(last, "2", 0);
        items.UpdateOrder(drop, x => x.Order);

        last.Order.Should().Be(0, "moved item should have Order updated to its new position");
    }

    [Fact]
    public void UpdateOrder_does_not_physically_reorder_the_list()
    {
        var items = Zone2Items(3);
        var originalFirst = items[0];

        var drop = new MudItemDropInfo<Item>(items[2], "2", 0);
        items.UpdateOrder(drop, x => x.Order);

        items[0].Should().BeSameAs(originalFirst,
            "UpdateOrder updates .Order property but does not move items in the list");
    }

    [Fact]
    public void After_drop_to_first_position_moved_item_has_Position_1()
    {
        var items = Zone2Items(3); // Orders: 0, 1, 2
        var last = items[2];

        var drop = new MudItemDropInfo<Item>(last, "2", 0);
        items.UpdateOrder(drop, x => x.Order);
        PhysicallyReorderByOrder(items);
        RefreshPositions(items);

        // After reorder, physical list should be [last(Order=0), items[0](Order=1), items[1](Order=2)]
        items[0].Should().BeSameAs(last, "moved item should be physically first in list");
        last.Position.Should().Be(1, "first physical item = Position 1");
        items[1].Position.Should().Be(2);
        items[2].Position.Should().Be(3);
    }

    [Fact]
    public void After_drop_to_last_position_moved_item_has_correct_Position()
    {
        var items = Zone2Items(3); // Orders: 0, 1, 2
        var first = items[0];

        var drop = new MudItemDropInfo<Item>(first, "2", 2);
        items.UpdateOrder(drop, x => x.Order);
        PhysicallyReorderByOrder(items);
        RefreshPositions(items);

        items[2].Should().BeSameAs(first, "moved item should be physically last in list");
        first.Position.Should().Be(3, "last physical item = Position 3");
    }
}
