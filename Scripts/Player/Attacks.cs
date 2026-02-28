using Godot;
using Insanity.Scripts.Enemies;

namespace Insanity.Scripts.Player
{
	public partial class Attacks : Marker2D
	{
		[Export] public int KickDamage = 5;
		
		private RayCast2D _kickRaycast;

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
	    }

		private void _Kick()
		{
			var body = _kickRaycast.GetCollider() as EnemyBody2D;

			if (body is null)
			{
				return;
			}
			
			body.Hurt(KickDamage);
		}
    }
}

