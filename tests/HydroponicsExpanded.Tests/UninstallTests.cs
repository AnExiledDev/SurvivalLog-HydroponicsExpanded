using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

// A game without the mod drops what it does not know, so Uninstall prep must turn every station id into the grow box
// of the same size, and leave everything else alone (the game's own Large Station most of all).
public class UninstallTests
{
    [Theory]
    [InlineData("Small", 60006, 14067)]
    [InlineData("Medium", 60007, 14068)]
    public void EachStationBecomesTheGrowBoxOfItsSize(string size, int growBox, int growBoxPackage)
    {
        var kind = StationRows.All.Single(kind => kind.Size == size);

        Assert.Equal(growBox, Uninstall.StockFurniture(kind.Ids.Furniture, StationRows.All));
        Assert.Equal(growBoxPackage, Uninstall.StockItem(kind.Ids.Item, StationRows.All));
    }

    [Theory]
    [InlineData(60006)]
    [InlineData(60009)]
    [InlineData(14019)]
    [InlineData(0)]
    public void AnythingElseIsLeftAlone(int id)
    {
        Assert.Null(Uninstall.StockItem(id, StationRows.All));
        Assert.Null(Uninstall.StockFurniture(id, StationRows.All));
    }

    [Fact]
    public void BoughtStationsAreRecordedAsTheirGrowBoxes()
    {
        var records = new Dictionary<int, int>
        {
            [60007] = 4,
            [StationRows.Medium.Ids.Furniture] = 2,
            [60009] = 1,
            [StationRows.Small.Ids.Furniture] = 0,
        };

        var moves = Uninstall.LedgerMoves(records, StationRows.All);

        Assert.Equal(new[] { (StationRows.Medium.Ids.Furniture, 60007, 2) }, moves);
    }
}
