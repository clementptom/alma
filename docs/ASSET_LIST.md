# The Last Lock: Asset List

This list covers the art, sound, music and cinematics needed for **the first playable slice**
of *The Last Lock*: the Prologue (Mérida, 713, young Sisnando) and Act 0 (the Great St Bernard
pass, 757, Corvin and Barro). The story is in [`THE_LAST_LOCK.md`](THE_LAST_LOCK.md).

The code plays every item below by name. Drop a file into its slot and it works; until then
the game shows grey boxes and plays no sound. Every ID below (`barro_growl`, `fx_parry_spark`,
`Roll`...) matches the code exactly, so keep the names.

**Priority key:**
- **P1** = needed for the slice to look and sound like the game.
- **P2** = polish for the slice.
- **P3** = later acts, or the old sandbox demo (Area1/Area2), which is still in the project for testing.

---

## 0. Decide first: art style and resolution

The code works in world units. Corvin is 0.8 × 1.8 units, and one ground tile is 1 × 1 unit.
Choose one format and use it for every asset:

| | **A: HD illustrated (recommended for AI art)** | **B: Pixel art** |
|---|---|---|
| Pixels per unit (PPU) | 128 | 32 |
| Ground tile | 128 × 128 px | 32 × 32 px |
| Character frame canvas | 256 × 256 px (character ~230 px tall) | 64 × 64 px (character ~58 px tall) |
| Unity filter mode | Bilinear | Point (no filter) |
| Unity compression | Normal quality | None |

**Why A:** image generators like ChatGPT make clean illustrated art well, but their "pixel art"
usually has inconsistent pixel sizes and needs cleanup. Pick B only if you'll redraw or clean up frames.

**Look and feel, from the story:** early-medieval (8th century), not high fantasy. Visigothic,
Frankish, Arab and Berber dress and arms. Muted, cold palettes: ash red and candle gold for burning
Mérida, blue-grey and white for the Alps. The horror is quiet, so leave room in the art for darkness and snow.

### Rules for every sprite
- **Transparent PNG.** No background, no baked-in ground or shadow.
- **Side view, facing RIGHT.** The code mirrors sprites for the left side.
- **Same canvas size for every frame of a character**, with the **feet at the bottom center**. Set the Unity pivot to *Bottom Center*.
- **Same light direction everywhere** (e.g. light from the top-left).
- **Sprite sheets:** one animation per row, equal-sized cells, no gaps. Or numbered frames: `corvin_run_01.png`, `corvin_run_02.png`, ...

### Getting consistent characters from ChatGPT
1. First make a **character reference sheet** (front, side and back on a plain background) and save it.
2. Attach that image to **every** later prompt: "same character, same proportions, same colors as the reference".
3. Generate **key poses** first (idle, run contact, jump, attack windup, attack follow-through), then fill in between, or animate with fewer frames.
4. **Consider cutout animation** for Corvin, Barro and Malaspina: generate the character as separate parts (head, torso, upper/lower arms and legs, cloak, tail) on one sheet, and animate them in Unity with the free **2D Animation** package (bones). One consistent image then becomes every animation. I can wire this up when you're ready.

Prompt template:
> 2D side-view game sprite of [character], facing right, full body, hand-painted early-medieval dark fantasy, muted cold colors, transparent background, centered, feet at the bottom of the frame, soft light from the upper left, no ground, no shadow, no text. Pose: [pose].

---

## 1. Corvin of the Pass (player, P1)

Smuggler and lockpick, about 30. Practical mountain clothes, a smuggler's pack, a short sword,
and the **bishop's boots** Sisnando notices (good leather, too fine for him).

Folder: `Assets/Alma/Art/Characters/Corvin/`. Animator Controller (already has these states):
`Assets/Alma/Art/Animators/Player.controller`. Hitbox 0.8 × 1.8 units.

| State | What it shows | Frames | FPS | Loop | Priority |
|---|---|---|---|---|---|
| `Idle` | Standing, breathing, breath steaming in the cold | 6–8 | 8 | yes | P1 |
| `Run` | Run cycle | 8 | 14 | yes | P1 |
| `Jump` | Rising | 2–3 | 10 | no | P1 |
| `Fall` | Falling | 2–3 | 10 | yes | P1 |
| `Attack` | Forward sword slash | 4–5 | 18 | no | P1 |
| `Roll` | Dodge roll along the ground (~0.34 s) | 5–6 | 18 | no | P1 |
| `Parry` | Blade up, catching a blow (~0.2 s window, then recovery) | 3–4 | 16 | no | P1 |
| `Sneak` | Crouched walk | 6–8 | 10 | yes | P1 |
| `Crouch` | Crouched, still (hiding, waiting) | 2–4 | 6 | yes | P1 |
| `Hurt` | Recoil from a hit | 2 | 12 | no | P1 |
| `Death` | Collapses (last frame holds) | 8–10 | 12 | no | P1 |
| `Heal` | Kneels and drinks or binds a wound | 4–6 | 10 | yes | P2 |
| `Throw` | Underarm stone toss | 3–4 | 14 | no | P2 |
| `Swim` | Treading water | 4–6 | 8 | yes | P2 |
| `Attack2` | Second combo slash, also used for the riposte | 4–5 | 18 | no | P2 |
| `Land` | Short landing squash | 2–3 | 16 | no | P2 |
| `AttackUp` / `AttackDown` | Slash up / mid-air slash down | 4–5 each | 18 | no | P3 |
| `Dash`, `WallSlide`, `DoubleJump`, `Glide` | Later abilities (not in the slice) | — | — | — | P3 |

Missing states fall back to a close one automatically (no `Roll` plays `Dash`, then `Run`; no
`Parry` plays `Attack`; no `Sneak` plays `Run`...), so you can start with a few.
`Downed` exists in the controller but isn't used yet.

Also:
- **Dialogue portrait**, 512 × 512, bust (P2)
- **Character reference sheet** (for your own use with ChatGPT; not imported)

## 2. Young Sisnando (Prologue player, P1)

A deacon of about 26 in 713: plain dark robe, tonsure, carrying the crystal vial. He can't
fight, so he needs no attack, parry or roll animations.

Animator Controller: `Player_YoungSisnando.controller`. Same canvas and hitbox as Corvin.

| State | Notes | Priority |
|---|---|---|
| `Idle`, `Run`, `Jump`, `Fall`, `Hurt`, `Death` | As for Corvin. Death is "caught": dragged down by a hand, or seized by a soldier | P1 |
| `Sneak`, `Crouch` | The Prologue is a stealth chapter, so these matter most | P1 |
| `Throw` | Tossing a stone to distract a guard | P1 |
| `Swim` | Fallen into the aqueduct channels | P2 |
| `Land` | | P2 |

Plus a 512 × 512 **portrait** (P2).

## 3. Barro (companion, P1)

A huge Alpine mountain dog, the ancestor of the St Bernard: heavy, shaggy, brown and white,
kind eyes. From the end of Act 0 he wears **Sisnando's silver collar**.

Folder: `Assets/Alma/Art/Characters/Barro/`. Animator Controller: `Barro.controller`.
Body about 1.4 × 0.85 units with the head in front (hitbox 1.3 × 0.85).
**Draw the collar as a separate sprite** on its own layer: the code shows it only after the letter scene.

| State | When it plays | Frames | Loop | Priority |
|---|---|---|---|---|
| `Idle` | Standing, panting | 6 | yes | P1 |
| `Run` | Following Corvin, running to a target | 8 | yes | P1 |
| `Sit` | "Stay / guard" command; also after idling a few seconds | 3–4 | yes | P1 |
| `Sniff` | Tracking a scent, nose to the snow | 6 | yes | P1 |
| `Growl` | Hackles up, at a hidden threat (wolves in the snow) | 4 | yes | P1 |
| `Bite` | Attack command | 4 | no | P1 |
| `Drag` | Pulling a downed Corvin to safety by the collar (walks backwards, facing right) | 8 | yes | P1 |
| `Howl` | Head up, howling (the tracks scene) | 6 | no | P2 |
| `Bark` | Short bark (found something) | 3 | no | P2 |
| `Jump`, `Fall` | Following over ledges | 2 each | no | P2 |
| `Hurt` | Hit | 2 | no | P2 |
| `Downed` | Lying hurt; gets up after a few seconds | 4 | yes | P2 |

Plus a 512 × 512 portrait (P3).

## 4. Story characters (P1 for the slice)

They appear in cutscenes and walk when a story step moves them, so each needs `Idle` and a
`Move` (walk) loop. Each one already has its own Animator Controller.

| Character | Controller | Look | Size (units) | Extra |
|---|---|---|---|---|
| **Father Sisnando** (old, ~70) | `NPC_Sisnando` | Older Sisnando: white beard, patched robe, warm face | 0.85 × 1.6 | Portrait (P1) |
| **Abbot Ervigio** | `Actor_Ervigio` | Elderly abbot, rich but soot-stained vestments, a crozier | 0.85 × 1.75 | Portrait (P2) |
| **Brother Gundemar** | `Actor_Gundemar` | Lean monk in his 30s, hood; quiet, intense (the future villain) | 0.8 × 1.7 | Portrait (P2) |
| **Soldier** (ambush) | `Actor_Soldier` | Tired conquest-era soldier, mail and turban or helmet, spear | 0.9 × 1.8 | |
| **Sailor** | `Actor_Sailor` | Merchant-ship sailor | 0.9 × 1.75 | |
| **Pilgrim** | `Actor_Pilgrim` | Pilgrim in a heavy cloak, frost-bitten, with a staff | 0.85 × 1.65 | |
| **Stray dog** (Prologue) | `Actor_StrayDog` | Thin brown street dog. Fair-play clue: the dog Sisnando kneels beside | 0.9 × 0.6 | |
| **Malaspina** | (boss, see section 5) | | | Portrait (P2) |

## 5. Enemies and the Act 0 mini-boss (P1)

Folder: `Assets/Alma/Art/Characters/Enemies/`. Sprites can be a bit larger than the hitbox.

### Soldier / guard (`Enemy_Guard.controller`). Hitbox 0.9 × 1.8
Patrols, notices you (a "?" appears), searches, then chases and strikes. Same look as the ambush soldiers, with a sword.

| State | When | Frames | Priority |
|---|---|---|---|
| `Idle` | Standing watch | 4–6 | P1 |
| `Move` | Patrol walk | 8 | P1 |
| `Run` | Chasing | 8 | P1 |
| `Search` | Looking around, hand to brow (suspicious) | 4–6 | P2 |
| `Telegraph` | Sword raised before a strike (~0.5 s). Players learn to read this | 3–4 | P1 |
| `Attack` | The strike | 3–4 | P1 |
| `Stunned` | Staggered after you parry him | 3–4 | P1 |
| `Hurt`, `Death` | | 2 / 6–8 | P1 |

### Drowned One (`Enemy_Drowned.controller`). Hitbox 1 × 1.4
A drowned corpse in the aqueduct channels, mostly under water. It turns its head to follow you
long before it attacks, then rises and grabs.

| State | When | Frames | Priority |
|---|---|---|---|
| `Idle` | Floating, head turning slowly (the code sinks it ~0.9 units) | 4–6 | P1 |
| `Rise` | Surging up out of the water (~0.55 s) | 4–5 | P1 |
| `Attack` | Grabbing upward (reaches a player standing on the plank above) | 3–4 | P1 |
| `Telegraph`, `Hurt`, `Death` | | 2–6 | P2 |

### Pale Wolf (`Enemy_PaleWolf.controller`). Hitbox 1.2 × 0.8
Grey-white, nearly invisible in snow (the code draws it at ~20% opacity until Barro growls and reveals it).

| State | Frames | Priority |
|---|---|---|
| `Idle`, `Move` (lope), `Attack` (lunge bite), `Hurt`, `Death` | 4–8 each | P1 |

### Malaspina, King of the Pass (`Boss_Malaspina.controller`). Hitbox 0.95 × 1.85
Corvin's old smuggling boss: fur-trimmed coat, rings, a fine sword. The duel on the icy ledge
teaches parry and dodge. Pale flash = parry it; red flash = roll away.

| State | When | Frames | Priority |
|---|---|---|---|
| `Idle` | En garde | 6 | P1 |
| `Move` | Advancing | 8 | P1 |
| `Telegraph` | Wind-up before each slash (~0.4–0.6 s) | 4 | P1 |
| `Attack` | Slash | 4 | P1 |
| `Attack2` | Second slash of a combo | 4 | P2 |
| `Lunge` | The unparryable lunge (red flash) | 4–5 | P1 |
| `Stunned` | Parried and off balance | 3–4 | P1 |
| `Hurt`, `Death` | Death = yields, kneels (last frame holds) | 2 / 6–8 | P1 |

Plus a **boss name card** (optional, P3).

## 6. Places and environment art

For **each place**: a **tileset** (ground top, fill, edges, inner/outer corners, ceiling
underside, wall faces; 1 tile = 1 unit), a **3-layer parallax background** (far, mid, near;
seamless horizontally; 3840 × 2160 for option A), and **props**. The greybox scenes show where
everything goes; the builder names each placeholder ("Altar", "Hut door", "Mast"...).

### Prologue: Mérida, 713 (`Prologue_Merida` scene)
| Place | Tiles / backdrop | Props | Priority |
|---|---|---|---|
| **Basilica of Santa Eulalia** | Stone nave, pillars, doorway to the crypt; snow falling **indoors** | Altar, split reliquary, candles, siege glow in the windows | P1 |
| **Aqueduct channels** | Roman stone channels, dark water, walkways | Wooden planks over the water, shadowed alcoves (hiding spots), rubble | P1 |
| **The hills above Mérida** | Night hills, the burning city behind | Rock with a niche + sealing stone, shepherd's hut with a door, brambles | P1 |
| **The road west and the camp** | Night road | Campfire, sleeping soldiers, tent (hiding spot) | P1 |
| **The coast and the ship** | Shore, sea at night, burning villages along the coast | Dock, merchant ship (hull, deck, mast), crates, the **three-headed fire** rising | P1 |

### Act 0: The Great St Bernard pass, 757 (`Act0_Pass` scene)
| Place | Tiles / backdrop | Props | Priority |
|---|---|---|---|
| **The pass (trail)** | Snow slopes, rock, low cloud | Salt sacks, the **avalanche** (a big white wave sprite, or a particle effect) | P1 |
| **Sisnando's shelter** | Stone hut interior and exterior, roof you can walk on | Hearth (save point), bed, table with dice, **broken roof patches** (3, shown until fixed), the shed under the cliff with a lock | P1 |
| **The morning after** | Same shelter, fire cold, door open | **Three lines of footprints** in perfect step, the letter, half map and silver collar on the table | P1 |
| **The storm** | Whiteout, wind, heavy snow | Snow mounds, the buried pilgrim's mound | P1 |
| **The icy ledge** | Ice floor over a drop, the arena gate | Malaspina's picks (on the ground after the duel) | P1 |

Seasons are shown by title cards (Spring, Summer, Autumn, Winter). Seasonal variants of the shelter backdrop are P3.

## 7. World objects (P1 unless noted)

| Object | Size (units) | Notes |
|---|---|---|
| Save point: **the hearth** | 1.6 × 0.7 base | States `Idle` (embers) and `Active` (fire, loop) |
| **Checkpoint marker** | — | Checkpoints are invisible; optional small cairn or shrine (P3) |
| **Hiding spot** (shadow, alcove, tent, crates) | Varies | Darker area drawn over the player. A soft dark gradient sprite works everywhere |
| **Water** | Tileable | Surface strip (animated, P2) + translucent body |
| **Lock** (the shed lock) | ~0.4 × 0.5 | Small padlock/hasp on the door |
| Gate / door (sliding) | 1 × 3–5 | The shed door, the brambles, the snowdrift and the ledge gate all use this |
| Spikes / icy drop | 1 × 0.6 per tile | Tileable |
| One-way platform (planks, steps) | 1 × 0.35 per tile | Left end, middle, right end |
| **Throwable stone** | 0.25 | Small rock (it arcs and clatters) |
| **Health upgrade** (in the shed) | 0.6 | Something fitting the period: a saint's medal, a flask |
| Key items (icons only, 256 × 256) | — | `relic_snow` (crystal vial of meltwater), `relic_bone`, `relic_dove`, `half_map`, `sisnando_letter`, `silver_collar`, `malaspina_picks` |
| Currency (coins dropped by enemies) | 0.3 | Period silver coins (dirham / tremissis), 6-frame spin |

## 8. Visual effects

Folder: `Assets/Alma/Art/VFX/`. Each effect is a prefab assigned in
`Assets/Alma/Resources/EffectsLibrary.asset`. Easiest way: a sprite flipbook. Add the
`SimpleSpriteAnimator` component, drag in the frames, and save as a prefab.

### New for The Last Lock
| ID | What it is | Frames | Priority |
|---|---|---|---|
| `fx_parry_spark` | Bright clash of steel at the blades on a successful parry | 4–5 | P1 |
| `fx_roll_dust` | Snow or dust kicked up by a dodge roll | 4–5 | P1 |
| `fx_splash` | Water splash (falling in, swimming, a Drowned One rising) | 5–6 | P1 |
| `fx_stone_impact` | Thrown stone hitting the ground (small chips and dust) | 3–4 | P2 |
| `fx_dread_phantom` | A false figure at the edge of vision at high Dread: a dark, smoky human shape that fades | 6–8 | P2 |
| `fx_barro_puff` | Snow puff where Barro digs or lands | 4 | P2 |

### From the base systems (still used)
| ID | What it is | Priority |
|---|---|---|
| `fx_slash` | Sword arc in front of Corvin (drawn facing right) | P1 |
| `fx_hit_spark` | Impact burst when a hit lands | P1 |
| `fx_player_hurt` | Burst when Corvin is hit | P1 |
| `fx_enemy_death` | Enemy falling or dissolving | P1 |
| `fx_dust_jump`, `fx_dust_land` | Snow/dust at the feet | P2 |
| `fx_heal_charge`, `fx_heal` | While healing / when the heal completes | P2 |
| `fx_save_station` | Warm glow rising from the hearth | P2 |
| `fx_pickup_sparkle` | Collecting an item | P2 |
| `fx_slash_vertical`, `fx_dust_wall`, `fx_double_jump`, `fx_dash`, `fx_projectile_hit`, `fx_boss_slam`, `fx_breakable_debris`, `fx_ability_get`, `fx_travel`, `fx_bounce` | Sandbox demo and later abilities | P3 |

**Snowfall** is a particle system the scene builder already makes. It only needs a soft round
snowflake sprite if you want one (P3).

## 9. UI art

Assign in `Assets/Alma/Resources/UISkin.asset`. Everything is optional: empty slots draw flat colors.

| Item | Spec | Priority |
|---|---|---|
| **Font** (.ttf/.otf) | Readable at small sizes, slightly old-world (e.g. an uncial-inspired display font for titles plus a plain serif for text; check the license) | P1 |
| **Title logo** "The Last Lock" | ~2000 × 700, transparent | P1 |
| **Title screen background** | 1920 × 1080 or 3840 × 2160: snow on a burning city, or the shelter in the pass | P1 |
| Health full / empty icons | 128 × 128 each | P1 |
| **Dread vignette** (`dreadVignette`) | 1920 × 1080, transparent center, dark smoky edges. Fades in as Dread rises | P1 |
| **Dread bar** (`dreadFrame`, `dreadFill`) | ~640 × 24 | P2 |
| Energy bar (`energyFrame`, `energyFill`) | ~640 × 24 (heal charge) | P2 |
| Currency icon | 128 × 128 | P2 |
| Panel (dialogue box, menus) | 9-slice, ~256 × 256 (parchment or dark wood) | P2 |
| Button | 9-slice, ~512 × 128 | P2 |
| Boss health bar | Frame + fill, ~1200 × 40 | P2 |
| Item slot | 9-slice, ~160 × 160 | P2 |
| **Lockpick screen** | Currently drawn in flat colors (track, gold "sweet spot", moving pick, pin icons). A lock-face background and a pick sprite need a small code change; ask me when you have them | P3 |
| **Alert markers** over guards ("?" and "!") | Drawn as text now; 64 × 64 icons need a small code change | P3 |
| Map markers (player, save, travel) | 64 × 64 each | P3 |
| Button prompt icons (keyboard, Xbox, PlayStation) | 64 × 64 each; prompts are text for now | P3 |

## 10. Sound effects

Assign in `Assets/Alma/Resources/AudioLibrary.asset` (every ID is already listed there; run
*Alma > Setup > Create or Update Data Assets* if one is missing). Each entry can take **several
clips** and picks one at random, so give footsteps, hits and barks 3–4 variations.

**Format:** WAV, 44.1 or 48 kHz, 16-bit, mono is fine. Trim the silence at the start.
**Sources:** AI sound generators (e.g. ElevenLabs Sound Effects), freesound.org (check each
license; CC0 is easiest), Kenney.nl audio packs (CC0), the free Sonniss GDC bundles.

### Corvin and Sisnando (P1)
| ID | Sound |
|---|---|
| `player_footstep` | Steps on stone and snow (4 variations; mix both) |
| `player_jump` / `player_land` | Cloth and breath / soft thud in snow |
| `player_attack` | Sword swing whoosh (3) |
| `player_attack_hit` | Blade hitting cloth and flesh (3) |
| `player_hurt` | Impact plus grunt (2) |
| `player_death` | Heavy hit, then a fading tone |
| `player_roll` | Body tumbling over snow/stone |
| `player_parry` | Blade raised, quick metallic shink |
| `player_parry_success` | Big ringing steel clash: the most satisfying sound in the game |
| `player_riposte` | Heavy counter-thrust after a parry |
| `player_heal_charge` / `player_heal` | Uncorking and drinking / a soft relieved breath |
| `stone_throw` | Small whoosh |
| `stone_impact` | Stone clattering on stone (this is what guards hear) |
| `splash` | Falling into water / swimming strokes |
| `caught` | Sting when a guard seizes the defenseless Sisnando |

### Barro (P1)
| ID | Sound |
|---|---|
| `barro_bark` | Deep, big-dog bark (3) |
| `barro_growl` | Low warning growl (at wolves hidden in the snow) |
| `barro_whine` | Hurt whine |
| `barro_howl` | Long mournful howl (the tracks scene) |
| `barro_bite` | Snarl and snap |
| `barro_sniff` | Sniffing, snuffling in snow |

### Enemies (P1)
| ID | Sound |
|---|---|
| `enemy_windup` | Soft tell before a parryable strike (steel scrape, intake of breath) |
| `enemy_unblockable` | A sharper, distinct warning before an attack you must roll away from |
| `enemy_swing` | Weapon swing |
| `enemy_hurt` / `enemy_death` | Grunt (3) / fall |
| `enemy_alert` | Generic alert (sandbox enemies) |
| `guard_suspicious` | "Hm? Who's there?" murmur |
| `guard_alert` | Shout: "There!" / "Halt!" |
| `drowned_rise` | Water surging, a wet gasp |
| `wolf_howl` | A pale wolf spotting you |
| `boss_hurt`, `boss_death` | Malaspina grunting / yielding |
| `jingle_victory` | Short sting after the duel |

### World and story (P1 / P2)
| ID | Sound |
|---|---|
| `lockpick_pin` | Pin setting, a crisp click |
| `lockpick_slip` | Pick slipping, a scrape |
| `lockpick_break` | Pick snapping (guards nearby can hear it) |
| `lock_open` | Lock giving way, a heavy clunk |
| `knock` | One knock from inside stone (played three times in the hills; make it wrong-sounding) |
| `avalanche` | Rumble growing into a roar |
| `dig` | Digging through snow |
| `roof_hammer` | Hammer on wood/slate |
| `dice_roll` | Bone dice on a wooden table |
| `door_creak` | Old wooden door |
| `dread_sting` | Sudden dissonant sting when Dread spikes |
| `gate_open` / `gate_close` | Heavy door or stone grind (also the niche being sealed) |
| `save_station` | Fire crackling up when you rest at the hearth |
| `pickup_item` | Item get (relics, letter, picks) |
| `pickup_health_upgrade` | Warm chime |
| `pickup_currency` | Coin (very short; it plays a lot) |

### UI (P2)
`ui_move`, `ui_confirm`, `ui_back`, `ui_pause`, `ui_map_open`, `ui_dialogue_blip` (tiny blip while
text types out, ~50 ms), `ui_error`.

### Sandbox demo only (P3)
`player_double_jump`, `player_wall_jump`, `player_dash`, `enemy_shoot`, `projectile_hit`,
`boss_roar`, `boss_charge`, `boss_slam`, `pickup_ability`, `pickup_energy_upgrade`, `shop_buy`,
`travel_activate`, `travel_depart`, `platform_crumble`, `bounce_pad`, `lever`, `breakable_hit`,
`breakable_break`, `door_locked`.

## 11. Music and ambience

Also in `AudioLibrary.asset`, under *Music*. **Format:** OGG Vorbis or WAV. Music **must loop
seamlessly**; 1.5–3 minutes each. Ambience loops can be 30–60 s.
**Sources:** a composer, royalty-free libraries, or AI music generators (check that your plan
allows commercial use). Period touch: Mozarabic/Visigothic chant, frame drums, oud, vielle, low drones.

| ID | Where | Mood | Priority |
|---|---|---|---|
| `music_prologue_hymn` | Cold open in the basilica | **A girl's voice singing a Latin hymn**, alone, reverberant (the story's first sound) | P1 |
| `music_merida_escape` | Aqueduct, hills, camp | Tense, sparse, stealthy | P1 |
| `music_pass` | The trail, storm, ledge | Cold, wide, lonely mountain theme | P1 |
| `music_hermitage` | Sisnando's shelter, the seasons | Warm, gentle, a little sad (the heart of Act 0) | P1 |
| `music_duel` | Malaspina fight | Driving, rhythmic, a duel rather than a monster fight | P1 |
| `music_title` | Title screen | Main theme | P1 |
| `music_credits` | Credits | Main theme, resolved | P3 |
| `amb_siege` | Basilica | Distant drums, fire, shouting, bells | P1 |
| `amb_aqueduct` | Channels | Dripping, echoing water | P1 |
| `amb_shore` | Coast and ship | Waves, creaking ship, distant fire | P1 |
| `amb_pass_wind` | Alps outdoors | Howling wind (strong enough for the storm) | P1 |
| `amb_hermitage_fire` | Inside the shelter | Crackling hearth, wind outside | P1 |
| `amb_whispers` | Plays louder as Dread rises | Indistinct whispering in several voices | P1 |
| `music_area1`, `music_area2`, `music_boss`, `amb_area1`, `amb_area2` | Sandbox demo | | P3 |

## 12. Cinematics

Story scenes are built **in-engine** right now: title cards, fades, dialogue, characters walking,
camera shakes, all driven by `StorySequence` steps in the scene. Any of them can also play a
**pre-rendered video** (MP4 H.264, 1920 × 1080, 24–30 fps) or a **Unity Timeline** instead: add a
`Video` or `Timeline` step to that sequence.

| Scene (storyboard) | Sequence in the scene | Now | Could become |
|---|---|---|---|
| Cold open: hymn, snow on a burning city, *Emerita Augusta, 713* | `pro_seq_opening` | Title card on black | A 10–20 s establishing video of burning Mérida (P2) |
| **Scene 1:** the splitting of the relic | `pro_seq_opening` | Two figures + dialogue | Close-up of Ervigio's hands prying the reliquary apart (P2) |
| The parting and the three knocks | `pro_seq_parting` | Actors walk, knock sounds | Timeline with the niche sealing (P3) |
| Hiding the vial (a closed door) | `pro_seq_hide` | Fade out, door creak, Sisnando beside the dog | Keep it hidden: that's the point |
| The capture, the night in the camp | `pro_seq_capture` | Dialogue and a title card | Still images of the soldier giving water, Gundemar led away (P2) |
| **Scene 2:** the ship, the three-headed fire | `pro_seq_ship` | Fires appear, fade to white | A short video of the fire splitting in three (P1 if you make one video) |
| *Great St Bernard Pass, 757* | `a0_seq_start` | Title card | Establishing shot of the pass (P3) |
| The avalanche | `a0_seq_avalanche` | White wave sprite, shake, fade | Short video or Timeline (P2) |
| **Scene 3:** the dog's face | `a0_seq_avalanche` (after the dig) | Digging minigame, then dialogue | First-person image: snow breaks, a huge muzzle (P1: a single painted still works) |
| Seasons montage | `a0_seq_summer`, `a0_seq_autumn`, ... | Title cards | Painted season cards (P2) |
| **Scene 4:** the confession by the fire | `a0_seq_winter` | Dialogue | A painted still: firelight, Barro across their feet, the collar in Sisnando's hands (P2) |
| **Scene 5:** vanished, the tracks | `a0_seq_winter` end + `a0_letter` | Footprints, Barro howls, the letter | A still of the dawn, door swinging, three lines of tracks (P2) |
| Malaspina arrives / the outro | `a0_seq_malaspina`, `a0_seq_outro` | Dialogue, title cards, end of demo | Corvin and Barro leaving down the pass (P3) |

Also: **storyboard panels** (for you, not imported). Painted stills make a great middle ground:
put one on screen with a `SetActive` step behind the dialogue.

## 13. Voice and writing

- **The hymn** (`music_prologue_hymn`) is the only voice the slice really needs.
- **Placeholder dialogue to replace:** the storyboard scripts Scenes 1–5, and those lines are
  used as written. These beats had no script, so they have short placeholder lines in
  `Editor/GreyboxDemoBuilder.LastLock.cs`, which you can edit in the scene or in that file:
  the parting in the hills, the capture and the camp, the sailor's line before Scene 2, Corvin's
  opening line, the dice game (Pio is mentioned once), the shed request, the autumn pilgrim
  request, the pilgrim's thanks, and all of Malaspina's lines.
- **Item names and descriptions:** `Assets/Alma/Resources/ItemDatabase.asset` (the demo builder fills in the
  relics, map, letter, collar and picks).
- **Tutorial hints:** the text in each `Hint` step and `TutorialHint` area. `{Sneak}`, `{Throw}`,
  `{BarroTrack}` and the like are replaced by the player's current key or button.
- **Credits:** `Assets/Alma/Text/Credits.txt`.

---

## Import checklist (once files exist)

| Asset | In Unity |
|---|---|
| Sprites | Texture Type *Sprite (2D and UI)*; set PPU (128 or 32); set filter and compression per section 0; sheets: Sprite Mode *Multiple*, then slice in the Sprite Editor; pivot *Bottom Center* for characters |
| Character animation | Select the frames, drag them into the scene to create a clip, then drag the clip into the matching state of the existing Animator Controller (`Assets/Alma/Art/Animators/`). The controller sits on the character's `Visual` child: replace the grey `Body` sprite there (and hide the placeholder `Eye`) |
| Effects | Prefab with `SimpleSpriteAnimator` → assign in `Resources/EffectsLibrary` |
| SFX | Load Type *Decompress On Load* → assign in `Resources/AudioLibrary` |
| Music / ambience | Load Type *Streaming*, compression *Vorbis* → assign in `Resources/AudioLibrary` |
| UI art | Sprite; set the 9-slice borders in the Sprite Editor → assign in `Resources/UISkin` |
| Video | Drop the .mp4 into `Assets/Alma/Cinematics/` → add a `Video` step to the scene's story sequence |

**Big files:** use Git LFS for PNG/WAV/MP4 (`git lfs install`, then `git lfs track "*.png" "*.wav" "*.ogg" "*.mp4"`) so the repository stays small.

---

## Appendix: sandbox demo (Area1 / Area2), P3

The original system-test levels are still built by *Alma > Build Greybox Demo*. Their art is
only needed if you keep them: the **Crawler** (`Enemy_Walker`), **Wisp** (`Enemy_Flyer`) and
**Spitter** (`Enemy_Shooter`) enemies, the **Brute** boss (`Boss_Brute`: Idle, Wake, Move,
Telegraph, Charge, Leap, Attack, Stunned, Hurt, Death), the merchant and wanderer NPCs, the lever,
breakable wall, moving / crumbling platforms, bounce pad, fast travel shrine, ability orbs, and the
`music_area1`/`music_area2`/`music_boss` tracks.
