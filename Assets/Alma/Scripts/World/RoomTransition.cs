using UnityEngine;

namespace Alma
{
    public enum ExitDirection
    {
        Left = -1,
        Right = 1,
        Up = 2,
        Down = 3
    }

    /// <summary>
    /// A doorway to another scene (or another spot in the same scene). Walking into its
    /// box loads <see cref="targetScene"/> and puts the player at the door whose
    /// <see cref="doorId"/> matches <see cref="targetDoorId"/>.
    ///
    /// Example: Area1's right edge has doorId "a1_east" -> targetScene "Area2", targetDoorId "a2_west".
    ///          Area2's left edge has doorId "a2_west" -> targetScene "Area1", targetDoorId "a1_east".
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomTransition : MonoBehaviour, ISpawnLocation
    {
        [Tooltip("This door's own id, so other doors can lead here.")]
        public string doorId = "door";
        [Tooltip("Scene to load. Leave empty to move within the current scene.")]
        public string targetScene = "";
        public string targetDoorId = "";

        [Header("Arriving here")]
        [Tooltip("Which way the player moves when they come out of this door.")]
        public ExitDirection exitDirection = ExitDirection.Right;
        [Tooltip("Where the player appears, relative to this door.")]
        public Vector2 spawnOffset = new Vector2(1.5f, 0f);

        public string SpawnId => doorId;
        public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;

        BoxCollider2D box;
        float armTimer = 0.4f;
        bool playerWasInside = true;

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (player == null || gm == null || gm.State != GameState.Playing) return;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading) return;

            bool inside = box.OverlapPoint(player.Center);
            if (armTimer > 0f)
            {
                armTimer -= Time.deltaTime;
                playerWasInside = inside;
                return;
            }

            // Only trigger when the player walks in, not when they spawn inside.
            if (inside && !playerWasInside) Use(player);
            playerWasInside = inside;
        }

        void Use(PlayerController player)
        {
            if (SceneLoader.Instance == null) return;
            SceneLoader.Instance.Travel(targetScene, targetDoorId);
        }

        public void OnPlayerArrived(PlayerController player)
        {
            armTimer = 0.4f;
            playerWasInside = true;
            switch (exitDirection)
            {
                case ExitDirection.Left:
                case ExitDirection.Right:
                    player.ForceMove((int)exitDirection, 0.25f);
                    break;
                case ExitDirection.Up:
                    player.Bounce(16f);
                    break;
            }
        }

        void Reset()
        {
            var b = GetComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = new Vector2(1f, 4f);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            var b = GetComponent<BoxCollider2D>();
            if (b != null)
            {
                Rect r = RectUtil.FromBox(b);
                Gizmos.DrawWireCube(r.center, r.size);
            }
            Gizmos.DrawWireSphere(SpawnPosition, 0.3f);
        }
    }
}
