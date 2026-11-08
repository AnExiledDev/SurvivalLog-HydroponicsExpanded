using System;
using System.Collections.Generic;

namespace HydroponicsExpanded.Core;

/// <summary>The ids of one station's six rows: the recipe crafts the package item, which unpacks into the furniture, whose plant row says how it grows.</summary>
public sealed record RowIds(int Item, int Furniture, int Plant, int Recipe, int Action, int Effect);

/// <summary>What the player sets for one station. Materials is written as id:count pairs, "20102:1, 20104:1".</summary>
public sealed record StationSettings(int PowerCost, float GrowthBoost, string Materials, int ShopPrice, int ShopStock);

/// <summary>
/// One hydroponic station the mod adds: the grow box it copies (its rows, its prefab and the spot it fits), its own ids,
/// model and text, and the settings it starts with. It grows like the game's Hydroponic Station (Large).
/// </summary>
public sealed record StationKind
{
    /// <summary>The English name, for the log.</summary>
    public string Name { get; init; } = "";

    /// <summary>"Small" or "Medium": the middle of each text key, HydroponicsExpanded_Small_Name.</summary>
    public string Size { get; init; } = "";

    public RowIds Ids { get; init; } = new(0, 0, 0, 0, 0, 0);

    /// <summary>
    /// The grow box of the same size: the rows ours are copied from (the recipe is the Large Station's), and the ids a
    /// save writes in place of ours. Plant is the grow box's plant row, which a save names along with the furniture.
    /// </summary>
    public RowIds Stock { get; init; } = new(0, 0, 0, 0, 0, 0);

    /// <summary>The prefab name the furniture row gives. The game has no such prefab: the mod builds it.</summary>
    public string ResName { get; init; } = "";

    /// <summary>The grow box's prefab the model is built on, so it takes the same spot and its plant spots.</summary>
    public string StockPrefab { get; init; } = "";

    public string ModelFile { get; init; } = "";

    public string IconFile { get; init; } = "";

    /// <summary>The Planting level that unlocks its recipe by default.</summary>
    public int UnlockLevel { get; init; }

    /// <summary>The pre-disaster shops (Config_ShopFurniture) that sell it, the ones that sell its grow box.</summary>
    public IReadOnlyList<int> Shops { get; init; } = Array.Empty<int>();

    /// <summary>How many small plants it holds, as the grow box it copies.</summary>
    public int Capacity { get; init; }

    /// <summary>What the save's name field starts with while this station is saved as its grow box.</summary>
    public string SaveMarker { get; init; } = "";

    public StationSettings Defaults { get; init; } = new(0, 0f, "", 0, 0);

    /// <summary>The names and descriptions its rows point at.</summary>
    public IReadOnlyList<Text> Texts { get; init; } = Array.Empty<Text>();

    /// <summary>Where the web UI finds the icon, written the way the game writes its own (from a page two folders below WebUI).</summary>
    public string WebIcon => "../../" + StationRows.WebIconFolder + "/" + IconFile;

    public string Key(string part) => $"HydroponicsExpanded_{Size}_{part}";
}
