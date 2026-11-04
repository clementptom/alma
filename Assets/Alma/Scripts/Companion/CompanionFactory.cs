using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Builds Barro in code. Used when there's no Resources/Barro prefab, and by the editor
    /// tools to make that prefab.
    /// </summary>
    public static class CompanionFactory
    {
        public static readonly Color PlaceholderColor = new Color(0.62f, 0.42f, 0.26f);

        public static GameObject CreatePlaceholderBarro(Sprite sprite = null)
        {
            if (sprite == null) sprite = Placeholders.Square;

            var root = new GameObject("Barro");
            root.SetActive(false);

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.3f, 0.85f);
            col.offset = new Vector2(0f, 0.425f);

            var health = root.AddComponent<Health>();
            health.maxHealth = 6;
            health.invulnerabilityTime = 0.8f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Part(visual.transform, "Body", sprite, PlaceholderColor, new Vector2(0f, 0.45f), new Vector2(1.4f, 0.85f), 9);
            Part(visual.transform, "Head", sprite, PlaceholderColor * 0.9f, new Vector2(0.78f, 0.9f), new Vector2(0.55f, 0.5f), 10);
            Part(visual.transform, "Eye", sprite, new Color(0.1f, 0.05f, 0.05f), new Vector2(0.9f, 0.98f), new Vector2(0.12f, 0.12f), 11);
            var collar = Part(visual.transform, "Collar", sprite, new Color(0.85f, 0.88f, 0.95f), new Vector2(0.55f, 0.78f), new Vector2(0.14f, 0.4f), 11);
            collar.SetActive(false);
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(visual.transform, false);
            mouth.transform.localPosition = new Vector3(1.05f, 0.75f, 0f);
            visual.AddComponent<Animator>();

            var barro = root.AddComponent<BarroCompanion>();
            barro.visual = visual.transform;
            barro.collar = collar;
            barro.mouth = mouth.transform;

            Layers.SetLayerRecursively(root, Layers.Companion);
            root.SetActive(true);
            return root;
        }

        static GameObject Part(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(color.r, color.g, color.b, 1f);
            sr.sortingOrder = order;
            return go;
        }
    }
}
