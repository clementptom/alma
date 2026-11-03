# Alma

A 2D metroidvania built in Unity 6. This repository holds the **game systems**: movement,
combat, enemies, bosses, saving, map, dialogue and cutscenes. They run on grey placeholder
boxes until the real art, audio and story are added.

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
| Move / aim | WASD or arrows | Left stick / D-pad |
| Jump (hold for higher) | Space or Z | A / Cross |
| Attack (hold Up/Down to aim) | J or X | X / Square |
| Dash | K, C or Left Shift | RB / RT |
| Interact (talk, rest, pull) | E | Y / Triangle |
| Drop through platform | Down + Jump | Down + A |
| Map | Tab or M | Select |
| Pause | Esc | Start |

Cheats in the editor and development builds: **F1** all abilities, **F2** heal,
**F3** +100 currency, **F4** god mode, **F5** save, **F6** delete the debug save.

## What's in the demo

**Area 1:** a tutorial NPC, a save station, one-way platforms, a crawler and a wisp,
a spike pit, a hidden breakable wall with a health upgrade, and a lever that opens a gate.
The second room has the **Dash** ability, a gap that needs it, and a moving platform.

**Area 2:** an ambient cutscene, a shooter enemy, a key on high platforms, a locked door,
and the **Brute** boss fight, which rewards **Wall Jump**. After that comes a wall-jump
shaft up to **Double Jump** and **Glide**.

## Systems

| System | Where | Notes |
|---|---|---|
| Player movement | `Player/PlayerController.cs` | Acceleration, coyote time, jump buffer, variable jump, one-way drop-through, moving platforms. Tune it in the Inspector on the Player prefab |
| Abilities | `Player/Abilities.cs` | Dash, Wall Jump, Double Jump, Glide. Unlocked by `AbilityPickup`, stored in the save |
| Combat | `Player/PlayerCombat.cs`, `Combat/` | Forward / up / down attacks, two-hit combo, pogo on downward hits, hit-stop, screen shake, knockback, invulnerability frames |
| Enemies | `Enemies/` | `Enemy` base class plus walker, flyer and shooter. Subclass `Enemy` and override `Tick()` for new behaviors |
| Bosses | `Enemies/Boss.cs`, `BruteBoss.cs`, `BossArena.cs` | Sleep until the arena wakes them, phase 2 at 50%, gates lock during the fight, victory is saved |
| Rooms and camera | `World/Room.cs`, `CameraSystem/` | A `Room` box keeps the camera inside it, sets the music and ambience, and adds itself to the map |
| Scene travel | `World/RoomTransition.cs`, `Core/SceneLoader.cs` | Doors load another scene and place you at the matching door |
| Saving | `Save/` | JSON save slots. Save stations heal, save and set the respawn point. Collected items, opened gates, beaten bosses and seen cutscenes are remembered through `PersistentId` |
| Death and hazards | `Core/GameManager.cs`, `Combat/ContactDamage.cs` | Death respawns you at the last save station. Spikes send you back to the last safe ground |
| World objects | `World/` | Gates, levers, key doors, breakable walls, pickups, one-way and moving platforms, parallax |
| Dialogue and cutscenes | `Narrative/` | NPC conversations (first and repeat), `CutsceneTrigger` for video, Timeline and/or dialogue |
| UI | `UI/` | HUD (health, currency, prompts, boss bar, toasts), pause menu with volume, map, popups, title screen. All built in code, styled by `Resources/UISkin` |
| Audio | `Audio/` | Sounds and music are played by ID; assign clips in `Resources/AudioLibrary` |
| Effects | `Effects/` | Visual effects are spawned by ID; assign prefabs in `Resources/EffectsLibrary` |

Everything starts automatically: `Core/Bootstrapper.cs` creates the persistent managers
before the first scene loads, so any scene can be played directly.

### Adding content

- **New level:** a new scene with ground (sprites or a Tilemap on the **Ground** layer),
  `Room` boxes, at least one `SpawnPoint`, and `RoomTransition` doors. Add the scene to
  *File > Build Profiles*. Lay scenes out in one shared world coordinate system so the map lines up.
- **New ability:** add it to `AbilityType`, describe it in `AbilityInfo`, and check
  `GameManager.Instance.HasAbility(...)` where it matters.
- **New enemy:** subclass `Enemy` (see `WalkerEnemy.cs`), then add a `Health`, a collider,
  and `ContactDamage`.
- **New sound or effect:** add an ID to `AudioIds` / `EffectIds`, then run
  *Alma > Setup > Create or Update Data Assets* to add the slot.
- **Physics layers used:** Ground, OneWayPlatform, Player, Enemy, Breakable, Pickup.
  Hazards (spikes) go on Default.

## Not built yet

Shops, an inventory screen, fast travel, a healing ability, input rebinding, localization,
and an ending sequence. These will be added once the game design is decided.

## Folder layout

```
Assets/Alma/
  Scripts/        game code (Editor/ = editor tools)
  Resources/      GameConfig, AudioLibrary, EffectsLibrary, UISkin, Player prefab (generated)
  Prefabs/        enemies, pickups, world objects (generated by the demo builder)
  Scenes/         MainMenu, Area1, Area2 (generated by the demo builder)
  Art/            Animators/ (generated), Placeholder/ (generated), your art goes here
docs/ASSET_LIST.md   every sprite, sound, music track and cinematic the game needs
```
