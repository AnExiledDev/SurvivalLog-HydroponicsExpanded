using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using ExiledToolkit.Game;
using HydroponicsExpanded.Core;

namespace HydroponicsExpanded.Adapters;

/// <summary>
/// The Mod Manager's questions about the stations' Materials and Returns lists. The Mod Manager finds this class by its
/// name, so neither mod needs the other. It asks again after every change, so each answer is worked out afresh from
/// the settings as they stand and the game's item and recipe tables, the same way the stations' rows are written.
/// Before the game's tables are loaded, every item counts as known and no stock recipe as taken.
/// </summary>
public static class ModManagerAnswers
{
    /// <summary>Each station's settings as the config holds them now, in the order of <see cref="StationRows.All"/>.</summary>
    internal static Func<IReadOnlyList<StationSettings>>? Settings { get; set; }

    public static string Problem(string section, string key) => StationChoices.Problem(Choices(), section, key);

    public static string Blank(string section, string key) => StationChoices.Blank(Choices(), section, key);

    public static string BlankNote(string section, string key) => StationChoices.BlankNote(Choices(), section, key);

    // One reading of the settings asks three questions of each list, and each answer looks through every recipe, so
    // the answers are kept until a setting or a table changes.
    private static StationSettings[] _answeredFor = Array.Empty<StationSettings>();
    private static (IntPtr Items, IntPtr Recipes) _answeredTables;
    private static IReadOnlyList<StationChoice> _answers = Array.Empty<StationChoice>();

    private static IReadOnlyList<StationChoice> Choices()
    {
        var settings = (Settings?.Invoke() ?? StationRows.All.Select(kind => kind.Defaults).ToList()).ToArray();
        var config = GameSingleton.Existing<ConfigManager>();
        var items = config?._Config_Item_Dict;
        var recipes = config?._Config_ProductionList_Dict;
        var tables = (items?.Pointer ?? IntPtr.Zero, recipes?.Pointer ?? IntPtr.Zero);

        if (_answers.Count > 0 && tables == _answeredTables && settings.SequenceEqual(_answeredFor)) return _answers;

        // The coloured recipes are each plain one plus a dye, which a list can never hold, so only the plain ones matter.
        var ourRecipes = StationRows.All.Select(kind => kind.Ids.Recipe).ToHashSet();
        Func<int, bool> isItem = items != null && items.Count > 0 ? items.ContainsKey : _ => true;
        Func<IReadOnlyList<int>, int?> recipeUsing = recipes != null && recipes.Count > 0 ? list => StationTables.RecipeUsing(recipes, list, recipe => ourRecipes.Contains(recipe.ID)) : _ => null;

        _answers = StationChoices.Of(settings, isItem, recipeUsing, id => RecipeName(config, id));
        _answeredFor = settings;
        _answeredTables = tables;

        return _answers;
    }

    /// <summary>The name of the first thing a recipe makes, in both languages, or null when it cannot be read.</summary>
    private static (string English, string Chinese)? RecipeName(ConfigManager? config, int recipeId)
    {
        try
        {
            var recipes = config?._Config_ProductionList_Dict;
            var items = config?._Config_Item_Dict;

            if (config == null || recipes == null || items == null || !recipes.ContainsKey(recipeId)) return null;

            var made = recipes[recipeId]?.ProductID;

            if (made == null || made.Count == 0 || !items.ContainsKey(made[0])) return null;

            var key = items[made[0]]?.ItemName;
            var english = GameNames.Translate(config, key, LanguageType.English);
            var chinese = GameNames.Translate(config, key, LanguageType.Chinese) ?? items[made[0]]?.ItemName_Local;

            return english != null ? (english, string.IsNullOrWhiteSpace(chinese) ? english : chinese) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
