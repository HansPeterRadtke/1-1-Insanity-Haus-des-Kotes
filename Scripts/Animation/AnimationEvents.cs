using System;
using System.Collections.Generic;

namespace Insanity.Scripts.Animation
{
    public readonly struct AnimationEventCue
    {
        public AnimationEventCue(string eventName, double normalizedTime)
        {
            EventName = AnimationEvents.Normalize(eventName);
            NormalizedTime = Math.Clamp(normalizedTime, 0.0, 1.0);
        }

        public string EventName { get; }
        public double NormalizedTime { get; }
    }

    public static class AnimationEvents
    {
        public const string Footstep = "footstep";
        public const string Jump = "jump";
        public const string Swing = "swing";
        public const string Hit = "hit";
        public const string Shoot = "shoot";
        public const string Explode = "explode";
        public const string Land = "land";
        public const string Pickup = "pickup";

        private static readonly string[] _all =
        {
            Footstep,
            Jump,
            Swing,
            Hit,
            Shoot,
            Explode,
            Land,
            Pickup,
        };

        private static readonly Dictionary<string, AnimationEventCue[]> _defaultCues = new(StringComparer.Ordinal)
        {
            [AnimationStates.Run] = new[]
            {
                new AnimationEventCue(Footstep, 0.18),
                new AnimationEventCue(Footstep, 0.62),
            },
            [AnimationStates.Dash] = new[]
            {
                new AnimationEventCue(Footstep, 0.14),
                new AnimationEventCue(Footstep, 0.48),
            },
            [AnimationStates.WallSlide] = new[]
            {
                new AnimationEventCue(Footstep, 0.32),
            },
            [AnimationStates.Jump] = new[]
            {
                new AnimationEventCue(Jump, 0.0),
            },
            [AnimationStates.WallJump] = new[]
            {
                new AnimationEventCue(Jump, 0.0),
            },
            [AnimationStates.Land] = new[]
            {
                new AnimationEventCue(Land, 0.0),
            },
            [AnimationStates.AttackMelee] = new[]
            {
                new AnimationEventCue(Swing, 0.22),
            },
            [AnimationStates.AttackRanged] = new[]
            {
                new AnimationEventCue(Shoot, 0.08),
            },
            [AnimationStates.Hit] = new[]
            {
                new AnimationEventCue(Hit, 0.0),
            },
            [AnimationStates.Die] = new[]
            {
                new AnimationEventCue(Hit, 0.0),
            },
            [AnimationStates.Pickup] = new[]
            {
                new AnimationEventCue(Pickup, 0.0),
            },
        };

        public static IReadOnlyList<string> All => _all;

        public static IReadOnlyList<AnimationEventCue> GetDefaultCues(string stateName)
        {
            foreach (string candidate in AnimationStates.BuildFallbackChain(stateName))
            {
                if (_defaultCues.TryGetValue(candidate, out AnimationEventCue[] cues))
                {
                    return cues;
                }
            }

            return Array.Empty<AnimationEventCue>();
        }

        public static IEnumerable<string> BuildFallbackChain(string requestedEvent)
        {
            string normalized = Normalize(requestedEvent);
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
        }

        public static string Normalize(string eventName)
        {
            return string.IsNullOrWhiteSpace(eventName) ? Footstep : eventName.Trim().ToLowerInvariant();
        }
    }
}
