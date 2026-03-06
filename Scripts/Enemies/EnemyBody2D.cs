using Godot;
using System;

namespace Insanity.Scripts.Enemies
{
	[GlobalClass]
	public partial class EnemyBody2D : CharacterBody2D
    {
    	[Export] public int  Health       = 100 ;
	    [Export] public int  MaxHealth    = 100 ;
    	[Export] public bool IsVulnerable = true;
    
    	public virtual void Hurt(int damage)
    	{
    		if (!IsVulnerable) { return; }
    		Health -= damage;
    		if (Health <= 0) { Die(); }
    	}

	    public virtual void Heal(int amount)
	    {
		    Health += amount;
		    Health  = Math.Clamp(Health, 0, MaxHealth);
	    }
	    
	    
    
    	public virtual void Die()
    	{
    		// Child Class Deal With It
    	}

	    public override void _Process(double delta)
	    {
		    base._Process(delta);
	    }
    }

}
