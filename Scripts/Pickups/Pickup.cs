using Godot;
using PlayerCharacter = Insanity.Scripts.Player.Player;

namespace Insanity.Scripts.Pickups
{
    public enum PickupKind
    {
        Health,
        Ammo,
        Speed,
        Coin,
    }

    public partial class Pickup : Area2D
    {
        [Export] private PickupKind _kind = PickupKind.Health;
        [Export] private int _amount = 1;
        [Export] private float _multiplier = 1.5f;
        [Export] private float _durationSeconds = 6.0f;
        [Export] private Color _accent = Colors.White;

        private Polygon2D _polygon;
        private Label _label;

        public override void _Ready()
        {
            _polygon = GetNodeOrNull<Polygon2D>("Polygon2D");
            _label = GetNodeOrNull<Label>("Label");
            BodyEntered += OnBodyEntered;
            UpdateVisuals();
        }

        public void ApplyTo(PlayerCharacter player)
        {
            if (player == null)
            {
                return;
            }

            switch (_kind)
            {
                case PickupKind.Health:
                    player.Heal(_amount);
                    break;
                case PickupKind.Ammo:
                    player.AddAmmo(_amount);
                    break;
                case PickupKind.Speed:
                    player.ApplySpeedBoost(_multiplier, _durationSeconds);
                    break;
                case PickupKind.Coin:
                    player.AddCoins(_amount);
                    break;
            }

            QueueFree();
        }

        private void OnBodyEntered(Node2D body)
        {
            if (body is PlayerCharacter player)
            {
                ApplyTo(player);
            }
        }

        private void UpdateVisuals()
        {
            if (_polygon != null)
            {
                _polygon.Color = _accent;
            }

            if (_label != null)
            {
                _label.Text = _kind switch
                {
                    PickupKind.Health => $"+HP {_amount}",
                    PickupKind.Ammo => $"+AM {_amount}",
                    PickupKind.Speed => $"SPD x{_multiplier:0.0}",
                    PickupKind.Coin => $"+${_amount}",
                    _ => "PICKUP",
                };
            }
        }
    }
}
