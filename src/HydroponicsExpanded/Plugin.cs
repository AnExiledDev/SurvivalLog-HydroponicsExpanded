using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using HarmonyLib;
using HydroponicsExpanded.Adapters;
using HydroponicsExpanded.Core;

namespace HydroponicsExpanded;

[BepInPlugin(Guid, "Hydroponics Expanded", Version)]
[BepInDependency(ToolkitGuid, ToolkitVersion)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.hydroponicsexpanded";
    private const string Version = "0.1.11";

    /// <summary>Exiled's Toolkit: applies each setting as soon as it changes, and reads the game's singletons safely.</summary>
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";
    private const string ToolkitVersion = ">=0.19.0";

    public override void Load()
    {
        _uninstallPrep = Config.Bind("Uninstall", "UninstallPrep", false,
            "Turn this on before you remove Hydroponics Expanded. Once a save is loaded, every Small or Medium Hydroponic Station Package becomes the Plant Grow Box Package of its size, in your bags, in storage and on the floor, in every place the save holds, and the shops stop selling the stations. Placed and packed stations are always saved as grow boxes, so they need nothing. The Small and Medium recipes come off the unlocked list. Then save the game, quit, and remove HydroponicsExpanded.dll. LogOutput.log lists what was changed. Turning this off again does not turn the packages back.\n" +
            "移除水培扩展模组之前请先打开此项。读取存档后，背包、储物和地上的每个小型或中型水培站包裹都会变成同尺寸的植物灯培育箱包裹，存档里的每个地点都一样，商店也不再出售水培站。已安装和已打包的水培站本来就按植物灯培育箱保存，不受影响。小型和中型水培站的配方也会从已解锁列表中移除。然后存档、退出游戏，再删除 HydroponicsExpanded.dll。改了哪些东西写在 LogOutput.log 里。再关掉此项，包裹也不会变回来。");

        var newLook = Config.Bind("LargeStation", "NewLook", true,
            "Gives the game's Hydroponic Station (Large) the same look as the Small and Medium ones. Turn it off for the game's look. A change shows on stations placed or loaded afterwards: load the save again to see it on the ones already standing.\n" +
            "让游戏自带的全光谱水培站（大）换上和小型、中型水培站一样的模型。关掉则用游戏原本的样子。改动对之后放置或读取的水培站生效：重新读取存档即可看到已摆放的水培站的变化。");

        var largeUnlock = Config.Bind("LargeStation", "UnlockLevel", StationRows.LargeUnlockLevel, new ConfigDescription(
            "The Planting level that unlocks the Hydroponic Station (Large)'s recipe, which also unlocks the Small and Medium ones. 0, the default, leaves it to the game's Hydroponic Cultivation talent, which unlocks it at any level. A run already at this level gets the recipe when its save loads.\n" +
            "种植等级达到多少时解锁全光谱水培站（大）的配方，同时也会解锁小型和中型水培站的配方。0（默认）表示只靠游戏的「无土栽培」天赋解锁，任何等级都可以。已经达到这个等级的存档在读取时就会获得配方。",
            new AcceptableValueRange<int>(0, Unlocks.TopPlantingLevel)));
        LiveSettings.Watch(largeUnlock, level => CurrentSettings.LargeUnlockLevel = level);

        _returnPercent = Config.Bind("Dismantle", "ReturnPercent", MaterialList.DefaultReturnPercent, new ConfigDescription(
            "How much of a station's materials taking it down gives back, as a percent of its recipe. 50 is about half, like the game's furniture. 0 gives nothing back and 100 gives back everything. Each material's share is rounded down, then topped up one at a time to the right total. A station whose Returns list is filled in gives back that list instead.\n" +
            "拆除水培站时返还多少材料，按配方的百分比计。50 约为一半，和游戏自带的家具一样。0 什么都不返还，100 全部返还。每种材料按比例向下取整，再逐个补足到应有的总数。填了 Returns 清单的水培站按清单返还。",
            new AcceptableValueRange<int>(0, 100)));
        BindSettings(StationRows.Small, "Plant Grow Box (Small)", "植物灯培育箱（小）", 25, 1);
        BindSettings(StationRows.Medium, "Plant Grow Box (Medium)", "植物灯培育箱（中）", 50, 3);
        ModManagerAnswers.Settings = () => CurrentSettings.Current;
        LiveSettings.Watch(newLook, isOn => CurrentSettings.NewLook = isOn);
        LiveSettings.Watch(_uninstallPrep, isOn =>
        {
            UninstallPrep.Set(isOn);
            if (isOn) Log.LogInfo("Uninstall prep is on: load a save to turn the station packages into grow box packages");
        });

        var tables = new StationTables(message => Log.LogInfo(message));
        var harmony = new Harmony(Guid);
        var iconPlaced = false;

        SavePatches.Apply(harmony, message => Log.LogWarning(message));
        ShopStock.Apply(harmony, message => Log.LogWarning(message));
        StationModel.Apply(harmony, message => Log.LogInfo(message));

        StationTicker.Log = message => Log.LogError(message);
        UninstallPrep.Log = message => Log.LogInfo(message);
        StationTicker.Check = now =>
        {
            if (!iconPlaced)
            {
                iconPlaced = true;
                StationIcon.Place();
            }

            StationIcon.Report(message => Log.LogInfo(message));

            tables.Tick(now);
            UninstallPrep.Tick(now);
        };
        AddComponent<StationTicker>();
        Log.LogInfo($"Hydroponics Expanded {Version} loaded");
    }

    /// <summary>One section of settings per station. Every one applies live: the station's rows are rewritten when one changes.</summary>
    private void BindSettings(StationKind kind, string growBox, string growBoxChinese, int growBoxPrice, int growBoxPower)
    {
        var section = kind.Section;
        var name = kind.Name;
        var chinese = kind.ChineseName;
        var defaults = kind.Defaults;
        var listTags = new[] { "ModManager.Control=ItemCounts", $"ModManager.MaxKinds={MaterialList.MostKinds}", $"ModManager.MaxCount={MaterialList.MostOfOne}" };

        var power = Config.Bind(section, "PowerCost", defaults.PowerCost, new ConfigDescription(
            $"Power the {name} draws while it runs. The Hydroponic Station (Large) draws 6 and the {growBox} {growBoxPower}.\n" +
            $"{chinese}运行时的耗电量。全光谱水培站（大）耗电 6，{growBoxChinese}耗电 {growBoxPower}。",
            new AcceptableValueRange<int>(0, 50)));
        var growth = Config.Bind(section, "GrowthBoost", defaults.GrowthBoost, new ConfigDescription(
            $"How much faster plants grow in the {name} while it has power: 0.5 is 50% faster, the same as the Hydroponic Station (Large). The {growBox} gives 0.3.\n" +
            $"{chinese}通电时植物生长加快多少：0.5 表示快一半，和全光谱水培站（大）一样。{growBoxChinese}是 0.3。",
            new AcceptableValueRange<float>(0f, 5f)));
        var materials = Config.Bind(section, StationChoices.MaterialsKey, defaults.Materials, new ConfigDescription(
            $"What crafting a {name} Package takes, written as item id:count pairs split by commas. 20102 is Container, 20104 Wire, 20210 Electronic Components, 20361 Iron Rod and 20362 Iron Parts. Up to {MaterialList.MostKinds} different items, up to {MaterialList.MostOfOne} of each. A list the game cannot use (an unknown item, a dye, or the same materials as another recipe) falls back to the default, and LogOutput.log says why.\n" +
            $"制作{chinese}包裹所需的材料，写成「物品id:数量」，用逗号隔开。20102 是容器，20104 是铁丝，20210 是电器元件，20361 是铁棍，20362 是铁制零件。最多 {MaterialList.MostKinds} 种物品，每种最多 {MaterialList.MostOfOne} 个。游戏用不了的清单（物品不存在、含有染料，或和别的配方材料相同）会改用默认值，原因写在 LogOutput.log 里。",
            null, listTags.Append($"ModManager.Makes={kind.Ids.Item}").ToArray<object>()));
        var returns = Config.Bind(section, StationChoices.ReturnsKey, defaults.Returns, new ConfigDescription(
            $"What taking a {name} down gives back, written like Materials. Blank gives back the ReturnPercent share of its materials (under Dismantle); {MaterialList.NoItems} gives nothing back; a list gives back exactly that, and may name any items. Up to {MaterialList.MostKinds} different items, up to {MaterialList.MostOfOne} of each. A list the game cannot use (an unknown item) gives back the ReturnPercent share instead, and LogOutput.log says why.\n" +
            $"拆除{chinese}时返还的物品，写法和 Materials 一样。留空时按 Dismantle 下的 ReturnPercent 返还一部分材料；写 {MaterialList.NoItems} 什么都不返还；写清单就照清单返还，可以是任何物品。最多 {MaterialList.MostKinds} 种物品，每种最多 {MaterialList.MostOfOne} 个。游戏用不了的清单（物品不存在）会改按 ReturnPercent 返还，原因写在 LogOutput.log 里。",
            null, listTags.Concat(new[] { "ModManager.AutoWhenBlank", $"ModManager.ReturnsOf={StationChoices.MaterialsKey}" }).ToArray<object>()));
        var price = Config.Bind(section, "ShopPrice", defaults.ShopPrice, new ConfigDescription(
            $"What the flowerpot shops ask for a {name} before the disaster. The {growBox} costs {growBoxPrice} there and the Hydroponic Station (Large) 150.\n" +
            $"灾难前花盆商店卖{chinese}的价格。{growBoxChinese}在那里卖 {growBoxPrice}，全光谱水培站（大）卖 150。",
            new AcceptableValueRange<int>(0, 9999)));
        var stock = Config.Bind(section, "ShopStock", defaults.ShopStock, new ConfigDescription(
            $"How many {name}s each flowerpot shop that sells the {growBox} has for sale before the disaster. 0 takes them off the shelves. A shop that already has more than this is brought down to it, and stations you already bought stay yours.\n" +
            $"灾难前每家出售{growBoxChinese}的花盆商店有几台{chinese}出售。0 表示不卖。已有更多存货时会减到这个数；已经买下的不受影响。",
            new AcceptableValueRange<int>(0, 20)));
        var unlock = Config.Bind(section, "UnlockLevel", kind.UnlockLevel, new ConfigDescription(
            $"The Planting level that unlocks the {name}'s recipe. Getting the Hydroponic Station (Large)'s recipe, by its level or by the Hydroponic Cultivation talent, unlocks it at any level. 0 leaves it to the Large Station's recipe alone. A run already at this level gets the recipe when its save loads.\n" +
            $"种植等级达到多少时解锁{chinese}的配方。获得全光谱水培站（大）的配方后（无论是靠等级还是「无土栽培」天赋），任何等级都会解锁它。0 表示只随大型水培站的配方解锁。已经达到这个等级的存档在读取时就会获得配方。",
            new AcceptableValueRange<int>(0, Unlocks.TopPlantingLevel)));

        LiveSettings.Watch(unlock, level => CurrentSettings.SetUnlockLevel(kind, level));

        // Uninstall prep holds the stock at 0, so the shops stop listing the station.
        LiveSettings.Watch(() => CurrentSettings.Set(kind, new StationSettings(power.Value, growth.Value, materials.Value, price.Value, _uninstallPrep!.Value ? 0 : stock.Value, _returnPercent!.Value, returns.Value)), power, growth, materials, returns, price, stock, _uninstallPrep!, _returnPercent!);
    }

    private ConfigEntry<bool>? _uninstallPrep;
    private ConfigEntry<int>? _returnPercent;
}
