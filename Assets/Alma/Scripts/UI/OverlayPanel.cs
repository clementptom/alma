using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Alma
{
    /// <summary>
    /// A full-screen menu over the game (inventory, shop, fast travel, controls). During
    /// gameplay it opens through the GameManager, which pauses the world. On the title
    /// screen it opens standalone. Back / Esc / B closes it.
    /// </summary>
    public abstract class OverlayPanel : UIPanel
    {
        Action onClosed;
        bool openedByGameManager;
        int openedFrame;

        /// <summary>Opens the panel the right way for the current context.</summary>
        public void Open()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsInGameplayScene) gm.OpenOverlay(this);
            else OpenStandalone(null);
        }

        /// <summary>Opens without involving the GameManager (title screen).</summary>
        public void OpenStandalone(Action closed)
        {
            onClosed = closed;
            openedByGameManager = false;
            Show();
        }

        /// <summary>Called by GameManager.OpenOverlay.</summary>
        public void ShowFromGameManager()
        {
            openedByGameManager = true;
            Show();
        }

        /// <summary>Called by GameManager.CloseOverlay.</summary>
        public void HideFromGameManager() => Hide();

        void Show()
        {
            IsOpen = true;
            openedFrame = Time.frameCount;
            SetVisible(true);
            OnOpened();
            StartCoroutine(BlockSubmitBriefly());
        }

        /// <summary>
        /// The button press that opened this menu (e.g. closing a dialogue with A) must not
        /// also click the first button here, so menu navigation waits a moment.
        /// </summary>
        IEnumerator BlockSubmitBriefly()
        {
            var es = EventSystem.current;
            if (es == null) yield break;
            es.sendNavigationEvents = false;
            yield return new WaitForSecondsRealtime(0.15f);
            if (es != null && !(InputReader.Instance != null && InputReader.Instance.IsRebinding)) es.sendNavigationEvents = true;
        }

        void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            SetVisible(false);
            OnClosed();
            UIManager.Select(null);
            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        public void RequestClose()
        {
            if (!IsOpen) return;
            // The Back press that closes this menu shouldn't also back out of the menu underneath.
            if (InputReader.Instance != null) InputReader.Instance.BlockMenuInput(2);
            AudioManager.PlaySfx(AudioIds.UiBack);
            if (openedByGameManager && GameManager.Instance != null) GameManager.Instance.CloseOverlay();
            else Hide();
        }

        protected virtual void OnOpened() { }

        protected virtual void OnClosed() { }

        /// <summary>Extra input that closes this panel (e.g. the Inventory button toggles the inventory).</summary>
        protected virtual bool ClosePressed(InputReader input) => false;

        protected virtual void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
            var input = InputReader.Instance;
            if (input == null || input.IsRebinding) return;
            if (input.CancelPressed || input.PausePressed || ClosePressed(input)) RequestClose();
        }
    }
}
