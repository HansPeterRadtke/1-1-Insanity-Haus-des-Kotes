	using Godot;
	using Insanity.Scripts.Enemies;
	using System;
	using Insanity.Scripts.Shared;

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
			private Player _player;
			private float _timeSinceBall = 0.0f;

			public override void _Ready()
			{
				_player = GetParent<Player>();
				_kickRaycast = GetNode<RayCast2D>("KickRaycast");
			}

		public override void _Process(double delta)
	    {
		    Vector2 relativeMousePos = GetGlobalMousePosition() - GlobalPosition;

		    float mouseAngle = Mathf.Atan2(relativeMousePos.Y, relativeMousePos.X);

		    Rotation = mouseAngle;

			    if (Input.IsActionJustPressed("melee"))
			    {
				    _Kick();
			    }

			    if (Input.IsActionPressed("shoot") && _canSpawnBall())
			    {
				    _spawnBall(Rotation);
			    }

			    if (Input.IsActionJustPressed("shoot_forward") && _canSpawnBall())
			    {
				    float facingRotation = _player is null || _player.FacingDirection >= 0.0f ? 0.0f : MathF.PI;
				    _spawnBall(facingRotation);
			    }

			    _timeSinceBall += (float)delta;
	    }

			private void _Kick()
			{
				_kickRaycast.ForceRaycastUpdate();
				var body = _kickRaycast.GetCollider() as EnemyBody2D;

				if (body is null)
			{
				return;
			}
			
			body.Hurt(KickDamage);
		}

			private void _spawnBall(float rotation)
			{
				_timeSinceBall = 0.0f;
				var instance = _blueBallPrefab.Instantiate();

				if (instance is BlueBall blueBall)
				{
					blueBall.Rotation = rotation + float.DegreesToRadians((float)GD.RandRange(-BlueBallAngle, BlueBallAngle));
					blueBall.GlobalPosition = GlobalPosition;
					GetTree().Root.AddChild(blueBall);
				}

			
			}
			
			private bool _canSpawnBall() => GameplayRules.CanUseCooldown(_timeSinceBall, BlueBallRate);
	    }
	}
