using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

/// <summary>
/// The stations' materials and returns are settled once for the table rows and the Mod Manager's questions alike, so
/// what the Mods screen says about a list is what the game then does with it.
/// </summary>
public class StationChoicesTests
{
    private static bool AnyItem(int id) => true;

    private static int? NoStockRecipe(IReadOnlyList<int> materials) => null;

    /// <summary>The settings in the order of StationRows.All (Small, Medium), each from its defaults with changes.</summary>
    private static List<StationSettings> Settings(Func<StationKind, StationSettings, StationSettings>? change = null) =>
        StationRows.All.Select(kind => change == null ? kind.Defaults : change(kind, kind.Defaults)).ToList();

    [Fact]
    public void WithTheDefaultsEveryListCanBeUsedAndABlankReturnsGivesBackHalf()
    {
        var choices = StationChoices.Of(Settings(), AnyItem, NoStockRecipe);

        Assert.All(new[] { "SmallStation", "MediumStation" }, section =>
        {
            Assert.Equal("", StationChoices.Problem(choices, section, "Materials"));
            Assert.Equal("", StationChoices.Problem(choices, section, "Returns"));
        });
        Assert.Equal("20102:1,20210:1", StationChoices.Blank(choices, "MediumStation", "Returns"));
        Assert.Equal(MaterialList.Standard("20102:1, 20210:1"), choices[1].Back);
        Assert.Equal("Follows Return percent: 50% of the materials.\n按「Return percent」返还材料的 50%。", StationChoices.BlankNote(choices, "MediumStation", "Returns"));
    }

    [Fact]
    public void TheBlankReturnsFollowsTheReturnPercent()
    {
        var choices = StationChoices.Of(Settings((kind, s) => s with { ReturnPercent = 100 }), AnyItem, NoStockRecipe);

        Assert.Equal("20102:1,20104:1,20210:2,20361:1", StationChoices.Blank(choices, "MediumStation", "Returns"));
        Assert.StartsWith("Follows Return percent: 100%", StationChoices.BlankNote(choices, "MediumStation", "Returns"));
    }

    // Small comes before Medium, so Medium's default is kept for it.
    [Fact]
    public void AStationCannotTakeTheLaterStationsDefaultAndTheScreenSaysWhich()
    {
        var clash = StationChoices.Of(Settings((kind, s) => kind == StationRows.Small ? s with { Materials = StationRows.Medium.Defaults.Materials } : s), AnyItem, NoStockRecipe);

        Assert.StartsWith("These are the Hydroponic Station (Medium)'s default materials", StationChoices.Problem(clash, "SmallStation", "Materials"));
        Assert.Equal(MaterialList.Standard(StationRows.Small.Defaults.Materials), clash[0].Materials);
    }

    [Fact]
    public void AStationThatTakesTheSameMaterialsAsAnEarlierOneIsToldWhich()
    {
        var choices = StationChoices.Of(Settings((kind, s) => kind == StationRows.Small ? s with { Materials = "20102:2" } : s with { Materials = "20102:2" }), AnyItem, NoStockRecipe);

        Assert.Equal("", StationChoices.Problem(choices, "SmallStation", "Materials"));
        Assert.StartsWith("The Hydroponic Station (Small) already takes exactly these materials.", StationChoices.Problem(choices, "MediumStation", "Materials"));
    }

    // What a blank Returns gives back follows the materials the station really uses, its default when its own fail.
    [Fact]
    public void ABlankReturnsFollowsTheMaterialsInUse()
    {
        var choices = StationChoices.Of(Settings((kind, s) => kind == StationRows.Small ? s with { Materials = "20312:4" } : s with { Materials = "99999:2" }), id => id != 99999, NoStockRecipe);

        Assert.Equal("20312:2", StationChoices.Blank(choices, "SmallStation", "Returns"));
        Assert.Equal(new[] { 20312, 20312 }, choices[0].Back);
        Assert.Equal("20102:1,20210:1", StationChoices.Blank(choices, "MediumStation", "Returns"));
        Assert.StartsWith("The game has no item 99999,", StationChoices.Problem(choices, "MediumStation", "Materials"));
    }

    [Fact]
    public void AReturnsListSetByHandIsWhatComesBack()
    {
        var choices = StationChoices.Of(Settings((kind, s) => kind == StationRows.Small ? s with { Returns = "20312:2" } : s with { Returns = "none" }), AnyItem, NoStockRecipe);

        Assert.Equal(new[] { 20312, 20312 }, choices[0].Back);
        Assert.Empty(choices[1].Back);
        Assert.Equal("", StationChoices.Problem(choices, "SmallStation", "Returns"));
        Assert.Equal("20102:1,20104:1", StationChoices.Blank(choices, "SmallStation", "Returns"));
    }

    [Fact]
    public void AReturnsListTheGameCannotUseIsExplainedOnTheScreen()
    {
        var choices = StationChoices.Of(Settings((kind, s) => kind == StationRows.Small ? s with { Returns = "99999:1" } : s), id => id != 99999, NoStockRecipe);

        Assert.StartsWith("The game has no item 99999, so taking the station down gives back", StationChoices.Problem(choices, "SmallStation", "Returns"));
        Assert.Equal(MaterialList.Standard("20102:1, 20104:1"), choices[0].Back);
    }

    [Fact]
    public void OtherSettingsGetNoAnswer()
    {
        var choices = StationChoices.Of(Settings((kind, s) => s with { Materials = "" }), AnyItem, NoStockRecipe);

        Assert.Equal("", StationChoices.Problem(choices, "SmallStation", "PowerCost"));
        Assert.Equal("", StationChoices.Problem(choices, "Dismantle", "ReturnPercent"));
        Assert.Equal("", StationChoices.Blank(choices, "SmallStation", "Materials"));
        Assert.Equal("", StationChoices.BlankNote(choices, "SmallStation", "Materials"));
        Assert.NotEqual("", StationChoices.Problem(choices, "SmallStation", "Materials"));
    }
}
