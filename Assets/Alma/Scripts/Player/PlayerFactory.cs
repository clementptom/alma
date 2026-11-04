using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Builds a complete player GameObject in code. Used when there's no player prefab
    /// (so an empty scene is still playable), and by the editor tools to make Player.prefab.
    /// </summary>
    public static class PlayerFactory
    {
        public static readonly Vector2 ColliderSize = new Vector2(0.8f, 1.8f);
        public static readonly Color PlaceholderColor = new Color(0.95f, 0.85f, 0.55f);

        public static GameObject CreatePlaceholderPlayer(Sprite sprite = null)
        {
            if (sprite == null) sprite = Placeholders.Square;

            var root = new GameObject("Player");
            root.SetActive(false); // configure everything before Awake runs

            var rb = root.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = ColliderSize;
            col.offset = new Vector2(0f, ColliderSize.y * 0.5f);

            var health = root.AddComponent<Health>();
            health.maxHealth = 5;

            // Visual child: real art goes here (SpriteRenderer + Animator).
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var body = new GameObject("Body");
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0f, ColliderSize.y * 0.5f, 0f);
            body.transform.localScale = new Vector3(ColliderSize.x, ColliderSize.y, 1f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = PlaceholderColor;
            sr.sortingOrder = 10;

            // A small "eye" so you can tell which way the placeholder faces.
            var eye = new GameObject("FacingMarker");
            eye.transform.SetParent(visual.transform, false);
            eye.transform.localPosition = new Vector3(0.22f, ColliderSize.y * 0.78f, 0f);
            eye.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
            var eyeSr = eye.AddComponent<SpriteRenderer>();
            eyeSr.sprite = sprite;
            eyeSr.color = new Color(0.15f, 0.1f, 0.2f);
            eyeSr.sortingOrder = 11;

            visual.AddComponent<Animator>();

            root.AddComponent<PlayerHealth>();
            root.AddComponent<PlayerEnergy>();
            root.AddComponent<PlayerStealth>();
            root.AddComponent<PlayerProfile>();
            root.AddComponent<StoneThrower>();
            var combat = root.AddComponent<PlayerCombat>();
            combat.showPlaceholderSlash = true;
            var controller = root.AddComponent<PlayerController>();
            controller.visual = visual.transform;
            root.AddComponent<PlayerAnimator>();
            root.AddComponent<PlayerInteractor>();

            Layers.SetLayerRecursively(root, Layers.Player);
            root.SetActive(true);
            return root;
        }
    }
}
