using Godot;
using Insanity.Scripts.Shared;

namespace Insanity.Scripts.Enemies
{
    public partial class ExplosiveEnemy : EnemyBody2D
    {
        [Export] private float _moveSpeed = 140.0f;
        [Export] private float _aggroRange = 400.0f;
        [Export] private float _stopRange = 12.0f;
        [Export] private float _explosionRange = 26.0f;
        [Export] private float _triggerCooldown = 0.1f;

        private float _timeSinceTrigger = 99.0f;

        public override void _PhysicsProcess(double delta)
        {
            _timeSinceTrigger += (float)delta;
            Vector2 velocity = ApplyBasicGravity(delta, Velocity);
            var player = GetPlayer();

            if (player != null)
            {
                EnemyStepResult step = GameplayRules.StepEnemy(
                    new EnemyConfig(EnemyKind.Explosive, MaxHealth, 12, _moveSpeed, _aggroRange, _stopRange, _explosionRange, _triggerCooldown, 0.0f, _explosionRange, 20),
                    GlobalPosition.X,
                    player.GlobalPosition.X,
                    _timeSinceTrigger
                );

                velocity.X = step.VelocityX;
                if (step.ShouldExplode)
                {
                    player.ApplyDamage(20);
                    Die();
                    return;
                }
            }
            else
            {
                velocity.X = 0.0f;
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        public override void Die()
        {
            QueueFree();
        }
    }
}
