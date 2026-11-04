using UnityEngine;

namespace Alma
{
    /// <summary>A spring, mushroom or geyser that launches the player upward when they land on it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class BouncePad : MonoBehaviour
    {
        [Tooltip("Upward speed. 26 reaches about 7 units high with the default jump settings.")]
        public float bounceSpeed = 26f;
        public Animator animator;

        CharacterAnimator anim;
        float cooldown;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            anim = new CharacterAnimator(animator);
        }

        void Start()
        {
            anim.Play(AnimStates.Idle);
        }

        void FixedUpdate()
        {
            if (cooldown > 0f)
            {
                cooldown -= Time.fixedDeltaTime;
                return;
            }
            var player = PlayerController.Instance;
            if (player == null || player.GroundCollider == null) return;
            var ground = player.GroundCollider.transform;
            if (ground != transform && !ground.IsChildOf(transform)) return;

            player.Bounce(bounceSpeed);
            cooldown = 0.25f;
            AudioManager.PlaySfx(AudioIds.BouncePad);
            EffectsManager.Spawn(EffectIds.Bounce, player.Center);
            anim.Play(AnimStates.Active, true);
        }
    }
}
