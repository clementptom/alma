using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alma.EditorTools
{
    /// <summary>
    /// Alma > Build Greybox Demo
    ///
    /// Generates placeholder sprites, prefabs for every gameplay object, and the playable scenes:
    /// MainMenu, the Last Lock slice (Prologue_Merida and Act0_Pass, see GreyboxDemoBuilder.LastLock.cs),
    /// and the systems sandbox (Area1, Area2: all four abilities, the Brute boss, shops, fast travel,
    /// lever + gate, key + locked door, breakable secret wall, moving and one-way platforms, the map).
    ///
    /// Safe to run again: it overwrites the generated files in Assets/Alma/Prefabs,
    /// Assets/Alma/Scenes and Assets/Alma/Art/Placeholder only.
    /// </summary>
    public static partial class GreyboxDemoBuilder
    {
        const string Root = AlmaProjectSetup.RootFolder;
        const string PrefabFolder = Root + "/Prefabs";
        const string SceneFolder = Root + "/Scenes";
        const string PlaceholderFolder = Root + "/Art/Placeholder";

        static readonly Color GroundColor = new Color(0.24f, 0.23f, 0.3f);
        static readonly Color WallColor = new Color(0.18f, 0.17f, 0.23f);
        static readonly Color PlatformColor = new Color(0.42f, 0.38f, 0.5f);

        static Sprite square;
        static Sprite circle;
        static Dictionary<string, AnimatorController> controllers;
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        static Transform geometryRoot;
        static Transform actorsRoot;
        static Transform roomsRoot;

        [MenuItem("Alma/Build Greybox Demo", priority = 0)]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Build Greybox Demo",
                    "This creates placeholder sprites, prefabs and the MainMenu, Prologue_Merida, Act0_Pass, Area1 and Area2 scenes in Assets/Alma, " +
                    "replacing earlier generated versions.\n\nContinue?", "Build", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Alma", "Project setup...", 0.1f);
                AlmaProjectSetup.CreateLayers();
                AlmaProjectSetup.CreateDataAssets();
                controllers = AlmaProjectSetup.CreateAnimatorControllers();
                CreatePlaceholderSprites();

                EditorUtility.DisplayProgressBar("Alma", "Prefabs...", 0.3f);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreatePrefabs();

                EditorUtility.DisplayProgressBar("Alma", "Scenes...", 0.6f);
                AlmaProjectSetup.EnsureFolder(SceneFolder);
                BuildMainMenu();
                BuildPrologue();
                BuildAct0();
                BuildArea1();
                BuildArea2();
                ConfigureBuildSettings();
                ConfigureGameConfig();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(SceneFolder + "/MainMenu.unity");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog("Greybox demo ready",
                "Press Play in MainMenu and choose Play > a slot to start The Last Lock (prologue, then Act 0).\n\n" +
                "Open Prologue_Merida or Act0_Pass and press Play to test a chapter directly. Area1 / Area2 are the systems sandbox.\n\n" +
                "Cheats in the editor: F1 all abilities, F2 heal, F3 +100 currency, F4 god mode.", "OK");
        }

        // ================================================================ Sprites

        static void CreatePlaceholderSprites()
        {
            AlmaProjectSetup.EnsureFolder(PlaceholderFolder);

            var sq = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            sq.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            square = SaveSprite(PlaceholderFolder + "/Square.png", sq, 4f, FilterMode.Point);

            const int size = 64;
            var ci = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                ci.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
            circle = SaveSprite(PlaceholderFolder + "/Circle.png", ci, size, FilterMode.Bilinear);
        }

        static Sprite SaveSprite(string path, Texture2D texture, float pixelsPerUnit, FilterMode filter)
        {
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================ Prefabs

        static void CreatePrefabs()
        {
            AlmaProjectSetup.EnsureFolder(PrefabFolder);
            prefabs.Clear();

            // Player lives in Resources so the GameManager can spawn it in any scene.
            var player = PlayerFactory.CreatePlaceholderPlayer(square);
            player.GetComponentInChildren<Animator>().runtimeAnimatorController = controllers["Player"];
            prefabs["Player"] = SavePrefab(player, AlmaProjectSetup.ResourcesFolder + "/Player.prefab");

            prefabs["Coin"] = SavePrefab(MakeCoin(), PrefabFolder + "/Pickups/Coin.prefab");
            prefabs["Projectile"] = SavePrefab(MakeProjectile("EnemyProjectile", new Color(1f, 0.45f, 0.3f), 0.45f, 0.22f), PrefabFolder + "/Combat/EnemyProjectile.prefab");
            var shockwave = MakeProjectile("Shockwave", new Color(1f, 0.7f, 0.3f), 0.7f, 0.35f);
            var wave = shockwave.GetComponent<Projectile>();
            wave.lifetime = 2.2f;
            wave.rotateToDirection = false;
            prefabs["Shockwave"] = SavePrefab(shockwave, PrefabFolder + "/Combat/Shockwave.prefab");

            prefabs["Walker"] = SavePrefab(MakeWalker(), PrefabFolder + "/Enemies/Walker.prefab");
            prefabs["Flyer"] = SavePrefab(MakeFlyer(), PrefabFolder + "/Enemies/Flyer.prefab");
            prefabs["Shooter"] = SavePrefab(MakeShooter(), PrefabFolder + "/Enemies/Shooter.prefab");
            prefabs["Brute"] = SavePrefab(MakeBrute(), PrefabFolder + "/Enemies/BruteBoss.prefab");

            prefabs["SaveStation"] = SavePrefab(MakeSaveStation(), PrefabFolder + "/World/SaveStation.prefab");
            prefabs["NPC"] = SavePrefab(MakeNpc(), PrefabFolder + "/World/NPC.prefab");
            prefabs["Lever"] = SavePrefab(MakeLever(), PrefabFolder + "/World/Lever.prefab");
            prefabs["AbilityPickup"] = SavePrefab(MakeAbilityPickup(), PrefabFolder + "/Pickups/AbilityPickup.prefab");
            prefabs["HealthUpgrade"] = SavePrefab(MakeHealthUpgrade(), PrefabFolder + "/Pickups/HealthUpgrade.prefab");
            prefabs["KeyItem"] = SavePrefab(MakeKeyItem(), PrefabFolder + "/Pickups/KeyItem.prefab");
            prefabs["EnergyUpgrade"] = SavePrefab(MakeEnergyUpgrade(), PrefabFolder + "/Pickups/EnergyUpgrade.prefab");
            prefabs["Shopkeeper"] = SavePrefab(MakeShopkeeper(), PrefabFolder + "/World/Shopkeeper.prefab");
            prefabs["FastTravel"] = SavePrefab(MakeFastTravel(), PrefabFolder + "/World/FastTravelPoint.prefab");

            // Building blocks for your own levels (scenes below create these directly, sized per use).
            SavePrefab(MakeGate(4f), PrefabFolder + "/World/Gate.prefab");
            SavePrefab(MakeSpikes(3f), PrefabFolder + "/World/Spikes.prefab");
            SavePrefab(MakeBreakable(1f, 3f), PrefabFolder + "/World/BreakableWall.prefab");
            SavePrefab(MakeOneWay(4f), PrefabFolder + "/World/OneWayPlatform.prefab");
            SavePrefab(MakeMovingPlatform(3f), PrefabFolder + "/World/MovingPlatform.prefab");
            SavePrefab(MakeDoor(), PrefabFolder + "/World/Door.prefab");
            SavePrefab(MakeCrumbling(3f), PrefabFolder + "/World/CrumblingPlatform.prefab");
            SavePrefab(MakeBouncePad(), PrefabFolder + "/World/BouncePad.prefab");
            CreateLastLockPrefabs();
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            AlmaProjectSetup.EnsureFolder(Path.GetDirectoryName(path));
            foreach (var pid in go.GetComponentsInChildren<PersistentId>(true)) pid.EditorSetId("");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject NewObject(string name, string layer = null)
        {
            var go = new GameObject(name);
            if (layer != null)
            {
                int index = LayerMask.NameToLayer(layer);
                if (index >= 0) go.layer = index;
            }
            return go;
        }

        /// <summary>Adds Visual/Body children with a placeholder sprite. Returns the Visual transform.</summary>
        static Transform AddVisual(GameObject root, Sprite sprite, Color color, Vector2 size, Vector2 offset, int order = 10)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var body = new GameObject("Body");
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = offset;
            body.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return visual.transform;
        }

        static void AddEye(Transform visual, Vector2 position, float size = 0.2f)
        {
            var eye = new GameObject("FacingMarker");
            eye.transform.SetParent(visual, false);
            eye.transform.localPosition = position;
            eye.transform.localScale = new Vector3(size, size, 1f);
            var sr = eye.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.1f, 0.05f, 0.1f);
            sr.sortingOrder = 12;
        }

        static void AddAnimator(Transform visual, string controller)
        {
            var animator = visual.gameObject.AddComponent<Animator>();
            if (controllers != null && controllers.TryGetValue(controller, out var c)) animator.runtimeAnimatorController = c;
        }

        static GameObject MakeCoin()
        {
            var go = NewObject("Coin", Layers.Pickup);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
            AddVisual(go, circle, new Color(1f, 0.85f, 0.3f), new Vector2(0.32f, 0.32f), Vector2.zero, 20);
            var coin = go.AddComponent<CurrencyPickup>();
            coin.persistent = false;
            coin.bobAmplitude = 0f;
            coin.collectRadius = 0.7f;
            return go;
        }

        static GameObject MakeProjectile(string name, Color color, float size, float radius)
        {
            var go = NewObject(name);
            AddVisual(go, circle, color, new Vector2(size, size), Vector2.zero, 30);
            var p = go.AddComponent<Projectile>();
            p.radius = radius;
            return go;
        }

        static GameObject MakeEnemyBase(string name, Vector2 size, bool feetPivot, Color color, Sprite sprite, string controller, int hp)
        {
            var go = NewObject(name, Layers.Enemy);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Vector2 offset = feetPivot ? new Vector2(0f, size.y * 0.5f) : Vector2.zero;
            if (sprite == circle)
            {
                var c = go.AddComponent<CircleCollider2D>();
                c.radius = size.x * 0.5f;
                c.offset = offset;
            }
            else
            {
                var b = go.AddComponent<BoxCollider2D>();
                b.size = size;
                b.offset = offset;
            }
            go.AddComponent<Health>().maxHealth = hp;
            go.AddComponent<ContactDamage>();
            var visual = AddVisual(go, sprite, color, size, offset);
            AddEye(visual, new Vector2(size.x * 0.3f, offset.y + size.y * 0.2f));
            AddAnimator(visual, controller);
            return go;
        }

        static GameObject MakeWalker()
        {
            var go = MakeEnemyBase("Walker", new Vector2(1.1f, 0.9f), true, new Color(0.65f, 0.35f, 0.75f), square, "Walker", 3);
            var e = go.AddComponent<WalkerEnemy>();
            e.displayName = "Crawler";
            e.currencyPrefab = prefabs["Coin"].GetComponent<CurrencyPickup>();
            return go;
        }

        static GameObject MakeFlyer()
        {
            var go = MakeEnemyBase("Flyer", new Vector2(0.9f, 0.9f), false, new Color(0.3f, 0.75f, 0.7f), circle, "Flyer", 2);
            var e = go.AddComponent<FlyerEnemy>();
            e.displayName = "Wisp";
            e.currencyPrefab = prefabs["Coin"].GetComponent<CurrencyPickup>();
            return go;
        }

        static GameObject MakeShooter()
        {
            var go = MakeEnemyBase("Shooter", new Vector2(0.9f, 1.3f), true, new Color(0.8f, 0.5f, 0.25f), square, "Shooter", 3);
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.5f, 1f, 0f);
            var e = go.AddComponent<ShooterEnemy>();
            e.displayName = "Spitter";
            e.muzzle = muzzle.transform;
            e.projectilePrefab = prefabs["Projectile"].GetComponent<Projectile>();
            e.currencyPrefab = prefabs["Coin"].GetComponent<CurrencyPickup>();
            return go;
        }

        static GameObject MakeBrute()
        {
            var go = MakeEnemyBase("BruteBoss", new Vector2(2.4f, 2.6f), true, new Color(0.75f, 0.25f, 0.3f), square, "Brute", 24);
            go.GetComponent<ContactDamage>().damage = 1;
            var boss = go.AddComponent<BruteBoss>();
            boss.displayName = "The Brute";
            boss.shockwavePrefab = prefabs["Shockwave"].GetComponent<Projectile>();
            boss.volleyPrefab = prefabs["Projectile"].GetComponent<Projectile>();
            boss.currencyPrefab = prefabs["Coin"].GetComponent<CurrencyPickup>();
            return go;
        }

        static GameObject MakeSaveStation()
        {
            var go = NewObject("SaveStation");
            var visual = AddVisual(go, square, new Color(0.35f, 0.6f, 0.95f), new Vector2(1.6f, 0.7f), new Vector2(0f, 0.35f), 5);
            AddAnimator(visual, "SaveStation");
            go.AddComponent<SaveStation>();
            return go;
        }

        static GameObject MakeNpc()
        {
            var go = NewObject("NPC");
            var visual = AddVisual(go, square, new Color(0.45f, 0.8f, 0.45f), new Vector2(0.8f, 1.7f), new Vector2(0f, 0.85f), 8);
            AddEye(visual, new Vector2(0.2f, 1.4f));
            AddAnimator(visual, "NPC");
            var npc = go.AddComponent<NPC>();
            npc.visual = visual;
            return go;
        }

        static GameObject MakeLever()
        {
            var go = NewObject("Lever");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 1.2f);
            col.offset = new Vector2(0f, 0.6f);
            var visual = AddVisual(go, square, new Color(0.5f, 0.45f, 0.4f), new Vector2(0.7f, 0.3f), new Vector2(0f, 0.15f), 6);
            var handle = new GameObject("Handle");
            handle.transform.SetParent(visual, false);
            handle.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var stick = new GameObject("Stick");
            stick.transform.SetParent(handle.transform, false);
            stick.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            stick.transform.localScale = new Vector3(0.15f, 0.9f, 1f);
            var sr = stick.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.95f, 0.8f, 0.3f);
            sr.sortingOrder = 6;
            AddAnimator(visual, "Lever");
            var lever = go.AddComponent<Lever>();
            lever.handle = handle.transform;
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
            return go;
        }

        static GameObject MakePickupBase(string name, Color color, Sprite sprite, float size)
        {
            var go = NewObject(name);
            AddVisual(go, sprite, color, new Vector2(size, size), Vector2.zero, 20);
            return go;
        }

        static GameObject MakeAbilityPickup()
        {
            var go = MakePickupBase("AbilityPickup", new Color(0.6f, 0.95f, 1f), circle, 0.8f);
            go.AddComponent<AbilityPickup>();
            return go;
        }

        static GameObject MakeHealthUpgrade()
        {
            var go = MakePickupBase("HealthUpgrade", new Color(1f, 0.35f, 0.45f), square, 0.6f);
            go.transform.GetChild(0).GetChild(0).localRotation = Quaternion.Euler(0f, 0f, 45f);
            go.AddComponent<HealthUpgradePickup>();
            return go;
        }

        static GameObject MakeEnergyUpgrade()
        {
            var go = MakePickupBase("EnergyUpgrade", new Color(0.45f, 0.8f, 1f), circle, 0.6f);
            go.AddComponent<EnergyUpgradePickup>();
            return go;
        }

        static GameObject MakeShopkeeper()
        {
            var go = NewObject("Shopkeeper");
            var visual = AddVisual(go, square, new Color(0.85f, 0.65f, 0.35f), new Vector2(1f, 1.6f), new Vector2(0f, 0.8f), 8);
            AddEye(visual, new Vector2(0.25f, 1.3f));
            AddAnimator(visual, "Shopkeeper");
            var shop = go.AddComponent<Shopkeeper>();
            shop.visual = visual;
            return go;
        }

        static GameObject MakeFastTravel()
        {
            var go = NewObject("FastTravelPoint");
            var visual = AddVisual(go, circle, new Color(0.7f, 0.5f, 1f), new Vector2(1.1f, 1.1f), new Vector2(0f, 1.3f), 5);
            var pedestal = new GameObject("Pedestal");
            pedestal.transform.SetParent(visual, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            pedestal.transform.localScale = new Vector3(1.2f, 0.6f, 1f);
            var sr = pedestal.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.4f, 0.35f, 0.5f);
            sr.sortingOrder = 4;
            AddAnimator(visual, "FastTravel");
            go.AddComponent<FastTravelPoint>();
            return go;
        }

        static GameObject MakeCrumbling(float width)
        {
            var go = NewObject("CrumblingPlatform", Layers.Ground);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.5f);
            col.offset = new Vector2(0f, -0.25f);
            AddVisual(go, square, new Color(0.62f, 0.5f, 0.38f), new Vector2(width, 0.5f), new Vector2(0f, -0.25f), 1);
            go.AddComponent<CrumblingPlatform>();
            return go;
        }

        static GameObject MakeBouncePad()
        {
            var go = NewObject("BouncePad", Layers.Ground);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 0.4f);
            col.offset = new Vector2(0f, 0.2f);
            var visual = AddVisual(go, square, new Color(0.4f, 0.95f, 0.5f), new Vector2(1.4f, 0.4f), new Vector2(0f, 0.2f), 3);
            AddAnimator(visual, "BouncePad");
            go.AddComponent<BouncePad>();
            return go;
        }

        static GameObject MakeKeyItem()
        {
            var go = MakePickupBase("KeyItem", new Color(1f, 0.85f, 0.35f), square, 0.5f);
            go.AddComponent<KeyItemPickup>();
            return go;
        }

        static GameObject MakeGate(float height)
        {
            var go = NewObject("Gate", Layers.Ground);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, height);
            col.offset = new Vector2(0f, height * 0.5f);
            AddVisual(go, square, new Color(0.55f, 0.5f, 0.45f), new Vector2(1f, height), new Vector2(0f, height * 0.5f), 4);
            var gate = go.AddComponent<Gate>();
            gate.openOffset = new Vector2(0f, height);
            return go;
        }

        static GameObject MakeSpikes(float width)
        {
            var go = NewObject("Spikes");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, 0.6f);
            col.offset = new Vector2(0f, 0.3f);
            AddVisual(go, square, new Color(0.9f, 0.3f, 0.3f), new Vector2(width, 0.6f), new Vector2(0f, 0.3f), 3);
            go.AddComponent<ContactDamage>().isHazard = true;
            return go;
        }

        static GameObject MakeBreakable(float width, float height)
        {
            var go = NewObject("BreakableWall", Layers.Breakable);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);
            col.offset = new Vector2(0f, height * 0.5f);
            AddVisual(go, square, new Color(0.45f, 0.36f, 0.3f), new Vector2(width, height), new Vector2(0f, height * 0.5f), 2);
            go.AddComponent<Breakable>();
            return go;
        }

        static GameObject MakeOneWay(float width)
        {
            var go = NewObject("OneWayPlatform", Layers.OneWayPlatform);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.35f);
            col.offset = new Vector2(0f, -0.175f);
            col.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            go.AddComponent<OneWayPlatform>();
            AddVisual(go, square, PlatformColor, new Vector2(width, 0.35f), new Vector2(0f, -0.175f), 1);
            return go;
        }

        static GameObject MakeMovingPlatform(float width)
        {
            var go = NewObject("MovingPlatform", Layers.Ground);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.5f);
            col.offset = new Vector2(0f, -0.25f);
            AddVisual(go, square, new Color(0.55f, 0.6f, 0.75f), new Vector2(width, 0.5f), new Vector2(0f, -0.25f), 1);
            go.AddComponent<MovingPlatform>();
            return go;
        }

        static GameObject MakeDoor()
        {
            var go = NewObject("Door");
            var col = go.AddComponent<BoxCollider2D>();
            go.AddComponent<RoomTransition>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 4.5f);
            col.offset = new Vector2(0f, 2.25f);
            AddVisual(go, square, new Color(1f, 0.85f, 0.3f, 0.15f), new Vector2(1f, 4.5f), new Vector2(0f, 2.25f), 0);
            return go;
        }

        // ================================================================= Scenes

        static void ConfigureGameConfig()
        {
            var config = AlmaProjectSetup.LoadOrCreate<GameConfig>(AlmaProjectSetup.ResourcesFolder + "/GameConfig.asset");
            config.mainMenuScene = "MainMenu";
            config.startScene = PrologueScene;
            config.startSpawnId = "prologue_start";
            config.playerPrefab = prefabs["Player"];

            // Placeholder credits text you can edit.
            string folder = Root + "/Text";
            string creditsPath = folder + "/Credits.txt";
            AlmaProjectSetup.EnsureFolder(folder);
            if (!File.Exists(creditsPath))
            {
                File.WriteAllText(creditsPath, CreditsUI.PlaceholderCredits);
                AssetDatabase.ImportAsset(creditsPath);
            }
            config.credits = AssetDatabase.LoadAssetAtPath<TextAsset>(creditsPath);
            EditorUtility.SetDirty(config);

            // Names for the demo's items.
            var items = AlmaProjectSetup.LoadOrCreate<ItemDatabase>(AlmaProjectSetup.ResourcesFolder + "/ItemDatabase.asset");
            AddItem(items, "key_arena", "Arena Key", "A heavy iron key. It opens the gate to the Brute's arena.");
            AddItem(items, "old_lantern", "Old Lantern", "Sold by a wandering merchant. It doesn't seem to do anything... yet.");
            AddLastLockItems(items);
            EditorUtility.SetDirty(items);
        }

        static void AddItem(ItemDatabase db, string id, string name, string description)
        {
            if (db.FindItem(id) != null) return;
            db.items.Add(new ItemDefinition { id = id, displayName = name, description = description, category = ItemCategory.KeyItem });
        }

        static void ConfigureBuildSettings()
        {
            var ours = new[] { "MainMenu", PrologueScene, Act0Scene, "Area1", "Area2" }.Select(n => $"{SceneFolder}/{n}.unity").ToList();
            var others = EditorBuildSettings.scenes.Where(s => !ours.Contains(s.path));
            EditorBuildSettings.scenes = ours.Select(p => new EditorBuildSettingsScene(p, true)).Concat(others).ToArray();
        }

        static void BeginScene(Color background)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            camGo.transform.position = new Vector3(0f, 3f, -10f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraController>();

            geometryRoot = new GameObject("Geometry").transform;
            roomsRoot = new GameObject("Rooms").transform;
            actorsRoot = new GameObject("Actors").transform;
        }

        static void SaveScene(string name)
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"{SceneFolder}/{name}.unity");
        }

        static GameObject Block(float x0, float y0, float x1, float y1, Color? color = null, string name = "Block")
        {
            var go = NewObject(name, Layers.Ground);
            go.transform.SetParent(geometryRoot, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            go.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color ?? GroundColor;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            go.isStatic = true;
            return go;
        }

        static GameObject Wall(float x0, float y0, float x1, float y1) => Block(x0, y0, x1, y1, WallColor, "Wall");

        static GameObject Put(GameObject go, float x, float y, Transform parent = null)
        {
            go.transform.SetParent(parent != null ? parent : geometryRoot, false);
            go.transform.position = new Vector3(x, y, 0f);
            return go;
        }

        static void OneWay(float x0, float x1, float y) => Put(MakeOneWay(x1 - x0), (x0 + x1) * 0.5f, y);

        static void Spikes(float x0, float x1, float y) => Put(MakeSpikes(x1 - x0), (x0 + x1) * 0.5f, y);

        static Gate GateAt(float x, float y, float height, string id, bool startsOpen = false, bool remember = true)
        {
            var go = Put(MakeGate(height), x, y);
            var gate = go.GetComponent<Gate>();
            gate.startsOpen = startsOpen;
            gate.rememberWhenOpened = remember;
            SetId(go, id);
            return gate;
        }

        static Room RoomArea(string id, string displayName, float x0, float y0, float x1, float y1, string music, string ambience)
        {
            var go = new GameObject("Room - " + displayName);
            go.transform.SetParent(roomsRoot, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            var room = go.AddComponent<Room>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0, y1 - y0);
            room.displayName = displayName;
            room.musicId = music;
            room.ambienceId = ambience;
            SetId(go, id);
            return room;
        }

        static void Background(float x0, float x1, Color color)
        {
            var bg = new GameObject("Background (parallax)");
            var layer = bg.AddComponent<ParallaxLayer>();
            layer.horizontalFactor = 0.6f;
            layer.verticalFactor = 0.4f;
            var rng = new System.Random(7);
            for (float x = x0; x < x1; x += 7f)
            {
                float h = 6f + (float)rng.NextDouble() * 10f;
                var pillar = new GameObject("Silhouette");
                pillar.transform.SetParent(bg.transform, false);
                pillar.transform.position = new Vector3(x, h * 0.5f - 4f, 0f);
                pillar.transform.localScale = new Vector3(3f + (float)rng.NextDouble() * 3f, h, 1f);
                var sr = pillar.AddComponent<SpriteRenderer>();
                sr.sprite = square;
                sr.color = color;
                sr.sortingOrder = -20;
            }
        }

        static T Spawn<T>(string prefab, float x, float y, string id = null) where T : Component
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[prefab]);
            go.transform.SetParent(actorsRoot, false);
            go.transform.position = new Vector3(x, y, 0f);
            Record(go.transform);
            if (id != null) SetId(go, id);
            return go.GetComponent<T>();
        }

        static void SetId(GameObject go, string id)
        {
            var pid = go.GetComponent<PersistentId>();
            if (pid == null) return;
            pid.EditorSetId(id);
            Record(pid);
        }

        /// <summary>Makes sure changes to prefab instances are saved as overrides.</summary>
        static void Record(Object target)
        {
            if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static DialogueLine Line(string speaker, string text) => new DialogueLine { speaker = speaker, text = text };

        // ---------------------------------------------------------------- MainMenu

        static void BuildMainMenu()
        {
            BeginScene(new Color(0.04f, 0.04f, 0.08f));
            Object.DestroyImmediate(Object.FindFirstObjectByType<CameraController>());
            Object.DestroyImmediate(geometryRoot.gameObject);
            Object.DestroyImmediate(roomsRoot.gameObject);
            Object.DestroyImmediate(actorsRoot.gameObject);
            new GameObject("MainMenu").AddComponent<MainMenu>();
            SaveScene("MainMenu");
        }

        // ------------------------------------------------------------------ Area 1

        static void BuildArea1()
        {
            BeginScene(new Color(0.09f, 0.1f, 0.16f));
            Background(-10f, 100f, new Color(0.13f, 0.14f, 0.22f));

            // ---- Room: Entrance (x -8..48) ----
            RoomArea("a1_room_entrance", "Entrance", -8f, -6f, 48f, 22f, AudioIds.MusicArea1, AudioIds.AmbienceArea1);
            Wall(-8f, -6f, -6f, 22f);                 // outer left wall
            Block(-6f, -6f, 38f, 0f);                 // floor
            Block(38f, -6f, 42f, -3f);                // spike pit floor
            Spikes(38f, 42f, -3f);
            Block(42f, -6f, 66f, 0f);                 // floor continues into the hall
            Wall(-6f, 3f, 0f, 20f);                   // solid wall above the secret alcove
            Wall(-8f, 20f, 100f, 22f);                // ceiling for the whole area
            Block(18f, 0f, 21f, 1.5f);                // small step
            OneWay(23f, 27f, 3.2f);
            OneWay(29f, 33f, 6.4f);

            // Secret: breakable wall hiding a health upgrade
            var breakable = Put(MakeBreakable(1f, 3f), -0.5f, 0f);
            SetId(breakable, "a1_secret_wall");
            Spawn<HealthUpgradePickup>("HealthUpgrade", -3.5f, 1f, "a1_health_upgrade");

            var start = new GameObject("Spawn - start").AddComponent<SpawnPoint>();
            start.spawnId = "start";
            start.transform.position = new Vector3(3f, 0f, 0f);

            Spawn<SaveStation>("SaveStation", 7f, 0f, "a1_save");

            var npc = Spawn<NPC>("NPC", 12f, 0f, "a1_npc_wanderer");
            npc.npcName = "Old Wanderer";
            npc.lines = new[]
            {
                Line("", "Ah, a new face. Few come down this far anymore."),
                Line("", "Move with A/D or the stick. Jump with SPACE (A). Hold it to jump higher."),
                Line("", "Strike with J (X). Hold UP or DOWN while striking to aim. Hit things below you mid-air to bounce."),
                Line("", "That bench behind me lets you rest. Resting saves your progress."),
                Line("", "Striking enemies fills your energy. Stand still and hold L (B) to turn it into health."),
                Line("", "The gate ahead is shut. Pull the lever up high. Or hit it, if you're the impatient sort."),
            };
            npc.repeatLines = new[] { Line("", "Rest when you can. The deep doesn't forgive.") };
            Record(npc);

            var merchant = Spawn<Shopkeeper>("Shopkeeper", 15.5f, 0f, "a1_merchant");
            merchant.shopName = "Wandering Merchant";
            merchant.greeting = new[]
            {
                Line("Merchant", "Oh! A customer, down here? Have a look, have a look."),
                Line("Merchant", "Bring me the shards those creatures drop and we'll do business."),
            };
            merchant.stock = new[]
            {
                // Cheap enough to afford from the demo's enemy drops (F3 cheat: +100).
                new ShopEntry { type = ShopItemType.MaxHealth, amount = 1, price = 20 },
                new ShopEntry { type = ShopItemType.MaxEnergy, amount = 33, price = 30 },
                new ShopEntry { type = ShopItemType.KeyItem, itemId = "old_lantern", price = 8 },
            };
            Record(merchant);

            Spawn<WalkerEnemy>("Walker", 26f, 0f);
            Put(MakeBouncePad(), 35f, 0f);            // optional springboard up to the lever
            Spawn<FlyerEnemy>("Flyer", 36f, 9f);

            // Lever on the high platform opens the gate to the hall
            Wall(47f, 5f, 48f, 20f);
            var gate = GateAt(47.5f, 0f, 5f, "a1_gate_hall");
            var lever = Spawn<Lever>("Lever", 31f, 6.4f, "a1_lever");
            lever.targets = new[] { gate };
            Record(lever);

            // ---- Room: Hall (x 48..100) ----
            RoomArea("a1_room_hall", "Great Hall", 48f, -6f, 98f, 22f, AudioIds.MusicArea1, AudioIds.AmbienceArea1);
            var hallShrine = Spawn<FastTravelPoint>("FastTravel", 51.5f, 0f, "a1_travel_hall");
            hallShrine.pointName = "Great Hall Shrine";
            Record(hallShrine);

            OneWay(56f, 60f, 3f);
            var dash = Spawn<AbilityPickup>("AbilityPickup", 58f, 4f, "a1_ability_dash");
            dash.ability = AbilityType.Dash;
            Record(dash);

            Block(66f, -6f, 73f, -4f);                // wide pit: needs Dash (or Double Jump)
            Spikes(66f, 73f, -4f);
            Block(73f, -6f, 98f, 0f);

            // Moving platform lifts you over a tall wall
            var lift = Put(MakeMovingPlatform(3f), 81f, 0.6f);
            var mover = lift.GetComponent<MovingPlatform>();
            mover.waypoints = new[] { Vector2.zero, new Vector2(0f, 8.6f) };
            mover.waitAtEachPoint = 0.8f;
            mover.speed = 3f;
            Block(84f, 0f, 86f, 9f, WallColor, "Tall Wall");
            Spawn<EnergyUpgradePickup>("EnergyUpgrade", 85f, 10f, "a1_energy_upgrade");
            OneWay(88f, 91f, 3f);                     // steps back over the wall when returning
            OneWay(86f, 88.5f, 6.2f);

            Spawn<FlyerEnemy>("Flyer", 76f, 7f);
            Spawn<WalkerEnemy>("Walker", 92f, 0f);

            // Door to Area2
            Wall(98f, -6f, 100f, 22f);
            Wall(97f, 4.5f, 98f, 20f);
            var door = Put(MakeDoor(), 97.5f, 0f).GetComponent<RoomTransition>();
            door.doorId = "a1_east";
            door.targetScene = "Area2";
            door.targetDoorId = "a2_west";
            door.exitDirection = ExitDirection.Left;
            door.spawnOffset = new Vector2(-2f, 0f);

            SaveScene("Area1");
        }

        // ------------------------------------------------------------------ Area 2

        static void BuildArea2()
        {
            BeginScene(new Color(0.12f, 0.08f, 0.1f));
            Background(95f, 200f, new Color(0.19f, 0.12f, 0.15f));

            // ---- Room: Gallery (x 96..140) ----
            // Area2 continues Area1's world coordinates (it sits to the right), so the map lines up.
            RoomArea("a2_room_gallery", "Echoing Gallery", 98f, -6f, 140f, 22f, AudioIds.MusicArea2, AudioIds.AmbienceArea2);
            Wall(96f, -6f, 98f, 22f);
            Block(98f, -6f, 140f, 0f);
            Wall(96f, 20f, 140f, 22f);
            Wall(98f, 4.5f, 99f, 20f);

            var door = Put(MakeDoor(), 98.5f, 0f).GetComponent<RoomTransition>();
            door.doorId = "a2_west";
            door.targetScene = "Area1";
            door.targetDoorId = "a1_east";
            door.exitDirection = ExitDirection.Right;
            door.spawnOffset = new Vector2(2.5f, 0f);

            var spawn = new GameObject("Spawn - a2_start").AddComponent<SpawnPoint>();
            spawn.spawnId = "a2_start";
            spawn.transform.position = new Vector3(102f, 0f, 0f);

            Spawn<SaveStation>("SaveStation", 105f, 0f, "a2_save");
            var galleryShrine = Spawn<FastTravelPoint>("FastTravel", 113f, 0f, "a2_travel_gallery");
            galleryShrine.pointName = "Gallery Shrine";
            Record(galleryShrine);

            // A one-time story moment when you first walk in
            var cutsceneGo = new GameObject("Cutscene - Gallery Voice");
            cutsceneGo.transform.position = new Vector3(110f, 3f, 0f);
            var area = cutsceneGo.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(2f, 6f);
            var cutscene = cutsceneGo.AddComponent<CutsceneTrigger>();
            cutscene.area = area;
            cutscene.dialogue = new[]
            {
                Line("???", "...another one climbs down into the dark."),
                Line("???", "The Brute guards the way forward. Find the key, if you must meet it."),
            };
            SetId(cutsceneGo, "a2_cutscene_voice");

            Block(116f, 0f, 122f, 4f);
            Spawn<ShooterEnemy>("Shooter", 119f, 4f);

            OneWay(125f, 128f, 3f);
            var crumble = Put(MakeCrumbling(3f), 131.5f, 6.2f); // falls apart shortly after you land
            crumble.name = "CrumblingPlatform";
            OneWay(125f, 128f, 9.4f);
            var key = Spawn<KeyItemPickup>("KeyItem", 126.5f, 10.4f, "a2_key_arena");
            key.itemId = "key_arena";
            key.displayName = "Arena Key";
            Record(key);
            Spawn<FlyerEnemy>("Flyer", 124f, 13f);

            Wall(139f, 5f, 140f, 20f);
            var keyGate = GateAt(139.5f, 0f, 5f, "a2_key_door");
            var keyDoor = keyGate.gameObject.AddComponent<KeyDoor>();
            keyDoor.requiredItemId = "key_arena";
            keyDoor.lockedMessage = "Locked. There must be a key somewhere.";
            keyDoor.promptOffset = new Vector2(-1f, 2.2f);
            keyDoor.radius = 2f;

            // ---- Room: Arena (x 140..172) ----
            RoomArea("a2_room_arena", "Brute's Arena", 140f, -6f, 172f, 20f, AudioIds.MusicArea2, AudioIds.AmbienceArea2);
            Block(140f, -6f, 172f, 0f);
            Wall(140f, 18f, 172f, 20f);
            Wall(140f, 5f, 141f, 18f);
            Wall(171f, 5f, 172f, 18f);
            var entrance = GateAt(140.5f, 0f, 5f, "a2_arena_gate_in", true, false);
            var exit = GateAt(171.5f, 0f, 5f, "a2_arena_gate_out", false, false);

            var boss = Spawn<BruteBoss>("Brute", 163f, 0f);
            var wallJump = Spawn<AbilityPickup>("AbilityPickup", 156f, 1.2f, "a2_ability_walljump");
            wallJump.ability = AbilityType.WallJump;
            Record(wallJump);

            var arenaGo = new GameObject("BossArena");
            arenaGo.transform.position = new Vector3(157f, 8.5f, 0f);
            var arenaBox = arenaGo.AddComponent<BoxCollider2D>();
            arenaBox.isTrigger = true;
            arenaBox.size = new Vector2(26f, 17f);
            var arena = arenaGo.AddComponent<BossArena>();
            arena.boss = boss;
            arena.gates = new[] { entrance, exit };
            arena.activateOnDefeat = new[] { wallJump.gameObject };
            arena.victoryToast = "The Brute has fallen";
            SetId(arenaGo, "a2_arena");

            // ---- Room: Shaft (wall-jump climb) ----
            RoomArea("a2_room_shaft", "Shaft", 172f, -6f, 181f, 36f, AudioIds.MusicArea2, AudioIds.AmbienceArea2);
            Block(172f, -6f, 181f, 0f);
            Wall(172f, 5f, 174f, 34f);
            Wall(179f, 0f, 181f, 22f);
            Wall(172f, 34f, 198f, 36f);
            OneWay(175f, 178f, 12f);

            // ---- Room: Summit ----
            RoomArea("a2_room_summit", "Summit", 181f, 22f, 198f, 36f, AudioIds.MusicArea2, AudioIds.AmbienceArea2);
            Block(179f, 22f, 196f, 26f);
            Wall(196f, 22f, 198f, 36f);
            var doubleJump = Spawn<AbilityPickup>("AbilityPickup", 186f, 27.2f, "a2_ability_doublejump");
            doubleJump.ability = AbilityType.DoubleJump;
            Record(doubleJump);
            var glide = Spawn<AbilityPickup>("AbilityPickup", 192f, 27.2f, "a2_ability_glide");
            glide.ability = AbilityType.Glide;
            Record(glide);

            // End of the demo: walking to the far end of the summit rolls the credits.
            var endingGo = new GameObject("GameEnding");
            endingGo.transform.position = new Vector3(195f, 28f, 0f);
            var endArea = endingGo.AddComponent<BoxCollider2D>();
            endArea.isTrigger = true;
            endArea.size = new Vector2(2f, 4f);
            var ending = endingGo.AddComponent<GameEnding>();
            ending.area = endArea;
            ending.finalDialogue = new[]
            {
                Line("???", "So you made it to the top."),
                Line("???", "This is as far as the world goes... for now."),
            };

            SaveScene("Area2");
        }
    }
}
