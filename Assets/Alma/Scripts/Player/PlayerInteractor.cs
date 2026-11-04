using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Finds the closest Interactable (NPC, save station, lever, locked door...),
    /// shows its prompt on the HUD and triggers it when INTERACT is pressed.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public Interactable Current { get; private set; }

        PlayerController controller;
        float holdTimer;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool canInteract = controller != null && !controller.IsFrozen && !controller.PlayerHealth.IsDead
                               && (gm == null || gm.State == GameState.Playing);

            Interactable best = canInteract ? Interactable.FindBest(controller) : null;
            if (best != Current)
            {
                Current = best;
                if (UIManager.Instance != null) UIManager.Instance.Hud.SetPrompt(Current);
            }

            var input = InputReader.Instance;
            if (Current == null || input == null)
            {
                holdTimer = 0f;
                return;
            }
            if (Current.holdDuration <= 0f)
            {
                if (input.InteractPressed) Current.Interact(controller);
                return;
            }
            // Hold to interact: fill the prompt bar.
            holdTimer = input.InteractHeld ? holdTimer + Time.deltaTime : 0f;
            if (UIManager.Instance != null) UIManager.Instance.Hud.SetPromptProgress(holdTimer / Current.holdDuration);
            if (holdTimer >= Current.holdDuration)
            {
                holdTimer = 0f;
                var target = Current;
                target.Interact(controller);
            }
        }

        void OnDisable()
        {
            Current = null;
            if (UIManager.Instance != null) UIManager.Instance.Hud.SetPrompt(null);
        }
    }
}
