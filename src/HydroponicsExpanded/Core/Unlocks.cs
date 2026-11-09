using System;
using System.Collections.Generic;
using System.Linq;

namespace HydroponicsExpanded.Core;

/// <summary>
/// The edits to the game's shops that sell the stations: the pre-disaster shops' stock lists, and how a live
/// shop's count follows a change in the stock setting. Each edit can run again on rows it already changed and leaves
/// them as they are.
/// </summary>
public static class Unlocks
{
    /// <summary>
    /// A shop's stock lists, the furniture ids and how many of each, with every one of <paramref name="ours"/> taken
    /// out and then each of <paramref name="wanted"/> with a count above 0 added at the end. Null when that changes
    /// nothing. A station with no stock is left out, so the shop does not list it at all.
    /// </summary>
    public static (int[] Ids, int[] Counts)? Shop(IReadOnlyList<int> ids, IReadOnlyList<int> counts, IReadOnlyCollection<int> ours, IReadOnlyList<(int Id, int Count)> wanted)
    {
        var keptIds = new List<int>();
        var keptCounts = new List<int>();

        for (var i = 0; i < ids.Count; i++)
        {
            if (ours.Contains(ids[i])) continue;

            keptIds.Add(ids[i]);
            keptCounts.Add(i < counts.Count ? counts[i] : 0);
        }

        foreach (var (id, count) in wanted.Where(pair => pair.Count > 0))
        {
            keptIds.Add(id);
            keptCounts.Add(count);
        }

        var isSame = keptIds.SequenceEqual(ids) && keptCounts.SequenceEqual(counts.Take(ids.Count)) && counts.Count == ids.Count;

        return isSame ? null : (keptIds.ToArray(), keptCounts.ToArray());
    }

    /// <summary>
    /// A live shop's count of one station after the stock setting goes from <paramref name="before"/> to
    /// <paramref name="after"/>: moved by the difference, so ones already bought stay bought, never below none and
    /// never above a full shop.
    /// </summary>
    public static int NewStock(int current, int before, int after) => Math.Clamp(current + after - before, 0, Math.Max(0, after));

    /// <summary>The highest Planting level in the game.</summary>
    public const int TopPlantingLevel = 5;

    /// <summary>
    /// The station recipes a run should be able to craft and cannot yet, from its Planting level and the recipes it
    /// can already craft. The Large Station's comes at its own level. The Small's and the Medium's come at theirs, or
    /// with the Large Station's however the run got that one: the Hydroponic Cultivation talent unlocks only the Large.
    /// A level of 0 means the level never unlocks that station.
    /// </summary>
    /// <param name="large">The Large Station's recipe and the Planting level that unlocks it.</param>
    /// <param name="ours">The mod's stations' recipes and the level that unlocks each.</param>
    public static IReadOnlyList<int> RecipesDue(int planting, (int Recipe, int Level) large, IReadOnlyList<(int Recipe, int Level)> ours, IReadOnlyCollection<int> unlocked)
    {
        var due = new List<int>();
        var hasLarge = unlocked.Contains(large.Recipe) || Reached(planting, large.Level);

        if (hasLarge && !unlocked.Contains(large.Recipe)) due.Add(large.Recipe);

        foreach (var (recipe, level) in ours)
            if (!unlocked.Contains(recipe) && !due.Contains(recipe) && (hasLarge || Reached(planting, level))) due.Add(recipe);

        return due;
    }

    private static bool Reached(int planting, int level) => level > 0 && planting >= level;
}
