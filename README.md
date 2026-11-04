# Alma

A 2D metroidvania built in Unity 6. This repository holds the **game systems**: movement,
combat, healing, enemies, bosses, saving, map, inventory, shops, fast travel, dialogue,
cutscenes and the ending. They run on grey placeholder boxes until the real art, audio and
story are added.

- **What art and audio the game needs:** [`docs/ASSET_LIST.md`](docs/ASSET_LIST.md)

## Getting started

1. Install **Unity 6** (6000.0 LTS or newer) through Unity Hub.
2. In Unity Hub choose **Add > Add project from disk** and select this folder. If Hub
   offers a newer editor version, accept it.
3. When Unity asks *"enable the new input system backends?"*, click **Yes** (Unity restarts).
   If no prompt appears, set *Edit > Project Settings > Player > Active Input Handling* to
   **Input System Package (New)** or **Both**.
4. On first open the project creates its physics layers and data assets on its own.
5. Run **Alma > Build Greybox Demo** from the top menu. This generates placeholder sprites,
   prefabs and three playable scenes.
6. Open `Assets/Alma/Scenes/MainMenu.unity` and press **Play**. You can also open `Area1` or
   `Area2` and press Play to test a level directly; that uses a separate debug save.

> If Unity reports a package version it can't find, open `Packages/manifest.json` and change
> that package's version to the one *Window > Package Manager* offers (or remove the line and
> install the package from there). The game needs **Input System**, **Timeline** and **Unity UI**.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move / aim | WASD (or arrows) | Left stick / D-pad |
| Jump (hold for higher) | Space | A / Cross |
| Attack (hold Up/Down to aim) | J | X / Square |
| Dash | K | RB (or RT) |
| Heal (hold, standing still) | L | B / Circle |
| Interact (talk, rest, shop, travel) | E | Y / Triangle |
| Drop through platform | Down + Jump | Down + A |
| Map | Tab | Select |
| Inventory | I | LB |
| Pause | Esc | Start |
| Back (in menus) | Esc or Backspace | B / Circle |

Every action except Pause can be rebound in **Controls** (pause menu, or Settings on the
title screen). Rebinds are saved, and a key already in use swaps with the old one.

Cheats in the editor and development builds: **F1** all abilities, **F2** refill health and
energy, **F3** +100 currency, **F4** god mode, **F5** save, **F6** delete the debug save.

## What's in the demo

**Area 1:** a tutorial NPC, a save station, a merchant, one-way platforms, a bounce pad,
a crawler and a wisp, a spike pit, a hidden breakable wall with a health upgrade, and a
lever that opens a gate. The second room has a fast travel shrine, the **Dash** ability,
a gap that needs it, a moving platform, and an energy upgrade on top of a tall wall.

**Area 2:** a second fast travel shrine, an ambient cutscene, a shooter enemy, a key on
high platforms (one of them crumbles), a locked door, and the **Brute** boss fight, which
rewards **Wall Jump**. After that comes a wall-jump shaft up to **Double Jump** and
**Glide**. Walking to the end of the summit plays the ending and the credits.

## Systems

| System | Where | Notes |
|---|---|---|
| Player movement | `Player/PlayerController.cs` | Acceleration, coyote time, jump buffer, variable jump, one-way drop-through, moving platforms. Tune it in the Inspector on the Player prefab |
| Abilities | `Player/Abilities.cs` | Dash, Wall Jump, Double Jump, Glide. Unlocked by `AbilityPickup` or bought in a shop, stored in the save |
| Energy and healing | `Player/PlayerEnergy.cs` | Hitting enemies fills energy; hold Heal while standing to spend it on health. Getting hit interrupts. `EnergyUpgradePickup` raises the maximum |
| Combat | `Player/PlayerCombat.cs`, `Combat/` | Forward / up / down attacks, two-hit combo, pogo on downward hits, hit-stop, screen shake, knockback, invulnerability frames |
| Enemies | `Enemies/` | `Enemy` base class plus walker, flyer and shooter. Subclass `Enemy` and override `Tick()` for new behaviors |
| Bosses | `Enemies/Boss.cs`, `BruteBoss.cs`, `BossArena.cs` | Sleep until the arena wakes them, phase 2 at 50%, gates lock during the fight, victory is saved |
| Rooms and camera | `World/Room.cs`, `CameraSystem/` | A `Room` box keeps the camera inside it, sets the music and ambience, and adds itself to the map |
| Scene travel | `World/RoomTransition.cs`, `Core/SceneLoader.cs` | Doors load another scene and place you at the matching door |
| Saving | `Save/` | Three JSON save slots, chosen on the title screen (with erase). Save stations heal, save and set the respawn point. Collected items, opened gates, beaten bosses, bought items and seen cutscenes are remembered through `PersistentId` |
| Inventory | `UI/InventoryUI.cs`, `Core/ItemDatabase.cs` | Abilities, key items and stats. Names, descriptions and icons come from `Resources/ItemDatabase` |
| Shops | `World/Shopkeeper.cs`, `UI/ShopUI.cs` | Sell key items, health/energy upgrades or abilities for currency. One-time purchases are saved |
| Fast travel | `World/FastTravelPoint.cs`, `UI/FastTravelUI.cs` | Shrines unlock when you walk past them; use one to travel to any other, across scenes |
| Controls | `Input/InputReader.cs`, `UI/ControlsMenu.cs` | Keyboard and gamepad rebinding with conflict swapping, saved in PlayerPrefs |
| Death and hazards | `Core/GameManager.cs`, `Combat/ContactDamage.cs` | Death respawns you at the last save station. Spikes send you back to the last safe ground |
| World objects | `World/` | Gates, levers, key doors, breakable walls, pickups, one-way, moving and crumbling platforms, bounce pads, parallax |
| Dialogue and cutscenes | `Narrative/` | NPC conversations (first and repeat), `CutsceneTrigger` for video, Timeline and/or dialogue |
| Ending | `Narrative/GameEnding.cs`, `UI/CreditsUI.cs` | Final dialogue / video / Timeline, marks the save completed, scrolls the credits (`Assets/Alma/Text/Credits.txt`), returns to the title |
| UI | `UI/` | HUD (health, energy, currency, prompts, boss bar, toasts), pause menu, map with save/travel markers, popups, title screen. All built in code, styled by `Resources/UISkin` |
| Audio | `Audio/` | Sounds and music are played by ID; assign clips in `Resources/AudioLibrary` |
| Effects | `Effects/` | Visual effects are spawned by ID; assign prefabs in `Resources/EffectsLibrary` |

Everything starts automatically: `Core/Bootstrapper.cs` creates the persistent managers
before the first scene loads, so any scene can be played directly.

### Adding content

- **New level:** a new scene with ground (sprites or a Tilemap on the **Ground** layer),
  `Room` boxes, at least one `SpawnPoint`, and `RoomTransition` doors. Add the scene to
  *File > Build Profiles*. Lay scenes out in one shared world coordinate system so the map lines up.
- **New ability:** add it to `AbilityType`, describe it in `AbilityInfo` (or in
  `Resources/ItemDatabase`), and check `GameManager.Instance.HasAbility(...)` where it matters.
- **New key item:** add an entry to `Resources/ItemDatabase` (id, name, description, icon),
  then use that id on a `KeyItemPickup`, a `KeyDoor` or a shop entry.
- **New enemy:** subclass `Enemy` (see `WalkerEnemy.cs`), then add a `Health`, a collider,
  and `ContactDamage`.
- **New sound or effect:** add an ID to `AudioIds` / `EffectIds`, then run
  *Alma > Setup > Create or Update Data Assets* to add the slot.
- **Physics layers used:** Ground, OneWayPlatform, Player, Enemy, Breakable, Pickup.
  Hazards (spikes) go on Default.

## Not built yet

Localization, equipment or charm slots, spells or ranged attacks, achievements, and
difficulty options. Which of these the game needs depends on the design.

## Folder layout

```
Assets/Alma/
  Scripts/        game code (Editor/ = editor tools)
  Resources/      GameConfig, AudioLibrary, EffectsLibrary, UISkin, ItemDatabase, Player prefab (generated)
  Prefabs/        enemies, pickups, world objects (generated by the demo builder)
  Scenes/         MainMenu, Area1, Area2 (generated by the demo builder)
  Art/            Animators/ (generated), Placeholder/ (generated), your art goes here
  Text/           Credits.txt (generated placeholder)
docs/ASSET_LIST.md   every sprite, sound, music track and cinematic the game needs
```
