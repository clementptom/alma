using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Alma
{
    /// <summary>
    /// Something Barro can find with the TRACK command: a scent trail, a hidden door, a
    /// buried person, an item. Barro runs to it, sniffs, barks, and onFound fires (open a
    /// passage, reveal a pickup, continue a story sequence...).
    /// Set refuseToTrack for trails he won't follow (he sits and howls instead).
    /// </summary>
    public class BarroPointOfInterest : MonoBehaviour
    {
        public enum Kind
        {
            Scent,
            HiddenPassage,
            Person,
            Item
        }

        public Kind kind = Kind.Scent;
        [Tooltip("Barro tracks this from up to this far away.")]
        public float trackRange = 20f;
        [Tooltip("Only found once (remembered in the save if a PersistentId is attached).")]
        public bool once = true;
        [Tooltip("Barro refuses this trail: he sits and howls (story beat).")]
        public bool refuseToTrack = false;
        public string foundMessage = "";
        [Tooltip("Objects enabled when Barro finds this (a hidden door, a buried item...).")]
        public GameObject[] revealOnFound = new GameObject[0];
        public UnityEvent onFound = new UnityEvent();

        static readonly List<BarroPointOfInterest> active = new List<BarroPointOfInterest>();
        public static IReadOnlyList<BarroPointOfInterest> Active => active;

        public bool Found { get; private set; }

        PersistentId pid;
        const string FoundFlag = "found";

        void Awake()
        {
            pid = GetComponent<PersistentId>();
        }

        void Start()
        {
            if (once && pid != null && pid.IsFlagSet(FoundFlag))
            {
                Found = true;
                Reveal();
            }
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public bool Available => !(once && Found);

        public void MarkFound()
        {
            if (!Available) return;
            Found = true;
            if (once && pid != null) pid.SetFlag(FoundFlag);
            Reveal();
            if (!string.IsNullOrEmpty(foundMessage)) GameEvents.RaiseToast(foundMessage);
            onFound.Invoke();
        }

        void Reveal()
        {
            foreach (var go in revealOnFound)
                if (go != null) go.SetActive(true);
        }

        /// <summary>The nearest available point within its track range of a position.</summary>
        public static BarroPointOfInterest Nearest(Vector2 from)
        {
            BarroPointOfInterest best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in active)
            {
                if (p == null || !p.Available) continue;
                float d = Vector2.Distance(from, p.transform.position);
                if (d > p.trackRange || d >= bestDistance) continue;
                best = p;
                bestDistance = d;
            }
            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawWireSphere(transform.position, trackRange);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
