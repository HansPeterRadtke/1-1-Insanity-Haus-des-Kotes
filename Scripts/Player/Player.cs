using Godot;
using Insanity.Scripts.Interaction;
using Insanity.Scripts.Shared;

namespace Insanity.Scripts.Player
{
    public partial class Player : CharacterBody2D
    {
        [Export] private float _speed = 300.0f;
        [Export] private float _sprintMultiplier = 1.5f;
        [Export] private float _jumpVelocity = -400.0f;
        [Export] private float _coyoteTime = 0.2f;

        private Vector2 _direction;
        private bool _flipped;
        private float _timeSinceGrounded;
        private RayCast2D _interactionRaycast;
        
        public float FacingDirection { get; private set; } = 1.0f;

        public override void _Ready()
        {
            AddToGroup("player");
            _interactionRaycast = GetNode<RayCast2D>("InteractionRaycast");
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
    
            // Handle Jump.
            if (Input.IsActionJustPressed("jump") && CanJump())
            {
                velocity.Y = _jumpVelocity;
            }
    
            float inputX = Input.GetAxis("move_left", "move_right");
            _direction = new Vector2(inputX, 0.0f);
            velocity.X = GameplayRules.ResolveHorizontalVelocity(
                Velocity.X,
                inputX,
                _speed,
                _sprintMultiplier,
                Input.IsActionPressed("sprint")
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

            _interactionRaycast.TargetPosition = new Vector2(48.0f * FacingDirection, 0.0f);
        }
    }
}
