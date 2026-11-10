using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using ExiledToolkit.Game;
using HydroponicsExpanded.Core;
using Il2CppAgentSaves = Il2CppSystem.Collections.Generic.List<GameCore.HotUpdate.AgentSave>;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// Saves each station as the stock grow box it copies and turns it back on load (see <see cref="SaveTwin"/>). The
/// furniture and packed-furniture saves are hooked after they write their ids (a furniture's planter writes its plant row
/// id inside the furniture's own save, so that is there too); the load is hooked where the game
/// gathers a chapter's saves before creating them; moving furniture between chapters clones a save without its name,
/// so the clone gets the marker back. Every target was checked in GameAssembly.dll: no relative jump or call in its
/// first 14 bytes.
/// </summary>
internal static class SavePatches
{
    private const string IfMissed = "stations may not save as grow boxes";

    private static PatchKit? _hooks;

    public static void Apply(PatchKit hooks)
    {
        _hooks = hooks;

        hooks.Patch(typeof(SavePatches), typeof(Furniture), "SaveGame", postfix: nameof(SaveAsStock), consequence: IfMissed);
        hooks.Patch(typeof(SavePatches), typeof(BagFurniture), "SaveGame", postfix: nameof(SaveAsStock), consequence: IfMissed);
        hooks.Patch(typeof(SavePatches), typeof(AgentManager), "DeduplicateAgentSavesForLoad", prefix: nameof(LoadAsStation), consequence: IfMissed);
        hooks.Patch(typeof(SavePatches), typeof(GameSaveManager), "CloneAgentSave", postfix: nameof(KeepMarkerOnClone), consequence: IfMissed);
    }

    private static void SaveAsStock(AgentSave __0)
    {
        try
        {
            if (__0 == null) return;

            var fields = Read(__0);

            if (SaveTwin.ToStock(ref fields, StationRows.All)) Write(__0, fields);
        }
        catch (Exception ex)
        {
            _hooks?.Fail("saving a station as its grow box", ex);
        }
    }

    private static void LoadAsStation(Il2CppAgentSaves __0)
    {
        try
        {
            if (__0 == null) return;

            for (var i = 0; i < __0.Count; i++)
            {
                var save = __0[i];

                if (save == null) continue;

                var fields = Read(save);

                if (SaveTwin.FromStock(ref fields, StationRows.All)) Write(save, fields);
            }
        }
        catch (Exception ex)
        {
            _hooks?.Fail("loading a grow box back as a station", ex);
        }
    }

    private static void KeepMarkerOnClone(AgentSave __0, AgentSave __result)
    {
        try
        {
            if (__0?.Name != null && __result != null && __0.Name.StartsWith(SaveTwin.MarkerStart, StringComparison.Ordinal)) __result.Name = __0.Name;
        }
        catch (Exception ex)
        {
            _hooks?.Fail("keeping a station through a chapter move", ex);
        }
    }

    private static SaveTwin.Fields Read(AgentSave save) => new()
    {
        AgentConfigId = save.AgentConfigId,
        IsBagFurniture = save.IsBagFurniture,
        BagFurnitureConfigId = save.BagFurnitureConfigId,
        PlantFurnitureConfigId = save.PlantFurnitureConfigId,
        Name = save.Name,
    };

    private static void Write(AgentSave save, SaveTwin.Fields fields)
    {
        save.AgentConfigId = fields.AgentConfigId;
        save.BagFurnitureConfigId = fields.BagFurnitureConfigId;
        save.PlantFurnitureConfigId = fields.PlantFurnitureConfigId;
        save.Name = fields.Name;
    }
}
