using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Alma.EditorTools
{
    /// <summary>
    /// One-time project setup. Runs automatically when the project opens:
    ///  - creates the physics layers the code needs
    ///  - creates the data assets in Assets/Alma/Resources (GameConfig, AudioLibrary, EffectsLibrary, UISkin)
    /// Everything is also available from the Alma menu.
    /// </summary>
    [InitializeOnLoad]
    public static class AlmaProjectSetup
    {
        public const string RootFolder = "Assets/Alma";
        public const string ResourcesFolder = RootFolder + "/Resources";
        public const string AnimatorFolder = RootFolder + "/Art/Animators";

        static AlmaProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (SessionState.GetBool("Alma.AutoSetupDone", false)) return;
            SessionState.SetBool("Alma.AutoSetupDone", true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!Layers.AllLayersExist()) CreateLayers();
            if (AssetDatabase.LoadAssetAtPath<AudioLibrary>(ResourcesFolder + "/AudioLibrary.asset") == null) CreateDataAssets();
        }

        // ------------------------------------------------------------- Layers

        [MenuItem("Alma/Setup/Create Layers", priority = 100)]
        public static void CreateLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Alma] Couldn't open ProjectSettings/TagManager.asset.");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            var added = new List<string>();

            foreach (var name in Layers.All)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                {
                    if (layers.GetArrayElementAtIndex(i).stringValue == name)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                for (int i = 8; i < layers.arraySize; i++)
                {
                    var element = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(element.stringValue))
                    {
                        element.stringValue = name;
                        added.Add(name);
                        break;
                    }
                }
            }

            if (added.Count > 0)
            {
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[Alma] Created physics layers: " + string.Join(", ", added));
            }

            if (!Layers.AllLayersExist())
                Debug.LogWarning("[Alma] Not all layers could be created (no free layer slots?). Needed: " + string.Join(", ", Layers.All));
        }

        // -------------------------------------------------------- Data assets

        [MenuItem("Alma/Setup/Create or Update Data Assets", priority = 101)]
        public static void CreateDataAssets()
        {
            EnsureFolder(ResourcesFolder);

            LoadOrCreate<GameConfig>(ResourcesFolder + "/GameConfig.asset");
            LoadOrCreate<UISkin>(ResourcesFolder + "/UISkin.asset");

            var audio = LoadOrCreate<AudioLibrary>(ResourcesFolder + "/AudioLibrary.asset");
            foreach (var id in ConstantsOf(typeof(AudioIds)))
            {
                bool isLoop = id.StartsWith("music_") || id.StartsWith("amb_");
                if (isLoop)
                {
                    if (!audio.music.Any(m => m.id == id)) audio.music.Add(new MusicEntry { id = id });
                }
                else if (!audio.sounds.Any(s => s.id == id))
                {
                    audio.sounds.Add(new SoundEntry { id = id });
                }
            }
            EditorUtility.SetDirty(audio);

            var effects = LoadOrCreate<EffectsLibrary>(ResourcesFolder + "/EffectsLibrary.asset");
            foreach (var id in ConstantsOf(typeof(EffectIds)))
            {
                if (!effects.effects.Any(e => e.id == id)) effects.effects.Add(new EffectEntry { id = id });
            }
            EditorUtility.SetDirty(effects);

            AssetDatabase.SaveAssets();
            Debug.Log("[Alma] Data assets are ready in " + ResourcesFolder + " (GameConfig, AudioLibrary, EffectsLibrary, UISkin).");
        }

        static IEnumerable<string> ConstantsOf(System.Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue());
        }

        // ----------------------------------------------- Animator controllers

        /// <summary>
        /// Creates Animator Controllers with every state the code plays, but no clips.
        /// Drag your animation clips into the matching states. Already-existing controllers
        /// only get the missing states added.
        /// </summary>
        [MenuItem("Alma/Setup/Create Animator Controllers", priority = 102)]
        public static Dictionary<string, AnimatorController> CreateAnimatorControllers()
        {
            EnsureFolder(AnimatorFolder);
            var result = new Dictionary<string, AnimatorController>
            {
                ["Player"] = EnsureController("Player", AnimStates.PlayerStates),
                ["Walker"] = EnsureController("Enemy_Walker", AnimStates.EnemyStates),
                ["Flyer"] = EnsureController("Enemy_Flyer", AnimStates.EnemyStates),
                ["Shooter"] = EnsureController("Enemy_Shooter", AnimStates.EnemyStates),
                ["Brute"] = EnsureController("Boss_Brute", AnimStates.BossStates),
                ["NPC"] = EnsureController("NPC", new[] { AnimStates.Idle }),
                ["SaveStation"] = EnsureController("SaveStation", new[] { AnimStates.Idle, AnimStates.Active }),
                ["Lever"] = EnsureController("Lever", new[] { AnimStates.Idle, AnimStates.Active }),
            };
            AssetDatabase.SaveAssets();
            return result;
        }

        static AnimatorController EnsureController(string name, string[] states)
        {
            string path = $"{AnimatorFolder}/{name}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            var machine = controller.layers[0].stateMachine;
            for (int i = 0; i < states.Length; i++)
            {
                string stateName = states[i];
                if (machine.states.Any(s => s.state.name == stateName)) continue;
                var state = machine.AddState(stateName, new Vector3(300f + (i % 4) * 240f, 60f * (i / 4), 0f));
                state.writeDefaultValues = false;
            }

            var idle = machine.states.FirstOrDefault(s => s.state.name == AnimStates.Idle).state;
            if (idle != null) machine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ------------------------------------------------------------ Helpers

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        [MenuItem("Alma/Open Save Folder", priority = 200)]
        static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("Alma/Delete All Save Files", priority = 201)]
        static void DeleteSaves()
        {
            if (!EditorUtility.DisplayDialog("Delete saves", "Delete every Alma save slot (including the debug slot)?", "Delete", "Cancel")) return;
            SaveSystem.Delete(SaveSystem.DebugSlot);
            for (int i = 0; i < SaveSystem.SlotCount; i++) SaveSystem.Delete(i);
            Debug.Log("[Alma] Save files deleted.");
        }
    }
}
