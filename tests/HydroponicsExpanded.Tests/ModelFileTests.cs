using System.IO.Compression;
using System.Numerics;
using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

public class ModelFileTests
{
    private static byte[] Asset(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "assets", name));

    private static readonly IReadOnlyDictionary<string, Vector3> Movers = new[] { "Lights", "Seats_1", "Seats_2", "Seats_4" }.ToDictionary(name => name, _ => Vector3.UnitY);

    private const float BayOffset = 0.4525f;
    private const float PotTop = 0.535f;

    private static readonly Model Small = Glb.Read(Asset(StationRows.Small.ModelFile), Movers);

    private static readonly Model Medium = Glb.Read(Asset(StationRows.Medium.ModelFile), Movers);

    private static readonly Model Large = Glb.Read(Asset(StationRows.LargeModelFile), Movers);

    private static Model Load(string size) => size switch { "Medium" => Medium, "Small" => Small, _ => Large };

    private static string IconFile(string size) => size == "Large" ? StationRows.LargeIconFile : StationRows.All.Single(kind => kind.Size == size).IconFile;

    // The grow lights are a part of their own, so the mod can hang them under the game's Plant_Light switch and the
    // game turns them off with the power.
    // The bay seats for each plant size are parts of their own too, so the mod can show the ones that fit the crop.
    [Theory]
    [InlineData("Small", new[] { "Seats_1" })]
    [InlineData("Medium", new[] { "Seats_1", "Seats_2" })]
    [InlineData("Large", new[] { "Seats_1", "Seats_2", "Seats_4" })]
    public void TheLightsArePartOfTheirOwnAndHoldOnlyTheGlowingParts(string size, string[] seats)
    {
        var model = Load(size);
        var lights = model.Parts.Single(part => part.Name == "Lights");

        Assert.Equal(new[] { Glb.BodyName, "Lights" }.Concat(seats).OrderBy(name => name, StringComparer.Ordinal), model.Parts.Select(part => part.Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(new[] { "GrowLED", "StatusLED" }, lights.Meshes.Select(mesh => model.Materials[mesh.Material].Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.DoesNotContain(model.Parts.Single(part => part.Name == Glb.BodyName).Meshes, mesh => model.Materials[mesh.Material].Emissive != Vector3.Zero);
    }

    // The LEDs glow, and the grow light glows pink-magenta like real grow lights: red and blue above green.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    [InlineData("Large")]
    public void TheGrowLightGlowsMagentaAndNothingElseButTheStatusLightGlows(string size)
    {
        var materials = Load(size).Materials;
        var grow = materials.Single(m => m.Name == "GrowLED").Emissive;

        Assert.True(grow.X > grow.Y && grow.Z > grow.Y, $"grow light {grow}");
        Assert.NotEqual(Vector3.Zero, materials.Single(m => m.Name == "StatusLED").Emissive);
        Assert.All(materials.Where(m => m.Name is not ("GrowLED" or "StatusLED")), m => Assert.Equal(Vector3.Zero, m.Emissive));
    }

    // Every station stands on the ground, the same height as the others, and its tub fills the grow box's spot:
    // Small 1 m by 1 m, Medium 2 m by 1 m, Large 2 m by 2 m. Taps and pipes reach a little past it.
    [Theory]
    [InlineData("Small", 1f, 1f)]
    [InlineData("Medium", 2f, 1f)]
    [InlineData("Large", 2f, 2f)]
    public void EachStationStandsOnTheGroundAndFitsItsSpot(string size, float spotX, float spotZ)
    {
        var (min, max) = Load(size).Bounds();

        Assert.InRange(min.Y, -0.01f, 0.01f);
        Assert.InRange(max.X - min.X, spotX * 0.9f, spotX + 0.2f);
        Assert.InRange(max.Z - min.Z, spotZ * 0.9f, spotZ + 0.2f);
    }

    // A fully grown crop stands under the grow light, not through it. The tallest crop each station takes, measured from
    // the game's plant models: strawberry 1.24 m (small crops), tomato 1.43 m (medium), corn 2.35 m (large). The arch
    // is no taller than it needs to be, so it fits under a ceiling.
    [Theory]
    [InlineData("Small", 1.24f)]
    [InlineData("Medium", 1.43f)]
    [InlineData("Large", 2.35f)]
    public void TheGrowLightHangsAboveTheTallestCropTheStationTakes(string size, float tallestCrop)
    {
        var model = Load(size);
        var lights = model.Parts.Single(part => part.Name == "Lights");
        var lowest = lights.Meshes.Where(mesh => model.Materials[mesh.Material].Name == "GrowLED").SelectMany(mesh => mesh.Positions).Min(p => p.Y) + lights.Pivot.Y;

        Assert.InRange(lowest - (PotTop + tallestCrop), 0.03f, 0.25f);
    }

    // The stock prefab's light sources go just under the model's own grow light bar, so each station's light starts
    // at its own bar: the Small's lowest, then the Medium's, then the Large's, the height the stock lamp hung at.
    [Fact]
    public void EachStationsLightStartsAtItsOwnBar()
    {
        var bars = new[] { "Small", "Medium", "Large" }.Select(size => Load(size).Lowest("GrowLED")).ToList();

        Assert.All(bars, bar => Assert.NotNull(bar));
        Assert.True(bars[0] < bars[1] && bars[1] < bars[2], $"bars at {string.Join(", ", bars)}");
        Assert.InRange(bars[2]!.Value - bars[0]!.Value, 0.8f, 1.5f);
        Assert.Null(Load("Small").Lowest("NoSuchMaterial"));

        // The light sources go under the bar's lowest face, not inside or above it.
        foreach (var size in new[] { "Small", "Medium", "Large" })
        {
            var model = Load(size);
            var lights = model.Parts.Single(part => part.Name == "Lights");
            var heights = lights.Meshes.Where(mesh => model.Materials[mesh.Material].Name == "GrowLED").SelectMany(mesh => mesh.Positions).Select(p => p.Y + lights.Pivot.Y).ToList();

            Assert.Equal(heights.Min(), model.Lowest("GrowLED"));
        }
    }

    // The game hangs a plant that fills several spots at the middle of those spots, taken in their order in the grow
    // box (the Large's front pair, then its back pair). Each seat set's pots must be right there, so the crop grows out
    // of a pot, with the plant's foot inside the pot rather than over a bay divider or floating above the rim.
    [Theory]
    [InlineData("Small", "Seats_1", 1)]
    [InlineData("Medium", "Seats_1", 2)]
    [InlineData("Medium", "Seats_2", 1)]
    [InlineData("Large", "Seats_1", 4)]
    [InlineData("Large", "Seats_2", 2)]
    [InlineData("Large", "Seats_4", 1)]
    public void EachSeatSetHasAPotWhereTheGameHangsThePlant(string size, string seatsName, int seeds)
    {
        var model = Load(size);
        var seats = model.Parts.Single(part => part.Name == seatsName);
        var pots = seats.Meshes.Where(mesh => model.Materials[mesh.Material].Name == "Dark").SelectMany(mesh => mesh.Positions).Select(p => p + seats.Pivot).ToList();
        var anchors = GameAnchors(StockSpots(size), seeds);

        Assert.NotEmpty(pots);
        Assert.All(pots, p => Assert.Contains(anchors, a => Vector2.Distance(a, new Vector2(p.X, p.Z)) < 0.25f));

        foreach (var anchor in anchors)
        {
            var near = pots.Where(p => Vector2.Distance(anchor, new Vector2(p.X, p.Z)) < 0.25f).ToList();
            var centre = new Vector2((near.Min(p => p.X) + near.Max(p => p.X)) / 2f, (near.Min(p => p.Z) + near.Max(p => p.Z)) / 2f);

            Assert.InRange(Vector2.Distance(anchor, centre), 0f, 0.01f);
            Assert.InRange(near.Max(p => p.Y) - PotTop, 0f, 0.06f);
        }
    }

    // The stock grow boxes' plant spots, in order, as the mod moves them onto the bays.
    private static Vector2[] StockSpots(string size) => (size switch
    {
        "Small" => new[] { (0f, 0f) },
        "Medium" => new[] { (-0.5f, 0f), (0.5f, 0f) },
        _ => new[] { (-0.5f, 0.5f), (0.5f, 0.5f), (-0.5f, -0.5f), (0.5f, -0.5f) },
    }).Select(spot => StationRows.PlantSpot(spot.Item1, spot.Item2, BayOffset, PotTop)).Select(spot => new Vector2(spot.X, spot.Z)).ToArray();

    // As the game does it: the spots are shared out evenly between the seeds, and each plant stands at the middle of its share.
    private static Vector2[] GameAnchors(Vector2[] spots, int seeds)
    {
        var share = spots.Length / Math.Min(seeds, spots.Length);

        return Enumerable.Range(0, spots.Length / share).Select(i => spots.Skip(i * share).Take(share).Aggregate(Vector2.Zero, (sum, spot) => sum + spot) / share).ToArray();
    }

    // The plant spots are worked out from the model's own origin, so the bays must sit around it: the model is not
    // pushed off to one side.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    [InlineData("Large")]
    public void TheStationIsCentredAboutItsOrigin(string size)
    {
        var (min, max) = Load(size).Bounds();
        var centre = (min + max) / 2f;

        Assert.InRange(Math.Abs(centre.X), 0f, 0.15f);
        Assert.InRange(Math.Abs(centre.Z), 0f, 0.15f);
    }

    [Theory]
    [InlineData("Medium")]
    [InlineData("Large")]
    [InlineData("Small")]
    public void EveryMeshIsWholeAndPointsAtARealMaterial(string size)
    {
        var model = Load(size);

        Assert.InRange(model.TriangleCount, 500, 20000);

        foreach (var mesh in model.Parts.SelectMany(part => part.Meshes))
        {
            Assert.InRange(mesh.Material, 0, model.Materials.Count - 1);
            Assert.Equal(mesh.Positions.Length, mesh.Normals.Length);
            Assert.Equal(mesh.Positions.Length, mesh.Uvs.Length);
            Assert.Equal(0, mesh.Triangles.Length % 3);
            Assert.All(mesh.Triangles, index => Assert.InRange(index, 0, mesh.Positions.Length - 1));
        }
    }

    [Theory]
    [InlineData("Medium")]
    [InlineData("Large")]
    [InlineData("Small")]
    public void EveryTextureInTheModelDecodes(string size)
    {
        var model = Load(size);
        var textured = model.Materials.Where(material => material.Image != null).ToList();

        Assert.NotEmpty(textured);

        foreach (var material in textured)
        {
            var picture = Png.Decode(model.Images[material.Image!.Value]);

            Assert.Equal(picture.Width * picture.Height * 4, picture.Rgba.Length);
            Assert.True(picture.Width >= 16 && picture.Height >= 16);
        }
    }

    [Theory]
    [InlineData("Medium")]
    [InlineData("Large")]
    [InlineData("Small")]
    public void TheIconIsTheSameSizeAsTheGamesOwn(string size)
    {
        var icon = Png.Decode(Asset(IconFile(size)));

        Assert.Equal((512, 512), (icon.Width, icon.Height));
    }

    // Unity's axes mirror glTF's left to right, so a point on glTF's +X lands on Unity's -X.
    [Fact]
    public void ModelSpaceIsMirroredIntoUnitysHandedness()
    {
        Assert.Equal(new Vector3(-1, 2, 3), Glb.ToUnity(new Vector3(1, 2, 3)));
    }

    [Fact]
    public void ANonGlbFileIsRefused()
    {
        Assert.Throws<FormatException>(() => Glb.Read(new byte[40], Movers));
    }

    // A 3x2 picture whose rows use filters 1 (left), 2 (up), so the decoder must undo each against the right neighbour.
    [Fact]
    public void FilteredRowsDecodeToTheOriginalPixels()
    {
        var expected = new byte[]
        {
            10, 20, 30, 255, 15, 25, 35, 255, 20, 30, 40, 128,
            11, 21, 31, 255, 16, 26, 36, 254, 25, 35, 45, 120,
        };
        var raw = new List<byte> { 1 };

        for (var x = 0; x < 12; x++) raw.Add((byte)(expected[x] - (x >= 4 ? expected[x - 4] : 0)));

        raw.Add(2);

        for (var x = 0; x < 12; x++) raw.Add((byte)(expected[12 + x] - expected[x]));

        var picture = Png.Decode(MakePng(3, 2, raw.ToArray()));

        Assert.Equal(expected, picture.Rgba);
    }

    [Fact]
    public void AnInterlacedPngIsRefused()
    {
        var png = MakePng(1, 1, new byte[] { 0, 1, 2, 3, 4 }, interlace: 1);

        Assert.Throws<FormatException>(() => Png.Decode(png));
    }

    private static byte[] MakePng(int width, int height, byte[] filteredRows, byte interlace = 0)
    {
        using var file = new MemoryStream();

        file.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        var header = new byte[13];

        BigEndian(header, 0, width);
        BigEndian(header, 4, height);
        header[8] = 8;
        header[9] = 6;
        header[12] = interlace;
        Chunk(file, "IHDR", header);

        using (var data = new MemoryStream())
        {
            using (var zlib = new ZLibStream(data, CompressionLevel.Optimal, leaveOpen: true)) zlib.Write(filteredRows);

            Chunk(file, "IDAT", data.ToArray());
        }

        Chunk(file, "IEND", Array.Empty<byte>());

        return file.ToArray();
    }

    // The decoder does not check chunk CRCs, so these are left zero.
    private static void Chunk(Stream file, string type, byte[] data)
    {
        var length = new byte[4];

        BigEndian(length, 0, data.Length);
        file.Write(length);
        file.Write(System.Text.Encoding.ASCII.GetBytes(type));
        file.Write(data);
        file.Write(new byte[4]);
    }

    private static void BigEndian(byte[] into, int at, int value)
    {
        into[at] = (byte)(value >> 24);
        into[at + 1] = (byte)(value >> 16);
        into[at + 2] = (byte)(value >> 8);
        into[at + 3] = (byte)value;
    }
}
