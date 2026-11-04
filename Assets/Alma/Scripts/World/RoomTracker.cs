using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Works out which Room the player is in and applies its camera bounds, music,
    /// ambience and map discovery. Lives on the [Systems] object.
    /// </summary>
    public class RoomTracker : MonoBehaviour
    {
        public static RoomTracker Instance { get; private set; }

        public Room Current { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 p = player.Center;
            if (Current != null && Current.isActiveAndEnabled && Current.Contains(p))
            {
                // Still inside, but a smaller nested room may have taken over.
                var nested = Room.FindAt(p);
                if (nested == null || nested == Current) return;
                Enter(nested, false);
                return;
            }

            var room = Room.FindAt(p);
            if (room != null) Enter(room, false);
        }

        /// <summary>Forces a re-check, e.g. right after a scene load or teleport.</summary>
        public void Refresh(bool snapCamera)
        {
            Current = null;
            var player = PlayerController.Instance;
            if (player == null) return;
            var room = Room.FindAt(player.Center);
            if (room != null)
            {
                Enter(room, snapCamera);
            }
            else if (CameraController.Main != null)
            {
                CameraController.Main.SetBounds(null, snapCamera);
            }
        }

        void Enter(Room room, bool snapCamera)
        {
            Current = room;
            if (CameraController.Main != null)
                CameraController.Main.SetBounds(room.clampCamera ? room.WorldRect : (Rect?)null, snapCamera);
            ApplyAudio(room);
            if (GameManager.Instance != null && room.showOnMap) GameManager.Instance.DiscoverRoom(room);
            GameEvents.RaiseRoomEntered(room);
        }

        static void ApplyAudio(Room room)
        {
            if (room == null) return;
            if (!string.IsNullOrEmpty(room.musicId)) AudioManager.PlayMusic(room.musicId);
            if (!string.IsNullOrEmpty(room.ambienceId)) AudioManager.PlayAmbience(room.ambienceId);
        }

        /// <summary>Restarts the current room's music, e.g. after boss music ends.</summary>
        public static void ReapplyRoomAudio()
        {
            if (Instance == null || Instance.Current == null) return;
            var am = AudioManager.Instance;
            if (am != null && am.CurrentMusicId == Instance.Current.musicId)
            {
                // Force a restart if the boss stopped it.
                am.SetMusic(null, 0f);
            }
            ApplyAudio(Instance.Current);
        }
    }
}
