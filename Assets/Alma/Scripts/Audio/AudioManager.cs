using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Plays sound effects from a pool, cross-fades music, and runs a separate ambience loop.
    /// Volumes are saved in PlayerPrefs. Call the static helpers from anywhere:
    ///   AudioManager.PlaySfx(AudioIds.PlayerJump);
    ///   AudioManager.PlayMusic(AudioIds.MusicBoss);
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const string MasterKey = "alma.volume.master";
        const string MusicKey = "alma.volume.music";
        const string SfxKey = "alma.volume.sfx";

        public int sfxVoices = 16;
        public float defaultMusicFade = 1.2f;

        public AudioLibrary Library { get; private set; }
        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 1f;
        public string CurrentMusicId { get; private set; }
        public string CurrentAmbienceId { get; private set; }

        readonly List<AudioSource> sfxPool = new List<AudioSource>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        readonly HashSet<string> warnedMissing = new HashSet<string>();
        AudioSource musicA, musicB, ambience;
        AudioSource activeMusic;
        float activeMusicBaseVolume = 1f;
        float ambienceBaseVolume = 1f;
        Coroutine musicFade;
        int nextVoice;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            Library = Resources.Load<AudioLibrary>("AudioLibrary");
            if (Library == null) Library = ScriptableObject.CreateInstance<AudioLibrary>();

            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);

            musicA = CreateSource("Music A", true);
            musicB = CreateSource("Music B", true);
            ambience = CreateSource("Ambience", true);
            for (int i = 0; i < sfxVoices; i++) sfxPool.Add(CreateSource("SFX " + i, false));
            activeMusic = musicA;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        // ------------------------------------------------------------ Static API

        public static void PlaySfx(string id, float volumeScale = 1f)
        {
            if (Instance != null) Instance.PlaySound(id, volumeScale);
        }

        public static void PlayMusic(string id, float fade = -1f)
        {
            if (Instance != null) Instance.SetMusic(id, fade);
        }

        public static void StopMusic(float fade = -1f)
        {
            if (Instance != null) Instance.SetMusic(null, fade);
        }

        public static void PlayAmbience(string id)
        {
            if (Instance != null) Instance.SetAmbience(id);
        }

        // ------------------------------------------------------------------ SFX

        public void PlaySound(string id, float volumeScale = 1f)
        {
            var entry = Library.FindSound(id);
            if (entry == null || entry.clips == null || entry.clips.Length == 0)
            {
                WarnMissingOnce(id);
                return;
            }

            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < entry.minInterval) return;
            lastPlayed[id] = now;

            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null) return;

            var source = sfxPool[nextVoice];
            nextVoice = (nextVoice + 1) % sfxPool.Count;
            source.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            source.volume = entry.volume * volumeScale * SfxVolume * MasterVolume;
            source.clip = clip;
            source.Play();
        }

        void WarnMissingOnce(string id)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(id) || !warnedMissing.Add(id)) return;
            if (warnedMissing.Count == 1)
                Debug.Log($"[Audio] No clip assigned for '{id}' (and maybe others) in Resources/AudioLibrary. Fine while prototyping; this is only logged once.");
#endif
        }

        // ---------------------------------------------------------------- Music

        public void SetMusic(string id, float fade = -1f)
        {
            if (fade < 0f) fade = defaultMusicFade;
            if (id == CurrentMusicId) return;
            CurrentMusicId = id;

            var entry = Library.FindMusic(id);
            if (!string.IsNullOrEmpty(id) && (entry == null || entry.clip == null)) WarnMissingOnce(id);

            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(CrossFade(entry, fade));
        }

        IEnumerator CrossFade(MusicEntry entry, float duration)
        {
            var from = activeMusic;
            var to = activeMusic == musicA ? musicB : musicA;
            float fromStart = from.volume;

            bool hasNew = entry != null && entry.clip != null;
            if (hasNew)
            {
                to.clip = entry.clip;
                to.loop = entry.loop;
                to.volume = 0f;
                to.Play();
            }

            float targetBase = hasNew ? entry.volume : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (hasNew) to.volume = Mathf.Lerp(0f, targetBase * MusicVolume * MasterVolume, k);
                yield return null;
            }

            from.Stop();
            from.volume = 0f;
            if (hasNew)
            {
                activeMusic = to;
                activeMusicBaseVolume = targetBase;
                to.volume = targetBase * MusicVolume * MasterVolume;
            }
            musicFade = null;
        }

        public void SetAmbience(string id)
        {
            if (id == CurrentAmbienceId) return;
            CurrentAmbienceId = id;
            var entry = Library.FindMusic(id);
            if (entry == null || entry.clip == null)
            {
                if (!string.IsNullOrEmpty(id)) WarnMissingOnce(id);
                ambience.Stop();
                return;
            }
            ambience.clip = entry.clip;
            ambience.loop = true;
            ambienceBaseVolume = entry.volume;
            ambience.volume = ambienceBaseVolume * SfxVolume * MasterVolume;
            ambience.Play();
        }

        // -------------------------------------------------------------- Volumes

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            ApplyVolumes();
        }

        void ApplyVolumes()
        {
            if (musicFade == null && activeMusic != null && activeMusic.isPlaying)
                activeMusic.volume = activeMusicBaseVolume * MusicVolume * MasterVolume;
            if (ambience != null) ambience.volume = ambienceBaseVolume * SfxVolume * MasterVolume;
        }
    }
}
