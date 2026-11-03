using UnityEngine;
using UnityEngine.InputSystem;

namespace Alma
{
    /// <summary>
    /// Every input the game reads goes through here. Actions are defined in code, so no
    /// .inputactions asset is needed. Keyboard and gamepad both work out of the box.
    ///
    /// Default controls:
    ///   Move      WASD / Arrows / Left stick / D-pad
    ///   Jump      Space / Z / South button (A, Cross)
    ///   Attack    J / X / West button (X, Square)
    ///   Dash      K / C / Left Shift / RB / RT
    ///   Interact  E / North button (Y, Triangle)
    ///   Map       Tab / M / Select
    ///   Pause     Esc / Start
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        [Tooltip("Stick values below this count as zero for movement.")]
        public float stickDeadzone = 0.35f;

        InputAction move, jump, attack, dash, interact, map, pause;

        /// <summary>When false, gameplay inputs (move, jump, attack...) read as idle. Menu inputs keep working.</summary>
        public bool GameplayEnabled
        {
            get => gameplayEnabled && Time.frameCount != enabledFrame;
            set
            {
                // Ignore the frame gameplay resumes, so the button that closed a menu doesn't also jump.
                if (value && !gameplayEnabled) enabledFrame = Time.frameCount;
                gameplayEnabled = value;
            }
        }

        bool gameplayEnabled = true;
        int enabledFrame = -1;

        public bool LastInputWasGamepad { get; private set; }

        // --- Gameplay (respect GameplayEnabled) ---
        public Vector2 Move => GameplayEnabled ? RawMove : Vector2.zero;
        public int MoveX => GameplayEnabled ? Digital(RawMove.x) : 0;
        public int MoveY => GameplayEnabled ? Digital(RawMove.y) : 0;
        public bool JumpPressed => GameplayEnabled && jump.WasPressedThisFrame();
        public bool JumpHeld => GameplayEnabled && jump.IsPressed();
        public bool AttackPressed => GameplayEnabled && attack.WasPressedThisFrame();
        public bool DashPressed => GameplayEnabled && dash.WasPressedThisFrame();
        public bool InteractPressed => GameplayEnabled && interact.WasPressedThisFrame();

        // --- Always available (menus, dialogue, pause) ---
        public Vector2 RawMove => move.ReadValue<Vector2>();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool MapPressed => map.WasPressedThisFrame();
        /// <summary>Jump, Attack or Interact pressed: used to advance dialogue and close popups.</summary>
        public bool ConfirmPressed => jump.WasPressedThisFrame() || attack.WasPressedThisFrame() || interact.WasPressedThisFrame();
        public bool ConfirmHeld => jump.IsPressed() || attack.IsPressed() || interact.IsPressed();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            CreateActions();
        }

        void OnEnable()
        {
            SetActionsEnabled(true);
        }

        void OnDisable()
        {
            SetActionsEnabled(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var action in AllActions())
            {
                if (action == null) continue;
                action.performed -= OnAnyPerformed;
                action.Dispose();
            }
        }

        int Digital(float value) => value > stickDeadzone ? 1 : value < -stickDeadzone ? -1 : 0;

        void CreateActions()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");

            jump = Button("Jump", "<Keyboard>/space", "<Keyboard>/z", "<Gamepad>/buttonSouth");
            attack = Button("Attack", "<Keyboard>/j", "<Keyboard>/x", "<Gamepad>/buttonWest");
            dash = Button("Dash", "<Keyboard>/k", "<Keyboard>/c", "<Keyboard>/leftShift",
                "<Gamepad>/rightShoulder", "<Gamepad>/rightTrigger");
            interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonNorth");
            map = Button("Map", "<Keyboard>/tab", "<Keyboard>/m", "<Gamepad>/select");
            pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");

            foreach (var action in AllActions()) action.performed += OnAnyPerformed;
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var binding in bindings) action.AddBinding(binding);
            return action;
        }

        InputAction[] AllActions() => new[] { move, jump, attack, dash, interact, map, pause };

        void SetActionsEnabled(bool enabled)
        {
            foreach (var action in AllActions())
            {
                if (action == null) continue;
                if (enabled) action.Enable();
                else action.Disable();
            }
        }

        void OnAnyPerformed(InputAction.CallbackContext context)
        {
            if (context.control != null) LastInputWasGamepad = context.control.device is Gamepad;
        }

        /// <summary>Text for a button in the current input scheme, used by HUD prompts.</summary>
        public string PromptFor(string actionName)
        {
            if (LastInputWasGamepad)
            {
                switch (actionName)
                {
                    case "Jump": return "A";
                    case "Attack": return "X";
                    case "Dash": return "RB";
                    case "Interact": return "Y";
                    case "Map": return "Select";
                    case "Pause": return "Start";
                }
            }
            else
            {
                switch (actionName)
                {
                    case "Jump": return "Space";
                    case "Attack": return "J";
                    case "Dash": return "K";
                    case "Interact": return "E";
                    case "Map": return "Tab";
                    case "Pause": return "Esc";
                }
            }
            return actionName;
        }
    }
}
