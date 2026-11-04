using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    [Serializable]
    public class SoundEntry
    {
        public string id;
        [Tooltip("One is picked at random each time (great for footsteps and hits).")]
        public AudioClip[] clips = new AudioClip[0];
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;
        [Tooltip("The same sound won't play again within this many seconds.")]
        public float minInterval = 0.03f;
    }

    [Serializable]
    public class MusicEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.8f;
        public bool loop = true;
    }

    /// <summary>
    /// Maps sound IDs (AudioIds) to audio clips. Lives at Resources/AudioLibrary.asset.
    /// Entries with no clips are skipped silently, so the game runs before any audio exists.
    /// </summary>
    [CreateAssetMenu(menuName = "Alma/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        public List<SoundEntry> sounds = new List<SoundEntry>();
        [Tooltip("Music and ambience loops.")]
        public List<MusicEntry> music = new List<MusicEntry>();

        Dictionary<string, SoundEntry> soundLookup;
        Dictionary<string, MusicEntry> musicLookup;

        public SoundEntry FindSound(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (soundLookup == null)
            {
                soundLookup = new Dictionary<string, SoundEntry>();
                foreach (var s in sounds)
                {
                    if (s != null && !string.IsNullOrEmpty(s.id)) soundLookup[s.id] = s;
                }
            }
            soundLookup.TryGetValue(id, out var entry);
            return entry;
        }

        public MusicEntry FindMusic(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (musicLookup == null)
            {
                musicLookup = new Dictionary<string, MusicEntry>();
                foreach (var m in music)
                {
                    if (m != null && !string.IsNullOrEmpty(m.id)) musicLookup[m.id] = m;
                }
            }
            musicLookup.TryGetValue(id, out var entry);
            return entry;
        }

        void OnValidate()
        {
            soundLookup = null;
            musicLookup = null;
        }
    }
}
