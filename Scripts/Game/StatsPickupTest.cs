using Godot;
using Insanity.Scripts.Pickups;
using PlayerCharacter = Insanity.Scripts.Player.Player;

namespace Insanity.Scripts.Game
{
    public partial class StatsPickupTest : Node2D
    {
        public override async void _Ready()
        {
            PlayerCharacter player = GetNode<PlayerCharacter>("Player");
            Control hud = GetNode<Control>("CanvasLayer/HUD");

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            bool initialStatsOk = player.Health == player.MaxHealth &&
                                  player.Ammo == 12 &&
                                  player.Coins == 0;

            bool damageApplied = player.ApplyDamage(3);
            bool ammoSpent = player.TryConsumeAmmo(2);

            ApplyPickup("res://Scenes/Pickups/HealthPickup.tscn", player);
            ApplyPickup("res://Scenes/Pickups/AmmoPickup.tscn", player);
            ApplyPickup("res://Scenes/Pickups/SpeedPickup.tscn", player);
            ApplyPickup("res://Scenes/Pickups/CoinPickup.tscn", player);

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            Label healthLabel = hud.GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/HealthLabel");
            Label ammoLabel = hud.GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/AmmoLabel");
            Label coinsLabel = hud.GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/CoinsLabel");
            Label boostLabel = hud.GetNode<Label>("MarginContainer/PanelContainer/VBoxContainer/BoostLabel");

            bool finalStatsOk = player.Health == player.MaxHealth &&
                                player.Ammo == 18 &&
                                player.Coins == 1 &&
                                player.HasSpeedBoost;

            bool hudOk = healthLabel.Text.Contains($"{player.MaxHealth}/{player.MaxHealth}") &&
                         ammoLabel.Text.Contains($"{player.Ammo}/{player.MaxAmmo}") &&
                         coinsLabel.Text.Contains("Coins: 1") &&
                         !boostLabel.Text.Contains("none");

            if (!initialStatsOk || !damageApplied || !ammoSpent || !finalStatsOk || !hudOk)
            {
                GD.PrintErr(
                    "StatsPickupTest failed. initial=", initialStatsOk,
                    " damage=", damageApplied,
                    " ammoSpent=", ammoSpent,
                    " final=", finalStatsOk,
                    " hud=", hudOk,
                    " healthLabel=", healthLabel.Text,
                    " ammoLabel=", ammoLabel.Text,
                    " coinsLabel=", coinsLabel.Text,
                    " boostLabel=", boostLabel.Text
                );
                GetTree().Quit(1);
                return;
            }

            GD.Print("StatsPickupTest passed.");
            GetTree().Quit();
        }

        private static void ApplyPickup(string scenePath, PlayerCharacter player)
        {
            PackedScene scene = GD.Load<PackedScene>(scenePath);
            if (scene?.Instantiate() is Pickup pickup)
            {
                pickup.ApplyTo(player);
            }
        }
    }
}
