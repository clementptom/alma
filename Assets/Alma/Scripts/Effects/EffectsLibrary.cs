using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    [Serializable]
    public class EffectEntry
    {
        public string id;
        [Tooltip("Any prefab: a SimpleSpriteAnimator, a ParticleSystem, or both.")]
        public GameObject prefab;
        [Tooltip("Destroyed after this many seconds. 0 means the prefab cleans itself up.")]
        public float lifetime = 1f;
    }

    /// <summary>Maps effect IDs (EffectIds) to prefabs. Lives at Resources/EffectsLibrary.asset.</summary>
    [CreateAssetMenu(menuName = "Alma/Effects Library", fileName = "EffectsLibrary")]
    public class EffectsLibrary : ScriptableObject
    {
        public List<EffectEntry> effects = new List<EffectEntry>();

        Dictionary<string, EffectEntry> lookup;

        public EffectEntry Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null)
            {
                lookup = new Dictionary<string, EffectEntry>();
                foreach (var e in effects)
                {
                    if (e != null && !string.IsNullOrEmpty(e.id)) lookup[e.id] = e;
                }
            }
            lookup.TryGetValue(id, out var entry);
            return entry;
        }

        void OnValidate() => lookup = null;
    }
}
