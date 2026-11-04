using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A drowned corpse in the water channels. It turns its head to follow the player long
    /// before it attacks. If the player wades in, or makes noise near the edge, it rises and
    /// grabs them, pulling them under (back to the last safe ground).
    /// Fight it from the walkways, or sneak past.
    /// </summary>
    [RequireComponent(typeof(StealthPerception))]
    public class DrownedOne : Enemy
    {
        public float watchRange = 10f;
        [Tooltip("Horizontal reach of the grab.")]
        public float grabRange = 1.8f;
        public int grabDamage = 1;
        public float windup = 0.55f;
        public float riseDepth = 0.9f;
        [Tooltip("A grab sends the player back to safe ground, like a hazard.")]
        public bool pullsUnderwater = true;

        StealthPerception perception;
        Vector3 visualBase;
        bool busy;
        float raised;

        protected override void Awake()
        {
            base.Awake();
            perception = GetComponent<StealthPerception>();
            perception.facing = () => Facing;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
            if (visual != null) visualBase = visual.localPosition;
            knockbackResistance = 1f;
            Health.Damaged += _ => { if (Player != null) perception.Alert(Player.Center); };
        }

        protected override void Tick()
        {
            var player = Player;
            if (player == null || busy) return;
            float distance = Vector2.Distance(player.Center, Center);
            // The horror beat: they watch you.
            if (distance <= watchRange) Facing = player.Center.x >= Center.x ? 1 : -1;

            float dx = Mathf.Abs(player.Center.x - Center.x);
            bool inReach = dx <= grabRange && player.Center.y < Center.y + 3f;
            bool provoked = player.IsInWater || perception.State == Awareness.Alert
                            || (perception.State == Awareness.Suspicious && Vector2.Distance(perception.Target, Center) < grabRange + 2f);
            if (inReach && provoked) StartCoroutine(Grab());
        }

        IEnumerator Grab()
        {
            busy = true;
            AudioManager.PlaySfx(AudioIds.DrownedRise);
            // Tall reach: from below the surface up to a player standing on the plank above.
            yield return Strike(new Vector2(0.4f, 1.4f), new Vector2(grabRange + 0.6f, 3.2f), grabDamage, windup, 0.25f, 0.9f,
                true, 0f, AnimStates.Rise, AnimStates.Attack);
            busy = false;
            perception.ResetAwareness();
        }

        protected override void ModifyStrike(ref DamageInfo info)
        {
            info.respawnAtSafeGround = pullsUnderwater;
        }

        protected override void Update()
        {
            base.Update();
            if (visual == null) return;
            // Mostly submerged; rises while attacking.
            float target = busy ? 1f : 0f;
            raised = Mathf.MoveTowards(raised, target, Time.deltaTime * 3f);
            visual.localPosition = visualBase + Vector3.down * riseDepth * (1f - raised);
        }

        protected override string AnimationState() => AnimStates.Idle;
    }
}
