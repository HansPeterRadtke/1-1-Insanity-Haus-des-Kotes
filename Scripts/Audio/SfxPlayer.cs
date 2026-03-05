using Godot;
using Insanity.Scripts.Animation;
using System;
using System.Collections.Generic;

namespace Insanity.Scripts.Audio
{
    public partial class SfxPlayer : AudioStreamPlayer2D
    {
        [Export] public bool LogPlayback { get; set; }
        [Export] public bool UseGeneratedFallbackSfx { get; set; }

        private readonly RandomNumberGenerator _rng = new();
        private readonly Dictionary<string, AudioStreamWav> _generatedFallbacks = new(StringComparer.Ordinal);
        private AnimationRegistry _registry;
        private string _actorId = string.Empty;

        public override void _Ready()
        {
            ConfigureRandomSeed(null);
        }

        public override void _ExitTree()
        {
            Stop();
            Stream = null;
            _generatedFallbacks.Clear();
        }

        public void Configure(AnimationRegistry registry, string actorId, int? deterministicSeed)
        {
            _registry = registry;
            _actorId = actorId?.Trim() ?? string.Empty;
            ConfigureRandomSeed(deterministicSeed);
        }

        public bool PlayEvent(string eventName)
        {
            string normalizedEvent = AnimationEvents.Normalize(eventName);
            AudioStream stream = null;
            string variantName = string.Empty;

            IReadOnlyList<AnimationRegistry.SoundVariant> variants = _registry?.GetSoundVariants(_actorId, normalizedEvent)
                ?? Array.Empty<AnimationRegistry.SoundVariant>();

            if (variants.Count > 0)
            {
                int index = PickVariantIndex(variants.Count);
                stream = variants[index].Stream;
                variantName = variants[index].VariantName;
            }
            else if (UseGeneratedFallbackSfx)
            {
                stream = GetOrCreateFallbackTone(normalizedEvent);
                variantName = "debug_tone";
            }

            if (stream == null)
            {
                return false;
            }

            Stream = stream;
            Play();

            if (LogPlayback)
            {
                GD.Print($"[SfxPlayer] {GetParent()?.GetParent()?.Name ?? Name} played '{normalizedEvent}' variant '{variantName}'");
            }

            return true;
        }

        private void ConfigureRandomSeed(int? deterministicSeed)
        {
            if (deterministicSeed.HasValue)
            {
                _rng.Seed = (ulong)Math.Max(1, deterministicSeed.Value);
                return;
            }

            _rng.Randomize();
        }

        private int PickVariantIndex(int count)
        {
            return count <= 1 ? 0 : (int)_rng.RandiRange(0, count - 1);
        }

        private AudioStreamWav GetOrCreateFallbackTone(string eventName)
        {
            if (_generatedFallbacks.TryGetValue(eventName, out AudioStreamWav existing))
            {
                return existing;
            }

            const int mixRate = 22050;
            const double durationSeconds = 0.08;
            int sampleCount = (int)(mixRate * durationSeconds);
            byte[] pcm = new byte[sampleCount * 2];
            double frequency = eventName switch
            {
                AnimationEvents.Footstep => 180.0,
                AnimationEvents.Jump => 360.0,
                AnimationEvents.Swing => 520.0,
                AnimationEvents.Shoot => 680.0,
                AnimationEvents.Hit => 240.0,
                AnimationEvents.Explode => 120.0,
                AnimationEvents.Land => 140.0,
                AnimationEvents.Pickup => 880.0,
                _ => 440.0,
            };

            for (int i = 0; i < sampleCount; i++)
            {
                double envelope = 1.0 - (i / (double)sampleCount);
                short sample = (short)(Math.Sin(i * Math.Tau * frequency / mixRate) * envelope * short.MaxValue * 0.15);
                pcm[i * 2] = (byte)(sample & 0xff);
                pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
            }

            AudioStreamWav stream = new()
            {
                Data = pcm,
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = mixRate,
                Stereo = false,
                LoopMode = AudioStreamWav.LoopModeEnum.Disabled,
            };

            _generatedFallbacks[eventName] = stream;
            return stream;
        }
    }
}
