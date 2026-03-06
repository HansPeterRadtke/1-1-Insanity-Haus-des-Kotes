using Godot;
using System;
using Insanity.Scripts.Enemies;

public partial class BlueBall : Area2D
{
	[Export] private int _damageMin = 2;
	[Export] private int _damageMax = 7;
	[Export] private float _speed = 5.0f;
	[Export] private float _lifetime = 10.0f;

	private float _age;
	public override void _Ready()
	{
		BodyEntered += _OnHitEnemy;
	}

	public override void _PhysicsProcess(double delta)
	{
		_age += (float)delta;
		
		Translate(Vector2.FromAngle(Rotation) * _speed);

		if (_age >= _lifetime)
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
