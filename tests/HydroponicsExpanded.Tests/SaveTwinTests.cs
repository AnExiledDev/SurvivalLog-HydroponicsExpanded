using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

public class SaveTwinTests
{
    private static readonly IReadOnlyList<StationKind> Kinds = StationRows.All;

    private static StationKind Kind(string size) => StationRows.All.Single(kind => kind.Size == size);

    private static SaveTwin.Fields Placed(StationKind kind) => new() { AgentConfigId = kind.Ids.Furniture, PlantFurnitureConfigId = kind.Ids.Plant };

    // Without the mod the game must find ids it knows, or the save does not load: the furniture and its plant row both.
    [Theory]
    [InlineData("Small", 60006, 17)]
    [InlineData("Medium", 60007, 18)]
    public void APlacedStationIsSavedAsTheGrowBoxOfItsSize(string size, int growBox, int growBoxPlant)
    {
        var kind = Kind(size);
        var save = Placed(kind);

        Assert.True(SaveTwin.ToStock(ref save, Kinds));
        Assert.Equal((growBox, growBoxPlant), (save.AgentConfigId, save.PlantFurnitureConfigId));
        Assert.StartsWith(kind.SaveMarker, save.Name);
        Assert.StartsWith(SaveTwin.MarkerStart, save.Name);
    }

    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void LoadingWithTheModTurnsItBackIntoTheSameStation(string size)
    {
        var kind = Kind(size);
        var save = Placed(kind);

        SaveTwin.ToStock(ref save, Kinds);

        Assert.True(SaveTwin.FromStock(ref save, Kinds));
        Assert.Equal((kind.Ids.Furniture, kind.Ids.Plant), (save.AgentConfigId, save.PlantFurnitureConfigId));
        Assert.Null(save.Name);
    }

    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void APackedStationSwapsItsPackedIdToo(string size)
    {
        var kind = Kind(size);
        var save = new SaveTwin.Fields { AgentConfigId = 7, IsBagFurniture = true, BagFurnitureConfigId = kind.Ids.Furniture };

        SaveTwin.ToStock(ref save, Kinds);

        Assert.Equal((7, kind.Stock.Furniture), (save.AgentConfigId, save.BagFurnitureConfigId));

        SaveTwin.FromStock(ref save, Kinds);

        Assert.Equal((7, kind.Ids.Furniture), (save.AgentConfigId, save.BagFurnitureConfigId));
    }

    // A station saved with some other plant row (none yet, say) keeps it: only our own plant row is swapped.
    [Fact]
    public void APlantRowThatIsNotOursIsLeftAsItIs()
    {
        var kind = StationRows.Small;
        var save = new SaveTwin.Fields { AgentConfigId = kind.Ids.Furniture, PlantFurnitureConfigId = 0 };

        SaveTwin.ToStock(ref save, Kinds);

        Assert.Equal(0, save.PlantFurnitureConfigId);
        Assert.DoesNotContain("c", save.Name!.Substring(kind.SaveMarker.Length));

        SaveTwin.FromStock(ref save, Kinds);

        Assert.Equal((kind.Ids.Furniture, 0), (save.AgentConfigId, save.PlantFurnitureConfigId));
    }

    // Some other furniture's plant row matching ours by chance must not be swapped when nothing of ours is in the save.
    [Fact]
    public void OurPlantRowAloneDoesNotMakeAStation()
    {
        var save = new SaveTwin.Fields { AgentConfigId = 60009, PlantFurnitureConfigId = StationRows.Small.Ids.Plant };

        Assert.False(SaveTwin.ToStock(ref save, Kinds));
        Assert.Equal((60009, StationRows.Small.Ids.Plant), (save.AgentConfigId, save.PlantFurnitureConfigId));
    }

    [Fact]
    public void NeitherMarkerIsReadAsTheOther()
    {
        foreach (var kinds in new[] { Kinds, Kinds.Reverse().ToList() })
        foreach (var kind in Kinds)
        {
            var save = new SaveTwin.Fields { AgentConfigId = kind.Stock.Furniture, PlantFurnitureConfigId = kind.Stock.Plant, Name = kind.SaveMarker + "ac" };

            Assert.True(SaveTwin.FromStock(ref save, kinds));
            Assert.Equal((kind.Ids.Furniture, kind.Ids.Plant), (save.AgentConfigId, save.PlantFurnitureConfigId));
        }
    }

    // A real grow box has no marker, so loading must never turn it into a station.
    [Theory]
    [InlineData(60006, 17)]
    [InlineData(60007, 18)]
    public void ARealGrowBoxStaysAGrowBox(int growBox, int plant)
    {
        var save = new SaveTwin.Fields { AgentConfigId = growBox, PlantFurnitureConfigId = plant };

        Assert.False(SaveTwin.ToStock(ref save, Kinds));
        Assert.False(SaveTwin.FromStock(ref save, Kinds));
        Assert.Equal((growBox, plant), (save.AgentConfigId, save.PlantFurnitureConfigId));
        Assert.Null(save.Name);
    }

    // Only the ids the marker names turn back: a marked packed id never changes the placed one.
    [Fact]
    public void OnlyTheIdsTheMarkerNamesAreTurnedBack()
    {
        var kind = StationRows.Medium;
        var save = new SaveTwin.Fields { AgentConfigId = kind.Stock.Furniture, IsBagFurniture = true, BagFurnitureConfigId = kind.Stock.Furniture, PlantFurnitureConfigId = kind.Stock.Plant, Name = kind.SaveMarker + "b" };

        SaveTwin.FromStock(ref save, Kinds);

        Assert.Equal((kind.Stock.Furniture, kind.Ids.Furniture, kind.Stock.Plant), (save.AgentConfigId, save.BagFurnitureConfigId, save.PlantFurnitureConfigId));
    }

    [Fact]
    public void ASaveThatAlreadyHasANameIsLeftAlone()
    {
        var save = Placed(StationRows.Medium);

        save.Name = "Kitchen";

        Assert.False(SaveTwin.ToStock(ref save, Kinds));
        Assert.Equal((StationRows.Medium.Ids.Furniture, "Kitchen"), (save.AgentConfigId, save.Name));
    }

    [Fact]
    public void OtherFurnitureIsNotTouched()
    {
        var save = new SaveTwin.Fields { AgentConfigId = 60009, PlantFurnitureConfigId = 20, Name = "Kitchen" };

        Assert.False(SaveTwin.FromStock(ref save, Kinds));

        save.Name = null;

        Assert.False(SaveTwin.ToStock(ref save, Kinds));
        Assert.Equal((60009, 20), (save.AgentConfigId, save.PlantFurnitureConfigId));
        Assert.Null(save.Name);
    }
}
