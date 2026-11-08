using System.Collections.Generic;

namespace HydroponicsExpanded.Core;

/// <summary>
/// What Uninstall prep turns the stations' own ids into, so a save made with it on loads cleanly without the mod. A
/// game without the mod drops an item it does not know when it loads, so a station package becomes the grow box
/// package of its size (the package rows are copies of the grow boxes', so its size and weight stay the same). A shop
/// stops selling the stations, and the pre-disaster purchase record counts each one bought as its grow box.
/// Placed and packed stations need nothing here: every save already writes them as grow boxes.
/// </summary>
public static class Uninstall
{
    /// <summary>
    /// The grow box package a station package becomes, or null for any other item. A coloured station package becomes
    /// the grow box package of the same colour.
    /// </summary>
    public static int? StockItem(int itemId, IReadOnlyList<StationKind> kinds)
    {
        for (var i = 0; i < kinds.Count; i++)
            if (kinds[i].Ids.Item == itemId) return kinds[i].Stock.Item;

        return Dyes.OfPackage(itemId, kinds) is var (kind, dye) ? Dyes.StockIds(kind, dye).Item : null;
    }

    /// <summary>The grow box a station's furniture id becomes, or null for any other furniture.</summary>
    public static int? StockFurniture(int furnitureId, IReadOnlyList<StationKind> kinds)
    {
        for (var i = 0; i < kinds.Count; i++)
            if (kinds[i].Ids.Furniture == furnitureId) return kinds[i].Stock.Furniture;

        return null;
    }

    /// <summary>
    /// The moves that turn the stations in a purchase record into their grow boxes: take <c>Count</c> off
    /// <c>From</c> and add it to <c>To</c>. Records of anything else stay as they are.
    /// </summary>
    public static List<(int From, int To, int Count)> LedgerMoves(IEnumerable<KeyValuePair<int, int>> records, IReadOnlyList<StationKind> kinds)
    {
        var moves = new List<(int From, int To, int Count)>();

        foreach (var record in records)
            if (record.Value > 0 && StockFurniture(record.Key, kinds) is int stock) moves.Add((record.Key, stock, record.Value));

        return moves;
    }
}
