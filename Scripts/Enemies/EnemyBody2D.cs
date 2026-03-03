using Godot;
using System;
using Insanity.Scripts.Shared;

namespace Insanity.Scripts.Enemies
{
	[GlobalClass]
	public partial class EnemyBody2D : CharacterBody2D
    {
    	[Export] public int Health = 100;
	    [Export] public int MaxHealth = 100;
    	[Export] public bool IsVulnerable = true;

	    public override void _Ready()
	    {
		    Health = GameplayRules.ClampHealth(Health, MaxHealth);
	    }
    
    	public virtual void Hurt(int damage)
    	{
    		if (!IsVulnerable) { return; }
    		Health = GameplayRules.ApplyDamage(Health, damage);
    		if (Health <= 0) { Die(); }
    	}

	    public virtual void Heal(int amount)
	    {
		    Health = GameplayRules.ClampHealth(Health + Math.Max(0, amount), MaxHealth);
	    }
	    
	    
    
    	public virtual void Die() => QueueFree();

	    protected Insanity.Scripts.Player.Player GetPlayer()
	    {
		    return GetTree().GetFirstNodeInGroup("player") as Insanity.Scripts.Player.Player;
	    }

	    protected Vector2 ApplyBasicGravity(double delta, Vector2 velocity)
	    {
		    if (!IsOnFloor())
		    {
			    velocity += GetGravity() * (float)delta;
		    }
		    else if (velocity.Y > 0.0f)
		    {
			    velocity.Y = 0.0f;
		    }

		    return velocity;
	    }
    }

}
