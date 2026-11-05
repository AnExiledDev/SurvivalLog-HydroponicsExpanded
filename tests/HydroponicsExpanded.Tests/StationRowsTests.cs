using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

public class StationRowsTests
{
    private static StationKind Kind(string size) => StationRows.All.Single(kind => kind.Size == size);

    private static StationPlan Plan(StationKind kind) => StationRows.Plan(kind, MaterialList.Standard(kind.Defaults.Materials), kind.Defaults);

    private static RowPlan Row(StationPlan plan, Table table) => plan.Rows.Single(row => row.Table == table);

    private static object Field(StationPlan plan, Table table, string name) => Row(plan, table).Fields.Single(field => field.Name == name).Value;

    // The game follows recipe -> package -> unpack action -> effect, and furniture -> plant row. One wrong link makes a
    // recipe that crafts a grow box, or a station that grows like one.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void EveryLinkPointsAtTheStationsOwnRows(string size)
    {
        var plan = Plan(Kind(size));

        Assert.Equal(new[] { Row(plan, Table.Item).Id }, (int[])Field(plan, Table.Recipe, "ProductID"));
        Assert.Equal(Row(plan, Table.Furniture).Id, Field(plan, Table.Item, "TargetFurnitureID"));
        Assert.Equal(Row(plan, Table.Action).Id, Field(plan, Table.Item, "UseAction"));
        Assert.Equal(Row(plan, Table.Effect).Id, Field(plan, Table.Action, "EffectConfigID"));
        Assert.Equal(Row(plan, Table.Plant).Id, Field(plan, Table.Furniture, "PlantFurnitureID"));
    }

    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void EachRowSetsItsOwnIdField(string size)
    {
        var plan = Plan(Kind(size));

        foreach (var row in plan.Rows) Assert.Equal(row.Id, Field(plan, row.Table, "ID"));
    }

    // The package, action, effect and furniture copy the grow box of the same size, so the station takes its spot and
    // installs the same way; the recipe and the plant row copy the Large Station's, so it grows like it.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void EachRowCopiesTheRightStockRowUnderANewId(string size)
    {
        var kind = Kind(size);
        var copied = Plan(kind).Rows.ToDictionary(row => row.Table, row => row.CopyFrom);

        Assert.Equal(StationRows.LargeRecipe, copied[Table.Recipe]);
        Assert.Equal(kind.Stock.Item, copied[Table.Item]);
        Assert.Equal(kind.Stock.Action, copied[Table.Action]);
        Assert.Equal(kind.Stock.Effect, copied[Table.Effect]);
        Assert.Equal(kind.Stock.Furniture, copied[Table.Furniture]);
        Assert.Equal(StationRows.LargePlant, copied[Table.Plant]);
        Assert.All(Plan(kind).Rows, row => Assert.NotEqual(row.CopyFrom, row.Id));
    }

    [Fact]
    public void EachStationStandsInForTheGrowBoxOfItsSize()
    {
        Assert.Equal((60006, 14067, 17, "P_FlowerPot_011_S1_M01deng"), (StationRows.Small.Stock.Furniture, StationRows.Small.Stock.Item, StationRows.Small.Stock.Plant, StationRows.Small.StockPrefab));
        Assert.Equal((60007, 14068, 18, "P_FlowerPot_011_S1_M02_deng"), (StationRows.Medium.Stock.Furniture, StationRows.Medium.Stock.Item, StationRows.Medium.Stock.Plant, StationRows.Medium.StockPrefab));
    }

    // The Large Station's plant row holds four plants; ours hold what the grow box of the same size holds.
    [Theory]
    [InlineData("Small", 1)]
    [InlineData("Medium", 2)]
    public void ThePlantRowHoldsTheGrowBoxesPlantCount(string size, int plants)
    {
        Assert.Equal(plants, Field(Plan(Kind(size)), Table.Plant, "Capacity"));
    }

    [Fact]
    public void TheSettingsGoIntoTheRows()
    {
        var plan = StationRows.Plan(StationRows.Medium, new[] { 20104 }, new StationSettings(5, 1.25f, "20104:1", 75, 3));

        Assert.Equal(5, Field(plan, Table.Furniture, "PowerCost"));
        Assert.Equal(75, Field(plan, Table.Furniture, "FurniturePrice"));
        Assert.Equal(1.25f, Field(plan, Table.Plant, "GrowthFaster"));
    }

    // The design: "Same stats as the Large, with the power draw scaled down by size". The Large draws 6 for four plants.
    [Fact]
    public void TheDefaultsAreTheLargeStationsWithThePowerScaledBySize()
    {
        Assert.Equal(new StationSettings(2, 0.5f, "20102:1, 20104:1, 20210:1, 20361:1", 60, 2), StationRows.Small.Defaults);
        Assert.Equal(new StationSettings(3, 0.5f, "20102:1, 20104:1, 20210:2, 20361:1", 100, 2), StationRows.Medium.Defaults);
    }

    [Fact]
    public void TheMaterialsAreTheOnesGiven()
    {
        var plan = StationRows.Plan(StationRows.Small, new[] { 20102, 20104, 20104 }, StationRows.Small.Defaults);

        Assert.Equal(new[] { 20102, 20104, 20104 }, (int[])Field(plan, Table.Recipe, "MaterialList"));
        Assert.Equal(new[] { 20102, 20104, 20104 }, (int[])Field(plan, Table.Recipe, "perfect_item_id"));
    }

    // The game's own recipe for the Large Station, as recipe 319 lists it in 1.1.20151. Two recipes with one key are
    // filed as one, and only one of them shows at the workbench.
    private static readonly int[] LargeMaterials = { 20102, 20104, 20210, 20210, 20361, 20362 };

    [Fact]
    public void NoDefaultRecipeIsFiledUnderAnotherOnesKey()
    {
        var keys = StationRows.All.Select(kind => StationRows.MaterialKey(MaterialList.Standard(kind.Defaults.Materials))).Append(StationRows.MaterialKey(LargeMaterials)).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void NoTwoStationsShareAnIdInAnyTable()
    {
        var rows = StationRows.All.SelectMany(kind => Plan(kind).Rows).Select(row => (row.Table, row.Id)).ToList();

        Assert.Equal(rows.Count, rows.Distinct().Count());
    }

    // GetLocalTxt shows the key itself when it is missing, and the _Local fields hold the Chinese text directly.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void EveryTextKeyHasEnglishAndChineseAndEachLocalFieldMatchesItsKey(string size)
    {
        var plan = Plan(Kind(size));
        var texts = plan.Texts.ToDictionary(text => text.Key);

        foreach (var row in plan.Rows)
        {
            foreach (var field in row.Fields.Where(field => field.Name.EndsWith("_Local")))
            {
                var key = (string)Field(plan, row.Table, field.Name[..^"_Local".Length]);

                Assert.True(texts.ContainsKey(key), $"{row.Table}.{field.Name}: no text for {key}");
                Assert.Equal(texts[key].Zh, field.Value);
            }
        }

        Assert.All(plan.Texts, text =>
        {
            Assert.False(string.IsNullOrWhiteSpace(text.En));
            Assert.False(string.IsNullOrWhiteSpace(text.Zh));
        });
    }

    [Fact]
    public void NoTextKeyIsUsedTwice()
    {
        var keys = StationRows.All.SelectMany(kind => kind.Texts).Select(text => text.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    // The web UI finds the furniture's and the package's icon at the path the rows give, next to the game's own folders.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void TheIconPathIsTheModsFolder(string size)
    {
        var kind = Kind(size);
        var plan = Plan(kind);

        Assert.Equal("../../Res/Mods/HydroponicsExpanded/" + kind.IconFile, Field(plan, Table.Furniture, "ICON"));
        Assert.Equal(kind.WebIcon, Field(plan, Table.Item, "WebIcon"));
        Assert.Equal(kind.WebIcon, Field(plan, Table.Action, "WebIcon"));
    }

    [Fact]
    public void AnEmptyGameWithAllStockRowsHasNoProblems()
    {
        var kind = StationRows.Medium;

        Assert.Empty(StationRows.Problems(Plan(kind), (table, id) => IsStock(kind, table, id) ? Slot.Stock : Slot.Empty, NoOtherRecipe));
    }

    [Fact]
    public void RowsWeWroteEarlierCanBeWrittenAgain()
    {
        var kind = StationRows.Small;

        Assert.Empty(StationRows.Problems(Plan(kind), (table, id) => IsStock(kind, table, id) ? Slot.Stock : Slot.Ours, NoOtherRecipe));
    }

    [Fact]
    public void AGameRowAtOneOfOurIdsStopsTheWholeStation()
    {
        var kind = StationRows.Medium;
        var problems = StationRows.Problems(Plan(kind), (table, id) => IsStock(kind, table, id) || (table == Table.Plant && id == kind.Ids.Plant) ? Slot.Stock : Slot.Empty, NoOtherRecipe);

        Assert.Equal(new[] { $"Plant {kind.Ids.Plant} is already used by the game" }, problems);
    }

    [Fact]
    public void AMissingLargeStationPlantRowIsReported()
    {
        var kind = StationRows.Small;
        var problems = StationRows.Problems(Plan(kind), (table, id) => table == Table.Plant && id == StationRows.LargePlant ? Slot.Empty : IsStock(kind, table, id) ? Slot.Stock : Slot.Empty, NoOtherRecipe);

        Assert.Equal(new[] { $"Plant {StationRows.LargePlant}, the row to copy, is missing" }, problems);
    }

    [Fact]
    public void AnotherRecipeWithTheSameMaterialsStopsTheWholeStation()
    {
        var kind = StationRows.Medium;
        var ours = StationRows.MaterialKey(MaterialList.Standard(kind.Defaults.Materials));
        var problems = StationRows.Problems(Plan(kind), (table, id) => IsStock(kind, table, id) ? Slot.Stock : Slot.Empty, materials => StationRows.MaterialKey(materials) == ours ? 555 : null);

        Assert.Equal(new[] { "Recipe 555 already uses the same materials, so only one of the two could be crafted" }, problems);
    }

    // Taking a station down gives back about half of what it was made from, like the game's own furniture, so the grow
    // box's leftovers never come out of a station.
    [Fact]
    public void TakingAStationDownGivesBackHalfOfItsOwnMaterials()
    {
        var plan = StationRows.Plan(StationRows.Small, new[] { 20102, 20102, 20102, 20104, 20210, 20210 }, StationRows.Small.Defaults);

        Assert.Equal(new[] { 20102, 20102, 20210 }, (int[])Field(plan, Table.Furniture, "RemoveGet"));
    }

    [Fact]
    public void MaterialKeyIgnoresOrderButNotCount()
    {
        Assert.Equal(StationRows.MaterialKey(new[] { 3, 1, 2, 1 }), StationRows.MaterialKey(new[] { 1, 1, 2, 3 }));
        Assert.NotEqual(StationRows.MaterialKey(new[] { 1, 2 }), StationRows.MaterialKey(new[] { 1, 2, 2 }));
    }

    // The grow boxes lay their spots out half a metre from the middle; the station's bays are 0.4525 out, pots 0.535 up.
    [Theory]
    [InlineData(0f, 0f, 0f, 0f)]
    [InlineData(0.5f, 0f, 0.4525f, 0f)]
    [InlineData(-0.5f, 0f, -0.4525f, 0f)]
    [InlineData(0.5f, -0.5f, 0.4525f, -0.4525f)]
    [InlineData(0.05f, -0.09f, 0f, 0f)]
    public void EachPlantSpotGoesToTheBayOnItsSide(float x, float z, float bayX, float bayZ)
    {
        Assert.Equal((bayX, 0.535f, bayZ), StationRows.PlantSpot(x, z, 0.4525f, 0.535f));
    }

    private static int? NoOtherRecipe(IReadOnlyList<int> materials) => null;

    private static bool IsStock(StationKind kind, Table table, int id) => table switch
    {
        Table.Item => id == kind.Stock.Item,
        Table.Furniture => id == kind.Stock.Furniture,
        Table.Plant => id == kind.Stock.Plant || id == StationRows.LargePlant,
        Table.Recipe => id == kind.Stock.Recipe,
        Table.Action => id == kind.Stock.Action,
        Table.Effect => id == kind.Stock.Effect,
        _ => false,
    };
}
