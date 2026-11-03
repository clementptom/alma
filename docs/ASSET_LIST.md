# Alma: Asset List

Everything the game needs from artists, AI tools and sound libraries. The code already
plays each item by name, so when a file is ready you drop it in and it works. Until then
the game uses grey boxes and plays no sound.

Every ID below (`player_jump`, `fx_slash`, `Run`...) matches the code exactly. Keep the names.

**Priority key:** **P1** = needed for a playable vertical slice. **P2** = polish. **P3** = nice to have.

---

## 0. Decide first: art style and resolution

The code works in world units. The player is 0.8 × 1.8 units, and one ground tile is 1 × 1 unit.
Choose one format and use it for every asset:

| | **A: HD illustrated (recommended for AI art)** | **B: Pixel art** |
|---|---|---|
| Pixels per unit (PPU) | 128 | 32 |
| Ground tile | 128 × 128 px | 32 × 32 px |
| Player frame canvas | 256 × 256 px (character ~230 px tall) | 64 × 64 px (character ~58 px tall) |
| Unity filter mode | Bilinear | Point (no filter) |
| Unity compression | Normal quality | None |

**Why A:** image generators like ChatGPT make clean illustrated art well, but their "pixel art"
usually has inconsistent pixel sizes and needs manual cleanup. Pick B only if you'll redraw or clean up frames.

### Rules for every sprite
- **Transparent PNG.** No background, no baked-in ground or shadow.
- **Side view, facing RIGHT.** The code mirrors sprites for the left side.
- **Same canvas size for every frame of a character**, with the **feet at the bottom center**. Set the Unity pivot to *Bottom Center*.
- **Same light direction everywhere** (e.g. light from the top-left).
- **Sprite sheets:** one animation per row, equal-sized cells, no gaps. Or deliver numbered frames: `player_run_01.png`, `player_run_02.png`, ...

### Getting consistent characters from ChatGPT
Frame-by-frame consistency is the hardest part of AI-made sprites. What works best:
1. First make a **character reference sheet**: front, side and back views on a plain background. Save it.
2. Attach that image to **every** later prompt and say "same character, same proportions, same colors as the reference".
3. Generate **key poses** (idle, run contact, jump, attack windup, attack follow-through), then fill the in-between frames, or animate with fewer frames.
4. **Strongly consider cutout animation** for the player and bosses: generate the character as separate parts (head, torso, upper/lower arms, upper/lower legs, cape) on one sheet, then animate in Unity with the free **2D Animation** package (bones). One consistent image then becomes every animation. I can wire this up when you're ready.

Prompt template:
> 2D side-view game sprite of [character], facing right, full body, [style: hand-painted dark fantasy / clean vector / ...], transparent background, centered, feet at the bottom of the frame, soft light from the upper left, no ground, no shadow, no text. Pose: [pose].

---

## 1. Player character (P1)

Folder: `Assets/Alma/Art/Characters/Player/`
Animator Controller (already created with these states): `Assets/Alma/Art/Animators/Player.controller`

| State | What it shows | Frames | FPS | Loop | Priority |
|---|---|---|---|---|---|
| `Idle` | Standing, breathing, cape sway | 6–8 | 8 | yes | P1 |
| `Run` | Full run cycle | 8 | 14 | yes | P1 |
| `Jump` | Rising: tucked, cape trailing down | 2–3 | 10 | no | P1 |
| `Fall` | Falling: arms up, cape up | 2–3 | 10 | yes | P1 |
| `Attack` | Forward slash (weapon in front) | 4–5 | 18 | no | P1 |
| `Hurt` | Recoil from a hit | 2 | 12 | no | P1 |
| `Death` | Collapses and ends lying down (last frame holds) | 8–10 | 12 | no | P1 |
| `Attack2` | Second combo slash (backhand) | 4–5 | 18 | no | P2 |
| `AttackUp` | Slash straight up | 4–5 | 18 | no | P2 |
| `AttackDown` | Mid-air slash straight down | 4–5 | 18 | no | P2 |
| `Land` | Short landing squash | 2–3 | 16 | no | P2 |
| `Dash` | Lunge pose, stretched forward | 3–4 | 16 | no | P2 |
| `WallSlide` | Clinging, wall is BEHIND the character (left side of the frame), facing right | 2 | 8 | yes | P2 |
| `DoubleJump` | Mid-air flip or spin | 4–6 | 16 | no | P2 |
| `Glide` | Cape or wings spread, drifting | 2–4 | 8 | yes | P2 |

Missing states fall back automatically (no `Glide` plays `Fall`; no `Attack2` plays `Attack`...),
so the seven P1 states are enough to start.

Also:
- **Player portrait** for dialogue, 512 × 512, bust shot (P3)
- **Character reference sheet** (for your own use with ChatGPT; not imported)

## 2. Enemies (P1)

Folder: `Assets/Alma/Art/Characters/Enemies/`. One Animator Controller per enemy already exists.
The collision box is listed so you can size the art: sprites can be a bit larger than it.

| Enemy (placeholder name) | Behavior | Hitbox (units) | States |
|---|---|---|---|
| **Crawler** (Walker) | Patrols the ground, charges when it sees you | 1.1 × 0.9 | `Idle`, `Move` (walk loop), `Hurt`, `Death` |
| **Wisp** (Flyer) | Hovers, swoops at the player | 0.9 round | `Move` (flying loop), `Hurt`, `Death` (+ `Idle` optional) |
| **Spitter** (Shooter) | Stationary, spits projectiles | 0.9 × 1.3 | `Idle`, `Attack` (0.35 s windup, then the shot), `Hurt`, `Death` |

Plus:
- **Enemy projectile**: small glowing orb or spit, 1–4 frame loop, ~0.45 units (P1)
- **Currency drop**: coin, shard or soul that the player collects, ~0.3 units, 6-frame spin (P1)

## 3. Boss: "The Brute" (P1)

Folder: `Assets/Alma/Art/Characters/Bosses/Brute/`. Hitbox 2.4 × 2.6 units: a big, heavy brawler.

| State | When it plays | Frames | Loop |
|---|---|---|---|
| `Idle` | Asleep before the fight, and between attacks | 6 | yes |
| `Wake` | Roar when the fight starts (~1.2 s) | 8–10 | no |
| `Move` | Walking toward the player | 8 | yes |
| `Telegraph` | Wind-up before every attack (~0.5 s): this is what players learn to read | 4–6 | no |
| `Charge` | Rushing across the arena | 4–6 | yes |
| `Leap` | Jumping into the air | 3–4 | no |
| `Attack` | Slam landing / firing the volley | 4–5 | no |
| `Stunned` | Dazed after hitting a wall (~1.1 s) | 4 | yes |
| `Death` | Defeat (last frame holds) | 10–12 | no |

Plus: **shockwave** sprite (travels along the floor, ~0.7 units tall), **boss projectile**, and a **boss name card or portrait** (P3).

## 4. NPCs and characters (P2)

- **Old Wanderer** (the tutorial NPC): `Idle` loop (4–6 frames), dialogue portrait 512 × 512
- **The voice in the Gallery** (cutscene): portrait or symbol 512 × 512 (P3)
- Add one row per NPC from your storyboard (each needs `Idle` plus a portrait)

## 5. World objects (P1 unless noted)

Folder: `Assets/Alma/Art/World/`

| Object | Size (units) | Notes |
|---|---|---|
| Save station (bench, shrine, campfire...) | 1.6 × 0.7 base | States: `Idle` (dim) and `Active` (lit, loop) |
| Lever | 0.7 × 1.2 | Base + handle as separate sprites (the code rotates the handle), or states `Idle`/`Active` |
| Gate | 1 × 5 | Slides up into the ceiling. Tileable vertically |
| Locked gate / key door | 1 × 5 | Same as the gate, with a lock symbol |
| Spikes / thorns | 1 × 0.6 per tile | Tileable horizontally |
| Breakable wall | 1 × 3 | Normal look + cracked look (P2) + debris chunks |
| One-way platform | 1 × 0.35 per tile | Tileable: left end, middle, right end |
| Moving platform | 3 × 0.5 | Mechanical or magical |
| Door / room exit marker | 1 × 4.5 | Optional arch or dark doorway (P2) |
| **Ability orb** (pickup) | 0.8 | Glowing loop, 6–8 frames |
| **Health upgrade** (pickup) | 0.6 | e.g. heart shard or mask fragment |
| **Key**: "Arena Key" | 0.5 | Plus a 256 × 256 icon for its pickup popup |

## 6. Environment art (P1 for Area 1, P2 for the rest)

For **each area** in your storyboard (currently *Area 1: Entrance + Great Hall*,
*Area 2: Echoing Gallery, Brute's Arena, Shaft, Summit*):

1. **Tileset** (Unity Tilemap): ground top, ground fill, left/right edges, inner and outer corners, ceiling underside, wall faces. Optional: 45° slopes. One sheet per area, 1 tile = 1 unit.
2. **Parallax background**: 3 layers (far, mid, near), each wide and seamless horizontally: 3840 × 2160 (option A) or 960 × 540 (option B). The code's `ParallaxLayer` does the motion.
3. **Foreground decoration** (P2): vines, roots, chains, columns, hanging lanterns, light shafts. Drawn in front of the player at low opacity.
4. **Props** (P2): rocks, bones, statues, signposts, banners: 5–10 per area.
5. **Boss arena backdrop** (P2): a special background for the fight.

## 7. Visual effects (P2)

Folder: `Assets/Alma/Art/VFX/`. Each effect is a prefab you assign in
`Assets/Alma/Resources/EffectsLibrary.asset`. Easiest way: a sprite flipbook. Add the
`SimpleSpriteAnimator` component, drag in the frames, and save as a prefab.

| ID | What it is | Frames |
|---|---|---|
| `fx_slash` | Crescent slash arc in front of the player (drawn facing right) | 4–5 |
| `fx_slash_vertical` | Same, for up/down attacks (drawn pointing right; the code rotates it) | 4–5 |
| `fx_hit_spark` | Impact burst when a hit lands | 4–6 |
| `fx_dust_jump` | Dust puff at the feet on jump | 4–5 |
| `fx_dust_land` | Dust spreading left/right on landing | 4–5 |
| `fx_dust_wall` | Small dust burst from a wall jump | 4 |
| `fx_double_jump` | Ring, feathers or burst under the feet | 5–6 |
| `fx_dash` | Speed streak or afterimage behind the player | 4–5 |
| `fx_player_hurt` | Dark or red burst when the player is hit | 4–5 |
| `fx_enemy_death` | Enemy dissolving or puff of smoke | 6–8 |
| `fx_projectile_hit` | Projectile popping | 4 |
| `fx_boss_slam` | Ground-cracking burst under the boss | 6–8 |
| `fx_breakable_debris` | Rock chunks flying (a particle system works well) | 6 |
| `fx_pickup_sparkle` | Sparkles when collecting | 5–6 |
| `fx_save_station` | Healing glow rising | 8 |
| `fx_ability_get` | Big burst of light for a new ability | 8–10 |

## 8. UI art

Assign in `Assets/Alma/Resources/UISkin.asset`. Everything is optional: empty slots draw flat colors.

| Item | Spec | Priority |
|---|---|---|
| **Font** (.ttf/.otf) | Readable at small sizes. Free options at Google Fonts (check the license) | P1 |
| Title logo "ALMA" | ~2000 × 700, transparent | P1 |
| Title screen background | 1920 × 1080 (or 3840 × 2160) | P1 |
| Health full / health empty icons | 128 × 128 each (mask, heart, flame...) | P1 |
| Currency icon | 128 × 128 | P1 |
| Panel (dialogue box, menus) | 9-slice frame, ~256 × 256 with clear borders | P2 |
| Button | 9-slice, ~512 × 128 | P2 |
| Boss health bar | Frame + fill, ~1200 × 40 | P2 |
| Ability icons: Dash, Wall Jump, Double Jump, Glide | 256 × 256 each (shown in the "new ability" popup) | P2 |
| Map player marker | 64 × 64 | P3 |
| Button prompt icons (keyboard + Xbox + PlayStation) | 64 × 64 each. The code shows text prompts for now; icons need a small UI change | P3 |

## 9. Sound effects

Assign in `Assets/Alma/Resources/AudioLibrary.asset` (every ID is already listed there).
Each entry can take **several clips**: one is picked at random each time, so give
footsteps and hits 3–4 variations.

**Format:** WAV, 44.1 or 48 kHz, 16-bit, mono is fine. Trim the silence at the start.
**Sources:** AI sound generators (e.g. ElevenLabs Sound Effects), freesound.org (check each
license: CC0 is easiest), Kenney.nl audio packs (CC0), the free Sonniss GDC bundles,
jsfxr / ChipTone (for a retro sound).

### Player (P1)
| ID | Sound | Variations |
|---|---|---|
| `player_footstep` | Soft step on stone | 4 |
| `player_jump` | Light whoosh or cloth flap | 2 |
| `player_land` | Soft thud | 2 |
| `player_attack` | Weapon swing whoosh | 3 |
| `player_attack_hit` | Blade hitting flesh or shell, punchy | 3 |
| `player_hurt` | Impact plus grunt or crack | 2 |
| `player_death` | Dramatic hit, then a fading tone | 1 |
| `player_double_jump` | Magical whoosh, higher pitch than the jump | 1 |
| `player_wall_jump` | Kick off stone plus whoosh | 2 |
| `player_dash` | Fast air burst | 2 |
| `player_heal` | Soft rising chime (resting at a save station) | 1 |

### Enemies and boss (P1)
| ID | Sound |
|---|---|
| `enemy_hurt` | Squish, crack or chitter (3 variations) |
| `enemy_death` | Burst or dissolve |
| `enemy_alert` | Short screech when an enemy spots you |
| `enemy_shoot` | Spit or magic launch |
| `projectile_hit` | Small pop or splat |
| `boss_roar` | Big, long roar (fight start) |
| `boss_charge` | Heavy footsteps and grunt when it charges |
| `boss_slam` | Huge ground impact (also used when it hits a wall) |
| `boss_hurt` | Deep grunt (3 variations) |
| `boss_death` | Long death cry plus collapse |
| `jingle_victory` | 2–4 s triumphant sting after the boss falls |

### World (P1 / P2)
| ID | Sound |
|---|---|
| `pickup_currency` | Tiny bright chime (very short; it plays a lot) |
| `pickup_item` | Item get (key items) |
| `pickup_ability` | 2–3 s fanfare for a new ability |
| `pickup_health_upgrade` | Warm heartbeat-like chime |
| `save_station` | Magical shimmer when resting |
| `gate_open` / `gate_close` | Heavy stone or metal grind |
| `lever` | Mechanical clunk |
| `breakable_hit` | Rock thud with crumble |
| `breakable_break` | Wall collapsing |
| `door_locked` | Rattle of a locked door |

### UI (P2)
| ID | Sound |
|---|---|
| `ui_move` | Soft tick when moving between buttons |
| `ui_confirm` | Select / confirm |
| `ui_back` | Close / back |
| `ui_pause` | Pause |
| `ui_map_open` | Paper unfolding |
| `ui_dialogue_blip` | Tiny voice blip while text types out (very short, ~50 ms) |

## 10. Music and ambience

Also in `AudioLibrary.asset`, under *Music*. **Format:** OGG Vorbis or WAV. **Must loop
seamlessly** (cut on the beat; test the loop point). 1.5–3 minutes each.
**Sources:** a composer, royalty-free libraries, or AI music generators (check that the plan
you use allows commercial use if you'll sell the game).

| ID | Mood | Priority |
|---|---|---|
| `music_title` | Main theme: melancholic, sets the tone | P1 |
| `music_area1` | Exploration: calm, mysterious (Entrance and Great Hall) | P1 |
| `music_area2` | Exploration: tenser, echoing (Gallery, Shaft, Summit) | P2 |
| `music_boss` | Boss fight: driving, percussive | P1 |
| `amb_area1` | Ambience loop: cave wind, distant drips | P2 |
| `amb_area2` | Ambience loop: echoes, low hum | P2 |

Add a music and ambience ID per area as your storyboard grows. Rooms pick their music by ID.

## 11. Cinematics

Two ways to do story scenes (both supported):

1. **Pre-rendered video** (from AI video tools, or rendered animation): plays full-screen
   with hold-to-skip. **Format:** MP4 (H.264) 1920 × 1080, 24–30 fps, AAC audio, or WebM (VP8).
2. **In-engine scene** (Unity Timeline): animate your sprites, camera and dialogue in Unity.
   Costs less, and always matches the game's art.

| Cinematic | Where it's used | Length | Priority |
|---|---|---|---|
| **Intro / opening** | `GameConfig > Intro Cinematic`. Plays on New Game | 30–90 s | P1 |
| Gallery "voice" scene | `CutsceneTrigger` in Area 2 (currently dialogue only; add a video or Timeline) | 10–20 s | P3 |
| Boss intro (optional) | Before the Brute wakes | 5–10 s | P3 |
| **Ending** | After the final boss (plays through a `CutsceneTrigger`) | 60–120 s | P2 |
| Any storyboard moments | One `CutsceneTrigger` each | — | — |

Also: **storyboard panels** (for you, not imported). Turning them into the intro video or a
still-image slideshow works well.

## 12. Writing (from your storyboard)

- NPC dialogue: each line has a speaker, text and an optional portrait. Use **Create > Alma > Dialogue**, or type lines on the NPC.
- Item / ability names and descriptions
- Area and room names (shown on the map)
- Boss names

---

## Import checklist (once files exist)

| Asset | In Unity |
|---|---|
| Sprites | Texture Type *Sprite (2D and UI)*; set PPU (128 or 32); set filter and compression per section 0; sheets: Sprite Mode *Multiple*, then slice in the Sprite Editor; pivot *Bottom Center* for characters |
| Character animation | Select the frames, drag them into the scene to create a clip, then drag the clip into the matching state of the existing Animator Controller. Put the controller on the character's `Visual` child, and replace the placeholder `Body` sprite |
| Effects | Prefab with `SimpleSpriteAnimator` → assign in `Resources/EffectsLibrary` |
| SFX | Load Type *Decompress On Load* → assign in `Resources/AudioLibrary` |
| Music / ambience | Load Type *Streaming*, compression *Vorbis* → assign in `Resources/AudioLibrary` |
| UI art | Sprite; set the 9-slice borders in the Sprite Editor → assign in `Resources/UISkin` |
| Video | Drop the .mp4 into `Assets/Alma/Cinematics/` → assign in `GameConfig` or a `CutsceneTrigger` |

**Big files:** use Git LFS for PNG/WAV/MP4 (`git lfs install`, then `git lfs track "*.png" "*.wav" "*.ogg" "*.mp4"`) so the repository stays small.
