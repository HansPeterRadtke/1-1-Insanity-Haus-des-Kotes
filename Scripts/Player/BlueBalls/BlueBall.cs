using Godot;
using System;
using Insanity.Scripts.Enemies;
using Insanity.Scripts.Game;

public partial class BlueBall : Area2D, ISaveStateNode
{
	[Export] private int _damageMin = 2;
	[Export] private int _damageMax = 7;
	[Export] private float _speed = 5.0f;
	[Export] private float _lifetime = 10.0f;

	private float _age;
	public override void _Ready()
	{
		AddToGroup("save_state");
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
		var body = collider as EnemyBody2D;

		if (body is null)
		{
			return;
		}

		int damage = GD.RandRange(_damageMin, _damageMax);
		
		body.Hurt(damage);
		QueueFree();
	}

	public Godot.Collections.Dictionary<string, Variant> CaptureSaveState()
	{
		return new Godot.Collections.Dictionary<string, Variant>
		{
			["global_position"] = GlobalPosition,
			["rotation"] = Rotation,
			["speed"] = _speed,
			["age"] = _age,
		};
	}

	public void RestoreSaveState(Godot.Collections.Dictionary<string, Variant> state)
	{
		if (state.TryGetValue("global_position", out Variant positionValue))
		{
			GlobalPosition = positionValue.AsVector2();
		}

		if (state.TryGetValue("rotation", out Variant rotationValue))
		{
			Rotation = rotationValue.AsSingle();
		}

		if (state.TryGetValue("speed", out Variant speedValue))
		{
			_speed = speedValue.AsSingle();
		}

		if (state.TryGetValue("age", out Variant ageValue))
		{
			_age = ageValue.AsSingle();
		}
	}
}
