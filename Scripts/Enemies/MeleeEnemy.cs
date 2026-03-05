using Godot;
using Insanity.Scripts.Animation;
using Insanity.Scripts.Shared;

namespace Insanity.Scripts.Enemies
{
    public partial class MeleeEnemy : EnemyBody2D
    {
        [Export] private float _moveSpeed = 110.0f;
        [Export] private float _aggroRange = 360.0f;
        [Export] private float _stopRange = 28.0f;
        [Export] private float _attackRange = 42.0f;
        [Export] private float _attackCooldown = 0.9f;

        private float _timeSinceAttack = 99.0f;
        private bool _wasGrounded;
        private bool _justLanded;

        public override void _PhysicsProcess(double delta)
        {
            _timeSinceAttack += (float)delta;
            Vector2 velocity = ApplyBasicGravity(delta, Velocity);
            var player = GetPlayer();

            if (player != null)
            {
                EnemyStepResult step = GameplayRules.StepEnemy(
                    new EnemyConfig(EnemyKind.Melee, MaxHealth, 8, _moveSpeed, _aggroRange, _stopRange, _attackRange, _attackCooldown, 0.0f, 0.0f, 0),
                    GlobalPosition.X,
                    player.GlobalPosition.X,
                    _timeSinceAttack
                );

                velocity.X = step.VelocityX;
                if (step.ShouldAttack)
                {
                    _timeSinceAttack = 0.0f;
                    AnimationController?.PlayOneShot(AnimationStates.AttackMelee);
                    player.ApplyDamage(8);
                    UpdateVisualModulate(new Color(1.0f, 0.7f, 0.7f));
                }
                else
                {
                    UpdateVisualModulate(Colors.White);
                }
            }
            else
            {
                velocity.X = 0.0f;
            }

            Velocity = velocity;
            MoveAndSlide();

            bool groundedNow = IsOnFloor();
            _justLanded = !_wasGrounded && groundedNow;
            _wasGrounded = groundedNow;

            float facing = player != null && player.GlobalPosition.X < GlobalPosition.X ? -1.0f : 1.0f;
            AnimationController?.UpdateFromGameplay(new ActorAnimationInput(
                Velocity,
                groundedNow,
                false,
                false,
                false,
                false,
                _justLanded,
                false,
                facing
            ));
            _justLanded = false;
        }
    }
}
