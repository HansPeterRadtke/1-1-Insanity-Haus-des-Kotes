using Godot;
using Insanity.Scripts.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Insanity.Scripts.Animation
{
    public readonly struct ActorAnimationInput
    {
        public ActorAnimationInput(
            Vector2 velocity,
            bool isGrounded,
            bool isCrouched,
            bool isAttacking,
            bool isHit,
            bool isDead,
            bool justLanded = false,
            bool isDashing = false,
            float facingX = 1.0f,
            string requestedState = "")
        {
            Velocity = velocity;
            IsGrounded = isGrounded;
            IsCrouched = isCrouched;
            IsAttacking = isAttacking;
            IsHit = isHit;
            IsDead = isDead;
            JustLanded = justLanded;
            IsDashing = isDashing;
            FacingX = facingX;
            RequestedState = requestedState ?? string.Empty;
        }

        public Vector2 Velocity { get; }
        public bool IsGrounded { get; }
        public bool IsCrouched { get; }
        public bool IsAttacking { get; }
        public bool IsHit { get; }
        public bool IsDead { get; }
        public bool JustLanded { get; }
        public bool IsDashing { get; }
        public float FacingX { get; }
        public string RequestedState { get; }
    }

    public partial class AnimationController : Node
    {
        [Export] public string ActorId = "Player";
        [Export] public bool DeterministicVariants;
        [Export] public int DeterministicSeed = 1;
        [Export] public bool LogStateChanges { get; set; }
        [Export] public bool LogEvents { get; set; }
        [Export] private string _defaultState = AnimationStates.Idle;
        [Export] private NodePath _animatedSpritePath = "../AnimatedSprite2D";
        [Export] private NodePath _fallbackSpritePath = "../Sprite2D";
        [Export] private NodePath _animationPlayerPath = "AnimationPlayer";
        [Export] private NodePath _animationTreePath = "AnimationTree";
        [Export] private NodePath _sfxPlayerPath = "SfxPlayer";

        private readonly AnimationRegistry _registry = new();
        private readonly RandomNumberGenerator _variantRng = new();
        private readonly Dictionary<string, AnimationRegistry.AnimationVariant> _preparedClips = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _stateDurations = new(StringComparer.Ordinal);
        private readonly HashSet<string> _registeredStates = new(StringComparer.Ordinal);
        private AnimatedSprite2D _animatedSprite;
        private Sprite2D _fallbackSprite;
        private AnimationPlayer _animationPlayer;
        private AnimationTree _animationTree;
        private SfxPlayer _sfxPlayer;
        private AnimationLibrary _animationLibrary;
        private AnimationNodeStateMachine _stateMachineRoot;
        private AnimationNodeStateMachinePlayback _playback;
        private Texture2D _fallbackTexture;
        private bool _isAvailable;
        private string _currentState = string.Empty;
        private string _currentVariant = string.Empty;
        private string _requestedBaseState = AnimationStates.Idle;
        private string _lockedOneShotState = string.Empty;
        private double _lockedOneShotRemaining;

        public event Action<string, string, bool> StateChanged;
        public event Action<string> AnimationEventRaised;

        public string CurrentState => _currentState;
        public string CurrentVariant => _currentVariant;

        public override void _Ready()
        {
            ResolveNodes();
            ConfigureVariantSelection();
            _ = _registry.GetKnownStates(ActorId);
            _sfxPlayer?.Configure(_registry, ActorId, DeterministicVariants ? DeterministicSeed : null);
            _fallbackTexture = _fallbackSprite?.Texture;

            if (_animatedSprite == null || _animationPlayer == null || _animationTree == null)
            {
                _isAvailable = false;
                if (_fallbackSprite != null)
                {
                    _fallbackSprite.Visible = true;
                }
                return;
            }

            _animationPlayer.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Idle;
            _animationPlayer.CallbackModeMethod = AnimationMixer.AnimationCallbackModeMethod.Immediate;
            _animationPlayer.RootNode = _animationPlayer.GetPathTo(GetParent());
            _animationPlayer.AnimationFinished += OnAnimationFinished;

            if (_animatedSprite.SpriteFrames == null)
            {
                _animatedSprite.SpriteFrames = new SpriteFrames();
            }

            _animationLibrary = _animationPlayer.HasAnimationLibrary("")
                ? _animationPlayer.GetAnimationLibrary("")
                : null;
            if (_animationLibrary == null)
            {
                _animationLibrary = new AnimationLibrary();
                _animationPlayer.AddAnimationLibrary("", _animationLibrary);
            }

            BuildStateMachine();
            _playback = _animationTree.Get("parameters/playback").AsGodotObject() as AnimationNodeStateMachinePlayback;
            _isAvailable = _playback != null;
            _animationTree.Active = _isAvailable;

            if (_isAvailable)
            {
                if (_fallbackSprite != null)
                {
                    _fallbackSprite.Visible = false;
                }
                _animatedSprite.Visible = true;
                PlayState(_defaultState, force: true);
            }
        }

        public override void _Process(double delta)
        {
            if (_lockedOneShotRemaining > 0.0)
            {
                _lockedOneShotRemaining = Math.Max(0.0, _lockedOneShotRemaining - delta);
                if (_lockedOneShotRemaining <= 0.0)
                {
                    ClearOneShotLock();
                }
            }
        }

        public override void _ExitTree()
        {
            if (_animationPlayer != null)
            {
                _animationPlayer.AnimationFinished -= OnAnimationFinished;
            }

            _preparedClips.Clear();
            _stateDurations.Clear();
            _registry.Clear();
        }

        public void UpdateFromGameplay(ActorAnimationInput input)
        {
            string requestedState = string.IsNullOrWhiteSpace(input.RequestedState)
                ? string.Empty
                : AnimationStates.Normalize(input.RequestedState);

            UpdateFacing(input.FacingX);
            _requestedBaseState = DetermineBaseState(input);

            if (!_isAvailable)
            {
                return;
            }

            if (input.IsDead || string.Equals(requestedState, AnimationStates.Die, StringComparison.Ordinal))
            {
                PlayOneShot(AnimationStates.Die);
                return;
            }

            if (input.IsHit || string.Equals(requestedState, AnimationStates.Hit, StringComparison.Ordinal))
            {
                PlayOneShot(AnimationStates.Hit);
                return;
            }

            if (!string.IsNullOrEmpty(_lockedOneShotState))
            {
                return;
            }

            if (input.JustLanded)
            {
                PlayOneShot(AnimationStates.Land);
                return;
            }

            if (!string.IsNullOrEmpty(requestedState))
            {
                _requestedBaseState = AnimationStates.IsOneShotPreferred(requestedState)
                    ? _requestedBaseState
                    : requestedState;

                if (AnimationStates.IsOneShotPreferred(requestedState))
                {
                    PlayOneShot(requestedState);
                }
                else
                {
                    PlayState(requestedState);
                }

                return;
            }

            if (input.IsAttacking)
            {
                PlayOneShot(AnimationStates.AttackMelee);
                return;
            }

            PlayState(_requestedBaseState);
        }

        public void PlayState(string stateName)
        {
            PlayState(stateName, force: false);
        }

        public void PlayOneShot(string stateName)
        {
            string normalized = AnimationStates.Normalize(stateName);
            if (!_isAvailable)
            {
                return;
            }

            if (string.Equals(_lockedOneShotState, normalized, StringComparison.Ordinal) && _lockedOneShotRemaining > 0.0)
            {
                return;
            }

            EnsureStateRegistered(normalized);
            AnimationRegistry.AnimationVariant clip = PrepareState(normalized, selectFreshVariant: true);
            _lockedOneShotState = normalized;
            _lockedOneShotRemaining = Math.Max(0.05, clip.Duration);
            Travel(normalized);
        }

        public void TriggerEvent(string eventName)
        {
            _sfxPlayer?.PlayEvent(eventName);
        }

        public void OnAnimEvent(string eventName)
        {
            string normalized = AnimationEvents.Normalize(eventName);
            if (LogEvents)
            {
                GD.Print($"[AnimationController] {GetParent()?.Name ?? Name} event '{normalized}'");
            }

            AnimationEventRaised?.Invoke(normalized);
            TriggerEvent(normalized);
        }

        public double GetSuggestedDuration(string stateName)
        {
            string normalized = AnimationStates.Normalize(stateName);
            if (_stateDurations.TryGetValue(normalized, out double duration))
            {
                return duration;
            }

            if (!_isAvailable)
            {
                return 0.2;
            }

            EnsureStateRegistered(normalized);
            PrepareState(normalized, selectFreshVariant: false);
            return _stateDurations.TryGetValue(normalized, out duration) ? duration : 0.2;
        }

        private void PlayState(string stateName, bool force)
        {
            string normalized = AnimationStates.Normalize(stateName);
            if (!_isAvailable)
            {
                return;
            }

            if (!force && string.Equals(_currentState, normalized, StringComparison.Ordinal))
            {
                return;
            }

            EnsureStateRegistered(normalized);
            PrepareState(normalized, selectFreshVariant: true);
            Travel(normalized);
        }

        private void Travel(string stateName)
        {
            string normalized = AnimationStates.Normalize(stateName);
            bool restart = !string.Equals(_currentState, normalized, StringComparison.Ordinal);
            _currentState = normalized;
            if (_playback == null)
            {
                return;
            }

            if (_playback.IsPlaying())
            {
                _playback.Travel(_currentState, true);
            }
            else
            {
                _playback.Start(_currentState, true);
            }

            if (restart && LogStateChanges)
            {
                GD.Print($"[AnimationController] {GetParent()?.Name ?? Name} state '{_currentState}' variant '{_currentVariant}'");
            }

            StateChanged?.Invoke(_currentState, _currentVariant, !string.IsNullOrEmpty(_lockedOneShotState));
        }

        private void ResolveNodes()
        {
            _animatedSprite = GetNodeOrNull<AnimatedSprite2D>(_animatedSpritePath);
            _fallbackSprite = GetNodeOrNull<Sprite2D>(_fallbackSpritePath);
            _animationPlayer = GetNodeOrNull<AnimationPlayer>(_animationPlayerPath);
            _animationTree = GetNodeOrNull<AnimationTree>(_animationTreePath);
            _sfxPlayer = GetNodeOrNull<SfxPlayer>(_sfxPlayerPath);
        }

        private void ConfigureVariantSelection()
        {
            if (DeterministicVariants)
            {
                _variantRng.Seed = (ulong)Math.Max(1, DeterministicSeed);
                return;
            }

            _variantRng.Randomize();
        }

        private void BuildStateMachine()
        {
            _preparedClips.Clear();
            _stateDurations.Clear();
            _registeredStates.Clear();
            _stateMachineRoot = new AnimationNodeStateMachine
            {
                AllowTransitionToSelf = true,
            };

            List<string> orderedStates = new();
            foreach (string stateName in AnimationStates.All)
            {
                AddUniqueState(orderedStates, stateName);
            }

            foreach (string stateName in _registry.GetKnownStates(ActorId))
            {
                AddUniqueState(orderedStates, stateName);
            }

            AddUniqueState(orderedStates, _defaultState);

            _animationTree.AnimPlayer = _animationTree.GetPathTo(_animationPlayer);
            _animationTree.TreeRoot = _stateMachineRoot;

            for (int i = 0; i < orderedStates.Count; i++)
            {
                AddStateToStateMachine(orderedStates[i], i);
            }
        }

        private void AddStateToStateMachine(string stateName, int index)
        {
            string normalized = AnimationStates.Normalize(stateName);
            if (string.IsNullOrEmpty(normalized) || _registeredStates.Contains(normalized))
            {
                return;
            }

            AnimationNodeAnimation node = new()
            {
                Animation = normalized,
            };
            _stateMachineRoot?.AddNode(normalized, node, new Vector2((index % 4) * 220.0f, (index / 4) * 120.0f));
            _registeredStates.Add(normalized);

            foreach (string existingState in _registeredStates)
            {
                if (string.Equals(existingState, normalized, StringComparison.Ordinal))
                {
                    continue;
                }

                AddTransition(existingState, normalized);
                AddTransition(normalized, existingState);
            }

            PrepareState(normalized, selectFreshVariant: false);
        }

        private void AddTransition(string from, string to)
        {
            if (_stateMachineRoot == null || _stateMachineRoot.HasTransition(from, to))
            {
                return;
            }

            AnimationNodeStateMachineTransition transition = new()
            {
                XfadeTime = 0.0f,
            };
            _stateMachineRoot.AddTransition(from, to, transition);
        }

        private void EnsureStateRegistered(string stateName)
        {
            string normalized = AnimationStates.Normalize(stateName);
            if (_registeredStates.Contains(normalized))
            {
                return;
            }

            AddStateToStateMachine(normalized, _registeredStates.Count);
        }

        private AnimationRegistry.AnimationVariant PrepareState(string stateName, bool selectFreshVariant)
        {
            stateName = AnimationStates.Normalize(stateName);
            if (_animatedSprite?.SpriteFrames == null || _animationLibrary == null || _animationPlayer == null)
            {
                return BuildPlaceholderClip(stateName);
            }

            if (!selectFreshVariant && _preparedClips.TryGetValue(stateName, out AnimationRegistry.AnimationVariant prepared))
            {
                return prepared;
            }

            AnimationRegistry.AnimationVariant clip = SelectVariant(stateName, selectFreshVariant);
            ApplySpriteFrames(stateName, clip);
            RebuildAnimationClip(stateName, clip);
            _preparedClips[stateName] = clip;
            _stateDurations[stateName] = clip.Duration;
            _currentVariant = clip.VariantName;
            return clip;
        }

        private AnimationRegistry.AnimationVariant SelectVariant(string stateName, bool selectFreshVariant)
        {
            IReadOnlyList<AnimationRegistry.AnimationVariant> variants = _registry.GetAnimationVariants(ActorId, stateName);
            if (variants.Count == 0)
            {
                return BuildPlaceholderClip(stateName);
            }

            int index = selectFreshVariant ? PickVariantIndex(variants.Count) : 0;
            return variants[index];
        }

        private AnimationRegistry.AnimationVariant BuildPlaceholderClip(string stateName)
        {
            Texture2D fallbackTexture = _fallbackTexture ?? CreateGeneratedPlaceholderTexture();
            return new AnimationRegistry.AnimationVariant
            {
                ActorId = ActorId,
                StateName = AnimationStates.Normalize(stateName),
                VariantName = "generated_placeholder",
                Frames = new[] { fallbackTexture },
                FramesPerSecond = 6.0f,
                Loop = AnimationStates.IsLooping(stateName),
                SourcePath = "runtime://placeholder",
            };
        }

        private void ApplySpriteFrames(string stateName, AnimationRegistry.AnimationVariant clip)
        {
            SpriteFrames frames = _animatedSprite.SpriteFrames;
            if (frames.HasAnimation(stateName))
            {
                frames.RemoveAnimation(stateName);
            }

            frames.AddAnimation(stateName);
            frames.SetAnimationLoop(stateName, clip.Loop);
            frames.SetAnimationSpeed(stateName, clip.FramesPerSecond);
            foreach (Texture2D frame in clip.Frames)
            {
                frames.AddFrame(stateName, frame);
            }
        }

        private void RebuildAnimationClip(string stateName, AnimationRegistry.AnimationVariant clip)
        {
            if (_animationLibrary.HasAnimation(stateName))
            {
                _animationLibrary.RemoveAnimation(stateName);
            }

            Godot.Animation animation = new()
            {
                Length = (float)clip.Duration,
                LoopMode = clip.Loop ? Godot.Animation.LoopModeEnum.Linear : Godot.Animation.LoopModeEnum.None,
            };

            Node actor = GetParent();
            NodePath spritePath = actor.GetPathTo(_animatedSprite);
            NodePath controllerPath = actor.GetPathTo(this);

            int animationTrack = animation.AddTrack(Godot.Animation.TrackType.Value);
            animation.TrackSetPath(animationTrack, new NodePath($"{spritePath}:animation"));
            animation.ValueTrackSetUpdateMode(animationTrack, Godot.Animation.UpdateMode.Discrete);
            animation.TrackInsertKey(animationTrack, 0.0, stateName);

            int playingTrack = animation.AddTrack(Godot.Animation.TrackType.Value);
            animation.TrackSetPath(playingTrack, new NodePath($"{spritePath}:playing"));
            animation.ValueTrackSetUpdateMode(playingTrack, Godot.Animation.UpdateMode.Discrete);
            animation.TrackInsertKey(playingTrack, 0.0, true);

            int frameTrack = animation.AddTrack(Godot.Animation.TrackType.Value);
            animation.TrackSetPath(frameTrack, new NodePath($"{spritePath}:frame"));
            animation.ValueTrackSetUpdateMode(frameTrack, Godot.Animation.UpdateMode.Discrete);
            animation.TrackInsertKey(frameTrack, 0.0, 0);

            IReadOnlyList<AnimationEventCue> cues = AnimationEvents.GetDefaultCues(stateName);
            if (cues.Count > 0)
            {
                int methodTrack = animation.AddTrack(Godot.Animation.TrackType.Method);
                animation.TrackSetPath(methodTrack, controllerPath);
                foreach (AnimationEventCue cue in cues)
                {
                    double eventTime = Math.Clamp(cue.NormalizedTime * clip.Duration, 0.0, Math.Max(0.0, clip.Duration - 0.001));
                    Godot.Collections.Array args = new() { cue.EventName };
                    Godot.Collections.Dictionary call = new()
                    {
                        ["method"] = new StringName(nameof(OnAnimEvent)),
                        ["args"] = args,
                    };
                    animation.TrackInsertKey(methodTrack, eventTime, call);
                }
            }

            _animationLibrary.AddAnimation(stateName, animation);
        }

        private void UpdateFacing(float facingX)
        {
            bool flip = facingX < 0.0f;
            if (_animatedSprite != null)
            {
                _animatedSprite.FlipH = flip;
            }

            if (_fallbackSprite != null)
            {
                _fallbackSprite.FlipH = flip;
            }
        }

        private void ClearOneShotLock()
        {
            if (string.IsNullOrEmpty(_lockedOneShotState))
            {
                return;
            }

            _lockedOneShotState = string.Empty;
            _lockedOneShotRemaining = 0.0;
            PlayState(_requestedBaseState, force: true);
        }

        private void OnAnimationFinished(StringName animationName)
        {
            if (string.Equals(animationName.ToString(), _lockedOneShotState, StringComparison.Ordinal))
            {
                ClearOneShotLock();
            }
        }

        private int PickVariantIndex(int count)
        {
            return count <= 1 ? 0 : (int)_variantRng.RandiRange(0, count - 1);
        }

        private static Texture2D CreateGeneratedPlaceholderTexture()
        {
            Image image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
            image.Fill(new Color(0.18f, 0.8f, 0.64f, 1.0f));
            image.FillRect(new Rect2I(4, 4, 24, 24), new Color(0.06f, 0.14f, 0.16f, 1.0f));
            image.FillRect(new Rect2I(10, 10, 12, 12), new Color(0.92f, 0.96f, 0.94f, 1.0f));
            return ImageTexture.CreateFromImage(image);
        }

        private static string DetermineBaseState(ActorAnimationInput input)
        {
            if (!input.IsGrounded)
            {
                return input.Velocity.Y < 0.0f ? AnimationStates.Jump : AnimationStates.Fall;
            }

            if (input.IsCrouched)
            {
                return AnimationStates.Crouch;
            }

            if (Mathf.Abs(input.Velocity.X) > 5.0f)
            {
                return input.IsDashing ? AnimationStates.Dash : AnimationStates.Run;
            }

            return AnimationStates.Idle;
        }

        private static void AddUniqueState(ICollection<string> states, string stateName)
        {
            string normalized = AnimationStates.Normalize(stateName);
            if (!states.Contains(normalized))
            {
                states.Add(normalized);
            }
        }
    }
}
