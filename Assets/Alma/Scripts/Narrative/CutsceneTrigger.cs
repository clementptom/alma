using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace Alma
{
    public enum CutsceneStart
    {
        PlayerEntersArea,
        SceneStart,
        Manual
    }

    /// <summary>
    /// Plays a story moment: a pre-rendered video, a Timeline (PlayableDirector), dialogue,
    /// or any combination, in that order. Player control is disabled while it runs.
    /// Use Manual and call Play() from a UnityEvent (e.g. NPC.onFirstConversationEnd).
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class CutsceneTrigger : MonoBehaviour
    {
        public CutsceneStart startWhen = CutsceneStart.PlayerEntersArea;
        [Tooltip("Area the player must enter (PlayerEntersArea). Uses this object's BoxCollider2D.")]
        public BoxCollider2D area;
        public bool playOnce = true;

        [Header("Content (any combination)")]
        public VideoClip video;
        public bool allowSkip = true;
        public PlayableDirector timeline;
        public DialogueLine[] dialogue = new DialogueLine[0];
        public string dialogueSpeaker = "";

        [Header("Audio")]
        [Tooltip("Music to play during the cutscene. Empty = keep current.")]
        public string musicId = "";

        public UnityEvent onFinished = new UnityEvent();

        public bool IsPlaying { get; private set; }

        PersistentId pid;
        Rect areaRect;
        bool done;

        const string SeenFlag = "seen";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (area == null) area = GetComponent<BoxCollider2D>();
            if (area != null)
            {
                areaRect = RectUtil.FromBox(area);
                area.enabled = false;
            }
            if (timeline != null) timeline.playOnAwake = false;
        }

        void Start()
        {
            if (playOnce && pid.IsFlagSet(SeenFlag))
            {
                done = true;
                return;
            }
            if (startWhen == CutsceneStart.SceneStart) StartCoroutine(PlayAfterLoad());
        }

        IEnumerator PlayAfterLoad()
        {
            while (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading) yield return null;
            Play();
        }

        void Update()
        {
            if (done || IsPlaying || startWhen != CutsceneStart.PlayerEntersArea || area == null) return;
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (player == null || (gm != null && gm.State != GameState.Playing)) return;
            if (areaRect.Contains(player.Center)) Play();
        }

        public void Play()
        {
            if (IsPlaying || (done && playOnce)) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            IsPlaying = true;
            var gm = GameManager.Instance;
            var player = PlayerController.Instance;
            if (gm != null) gm.SetState(GameState.Cutscene);
            if (player != null) player.Freeze();
            if (!string.IsNullOrEmpty(musicId)) AudioManager.PlayMusic(musicId);

            if (video != null && UIManager.Instance != null)
            {
                bool finished = false;
                UIManager.Instance.Cinematic.Play(video, allowSkip, () => finished = true);
                while (!finished) yield return null;
            }

            if (timeline != null)
            {
                timeline.Play();
                yield return null;
                while (timeline != null && timeline.state == PlayState.Playing) yield return null;
            }

            if (dialogue != null && dialogue.Length > 0)
            {
                bool finished = false;
                if (gm != null) gm.SetState(GameState.Playing); // DialogueRunner manages its own state
                DialogueRunner.Play(dialogue, dialogueSpeaker, () => finished = true);
                while (!finished) yield return null;
            }

            if (player != null) player.Unfreeze();
            if (gm != null) gm.SetState(GameState.Playing);
            if (playOnce) pid.SetFlag(SeenFlag);
            done = true;
            IsPlaying = false;
            RoomTracker.ReapplyRoomAudio();
            onFinished.Invoke();
        }

        void OnDrawGizmos()
        {
            var box = area != null ? area : GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? areaRect : RectUtil.FromBox(box);
            Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.7f);
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
