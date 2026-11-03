using System;

namespace Alma
{
    /// <summary>Starts conversations from anywhere: DialogueRunner.Play(lines, "Name", onDone).</summary>
    public static class DialogueRunner
    {
        public static bool IsRunning => UIManager.Instance != null && UIManager.Instance.Dialogue.IsOpen;

        public static void Play(DialogueLine[] lines, string defaultSpeaker = "", Action onComplete = null)
        {
            var ui = UIManager.Instance;
            var gm = GameManager.Instance;
            if (ui == null || lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            GameState previous = gm != null ? gm.State : GameState.Playing;
            if (gm != null) gm.SetState(GameState.Dialogue);
            var player = PlayerController.Instance;
            if (player != null) player.Freeze();

            ui.Dialogue.Show(lines, defaultSpeaker, () =>
            {
                if (player != null) player.Unfreeze();
                if (gm != null && gm.State == GameState.Dialogue)
                    gm.SetState(previous == GameState.Dialogue ? GameState.Playing : previous);
                onComplete?.Invoke();
            });
        }
    }
}
