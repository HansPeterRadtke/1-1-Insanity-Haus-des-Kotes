using Godot;

namespace Insanity.Scripts.Menu
{
    public partial class MainMenu : Control
    {
        private const string GameScenePath = "res://Scenes/Tests/MechanicsTest.tscn";

        private Button _startButton;
        private Button _settingsButton;
        private Button _quitButton;
        private Label _statusLabel;
        private int _settingsTipIndex;

        private readonly string[] _settingsTips =
        {
            "Settings are not ready yet, keep exploring!",
            "Gravity feels smooth? Toggle the slider in your head.",
            "You can pretend the HUD toggles are real.",
        };

        public override void _Ready()
        {
            _startButton = GetNode<Button>("CenterContainer/MenuPanel/VBoxContainer/StartButton");
            _settingsButton = GetNode<Button>("CenterContainer/MenuPanel/VBoxContainer/SettingsButton");
            _quitButton = GetNode<Button>("CenterContainer/MenuPanel/VBoxContainer/QuitButton");
            _statusLabel = GetNode<Label>("CenterContainer/MenuPanel/VBoxContainer/StatusLabel");

            _startButton.Pressed += OnStartButtonPressed;
            _settingsButton.Pressed += OnSettingsButtonPressed;
            _quitButton.Pressed += OnQuitButtonPressed;

            _statusLabel.Text = "Ready to dive into YTK-Land.";
        }

        private void OnStartButtonPressed()
        {
            GetTree().ChangeSceneToFile(GameScenePath);
        }

        private void OnSettingsButtonPressed()
        {
            _settingsTipIndex = (_settingsTipIndex + 1) % _settingsTips.Length;
            _statusLabel.Text = _settingsTips[_settingsTipIndex];
        }

        private void OnQuitButtonPressed()
        {
            GetTree().Quit();
        }
    }
}
