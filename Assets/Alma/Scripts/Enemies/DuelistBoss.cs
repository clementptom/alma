using System.Collections;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A swordsman duelist (Malaspina, King of the Pass). Teaches the two defensive moves:
    ///   Slash and combos - parryable (pale flash). Parry them to stagger him, then riposte.
    ///   Lunge            - can't be parried (red flash). Dodge-roll through it.
    /// Phase 2 (below half health): faster wind-ups and a three-hit combo.
    /// Shows a tutorial hint the first time each kind of attack comes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class DuelistBoss : Boss
    {
        [Header("Footwork")]
        public float walkSpeed = 2.6f;
        public float preferredDistance = 1.7f;
        public float lungeFromDistance = 3.4f;

        [Header("Blows")]
        public int damage = 1;
        public Vector2 slashOffset = new Vector2(1f, 0.1f);
        public Vector2 slashSize = new Vector2(1.8f, 1.4f);
        public float slashWindup = 0.55f;
        public float comboWindup = 0.32f;
        public float recovery = 0.75f;
        public float lungeWindup = 0.7f;
        public float lungeSpeed = 12f;
        public float lungeTime = 0.35f;
        public float phaseTwoSpeedUp = 0.8f;

        [Header("Tutorial")]
        [TextArea] public string parryHint = "A pale flash means you can parry: press {Parry} just before the blade lands.";
        [TextArea] public string dodgeHint = "A red flash can't be parried. {Dodge} through it.";

        protected override void Awake()
        {
            base.Awake();
            parryStaggerTime = 1.4f;
            currencyDrop = new Vector2Int(15, 25);
        }

        protected override void OnAwake()
        {
            StartCoroutine(Brain());
        }

        IEnumerator Brain()
        {
            while (!IsDead)
            {
                while (stunTimer > 0f && !IsDead) yield return new WaitForFixedUpdate();
                var player = Player;
                if (player == null)
                {
                    yield return Wait(0.3f);
                    continue;
                }

                float pace = Phase == 2 ? phaseTwoSpeedUp : 1f;
                Facing = DirectionToPlayer();
                float distance = Mathf.Abs(player.Center.x - Center.x);

                if (distance > lungeFromDistance && Random.value < 0.6f)
                {
                    Hint("hint:dodge", dodgeHint);
                    yield return Strike(slashOffset, slashSize, damage, lungeWindup * pace, lungeTime, recovery * pace, false, lungeSpeed,
                        AnimStates.Telegraph, AnimStates.Lunge);
                }
                else if (distance > preferredDistance + 0.3f)
                {
                    yield return Approach(0.6f);
                    continue;
                }
                else
                {
                    Hint("hint:parry", parryHint);
                    int hits = Phase == 2 && Random.value < 0.5f ? 3 : Random.value < 0.45f ? 2 : 1;
                    for (int i = 0; i < hits && !IsDead && stunTimer <= 0f; i++)
                    {
                        float windup = (i == 0 ? slashWindup : comboWindup) * pace;
                        float rest = i == hits - 1 ? recovery * pace : 0.05f;
                        yield return Strike(slashOffset, slashSize, damage, windup, 0.15f, rest, true, 0f,
                            AnimStates.Telegraph, i % 2 == 0 ? AnimStates.Attack : AnimStates.Attack2);
                    }
                }
                yield return Wait(Random.Range(0.2f, 0.6f) * pace);
            }
        }

        IEnumerator Approach(float seconds)
        {
            float t = 0f;
            while (t < seconds && !IsDead && stunTimer <= 0f)
            {
                var player = Player;
                if (player == null) break;
                Facing = DirectionToPlayer();
                float distance = Mathf.Abs(player.Center.x - Center.x);
                float vx = distance > preferredDistance ? Facing * walkSpeed * (Phase == 2 ? 1.25f : 1f) : 0f;
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
                actionAnimation = vx != 0f ? AnimStates.Move : null;
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            actionAnimation = null;
        }

        static void Hint(string flag, string text)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.HasFlag(flag)) return;
            gm.SetFlag(flag);
            GameEvents.RaiseHint(TutorialHint.Format(text));
        }

        protected override string AnimationState() => AnimStates.Idle;
    }
}
