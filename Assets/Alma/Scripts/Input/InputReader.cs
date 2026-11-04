using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alma
{
    /// <summary>
    /// Every input the game reads goes through here. Actions are defined in code (no
    /// .inputactions asset needed), keyboard and gamepad both work, and every action can be
    /// rebound from the Controls menu. Custom bindings are saved in PlayerPrefs.
    ///
    /// Default controls (keyboard / gamepad):
    ///   Move          WASD (+ arrow keys)   / Left stick
    ///   Jump          Space                 / A (Cross)
    ///   Attack        J                     / X (Square)
    ///   Dodge         K                     / B (Circle)      roll on the ground, air dash once unlocked
    ///   Parry         L                     / LB
    ///   Heal          H                     / RB
    ///   Interact      E                     / Y (Triangle)
    ///   Sneak (hold)  Left Shift            / LT
    ///   Throw stone   F                     / RT
    ///   Barro         1 Track, 2 Guard, 3 Fetch, 4 Attack / D-pad up, down, left, right
    ///   Map           Tab                   / Select (View)
    ///   Inventory     I                     / R3
    ///   Pause         Esc                   / Start (Menu)       (not rebindable)
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        /// <summary>Binding groups. Primary groups are shown in the Controls menu and can be rebound.</summary>
        public const string KeyboardGroup = "Keyboard";
        public const string GamepadGroup = "Gamepad";
        /// <summary>Extra fixed bindings (arrow keys, D-pad, RT) that aren't shown in the Controls menu.</summary>
        public const string KeyboardExtraGroup = "KeyboardExtra";
        public const string GamepadExtraGroup = "GamepadExtra";

        const string OverridesKey = "alma.input.bindings";
        /// <summary>Bump this if you change the default bindings layout, so old saved rebinds are ignored.</summary>
        const int BindingLayoutVersion = 2;

        // Saved as JSON. Bindings created in code get new internal ids every launch, so
        // overrides are stored by action name + binding index instead.
        [Serializable]
        class SavedBinding
        {
            public string action;
            public int index;
            public string path;
        }

        [Serializable]
        class SavedBindings
        {
            public int version;
            public List<SavedBinding> bindings = new List<SavedBinding>();
        }

        /// <summary>Actions shown in the Controls menu, in order.</summary>
        public static readonly string[] RebindableActions =
        {
            "Jump", "Attack", "Dodge", "Parry", "Heal", "Interact", "Sneak", "Throw",
            "BarroTrack", "BarroGuard", "BarroFetch", "BarroAttack", "Map", "Inventory"
        };

        /// <summary>Readable name for an action, for menus.</summary>
        public static string ActionLabel(string action)
        {
            switch (action)
            {
                case "Throw": return "Throw Stone";
                case "BarroTrack": return "Barro: Track";
                case "BarroGuard": return "Barro: Guard / Heel";
                case "BarroFetch": return "Barro: Fetch";
                case "BarroAttack": return "Barro: Attack";
                default: return action;
            }
        }
        /// <summary>Keyboard movement keys shown in the Controls menu (parts of the Move composite).</summary>
        public static readonly string[] MoveParts = { "Up", "Down", "Left", "Right" };

        [Tooltip("Stick values below this count as zero for movement.")]
        public float stickDeadzone = 0.35f;

        public InputActionMap Actions { get; private set; }

        InputAction move, jump, attack, dodge, parry, heal, interact, sneak, throwStone, map, inventory, pause, cancel;
        InputAction barroTrack, barroGuard, barroFetch, barroAttack;

        /// <summary>Raised when the player switches between keyboard and gamepad (prompts update).</summary>
        public event Action DeviceChanged;
        /// <summary>Raised after any binding changes (rebind or reset).</summary>
        public event Action BindingsChanged;

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
        int menuBlockedUntilFrame = -1;
        InputActionRebindingExtensions.RebindingOperation rebinding;

        public bool LastInputWasGamepad { get; private set; }
        public bool IsRebinding => rebinding != null;

        bool MenuInputBlocked => Time.frameCount <= menuBlockedUntilFrame || IsRebinding;

        // --- Gameplay (respect GameplayEnabled) ---
        public Vector2 Move => GameplayEnabled ? RawMove : Vector2.zero;
        public int MoveX => GameplayEnabled ? Digital(RawMove.x) : 0;
        public int MoveY => GameplayEnabled ? Digital(RawMove.y) : 0;
        public bool JumpPressed => GameplayEnabled && jump.WasPressedThisFrame();
        public bool JumpHeld => GameplayEnabled && jump.IsPressed();
        public bool AttackPressed => GameplayEnabled && attack.WasPressedThisFrame();
        public bool DodgePressed => GameplayEnabled && dodge.WasPressedThisFrame();
        public bool ParryPressed => GameplayEnabled && parry.WasPressedThisFrame();
        public bool HealPressed => GameplayEnabled && heal.WasPressedThisFrame();
        public bool HealHeld => GameplayEnabled && heal.IsPressed();
        public bool InteractPressed => GameplayEnabled && interact.WasPressedThisFrame();
        public bool InteractHeld => GameplayEnabled && interact.IsPressed();
        public bool SneakHeld => GameplayEnabled && sneak.IsPressed();
        public bool ThrowPressed => GameplayEnabled && throwStone.WasPressedThisFrame();
        public bool BarroTrackPressed => GameplayEnabled && barroTrack.WasPressedThisFrame();
        public bool BarroGuardPressed => GameplayEnabled && barroGuard.WasPressedThisFrame();
        public bool BarroFetchPressed => GameplayEnabled && barroFetch.WasPressedThisFrame();
        public bool BarroAttackPressed => GameplayEnabled && barroAttack.WasPressedThisFrame();

        // --- Always available (menus, dialogue, pause) ---
        public Vector2 RawMove => move.ReadValue<Vector2>();
        public bool PausePressed => !MenuInputBlocked && pause.WasPressedThisFrame();
        public bool MapPressed => !MenuInputBlocked && map.WasPressedThisFrame();
        public bool InventoryPressed => !MenuInputBlocked && inventory.WasPressedThisFrame();
        /// <summary>Back out of a menu: Esc, Backspace, or B / Circle.</summary>
        public bool CancelPressed => !MenuInputBlocked && cancel.WasPressedThisFrame();
        /// <summary>Jump, Attack or Interact pressed: used to advance dialogue and close popups.</summary>
        public bool ConfirmPressed => !MenuInputBlocked &&
                                      (jump.WasPressedThisFrame() || attack.WasPressedThisFrame() || interact.WasPressedThisFrame());
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
            LoadBindings();
        }

        void OnEnable()
        {
            if (Actions != null) Actions.Enable();
        }

        void OnDisable()
        {
            CancelRebind();
            if (Actions != null) Actions.Disable();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Actions == null) return;
            foreach (var action in Actions.actions) action.performed -= OnAnyPerformed;
            Actions.Dispose();
        }

        int Digital(float value) => value > stickDeadzone ? 1 : value < -stickDeadzone ? -1 : 0;

        void CreateActions()
        {
            Actions = new InputActionMap("Gameplay");

            move = Actions.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KeyboardGroup).With("Down", "<Keyboard>/s", KeyboardGroup)
                .With("Left", "<Keyboard>/a", KeyboardGroup).With("Right", "<Keyboard>/d", KeyboardGroup);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", KeyboardExtraGroup).With("Down", "<Keyboard>/downArrow", KeyboardExtraGroup)
                .With("Left", "<Keyboard>/leftArrow", KeyboardExtraGroup).With("Right", "<Keyboard>/rightArrow", KeyboardExtraGroup);
            move.AddBinding("<Gamepad>/leftStick", groups: GamepadExtraGroup);

            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            attack = Button("Attack", "<Keyboard>/j", "<Gamepad>/buttonWest");
            dodge = Button("Dodge", "<Keyboard>/k", "<Gamepad>/buttonEast");
            parry = Button("Parry", "<Keyboard>/l", "<Gamepad>/leftShoulder");
            heal = Button("Heal", "<Keyboard>/h", "<Gamepad>/rightShoulder");
            interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonNorth");
            sneak = Button("Sneak", "<Keyboard>/leftShift", "<Gamepad>/leftTrigger");
            throwStone = Button("Throw", "<Keyboard>/f", "<Gamepad>/rightTrigger");
            barroTrack = Button("BarroTrack", "<Keyboard>/1", "<Gamepad>/dpad/up");
            barroGuard = Button("BarroGuard", "<Keyboard>/2", "<Gamepad>/dpad/down");
            barroFetch = Button("BarroFetch", "<Keyboard>/3", "<Gamepad>/dpad/left");
            barroAttack = Button("BarroAttack", "<Keyboard>/4", "<Gamepad>/dpad/right");
            map = Button("Map", "<Keyboard>/tab", "<Gamepad>/select");
            inventory = Button("Inventory", "<Keyboard>/i", "<Gamepad>/rightStickPress");
            pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");

            cancel = Actions.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape", groups: KeyboardExtraGroup);
            cancel.AddBinding("<Keyboard>/backspace", groups: KeyboardExtraGroup);
            cancel.AddBinding("<Gamepad>/buttonEast", groups: GamepadExtraGroup);

            foreach (var action in Actions.actions) action.performed += OnAnyPerformed;
        }

        InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = Actions.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboard, groups: KeyboardGroup);
            action.AddBinding(gamepad, groups: GamepadGroup);
            return action;
        }

        void OnAnyPerformed(InputAction.CallbackContext context)
        {
            if (context.control == null) return;
            bool gamepad = context.control.device is Gamepad;
            if (gamepad == LastInputWasGamepad) return;
            LastInputWasGamepad = gamepad;
            DeviceChanged?.Invoke();
        }

        /// <summary>Makes Pause/Cancel/Map/Confirm read as not pressed for a couple of frames.</summary>
        public void BlockMenuInput(int frames = 2)
        {
            menuBlockedUntilFrame = Mathf.Max(menuBlockedUntilFrame, Time.frameCount + frames);
        }

        // ============================================================ Bindings

        /// <summary>
        /// Index of the binding for <paramref name="actionName"/> in a binding group, or -1.
        /// For Move, pass the composite part name ("Up", "Down", "Left", "Right").
        /// </summary>
        public int FindBindingIndex(string actionName, string group, string part = null)
        {
            var action = Actions.FindAction(actionName);
            if (action == null) return -1;
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite) continue;
                if (!HasGroup(b.groups, group)) continue;
                if (!string.IsNullOrEmpty(part) && !string.Equals(b.name, part, StringComparison.OrdinalIgnoreCase)) continue;
                return i;
            }
            return -1;
        }

        static bool HasGroup(string groups, string group)
        {
            if (string.IsNullOrEmpty(groups)) return false;
            foreach (var g in groups.Split(';'))
                if (string.Equals(g.Trim(), group, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Human-readable name of a binding, e.g. "Space" or "A".</summary>
        public string GetBindingName(string actionName, string group, string part = null)
        {
            var action = Actions.FindAction(actionName);
            int index = FindBindingIndex(actionName, group, part);
            if (action == null || index < 0) return "-";
            string display = action.GetBindingDisplayString(index);
            return string.IsNullOrEmpty(display) ? "-" : display;
        }

        /// <summary>Text for a button in the current input scheme, used by HUD prompts.</summary>
        public string PromptFor(string actionName)
        {
            string name = GetBindingName(actionName, LastInputWasGamepad ? GamepadGroup : KeyboardGroup);
            return name == "-" ? actionName : name;
        }

        /// <summary>
        /// Waits for the player to press a new key/button for an action. Esc cancels, and so does
        /// waiting 6 seconds. If the new control is already used by another action, the two swap.
        /// </summary>
        public bool StartRebind(string actionName, string group, string part, Action<bool> onFinished)
        {
            if (IsRebinding) return false;
            var action = Actions.FindAction(actionName);
            int index = FindBindingIndex(actionName, group, part);
            if (action == null || index < 0)
            {
                onFinished?.Invoke(false);
                return false;
            }

            string oldPath = action.bindings[index].effectivePath;
            bool keyboard = group == KeyboardGroup;
            Actions.Disable();

            var op = action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath(keyboard ? "<Keyboard>" : "<Gamepad>")
                .WithControlsExcluding("<Keyboard>/escape")
                .WithControlsExcluding("<Gamepad>/start")
                .WithControlsExcluding("<Gamepad>/leftStick")
                .WithControlsExcluding("<Gamepad>/rightStick")
                .WithControlsExcluding("<Mouse>")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithExpectedControlType("Button")
                .WithTimeout(6f)
                .OnMatchWaitForAnother(0.1f);

            op.OnComplete(o =>
            {
                FinishRebind();
                ResolveConflicts(action, index, oldPath, keyboard);
                SaveBindings();
                onFinished?.Invoke(true);
            });
            op.OnCancel(o =>
            {
                FinishRebind();
                onFinished?.Invoke(false);
            });

            rebinding = op;
            op.Start();
            return true;
        }

        void FinishRebind()
        {
            if (rebinding != null)
            {
                rebinding.Dispose();
                rebinding = null;
            }
            BlockMenuInput(3);
            if (isActiveAndEnabled) Actions.Enable();
        }

        public void CancelRebind()
        {
            if (rebinding != null) rebinding.Cancel();
        }

        /// <summary>If another action already uses the new control, give it the old one instead.</summary>
        void ResolveConflicts(InputAction changed, int changedIndex, string oldPath, bool keyboard)
        {
            string newPath = changed.bindings[changedIndex].effectivePath;
            if (string.IsNullOrEmpty(newPath)) return;
            string primary = keyboard ? KeyboardGroup : GamepadGroup;
            string extra = keyboard ? KeyboardExtraGroup : GamepadExtraGroup;

            foreach (var action in Actions.actions)
            {
                if (action == pause || action == cancel) continue;
                var bindings = action.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (action == changed && i == changedIndex) continue;
                    var b = bindings[i];
                    if (b.isComposite || !string.Equals(b.effectivePath, newPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (HasGroup(b.groups, primary)) action.ApplyBindingOverride(i, oldPath);
                    else if (HasGroup(b.groups, extra)) action.ApplyBindingOverride(i, ""); // an empty path disables it
                }
            }
        }

        public void ResetBindings()
        {
            CancelRebind();
            Actions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(OverridesKey);
            BindingsChanged?.Invoke();
        }

        void SaveBindings()
        {
            var saved = new SavedBindings { version = BindingLayoutVersion };
            foreach (var action in Actions.actions)
            {
                var bindings = action.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    // null = not overridden; an empty string = disabled (kept).
                    string path = bindings[i].overridePath;
                    if (path == null) continue;
                    saved.bindings.Add(new SavedBinding { action = action.name, index = i, path = path });
                }
            }
            PlayerPrefs.SetString(OverridesKey, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
            BindingsChanged?.Invoke();
        }

        void LoadBindings()
        {
            string json = PlayerPrefs.GetString(OverridesKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedBindings>(json);
                if (saved == null || saved.version != BindingLayoutVersion || saved.bindings == null) return;
                foreach (var entry in saved.bindings)
                {
                    var action = Actions.FindAction(entry.action);
                    if (action == null || entry.index < 0 || entry.index >= action.bindings.Count) continue;
                    if (action.bindings[entry.index].isComposite) continue;
                    action.ApplyBindingOverride(entry.index, entry.path ?? "");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Alma] Couldn't load saved key bindings, using defaults: " + e.Message);
                Actions.RemoveAllBindingOverrides();
            }
        }
    }
}
