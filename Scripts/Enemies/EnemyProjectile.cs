using Godot;
using Insanity.Scripts.Game;
using PlayerCharacter = Insanity.Scripts.Player.Player;

namespace Insanity.Scripts.Enemies
{
    public partial class EnemyProjectile : Area2D, ISaveStateNode
    {
        [Export] private float _speed = 260.0f;
        [Export] private float _lifetime = 4.0f;

        private Vector2 _direction = Vector2.Right;
        private float _age;

        public override void _Ready()
        {
            AddToGroup("save_state");
            BodyEntered += OnBodyEntered;
        }

        public override void _PhysicsProcess(double delta)
        {
            _age += (float)delta;
            GlobalPosition += _direction * _speed * (float)delta;

            if (_age >= _lifetime)
            {
                QueueFree();
            }
        }

        public void Initialize(Vector2 direction, float speed)
        {
            if (direction.LengthSquared() > 0.0f)
            {
                _direction = direction.Normalized();
            }

            if (speed > 0.0f)
            {
                _speed = speed;
            }
        }

        private void OnBodyEntered(Node2D body)
        {
            if (body is PlayerCharacter player)
            {
                player.ApplyDamage(4);
            }

            QueueFree();
        }

        public Godot.Collections.Dictionary<string, Variant> CaptureSaveState()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["global_position"] = GlobalPosition,
                ["direction"] = _direction,
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

            if (state.TryGetValue("direction", out Variant directionValue))
            {
                _direction = directionValue.AsVector2();
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
}
