using Godot;

namespace Insanity.Scripts.Menu
{
    public partial class SettingsInputMapTest : Node
    {
        public override void _Ready()
        {
            SettingsManager settings = GetNode<SettingsManager>("/root/SettingsManager");
            settings.SetStoragePath("user://settings_test.cfg", false);
            settings.ResetToDefaults();
            settings.SaveSettings();

            InputEventKey jumpEvent = new()
            {
                PhysicalKeycode = Key.J,
                Keycode = Key.J,
            };
            InputEventMouseButton shootEvent = new()
            {
                ButtonIndex = MouseButton.Middle,
            };

            settings.SetBinding("jump", jumpEvent);
            settings.SetBinding("shoot", shootEvent);
            settings.LoadAndApply();

            bool jumpBound = InputMap.ActionHasEvent("jump", jumpEvent);
            bool shootBound = InputMap.ActionHasEvent("shoot", shootEvent);

            if (!jumpBound || !shootBound)
            {
                GD.PrintErr("SettingsInputMapTest failed.");
                GetTree().Quit(1);
                return;
            }

            GD.Print("SettingsInputMapTest passed.");
            GetTree().Quit();
        }
    }
}
