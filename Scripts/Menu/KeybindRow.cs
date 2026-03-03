using Godot;
using System;

namespace Insanity.Scripts.Menu
{
    public partial class KeybindRow : HBoxContainer
    {
        private Label _actionLabel;
        private Button _bindingButton;
        private string _actionName = string.Empty;

        public event Action<string> RebindRequested;

        public override void _Ready()
        {
            EnsureBuilt();
        }

        public void Configure(string displayName, string actionName, string bindingText)
        {
            EnsureBuilt();
            _actionName = actionName;
            _actionLabel.Text = displayName;
            _bindingButton.Text = bindingText;
        }

        public void SetBindingText(string text)
        {
            EnsureBuilt();
            _bindingButton.Text = text;
        }

        private void EnsureBuilt()
        {
            if (_actionLabel != null && _bindingButton != null)
            {
                return;
            }

            AddThemeConstantOverride("separation", 12);

            _actionLabel = new Label();
            _actionLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddChild(_actionLabel);

            _bindingButton = new Button();
            _bindingButton.CustomMinimumSize = new Vector2(220.0f, 32.0f);
            _bindingButton.Pressed += () => RebindRequested?.Invoke(_actionName);
            AddChild(_bindingButton);
        }
    }
}
