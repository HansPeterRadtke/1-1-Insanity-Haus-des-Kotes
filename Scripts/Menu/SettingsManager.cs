using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Insanity.Scripts.Menu
{
    public partial class SettingsManager : Node
    {
        private const string DefaultSettingsPath = "user://settings.cfg";

        private readonly string[] _managedActions =
        {
            "move_left",
            "move_right",
            "jump",
            "sprint",
            "duck",
            "shoot",
            "shoot_forward",
            "melee",
            "interact",
            "up",
            "down",
            "ability0",
            "ability1",
            "ability2",
            "ability3",
            "attack_kick",
            "attack_balls",
            "heal",
        };

        private readonly Dictionary<string, string> _displayNames = new()
        {
            ["move_left"] = "Move Left",
            ["move_right"] = "Move Right",
            ["jump"] = "Jump",
            ["sprint"] = "Sprint",
            ["duck"] = "Duck Toggle",
            ["shoot"] = "Shoot",
            ["shoot_forward"] = "Shoot Forward",
            ["melee"] = "Melee",
            ["interact"] = "Interact",
            ["up"] = "Move Up",
            ["down"] = "Move Down",
            ["ability0"] = "Ability 0",
            ["ability1"] = "Ability 1",
            ["ability2"] = "Ability 2",
            ["ability3"] = "Ability 3",
            ["attack_kick"] = "Legacy Kick",
            ["attack_balls"] = "Legacy Shoot",
            ["heal"] = "Heal",
        };

        private readonly Dictionary<string, InputEvent> _bindings = new();
        private readonly Dictionary<string, InputEvent> _defaults = new();
        private string _settingsPath = DefaultSettingsPath;

        public IReadOnlyList<string> ManagedActionNames => _managedActions;
        public float MasterVolumeDb { get; private set; } = 0.0f;
        public bool Fullscreen { get; private set; }
        public float WindowScale { get; private set; } = 1.0f;

        public override void _Ready()
        {
            InitializeDefaults();
            LoadAndApply();
        }

        public string GetActionDisplayName(string actionName)
        {
            return _displayNames.TryGetValue(actionName, out string displayName) ? displayName : actionName;
        }

        public string GetBindingLabel(string actionName)
        {
            if (!_bindings.TryGetValue(actionName, out InputEvent inputEvent))
            {
                return "Unbound";
            }

            return DescribeInputEvent(inputEvent);
        }

        public void SetBinding(string actionName, InputEvent inputEvent)
        {
            InputEvent normalized = NormalizeInputEvent(inputEvent);
            _bindings[actionName] = normalized;
            ApplyBinding(actionName, normalized);
            SaveSettings();
        }

        public void SetMasterVolumeDb(float volumeDb)
        {
            MasterVolumeDb = Mathf.Clamp(volumeDb, -40.0f, 6.0f);
        }

        public void SetVideoSettings(bool fullscreen, float windowScale)
        {
            Fullscreen = fullscreen;
            WindowScale = Mathf.Clamp(windowScale, 0.5f, 3.0f);
        }

        public void ApplyAudioSettings()
        {
            AudioServer.SetBusVolumeDb(0, MasterVolumeDb);
        }

        public void ApplyVideoSettings()
        {
            DisplayServer.WindowSetMode(
                Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
            );

            Window window = GetWindow();
            if (window != null)
            {
                window.ContentScaleFactor = WindowScale;
            }
        }

        public void SaveSettings()
        {
            ConfigFile config = new();
            config.SetValue("audio", "master_volume_db", MasterVolumeDb);
            config.SetValue("video", "fullscreen", Fullscreen);
            config.SetValue("video", "window_scale", WindowScale);

            foreach ((string action, InputEvent inputEvent) in _bindings)
            {
                config.SetValue("bindings", action, SerializeInputEvent(inputEvent));
            }

            config.Save(_settingsPath);
        }

        public void LoadAndApply()
        {
            _bindings.Clear();
            foreach ((string action, InputEvent inputEvent) in _defaults)
            {
                _bindings[action] = NormalizeInputEvent(inputEvent);
            }

            MasterVolumeDb = 0.0f;
            Fullscreen = false;
            WindowScale = 1.0f;

            ConfigFile config = new();
            if (config.Load(_settingsPath) == Error.Ok)
            {
                Variant audioValue = config.GetValue("audio", "master_volume_db", MasterVolumeDb);
                Variant fullscreenValue = config.GetValue("video", "fullscreen", Fullscreen);
                Variant windowScaleValue = config.GetValue("video", "window_scale", WindowScale);

                MasterVolumeDb = Mathf.Clamp((float)audioValue.AsDouble(), -40.0f, 6.0f);
                Fullscreen = fullscreenValue.AsBool();
                WindowScale = Mathf.Clamp((float)windowScaleValue.AsDouble(), 0.5f, 3.0f);

                foreach (string action in _managedActions)
                {
                    Variant stored = config.GetValue("bindings", action, Variant.CreateFrom((string)null));
                    if (stored.VariantType == Variant.Type.Nil)
                    {
                        continue;
                    }

                    InputEvent parsed = DeserializeInputEvent(stored.AsString());
                    if (parsed != null)
                    {
                        _bindings[action] = parsed;
                    }
                }
            }

            MigrateLegacyBindings();

            ApplyAllSettings();
        }

        public void SetStoragePath(string settingsPath, bool reloadFromDisk)
        {
            _settingsPath = settingsPath;
            if (reloadFromDisk)
            {
                LoadAndApply();
            }
        }

        public void ResetToDefaults()
        {
            _bindings.Clear();
            foreach ((string action, InputEvent inputEvent) in _defaults)
            {
                _bindings[action] = NormalizeInputEvent(inputEvent);
            }

            MasterVolumeDb = 0.0f;
            Fullscreen = false;
            WindowScale = 1.0f;
            ApplyAllSettings();
        }

        private void ApplyAllSettings()
        {
            foreach (string action in _managedActions)
            {
                EnsureActionExists(action);
                InputMap.ActionEraseEvents(action);

                if (_bindings.TryGetValue(action, out InputEvent inputEvent))
                {
                    InputMap.ActionAddEvent(action, inputEvent);
                }
            }

            ApplyAudioSettings();
            ApplyVideoSettings();
        }

        private void ApplyBinding(string actionName, InputEvent inputEvent)
        {
            EnsureActionExists(actionName);
            InputMap.ActionEraseEvents(actionName);
            InputMap.ActionAddEvent(actionName, inputEvent);
        }

        private void EnsureActionExists(string actionName)
        {
            if (!InputMap.HasAction(actionName))
            {
                InputMap.AddAction(actionName);
            }
        }

        private void InitializeDefaults()
        {
            if (_defaults.Count > 0)
            {
                return;
            }

            _defaults["move_left"] = CreateKey(Key.A);
            _defaults["move_right"] = CreateKey(Key.D);
            _defaults["jump"] = CreateKey(Key.Space);
            _defaults["sprint"] = CreateKey(Key.Shift);
            _defaults["duck"] = CreateKey(Key.C);
            _defaults["shoot"] = CreateMouse(MouseButton.Left);
            _defaults["shoot_forward"] = CreateKey(Key.Q);
            _defaults["melee"] = CreateKey(Key.F);
            _defaults["interact"] = CreateKey(Key.E);
            _defaults["up"] = CreateKey(Key.W);
            _defaults["down"] = CreateKey(Key.S);
            _defaults["ability0"] = CreateKey(Key.Q);
            _defaults["ability1"] = CreateKey(Key.E);
            _defaults["ability2"] = CreateKey(Key.Z);
            _defaults["ability3"] = CreateKey(Key.C);
            _defaults["attack_kick"] = CreateMouse(MouseButton.Left);
            _defaults["attack_balls"] = CreateMouse(MouseButton.Right);
            _defaults["heal"] = CreateKey(Key.F);
        }

        private void MigrateLegacyBindings()
        {
            if (_bindings.TryGetValue("duck", out InputEvent inputEvent) &&
                inputEvent is InputEventKey key &&
                key.PhysicalKeycode == Key.S)
            {
                _bindings["duck"] = CreateKey(Key.C);
                SaveSettings();
            }
        }

        private static InputEventKey CreateKey(Key key)
        {
            return new InputEventKey
            {
                PhysicalKeycode = key,
                Keycode = key,
            };
        }

        private static InputEventMouseButton CreateMouse(MouseButton button)
        {
            return new InputEventMouseButton
            {
                ButtonIndex = button,
            };
        }

        private static InputEvent NormalizeInputEvent(InputEvent inputEvent)
        {
            if (inputEvent is InputEventKey key)
            {
                return new InputEventKey
                {
                    PhysicalKeycode = key.PhysicalKeycode,
                    Keycode = key.Keycode,
                    ShiftPressed = key.ShiftPressed,
                    CtrlPressed = key.CtrlPressed,
                    AltPressed = key.AltPressed,
                    MetaPressed = key.MetaPressed,
                };
            }

            if (inputEvent is InputEventMouseButton mouse)
            {
                return new InputEventMouseButton
                {
                    ButtonIndex = mouse.ButtonIndex,
                };
            }

            return null;
        }

        private static string SerializeInputEvent(InputEvent inputEvent)
        {
            if (inputEvent is InputEventKey key)
            {
                return $"key:{(int)key.PhysicalKeycode}";
            }

            if (inputEvent is InputEventMouseButton mouse)
            {
                return $"mouse:{(int)mouse.ButtonIndex}";
            }

            return string.Empty;
        }

        private static InputEvent DeserializeInputEvent(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return null;
            }

            string[] parts = serialized.Split(':', 2);
            if (parts.Length != 2 || !int.TryParse(parts[1], out int value))
            {
                return null;
            }

            return parts[0] switch
            {
                "key" => CreateKey((Key)value),
                "mouse" => CreateMouse((MouseButton)value),
                _ => null,
            };
        }

        private static string DescribeInputEvent(InputEvent inputEvent)
        {
            if (inputEvent is InputEventKey key)
            {
                return OS.GetKeycodeString(key.PhysicalKeycode);
            }

            if (inputEvent is InputEventMouseButton mouse)
            {
                return mouse.ButtonIndex switch
                {
                    MouseButton.Left => "Mouse Left",
                    MouseButton.Right => "Mouse Right",
                    MouseButton.Middle => "Mouse Middle",
                    MouseButton.WheelUp => "Mouse Wheel Up",
                    MouseButton.WheelDown => "Mouse Wheel Down",
                    _ => $"Mouse {(int)mouse.ButtonIndex}",
                };
            }

            return "Unknown";
        }
    }
}
