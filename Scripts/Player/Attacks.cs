using Godot;
using Insanity.Scripts.Enemies;

namespace Insanity.Scripts.Player
{
	public partial class Attacks : Marker2D
	{
		[ExportGroup("Kick")]
		
		[Export] public int KickDamage = 5;

		
		
		[ExportGroup("Blue Balls")]
		
		[Export] public float BlueBallRate = 0.2f;
		[Export] public float BlueBallAngle = 22.5f;
		[Export] private PackedScene _blueBallPrefab;
		
		
		
		private RayCast2D _kickRaycast;
		private float _timeSinceBall = 0.0f;

		public override void _Ready()
		{
			_kickRaycast = GetNode<RayCast2D>("KickRaycast");
		}

		public override void _Process(double delta)
	    {
		    Vector2 relativeMousePos = GetGlobalMousePosition() - GlobalPosition;

		    float mouseAngle = Mathf.Atan2(relativeMousePos.Y, relativeMousePos.X);

		    Rotation = mouseAngle;

		    if (Input.IsActionJustPressed("attack_kick"))
		    {
			    _Kick();
		    }

		    if (Input.IsActionPressed("attack_balls") && _canSpawnBall())
		    {
			    _spawnBall();
		    }

		    _timeSinceBall += (float)delta;
	    }

		private void _Kick()
		{
			if (_kickRaycast.GetCollider() is not EnemyBody2D body) return;
			
			body.Hurt(KickDamage);
		}

		private void _spawnBall()
		{
			_timeSinceBall = 0.0f;
			
			var instance = _blueBallPrefab.Instantiate();
			if (instance is not BlueBall blueBall) return;
			
			float randomAngle = (float) GD.RandRange(-BlueBallAngle, BlueBallAngle);
			blueBall.Rotation = Rotation + float.DegreesToRadians(randomAngle);
				
			blueBall.GlobalPosition = GlobalPosition;
				
			GetTree().Root.AddChild(blueBall);
		}
		
		private bool _canSpawnBall() => _timeSinceBall > BlueBallRate;
    }
}

