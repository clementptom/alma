using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Per-scene settings: which character the player is, whether Barro can be with them,
    /// and the chapter's name. Put one in every story scene.
    /// </summary>
    public class ChapterSettings : MonoBehaviour
    {
        public static ChapterSettings Current { get; private set; }

        public CharacterProfile character;
        public string chapterTitle = "";
        [Tooltip("Barro follows the player here (once he has joined).")]
        public bool allowCompanion = true;
        [Tooltip("Music to start when the scene loads (rooms can override it).")]
        public string music = "";

        public CharacterProfile Character => character != null ? character : CharacterProfile.Default;

        void Awake()
        {
            Current = this;
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(music)) AudioManager.PlayMusic(music);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;
    }
}
