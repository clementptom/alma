using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Spawns one-shot visual effects by ID: EffectsManager.Spawn(EffectIds.HitSpark, position).
    /// Unassigned IDs do nothing, so gameplay code can call this before the art exists.
    /// </summary>
    public static class EffectsManager
    {
        static EffectsLibrary library;

        static EffectsLibrary Library
        {
            get
            {
                if (library == null)
                {
                    library = Resources.Load<EffectsLibrary>("EffectsLibrary");
                    if (library == null) library = ScriptableObject.CreateInstance<EffectsLibrary>();
                }
                return library;
            }
        }

        public static GameObject Spawn(string id, Vector2 position, bool flipX = false)
        {
            return Spawn(id, position, 0f, flipX, null);
        }

        public static GameObject Spawn(string id, Vector2 position, float angleDegrees, bool flipX, Transform parent)
        {
            var entry = Library.Find(id);
            if (entry == null || entry.prefab == null) return null;

            var go = Object.Instantiate(entry.prefab, position, Quaternion.Euler(0f, 0f, angleDegrees));
            if (parent != null) go.transform.SetParent(parent, true);
            if (flipX)
            {
                Vector3 s = go.transform.localScale;
                s.x = -Mathf.Abs(s.x);
                go.transform.localScale = s;
            }
            if (entry.lifetime > 0f) Object.Destroy(go, entry.lifetime);
            return go;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => library = null;
    }
}
