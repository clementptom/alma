using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Changes how the ground feels: put it on an icy ledge so the player slides when
    /// starting and stopping. Works on any ground collider.
    /// </summary>
    public class SurfaceModifier : MonoBehaviour
    {
        [Tooltip("Multiplies how quickly you speed up. Low = slippery.")]
        [Range(0.05f, 2f)] public float accelerationMultiplier = 0.35f;
        [Tooltip("Multiplies how quickly you slow down. Low = slippery.")]
        [Range(0.02f, 2f)] public float decelerationMultiplier = 0.12f;
    }
}
