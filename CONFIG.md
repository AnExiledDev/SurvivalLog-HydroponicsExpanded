# Hydroponics Expanded settings

Settings live in `BepInEx\config\com.dev.survivallog.hydroponicsexpanded.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back.

## SmallStation and MediumStation

Each station has its own section, `[SmallStation]` and `[MediumStation]`, with the same six settings.

| Setting | Small default | Medium default | What it does |
| --- | --- | --- | --- |
| `PowerCost` | `2` | `3` | Power the station draws while it runs. The Hydroponic Station (Large) draws 6, the Plant Grow Box (Small) 1 and the Plant Grow Box (Medium) 3. Any value from `0` to `50`. |
| `GrowthBoost` | `0.5` | `0.5` | How much faster plants grow in the station while it has power: `0.5` is 50% faster, the same as the Hydroponic Station (Large). The grow boxes give `0.3`. Any value from `0` to `5`. |
| `Materials` | `20102:1, 20104:1, 20210:1, 20361:1` | `20102:1, 20104:1, 20210:2, 20361:1` | What crafting the station's package takes, written as item id:count pairs split by commas. 20102 is Container, 20104 Wire, 20210 Electronic Components, 20361 Iron Rod and 20362 Iron Parts. Up to 7 different items and up to 99 of each. A list the game cannot use (one it cannot read, an item that does not exist, or the same materials as another recipe, the other station's included) falls back to the default, with a line in `BepInEx\LogOutput.log` saying why. Taking the station down gives back about half of these. |
| `ShopPrice` | `60` | `100` | What the flowerpot shops ask for the station before the disaster. The Plant Grow Box (Small) costs 25 there, the Plant Grow Box (Medium) 50 and the Hydroponic Station (Large) 150. Any value from `0` to `9999`. |
| `ShopStock` | `2` | `2` | How many of the station each flowerpot shop that sells the grow box of its size has for sale before the disaster. `0` takes them off the shelves. A shop that already has more than this is brought down to it, and stations you already bought stay yours. Any value from `0` to `20`. |
| `UnlockLevel` | `3` | `4` | The Planting level that unlocks the station's recipe. Getting the Hydroponic Station (Large)'s recipe, by its level or by the Hydroponic Cultivation talent, unlocks it at any level. `0` leaves it to the Large's recipe alone. A run already at this level gets the recipe when its save loads. Any value from `0` to `5`. |

## LargeStation

| Setting | Default | What it does |
| --- | --- | --- |
| `NewLook` | `true` | Gives the game's Hydroponic Station (Large) the new model and icon that match the Small and Medium. Turn it off for the game's look. A change shows on stations placed or loaded afterwards. |
| `UnlockLevel` | `0` | The Planting level that unlocks the Hydroponic Station (Large)'s recipe, which also unlocks the Small and Medium ones. `0`, the default, leaves it to the game's Hydroponic Cultivation talent, which unlocks it at any level. A run already at this level gets the recipe when its save loads. Any value from `0` to `5`. |

## Uninstall

| Setting | Default | What it does |
| --- | --- | --- |
| `UninstallPrep` | `false` | Turn this on before you remove the mod, then load your save. Every station package, in bags, in storage, on the floor and in every place the save holds, becomes the grow box package of its size, and the shops stop selling stations. Placed and packed stations are always saved as grow boxes, so they need nothing. The Small and Medium recipes come off the unlocked list. Save the game, quit, and remove `HydroponicsExpanded.dll`. `BepInEx\LogOutput.log` says what was changed. Turning it off again does not turn the packages back. |
