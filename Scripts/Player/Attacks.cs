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
		[Export] public float MeleeAreaOffset = 28.0f;

		
		
		[ExportGroup("Blue Balls")]
		
		[Export] public float BlueBallRate = 0.2f;
		[Export] public float BlueBallAngle = 22.5f;
		[Export] private PackedScene _blueBallPrefab;
		
			
			
			private RayCast2D _kickRaycast;
			private Area2D _meleeArea;
			private Player _player;
			private float _timeSinceBall = 0.0f;

			public override void _Ready()
			{
				_player = GetParent<Player>();
				_kickRaycast = GetNode<RayCast2D>("KickRaycast");
				_meleeArea = GetNode<Area2D>("MeleeArea");
			}

		public override void _Process(double delta)
	    {
		    Vector2 relativeMousePos = GetGlobalMousePosition() - GlobalPosition;

		    float mouseAngle = Mathf.Atan2(relativeMousePos.Y, relativeMousePos.X);

		    Rotation = mouseAngle;
		    UpdateMeleeArea();

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

			private void UpdateMeleeArea()
			{
				if (_meleeArea == null)
				{
					return;
				}

				float facingDirection = _player == null ? 1.0f : _player.FacingDirection;
				_meleeArea.Position = new Vector2(
					MeleeAreaOffset * facingDirection,
					_player != null && _player.IsDucking ? 10.0f : 0.0f
				);
			}

			private void _Kick()
			{
				if (_meleeArea != null)
				{
					foreach (Node2D bodyNode in _meleeArea.GetOverlappingBodies())
					{
						if (bodyNode is EnemyBody2D closeEnemy)
						{
							closeEnemy.Hurt(KickDamage);
							return;
						}
					}
				}

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
					GetTree().CurrentScene?.AddChild(blueBall);
				}

			
			}
			
			private bool _canSpawnBall() => GameplayRules.CanUseCooldown(_timeSinceBall, BlueBallRate);
	    }
	}
