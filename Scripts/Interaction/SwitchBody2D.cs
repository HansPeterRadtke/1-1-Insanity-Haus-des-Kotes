using Godot;
using Insanity.Scripts.Game;

namespace Insanity.Scripts.Interaction
{
    public partial class SwitchBody2D : StaticBody2D, IInteractable, ISaveStateNode
    {
        [Export] private Color _offColor = new(0.45f, 0.1f, 0.1f);
        [Export] private Color _onColor = new(0.1f, 0.65f, 0.2f);

        private bool _isOn;
        private Polygon2D _indicator;
        private Label _stateLabel;

        public bool IsOn => _isOn;

        public override void _Ready()
        {
            AddToGroup("save_state");
            _indicator = GetNode<Polygon2D>("Indicator");
            _stateLabel = GetNode<Label>("StateLabel");
            UpdateVisuals();
        }

        public void Interact(Node interactor)
        {
            _isOn = !_isOn;
            UpdateVisuals();
        }

        public void SetSwitchState(bool isOn)
        {
            _isOn = isOn;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_indicator != null)
            {
                _indicator.Color = _isOn ? _onColor : _offColor;
            }

            if (_stateLabel != null)
            {
                _stateLabel.Text = _isOn ? "Switch: ON" : "Switch: OFF";
            }
        }

        public Godot.Collections.Dictionary<string, Variant> CaptureSaveState()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["is_on"] = _isOn,
            };
        }

        public void RestoreSaveState(Godot.Collections.Dictionary<string, Variant> state)
        {
            if (state.TryGetValue("is_on", out Variant isOnValue))
            {
                SetSwitchState(isOnValue.AsBool());
            }
        }
    }
}
