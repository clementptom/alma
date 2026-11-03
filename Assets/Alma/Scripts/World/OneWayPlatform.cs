using UnityEngine;

namespace Alma
{
    /// <summary>
    /// A platform you can jump up through and stand on. Hold DOWN and press JUMP to drop through.
    /// Sets up the PlatformEffector2D and the layer for you.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(PlatformEffector2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        void Reset() => Configure();

        void Awake() => Configure();

        void Configure()
        {
            var col = GetComponent<Collider2D>();
            col.usedByEffector = true;
            var effector = GetComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            effector.useSideFriction = false;
            int layer = Layers.Index(Layers.OneWayPlatform);
            if (layer >= 0) gameObject.layer = layer;
        }
    }
}
