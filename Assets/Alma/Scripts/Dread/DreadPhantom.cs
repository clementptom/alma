using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A false figure that appears at high Dread: it stands at the edge of sight and is gone
    /// when you get close or look for too long.
    /// </summary>
    public class DreadPhantom : MonoBehaviour
    {
        public float lifetime = 3.5f;
        public float vanishDistance = 4f;

        SpriteRenderer[] renderers;
        float age;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        void Update()
        {
            age += Time.deltaTime;
            var player = PlayerController.Instance;
            bool vanish = age > lifetime || (player != null && Vector2.Distance(player.Center, transform.position) < vanishDistance);
            float alpha = vanish ? 0f : Mathf.Clamp01(age * 2f) * 0.55f;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var c = r.color;
                c.a = Mathf.MoveTowards(c.a, alpha, Time.deltaTime * (vanish ? 4f : 1f));
                r.color = c;
            }
            if (vanish && (renderers.Length == 0 || renderers[0] == null || renderers[0].color.a <= 0.01f)) Destroy(gameObject);
        }

        /// <summary>A placeholder silhouette, used until a phantom effect prefab is assigned.</summary>
        public static GameObject CreateDefault(Vector2 position)
        {
            var go = new GameObject("Phantom");
            go.transform.position = position;
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.8f, 1.8f, 1f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = Placeholders.Square;
            sr.color = new Color(0.05f, 0.03f, 0.08f, 0f);
            sr.sortingOrder = 40;
            go.AddComponent<DreadPhantom>();
            return go;
        }
    }
}
