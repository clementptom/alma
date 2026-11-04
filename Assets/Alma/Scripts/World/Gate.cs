using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A solid door that slides open. It can be opened by a Lever, a KeyDoor, a BossArena,
    /// or by a save flag (for example a boss defeated in another scene).
    /// Once opened by a lever or key, it stays open.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class Gate : MonoBehaviour
    {
        public bool startsOpen = false;
        [Tooltip("Save the open state (levers, keys). Turn off for boss-arena gates.")]
        public bool rememberWhenOpened = true;
        [Tooltip("Optional: open automatically when this save flag is set.")]
        public string openWhenFlagSet = "";

        [Header("Motion")]
        [Tooltip("The part that moves. Defaults to this object (its collider moves with it).")]
        public Transform movingPart;
        public Vector2 openOffset = new Vector2(0f, 3.5f);
        public float moveTime = 0.35f;

        public bool IsOpen { get; private set; }

        PersistentId pid;
        Vector3 closedLocalPosition;
        Coroutine motion;

        const string OpenFlag = "open";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            if (movingPart == null) movingPart = transform;
            closedLocalPosition = movingPart.localPosition;
        }

        void Start()
        {
            var gm = GameManager.Instance;
            bool open = startsOpen
                        || (rememberWhenOpened && pid.IsFlagSet(OpenFlag))
                        || (gm != null && !string.IsNullOrEmpty(openWhenFlagSet) && gm.HasFlag(openWhenFlagSet));
            SetOpenImmediate(open);
            GameEvents.FlagSet += OnFlagSet;
        }

        void OnDestroy()
        {
            GameEvents.FlagSet -= OnFlagSet;
        }

        void OnFlagSet(string flag)
        {
            if (!string.IsNullOrEmpty(openWhenFlagSet) && flag == openWhenFlagSet) Open();
        }

        public void Open(bool save = true)
        {
            if (IsOpen) return;
            IsOpen = true;
            if (save && rememberWhenOpened) pid.SetFlag(OpenFlag);
            AudioManager.PlaySfx(AudioIds.GateOpen);
            CameraController.Shake(0.1f, 0.3f);
            Animate();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            AudioManager.PlaySfx(AudioIds.GateClose);
            CameraController.Shake(0.15f, 0.2f);
            Animate();
        }

        public void SetOpenImmediate(bool open)
        {
            IsOpen = open;
            if (motion != null) StopCoroutine(motion);
            if (movingPart == null) movingPart = transform;
            movingPart.localPosition = closedLocalPosition + (open ? (Vector3)openOffset : Vector3.zero);
        }

        void Animate()
        {
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(MoveRoutine(closedLocalPosition + (IsOpen ? (Vector3)openOffset : Vector3.zero)));
        }

        IEnumerator MoveRoutine(Vector3 target)
        {
            Vector3 start = movingPart.localPosition;
            float t = 0f;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, moveTime <= 0f ? 1f : t / moveTime);
                movingPart.localPosition = Vector3.Lerp(start, target, k);
                yield return null;
            }
            movingPart.localPosition = target;
            motion = null;
        }
    }
}
