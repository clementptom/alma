using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace Alma
{
    /// <summary>
    /// Ends the game: optional final dialogue, ending video and/or Timeline, then marks the
    /// save as completed, rolls the credits and returns to the title screen.
    /// Trigger it by walking into its box, or call Play() from a UnityEvent
    /// (e.g. CutsceneTrigger.onFinished after the final boss).
    /// </summary>
    public class GameEnding : MonoBehaviour
    {
        /// <summary>Save flag set when the game is finished (shown on the save slot).</summary>
        public const string CompletedFlag = "game_completed";

        public bool triggerOnEnter = true;
        [Tooltip("Area the player walks into. Uses this object's BoxCollider2D if empty.")]
        public BoxCollider2D area;

        [Header("Content (any combination, played in this order)")]
        public DialogueLine[] finalDialogue = new DialogueLine[0];
        public string dialogueSpeaker = "";
        public VideoClip endingVideo;
        public PlayableDirector timeline;

        [Header("Credits")]
        [Tooltip("Overrides GameConfig's credits text for this ending (e.g. a secret ending).")]
        public TextAsset creditsOverride;
        public string creditsMusic = AudioIds.MusicCredits;

        public bool IsPlaying { get; private set; }

        Rect areaRect;

        void Awake()
        {
            if (area == null) area = GetComponent<BoxCollider2D>();
            if (area != null)
            {
                areaRect = RectUtil.FromBox(area);
                area.enabled = false;
            }
            if (timeline != null) timeline.playOnAwake = false;
        }

        void Update()
        {
            if (!triggerOnEnter || IsPlaying || area == null) return;
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;
            if (areaRect.Contains(player.Center)) Play();
        }

        public void Play()
        {
            if (IsPlaying) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            IsPlaying = true;
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            var player = PlayerController.Instance;
            if (gm != null) gm.SetState(GameState.Cutscene);
            if (player != null) player.Freeze();

            if (finalDialogue != null && finalDialogue.Length > 0)
            {
                bool done = false;
                DialogueRunner.Play(finalDialogue, dialogueSpeaker, () => done = true);
                while (!done) yield return null;
            }

            if (endingVideo != null && ui != null)
            {
                bool done = false;
                AudioManager.StopMusic(0.5f);
                ui.Cinematic.Play(endingVideo, true, () => done = true);
                while (!done) yield return null;
            }

            if (timeline != null)
            {
                timeline.Play();
                yield return null;
                while (timeline != null && timeline.state == PlayState.Playing) yield return null;
            }

            if (gm != null)
            {
                gm.SetFlag(CompletedFlag);
                gm.Save();
            }

            if (ui != null)
            {
                bool done = false;
                AudioManager.PlayMusic(creditsMusic, 1f);
                ui.SetHudVisible(false);
                var config = gm != null ? gm.Config : GameConfig.Get();
                TextAsset text = creditsOverride != null ? creditsOverride : config.credits;
                ui.Credits.Play(text != null ? text.text : null, () => done = true);
                while (!done) yield return null;
            }

            if (gm != null) gm.QuitToMainMenu();
        }

        void OnDrawGizmos()
        {
            var box = area != null ? area : GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? areaRect : RectUtil.FromBox(box);
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
