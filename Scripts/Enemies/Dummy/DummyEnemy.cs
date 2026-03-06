using Godot;
using System;

namespace Insanity.Scripts.Enemies.Dummy
{
    public partial class DummyEnemy : EnemyBody2D
    {
        [Export] private PackedScene _damageDisplay;

        public override void Hurt(int damage)
        {
            base.Hurt(damage);
            if (_damageDisplay != null)
            {
                var displayInstance = _damageDisplay.Instantiate<DamageDisplay>();
                displayInstance.number   = damage      ;
                displayInstance.Position = Vector2.Zero;
                AddChild(displayInstance);
            }
        }
    }
}
