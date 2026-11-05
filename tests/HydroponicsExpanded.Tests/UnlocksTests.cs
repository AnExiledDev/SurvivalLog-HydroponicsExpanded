using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

public class UnlocksTests
{
    private static readonly int[] Ours = { 920001, 920002 };

    [Fact]
    public void AShopGainsEachStationWithItsStockAndKeepsItsOwnGoods()
    {
        var shop = Unlocks.Shop(new[] { 60007, 60009 }, new[] { 3, 1 }, Ours, new[] { (920002, 2), (920001, 2) });

        Assert.Equal(new[] { 60007, 60009, 920002, 920001 }, shop!.Value.Ids);
        Assert.Equal(new[] { 3, 1, 2, 2 }, shop.Value.Counts);
    }

    [Fact]
    public void AShopAlreadyStockedAsSetIsLeftAlone()
    {
        Assert.Null(Unlocks.Shop(new[] { 60007, 920002 }, new[] { 3, 2 }, Ours, new[] { (920002, 2) }));
    }

    [Fact]
    public void ANewStockSettingReplacesTheStationsCount()
    {
        var shop = Unlocks.Shop(new[] { 920002, 60007 }, new[] { 2, 3 }, Ours, new[] { (920002, 5) });

        Assert.Equal(new[] { 60007, 920002 }, shop!.Value.Ids);
        Assert.Equal(new[] { 3, 5 }, shop.Value.Counts);
    }

    [Fact]
    public void AStockOfNoneTakesTheStationOffTheShelves()
    {
        var shop = Unlocks.Shop(new[] { 60007, 920002 }, new[] { 3, 2 }, Ours, new[] { (920002, 0) });

        Assert.Equal(new[] { 60007 }, shop!.Value.Ids);
        Assert.Equal(new[] { 3 }, shop.Value.Counts);
    }

    // A live shop moves by the change in the setting, so stations already bought stay bought.
    [Theory]
    [InlineData(2, 2, 3, 3)]   // nothing bought: the shop fills to the new setting
    [InlineData(1, 2, 3, 2)]   // one bought: one more than before
    [InlineData(2, 2, 0, 0)]   // off the shelves
    [InlineData(0, 2, 1, 0)]   // sold out and lowered: never below none
    [InlineData(5, 2, 3, 3)]   // more than the setting somehow: never above a full shop
    public void ALiveShopsCountMovesByTheChangeInTheSetting(int current, int before, int after, int expected)
    {
        Assert.Equal(expected, Unlocks.NewStock(current, before, after));
    }
}
