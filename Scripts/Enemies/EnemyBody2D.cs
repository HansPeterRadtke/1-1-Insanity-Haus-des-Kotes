using Godot;
using Insanity.Scripts.Animation;
using Insanity.Scripts.Game;
using Insanity.Scripts.Shared;
using System;

namespace Insanity.Scripts.Enemies
{
    [GlobalClass]
    public partial class EnemyBody2D : CharacterBody2D, ISaveStateNode
    {
        [Export] public int Health = 100;
        [Export] public int MaxHealth = 100;
        [Export] public bool IsVulnerable = true;

        private bool _isDying;
        protected AnimationController AnimationController;
        protected Sprite2D FallbackSprite;
        protected AnimatedSprite2D AnimatedSprite;

        public override void _Ready()
        {
            AddToGroup("save_state");
            Health = GameplayRules.ClampHealth(Health, MaxHealth);
            AnimationController = GetNodeOrNull<AnimationController>("AnimationController");
            FallbackSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            AnimatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        }

        public virtual void Hurt(int damage)
        {
            if (!IsVulnerable || _isDying)
            {
                return;
            }

            Health = GameplayRules.ApplyDamage(Health, damage);
            if (Health <= 0)
            {
                Die();
                return;
            }

            AnimationController?.PlayOneShot(AnimationStates.Hit);
        }

        public virtual void Heal(int amount)
        {
            Health = GameplayRules.ClampHealth(Health + Math.Max(0, amount), MaxHealth);
        }

        public virtual void Die()
        {
            if (_isDying)
            {
                return;
            }

            _isDying = true;
            IsVulnerable = false;
            AnimationController?.PlayOneShot(AnimationStates.Die);

            if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") is { } collisionShape)
            {
                collisionShape.Disabled = true;
            }

            Velocity = Vector2.Zero;
            SetPhysicsProcess(false);
            SetProcess(false);

            SceneTreeTimer timer = GetTree()?.CreateTimer((float)Math.Max(0.15, AnimationController?.GetSuggestedDuration(AnimationStates.Die) ?? 0.15));
            if (timer != null)
            {
                timer.Timeout += QueueFree;
            }
            else
            {
                QueueFree();
            }
        }

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

        protected void UpdateVisualModulate(Color color)
        {
            if (FallbackSprite != null)
            {
                FallbackSprite.Modulate = color;
            }

            if (AnimatedSprite != null)
            {
                AnimatedSprite.Modulate = color;
            }
        }

        public virtual Godot.Collections.Dictionary<string, Variant> CaptureSaveState()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["global_position"] = GlobalPosition,
                ["velocity"] = Velocity,
                ["health"] = Health,
                ["max_health"] = MaxHealth,
                ["is_vulnerable"] = IsVulnerable,
            };
        }

        public virtual void RestoreSaveState(Godot.Collections.Dictionary<string, Variant> state)
        {
            if (state.TryGetValue("global_position", out Variant positionValue))
            {
                GlobalPosition = positionValue.AsVector2();
            }

            if (state.TryGetValue("velocity", out Variant velocityValue))
            {
                Velocity = velocityValue.AsVector2();
            }

            if (state.TryGetValue("health", out Variant healthValue))
            {
                Health = healthValue.AsInt32();
            }

            if (state.TryGetValue("max_health", out Variant maxHealthValue))
            {
                MaxHealth = maxHealthValue.AsInt32();
            }

            if (state.TryGetValue("is_vulnerable", out Variant vulnerableValue))
            {
                IsVulnerable = vulnerableValue.AsBool();
            }
        }
    }
}
