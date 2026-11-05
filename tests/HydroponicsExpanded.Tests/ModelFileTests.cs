using System.IO.Compression;
using System.Numerics;
using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

public class ModelFileTests
{
    private static byte[] Asset(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "assets", name));

    private static readonly IReadOnlyDictionary<string, Vector3> Movers = new Dictionary<string, Vector3> { ["Lights"] = Vector3.UnitY };

    private static readonly Model Small = Glb.Read(Asset(StationRows.Small.ModelFile), Movers);

    private static readonly Model Medium = Glb.Read(Asset(StationRows.Medium.ModelFile), Movers);

    private static readonly Model Large = Glb.Read(Asset(StationRows.LargeModelFile), Movers);

    private static Model Load(string size) => size switch { "Medium" => Medium, "Small" => Small, _ => Large };

    private static string IconFile(string size) => size == "Large" ? StationRows.LargeIconFile : StationRows.All.Single(kind => kind.Size == size).IconFile;

    // The grow lights are a part of their own, so the mod can hang them under the game's Plant_Light switch and the
    // game turns them off with the power.
    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    [InlineData("Large")]
    public void TheLightsArePartOfTheirOwnAndHoldOnlyTheGlowingParts(string size)
    {
        var model = Load(size);
        var lights = model.Parts.Single(part => part.Name == "Lights");

        Assert.Equal(new[] { Glb.BodyName, "Lights" }, model.Parts.Select(part => part.Name).OrderBy(name => name, StringComparer.Ordinal));
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
        Assert.InRange(max.Y, 1.6f, 2.0f);
        Assert.InRange(max.X - min.X, spotX * 0.9f, spotX + 0.2f);
        Assert.InRange(max.Z - min.Z, spotZ * 0.9f, spotZ + 0.2f);
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
