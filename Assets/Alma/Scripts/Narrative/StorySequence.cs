using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace Alma
{
    public enum SequenceTrigger
    {
        PlayerEntersArea,
        SceneStart,
        /// <summary>Started by a StoryInteractable, a UnityEvent, or another sequence.</summary>
        Manual,
        /// <summary>Starts as soon as requiredFlag is set (e.g. after picking up an item).</summary>
        WhenFlagSet
    }

    public enum SequenceStepType
    {
        Dialogue,
        TitleCard,
        FadeOut,
        FadeIn,
        Wait,
        PlaySound,
        PlayMusic,
        StopMusic,
        PlayAmbience,
        SetActive,
        MoveActor,
        TeleportPlayer,
        FacePlayer,
        SetFlag,
        GiveItem,
        CameraShake,
        Mash,
        Hint,
        Video,
        Timeline,
        CompanionJoin,
        CompanionWalk,
        CompanionAnimation,
        StartBossFight,
        Save,
        LoadScene,
        EndDemo,
        Event
    }

    /// <summary>One step of a StorySequence. Only the fields its type uses matter.</summary>
    [Serializable]
    public class SequenceStep
    {
        public SequenceStepType type = SequenceStepType.Dialogue;

        [Tooltip("Dialogue: the lines to play.")]
        public DialogueLine[] lines = new DialogueLine[0];
        [Tooltip("Dialogue: default speaker. TitleCard: title. Hint/Mash: the text. CompanionAnimation: state name. FacePlayer: -1 left, 1 right.")]
        public string text = "";
        [Tooltip("TitleCard: subtitle.")]
        public string subtitle = "";
        [Tooltip("Sound / music / ambience / flag / item / scene id, depending on the step.")]
        public string id = "";
        [Tooltip("LoadScene: spawn point id in the new scene.")]
        public string spawnId = "";

        [Tooltip("Seconds (TitleCard, Fade, Wait, StopMusic fade, CameraShake, CompanionAnimation).")]
        public float duration = 1f;
        [Tooltip("CameraShake: strength.")]
        public float amount = 0.3f;
        [Tooltip("FadeOut: colour (black or white).")]
        public Color color = Color.black;
        [Tooltip("Mash: presses needed.")]
        public int count = 10;

        [Tooltip("SetActive: object to show/hide. StartBossFight: the BossArena.")]
        public GameObject target;
        public bool active = true;
        [Tooltip("MoveActor: who walks.")]
        public Transform actor;
        [Tooltip("MoveActor / TeleportPlayer / CompanionWalk: where to.")]
        public Transform destination;
        public float speed = 3f;
        [Tooltip("MoveActor: wait until the actor arrives.")]
        public bool wait = true;

        public VideoClip video;
        public PlayableDirector timeline;
        public UnityEvent onStep = new UnityEvent();
    }

    /// <summary>
    /// A scripted story beat made of steps (dialogue, title cards, fades, walking NPCs,
    /// items, flags, scene loads...), authored in the inspector. Player control is off while
    /// it runs. Each sequence plays once by default (remembered in the save).
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class StorySequence : MonoBehaviour
    {
        public SequenceTrigger trigger = SequenceTrigger.PlayerEntersArea;
        [Tooltip("PlayerEntersArea: the area. Uses this object's BoxCollider2D if empty.")]
        public BoxCollider2D area;
        [Tooltip("Only plays once this save flag is set (and, for WhenFlagSet, starts when it is).")]
        public string requiredFlag = "";
        [Tooltip("Also requires every one of these flags (e.g. roof_1, roof_2, roof_3).")]
        public string[] requiredFlagsAll = new string[0];
        [Tooltip("Never plays if this flag is set.")]
        public string blockedByFlag = "";
        public bool playOnce = true;
        public bool freezePlayer = true;
        [Tooltip("Hide the HUD while this plays.")]
        public bool hideHud = false;

        public SequenceStep[] steps = new SequenceStep[0];
        public UnityEvent onFinished = new UnityEvent();

        public bool IsPlaying { get; private set; }
        public bool HasPlayed => played;

        PersistentId pid;
        Rect areaRect;
        bool hasArea;
        bool played;
        bool loadedScene;

        const string PlayedFlag = "played";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (area == null) area = GetComponent<BoxCollider2D>();
            if (area != null)
            {
                areaRect = RectUtil.FromBox(area);
                area.enabled = false;
                hasArea = true;
            }
        }

        void Start()
        {
            if (playOnce && pid.IsFlagSet(PlayedFlag)) played = true;
            if (trigger == SequenceTrigger.SceneStart) StartCoroutine(PlayWhenReady());
        }

        IEnumerator PlayWhenReady()
        {
            while (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading) yield return null;
            yield return null;
            Play();
        }

        void Update()
        {
            if (IsPlaying || (playOnce && played)) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            if (trigger == SequenceTrigger.WhenFlagSet)
            {
                if (ConditionsMet()) Play();
                return;
            }
            if (trigger != SequenceTrigger.PlayerEntersArea || !hasArea) return;
            var player = PlayerController.Instance;
            if (player != null && areaRect.Contains(player.Center)) Play();
        }

        bool ConditionsMet()
        {
            var gm = GameManager.Instance;
            if (!string.IsNullOrEmpty(requiredFlag) && (gm == null || !gm.HasFlag(requiredFlag))) return false;
            if (requiredFlagsAll != null)
            {
                foreach (var flag in requiredFlagsAll)
                    if (!string.IsNullOrEmpty(flag) && (gm == null || !gm.HasFlag(flag))) return false;
            }
            if (!string.IsNullOrEmpty(blockedByFlag) && gm != null && gm.HasFlag(blockedByFlag)) return false;
            return true;
        }

        public void Play()
        {
            if (IsPlaying || (playOnce && played) || !ConditionsMet()) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            IsPlaying = true;
            played = true;
            if (playOnce) pid.SetFlag(PlayedFlag);

            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            var player = PlayerController.Instance;
            if (gm != null) gm.SetState(GameState.Cutscene);
            if (freezePlayer && player != null) player.Freeze();
            if (hideHud && ui != null) ui.SetHudVisible(false);

            foreach (var step in steps)
            {
                if (step == null) continue;
                yield return RunStep(step);
                if (loadedScene) yield break; // this object is going away with its scene
            }

            if (freezePlayer && player != null) player.Unfreeze();
            if (hideHud && ui != null) ui.SetHudVisible(true);
            if (gm != null && gm.State == GameState.Cutscene) gm.SetState(GameState.Playing);
            IsPlaying = false;
            onFinished.Invoke();
        }

        IEnumerator RunStep(SequenceStep step)
        {
            var gm = GameManager.Instance;
            var ui = UIManager.Instance;
            var player = PlayerController.Instance;

            switch (step.type)
            {
                case SequenceStepType.Dialogue:
                    if (step.lines != null && step.lines.Length > 0)
                    {
                        bool done = false;
                        DialogueRunner.Play(step.lines, step.text, () => done = true);
                        while (!done) yield return null;
                    }
                    break;

                case SequenceStepType.TitleCard:
                    if (ui != null) yield return ui.TitleCard.Show(step.text, step.subtitle, Mathf.Max(0.5f, step.duration));
                    break;

                case SequenceStepType.FadeOut:
                    if (ui != null)
                    {
                        ui.Fader.SetColor(step.color);
                        yield return ui.Fader.Fade(1f, step.duration);
                    }
                    break;

                case SequenceStepType.FadeIn:
                    if (ui != null) yield return ui.Fader.Fade(0f, step.duration);
                    break;

                case SequenceStepType.Wait:
                    yield return new WaitForSecondsRealtime(step.duration);
                    break;

                case SequenceStepType.PlaySound:
                    AudioManager.PlaySfx(step.id);
                    break;

                case SequenceStepType.PlayMusic:
                    AudioManager.PlayMusic(step.id, step.duration);
                    break;

                case SequenceStepType.StopMusic:
                    AudioManager.StopMusic(step.duration);
                    break;

                case SequenceStepType.PlayAmbience:
                    AudioManager.PlayAmbience(step.id);
                    break;

                case SequenceStepType.SetActive:
                    if (step.target != null) step.target.SetActive(step.active);
                    break;

                case SequenceStepType.MoveActor:
                    if (step.actor != null && step.destination != null)
                    {
                        if (step.wait) yield return MoveActor(step.actor, step.destination.position, step.speed);
                        else StartCoroutine(MoveActor(step.actor, step.destination.position, step.speed));
                    }
                    break;

                case SequenceStepType.TeleportPlayer:
                    if (player != null && step.destination != null)
                    {
                        player.Teleport(step.destination.position);
                        if (RoomTracker.Instance != null) RoomTracker.Instance.Refresh(true);
                        else if (CameraController.Main != null) CameraController.Main.Snap();
                        if (BarroCompanion.Instance != null && BarroCompanion.Instance.Current != BarroCompanion.Mode.Scripted)
                            BarroCompanion.Instance.PlaceNear(player);
                    }
                    break;

                case SequenceStepType.FacePlayer:
                    if (player != null) player.SetFacing(step.text == "-1" ? -1 : 1);
                    break;

                case SequenceStepType.SetFlag:
                    if (gm != null) gm.SetFlag(step.id);
                    break;

                case SequenceStepType.GiveItem:
                    if (gm != null && !string.IsNullOrEmpty(step.id))
                    {
                        gm.AddKeyItem(step.id);
                        GameEvents.RaiseToast("Obtained: " + ItemDatabase.ItemName(step.id));
                        AudioManager.PlaySfx(AudioIds.PickupItem);
                    }
                    break;

                case SequenceStepType.CameraShake:
                    CameraController.Shake(step.amount, step.duration);
                    break;

                case SequenceStepType.Mash:
                    if (ui != null) yield return ui.Mash.Run(step.text, step.count, step.id);
                    break;

                case SequenceStepType.Hint:
                    GameEvents.RaiseHint(TutorialHint.Format(step.text));
                    break;

                case SequenceStepType.Video:
                    if (step.video != null && ui != null)
                    {
                        bool done = false;
                        ui.Cinematic.Play(step.video, true, () => done = true);
                        while (!done) yield return null;
                    }
                    break;

                case SequenceStepType.Timeline:
                    if (step.timeline != null)
                    {
                        step.timeline.Play();
                        yield return null;
                        while (step.timeline != null && step.timeline.state == PlayState.Playing) yield return null;
                    }
                    break;

                case SequenceStepType.CompanionJoin:
                    if (gm != null)
                    {
                        gm.SetFlag(CompanionFlags.Joined);
                        gm.RefreshCompanion();
                    }
                    break;

                case SequenceStepType.CompanionWalk:
                    var barro = BarroCompanion.Instance;
                    if (barro != null)
                    {
                        if (step.destination == null)
                        {
                            barro.EndScript();
                        }
                        else
                        {
                            barro.Script(step.destination.position);
                            if (step.wait)
                            {
                                float t = 0f;
                                while (t < 8f && Vector2.Distance(barro.Position, step.destination.position) > 0.6f)
                                {
                                    t += Time.deltaTime;
                                    yield return null;
                                }
                            }
                        }
                    }
                    break;

                case SequenceStepType.CompanionAnimation:
                    if (BarroCompanion.Instance != null) BarroCompanion.Instance.PlayAnimation(step.text, step.duration);
                    if (!string.IsNullOrEmpty(step.id)) AudioManager.PlaySfx(step.id);
                    break;

                case SequenceStepType.StartBossFight:
                    var arena = step.target != null ? step.target.GetComponent<BossArena>() : null;
                    if (arena != null) arena.BeginFight();
                    break;

                case SequenceStepType.Save:
                    if (gm != null) gm.Save();
                    break;

                case SequenceStepType.LoadScene:
                    if (SceneLoader.Instance != null && !string.IsNullOrEmpty(step.id))
                    {
                        loadedScene = true;
                        if (freezePlayer && player != null) player.Unfreeze();
                        if (hideHud && ui != null) ui.SetHudVisible(true);
                        SceneLoader.Instance.Load(step.id, step.spawnId);
                    }
                    break;

                case SequenceStepType.EndDemo:
                    loadedScene = true;
                    if (gm != null)
                    {
                        gm.SetFlag("demo_complete");
                        gm.Save();
                        gm.QuitToMainMenu();
                    }
                    break;

                case SequenceStepType.Event:
                    step.onStep.Invoke();
                    break;
            }
        }

        static IEnumerator MoveActor(Transform actor, Vector2 to, float speed)
        {
            var visual = actor.Find("Visual");
            var animator = visual != null ? visual.GetComponent<Animator>() : null;
            PlayIfPresent(animator, AnimStates.Move);
            float timeout = 20f;
            while (actor != null && Vector2.Distance(actor.position, to) > 0.05f && timeout > 0f)
            {
                Vector2 next = Vector2.MoveTowards(actor.position, to, speed * Time.deltaTime);
                if (visual != null && Mathf.Abs(next.x - actor.position.x) > 0.0001f)
                {
                    Vector3 s = visual.localScale;
                    s.x = Mathf.Abs(s.x) * (next.x > actor.position.x ? 1f : -1f);
                    visual.localScale = s;
                }
                actor.position = next;
                timeout -= Time.deltaTime;
                yield return null;
            }
            PlayIfPresent(animator, AnimStates.Idle);
        }

        static void PlayIfPresent(Animator animator, string state)
        {
            if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled) return;
            int hash = Animator.StringToHash(state);
            if (animator.HasState(0, hash)) animator.Play(hash, 0);
        }

        void OnDrawGizmos()
        {
            var box = area != null ? area : GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? areaRect : RectUtil.FromBox(box);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
