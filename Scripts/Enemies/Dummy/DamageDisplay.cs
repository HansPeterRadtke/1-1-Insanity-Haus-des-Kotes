using Godot;
using System;

public partial class DamageDisplay : Node2D
{
	[Export] public int number;

	private AnimationPlayer _animationPlayer;

	private Label _label;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_label = GetNode<Label>("Label");

		_label.Text = number + "";
		
		_animationPlayer.Play("display");
	}


	public void Delete()
	{
		QueueFree();
	}

	
}
