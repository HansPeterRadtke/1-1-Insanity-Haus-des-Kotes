using Godot;
using Insanity.Scripts.Animation;
using Insanity.Scripts.Game;
using Insanity.Scripts.Interaction;
using Insanity.Scripts.Shared;

namespace Insanity.Scripts.Player
{
    public partial class Player : CharacterBody2D, ISaveStateNode
    {
        [Export] private float _speed = 300.0f;
        [Export] private float _sprintMultiplier = 1.5f;
        [Export] private float _duckSpeedMultiplier = 0.4f;
        [Export] private float _duckHeight = 36.0f;
        [Export] private float _interactRadius = 42.0f;
        [Export] private float _jumpVelocity = -400.0f;
        [Export] private float _coyoteTime = 0.2f;
        [ExportGroup("Stats")]
        [Export] private int _maxHealth = 10;
        [Export] private int _startingAmmo = 12;
        [Export] private int _maxAmmo = 24;
        [Export] private float _damageInvulnerabilitySeconds = 0.65f;
        [Export] private float _defaultSpeedBoostMultiplier = 1.6f;

        private Vector2 _direction;
        private bool _flipped;
        private bool _isDucking;
        private float _timeSinceGrounded;
        private int _health;
        private int _ammo;
        private int _coins;
        private float _damageInvulnerabilityRemaining;
        private float _speedBoostRemaining;
        private float _speedBoostMultiplier = 1.0f;
        private Vector2 _spawnPosition;
        private RayCast2D _interactionRaycast;
        private GameManager _gameManager;
        private CollisionShape2D _collisionShape;
        private RectangleShape2D _collisionRectangle;
        private Sprite2D _sprite;
        private AnimatedSprite2D _animatedSprite;
        private AnimationController _animationController;
        private Vector2 _standingCollisionSize;
        private Vector2 _standingCollisionOffset;
        private Vector2 _duckCollisionSize;
        private Vector2 _duckCollisionOffset;
        private Vector2 _standingSpritePosition;
        private Vector2 _standingSpriteScale;
        private Vector2 _duckSpritePosition;
        private Vector2 _duckSpriteScale;
        private Vector2 _standingAnimatedSpritePosition;
        private Vector2 _standingAnimatedSpriteScale;
        private Vector2 _duckAnimatedSpritePosition;
        private Vector2 _duckAnimatedSpriteScale;
        private bool _wasGrounded;
        private bool _justLandedThisFrame;
        
        public float FacingDirection { get; private set; } = 1.0f;
        public bool IsDucking => _isDucking;
        public int Health => _health;
        public int MaxHealth => _maxHealth;
        public int Ammo => _ammo;
        public int MaxAmmo => _maxAmmo;
        public int Coins => _coins;
        public bool HasSpeedBoost => _speedBoostRemaining > 0.001f;
        public float SpeedBoostRemaining => _speedBoostRemaining;
        public float SpeedBoostMultiplier => HasSpeedBoost ? _speedBoostMultiplier : 1.0f;
        public float CurrentMoveSpeed => GetBaseMoveSpeed() * (_isDucking ? _duckSpeedMultiplier : 1.0f);

        public override void _Ready()
        {
            AddToGroup("player");
            AddToGroup("save_state");
            _gameManager = GetNode<GameManager>("/root/GameManager");
            _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
            _collisionRectangle = _collisionShape?.Shape as RectangleShape2D;
            _sprite = GetNode<Sprite2D>("Sprite2D");
            _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            _animationController = GetNodeOrNull<AnimationController>("AnimationController");
            _interactionRaycast = GetNode<RayCast2D>("InteractionRaycast");
            InitializeDuckState();
            InitializeStats();
            UpdateInteractionRaycast();
            _wasGrounded = IsOnFloor();
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector2 velocity = Velocity;
            UpdateTimers((float)delta);
    
            // Add the gravity.
            if (!IsOnFloor())
            {
                velocity += GetGravity() * (float)delta;
                _timeSinceGrounded += (float)delta;
            }
            else
            {
                _timeSinceGrounded = 0.0f;
            }
    
            bool jumpPressed = Input.IsActionJustPressed("jump");
            if (_isDucking && jumpPressed)
            {
                DisableDuck();
            }

            // Handle Jump.
            if (!_isDucking && jumpPressed && CanJump())
            {
                velocity.Y = _jumpVelocity;
            }

            if (Input.IsActionJustPressed("duck"))
            {
                ToggleDuck();
            }

            if (_isDucking && Input.IsActionJustPressed("up"))
            {
                DisableDuck();
            }
    
            float inputX = Input.GetAxis("move_left", "move_right");
            _direction = new Vector2(inputX, 0.0f);
            float moveSpeed = _isDucking ? GetBaseMoveSpeed() * _duckSpeedMultiplier : GetBaseMoveSpeed();
            float sprintMultiplier = _isDucking ? 1.0f : _sprintMultiplier;
            velocity.X = GameplayRules.ResolveHorizontalVelocity(
                Velocity.X,
                inputX,
                moveSpeed,
                sprintMultiplier,
                Input.IsActionPressed("sprint") && !_isDucking
            );

            if (Input.IsActionJustPressed("interact"))
            {
                TryInteract();
            }
    
            Velocity = velocity;
            MoveAndSlide();
            bool groundedNow = IsOnFloor();
            _justLandedThisFrame = !_wasGrounded && groundedNow;
            _wasGrounded = groundedNow;
            SyncAnimationState();
        }

        public override void _Process(double delta)
        {
            FacingDirection = GameplayRules.ResolveFacingDirection(FacingDirection, _direction.X);
            UpdateInteractionRaycast();
            UpdateStatusVisuals();

            if (_direction.X != 0.0f)
            {
                _flipped = _direction.X < 0;
                if (_sprite != null)
                {
                    _sprite.FlipH = _flipped;
                }
                if (_animatedSprite != null)
                {
                    _animatedSprite.FlipH = _flipped;
                }
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey key &&
                key.Pressed &&
                !key.Echo &&
                key.Keycode == Key.Escape &&
                _gameManager?.GameActive == true &&
                _gameManager.GamePaused == false)
            {
                _gameManager.TogglePauseMenu();
            }
        }

        private bool CanJump() => IsOnFloor() || _timeSinceGrounded < _coyoteTime;

        private void TryInteract()
        {
            Node nearbyInteractable = FindNearbyInteractable();
            if (nearbyInteractable is IInteractable nearby)
            {
                nearby.Interact(this);
                _animationController?.PlayOneShot(AnimationStates.Interact);
                return;
            }

            if (_interactionRaycast == null)
            {
                return;
            }

            var collider = _interactionRaycast.GetCollider();
            if (ResolveInteractableNode(collider) is IInteractable interactable)
            {
                interactable.Interact(this);
                _animationController?.PlayOneShot(AnimationStates.Interact);
            }
        }

        private Node FindNearbyInteractable()
        {
            CircleShape2D interactShape = new()
            {
                Radius = _interactRadius,
            };

            PhysicsShapeQueryParameters2D query = new()
            {
                Shape = interactShape,
                Transform = new Transform2D(0.0f, GlobalPosition),
                CollideWithBodies = true,
                CollideWithAreas = false,
            };
            query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

            var results = GetWorld2D().DirectSpaceState.IntersectShape(query, 8);
            Node bestNode = null;
            float bestDistanceSquared = float.MaxValue;

            foreach (Godot.Collections.Dictionary result in results)
            {
                if (!result.TryGetValue("collider", out Variant colliderValue))
                {
                    continue;
                }

                Node interactableNode = ResolveInteractableNode(colliderValue.AsGodotObject());
                if (interactableNode is not IInteractable)
                {
                    continue;
                }

                if (interactableNode is not Node2D node2D)
                {
                    return interactableNode;
                }

                float distanceSquared = GlobalPosition.DistanceSquaredTo(node2D.GlobalPosition);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestNode = interactableNode;
                }
            }

            return bestNode;
        }

        private static Node ResolveInteractableNode(object collider)
        {
            if (collider is not Node node)
            {
                return null;
            }

            if (node is IInteractable)
            {
                return node;
            }

            Node parent = node.GetParent();
            return parent is IInteractable ? parent : null;
        }

        private void UpdateInteractionRaycast()
        {
            if (_interactionRaycast == null)
            {
                return;
            }

            _interactionRaycast.TargetPosition = new Vector2(
                48.0f * FacingDirection,
                _isDucking ? 10.0f : 0.0f
            );
        }

        private void InitializeDuckState()
        {
            if (_collisionRectangle == null)
            {
                return;
            }

            _standingCollisionSize = _collisionRectangle.Size;
            _standingCollisionOffset = _collisionShape.Position;
            _duckCollisionSize = new Vector2(_standingCollisionSize.X, Mathf.Min(_duckHeight, _standingCollisionSize.Y));
            float offsetDelta = (_standingCollisionSize.Y - _duckCollisionSize.Y) * 0.5f;
            _duckCollisionOffset = _standingCollisionOffset + new Vector2(0.0f, offsetDelta);

            if (_sprite != null)
            {
                _standingSpritePosition = _sprite.Position;
                _standingSpriteScale = _sprite.Scale;
                _duckSpritePosition = _standingSpritePosition + new Vector2(0.0f, offsetDelta);
                _duckSpriteScale = new Vector2(_standingSpriteScale.X, _standingSpriteScale.Y * 0.6f);
            }

            if (_animatedSprite != null)
            {
                _standingAnimatedSpritePosition = _animatedSprite.Position;
                _standingAnimatedSpriteScale = _animatedSprite.Scale;
                _duckAnimatedSpritePosition = _standingAnimatedSpritePosition + new Vector2(0.0f, offsetDelta);
                _duckAnimatedSpriteScale = new Vector2(_standingAnimatedSpriteScale.X, _standingAnimatedSpriteScale.Y * 0.6f);
            }
        }

        private void EnableDuck()
        {
            if (_isDucking)
            {
                return;
            }

            SetDuckState(true);
        }

        private void DisableDuck()
        {
            if (!_isDucking)
            {
                return;
            }

            if (CanStandUp())
            {
                SetDuckState(false);
            }
        }

        private void ToggleDuck()
        {
            if (_isDucking)
            {
                DisableDuck();
                return;
            }

            EnableDuck();
        }

        private bool CanStandUp()
        {
            if (_collisionRectangle == null)
            {
                return true;
            }

            RectangleShape2D standShape = new()
            {
                Size = _standingCollisionSize,
            };

            PhysicsShapeQueryParameters2D query = new()
            {
                Shape = standShape,
                Transform = new Transform2D(0.0f, GlobalPosition + _standingCollisionOffset),
                CollisionMask = CollisionMask,
                CollideWithBodies = true,
                CollideWithAreas = false,
            };
            query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

            return GetWorld2D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
        }

        private void SetDuckState(bool isDucking)
        {
            _isDucking = isDucking;

            if (_collisionRectangle != null)
            {
                _collisionRectangle.Size = isDucking ? _duckCollisionSize : _standingCollisionSize;
            }

            if (_collisionShape != null)
            {
                _collisionShape.Position = isDucking ? _duckCollisionOffset : _standingCollisionOffset;
            }

            if (_sprite != null)
            {
                _sprite.Position = isDucking ? _duckSpritePosition : _standingSpritePosition;
                _sprite.Scale = isDucking ? _duckSpriteScale : _standingSpriteScale;
            }

            if (_animatedSprite != null)
            {
                _animatedSprite.Position = isDucking ? _duckAnimatedSpritePosition : _standingAnimatedSpritePosition;
                _animatedSprite.Scale = isDucking ? _duckAnimatedSpriteScale : _standingAnimatedSpriteScale;
            }

            UpdateInteractionRaycast();
            EmitStatsChanged();
        }

        private void InitializeStats()
        {
            _maxHealth = Mathf.Max(1, _maxHealth);
            _maxAmmo = Mathf.Max(0, _maxAmmo);
            _health = _maxHealth;
            _ammo = Mathf.Clamp(_startingAmmo, 0, _maxAmmo);
            _coins = 0;
            _damageInvulnerabilityRemaining = 0.0f;
            _speedBoostRemaining = 0.0f;
            _speedBoostMultiplier = 1.0f;
            _spawnPosition = GlobalPosition;
            EmitStatsChanged();
        }

        private void UpdateTimers(float delta)
        {
            bool speedBoostExpired = false;

            if (_damageInvulnerabilityRemaining > 0.0f)
            {
                _damageInvulnerabilityRemaining = Mathf.Max(0.0f, _damageInvulnerabilityRemaining - delta);
            }

            if (_speedBoostRemaining > 0.0f)
            {
                _speedBoostRemaining = Mathf.Max(0.0f, _speedBoostRemaining - delta);
                if (_speedBoostRemaining <= 0.0f)
                {
                    _speedBoostMultiplier = 1.0f;
                    speedBoostExpired = true;
                }
            }

            if (speedBoostExpired)
            {
                EmitStatsChanged();
            }
        }

        private void UpdateStatusVisuals()
        {
            if (_sprite == null && _animatedSprite == null)
            {
                return;
            }

            if (_damageInvulnerabilityRemaining > 0.0f)
            {
                Color color = new(1.0f, 0.55f, 0.55f, 1.0f);
                ApplyVisualModulate(_sprite, color);
                ApplyVisualModulate(_animatedSprite, color);
                return;
            }

            if (HasSpeedBoost)
            {
                Color color = new(0.7f, 1.0f, 0.8f, 1.0f);
                ApplyVisualModulate(_sprite, color);
                ApplyVisualModulate(_animatedSprite, color);
                return;
            }

            ApplyVisualModulate(_sprite, Colors.White);
            ApplyVisualModulate(_animatedSprite, Colors.White);
        }

        private float GetBaseMoveSpeed()
        {
            return _speed * (HasSpeedBoost ? _speedBoostMultiplier : 1.0f);
        }

        public bool ApplyDamage(int damage)
        {
            if (damage <= 0 || _health <= 0 || _damageInvulnerabilityRemaining > 0.0f)
            {
                return false;
            }

            _health = GameplayRules.ApplyDamage(_health, damage);
            _damageInvulnerabilityRemaining = _damageInvulnerabilitySeconds;

            if (_health <= 0)
            {
                _animationController?.PlayOneShot(AnimationStates.Die);
                Respawn();
            }
            else
            {
                _animationController?.PlayOneShot(AnimationStates.Hit);
                EmitStatsChanged();
            }

            return true;
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int previousHealth = _health;
            _health = GameplayRules.ClampHealth(_health + amount, _maxHealth);
            if (_health != previousHealth)
            {
                _animationController?.PlayOneShot(AnimationStates.Pickup);
                EmitStatsChanged();
            }
        }

        public bool TryConsumeAmmo(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (_ammo < amount)
            {
                return false;
            }

            _ammo -= amount;
            EmitStatsChanged();
            return true;
        }

        public void AddAmmo(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int previousAmmo = _ammo;
            _ammo = Mathf.Clamp(_ammo + amount, 0, _maxAmmo);
            if (_ammo != previousAmmo)
            {
                _animationController?.PlayOneShot(AnimationStates.Pickup);
                EmitStatsChanged();
            }
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _coins += amount;
            _animationController?.PlayOneShot(AnimationStates.Pickup);
            EmitStatsChanged();
        }

        public void ApplySpeedBoost(float multiplier, float durationSeconds)
        {
            if (durationSeconds <= 0.0f)
            {
                return;
            }

            float requestedMultiplier = Mathf.Max(1.0f, multiplier <= 0.0f ? _defaultSpeedBoostMultiplier : multiplier);
            if (_speedBoostRemaining <= 0.0f || requestedMultiplier >= _speedBoostMultiplier)
            {
                _speedBoostMultiplier = requestedMultiplier;
            }

            _speedBoostRemaining = Mathf.Max(_speedBoostRemaining, durationSeconds);
            _animationController?.PlayOneShot(AnimationStates.Pickup);
            EmitStatsChanged();
        }

        private void Respawn()
        {
            _health = _maxHealth;
            _ammo = Mathf.Clamp(_startingAmmo, 0, _maxAmmo);
            _damageInvulnerabilityRemaining = _damageInvulnerabilitySeconds;
            _speedBoostRemaining = 0.0f;
            _speedBoostMultiplier = 1.0f;
            Velocity = Vector2.Zero;
            GlobalPosition = _spawnPosition;
            SetDuckState(false);
            EmitStatsChanged();
        }

        private void EmitStatsChanged()
        {
            // HUD polls every frame, but a single hook keeps state updates centralized.
        }

        private void SyncAnimationState()
        {
            if (_animationController == null)
            {
                return;
            }

            bool grounded = _wasGrounded;
            bool dashing = grounded && !_isDucking && Input.IsActionPressed("sprint") && Mathf.Abs(Velocity.X) > 5.0f;
            float facing = _direction.X == 0.0f ? FacingDirection : Mathf.Sign(_direction.X);
            _animationController.UpdateFromGameplay(new ActorAnimationInput(
                Velocity,
                grounded,
                _isDucking,
                false,
                false,
                false,
                _justLandedThisFrame,
                dashing,
                facing
            ));
            _justLandedThisFrame = false;
        }

        private static void ApplyVisualModulate(CanvasItem visual, Color color)
        {
            if (visual != null)
            {
                visual.Modulate = color;
            }
        }

        public Godot.Collections.Dictionary<string, Variant> CaptureSaveState()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["global_position"] = GlobalPosition,
                ["velocity"] = Velocity,
                ["facing_direction"] = FacingDirection,
                ["is_ducking"] = _isDucking,
                ["health"] = _health,
                ["ammo"] = _ammo,
                ["coins"] = _coins,
                ["damage_invulnerability"] = _damageInvulnerabilityRemaining,
                ["speed_boost_remaining"] = _speedBoostRemaining,
                ["speed_boost_multiplier"] = _speedBoostMultiplier,
                ["spawn_position"] = _spawnPosition,
            };
        }

        public void RestoreSaveState(Godot.Collections.Dictionary<string, Variant> state)
        {
            if (state.TryGetValue("global_position", out Variant positionValue))
            {
                GlobalPosition = positionValue.AsVector2();
            }

            if (state.TryGetValue("velocity", out Variant velocityValue))
            {
                Velocity = velocityValue.AsVector2();
            }

            if (state.TryGetValue("facing_direction", out Variant facingValue))
            {
                FacingDirection = facingValue.AsSingle();
            }

            if (state.TryGetValue("is_ducking", out Variant duckingValue))
            {
                SetDuckState(duckingValue.AsBool());
            }

            if (state.TryGetValue("health", out Variant healthValue))
            {
                _health = GameplayRules.ClampHealth(healthValue.AsInt32(), _maxHealth);
            }

            if (state.TryGetValue("ammo", out Variant ammoValue))
            {
                _ammo = Mathf.Clamp(ammoValue.AsInt32(), 0, _maxAmmo);
            }

            if (state.TryGetValue("coins", out Variant coinsValue))
            {
                _coins = Mathf.Max(0, coinsValue.AsInt32());
            }

            if (state.TryGetValue("damage_invulnerability", out Variant invulnerabilityValue))
            {
                _damageInvulnerabilityRemaining = Mathf.Max(0.0f, invulnerabilityValue.AsSingle());
            }

            if (state.TryGetValue("speed_boost_remaining", out Variant speedBoostRemainingValue))
            {
                _speedBoostRemaining = Mathf.Max(0.0f, speedBoostRemainingValue.AsSingle());
            }

            if (state.TryGetValue("speed_boost_multiplier", out Variant speedBoostMultiplierValue))
            {
                _speedBoostMultiplier = Mathf.Max(1.0f, speedBoostMultiplierValue.AsSingle());
            }

            if (state.TryGetValue("spawn_position", out Variant spawnPositionValue))
            {
                _spawnPosition = spawnPositionValue.AsVector2();
            }
        }
    }
}
