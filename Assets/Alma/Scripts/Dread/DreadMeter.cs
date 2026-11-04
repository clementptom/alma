using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Dread: 0 (calm) to 100 (terror). It rises in dark zones, near the Hollowed and the
    /// Drowned, while disguised, and when Barro is far away or down. Hits and scares add
    /// spikes. It fades when nothing is feeding it, faster by a fire.
    /// Each relic piece the player carries lowers the maximum.
    ///
    /// Effects: whispers (35+), false figures at the edge of sight and shaking aim (65+),
    /// and a darkening vignette drawn by the HUD.
    /// Lives on the [Systems] object.
    /// </summary>
    public class DreadMeter : MonoBehaviour
    {
        public static DreadMeter Instance { get; private set; }

        [Header("Rates (per second)")]
        public float decayRate = 7f;
        public float barroAwayRate = 3f;
        public float barroAwayDistance = 12f;
        public float barroDownRate = 6f;
        public float disguisedRate = 8f;

        [Header("Relics")]
        public float ceilingPerRelic = 15f;
        public string[] relicItemIds = { "relic_bone", "relic_dove", "relic_snow" };

        [Header("Effects")]
        public float whispersAt = 35f;
        public float phantomsAt = 65f;
        public float maxAimJitterDegrees = 18f;
        public Vector2 phantomInterval = new Vector2(4f, 9f);

        public float Value { get; private set; }
        public float Ceiling { get; private set; } = 100f;
        public float Normalized => Value / 100f;

        /// <summary>How much thrown aim wobbles right now (in degrees).</summary>
        public static float AimJitterDegrees
        {
            get
            {
                var d = Instance;
                if (d == null) return 0f;
                return Mathf.InverseLerp(d.phantomsAt, 100f, d.Value) * d.maxAimJitterDegrees;
            }
        }

        AudioSource whispers;
        string whisperClipId;
        float phantomTimer = 6f;
        float shakeTimer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            var go = new GameObject("Dread Whispers");
            go.transform.SetParent(transform, false);
            whispers = go.AddComponent<AudioSource>();
            whispers.loop = true;
            whispers.playOnAwake = false;
            whispers.spatialBlend = 0f;
            whispers.volume = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Adds a sudden jolt of Dread (a hit, a scream, Barro hurt).</summary>
        public static void Spike(float amount)
        {
            if (Instance == null || PlayerController.Instance == null) return;
            Instance.Value = Mathf.Clamp(Instance.Value + amount, 0f, Instance.Ceiling);
            if (amount >= 10f) AudioManager.PlaySfx(AudioIds.DreadSting);
        }

        public void ResetValue(float to = 0f) => Value = Mathf.Clamp(to, 0f, Ceiling);

        void Update()
        {
            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            bool active = player != null && gm != null && gm.IsInGameplayScene && !player.PlayerHealth.IsDead;
            if (!active)
            {
                Value = Mathf.MoveTowards(Value, 0f, decayRate * Time.unscaledDeltaTime);
                UpdateWhispers();
                return;
            }

            Ceiling = 100f - RelicCount(gm) * ceilingPerRelic;
            float rate = Rate(player, gm);
            float dt = Time.deltaTime;
            if (rate > 0f) Value += rate * dt;
            else Value -= (decayRate - rate) * dt;
            Value = Mathf.Clamp(Value, 0f, Ceiling);

            UpdateWhispers();
            UpdatePhantoms(player, dt);
            UpdateShake(dt);
        }

        int RelicCount(GameManager gm)
        {
            int count = 0;
            foreach (var id in relicItemIds)
                if (gm.HasKeyItem(id)) count++;
            return count;
        }

        float Rate(PlayerController player, GameManager gm)
        {
            Vector2 p = player.Center;
            float rate = 0f;
            foreach (var zone in DreadZone.Active)
                if (zone != null && zone.Contains(p)) rate += zone.ratePerSecond;
            foreach (var source in DreadSource.Active)
                if (source != null) rate += source.RateAt(p);
            if (player.Stealth != null && player.Stealth.IsDisguised) rate += disguisedRate;

            // Being without Barro is frightening once he's with you.
            bool hasBarro = gm.HasFlag(CompanionFlags.Joined) && player.Profile != null && player.Profile.HasCompanion
                            && (ChapterSettings.Current == null || ChapterSettings.Current.allowCompanion);
            if (hasBarro)
            {
                var barro = BarroCompanion.Instance;
                if (barro == null || barro.IsDowned) rate += barroDownRate;
                else if (Vector2.Distance(barro.Position, p) > barroAwayDistance) rate += barroAwayRate;
            }
            return rate;
        }

        void UpdateWhispers()
        {
            float target = Mathf.InverseLerp(whispersAt, 100f, Value);
            var am = AudioManager.Instance;
            if (am != null && whisperClipId != AudioIds.AmbienceWhispers)
            {
                var entry = am.Library.FindMusic(AudioIds.AmbienceWhispers);
                if (entry != null && entry.clip != null)
                {
                    whispers.clip = entry.clip;
                    whisperClipId = AudioIds.AmbienceWhispers;
                }
            }
            if (whispers.clip == null) return;
            float volume = target * (am != null ? am.SfxVolume * am.MasterVolume : 1f);
            whispers.volume = Mathf.MoveTowards(whispers.volume, volume, Time.unscaledDeltaTime * 0.5f);
            if (whispers.volume > 0.01f && !whispers.isPlaying) whispers.Play();
            else if (whispers.volume <= 0.01f && whispers.isPlaying) whispers.Stop();
        }

        void UpdatePhantoms(PlayerController player, float dt)
        {
            if (Value < phantomsAt) return;
            phantomTimer -= dt;
            if (phantomTimer > 0f) return;
            phantomTimer = Random.Range(phantomInterval.x, phantomInterval.y);

            var cam = CameraController.Main != null ? CameraController.Main.Camera : Camera.main;
            if (cam == null) return;
            float halfWidth = cam.orthographicSize * cam.aspect;
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector2 at = new Vector2(cam.transform.position.x + side * halfWidth * 0.85f, player.transform.position.y);
            var hit = PhysicsUtil.Raycast(at + Vector2.up * 3f, Vector2.down, 8f, Layers.GroundMask);
            if (hit.collider != null) at = hit.point;
            if (EffectsManager.Spawn(EffectIds.DreadPhantom, at) == null) DreadPhantom.CreateDefault(at);
        }

        void UpdateShake(float dt)
        {
            if (Value < 85f) return;
            shakeTimer -= dt;
            if (shakeTimer > 0f) return;
            shakeTimer = 1.2f;
            CameraController.Shake(0.04f, 0.4f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }

    /// <summary>Save flags used by the companion.</summary>
    public static class CompanionFlags
    {
        public const string Joined = "barro_joined";
        public const string Collar = "barro_collar";
    }
}
