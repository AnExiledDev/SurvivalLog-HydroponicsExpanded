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

    private const int Large = 319;
    private const int Small = 9201;
    private const int Medium = 9202;

    // The defaults: Small at Planting 3, Medium at 4, Large at 5.
    private static int[] Due(int planting, params int[] unlocked) => Due(planting, 5, 3, 4, unlocked);

    private static int[] Due(int planting, int largeLevel, int smallLevel, int mediumLevel, params int[] unlocked) =>
        Unlocks.RecipesDue(planting, (Large, largeLevel), new[] { (Small, smallLevel), (Medium, mediumLevel) }, unlocked).OrderBy(id => id).ToArray();

    // Each station's recipe opens once Planting reaches its level, one size at a time.
    [Theory]
    [InlineData(0, new int[0])]
    [InlineData(2, new int[0])]
    [InlineData(3, new[] { Small })]
    [InlineData(4, new[] { Small, Medium })]
    [InlineData(5, new[] { Large, Small, Medium })]
    public void EachStationUnlocksAtItsPlantingLevel(int planting, int[] expected)
    {
        Assert.Equal(expected.OrderBy(id => id), Due(planting));
    }

    // The Hydroponic Cultivation talent unlocks only the Large Station's recipe, so a run that has it, at any level,
    // gets the Small and the Medium too: a run already going when the mod arrives, or one that took the talent early.
    [Fact]
    public void TheLargeStationsRecipeBringsTheOthersWithIt()
    {
        Assert.Equal(new[] { Small, Medium }, Due(0, Large));
        Assert.Equal(new[] { Medium }, Due(3, Large, Small));
    }

    // Nothing is unlocked twice, so the check can run every few seconds.
    [Fact]
    public void ARecipeAlreadyUnlockedIsNotDueAgain()
    {
        Assert.Empty(Due(5, Large, Small, Medium));
        Assert.Empty(Due(4, Small, Medium));
    }

    // A level of 0 leaves that station to the talent alone.
    [Fact]
    public void ALevelOfZeroNeverUnlocksThatStation()
    {
        Assert.Equal(new[] { Medium }, Due(5, 0, 0, 4));
        Assert.Equal(new[] { Small, Medium }, Due(5, 0, 0, 4, Large));
        Assert.Equal(new[] { Small, Medium }, Due(1, 0, 0, 0, Large));
    }

    [Fact]
    public void By_default_no_planting_level_hands_out_the_large_stations_recipe()
    {
        // The Large is the game's own station; its recipe stays the talent's until the player sets a level.
        var due = Unlocks.RecipesDue(Unlocks.TopPlantingLevel, (StationRows.LargeRecipe, StationRows.LargeUnlockLevel), new (int, int)[0], new int[0]);

        Assert.DoesNotContain(StationRows.LargeRecipe, due);
    }
}
