using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alma.EditorTools
{
    /// <summary>
    /// The Last Lock vertical slice: greybox Prologue (Mérida, 713, young Sisnando) and
    /// Act 0 (the Great St Bernard pass, 757, Corvin and Barro), following docs/THE_LAST_LOCK.md.
    /// Dialogue from the storyboard is used as written; beats the storyboard doesn't script
    /// (the parting, the capture, the dice game, Malaspina) have short placeholder lines.
    /// </summary>
    public static partial class GreyboxDemoBuilder
    {
        public const string PrologueScene = "Prologue_Merida";
        public const string Act0Scene = "Act0_Pass";
        const string CharacterFolder = Root + "/Data/Characters";

        static CharacterProfile corvinProfile;
        static CharacterProfile sisnandoProfile;
        static List<FlagActivator.Rule> storyRules;

        static readonly Color Snow = new Color(0.86f, 0.89f, 0.93f);
        static readonly Color Stone = new Color(0.42f, 0.4f, 0.42f);
        static readonly Color Wood = new Color(0.45f, 0.32f, 0.22f);

        // ================================================================ Prefabs

        static void CreateLastLockPrefabs()
        {
            AlmaProjectSetup.EnsureFolder(CharacterFolder);
            corvinProfile = AlmaProjectSetup.LoadOrCreate<CharacterProfile>(CharacterFolder + "/Corvin.asset");
            corvinProfile.characterId = "corvin";
            corvinProfile.displayName = "Corvin";
            corvinProfile.animator = controllers["Player"];
            corvinProfile.placeholderColor = new Color(0.95f, 0.8f, 0.5f);
            corvinProfile.canAttack = corvinProfile.canParry = corvinProfile.canDodge = corvinProfile.canHeal = true;
            corvinProfile.canPickLocks = corvinProfile.canThrowStones = corvinProfile.hasCompanion = true;
            corvinProfile.speedMultiplier = 1f;
            EditorUtility.SetDirty(corvinProfile);

            sisnandoProfile = AlmaProjectSetup.LoadOrCreate<CharacterProfile>(CharacterFolder + "/YoungSisnando.asset");
            sisnandoProfile.characterId = "young_sisnando";
            sisnandoProfile.displayName = "Sisnando";
            sisnandoProfile.animator = controllers["YoungSisnando"];
            sisnandoProfile.placeholderColor = new Color(0.78f, 0.82f, 0.95f);
            sisnandoProfile.canAttack = sisnandoProfile.canParry = sisnandoProfile.canDodge = false;
            sisnandoProfile.canHeal = sisnandoProfile.canPickLocks = sisnandoProfile.hasCompanion = false;
            sisnandoProfile.canThrowStones = true;
            sisnandoProfile.speedMultiplier = 0.95f;
            EditorUtility.SetDirty(sisnandoProfile);

            // Barro lives in Resources so the GameManager can spawn him anywhere.
            var barro = CompanionFactory.CreatePlaceholderBarro(square);
            barro.GetComponentInChildren<Animator>().runtimeAnimatorController = controllers["Barro"];
            prefabs["Barro"] = SavePrefab(barro, AlmaProjectSetup.ResourcesFolder + "/Barro.prefab");

            prefabs["Guard"] = SavePrefab(MakeGuard(), PrefabFolder + "/Enemies/Guard.prefab");
            prefabs["Drowned"] = SavePrefab(MakeDrowned(), PrefabFolder + "/Enemies/DrownedOne.prefab");
            prefabs["Wolf"] = SavePrefab(MakeWolf(), PrefabFolder + "/Enemies/PaleWolf.prefab");
            prefabs["Malaspina"] = SavePrefab(MakeMalaspina(), PrefabFolder + "/Enemies/Malaspina.prefab");
        }

        static void AddLastLockItems(ItemDatabase items)
        {
            AddItem(items, "relic_bone", "The Bone", "A finger bone of Saint Eulalia in a silver case. It shows the last moments of the dead.");
            AddItem(items, "relic_dove", "The Dove", "A silver dove that sings when evil is near.");
            AddItem(items, "relic_snow", "The Snow", "A crystal vial of meltwater that never warms. It stills.");
            AddItem(items, "half_map", "Half a Map", "Sisnando's map, torn in two. It points to Mérida.");
            AddItem(items, "sisnando_letter", "Sisnando's Letter", "\"Do not come after me. Go after her.\"");
            AddItem(items, "silver_collar", "Silver Collar", "The collar Sisnando always polished. Now it's Barro's.");
            AddItem(items, "malaspina_picks", "Malaspina's Picks", "Fine steel picks. Locks too delicate before will now give, and picking is easier.");
        }

        static GameObject MakeGuard()
        {
            var go = MakeEnemyBase("Guard", new Vector2(0.9f, 1.8f), true, new Color(0.55f, 0.45f, 0.35f), square, "Guard", 4);
            Object.DestroyImmediate(go.GetComponent<ContactDamage>());
            var sword = new GameObject("Sword");
            sword.transform.SetParent(go.transform.Find("Visual"), false);
            sword.transform.localPosition = new Vector3(0.55f, 0.9f, 0f);
            sword.transform.localScale = new Vector3(0.12f, 0.9f, 1f);
            var sr = sword.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.8f, 0.8f, 0.85f);
            sr.sortingOrder = 12;
            go.AddComponent<StealthPerception>();
            var guard = go.AddComponent<GuardEnemy>();
            guard.displayName = "Soldier";
            guard.currencyPrefab = prefabs["Coin"].GetComponent<CurrencyPickup>();
            return go;
        }

        static GameObject MakeDrowned()
        {
            var go = MakeEnemyBase("DrownedOne", new Vector2(1f, 1.4f), true, new Color(0.45f, 0.56f, 0.52f), square, "Drowned", 3);
            Object.DestroyImmediate(go.GetComponent<ContactDamage>());
            var perception = go.AddComponent<StealthPerception>();
            perception.viewDistance = 0.5f; // they don't see you; they hear you
            perception.closeRange = 0.1f;
            perception.hearingMultiplier = 1.2f;
            go.AddComponent<DrownedOne>().displayName = "Drowned One";
            var dread = go.AddComponent<DreadSource>();
            dread.radius = 5f;
            dread.ratePerSecond = 4f;
            return go;
        }

        static GameObject MakeWolf()
        {
            var go = MakeEnemyBase("PaleWolf", new Vector2(1.2f, 0.8f), true, new Color(0.92f, 0.94f, 0.97f), square, "Wolf", 2);
            var wolf = go.AddComponent<WalkerEnemy>();
            wolf.displayName = "Pale Wolf";
            wolf.patrolSpeed = 2.5f;
            wolf.chaseSpeed = 6f;
            wolf.sightRange = 7f;
            wolf.hurtSound = AudioIds.EnemyHurt;
            wolf.alertSound = AudioIds.WolfHowl;
            wolf.currencyPrefab = prefabs["Coin"].GetComponent<CurrencyPickup>();
            var hidden = go.AddComponent<HiddenThreat>();
            hidden.hiddenAlpha = 0.18f;
            return go;
        }

        static GameObject MakeMalaspina()
        {
            var go = MakeEnemyBase("Malaspina", new Vector2(0.95f, 1.85f), true, new Color(0.5f, 0.25f, 0.3f), square, "Malaspina", 14);
            Object.DestroyImmediate(go.GetComponent<ContactDamage>());
            var boss = go.AddComponent<DuelistBoss>();
            boss.displayName = "Malaspina, King of the Pass";
            boss.bossMusic = AudioIds.MusicDuel;
            boss.wakeDuration = 0.8f;
            boss.currencyPrefab = prefabs["Coin"].GetComponent<CurrencyPickup>();
            return go;
        }

        // ================================================================ Helpers

        static void Chapter(CharacterProfile profile, string title, bool companion, string music)
        {
            var chapter = new GameObject("Chapter Settings").AddComponent<ChapterSettings>();
            chapter.character = profile;
            chapter.chapterTitle = title;
            chapter.allowCompanion = companion;
            chapter.music = music;
            storyRules = new List<FlagActivator.Rule>();
        }

        static void FinishStoryState()
        {
            var state = new GameObject("Story State").AddComponent<FlagActivator>();
            state.rules = storyRules.ToArray();
        }

        /// <summary>Object only shown between two story flags (either may be empty).</summary>
        static void ShowBetween(GameObject target, string showAfter, string hideAfter)
        {
            storyRules.Add(new FlagActivator.Rule { target = target, showAfterFlag = showAfter ?? "", hideAfterFlag = hideAfter ?? "" });
        }

        static Transform Marker(string name, float x, float y)
        {
            var go = new GameObject("Marker - " + name);
            go.transform.SetParent(actorsRoot, false);
            go.transform.position = new Vector3(x, y, 0f);
            return go.transform;
        }

        static GameObject Deco(float x0, float y0, float x1, float y1, Color color, int order = -5, string name = "Deco")
        {
            var go = new GameObject(name);
            go.transform.SetParent(geometryRoot, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            go.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        /// <summary>A non-interactive character for cutscenes (feet at its position).</summary>
        static GameObject Actor(string name, Color color, Vector2 size, float x, float y, string controller)
        {
            var go = NewObject(name);
            go.transform.SetParent(actorsRoot, false);
            go.transform.position = new Vector3(x, y, 0f);
            var visual = AddVisual(go, square, color, size, new Vector2(0f, size.y * 0.5f), 8);
            AddEye(visual, new Vector2(size.x * 0.25f, size.y * 0.8f));
            AddAnimator(visual, controller); // Idle, and Move while a story step walks it
            return go;
        }

        static NPC Npc(string id, string npcName, Color color, float x, float y, DialogueLine[] lines, DialogueLine[] repeat,
            string controller = "NPC")
        {
            var go = NewObject(npcName);
            go.transform.SetParent(actorsRoot, false);
            go.transform.position = new Vector3(x, y, 0f);
            var visual = AddVisual(go, square, color, new Vector2(0.85f, 1.6f), new Vector2(0f, 0.8f), 8);
            AddEye(visual, new Vector2(0.2f, 1.3f));
            AddAnimator(visual, controller);
            var npc = go.AddComponent<NPC>();
            npc.visual = visual;
            npc.npcName = npcName;
            npc.lines = lines;
            npc.repeatLines = repeat;
            SetId(go, id);
            return npc;
        }

        static T AreaOf<T>(string name, float x0, float y0, float x1, float y1) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(geometryRoot, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0, y1 - y0);
            return go.AddComponent<T>();
        }

        static void Water(float x0, float y0, float x1, float y1)
        {
            AreaOf<WaterVolume>("Water", x0, y0, x1, y1);
            Deco(x0, y0, x1, y1, new Color(0.15f, 0.3f, 0.45f, 0.75f), 20, "Water (visual)");
        }

        static void Hide(float x0, float y0, float x1, float y1)
        {
            AreaOf<HidingSpot>("Hiding Spot", x0, y0, x1, y1);
            Deco(x0, y0, x1, y1, new Color(0.02f, 0.02f, 0.03f, 0.75f), 15, "Shadow");
        }

        static void Dread(float x0, float y0, float x1, float y1, float rate)
        {
            AreaOf<DreadZone>(rate >= 0f ? "Dread Zone" : "Calm Zone", x0, y0, x1, y1).ratePerSecond = rate;
        }

        static void HintArea(string id, float x0, float y0, float x1, float y1, string text)
        {
            var hint = AreaOf<TutorialHint>("Hint - " + id, x0, y0, x1, y1);
            hint.text = text;
            SetId(hint.gameObject, id);
        }

        static void CheckpointAt(string id, float spawnX, float spawnY, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject("Checkpoint - " + id);
            go.transform.SetParent(geometryRoot, false);
            go.transform.position = new Vector3(spawnX, spawnY, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0, y1 - y0);
            box.offset = new Vector2((x0 + x1) * 0.5f - spawnX, (y0 + y1) * 0.5f - spawnY);
            go.AddComponent<Checkpoint>();
            SetId(go, id);
        }

        static void SpawnAt(string id, float x, float y, int face = 1)
        {
            var spawn = new GameObject("Spawn - " + id).AddComponent<SpawnPoint>();
            spawn.spawnId = id;
            spawn.faceDirection = face;
            spawn.transform.position = new Vector3(x, y, 0f);
        }

        static void Snowfall(float x0, float x1, float top, float fallHeight, float rate, float speed)
        {
            var go = new GameObject("Snowfall");
            go.transform.position = new Vector3((x0 + x1) * 0.5f, top, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startSpeed = 0f;
            main.startLifetime = fallHeight / Mathf.Max(0.1f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(x1 - x0, 0.2f, 0.1f);
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            velocity.y = new ParticleSystem.MinMaxCurve(-speed * 1.2f, -speed * 0.8f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 35;
            var material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (material != null) renderer.sharedMaterial = material;
        }

        static StorySequence Sequence(string id, SequenceTrigger trigger, Rect area, params SequenceStep[] steps)
        {
            var go = new GameObject("Story - " + id);
            go.transform.SetParent(roomsRoot, false);
            go.transform.position = new Vector3(area.center.x, area.center.y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(Mathf.Max(0.1f, area.width), Mathf.Max(0.1f, area.height));
            var seq = go.AddComponent<StorySequence>();
            seq.trigger = trigger;
            seq.area = box;
            seq.steps = steps;
            SetId(go, id);
            return seq;
        }

        static Rect Area(float x0, float y0, float x1, float y1) => Rect.MinMaxRect(x0, y0, x1, y1);

        static StoryInteractable InteractSpot(string id, float x, float y, string prompt, StorySequence sequence,
            float hold, string setsFlag, string requiredFlag)
        {
            var go = new GameObject("Interact - " + id);
            go.transform.SetParent(actorsRoot, false);
            go.transform.position = new Vector3(x, y, 0f);
            var spot = go.AddComponent<StoryInteractable>();
            spot.prompt = prompt;
            spot.sequence = sequence;
            spot.holdDuration = hold;
            spot.setsFlag = setsFlag;
            spot.requiredFlag = requiredFlag;
            spot.promptOffset = new Vector2(0f, 2f);
            return spot;
        }

        // ------------------------------------------------------------- Steps

        static DialogueLine L(string speaker, string text) => Line(speaker, text);

        static SequenceStep Dlg(params DialogueLine[] lines) => new SequenceStep { type = SequenceStepType.Dialogue, lines = lines };
        static SequenceStep Card(string title, string subtitle, float seconds) => new SequenceStep { type = SequenceStepType.TitleCard, text = title, subtitle = subtitle, duration = seconds };
        static SequenceStep FadeOut(float seconds) => new SequenceStep { type = SequenceStepType.FadeOut, duration = seconds, color = Color.black };
        static SequenceStep FadeOutWhite(float seconds) => new SequenceStep { type = SequenceStepType.FadeOut, duration = seconds, color = Color.white };
        static SequenceStep FadeIn(float seconds) => new SequenceStep { type = SequenceStepType.FadeIn, duration = seconds };
        static SequenceStep Wait(float seconds) => new SequenceStep { type = SequenceStepType.Wait, duration = seconds };
        static SequenceStep Sound(string id) => new SequenceStep { type = SequenceStepType.PlaySound, id = id };
        static SequenceStep Music(string id, float fade = 1.5f) => new SequenceStep { type = SequenceStepType.PlayMusic, id = id, duration = fade };
        static SequenceStep Silence(float fade) => new SequenceStep { type = SequenceStepType.StopMusic, duration = fade };
        static SequenceStep Ambience(string id) => new SequenceStep { type = SequenceStepType.PlayAmbience, id = id };
        static SequenceStep Show(GameObject target, bool visible) => new SequenceStep { type = SequenceStepType.SetActive, target = target, active = visible };
        static SequenceStep Move(Transform actor, Transform to, float speed, bool wait = true) => new SequenceStep { type = SequenceStepType.MoveActor, actor = actor, destination = to, speed = speed, wait = wait };
        static SequenceStep Teleport(Transform to) => new SequenceStep { type = SequenceStepType.TeleportPlayer, destination = to };
        static SequenceStep Flag(string id) => new SequenceStep { type = SequenceStepType.SetFlag, id = id };
        static SequenceStep Give(string itemId) => new SequenceStep { type = SequenceStepType.GiveItem, id = itemId };
        static SequenceStep Shake(float amount, float seconds) => new SequenceStep { type = SequenceStepType.CameraShake, amount = amount, duration = seconds };
        static SequenceStep Mash(string text, int presses, string sound) => new SequenceStep { type = SequenceStepType.Mash, text = text, count = presses, id = sound };
        static SequenceStep Tip(string text) => new SequenceStep { type = SequenceStepType.Hint, text = text };
        static SequenceStep JoinBarro() => new SequenceStep { type = SequenceStepType.CompanionJoin };
        static SequenceStep BarroWalk(Transform to) => new SequenceStep { type = SequenceStepType.CompanionWalk, destination = to, wait = true };
        static SequenceStep BarroRelease() => new SequenceStep { type = SequenceStepType.CompanionWalk, destination = null };
        static SequenceStep BarroAnim(string state, float seconds, string sound) => new SequenceStep { type = SequenceStepType.CompanionAnimation, text = state, duration = seconds, id = sound };
        static SequenceStep Fight(GameObject arena) => new SequenceStep { type = SequenceStepType.StartBossFight, target = arena };
        static SequenceStep Load(string scene, string spawn) => new SequenceStep { type = SequenceStepType.LoadScene, id = scene, spawnId = spawn };
        static SequenceStep EndOfDemo() => new SequenceStep { type = SequenceStepType.EndDemo };

        // ================================================================ Prologue

        static void BuildPrologue()
        {
            BeginScene(new Color(0.08f, 0.03f, 0.04f));
            Chapter(sisnandoProfile, "Prologue: The Siege Ends", false, "");
            Background(-20f, 305f, new Color(0.18f, 0.07f, 0.06f));
            var ash = new Color(0.25f, 0.2f, 0.22f);
            var soldier = new Color(0.55f, 0.45f, 0.35f);

            // ---- Basilica of Santa Eulalia (x -14..30) ----
            RoomArea("pro_room_basilica", "Basilica of Santa Eulalia", -14f, -8f, 30f, 16f, "", AudioIds.AmbienceSiege);
            Wall(-16f, -8f, -14f, 16f);
            Block(-14f, -8f, 30f, 0f, ash);
            Wall(-14f, 14f, 30f, 16f);
            Wall(28f, 3f, 30f, 14f); // doorway to the crypt below 3
            Deco(3.5f, 0f, 8.5f, 1.1f, new Color(0.6f, 0.5f, 0.3f), -2, "Altar");
            foreach (float px in new[] { -7f, 14f, 22f }) Deco(px, 0f, px + 1f, 14f, new Color(0.3f, 0.25f, 0.26f), -8, "Pillar");
            foreach (float cx in new[] { 3.2f, 8.6f }) Deco(cx, 1.1f, cx + 0.15f, 1.6f, new Color(1f, 0.85f, 0.4f), -1, "Candle");
            Snowfall(-10f, 26f, 13.5f, 13.5f, 14f, 0.6f);

            var ervigio = Actor("Ervigio", new Color(0.55f, 0.45f, 0.62f), new Vector2(0.85f, 1.75f), 4.8f, 0f, "Ervigio");
            var gundemar = Actor("Gundemar", new Color(0.45f, 0.52f, 0.4f), new Vector2(0.8f, 1.7f), 8f, 0f, "Gundemar");
            ShowBetween(ervigio, "", "pro_left_basilica");
            ShowBetween(gundemar, "", "pro_left_basilica");
            var crypt = Marker("Crypt door", 31f, 0f);

            SpawnAt("prologue_start", 1f, 0f);
            CheckpointAt("pro_cp_start", 1f, 0f, -6f, -1f, 6f, 4f);

            Sequence("pro_seq_opening", SequenceTrigger.SceneStart, Area(0f, 0f, 0.1f, 0.1f),
                FadeOut(0f), Music(AudioIds.MusicPrologueHymn, 2f), Ambience(AudioIds.AmbienceSiege), Wait(1.2f),
                Card("Emerita Augusta", "Year of Our Lord 713", 4.5f),
                FadeIn(2.5f),
                Dlg(L("Ervigio", "One piece for each of us. Apart, they are only a bone, a bird and water. Together, they are the last lock."),
                    L("Gundemar", "And if they take us?"),
                    L("Ervigio", "Then you forget where you put it. Forget it so well that pain cannot find it."),
                    L("Sisnando", "Father, the snow... it's falling indoors."),
                    L("Ervigio", "She knows. Go.")),
                Move(ervigio.transform, crypt, 3.5f, false),
                Move(gundemar.transform, crypt, 3.5f),
                Flag("pro_left_basilica"),
                Tip("You carry the Snow. Follow the others out through the crypt, to the right. {Interact} uses things."));

            // ---- Aqueduct channels (x 30..120): stealth tutorial ----
            RoomArea("pro_room_aqueduct", "The aqueduct channels", 30f, -8f, 120f, 8f, AudioIds.MusicMeridaEscape, AudioIds.AmbienceAqueduct);
            Block(30f, -8f, 120f, -4f, Stone);
            Wall(30f, 6f, 120f, 8f);
            Block(30f, -4f, 46f, 0f, Stone);
            Block(56f, -4f, 62f, 0f, Stone);
            Block(74f, -4f, 120f, 0f, Stone);
            Water(46f, -4f, 56f, -0.4f);
            Water(62f, -4f, 74f, -0.4f);
            OneWay(46f, 56f, 0f);
            OneWay(62f, 74f, 0f);
            Spawn<DrownedOne>("Drowned", 51f, -2.6f);
            Spawn<DrownedOne>("Drowned", 68f, -2.6f);
            Dread(30f, -8f, 120f, 8f, 2.5f);
            CheckpointAt("pro_cp_aqueduct", 35f, 0f, 31f, -1f, 40f, 5f);
            HintArea("pro_hint_sneak", 38f, -1f, 46f, 5f, "The dead in the water hear footsteps. Hold {Sneak} to cross the planks quietly.");
            CheckpointAt("pro_cp_aqueduct2", 77f, 0f, 75f, -1f, 80f, 5f);
            HintArea("pro_hint_stone", 80f, -1f, 84f, 5f, "A patrol. Throw a stone ({Throw}) to draw him away, or wait in a shadow until he turns. You can't fight.");
            Hide(84f, 0f, 86.5f, 3f);
            Hide(108f, 0f, 110.5f, 3f);
            var patrol = Spawn<GuardEnemy>("Guard", 98f, 0f);
            patrol.patrolPoints = new[] { new Vector2(-6f, 0f), new Vector2(6f, 0f) };
            Record(patrol);

            // ---- The hills: the parting (x 120..176) ----
            RoomArea("pro_room_hills", "The hills above Mérida", 120f, -8f, 176f, 20f, AudioIds.MusicMeridaEscape, "");
            Block(120f, -8f, 176f, 0f, ash);
            Block(138f, 0f, 144f, 1f, ash);
            var ervigioHills = Actor("Ervigio (hills)", new Color(0.55f, 0.45f, 0.62f), new Vector2(0.85f, 1.75f), 128f, 0f, "Ervigio");
            var gundemarHills = Actor("Gundemar (hills)", new Color(0.45f, 0.52f, 0.4f), new Vector2(0.8f, 1.7f), 131f, 0f, "Gundemar");
            var ervigioRoad = Actor("Ervigio (road)", new Color(0.55f, 0.45f, 0.62f), new Vector2(0.85f, 1.75f), 146f, 0f, "Ervigio");
            ShowBetween(ervigioHills, "pro_left_basilica", "pro_parted");
            ShowBetween(gundemarHills, "pro_left_basilica", "pro_parted");
            ShowBetween(ervigioRoad, "pro_parted", "pro_captured");
            Deco(150f, 0f, 153f, 5f, new Color(0.35f, 0.3f, 0.3f), -3, "Rock");
            Deco(151f, 0.2f, 152f, 1.8f, new Color(0.05f, 0.04f, 0.05f), -2, "Niche");
            var sealedStone = Deco(151f, 0.2f, 152f, 1.8f, new Color(0.5f, 0.47f, 0.45f), -1, "Sealed stone");
            ShowBetween(sealedStone, "pro_niche_sealed", "");
            Deco(160f, 0f, 168f, 4.5f, new Color(0.35f, 0.27f, 0.2f), -3, "Hut");
            Deco(159.5f, 4.5f, 168.5f, 5.2f, new Color(0.25f, 0.18f, 0.12f), -2, "Hut roof");
            Deco(163.5f, 0f, 165f, 2.6f, Wood, -2, "Hut door");
            var strayDog = Actor("Stray dog", new Color(0.5f, 0.38f, 0.25f), new Vector2(0.9f, 0.6f), 166.6f, 0f, "StrayDog");
            ShowBetween(strayDog, "pro_vial_hidden", "");
            var niche = Marker("Niche", 151.5f, 0f);
            var ervigioWait = Marker("Ervigio waits", 146f, 0f);
            var gundemarAway = Marker("Gundemar leaves", 121f, 0f);
            var besideDog = Marker("Beside the dog", 165.6f, 0f);

            Sequence("pro_seq_parting", SequenceTrigger.PlayerEntersArea, Area(124f, 0f, 127f, 4f),
                Dlg(L("Ervigio", "Here we part. Each of you hides your own piece, alone."),
                    L("Ervigio", "Tell no one where. Not even each other."),
                    L("Gundemar", "Then this is goodbye, little brother."),
                    L("Sisnando", "God keep you, Gundemar.")),
                Move(ervigioHills.transform, niche, 2.5f),
                Sound(AudioIds.GateClose), Flag("pro_niche_sealed"),
                Move(ervigioHills.transform, ervigioWait, 2.5f),
                Wait(0.8f), Sound(AudioIds.Knock), Wait(0.6f), Sound(AudioIds.Knock), Wait(0.6f), Sound(AudioIds.Knock), Wait(1.8f),
                Move(gundemarHills.transform, gundemarAway, 3f),
                Flag("pro_parted"),
                Tip("Find a place for your piece, where no one would ever look."))
                .requiredFlag = "pro_left_basilica";

            var hideSequence = Sequence("pro_seq_hide", SequenceTrigger.Manual, Area(164f, 0f, 164.1f, 0.1f),
                FadeOut(1.2f), Sound(AudioIds.DoorCreak), Wait(1.5f), Teleport(besideDog), Wait(0.4f), FadeIn(1.2f), Wait(1.5f),
                Tip("Rejoin the abbot on the road west."));
            InteractSpot("pro_hut_door", 164.2f, 0f, "Hide the vial", hideSequence, 0f, "pro_vial_hidden", "pro_parted");
            var brambles = GateAt(172.5f, 0f, 4f, "pro_brambles");
            brambles.openWhenFlagSet = "pro_vial_hidden";
            brambles.rememberWhenOpened = false;

            // ---- The road west and the camp (x 176..236) ----
            RoomArea("pro_room_road", "The road west", 176f, -8f, 236f, 20f, "", "");
            Block(176f, -8f, 236f, 0f, ash);
            var squad = new GameObject("Ambush");
            squad.transform.SetParent(actorsRoot, false);
            foreach (float sx in new[] { 196f, 198f, 200.5f })
                Actor("Soldier", soldier, new Vector2(0.9f, 1.8f), sx, 0f, "Soldier").transform.SetParent(squad.transform, true);
            ShowBetween(squad, "pro_ambush", "pro_captured");
            var campStart = Marker("Camp", 210f, 0f);

            Sequence("pro_seq_capture", SequenceTrigger.PlayerEntersArea, Area(184f, 0f, 188f, 4f),
                Flag("pro_ambush"), Sound(AudioIds.GuardAlert), Silence(1f),
                Dlg(L("Soldier", "Halt! Priests, on the road by night?"),
                    L("Ervigio", "We are only old men going home."),
                    L("Soldier", "Then you'll go home in chains.")),
                Dlg(L("Soldier", "Here. Drink. You're no use to anyone dead."),
                    L("Sisnando", "...Thank you.")),
                FadeOut(1.5f), Card("That night", "", 3f), Flag("pro_captured"), Teleport(campStart),
                Dlg(L("Sisnando", "They took Gundemar away alone. He did not come back."),
                    L("Sisnando", "The abbot burned with fever until dawn. By morning he was gone.")),
                FadeIn(1.5f), Music(AudioIds.MusicMeridaEscape, 1f),
                Tip("Slip out of the camp before dawn. Stay out of the guards' sight, and hold {Sneak} near them."))
                .requiredFlag = "pro_vial_hidden";

            var camp = new GameObject("Camp");
            camp.transform.SetParent(actorsRoot, false);
            Deco(215f, 0f, 216f, 0.6f, new Color(1f, 0.55f, 0.2f), -1, "Campfire").transform.SetParent(camp.transform, true);
            Deco(212f, 0f, 214f, 0.4f, soldier, -1, "Sleeping soldier").transform.SetParent(camp.transform, true);
            Deco(217.5f, 0f, 219.5f, 0.4f, soldier, -1, "Sleeping soldier").transform.SetParent(camp.transform, true);
            ShowBetween(camp, "pro_captured", "");
            Hide(221f, 0f, 224f, 3f);
            Deco(220.5f, 0f, 224.5f, 3.2f, new Color(0.3f, 0.26f, 0.2f), -4, "Tent");
            var sentry = Spawn<GuardEnemy>("Guard", 226f, 0f);
            sentry.patrolPoints = new[] { Vector2.zero };
            Record(sentry);
            var walker = Spawn<GuardEnemy>("Guard", 230f, 0f);
            walker.patrolPoints = new[] { new Vector2(-3f, 0f), new Vector2(4f, 0f) };
            Record(walker);
            ShowBetween(sentry.gameObject, "pro_captured", "");
            ShowBetween(walker.gameObject, "pro_captured", "");
            CheckpointAt("pro_cp_camp", 209f, 0f, 205f, -1f, 212f, 5f);

            // ---- The coast and the ship (x 236..302) ----
            RoomArea("pro_room_coast", "The coast", 236f, -8f, 302f, 20f, "", AudioIds.AmbienceShore);
            Block(236f, -8f, 280f, 0f, new Color(0.6f, 0.55f, 0.45f));
            Deco(266f, -0.3f, 280f, 0f, Wood, 1, "Dock");
            Block(280f, -8f, 302f, -6f, Stone);
            Water(280f, -6f, 302f, -0.5f);
            Deco(282f, -2f, 300f, 1.5f, new Color(0.35f, 0.24f, 0.16f), -2, "Hull");
            Block(282f, 1.5f, 300f, 2.5f, Wood, "Deck");
            Deco(290.8f, 2.5f, 291.2f, 13f, Wood, -3, "Mast");
            Hide(256f, 0f, 259f, 2.5f);
            Deco(255.5f, 0f, 259.5f, 2f, new Color(0.45f, 0.35f, 0.22f), -2, "Crates");
            var dockGuard = Spawn<GuardEnemy>("Guard", 272f, 0f);
            dockGuard.patrolPoints = new[] { new Vector2(-4f, 0f), new Vector2(4f, 0f) };
            Record(dockGuard);
            CheckpointAt("pro_cp_coast", 240f, 0f, 237f, -1f, 244f, 5f);
            var sailor = Actor("Sailor", new Color(0.4f, 0.45f, 0.55f), new Vector2(0.9f, 1.75f), 295f, 2.5f, "Sailor");
            sailor.name = "Sailor";
            for (int i = 0; i < 6; i++)
            {
                float fx = 240f + i * 6f;
                Deco(fx, 3f, fx + 1.2f, 4.5f + (i % 3), new Color(1f, 0.45f, 0.15f, 0.8f), -15, "Village fire");
            }
            var threeFires = new GameObject("Three-headed fire");
            threeFires.transform.SetParent(geometryRoot, false);
            foreach (float tx in new[] { 268f, 270f, 272f })
                Deco(tx, 3f, tx + 1.3f, 11f + (tx == 270f ? 2f : 0f), new Color(1f, 0.35f, 0.1f, 0.85f), -14, "Tall flame").transform.SetParent(threeFires.transform, true);
            ShowBetween(threeFires, "pro_three_fires", "");

            Sequence("pro_seq_ship", SequenceTrigger.PlayerEntersArea, Area(285f, 2.5f, 300f, 8f),
                Silence(2f), Ambience(AudioIds.AmbienceShore),
                Dlg(L("Sailor", "Don't look, priest. Whatever burns there isn't yours anymore."),
                    L("Sisnando", "(whispering) It was always mine.")),
                Flag("pro_three_fires"), Shake(0.15f, 1.5f), Wait(2.5f),
                FadeOutWhite(3f), Wait(1f),
                Load(Act0Scene, "act0_start"));

            FinishStoryState();
            SaveScene(PrologueScene);
        }

        // ================================================================== Act 0

        static void BuildAct0()
        {
            BeginScene(new Color(0.55f, 0.6f, 0.68f));
            Chapter(corvinProfile, "Act 0: The Hospice in the Snow", true, AudioIds.MusicPass);
            Background(-10f, 225f, new Color(0.68f, 0.72f, 0.78f));
            var ice = new Color(0.72f, 0.85f, 0.95f);

            // ---- The pass (x -6..70) ----
            RoomArea("a0_room_trail", "Great St Bernard Pass", -6f, -6f, 70f, 24f, AudioIds.MusicPass, AudioIds.AmbiencePassWind);
            Wall(-8f, -6f, -6f, 24f);
            Block(-6f, -6f, 20f, 0f, Snow);
            Block(20f, -6f, 32f, 1.5f, Snow);
            Block(32f, -6f, 46f, 3f, Snow);
            Block(46f, -6f, 70f, 4.5f, Snow);
            Snowfall(-4f, 70f, 22f, 20f, 30f, 1.2f);
            SpawnAt("act0_start", 0f, 0f);
            CheckpointAt("a0_cp_start", 0f, 0f, -5f, -1f, 5f, 5f);

            var avalanche = Deco(-10f, 8f, 30f, 26f, Color.white, 30, "Avalanche");
            avalanche.SetActive(false);
            var avalancheEnd = Marker("Avalanche end", 56f, 12f);
            var bed = Marker("Bed", 94.5f, 0f);

            Sequence("a0_seq_start", SequenceTrigger.SceneStart, Area(0f, 0f, 0.1f, 0.1f),
                FadeOut(0f), Card("The Great St Bernard Pass", "Year of Our Lord 757", 3.5f), FadeIn(2f),
                Dlg(L("Corvin", "One more load of salt over the pass, and Malaspina's debt is paid.")),
                Tip("Move with WASD or the left stick. {Jump} to jump, {Sneak} (hold) to walk quietly."));

            Sequence("a0_seq_avalanche", SequenceTrigger.PlayerEntersArea, Area(52f, 4f, 56f, 10f),
                Sound(AudioIds.Avalanche), Shake(0.35f, 2.5f), Show(avalanche, true),
                Move(avalanche.transform, avalancheEnd, 18f, false), Wait(1.1f),
                Silence(0.5f), FadeOutWhite(0.5f), Wait(1.2f), FadeOut(0.4f), Show(avalanche, false),
                Mash("Buried in the snow! Press {key} to dig.", 14, AudioIds.Dig),
                Teleport(bed), JoinBarro(), Ambience(AudioIds.AmbienceHermitageFire), Music(AudioIds.MusicHermitage, 2f), FadeIn(1.5f),
                Dlg(L("Sisnando", "Barro, leave him some face, he'll need it for lying."),
                    L("Corvin", "(coughing) Who says I lie?"),
                    L("Sisnando", "Your boots. Those are a bishop's boots, my son, and you are no bishop.")),
                Dlg(L("Sisnando", "Barro dug you out and dragged you all the way here. You'll mend."),
                    L("Corvin", "What do I owe you?"),
                    L("Sisnando", "Nothing. Well... come back and fix my roof.")),
                Tip("Barro follows you now: {BarroTrack} track, {BarroGuard} stay / heel, {BarroFetch} fetch, {BarroAttack} attack."),
                Card("Spring", "", 2.5f), Flag("montage_roof"),
                Tip("Fix the roof: climb the steps outside the shelter and hold {Interact} on each broken spot."));

            // ---- Sisnando's shelter (x 70..124) ----
            RoomArea("a0_room_hermitage", "Sisnando's shelter", 70f, -6f, 124f, 24f, AudioIds.MusicHermitage, AudioIds.AmbienceHermitageFire);
            Block(70f, -6f, 124f, 0f, Snow);
            Snowfall(70f, 124f, 22f, 20f, 18f, 1f);
            Deco(82f, 0f, 108f, 6.2f, new Color(0.3f, 0.28f, 0.27f), -6, "Shelter (inside)");
            Block(82f, 0f, 83f, 6.2f, Stone, "Shelter wall");
            Block(108f, 3.2f, 109f, 6.2f, Stone, "Shelter wall");
            OneWay(82f, 109f, 6.2f);
            OneWay(109.5f, 111.5f, 2.2f);
            OneWay(110.5f, 112.5f, 4.2f);
            Dread(83f, 0f, 108f, 6f, -10f); // warm by the fire
            Spawn<SaveStation>("SaveStation", 86f, 0f, "a0_hearth").prompt = "Rest by the fire";
            Deco(93f, 0f, 96f, 0.7f, Wood, -1, "Bed");
            Deco(98f, 0f, 101f, 1.1f, Wood, -1, "Table");
            var table = Marker("Table", 99.5f, 0f);
            var fire = Marker("Fire", 87.5f, 0f);
            var barroFire = Marker("Barro by the fire", 85f, 0f);

            var sisnando = Npc("a0_npc_sisnando", "Sisnando", new Color(0.62f, 0.56f, 0.5f), 90f, 0f,
                new[] { L("", "The pass gives and the pass takes, my son. Mostly it takes.") },
                new[] { L("", "Mind the roof. And the dog. Mostly the dog.") }, "Sisnando");
            ShowBetween(sisnando.gameObject, "", "act0_vanished");

            // Spring: the roof
            for (int i = 1; i <= 3; i++)
            {
                float rx = 82f + i * 6.5f;
                var broken = Deco(rx - 1f, 6.2f, rx + 1f, 6.5f, new Color(0.15f, 0.1f, 0.08f), 2, "Broken roof");
                ShowBetween(broken, "", "roof_" + i);
                var fix = Sequence("a0_seq_roof_" + i, SequenceTrigger.Manual, Area(rx, 6.2f, rx + 0.1f, 6.3f),
                    Sound(AudioIds.RoofHammer), Wait(0.25f), Sound(AudioIds.RoofHammer));
                fix.freezePlayer = false;
                InteractSpot("a0_roof_" + i, rx, 6.2f, "Fix the roof", fix, 1.6f, "roof_" + i, "montage_roof");
            }

            // Summer: dice, Pio, and the shed lock
            var summer = Sequence("a0_seq_summer", SequenceTrigger.WhenFlagSet, Area(99f, 0f, 99.1f, 0.1f),
                Card("Summer", "", 2.5f), Teleport(table), Sound(AudioIds.DiceRoll),
                Dlg(L("Sisnando", "Your throw."),
                    L("Corvin", "Six. Again."),
                    L("Sisnando", "Remarkable dice. They only roll sixes for you."),
                    L("Corvin", "My little brother taught me. Pio. He was better at it than me."),
                    L("Sisnando", "Was?"),
                    L("Corvin", "...Your throw, Father.")),
                Dlg(L("Sisnando", "While you're here: the shed under the cliff rusted shut years ago, and I lost the key."),
                    L("Corvin", "Lucky for you, I never needed keys.")),
                Flag("montage_lock"),
                Tip("Pick the lock on the shed under the cliff, west of the shelter: use it, then press {Interact} while the pick is in the gold zone."));
            summer.requiredFlagsAll = new[] { "roof_1", "roof_2", "roof_3" };

            // The shed under the cliff, west of the shelter: lockpicking tutorial.
            // Its roof is level with the trail, so it never blocks the way east.
            var shedDoor = GateAt(76.5f, 0f, 3f, "a0_shed_door");
            Block(70f, 4f, 77f, 4.5f, Wood, "Shed roof");
            Block(76f, 3f, 77f, 4f, Wood, "Shed wall");
            Deco(70f, 0f, 76f, 4f, new Color(0.25f, 0.2f, 0.16f), -6, "Shed (inside)");
            OneWay(78f, 80f, 2.3f); // a step back up to the trail
            Spawn<HealthUpgradePickup>("HealthUpgrade", 73f, 1f, "a0_shed_health");
            var shedLock = new GameObject("Shed lock");
            shedLock.transform.SetParent(actorsRoot, false);
            shedLock.transform.position = new Vector3(77.6f, 0f, 0f);
            var lockpick = shedLock.AddComponent<LockpickLock>();
            lockpick.pins = 2;
            lockpick.sweetSpot = 0.26f;
            lockpick.speed = 0.8f;
            lockpick.gate = shedDoor;
            SetId(shedLock, "a0_shed_lock");

            var autumn = Sequence("a0_seq_autumn", SequenceTrigger.WhenFlagSet, Area(114f, 0f, 114.1f, 0.1f),
                Card("Autumn", "", 2.5f),
                Dlg(L("Sisnando", "Corvin! A pilgrim was due over the pass yesterday, and the storm came first."),
                    L("Sisnando", "Take Barro. If anyone can find a man under the snow, he can.")),
                Flag("montage_pilgrim"),
                Tip("Head east into the storm. Press {BarroTrack} and Barro will search for the pilgrim."));
            autumn.requiredFlagsAll = new[] { "montage_lock", "a0_shed_lock:unlocked" };

            var drift = GateAt(124.5f, 0f, 4f, "a0_snowdrift");
            drift.openWhenFlagSet = "montage_pilgrim";
            drift.rememberWhenOpened = false;

            // Vanished: tracks, the letter
            var tracks = new GameObject("Three sets of tracks");
            tracks.transform.SetParent(geometryRoot, false);
            for (int row = 0; row < 3; row++)
            for (float tx = 109f; tx < 124f; tx += 0.9f)
                Deco(tx + row * 0.3f, 0.02f + row * 0.06f, tx + row * 0.3f + 0.35f, 0.08f + row * 0.06f, new Color(0.35f, 0.38f, 0.45f), 3, "Footprint")
                    .transform.SetParent(tracks.transform, true);
            ShowBetween(tracks, "act0_vanished", "");
            var scent = new GameObject("Tracks (Barro won't follow)");
            scent.transform.SetParent(actorsRoot, false);
            scent.transform.position = new Vector3(113f, 0.3f, 0f);
            var poi = scent.AddComponent<BarroPointOfInterest>();
            poi.refuseToTrack = true;
            poi.once = false;
            poi.foundMessage = "Barro sits by the tracks and howls. He won't follow them.";
            ShowBetween(scent, "act0_vanished", "");

            var letterSequence = Sequence("a0_seq_letter", SequenceTrigger.Manual, Area(99.5f, 0f, 99.6f, 0.1f),
                Dlg(L("Sisnando (letter)", "If you are reading this, I was too slow. Do not come after me. Go after her."),
                    L("Sisnando (letter)", "The map shows Mérida. Keep Barro close; he knows more than either of us."),
                    L("Sisnando (letter)", "And Corvin - fix the roof.")),
                Give("half_map"), Give("sisnando_letter"), Give("silver_collar"),
                Flag(CompanionFlags.Collar), JoinBarro(),
                Tip("You put the silver collar on Barro. Someone is waiting on the ledge to the east."));
            var letter = InteractSpot("a0_letter", 99.5f, 0f, "Read the letter", letterSequence, 0f, "a0_letter_read", "");
            ShowBetween(letter.gameObject, "act0_vanished", "");
            var letterProps = Deco(98.6f, 1.1f, 100.4f, 1.25f, new Color(0.9f, 0.88f, 0.8f), 1, "Letter, map and collar");
            ShowBetween(letterProps, "act0_vanished", "a0_letter_read");

            // Winter: the confession, then morning
            Sequence("a0_seq_winter", SequenceTrigger.PlayerEntersArea, Area(84f, 0f, 107f, 5f),
                Card("Winter", "", 2.5f), Teleport(fire), BarroWalk(barroFire), Music(AudioIds.MusicHermitage),
                Dlg(L("Corvin", "You never talk about Hispania."),
                    L("Sisnando", "Because I left it burning."),
                    L("Sisnando", "We had a relic. Saint Eulalia. A girl who died so young the sky covered her in snow out of pity. We split her in three and hid her from the war."),
                    L("Corvin", "Worth much?"),
                    L("Sisnando", "(laughs) Everything. Not to a buyer. There is something under Toledo that eats wars, Corvin. Her snow kept it sleeping. And now the war has started again."),
                    L("Corvin", "Old men's stories."),
                    L("Sisnando", "Yes. I am an old man. And the story is coming up the mountain.")),
                FadeOut(2.5f), Silence(2f), Card("The next morning", "", 3f),
                Flag("act0_vanished"), Sound(AudioIds.DoorCreak), BarroRelease(), BarroAnim(AnimStates.Howl, 3f, AudioIds.BarroHowl),
                FadeIn(2f), Tip("The fire is cold and Sisnando is gone. Look around the shelter."))
                .requiredFlag = "pilgrim_saved";

            // ---- The storm: the pilgrim (x 124..176) ----
            RoomArea("a0_room_storm", "The storm", 124f, -6f, 176f, 24f, AudioIds.MusicPass, AudioIds.AmbiencePassWind);
            Block(124f, -6f, 176f, 0f, Snow);
            Block(138f, 0f, 142f, 1.2f, Snow);
            Block(150f, 0f, 153f, 1.6f, Snow);
            Snowfall(124f, 176f, 22f, 20f, 60f, 1.8f);
            Dread(124f, -6f, 176f, 24f, 1.5f);
            CheckpointAt("a0_cp_storm", 127f, 0f, 126f, -1f, 130f, 5f);
            HintArea("a0_hint_combat", 130f, -1f, 136f, 5f,
                "Wolves in the snow. Barro growls when he smells them. {Attack} strikes, {Parry} parries, {Dodge} rolls, {BarroAttack} sends Barro.");
            Spawn<WalkerEnemy>("Wolf", 146f, 0f);
            Spawn<WalkerEnemy>("Wolf", 160f, 0f);

            var mound = Deco(166.5f, 0f, 169.5f, 0.8f, Color.white, 2, "Snow mound");
            ShowBetween(mound, "", "pilgrim_saved");
            var pilgrim = Actor("Pilgrim", new Color(0.55f, 0.5f, 0.4f), new Vector2(0.85f, 1.65f), 170f, 0f, "Pilgrim");
            ShowBetween(pilgrim, "pilgrim_saved", "");
            var pilgrimSequence = Sequence("a0_seq_pilgrim", SequenceTrigger.Manual, Area(168f, 0f, 168.1f, 0.1f),
                Sound(AudioIds.Dig),
                Dlg(L("Pilgrim", "Bless you... and bless the dog."),
                    L("Corvin", "Thank the dog. I only did the digging.")),
                Tip("Bring the pilgrim's news home to the shelter."));
            var dig = InteractSpot("a0_dig_pilgrim", 168f, 0f, "Dig", pilgrimSequence, 2f, "pilgrim_saved", "");
            dig.gameObject.SetActive(false);
            var buried = new GameObject("Buried pilgrim (scent)");
            buried.transform.SetParent(actorsRoot, false);
            buried.transform.position = new Vector3(168f, 0.5f, 0f);
            var pilgrimPoi = buried.AddComponent<BarroPointOfInterest>();
            pilgrimPoi.kind = BarroPointOfInterest.Kind.Person;
            pilgrimPoi.trackRange = 40f;
            pilgrimPoi.foundMessage = "Barro found someone under the snow! Dig them out.";
            pilgrimPoi.revealOnFound = new[] { dig.gameObject };
            buried.AddComponent<PersistentId>();
            SetId(buried, "a0_poi_pilgrim");

            // ---- The icy ledge: Malaspina (x 176..218) ----
            RoomArea("a0_room_ledge", "The icy ledge", 176f, -6f, 218f, 24f, AudioIds.MusicPass, AudioIds.AmbiencePassWind);
            var iceFloor = Block(176f, -6f, 212f, 0f, ice, "Ice");
            iceFloor.AddComponent<SurfaceModifier>();
            Spikes(212f, 216f, -5f);
            Block(212f, -6f, 216f, -5f, Stone);
            Wall(216f, -6f, 218f, 24f);
            CheckpointAt("a0_cp_ledge", 177f, 0f, 176.2f, -1f, 178f, 5f);
            Wall(178f, 5f, 179f, 24f);
            var ledgeGate = GateAt(178.5f, 0f, 5f, "a0_ledge_gate", true, false);

            var malaspina = Spawn<DuelistBoss>("Malaspina", 203f, 0f);
            ShowBetween(malaspina.gameObject, "a0_letter_read", ""); // he arrives once Sisnando is gone
            var picks = Spawn<KeyItemPickup>("KeyItem", 200f, 1.2f, "a0_malaspina_picks");
            picks.itemId = "malaspina_picks";
            picks.displayName = "Malaspina's Picks";
            picks.description = "Fine steel picks. Locks too delicate before will now give, and picking is easier.";
            picks.showPopup = true;
            Record(picks);

            var arenaGo = new GameObject("BossArena - Malaspina");
            arenaGo.transform.position = new Vector3(195f, 6f, 0f);
            var arenaBox = arenaGo.AddComponent<BoxCollider2D>();
            arenaBox.isTrigger = true;
            arenaBox.size = new Vector2(30f, 12f);
            var arena = arenaGo.AddComponent<BossArena>();
            arena.boss = malaspina;
            arena.gates = new[] { ledgeGate };
            arena.activateOnDefeat = new[] { picks.gameObject };
            arena.startOnEnter = false;
            arena.victoryToast = "Malaspina yields.";
            SetId(arenaGo, "a0_arena_malaspina");

            Sequence("a0_seq_malaspina", SequenceTrigger.PlayerEntersArea, Area(181f, 0f, 186f, 5f),
                Dlg(L("Malaspina", "Corvin. A year of patching an old man's roof, and not one load of salt for me."),
                    L("Corvin", "The priest is gone. Go home, Malaspina."),
                    L("Malaspina", "Gone? Then the shelter is mine. And your debt is still yours."),
                    L("Malaspina", "Pay it in silver, or pay it on this ledge.")),
                Fight(arenaGo))
                .requiredFlag = "a0_letter_read";

            Sequence("a0_seq_outro", SequenceTrigger.WhenFlagSet, Area(200f, 0f, 200.1f, 0.1f),
                Wait(1f),
                Dlg(L("Corvin", "Mérida, then."),
                    L("Corvin", "Come on, Barro. Let's go and find him.")),
                FadeOut(2.5f), Silence(2f),
                Card("Act I", "The Road of Ash", 3.5f),
                Card("End of the demo", "Thank you for playing", 3f),
                EndOfDemo())
                .requiredFlag = "a0_malaspina_picks";

            FinishStoryState();
            SaveScene(Act0Scene);
        }
    }
}
