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
            if (Current != null && input != null && input.InteractPressed)
            {
                Current.Interact(controller);
            }
        }

        void OnDisable()
        {
            Current = null;
            if (UIManager.Instance != null) UIManager.Instance.Hud.SetPrompt(null);
        }
    }
}
