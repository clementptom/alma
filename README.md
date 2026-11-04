# Alma: The Last Lock

A 2D Souls-like metroidvania with stealth, built in Unity 6. Hispania, 757: Corvin, a smuggler
and lockpick, and Barro, an old priest's dog, go after a saint's relic split in three.

This repository holds the **game systems** (movement, Souls-like combat with parry and dodge,
stealth, the dog companion, the Dread meter, lockpicking, saving, map, inventory, dialogue and
story sequences) and a **greybox vertical slice**: the Prologue and Act 0. Everything runs on
grey placeholder boxes until the real art and audio are added.

- **The story:** [`docs/THE_LAST_LOCK.md`](docs/THE_LAST_LOCK.md)
- **What art and audio the game needs:** [`docs/ASSET_LIST.md`](docs/ASSET_LIST.md)

## Getting started

### 1. Install Unity
1. Download and install **Unity Hub** from [unity.com/download](https://unity.com/download)
   (Windows 10/11 64-bit, macOS, or Ubuntu).
2. Open Unity Hub and sign in (a free Unity account is fine). If it asks about a license,
   choose the free **Personal** license (*Hub > Preferences > Licenses > Add*).
3. In **Installs > Install Editor**, pick **Unity 6.3 (LTS)**. Optional modules:
   *Microsoft Visual Studio Community* (to read or edit code) and build support for your
   own platform. Plan for 10+ GB of disk space.

### 2. Get the project
Any of these work:
- **GitHub Desktop (easiest to keep updated):** *File > Clone repository > clementptom/alma*.
  Later, *Fetch origin / Pull* brings in new changes.
- **ZIP:** on the GitHub page, *Code > Download ZIP*, then unzip it.
- **Command line:** `git clone https://github.com/clementptom/alma.git`

### 3. Open it
1. In Unity Hub choose **Projects > Add > Add project from disk** and select the folder that
   contains `Assets`, `Packages` and `ProjectSettings`.
2. If Hub says the editor version doesn't match, choose your installed **Unity 6.3** and continue.
   The first import takes a few minutes.
3. When Unity asks to *enable the new input system backends*, click **Yes**. Unity restarts.
   If no prompt appears, set *Edit > Project Settings > Player > Active Input Handling* to
   **Input System Package (New)** or **Both**.
4. Physics layers and data assets are created automatically on first open.

### 4. Play
1. Run **Alma > Build Greybox Demo** from the top menu bar. It generates placeholder sprites,
   prefabs and the scenes `MainMenu`, `Prologue_Merida`, `Act0_Pass`, `Area1` and `Area2`.
2. Open `Assets/Alma/Scenes/MainMenu.unity` and press **Play** (top center). Click inside
   the Game view so it receives keyboard input. Choose *Play*, then a save slot. A new game
   starts with the Prologue and continues into Act 0.
3. To test one chapter directly, open `Prologue_Merida` or `Act0_Pass` and press Play. That
   uses a separate debug save (F6 deletes it, so story scenes play again). `Area1` and `Area2`
   are the old systems sandbox: abilities, shops, fast travel and the Brute boss.

### If something goes wrong
- **"Enter Safe Mode?"** means scripts failed to compile. Choose *Enter Safe Mode*, open
  *Window > General > Console*, and send me the red error lines.
- **No Alma menu:** scripts haven't compiled yet. Check the Console for red errors.
- **Package errors:** if Unity reports a package version it can't find, open
  `Packages/manifest.json` and change it to the version *Window > Package Manager* offers
  (or remove the line and install it there). The game needs **Input System**, **Timeline**
  and **Unity UI**.
- Grey "[Audio] No clip assigned" messages are expected until sounds are added.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD (or arrows) | Left stick |
| Jump (hold for higher) | Space | A / Cross |
| Attack (hold Up/Down to aim) | J | X / Square |
| Dodge roll (air dash once unlocked) | K | B / Circle |
| Parry (just before a pale-flashing hit) | L | LB / L1 |
| Heal (hold, standing still) | H | RB / R1 |
| Sneak (hold) | Left Shift | LT / L2 |
| Throw a stone | F | RT / R2 |
| Interact (talk, rest, pick a lock; some need holding) | E | Y / Triangle |
| Barro: track / stay-heel / fetch / attack | 1 / 2 / 3 / 4 | D-pad up / down / left / right |
| Drop through platform | Down + Jump | Down + A |
| Map | Tab | Select / Share |
| Inventory | I | R3 (press the right stick) |
| Pause | Esc | Start |
| Back (in menus) | Esc or Backspace | B / Circle |

Every action except Pause can be rebound in **Controls** (pause menu, or Settings on the
title screen). Rebinds are saved, and a key already in use swaps with the old one.

Cheats in the editor and development builds: **F1** all abilities, **F2** refill health and
energy, **F3** +100 currency, **F4** god mode, **F5** save, **F6** delete the debug save.

## What's in the slice

**Prologue: The Siege Ends** (`Prologue_Merida`, Mérida 713, playing young Sisnando, who can't fight):
the relic is split in the basilica (Scene 1 of the storyboard); a stealth escape through the
aqueduct channels past the Drowned Ones and a patrol (sneak, hide in shadows, throw stones);
the parting in the hills and the three knocks; hiding the vial behind a closed door; the capture
and an escape from the camp before dawn; and the ship and the three-headed fire (Scene 2).
Being caught sends you back to the last checkpoint.

**Act 0: The Hospice in the Snow** (`Act0_Pass`, the Great St Bernard pass 757, playing Corvin):
the salt run and the avalanche (dig out with a button-mash), the dog's face (Scene 3), and Barro
joining. Then the seasons montage as the tutorial: fixing the roof, the dice game, picking the
shed lock, and finding a buried pilgrim with Barro in a storm full of pale wolves. The confession
by the fire (Scene 4), the morning Sisnando is gone (Scene 5: the tracks, the letter, the silver
collar), and the duel with **Malaspina** on the icy ledge, which teaches parry and dodge and
rewards his lockpicks. Then the demo ends.

Lines the storyboard scripts are used as written. Beats it doesn't script have short placeholder
lines (listed in `docs/ASSET_LIST.md`, section 13).

**Systems sandbox** (`Area1`, `Area2`): the original test levels, with an NPC, shop, fast
travel, the Dash / Wall Jump / Double Jump / Glide abilities, a key door, breakable walls, and
the Brute boss followed by the ending and credits.

## Systems

| System | Where | Notes |
|---|---|---|
| Player movement | `Player/PlayerController.cs` | Acceleration, coyote time, jump buffer, variable jump, one-way drop-through, moving platforms. Tune it in the Inspector on the Player prefab |
| Abilities | `Player/Abilities.cs` | Dash, Wall Jump, Double Jump, Glide. Unlocked by `AbilityPickup` or bought in a shop, stored in the save |
| Energy and healing | `Player/PlayerEnergy.cs` | Hitting enemies fills energy; hold Heal while standing to spend it on health. Getting hit interrupts. `EnergyUpgradePickup` raises the maximum |
| Combat | `Player/PlayerCombat.cs`, `Combat/` | Forward / up / down attacks, two-hit combo, hit-stop, screen shake, knockback. **Parry** a pale-flashing attack to stagger the enemy, then **riposte** for triple damage. **Dodge roll** with invulnerability frames through red-flashing (unparryable) attacks |
| Characters | `Player/CharacterProfile.cs`, `Core/ChapterSettings.cs` | A chapter picks who you play and what they can do (young Sisnando can't attack, parry, roll or heal; Corvin can), plus speed and whether Barro comes along |
| Stealth | `Stealth/`, `Player/PlayerStealth.cs` | Footsteps, landings, rolls, attacks and splashes make **noise**. Guards have a sight cone and a detection meter (?/! over their heads): unaware, suspicious, alert, searching. Sneak, hide in shadows, throw stones as distractions. `GuardEnemy` patrols, investigates, chases and strikes |
| Barro | `Companion/` | Follows, jumps after you, and teleports back if left behind. Commands: **track** (leads you to the nearest point of interest, e.g. a buried pilgrim), **stay / heel**, **fetch**, **attack**. Growls at hidden threats and reveals them. Drags you out of a fight once when you'd die (resets after a while). Can be knocked down for a few seconds |
| Dread | `Dread/` | 0–100 meter. Rises in dark zones, near the dead, while disguised and when Barro is away or down; drops by warm fires. High Dread brings whispers, false figures, a dark vignette and shaking aim. Each relic piece lowers the ceiling |
| Lockpicking | `Locks/` | Hold Interact on a lock, then press Confirm while the pick is in the gold zone, once per pin. Misses break picks and make noise. Malaspina's picks give an extra try and allow finer locks |
| Story sequences | `Narrative/StorySequence.cs`, `World/FlagActivator.cs` | Step lists for cutscenes: dialogue, title cards, fades, music, moving actors, teleports, flags, items, camera shake, button-mash, hints, video, Timeline, Barro steps, boss fights, scene loads. Triggered by area, scene start, interaction or story flags. `FlagActivator` keeps the world in step with the story after a reload |
| Water and ice | `World/WaterVolume.cs`, `World/SurfaceModifier.cs` | Slow swimming with strokes; slippery surfaces |
| Checkpoints | `World/Checkpoint.cs` | Autosave and respawn points along a chapter (besides save stations) |
| Enemies | `Enemies/` | `Enemy` base class plus walker, flyer and shooter. Subclass `Enemy` and override `Tick()` for new behaviors |
| Bosses | `Enemies/Boss.cs`, `BruteBoss.cs`, `BossArena.cs` | Sleep until the arena wakes them, phase 2 at 50%, gates lock during the fight, victory is saved |
| Rooms and camera | `World/Room.cs`, `CameraSystem/` | A `Room` box keeps the camera inside it, sets the music and ambience, and adds itself to the map |
| Scene travel | `World/RoomTransition.cs`, `Core/SceneLoader.cs` | Doors load another scene and place you at the matching door |
| Saving | `Save/` | Three JSON save slots, chosen on the title screen (with erase). Save stations heal, save and set the respawn point. Collected items, opened gates, beaten bosses, bought items and seen cutscenes are remembered through `PersistentId` |
| Inventory | `UI/InventoryUI.cs`, `Core/ItemDatabase.cs` | Abilities, key items and stats. Names, descriptions and icons come from `Resources/ItemDatabase` |
| Shops | `World/Shopkeeper.cs`, `UI/ShopUI.cs` | Sell key items, health/energy upgrades or abilities for currency. One-time purchases are saved |
| Fast travel | `World/FastTravelPoint.cs`, `UI/FastTravelUI.cs` | Shrines unlock when you walk past them; use one to travel to any other, across scenes |
| Controls | `Input/InputReader.cs`, `UI/ControlsMenu.cs` | Keyboard and gamepad rebinding with conflict swapping, saved in PlayerPrefs |
| Death and hazards | `Core/GameManager.cs`, `Combat/ContactDamage.cs` | Death respawns you at the last save station or checkpoint. Spikes (and a Drowned One's grab) send you back to the last safe ground |
| World objects | `World/` | Gates, levers, key doors, breakable walls, pickups, one-way, moving and crumbling platforms, bounce pads, parallax |
| Dialogue and cutscenes | `Narrative/` | NPC conversations (first and repeat), `CutsceneTrigger` for video, Timeline and/or dialogue |
| Ending | `Narrative/GameEnding.cs`, `UI/CreditsUI.cs` | Final dialogue / video / Timeline, marks the save completed, scrolls the credits (`Assets/Alma/Text/Credits.txt`), returns to the title |
| UI | `UI/`, `Locks/LockpickUI.cs` | HUD (health, energy, Dread, currency, stealth and Barro status, hints, hold prompts, boss bar, toasts), pause menu, map, popups, title cards, button-mash, lockpicking, title screen. All built in code, styled by `Resources/UISkin` |
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
- **New enemy:** subclass `Enemy` (see `WalkerEnemy.cs`), then add a `Health` and a collider.
  Use `Strike(...)` for telegraphed melee attacks (parryable or not; see `GuardEnemy.cs`), or
  `ContactDamage` for enemies that hurt on touch. Add `StealthPerception` if it should see and hear.
- **New story scene:** add a `StorySequence` and list its steps in the Inspector (see the
  generated Prologue and Act 0 scenes for examples). Use story flags and `FlagActivator` for
  anything that should stay changed after a reload.
- **New sound or effect:** add an ID to `AudioIds` / `EffectIds`, then run
  *Alma > Setup > Create or Update Data Assets* to add the slot.
- **Physics layers used:** Ground, OneWayPlatform, Player, Enemy, Breakable, Pickup, Companion (Barro).
  Hazards (spikes) go on Default.

## Not built yet

Acts I–IV, Hafsa and Idris, disguise among the Hollowed (the system exists, nothing uses it yet),
relic powers (the Bone's visions, the Dove's warning, the Snow's freeze), faction reputation,
endings, localization, achievements and difficulty options.

## Folder layout

```
Assets/Alma/
  Scripts/        game code (Editor/ = editor tools)
  Resources/      GameConfig, AudioLibrary, EffectsLibrary, UISkin, ItemDatabase, Player and Barro prefabs (generated)
  Data/Characters Corvin and YoungSisnando profiles (generated)
  Prefabs/        enemies, pickups, world objects (generated by the demo builder)
  Scenes/         MainMenu, Prologue_Merida, Act0_Pass, Area1, Area2 (generated by the demo builder)
  Art/            Animators/ (generated), Placeholder/ (generated), your art goes here
  Text/           Credits.txt (generated placeholder)
docs/THE_LAST_LOCK.md   the story
docs/ASSET_LIST.md      every sprite, sound, music track and cinematic the slice needs
```
