using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Alma
{
    /// <summary>
    /// Plays a full-screen video (intro, story cutscenes, ending). Hold JUMP/ATTACK, or press
    /// PAUSE, to skip. Use H.264 .mp4 or VP8 .webm files.
    /// </summary>
    public class CinematicPlayer : UIPanel
    {
        public float holdToSkipTime = 0.8f;

        RawImage screen;
        Text skipHint;
        VideoPlayer player;
        AudioSource audioSource;
        RenderTexture target;
        Coroutine routine;

        protected override void OnBuild()
        {
            var black = UIBuilder.CreateImage(transform, "Black", Color.black);
            UIBuilder.Stretch(black.rectTransform);
            black.raycastTarget = true;

            var screenRt = UIBuilder.CreateRect("Screen", transform);
            UIBuilder.Stretch(screenRt);
            screen = screenRt.gameObject.AddComponent<RawImage>();
            screen.raycastTarget = false;
            var fitter = screenRt.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;

            skipHint = UIBuilder.CreateText(transform, "SkipHint", "", 26, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.6f));
            UIBuilder.Place(skipHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(600f, 40f));

            var videoGo = new GameObject("VideoPlayer");
            videoGo.transform.SetParent(transform, false);
            player = videoGo.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.isLooping = false;
            audioSource = videoGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;

            SetVisible(false);
        }

        public void Play(VideoClip clip, bool allowSkip, Action onFinished)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PlayRoutine(clip, allowSkip, onFinished));
        }

        IEnumerator PlayRoutine(VideoClip clip, bool allowSkip, Action onFinished)
        {
            if (clip == null)
            {
                onFinished?.Invoke();
                yield break;
            }

            IsOpen = true;
            SetVisible(true);
            int width = Mathf.Max(16, (int)clip.width);
            int height = Mathf.Max(16, (int)clip.height);
            target = new RenderTexture(width, height, 0);
            player.targetTexture = target;
            screen.texture = target;
            var fitter = screen.GetComponent<AspectRatioFitter>();
            fitter.aspectRatio = (float)width / height;

            player.clip = clip;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, audioSource);
            var am = AudioManager.Instance;
            audioSource.volume = am != null ? am.MusicVolume * am.MasterVolume : 1f;

            player.Prepare();
            float wait = 0f;
            while (!player.isPrepared && wait < 10f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            player.Play();

            var input = InputReader.Instance;
            string key = input != null ? input.PromptFor("Jump") : "Jump";
            skipHint.text = allowSkip ? $"Hold {key} to skip" : "";

            float held = 0f;
            float startedAt = Time.unscaledTime;
            yield return null;
            while (player.isPlaying || Time.unscaledTime - startedAt < 0.5f)
            {
                if (allowSkip && input != null)
                {
                    held = input.ConfirmHeld ? held + Time.unscaledDeltaTime : 0f;
                    if (held >= holdToSkipTime || input.PausePressed) break;
                }
                if (!player.isPlaying && player.frame > 0 && (ulong)player.frame >= player.frameCount - 1) break;
                yield return null;
            }

            player.Stop();
            screen.texture = null;
            player.targetTexture = null;
            if (target != null)
            {
                target.Release();
                Destroy(target);
                target = null;
            }
            IsOpen = false;
            SetVisible(false);
            routine = null;
            onFinished?.Invoke();
        }
    }
}
