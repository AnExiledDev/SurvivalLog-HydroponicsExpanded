using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using ExiledToolkit.Game;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Says when the stations' recipes need checking, so <see cref="StationTables"/> checks only then: after the game
/// raises Planting (ProficiencyManager.ApplyLevelEffect, which both AddLevel and CheckLevelUp call), after it loads the
/// run's recipe list (ToolTableManager.LoadGame, which Initialize calls for a new run too), and after a setting that
/// changes which recipes are due. Both targets were checked in GameAssembly.dll: no relative jump or call in their
/// first 14 bytes.
/// </summary>
internal static class RecipeTriggers
{
    private const string IfMissed = "the stations' recipes may not unlock when Planting goes up";

    private static PatchKit? _hooks;

    /// <summary>True until a check has run on a world that was ready for it.</summary>
    public static bool IsDue { get; private set; } = true;

    public static void Apply(PatchKit hooks)
    {
        _hooks = hooks;

        hooks.Patch(typeof(RecipeTriggers), typeof(ProficiencyManager), "ApplyLevelEffect", postfix: nameof(AfterLevel), consequence: IfMissed);
        hooks.Patch(typeof(RecipeTriggers), typeof(ToolTableManager), "LoadGame", postfix: nameof(AfterLoad), consequence: IfMissed);
    }

    public static void MarkDue() => IsDue = true;

    public static void MarkDone() => IsDue = false;

    private static void AfterLevel(ProficiencyType __0)
    {
        try
        {
            if (__0 == ProficiencyType.Plant) IsDue = true;
        }
        catch (Exception ex)
        {
            _hooks?.Fail("noticing a new Planting level", ex);
        }
    }

    private static void AfterLoad() => IsDue = true;
}
