using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Plays a list of sprites as a flipbook, with no Animator Controller needed.
    /// Made for effects (slashes, dust, sparks, explosions): slice your sprite sheet,
    /// drag the frames into the array, and save it as a prefab.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames = new Sprite[0];
        public float framesPerSecond = 16f;
        public bool loop = false;
        public bool destroyWhenFinished = true;
        public bool useUnscaledTime = false;

        SpriteRenderer spriteRenderer;
        float time;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (frames.Length > 0) spriteRenderer.sprite = frames[0];
        }

        void OnEnable()
        {
            time = 0f;
        }

        void Update()
        {
            if (frames.Length == 0) return;
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            int frame = Mathf.FloorToInt(time * framesPerSecond);
            if (frame >= frames.Length)
            {
                if (loop)
                {
                    frame %= frames.Length;
                }
                else
                {
                    if (destroyWhenFinished) Destroy(gameObject);
                    else enabled = false;
                    return;
                }
            }
            spriteRenderer.sprite = frames[frame];
        }
    }
}
