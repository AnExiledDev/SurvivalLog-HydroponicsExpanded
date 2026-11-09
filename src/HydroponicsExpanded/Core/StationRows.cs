using System;
using System.Collections.Generic;
using System.Linq;

namespace HydroponicsExpanded.Core;

public enum Table
{
    Item,
    Furniture,
    Plant,
    Recipe,
    Action,
    Effect,
}

/// <summary>One field to set on a row, by the name the table gives it: an int, a float, a string, a bool or an int[] list.</summary>
public sealed record RowField(string Name, object Value);

/// <summary>A row to add: a copy of the stock row <paramref name="CopyFrom"/> under <paramref name="Id"/>, with these fields changed.</summary>
public sealed record RowPlan(Table Table, int Id, int CopyFrom, IReadOnlyList<RowField> Fields);

public sealed record Text(string Key, string En, string Zh);

public sealed record StationPlan(IReadOnlyList<RowPlan> Rows, IReadOnlyList<Text> Texts);

/// <summary>What the game's tables hold at one id, as far as writing our rows is concerned.</summary>
public enum Slot
{
    Empty,
    Ours,
    Stock,
}

/// <summary>
/// The two stations and the table rows that make each one: the recipe, the package it crafts, the unpack action and its
/// effect, the furniture, and its plant row. The package, action, effect and furniture are copies of the grow box of the
/// same size (so it fits the same spots and installs the same way); the recipe and the plant row are copies of the Large
/// Station's, the plant row with the grow box's plant count.
/// </summary>
public static class StationRows
{
    public const string WebIconFolder = "Res/Mods/HydroponicsExpanded";

    /// <summary>The game's Hydroponic Station (Large): its furniture, its package, its recipe and its plant row.</summary>
    public const int LargeFurniture = 60009;
    public const int LargeItem = 14019;
    public const int LargeRecipe = 319;
    public const int LargePlant = 20;

    /// <summary>The Large Station's first coloured recipe (Sage Green); the other seven follow it, a dye slot each.</summary>
    public const int LargeDyedRecipe = 1377;

    /// <summary>
    /// The Planting level that unlocks the Large Station's recipe by default: 0, none, so it stays the game's unlock,
    /// the Hydroponic Cultivation talent, and installing the mod changes no balance.
    /// </summary>
    public const int LargeUnlockLevel = 0;

    /// <summary>The Large Station's own prefab, and the one the mod builds for its new look.</summary>
    public const string LargeStockPrefab = "P_WaterTank_02";
    public const string LargeResName = "Mod_Hydroponic_L";
    public const string LargeModelFile = "HydroponicStationLarge.glb";
    public const string LargeIconFile = "UI_Item_Icon_Hydroponic_L.png";
    public static string LargeWebIcon => "../../" + WebIconFolder + "/" + LargeIconFile;

    public static readonly StationKind Small = new()
    {
        Name = "Hydroponic Station (Small)",
        Size = "Small",
        // Far above the game's ids (1.1.20151: item 70152, furniture 90291, recipe 1703, plant row 21), and clear of
        // Wind Power's 910000 range.
        Ids = new RowIds(Item: 920001, Furniture: 920001, Plant: 921, Recipe: 9201, Action: 100920001, Effect: 100920001),
        Stock = new RowIds(Item: 14067, Furniture: 60006, Plant: 17, Recipe: LargeRecipe, Action: 100014067, Effect: 100014067),
        ResName = "Mod_Hydroponic_S",
        StockPrefab = "P_FlowerPot_011_S1_M01deng",
        StockDyedItem = 14454,
        ModelFile = "HydroponicStationSmall.glb",
        IconFile = "UI_Item_Icon_Hydroponic_S.png",
        UnlockLevel = 3,
        // 3 Planter Market, 4 Small Planters, 7 Electric Planters: the shops that sell the small grow box.
        Shops = new[] { 3, 4, 7 },
        Capacity = 1,
        SaveMarker = "HydroponicsExpanded.Station.Small:",
        // The Large Station draws 6 for four plants; one plant draws a quarter of that, rounded up.
        Defaults = new StationSettings(PowerCost: 2, GrowthBoost: 0.5f, Materials: "20102:1, 20104:1, 20210:1, 20361:1", ShopPrice: 60, ShopStock: 2),
        Texts = new[]
        {
            new Text("HydroponicsExpanded_Small_Package", "Hydroponic Station (Small) Package", "全光谱水培站（小）包裹"),
            new Text("HydroponicsExpanded_Small_Name", "Hydroponic Station (Small)", "全光谱水培站（小）"),
            new Text("HydroponicsExpanded_Small_Des",
                "A small hydroponic growing station with full-spectrum lighting, for one small plant. Like the large station, crops require no manual care throughout their growth cycle.",
                "使用全光谱灯的小型水培种植站，可种一株小型植物。和大型水培站一样，作物生长全程无需人工照料。"),
            new Text("HydroponicsExpanded_Small_Unpack", "Unpack Hydroponic Station (Small) Package", "拆封 全光谱水培站（小）包裹"),
            new Text("HydroponicsExpanded_Small_Unpacking", "Unpacking Hydroponic Station (Small) Package...", "拆封 全光谱水培站（小）包裹中…"),
        },
    };

    public static readonly StationKind Medium = new()
    {
        Name = "Hydroponic Station (Medium)",
        Size = "Medium",
        Ids = new RowIds(Item: 920002, Furniture: 920002, Plant: 922, Recipe: 9202, Action: 100920002, Effect: 100920002),
        Stock = new RowIds(Item: 14068, Furniture: 60007, Plant: 18, Recipe: LargeRecipe, Action: 100014068, Effect: 100014068),
        ResName = "Mod_Hydroponic_M",
        StockPrefab = "P_FlowerPot_011_S1_M02_deng",
        StockDyedItem = 14462,
        ModelFile = "HydroponicStationMedium.glb",
        IconFile = "UI_Item_Icon_Hydroponic_M.png",
        UnlockLevel = 4,
        // 3 Planter Market, 5 Medium Planters, 7 Electric Planters: the shops that sell the medium grow box.
        Shops = new[] { 3, 5, 7 },
        Capacity = 2,
        SaveMarker = "HydroponicsExpanded.Station.Medium:",
        // Half the Large Station's 6, for half its plants.
        Defaults = new StationSettings(PowerCost: 3, GrowthBoost: 0.5f, Materials: "20102:1, 20104:1, 20210:2, 20361:1", ShopPrice: 100, ShopStock: 2),
        Texts = new[]
        {
            new Text("HydroponicsExpanded_Medium_Package", "Hydroponic Station (Medium) Package", "全光谱水培站（中）包裹"),
            new Text("HydroponicsExpanded_Medium_Name", "Hydroponic Station (Medium)", "全光谱水培站（中）"),
            new Text("HydroponicsExpanded_Medium_Des",
                "A medium hydroponic growing station with full-spectrum lighting, for two small plants or one medium plant. Like the large station, crops require no manual care throughout their growth cycle. If growing small plants, the entire station must be filled at once.",
                "使用全光谱灯的中型水培种植站，可种两株小型植物或一株中型植物。和大型水培站一样，作物生长全程无需人工照料；若选小型植物，需一次种满整个水培站。"),
            new Text("HydroponicsExpanded_Medium_Unpack", "Unpack Hydroponic Station (Medium) Package", "拆封 全光谱水培站（中）包裹"),
            new Text("HydroponicsExpanded_Medium_Unpacking", "Unpacking Hydroponic Station (Medium) Package...", "拆封 全光谱水培站（中）包裹中…"),
        },
    };

    public static readonly IReadOnlyList<StationKind> All = new[] { Small, Medium };

    /// <summary>The key the game files a recipe under: its materials, sorted.</summary>
    public static string MaterialKey(IEnumerable<int> materials) => string.Join(",", materials.OrderBy(id => id));

    /// <param name="materials">The recipe's materials, each id once per piece.</param>
    /// <param name="settings">The power, growth boost and price to write.</param>
    public static StationPlan Plan(StationKind kind, IReadOnlyList<int> materials, StationSettings settings)
    {
        var ids = kind.Ids;
        var package = kind.Key("Package");
        var name = kind.Key("Name");
        var des = kind.Key("Des");
        var unpack = kind.Key("Unpack");
        var unpacking = kind.Key("Unpacking");

        string Zh(string key) => kind.Texts.First(text => text.Key == key).Zh;

        var rows = new[]
        {
            // The Large Station's recipe, so it sits next to it at the workbench and needs the same level.
            new RowPlan(Table.Recipe, ids.Recipe, kind.Stock.Recipe, new[]
            {
                new RowField("ID", ids.Recipe),
                new RowField("ShopName", package),
                new RowField("ShopName_Local", Zh(package)),
                new RowField("ProductID", new[] { ids.Item }),
                new RowField("MaterialList", materials.ToArray()),
                new RowField("perfect_item_id", materials.ToArray()),
            }),
            new RowPlan(Table.Item, ids.Item, kind.Stock.Item, new[]
            {
                new RowField("ID", ids.Item),
                new RowField("ItemName", package),
                new RowField("ItemName_Local", Zh(package)),
                new RowField("TargetFurnitureID", ids.Furniture),
                new RowField("UseAction", ids.Action),
                new RowField("WebIcon", kind.WebIcon),
            }),
            new RowPlan(Table.Action, ids.Action, kind.Stock.Action, new[]
            {
                new RowField("ID", ids.Action),
                new RowField("Name", unpack),
                new RowField("Name_Local", Zh(unpack)),
                new RowField("NameDoing", unpacking),
                new RowField("NameDoing_Local", Zh(unpacking)),
                new RowField("EffectConfigID", ids.Effect),
                new RowField("WebIcon", kind.WebIcon),
            }),
            new RowPlan(Table.Effect, ids.Effect, kind.Stock.Effect, new[]
            {
                new RowField("ID", ids.Effect),
            }),
            new RowPlan(Table.Furniture, ids.Furniture, kind.Stock.Furniture, new[]
            {
                new RowField("ID", ids.Furniture),
                new RowField("Name", name),
                new RowField("Name_Local", Zh(name)),
                new RowField("Des", des),
                new RowField("Des_Local", Zh(des)),
                new RowField("PlantFurnitureID", ids.Plant),
                new RowField("PowerCost", settings.PowerCost),
                new RowField("ResName", kind.ResName),
                new RowField("ICON", kind.WebIcon),
                new RowField("FurniturePrice", settings.ShopPrice),
                // What taking it down gives back. The copied row would give the grow box's parts.
                new RowField("RemoveGet", MaterialList.GivenBack(materials)),
            }),
            // The Large Station's plant row: the light and heat it gives when powered, and the pests, weeds and dryness it
            // keeps off, with the grow box's plant count.
            new RowPlan(Table.Plant, ids.Plant, LargePlant, new[]
            {
                new RowField("ID", ids.Plant),
                new RowField("Capacity", kind.Capacity),
                new RowField("GrowthFaster", settings.GrowthBoost),
            }),
        };

        return new StationPlan(rows, kind.Texts);
    }

    /// <summary>
    /// Why the plan cannot be written, or nothing if it can. Writing is all or nothing: a station missing its recipe or
    /// its plant row is worse than no station, and a stock row at one of our ids must never be replaced.
    /// </summary>
    /// <param name="recipeUsing">The id of another recipe made of these materials in any order, or null.</param>
    public static List<string> Problems(StationPlan plan, Func<Table, int, Slot> slotAt, Func<IReadOnlyList<int>, int?> recipeUsing)
    {
        var problems = new List<string>();

        foreach (var row in plan.Rows)
        {
            if (slotAt(row.Table, row.CopyFrom) == Slot.Empty) problems.Add($"{row.Table} {row.CopyFrom}, the row to copy, is missing");

            if (slotAt(row.Table, row.Id) == Slot.Stock) problems.Add($"{row.Table} {row.Id} is already used by the game");

            if (row.Fields.FirstOrDefault(field => field.Name == "MaterialList")?.Value is int[] materials && recipeUsing(materials) is int other)
                problems.Add($"Recipe {other} already uses the same materials, so only one of the two could be crafted");
        }

        return problems;
    }

    /// <summary>
    /// Where a plant spot of the grow box goes on the station's model: each bay centre is <paramref name="bayOffset"/>
    /// from the middle along each axis the grow box's spot is off-centre on, and every pot top is at <paramref name="potTop"/>.
    /// The grow boxes put their spots half a metre out; anything within 10 cm of the middle stays in the middle.
    /// </summary>
    public static (float X, float Y, float Z) PlantSpot(float x, float z, float bayOffset, float potTop) => (Snap(x, bayOffset), potTop, Snap(z, bayOffset));

    private static float Snap(float value, float offset) => Math.Abs(value) < 0.1f ? 0f : Math.Sign(value) * offset;

    /// <summary>
    /// How many bays one plant fills, so the station shows seats to match: 1 (a pot per bay), 2 (one pot over two bays)
    /// or 4 (one pot over all four). The game shares a station's <paramref name="spots"/> out evenly between its
    /// <paramref name="seeds"/>, so a watermelon (one seed) in a four-spot station takes all four. An empty station shows
    /// a pot per bay.
    /// </summary>
    public static int PlantSize(int spots, int seeds, bool planted) => planted && spots > 0 && seeds > 0 ? spots / Math.Min(seeds, spots) : 1;
}
