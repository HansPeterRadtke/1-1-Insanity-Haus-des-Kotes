using Godot;
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
        [Export] private float _jumpVelocity = -400.0f;
        [Export] private float _coyoteTime = 0.2f;

        private Vector2 _direction;
        private bool _flipped;
        private bool _isDucking;
        private float _timeSinceGrounded;
        private RayCast2D _interactionRaycast;
        private GameManager _gameManager;
        private CollisionShape2D _collisionShape;
        private RectangleShape2D _collisionRectangle;
        private Sprite2D _sprite;
        private Vector2 _standingCollisionSize;
        private Vector2 _standingCollisionOffset;
        private Vector2 _duckCollisionSize;
        private Vector2 _duckCollisionOffset;
        private Vector2 _standingSpritePosition;
        private Vector2 _standingSpriteScale;
        private Vector2 _duckSpritePosition;
        private Vector2 _duckSpriteScale;
        
        public float FacingDirection { get; private set; } = 1.0f;
        public bool IsDucking => _isDucking;

        public override void _Ready()
        {
            AddToGroup("player");
            AddToGroup("save_state");
            _gameManager = GetNode<GameManager>("/root/GameManager");
            _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
            _collisionRectangle = _collisionShape?.Shape as RectangleShape2D;
            _sprite = GetNode<Sprite2D>("Sprite2D");
            _interactionRaycast = GetNode<RayCast2D>("InteractionRaycast");
            InitializeDuckState();
            UpdateInteractionRaycast();
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector2 velocity = Velocity;
    
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
    
            if (_isDucking && Input.IsActionJustPressed("jump"))
            {
                DisableDuck();
            }
            // Handle Jump.
            else if (!_isDucking && Input.IsActionJustPressed("jump") && CanJump())
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
            float moveSpeed = _isDucking ? _speed * _duckSpeedMultiplier : _speed;
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
        }

        public override void _Process(double delta)
        {
            FacingDirection = GameplayRules.ResolveFacingDirection(FacingDirection, _direction.X);
            UpdateInteractionRaycast();

            if (_direction.X != 0.0f)
            {
                _flipped = _direction.X < 0;
                GetNode<Sprite2D>("Sprite2D").FlipH = _flipped;
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
            if (_interactionRaycast == null)
            {
                return;
            }

            var collider = _interactionRaycast.GetCollider();
            if (collider is Node node && node is IInteractable interactable)
            {
                interactable.Interact(this);
            }
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

            UpdateInteractionRaycast();
        }

        public Godot.Collections.Dictionary<string, Variant> CaptureSaveState()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["global_position"] = GlobalPosition,
                ["velocity"] = Velocity,
                ["facing_direction"] = FacingDirection,
                ["is_ducking"] = _isDucking,
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
        }
    }
}
