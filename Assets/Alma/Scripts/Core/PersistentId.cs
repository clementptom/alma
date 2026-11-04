using System;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Gives a scene object a unique ID that survives saving and loading. Pickups, gates,
    /// levers, bosses and rooms use it to remember their state. The ID is generated
    /// automatically and regenerated when you duplicate an object.
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistentId : MonoBehaviour
    {
        [SerializeField] string id = "";

        /// <summary>Empty for objects spawned at runtime: those are not persistent.</summary>
        public string Id => id;

        public bool IsValid => !string.IsNullOrEmpty(id);

        /// <summary>Convenience: true if the save data says this object's flag is set.</summary>
        public bool IsFlagSet(string suffix = null)
        {
            var gm = GameManager.Instance;
            return IsValid && gm != null && gm.HasFlag(Key(suffix));
        }

        public void SetFlag(string suffix = null)
        {
            var gm = GameManager.Instance;
            if (IsValid && gm != null) gm.SetFlag(Key(suffix));
        }

        public string Key(string suffix) => string.IsNullOrEmpty(suffix) ? id : id + ":" + suffix;

#if UNITY_EDITOR
        void Reset()
        {
            Regenerate();
        }

        void OnValidate()
        {
            if (Application.isPlaying) return;

            // Prefab assets shouldn't own an ID: every instance gets its own.
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    id = "";
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                return;
            }

            if (string.IsNullOrEmpty(id) || HasDuplicateInScene())
            {
                Regenerate();
            }
        }

        bool HasDuplicateInScene()
        {
            var all = FindObjectsByType<PersistentId>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var other in all)
            {
                // OnValidate runs on the copy when you duplicate an object, so the copy
                // is the one that gets a fresh ID.
                if (other != this && other.id == id && other.gameObject.scene == gameObject.scene)
                {
                    return true;
                }
            }
            return false;
        }

        [ContextMenu("Regenerate ID")]
        void Regenerate()
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 12);
            UnityEditor.EditorUtility.SetDirty(this);
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }

        /// <summary>Used by editor tooling to assign a readable ID.</summary>
        public void EditorSetId(string newId)
        {
            id = newId;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
