using Godot;
using Insanity.Scripts.Interaction;
using Insanity.Scripts.Player;
using System;
using System.Reflection;

namespace Insanity.Scripts.Game
{
    public partial class SaveStateTest : Node
    {
        public override async void _Ready()
        {
            GameManager gameManager = GetNode<GameManager>("/root/GameManager");
            Node sceneRoot = GetNode("MechanicsRoot");
            Node playerNode = GetTree().GetFirstNodeInGroup("player");
            SwitchBody2D switchBody = sceneRoot.GetNode<SwitchBody2D>("SwitchStation");

            if (playerNode is not Node2D player2D)
            {
                GD.PrintErr("SaveStateTest failed: player not found.");
                GetTree().Quit(1);
                return;
            }

            Attacks attacks = player2D.GetNode<Attacks>("Attacks");
            if (!TrySetPrivateField(attacks, "_timeSinceBall", 3.5f))
            {
                GD.PrintErr("SaveStateTest failed: could not set attack state.");
                GetTree().Quit(1);
                return;
            }

            PackedScene switchScene = GD.Load<PackedScene>("res://Scenes/Props/InteractionSwitch.tscn");
            SwitchBody2D dynamicSwitch = switchScene.Instantiate<SwitchBody2D>();
            dynamicSwitch.Name = "DynamicSwitch";
            sceneRoot.AddChild(dynamicSwitch);

            Label runtimeLabel = new()
            {
                Name = "RuntimeLabel",
                Text = "Saved runtime label",
                Visible = true,
            };
            runtimeLabel.Position = new Vector2(210.0f, 18.0f);
            sceneRoot.AddChild(runtimeLabel);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            player2D.GlobalPosition = new Vector2(123.0f, 45.0f);
            switchBody.SetSwitchState(true);
            dynamicSwitch.SetSwitchState(true);
            gameManager.SaveGame(3);

            player2D.GlobalPosition = new Vector2(-999.0f, -999.0f);
            switchBody.SetSwitchState(false);
            TrySetPrivateField(attacks, "_timeSinceBall", 0.0f);
            dynamicSwitch.QueueFree();
            runtimeLabel.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            bool restored = gameManager.RestoreSaveIntoCurrentScene(3);

            bool positionOk = player2D.GlobalPosition.DistanceTo(new Vector2(123.0f, 45.0f)) < 0.1f;
            bool switchOk = switchBody.IsOn;
            SwitchBody2D restoredDynamicSwitch = sceneRoot.GetNodeOrNull<SwitchBody2D>("DynamicSwitch");
            bool dynamicSwitchOk = restoredDynamicSwitch != null && restoredDynamicSwitch.IsOn;
            Label restoredRuntimeLabel = sceneRoot.GetNodeOrNull<Label>("RuntimeLabel");
            bool runtimeLabelOk = restoredRuntimeLabel != null &&
                                  restoredRuntimeLabel.Text == "Saved runtime label" &&
                                  restoredRuntimeLabel.Visible;
            bool attackStateOk = Math.Abs(ReadPrivateFloat(attacks, "_timeSinceBall") - 3.5f) < 0.01f;

            if (!restored || !positionOk || !switchOk || !dynamicSwitchOk || !runtimeLabelOk || !attackStateOk)
            {
                GD.PrintErr(
                    "SaveStateTest failed. restored=", restored,
                    " player=", player2D.GlobalPosition,
                    " switch=", switchBody.IsOn,
                    " dynamicSwitch=", dynamicSwitchOk,
                    " runtimeLabel=", runtimeLabelOk,
                    " attackState=", ReadPrivateFloat(attacks, "_timeSinceBall")
                );
                GetTree().Quit(1);
                return;
            }

            gameManager.DeleteSaveSlot(3);
            if (gameManager.HasSaveSlot(3))
            {
                GD.PrintErr("SaveStateTest failed: slot delete failed.");
                GetTree().Quit(1);
                return;
            }

            GD.Print("SaveStateTest passed.");
            GetTree().Quit();
        }

        private static bool TrySetPrivateField(object target, string fieldName, float value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return false;
            }

            field.SetValue(target, value);
            return true;
        }

        private static float ReadPrivateFloat(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return float.NaN;
            }

            return field.GetValue(target) is float value ? value : float.NaN;
        }
    }
}
