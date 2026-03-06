using Godot;
using Insanity.Scripts.Enemies;

namespace Insanity.Scripts.Player
{
  public partial class Attacks : Marker2D
  {
    [ExportGroup("Kick")]
    
    [Export] public  int         KickDamage =  5;
    [Export] private Texture2D   _texIdle;
    [Export] private Texture2D   _texKick;

    [ExportGroup("Blue Balls")]
    
    [Export] public  float       BlueBallRate         =  0.2f;
    [Export] public  float       BlueBallConeSize_deg = 22.5f;
    [Export] private PackedScene _blueBallPrefab;
    
    private float     _timeSinceBall = 0.0f;
    private Vector2   _relativeMousePos;
    private float     _mouseAngle      ;
    private RayCast2D _kickRaycast     ;
    private Player    _player          ;
    private Sprite2D  _playerSprite    ;
    private Timer     _timer           ;

    public override void _Ready()
    {
      _kickRaycast          = GetNode<RayCast2D>("KickRaycast");
      _player               = GetParent<Player>();
      _playerSprite         = GetParent().GetNode<Sprite2D>("Sprite2D");
      _playerSprite.Texture = _texIdle;
      _timer           = new Timer();
      _timer.OneShot   = true;
      _timer.WaitTime  = 1.0f;
      _timer.Timeout  += OnKickEnd;
      AddChild(_timer);
    }

    public override void _Process(double delta)
    {
      _relativeMousePos = (GetGlobalMousePosition() - GlobalPosition);
      _mouseAngle       = Mathf.Atan2(_relativeMousePos.Y, _relativeMousePos.X);
      Rotation = _mouseAngle;

      if(Input.IsActionJustPressed("attack_kick"))
      {
        Kick();
      }

      if(Input.IsActionPressed("attack_balls") && CanSpawnBall())
      {
        SpawnBall();
      }

      _timeSinceBall += (float)delta;
    }

    private void Kick()
    {
      _playerSprite.Texture = _texKick;
      _playerSprite.FlipH   = (_relativeMousePos.X < 0);
      _playerSprite.Offset  = new Vector2(((_playerSprite.Texture.GetWidth() / 3) * Mathf.Sign(_relativeMousePos.X)), 0);
      _player.IsKicking     = true;
      _timer.Start();
      
      if(!_kickRaycast.IsColliding()) return;
      if(_kickRaycast.GetCollider() is not EnemyBody2D body) return;
      body.Hurt(KickDamage);
    }
    private void OnKickEnd()
    {
      _playerSprite.Texture = _texIdle;
      _playerSprite.Offset  = new Vector2(0, 0);
      _player.IsKicking     = false   ;
    }

    private void SpawnBall()
    {
      _timeSinceBall = 0.0f;
      var instance   = _blueBallPrefab.Instantiate();

      if(instance is BlueBall blueBall)
      {
        blueBall.Rotation       = (Rotation + float.DegreesToRadians((float)GD.RandRange(-BlueBallConeSize_deg, BlueBallConeSize_deg) / 2));
        blueBall.GlobalPosition = GlobalPosition;
        GetTree().Root.AddChild(blueBall);
      }
    }
    
    private bool CanSpawnBall() => (_timeSinceBall > BlueBallRate);
  }
}

