using Godot;
using System;
using Insanity.Scripts.Enemies;

public partial class BlueBall : Area2D
{
  [Export] private int   _damageMin  =   2   ;
  [Export] private int   _damageMax  =   7   ;
  [Export] private float _speed_pps  = 400.0f; // in (P)ixels(P)er(S)econd
  [Export] private float _lifetime_s =   1.0f; // in (S)econds

           private float _age_s      =   0.0f; // in (S)econds
  
  public override void _Ready()
  {
    BodyEntered += _OnHitEnemy;
  }

  public override void _PhysicsProcess(double delta_s)
  {
    _age_s += (float)delta_s;
    
    Godot.Vector2 trans = (Vector2.FromAngle(Rotation) * _speed_pps * (float)delta_s);
//    GD.Print($"(BlueBall) _age_s = {_age_s, 4: 0.0}; Rotation = {Rotation, 6:  0.00;- 0.00}; speed_pps = {_speed_pps, 6:  0.00} pps; delta_s = {delta_s, 6:0.0000}; trans = ({trans.X, 6:  0.00;- 0.00}, {trans.Y, 6:  0.00;- 0.00}); ");
    Translate(trans);

    if(_age_s >= _lifetime_s)
    {
      QueueFree();
    }
  }

	private void _OnHitEnemy(Node2D collider)
	{
		if (collider is not EnemyBody2D body) return;
		
		int damage = GD.RandRange(_damageMin, _damageMax);
		body.Hurt(damage);
		
		QueueFree();
	}
}
