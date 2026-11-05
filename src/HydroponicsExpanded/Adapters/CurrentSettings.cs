using System;
using System.Collections.Generic;
using System.Linq;
using HydroponicsExpanded.Core;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// The settings each station runs on now, in the order of <see cref="StationRows.All"/>. A change replaces the whole
/// list, so a reader always sees one complete set and the table check sees a change by the list itself.
/// </summary>
internal static class CurrentSettings
{
    private static StationSettings[] _current = StationRows.All.Select(kind => kind.Defaults).ToArray();

    public static IReadOnlyList<StationSettings> Current => _current;

    /// <summary>Whether the game's Hydroponic Station (Large) is built with the mod's model.</summary>
    public static bool NewLook { get; set; } = true;

    public static StationSettings Of(StationKind kind) => _current[IndexOf(kind)];

    public static void Set(StationKind kind, StationSettings settings)
    {
        var next = (StationSettings[])_current.Clone();

        next[IndexOf(kind)] = settings;
        _current = next;
    }

    private static int IndexOf(StationKind kind)
    {
        for (var i = 0; i < StationRows.All.Count; i++)
            if (ReferenceEquals(StationRows.All[i], kind)) return i;

        throw new ArgumentException(kind.Name + " is not one of the mod's stations", nameof(kind));
    }
}
