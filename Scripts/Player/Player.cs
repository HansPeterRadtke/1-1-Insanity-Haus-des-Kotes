using Godot;

namespace Insanity.Scripts.Player
{
    public partial class Player : CharacterBody2D
    {
        [Export] private float _speed = 300.0f;
        [Export] private float _jumpVelocity = -400.0f;
        [Export] private float _coyoteTime = 0.2f;

        private Vector2 _direction;
        private bool _flipped;
        private float _timeSinceGrounded;
    
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
    
            // Get the input direction and handle the movement/deceleration.
            // As good practice, you should replace UI actions with custom gameplay actions.
            _direction = Input.GetVector("left", "right", "up", "down");
            if (_direction != Vector2.Zero)
            {
                velocity.X = _direction.X * _speed;
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, _speed);
            }
    
            Velocity = velocity;
            MoveAndSlide();
        }
        public override void _Process(double delta)
        {
            if (_direction.X != 0)
            {
                _flipped = _direction.X < 0;
                GetNode<Sprite2D>("Sprite2D").FlipH = _flipped;
            }
        }

        private bool CanJump() => IsOnFloor() || _timeSinceGrounded < _coyoteTime;
    }
}

