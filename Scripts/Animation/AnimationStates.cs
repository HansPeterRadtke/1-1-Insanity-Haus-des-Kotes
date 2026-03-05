using System;
using System.Collections.Generic;
using System.Linq;

namespace Insanity.Scripts.Animation
{
    public static class AnimationStates
    {
        public const string Idle = "idle";
        public const string Run = "run";
        public const string Jump = "jump";
        public const string Fall = "fall";
        public const string Land = "land";
        public const string Crouch = "crouch";
        public const string Dash = "dash";
        public const string WallSlide = "wall_slide";
        public const string WallJump = "wall_jump";
        public const string AttackMelee = "attack_melee";
        public const string AttackRanged = "attack_ranged";
        public const string AttackCharge = "attack_charge";
        public const string Hit = "hit";
        public const string Die = "die";
        public const string Interact = "interact";
        public const string Pickup = "pickup";
        public const string Spawn = "spawn";
        public const string Despawn = "despawn";

        private static readonly string[] _core =
        {
            Idle,
            Run,
            Jump,
            Fall,
            Land,
            Crouch,
            Dash,
            WallSlide,
            WallJump,
        };

        private static readonly string[] _combat =
        {
            AttackMelee,
            AttackRanged,
            AttackCharge,
            Hit,
            Die,
        };

        private static readonly string[] _interaction =
        {
            Interact,
            Pickup,
            Spawn,
            Despawn,
        };

        private static readonly string[] _all = _core
            .Concat(_combat)
            .Concat(_interaction)
            .ToArray();

        private static readonly HashSet<string> _loopingStates = new(StringComparer.Ordinal)
        {
            Idle,
            Run,
            Crouch,
            Dash,
            Fall,
            WallSlide,
            AttackCharge,
        };

        private static readonly HashSet<string> _oneShotPreferredStates = new(StringComparer.Ordinal)
        {
            Jump,
            Land,
            WallJump,
            AttackMelee,
            AttackRanged,
            Hit,
            Die,
            Interact,
            Pickup,
            Spawn,
            Despawn,
        };

        public static IReadOnlyList<string> Core => _core;
        public static IReadOnlyList<string> Combat => _combat;
        public static IReadOnlyList<string> Interaction => _interaction;
        public static IReadOnlyList<string> All => _all;

        public static bool IsLooping(string stateName)
        {
            return _loopingStates.Contains(Normalize(stateName));
        }

        public static bool IsOneShotPreferred(string stateName)
        {
            return _oneShotPreferredStates.Contains(Normalize(stateName));
        }

        public static bool IsAttackState(string stateName)
        {
            string normalized = Normalize(stateName);
            return normalized is AttackMelee or AttackRanged or AttackCharge;
        }

        public static IEnumerable<string> BuildFallbackChain(string requestedState)
        {
            string normalized = Normalize(requestedState);
            HashSet<string> yielded = new(StringComparer.Ordinal);

            if (yielded.Add(normalized))
            {
                yield return normalized;
            }

            string current = normalized;
            while (current.Contains('_'))
            {
                current = current[..current.LastIndexOf('_')];
                if (yielded.Add(current))
                {
                    yield return current;
                }
            }

            if (yielded.Add(Idle))
            {
                yield return Idle;
            }
        }

        public static string Normalize(string stateName)
        {
            return string.IsNullOrWhiteSpace(stateName) ? Idle : stateName.Trim().ToLowerInvariant();
        }
    }
}
