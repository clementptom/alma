using UnityEngine;

namespace Alma
{
    /// <summary>Somewhere the player can be placed after a scene loads: spawn points, doors, save stations.</summary>
    public interface ISpawnLocation
    {
        string SpawnId { get; }
        Vector2 SpawnPosition { get; }
        void OnPlayerArrived(PlayerController player);
    }

    /// <summary>A named spawn location. GameConfig.startSpawnId refers to one of these ("start").</summary>
    public class SpawnPoint : MonoBehaviour, ISpawnLocation
    {
        public string spawnId = "start";
        public int faceDirection = 1;

        public string SpawnId => spawnId;
        public Vector2 SpawnPosition => transform.position;

        public void OnPlayerArrived(PlayerController player)
        {
            player.SetFacing(faceDirection);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * faceDirection);
        }
    }
}
