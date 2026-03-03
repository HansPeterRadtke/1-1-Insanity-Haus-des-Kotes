using Godot;
using PlayerCharacter = Insanity.Scripts.Player.Player;

namespace Insanity.Scripts.UI
{
    public partial class HudController : Control
    {
        private Label _healthLabel;
        private ProgressBar _healthBar;
        private Label _ammoLabel;
        private ProgressBar _ammoBar;
        private Label _coinsLabel;
        private Label _speedLabel;
        private Label _boostLabel;

        public override void _Ready()
        {
            _healthLabel = GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/HealthLabel");
            _healthBar = GetNode<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/HealthBar");
            _ammoLabel = GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/AmmoLabel");
            _ammoBar = GetNode<ProgressBar>("MarginContainer/PanelContainer/VBoxContainer/AmmoBar");
            _coinsLabel = GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/CoinsLabel");
            _speedLabel = GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/SpeedLabel");
            _boostLabel = GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/BoostLabel");
        }

        public override void _Process(double delta)
        {
            Refresh();
        }

        private void Refresh()
        {
            PlayerCharacter player = GetTree().GetFirstNodeInGroup("player") as PlayerCharacter;
            if (player == null)
            {
                _healthLabel.Text = "HP: --";
                _healthBar.MaxValue = 1.0;
                _healthBar.Value = 0.0;
                _ammoLabel.Text = "Ammo: --";
                _ammoBar.MaxValue = 1.0;
                _ammoBar.Value = 0.0;
                _coinsLabel.Text = "Coins: --";
                _speedLabel.Text = "Move: --";
                _boostLabel.Text = "Boost: --";
                return;
            }

            _healthLabel.Text = $"HP: {player.Health}/{player.MaxHealth}";
            _healthBar.MaxValue = player.MaxHealth;
            _healthBar.Value = player.Health;

            _ammoLabel.Text = $"Ammo: {player.Ammo}/{player.MaxAmmo}";
            _ammoBar.MaxValue = player.MaxAmmo <= 0 ? 1 : player.MaxAmmo;
            _ammoBar.Value = player.Ammo;

            _coinsLabel.Text = $"Coins: {player.Coins}";
            _speedLabel.Text = $"Move: {player.CurrentMoveSpeed:0}";
            _boostLabel.Text = player.HasSpeedBoost
                ? $"Boost: {player.SpeedBoostMultiplier:0.00}x ({player.SpeedBoostRemaining:0.0}s)"
                : "Boost: none";
        }
    }
}
