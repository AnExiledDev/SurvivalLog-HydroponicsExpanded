using System.Threading;
using GameCore.HotUpdate;
using ExiledToolkit.Game;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Wakes the table check when what it reads may have changed. ConfigManager fills its tables in Initialize, the two
/// languages in EnsureDualLanguageLoaded and the list of dyeable furniture in LoadFurnitureDyeable; each runs in steps,
/// so each step wakes the check. A setting change wakes it too. If a hook cannot go in, the check wakes every second
/// instead. All three targets were checked in GameAssembly.dll: no relative jump or call in their first 14 bytes.
/// </summary>
internal static class TableWakes
{
    private static int _count;

    /// <summary>How many wakes there have been. Goes up from any thread.</summary>
    public static int Count => Volatile.Read(ref _count);

    public static bool IsPolling { get; private set; }

    public static void Wake() => Interlocked.Increment(ref _count);

    public static void Apply(PatchKit hooks)
    {
        const string consequence = "the stations' rows are checked every second instead";

        var isPlaced = hooks.Patch(typeof(TableWakes), typeof(ConfigManager._Initialize_d__256), "MoveNext", postfix: nameof(AfterStep), consequence: consequence);

        isPlaced &= hooks.Patch(typeof(TableWakes), typeof(ConfigManager._EnsureDualLanguageLoaded_d__361), "MoveNext", postfix: nameof(AfterStep), consequence: consequence);
        isPlaced &= hooks.Patch(typeof(TableWakes), typeof(ConfigManager._LoadFurnitureDyeable_d__358), "MoveNext", postfix: nameof(AfterStep), consequence: consequence);

        if (!isPlaced) IsPolling = true;
    }

    private static void AfterStep() => Wake();
}
