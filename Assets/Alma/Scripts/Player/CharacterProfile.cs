using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Who the player is in a chapter, and what they can do. The prologue uses young
    /// Sisnando (no combat, no lockpicking, no dog); the rest of the game uses Corvin.
    /// Create via Assets > Create > Alma > Character Profile, and pick it in the scene's
    /// ChapterSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Alma/Character Profile", fileName = "CharacterProfile")]
    public class CharacterProfile : ScriptableObject
    {
        public string characterId = "corvin";
        public string displayName = "Corvin";

        [Header("Look")]
        [Tooltip("Animator Controller with this character's animations. Empty keeps the prefab's.")]
        public RuntimeAnimatorController animator;
        [Tooltip("Tint for the grey placeholder body (ignored once real art is in).")]
        public Color placeholderColor = new Color(0.95f, 0.85f, 0.55f);

        [Header("Abilities")]
        public bool canAttack = true;
        public bool canParry = true;
        public bool canDodge = true;
        public bool canHeal = true;
        public bool canPickLocks = true;
        public bool canThrowStones = true;
        [Tooltip("Barro can join this character.")]
        public bool hasCompanion = true;

        [Header("Stats")]
        public float speedMultiplier = 1f;
        [Tooltip("0 uses the save file's max health.")]
        public int maxHealthOverride = 0;

        static CharacterProfile defaultProfile;

        /// <summary>Everything allowed: used in scenes without ChapterSettings (the sandbox levels).</summary>
        public static CharacterProfile Default
        {
            get
            {
                if (defaultProfile == null)
                {
                    defaultProfile = CreateInstance<CharacterProfile>();
                    defaultProfile.name = "Default Profile";
                }
                return defaultProfile;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => defaultProfile = null;
    }
}
