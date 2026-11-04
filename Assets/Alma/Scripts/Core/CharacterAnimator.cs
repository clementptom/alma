using System.Collections.Generic;
using UnityEngine;

namespace Alma
{
    /// <summary>
    /// Animator state names used by the code. Name your Animator states exactly like this.
    /// </summary>
    public static class AnimStates
    {
        // Shared
        public const string Idle = "Idle";
        public const string Hurt = "Hurt";
        public const string Death = "Death";
        public const string Attack = "Attack";

        // Player
        public const string Run = "Run";
        public const string Jump = "Jump";
        public const string DoubleJump = "DoubleJump";
        public const string Fall = "Fall";
        public const string Land = "Land";
        public const string WallSlide = "WallSlide";
        public const string Dash = "Dash";
        public const string Glide = "Glide";
        public const string Attack2 = "Attack2";
        public const string AttackUp = "AttackUp";
        public const string AttackDown = "AttackDown";
        public const string Heal = "Heal";
        public const string Roll = "Roll";
        public const string Parry = "Parry";
        public const string Sneak = "Sneak";
        public const string Crouch = "Crouch";
        public const string Throw = "Throw";
        public const string Swim = "Swim";
        public const string Downed = "Downed";

        // Enemies and bosses
        public const string Move = "Move";
        public const string Telegraph = "Telegraph";
        public const string Charge = "Charge";
        public const string Leap = "Leap";
        public const string Stunned = "Stunned";
        public const string Wake = "Wake";
        public const string Search = "Search";
        public const string Rise = "Rise";
        public const string Lunge = "Lunge";

        // Barro
        public const string Sit = "Sit";
        public const string Bark = "Bark";
        public const string Growl = "Growl";
        public const string Bite = "Bite";
        public const string Sniff = "Sniff";
        public const string Howl = "Howl";
        public const string Drag = "Drag";

        // Objects
        public const string Open = "Open";
        public const string Closed = "Closed";
        public const string Active = "Active";

        public static readonly string[] PlayerStates =
        {
            Idle, Run, Jump, DoubleJump, Fall, Land, WallSlide, Dash, Glide,
            Attack, Attack2, AttackUp, AttackDown, Heal, Roll, Parry, Sneak, Crouch, Throw, Swim, Downed, Hurt, Death
        };

        public static readonly string[] GuardStates = { Idle, Move, Run, Search, Telegraph, Attack, Stunned, Hurt, Death };

        public static readonly string[] DuelistStates = { Idle, Move, Telegraph, Attack, Attack2, Lunge, Stunned, Hurt, Death };

        public static readonly string[] DrownedStates = { Idle, Rise, Telegraph, Attack, Hurt, Death };

        public static readonly string[] CompanionStates = { Idle, Run, Jump, Fall, Sit, Bark, Growl, Bite, Sniff, Howl, Drag, Hurt, Downed };

        public static readonly string[] EnemyStates = { Idle, Move, Attack, Hurt, Death };

        public static readonly string[] BossStates = { Idle, Wake, Move, Telegraph, Charge, Leap, Attack, Stunned, Hurt, Death };
    }

    /// <summary>
    /// Plays Animator states by name, with fallbacks for states that don't exist yet.
    /// That way you can ship placeholder art with only Idle and Run, and add the rest later.
    /// </summary>
    public class CharacterAnimator
    {
        static readonly Dictionary<string, string> Fallbacks = new Dictionary<string, string>
        {
            { AnimStates.Attack2, AnimStates.Attack },
            { AnimStates.AttackUp, AnimStates.Attack },
            { AnimStates.AttackDown, AnimStates.Attack },
            { AnimStates.DoubleJump, AnimStates.Jump },
            { AnimStates.Glide, AnimStates.Fall },
            { AnimStates.WallSlide, AnimStates.Fall },
            { AnimStates.Fall, AnimStates.Jump },
            { AnimStates.Land, AnimStates.Idle },
            { AnimStates.Heal, AnimStates.Idle },
            { AnimStates.Roll, AnimStates.Dash },
            { AnimStates.Parry, AnimStates.Attack },
            { AnimStates.Sneak, AnimStates.Run },
            { AnimStates.Crouch, AnimStates.Idle },
            { AnimStates.Throw, AnimStates.Attack },
            { AnimStates.Swim, AnimStates.Fall },
            { AnimStates.Downed, AnimStates.Death },
            { AnimStates.Search, AnimStates.Idle },
            { AnimStates.Rise, AnimStates.Attack },
            { AnimStates.Lunge, AnimStates.Attack },
            { AnimStates.Sit, AnimStates.Idle },
            { AnimStates.Bark, AnimStates.Idle },
            { AnimStates.Growl, AnimStates.Bark },
            { AnimStates.Bite, AnimStates.Attack },
            { AnimStates.Sniff, AnimStates.Idle },
            { AnimStates.Howl, AnimStates.Bark },
            { AnimStates.Drag, AnimStates.Run },
            { AnimStates.Dash, AnimStates.Run },
            { AnimStates.Jump, AnimStates.Idle },
            { AnimStates.Run, AnimStates.Idle },
            { AnimStates.Death, AnimStates.Hurt },
            { AnimStates.Hurt, AnimStates.Idle },
            { AnimStates.Attack, AnimStates.Idle },
            { AnimStates.Move, AnimStates.Run },
            { AnimStates.Telegraph, AnimStates.Attack },
            { AnimStates.Charge, AnimStates.Move },
            { AnimStates.Leap, AnimStates.Jump },
            { AnimStates.Stunned, AnimStates.Hurt },
            { AnimStates.Wake, AnimStates.Idle },
            { AnimStates.Open, AnimStates.Idle },
            { AnimStates.Closed, AnimStates.Idle },
            { AnimStates.Active, AnimStates.Idle },
        };

        readonly Animator animator;
        readonly Dictionary<string, int> resolved = new Dictionary<string, int>();
        RuntimeAnimatorController cachedController;
        int currentHash;

        public CharacterAnimator(Animator animator)
        {
            this.animator = animator;
        }

        public Animator Animator => animator;

        public bool IsValid => animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled;

        public void Play(string state, bool restart = false)
        {
            if (!IsValid) return;
            if (cachedController != animator.runtimeAnimatorController)
            {
                cachedController = animator.runtimeAnimatorController;
                resolved.Clear();
                currentHash = 0;
            }

            int hash = Resolve(state);
            if (hash == 0) return;
            if (!restart && hash == currentHash) return;
            animator.Play(hash, 0, 0f);
            currentHash = hash;
        }

        public bool HasState(string state) => IsValid && animator.HasState(0, Animator.StringToHash(state));

        int Resolve(string state)
        {
            if (resolved.TryGetValue(state, out int cached)) return cached;
            string candidate = state;
            int result = 0;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(candidate); i++)
            {
                int hash = Animator.StringToHash(candidate);
                if (animator.HasState(0, hash))
                {
                    result = hash;
                    break;
                }
                if (!Fallbacks.TryGetValue(candidate, out candidate)) break;
            }
            resolved[state] = result;
            return result;
        }
    }
}
