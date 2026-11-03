using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Background or foreground layer that moves at a different speed than the camera.
    /// 0 = fixed in the world, 1 = moves with the camera (infinitely far away).
    /// Negative values (foreground) move faster than the world.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(-1f, 1f)] public float horizontalFactor = 0.5f;
        [Range(-1f, 1f)] public float verticalFactor = 0.3f;

        Transform cam;
        Vector3 startPosition;
        Vector3 cameraStart;

        void Start()
        {
            startPosition = transform.position;
            if (Camera.main != null)
            {
                cam = Camera.main.transform;
                cameraStart = cam.position;
            }
        }

        void LateUpdate()
        {
            if (cam == null)
            {
                if (Camera.main == null) return;
                cam = Camera.main.transform;
                cameraStart = cam.position;
            }
            Vector3 delta = cam.position - cameraStart;
            transform.position = startPosition + new Vector3(delta.x * horizontalFactor, delta.y * verticalFactor, 0f);
        }
    }
}
