using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Saves the respawn point (without healing) when the player walks through its box.
    /// Used for the prologue's stealth sections and long rooms. Getting caught or dying
    /// restarts from the most recent checkpoint or save station.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PersistentId))]
    public class Checkpoint : MonoBehaviour, ISpawnLocation
    {
        public Vector2 spawnOffset = Vector2.zero;
        public bool showMessage = false;

        PersistentId pid;
        Rect area;
        bool reached;

        public string SpawnId => (pid != null ? pid : GetComponent<PersistentId>()).Id;
        public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;

        void Awake()
        {
            pid = GetComponent<PersistentId>();
            var box = GetComponent<BoxCollider2D>();
            area = RectUtil.FromBox(box);
            box.enabled = false;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing || gm.Data == null) return;
            bool inside = area.Contains(player.Center);
            if (!inside)
            {
                reached = false;
                return;
            }
            if (reached || gm.Data.respawnPointId == SpawnId) return;
            reached = true;
            gm.SaveAtCheckpoint(this);
            if (showMessage) GameEvents.RaiseToast("Checkpoint");
        }

        public void OnPlayerArrived(PlayerController player)
        {
            reached = true;
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Rect r = Application.isPlaying ? area : RectUtil.FromBox(box);
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            Gizmos.DrawWireCube(r.center, r.size);
            Gizmos.DrawWireSphere(SpawnPosition, 0.3f);
        }
    }
}
