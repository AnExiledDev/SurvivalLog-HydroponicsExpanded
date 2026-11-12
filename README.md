# Hydroponics Expanded

Adds a Small and a Medium Hydroponic Station to Survival Log, and gives the game's Hydroponic Station (Large) a new look to match them.

- **Hydroponic Station (Small)**: one small plant. It fits the Plant Grow Box (Small)'s spot.
- **Hydroponic Station (Medium)**: two small plants or one medium plant. It fits the Plant Grow Box (Medium)'s spot.
- **Hydroponic Station (Large)**: the one already in the game. It takes four small plants, two medium or one large, and gets the new model. One setting gives it its old look back.

The stations work like the Large one. While powered, their full-spectrum grow lights make plants grow faster (50% by default), keep them warm, and keep pests, weeds and dryness away, so crops need no care until harvest. Without power the grow lights go out and the plants get none of that. Each station has its own model and icon. You can craft one in any of the game's 8 colours, or dye it later like other furniture. The orange frame takes the colour, and the tubs, steel and water stay as they are.

Each station's pots change with what is planted: a pot in every bay for small plants, one big pot over two bays for a medium plant, and one over all four for a large plant such as corn or watermelon, so the crop grows in the middle of its pot instead of over the bay walls. The grow lights hang high enough for the tallest crop each station takes, so the Large stands about 3.2 m tall.

## Getting them

- Reaching a Planting level unlocks each recipe. By default that's level 3 for the Small and level 4 for the Medium, and you can set each level.
- The Hydroponic Station (Large) still unlocks with the game's Hydroponic Cultivation talent, unless you give it a Planting level too. Once you have the Large's recipe, the Small and Medium unlock with it.
- If your save is already past a level, you get the recipe when it loads.
- The recipes are at the workbench next to the Large's.
- By default, the Small's package takes 1 Container, 1 Wire, 1 Electronic Components and 1 Iron Rod. The Medium's takes 1 Container, 1 Wire, 2 Electronic Components and 1 Iron Rod. You can change both.
- Before the disaster, the flowerpot shops that sell a grow box also sell the station of the same size. By default each one has 2 for sale, the Small for 60 (Flowerpots, Small Flowerpots, Electronic Flowerpots) and the Medium for 100 (Flowerpots, Medium Flowerpots, Electronic Flowerpots).
- By default, taking a station down gives back about half of what its package took. The `ReturnPercent` setting changes how much, and each station's `Returns` setting can name exactly what it gives back instead.
- On the Mod Manager's Mods screen (0.14.0 or newer), each station's materials and what it gives back show as item cards, and you can pick any item in the game. The screen shows what the recipe is worth, warns you when taking the station down would give back more than that, and tells you when the station can't use a list, for example one with exactly the same materials as another recipe.
- **Test buttons**: with Cheat Menu installed, its Mod tests tab gets two buttons. One lists each station's power, plant count, growth boost, price, whether its recipe is unlocked and which model the Large uses. The other unlocks both recipes right away.

## Power

The Hydroponic Station (Large) draws 6 power for four plants. By default the Small draws 2 and the Medium 3. For the Small and Medium, you set their power, growth boost, materials, what they give back when taken down, shop price and shop stock (see Settings).

## Settings

Settings live in `BepInEx\config\com.dev.survivallog.hydroponicsexpanded.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. See [CONFIG.md](CONFIG.md) for every setting.

## Save safety

The game saves each station as the grow box of its size, and the mod turns it back into a station when the save loads. Without the mod, your stations become grow boxes and the save still loads. The Large is a station the game already has, so nothing changes for it.

## Uninstall

A station package in a bag or in storage would be lost without the mod, so do this first:

1. Turn on `UninstallPrep` (on the Mods screen, or in the config file while the game is closed).
2. Load your save. Every station package, wherever it is, becomes the grow box package of the same size, and the shops stop selling stations. `BepInEx\LogOutput.log` says what was changed.
3. Save the game. Do steps 2 and 3 for each save you want to keep, then quit.

Then delete `HydroponicsExpanded.dll` from the `BepInEx\plugins` folder. To remove its settings too, also delete `com.dev.survivallog.hydroponicsexpanded.cfg` from `BepInEx\config`.

## Requirements

- Survival Log 1.1.20719.
- BepInEx for Survival Log.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.19.0 or newer. Hydroponics Expanded does not load without it. It applies settings changed on the Mod Manager's Mods screen without a restart.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file. Each station's materials and what it gives back when taken down show as item cards with the item's picture and name: pick an item from a list and set how many, with no item ids to look up.

## Install

Install Exiled's Toolkit. Then download `HydroponicsExpanded-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-HydroponicsExpanded/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/144), and extract it into the game folder (the folder with `SurvivalLog.exe`). It puts `HydroponicsExpanded.dll` in `BepInEx/plugins`.

## Configuration

See [CONFIG.md](CONFIG.md).

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log`.

To reach me directly, message `exiled.dev` on Discord. You can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## License

MIT
