using Godot;

namespace Insanity.Scripts.Player
{
  public partial class Player : CharacterBody2D
  {
    [Export] private float   _speed_pps        =  300.0f; // in (P)ixels(P)er(S)econd
    [Export] private float   _jumpVelocity_pps =  400.0f; // in (P)ixels(P)er(S)econd
    [Export] private float   _coyoteTime_s     =    0.2f; // in (S)econds

             private Vector2 _direction                  ;
             private bool    _flipped                    ;
             private float   _timeSinceGrounded_s = 0.0f ; // in (S)econds

    public  bool  IsKicking {get; set; }

    public override void _Ready()
    {
      IsKicking = false;
    }
    public override void _PhysicsProcess(double delta_s)
    {
      Vector2 velocity = Velocity;
  
      // Add the gravity.
      if(!IsOnFloor())
      {
        velocity             += (GetGravity() * (float)delta_s);
        _timeSinceGrounded_s += (float)delta_s;
      }
      else
      {
        _timeSinceGrounded_s = 0.0f;
      }
  
      // Handle Jump.
      if(Input.IsActionJustPressed("jump") && CanJump())
      {
        velocity.Y = -_jumpVelocity_pps;
      }
  
      // Get the input direction and handle the movement/deceleration.
      // As good practice, you should replace UI actions with custom gameplay actions.
      _direction = Input.GetVector("left", "right", "up", "down");
      if(_direction != Vector2.Zero)
      {
        velocity.X = (_direction.X * _speed_pps);
      }
      else
      {
        velocity.X = Mathf.MoveToward(Velocity.X, 0, _speed_pps);
      }

//      GD.Print($"(Player) delta_s = {delta_s, 6:0.0000}; velocity = ({velocity.X, 5:0}, {velocity.Y, 5:0}); ");

      Velocity = velocity;
      MoveAndSlide();
    }
    public override void _Process(double delta)
    {
      if(_direction.X != 0)
      {
        if(!IsKicking)
        {
          _flipped = (_direction.X < 0);
          GetNode<Sprite2D>("Sprite2D").FlipH = _flipped;
        }
      }
    }

    private bool CanJump() => (IsOnFloor() || (_timeSinceGrounded_s < _coyoteTime_s));
  }
}
