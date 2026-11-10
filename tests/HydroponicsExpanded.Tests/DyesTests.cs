using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

// The crafting card offers a colour only when a recipe of the plain recipe's materials plus one dye crafts a package of
// the same furniture, so each station needs those rows, at ids nothing else uses, copied from the matching grow box.
public class DyesTests
{
    private static readonly int[] Materials = { 20102, 20104, 20210, 20361 };

    private static Dictionary<string, object> Fields(RowPlan row) => row.Fields.ToDictionary(field => field.Name, field => field.Value);

    [Fact]
    public void EachColourRecipeIsThePlainMaterialsPlusItsOneDye()
    {
        foreach (var kind in StationRows.All)
        foreach (var dye in Dyes.All)
        {
            var recipe = Fields(Dyes.Rows(kind, dye, Materials).Single(row => row.Table == Table.Recipe));

            Assert.Equal(Materials.Append(dye.Item), (int[])recipe["MaterialList"]);
            Assert.Equal(new[] { Dyes.Ids(kind.Ids, dye).Item }, (int[])recipe["ProductID"]);
        }
    }

    [Fact]
    public void EachColourPackageUnpacksIntoThePlainStationThroughItsOwnAction()
    {
        foreach (var kind in StationRows.All)
        foreach (var dye in Dyes.All)
        {
            var ids = Dyes.Ids(kind.Ids, dye);
            var rows = Dyes.Rows(kind, dye, Materials);
            var item = Fields(rows.Single(row => row.Table == Table.Item));
            var action = Fields(rows.Single(row => row.Table == Table.Action));

            Assert.Equal(kind.Ids.Furniture, item["TargetFurnitureID"]);
            Assert.Equal(ids.Action, item["UseAction"]);
            Assert.Equal(ids.Effect, action["EffectConfigID"]);
            Assert.NotEqual(kind.Ids.Action, ids.Action);
        }
    }

    [Fact]
    public void NoTwoRowsShareAnIdAndNoneTakesAPlainStationsId()
    {
        var plain = StationRows.All.SelectMany(kind => new[] { (Table.Item, kind.Ids.Item), (Table.Recipe, kind.Ids.Recipe), (Table.Action, kind.Ids.Action), (Table.Effect, kind.Ids.Effect) });
        var dyed = StationRows.All.SelectMany(kind => Dyes.All.SelectMany(dye => Dyes.Rows(kind, dye, Materials).Select(row => (row.Table, row.Id)))).ToList();

        Assert.Equal(dyed.Count, dyed.Distinct().Count());
        Assert.Empty(dyed.Intersect(plain));
    }

    // From the Data Dump (game 1.1.20719): 14454 is the Sage Green Small Grow Box Package and 14469 the Ochre Red
    // Medium one, which unpacks through action 100014469; recipes 1377 to 1384 are the Large Station's coloured ones.
    [Fact]
    public void TheRowsAreCopiedFromTheGrowBoxOfTheSameSizeAndColour()
    {
        var sageSmall = Dyes.StockIds(StationRows.Small, Dyes.All[0]);
        var ochreMedium = Dyes.StockIds(StationRows.Medium, Dyes.All[7]);

        Assert.Equal((14454, 1377), (sageSmall.Item, sageSmall.Recipe));
        Assert.Equal((14469, 1384, 100014469), (ochreMedium.Item, ochreMedium.Recipe, ochreMedium.Action));
        Assert.Equal(20370, Dyes.All[7].Item);
    }

    [Fact]
    public void APackageIsNamedForItsColour()
    {
        var texts = Dyes.Texts(StationRows.Medium, Dyes.All[3]);

        Assert.Equal("Dusk Blue Hydroponic Station (Medium) Package", texts[0].En);
        Assert.Equal("暮蓝全光谱水培站（中）包裹", texts[0].Zh);
        Assert.Equal("Unpack Dusk Blue Hydroponic Station (Medium) Package", texts[1].En);
    }

    [Fact]
    public void AColourPackageIsFoundFromItsId()
    {
        var ids = Dyes.Ids(StationRows.Medium.Ids, Dyes.All[4]);

        Assert.Equal((StationRows.Medium, Dyes.All[4]), Dyes.OfPackage(ids.Item, StationRows.All));
        Assert.Null(Dyes.OfPackage(StationRows.Medium.Ids.Item, StationRows.All));
    }

    [Fact]
    public void UninstallTurnsAColourPackageIntoTheGrowBoxPackageOfThatColour()
    {
        Assert.Equal(14454, Uninstall.StockItem(Dyes.Ids(StationRows.Small.Ids, Dyes.All[0]).Item, StationRows.All));
        Assert.Equal(14469, Uninstall.StockItem(Dyes.Ids(StationRows.Medium.Ids, Dyes.All[7]).Item, StationRows.All));
    }

    [Fact]
    public void ADyeInTheMaterialsSettingIsRefused()
    {
        var (materials, problem) = MaterialList.Choose("20102:2, 20366:1", StationRows.Medium, _ => true, _ => null, Array.Empty<(StationKind, IReadOnlyList<int>, bool)>());

        Assert.NotNull(problem);
        Assert.Equal(MaterialList.Standard(StationRows.Medium.Defaults.Materials), materials);
    }

    [Fact]
    public void NoStationsDefaultMaterialsHoldADye()
    {
        foreach (var kind in StationRows.All) Assert.DoesNotContain(MaterialList.Standard(kind.Defaults.Materials), Dyes.IsDye);
    }

    // The coloured icon is the plain one put through the dye shader: the orange frame takes the dye, and the rest of
    // the picture, and every pixel's transparency, stays as it was.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    public void AColourIconRecoloursOnlyThePaint(string size)
    {
        var kind = StationRows.All.Single(kind => kind.Size == size);
        var plain = Png.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "assets", kind.IconFile)));
        var mask = Paint.Mask(plain).Rgba;
        var blue = Paint.DyedIcons(plain, new[] { Dyes.All[3].Colour }).Single();
        int changedPaint = 0, paint = 0;

        for (var p = 0; p < plain.Width * plain.Height; p++)
        {
            Assert.Equal(plain.Rgba[p * 4 + 3], blue.Rgba[p * 4 + 3]);

            if (mask[p * 4] == 0)
            {
                Assert.Equal((plain.Rgba[p * 4], plain.Rgba[p * 4 + 1], plain.Rgba[p * 4 + 2]), (blue.Rgba[p * 4], blue.Rgba[p * 4 + 1], blue.Rgba[p * 4 + 2]));

                continue;
            }

            if (mask[p * 4] < 200 || plain.Rgba[p * 4 + 3] == 0) continue;

            paint++;

            // Dusk Blue has more blue than red; the orange paint had more red than blue.
            if (blue.Rgba[p * 4 + 2] > blue.Rgba[p * 4]) changedPaint++;
        }

        Assert.True(paint > 1000, $"only {paint} painted pixels in the icon");
        Assert.True(changedPaint > paint * 9 / 10, $"{changedPaint} of {paint} painted pixels turned blue");
    }

    [Fact]
    public void AnEncodedPictureDecodesToTheSamePixels()
    {
        var picture = new Picture(3, 2, Enumerable.Range(0, 24).Select(i => (byte)(i * 10)).ToArray());

        var back = Png.Decode(Png.Encode(picture));

        Assert.Equal((3, 2), (back.Width, back.Height));
        Assert.Equal(picture.Rgba, back.Rgba);
    }
}
