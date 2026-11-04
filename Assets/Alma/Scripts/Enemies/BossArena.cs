using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Boss fight controller. When the player enters the box, the gates close, boss music
    /// starts and the boss wakes. On victory the gates open, rewards appear, and the
    /// defeat is saved, so the boss stays dead on later visits.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PersistentId))]
    public class BossArena : MonoBehaviour
    {
        public Boss boss;
        [Tooltip("Gates that close during the fight. Set them to 'Starts Open'.")]
        public Gate[] gates = new Gate[0];
        [Tooltip("Enabled after victory (e.g. an ability pickup). Disabled until then.")]
        public GameObject[] activateOnDefeat = new GameObject[0];
        [Tooltip("Disabled after victory (e.g. a cutscene trigger).")]
        public GameObject[] deactivateOnDefeat = new GameObject[0];
        public float delayBeforeWake = 0.6f;
        public float rewardDelay = 1.5f;
        public string victoryToast = "";

        public bool Started { get; private set; }
        public bool Finished { get; private set; }

        PersistentId pid;
        Rect area;

        const string DefeatedFlag = "defeated";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            var box = GetComponent<BoxCollider2D>();
            area = RectUtil.FromBox(box);
            box.enabled = false;
        }

        void Start()
        {
            if (pid.IsFlagSet(DefeatedFlag))
            {
                Finished = true;
                if (boss != null) Destroy(boss.gameObject);
                SetActive(activateOnDefeat, true);
                SetActive(deactivateOnDefeat, false);
                foreach (var g in gates)
                    if (g != null) g.SetOpenImmediate(true);
                return;
            }

            SetActive(activateOnDefeat, false);
            if (boss != null) boss.Defeated += OnBossDefeated;
        }

        void OnDestroy()
        {
            if (boss != null) boss.Defeated -= OnBossDefeated;
        }

        void Update()
        {
            if (Started || Finished || boss == null) return;
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (player == null || (gm != null && gm.State != GameState.Playing)) return;
            if (area.Contains(player.Center)) StartCoroutine(StartFight());
        }

        IEnumerator StartFight()
        {
            Started = true;
            foreach (var g in gates)
                if (g != null) g.Close();
            AudioManager.StopMusic(0.4f);
            yield return new WaitForSeconds(delayBeforeWake);
            if (boss == null) yield break;
            AudioManager.PlayMusic(boss.bossMusic, 0.3f);
            boss.WakeUp();
        }

        void OnBossDefeated()
        {
            Finished = true;
            pid.SetFlag(DefeatedFlag);
            StartCoroutine(Victory());
        }

        IEnumerator Victory()
        {
            AudioManager.StopMusic(1f);
            yield return new WaitForSeconds(rewardDelay);
            foreach (var g in gates)
                if (g != null) g.Open(false);
            SetActive(activateOnDefeat, true);
            SetActive(deactivateOnDefeat, false);
            if (!string.IsNullOrEmpty(victoryToast)) GameEvents.RaiseToast(victoryToast);
            AudioManager.PlaySfx(AudioIds.JingleVictory);
            RoomTracker.ReapplyRoomAudio();
        }

        static void SetActive(GameObject[] objects, bool active)
        {
            foreach (var go in objects)
                if (go != null) go.SetActive(active);
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? area : RectUtil.FromBox(box);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
