using Godot;
using Insanity.Scripts.Game;
using System;
using System.Collections.Generic;

namespace Insanity.Scripts.Menu
{
    public partial class MainMenu : Control
    {
        private const string GameScenePath = "res://Scenes/Tests/MechanicsTest.tscn";

        private SettingsManager _settings;
        private VBoxContainer _panelHost;
        private Control _mainPanel;
        private Control _loadPanel;
        private Control _creditsPanel;
        private Control _settingsPanel;
        private Control _controlsPanel;
        private Control _audioPanel;
        private Control _videoPanel;
        private Label _statusLabel;
        private Label _controlsHelpLabel;
        private Label _audioValueLabel;
        private CheckButton _fullscreenToggle;
        private OptionButton _windowScaleOptions;
        private HSlider _audioSlider;
        private Button _continueButton;
        private Button _saveButton;
        private readonly List<Label> _slotLabels = new();
        private readonly List<Button> _slotLoadButtons = new();
        private readonly List<Button> _slotSaveButtons = new();
        private readonly List<Button> _slotDeleteButtons = new();
        private readonly Dictionary<string, KeybindRow> _bindingRows = new();
        private string _pendingAction = string.Empty;
        private GameManager _gameManager;
        private bool _isOverlayMode;

        public override void _Ready()
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            _settings = GetNode<SettingsManager>("/root/SettingsManager");
            _gameManager = GetNode<GameManager>("/root/GameManager");
            BuildUi();
            RefreshAllState();
            ShowPanel(_mainPanel, _isOverlayMode ? "Paused" : "Main Menu");
            UpdateSessionButtons();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (string.IsNullOrEmpty(_pendingAction))
            {
                if (_isOverlayMode &&
                    @event is InputEventKey key &&
                    key.Pressed &&
                    !key.Echo &&
                    key.Keycode == Key.Escape)
                {
                    OnContinuePressed();
                    AcceptEvent();
                }

                return;
            }

            if (!TryExtractBindableEvent(@event, out InputEvent bindEvent))
            {
                return;
            }

            _settings.SetBinding(_pendingAction, bindEvent);
            if (_bindingRows.TryGetValue(_pendingAction, out KeybindRow row))
            {
                row.SetBindingText(_settings.GetBindingLabel(_pendingAction));
            }

            _controlsHelpLabel.Text = $"Rebound {_settings.GetActionDisplayName(_pendingAction)}.";
            _pendingAction = string.Empty;
            AcceptEvent();
        }

        private void BuildUi()
        {
            ColorRect background = new()
            {
                Color = new Color(0.05f, 0.06f, 0.1f, 1.0f),
                AnchorRight = 1.0f,
                AnchorBottom = 1.0f,
            };
            AddChild(background);

            MarginContainer rootMargin = new()
            {
                AnchorRight = 1.0f,
                AnchorBottom = 1.0f,
                OffsetLeft = 32.0f,
                OffsetTop = 24.0f,
                OffsetRight = -32.0f,
                OffsetBottom = -24.0f,
            };
            AddChild(rootMargin);

            VBoxContainer rootLayout = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            rootLayout.AddThemeConstantOverride("separation", 18);
            rootMargin.AddChild(rootLayout);

            Label title = new()
            {
                Text = "Insanity Haus des Kotes",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            title.AddThemeFontSizeOverride("font_size", 32);
            rootLayout.AddChild(title);

            Label subtitle = new()
            {
                Text = "Standard menu + persistent settings",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            subtitle.AddThemeFontSizeOverride("font_size", 16);
            rootLayout.AddChild(subtitle);

            HBoxContainer body = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            body.AddThemeConstantOverride("separation", 24);
            rootLayout.AddChild(body);

            PanelContainer navPanel = new()
            {
                CustomMinimumSize = new Vector2(300.0f, 0.0f),
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            body.AddChild(navPanel);

            VBoxContainer navLayout = new()
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            navLayout.AddThemeConstantOverride("separation", 12);
            navPanel.AddChild(navLayout);

            Label navTitle = new()
            {
                Text = "Menu",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            navTitle.AddThemeFontSizeOverride("font_size", 20);
            navLayout.AddChild(navTitle);

            navLayout.AddChild(CreateMenuButton("New Game", OnNewGamePressed));
            _continueButton = CreateMenuButton("Continue", OnContinuePressed);
            navLayout.AddChild(_continueButton);
            _saveButton = CreateMenuButton("Save Game", OnSaveGamePressed);
            navLayout.AddChild(_saveButton);
            navLayout.AddChild(CreateMenuButton("Load Game", () => ShowPanel(_loadPanel, "Load Game")));
            navLayout.AddChild(CreateMenuButton("Settings", () => ShowPanel(_settingsPanel, "Settings")));
            navLayout.AddChild(CreateMenuButton("Credits", () => ShowPanel(_creditsPanel, "Credits")));
            navLayout.AddChild(CreateMenuButton("Quit", () => GetTree().Quit()));

            _statusLabel = new Label
            {
                Text = string.Empty,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            navLayout.AddChild(_statusLabel);

            PanelContainer contentPanel = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            body.AddChild(contentPanel);

            _panelHost = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            contentPanel.AddChild(_panelHost);

            _mainPanel = BuildInfoPanel(
                "Welcome",
                "Start a new run, open the mechanic test scene, or configure controls, audio, and video."
            );
            _loadPanel = BuildSaveLoadPanel();
            _creditsPanel = BuildStubPanel("Credits", "Prototype built in Godot 4.6 + C#.");
            _settingsPanel = BuildSettingsHub();
            _controlsPanel = BuildControlsPanel();
            _audioPanel = BuildAudioPanel();
            _videoPanel = BuildVideoPanel();

            _panelHost.AddChild(_mainPanel);
            _panelHost.AddChild(_loadPanel);
            _panelHost.AddChild(_creditsPanel);
            _panelHost.AddChild(_settingsPanel);
            _panelHost.AddChild(_controlsPanel);
            _panelHost.AddChild(_audioPanel);
            _panelHost.AddChild(_videoPanel);
        }

        private Control BuildInfoPanel(string title, string body)
        {
            VBoxContainer panel = CreatePanelRoot();
            panel.AddChild(CreateHeader(title));
            panel.AddChild(new Label
            {
                Text = body,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            });
            return panel;
        }

        private Control BuildStubPanel(string title, string body)
        {
            VBoxContainer panel = CreatePanelRoot();
            panel.AddChild(CreateHeader(title));
            panel.AddChild(new Label
            {
                Text = body,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            });
            panel.AddChild(CreateMenuButton("Back", () => ShowPanel(_mainPanel, title)));
            return panel;
        }

        private Control BuildSaveLoadPanel()
        {
            VBoxContainer panel = CreatePanelRoot();
            panel.AddChild(CreateHeader("Save / Load"));
            panel.AddChild(new Label
            {
                Text = "Use one of the save slots below. Load is available when a slot exists. Save is available only in-game.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });

            for (int slot = 1; slot <= 3; slot++)
            {
                HBoxContainer row = new();
                row.AddThemeConstantOverride("separation", 10);

                Label slotLabel = new()
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                _slotLabels.Add(slotLabel);
                row.AddChild(slotLabel);

                int capturedSlot = slot;
                Button loadButton = CreateMenuButton("Load", () => OnLoadSlotPressed(capturedSlot));
                loadButton.CustomMinimumSize = new Vector2(100.0f, 36.0f);
                _slotLoadButtons.Add(loadButton);
                row.AddChild(loadButton);

                Button saveButton = CreateMenuButton("Save", () => OnSaveSlotPressed(capturedSlot));
                saveButton.CustomMinimumSize = new Vector2(100.0f, 36.0f);
                _slotSaveButtons.Add(saveButton);
                row.AddChild(saveButton);

                Button deleteButton = CreateMenuButton("Delete", () => OnDeleteSlotPressed(capturedSlot));
                deleteButton.CustomMinimumSize = new Vector2(100.0f, 36.0f);
                _slotDeleteButtons.Add(deleteButton);
                row.AddChild(deleteButton);

                panel.AddChild(row);
            }

            panel.AddChild(CreateMenuButton("Back", () => ShowPanel(_mainPanel, _isOverlayMode ? "Paused" : "Main Menu")));
            return panel;
        }

        private Control BuildSettingsHub()
        {
            VBoxContainer panel = CreatePanelRoot();
            panel.AddChild(CreateHeader("Settings"));
            panel.AddChild(CreateMenuButton("Controls", () => ShowPanel(_controlsPanel, "Controls")));
            panel.AddChild(CreateMenuButton("Audio", () => ShowPanel(_audioPanel, "Audio")));
            panel.AddChild(CreateMenuButton("Video / Graphics", () => ShowPanel(_videoPanel, "Video")));
            panel.AddChild(CreateMenuButton("Back", () => ShowPanel(_mainPanel, "Main Menu")));
            return panel;
        }

        private Control BuildControlsPanel()
        {
            VBoxContainer panel = CreatePanelRoot();
            panel.AddChild(CreateHeader("Controls"));

            ScrollContainer scroll = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            panel.AddChild(scroll);

            VBoxContainer rows = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            rows.AddThemeConstantOverride("separation", 8);
            scroll.AddChild(rows);

            foreach (string action in _settings.ManagedActionNames)
            {
                KeybindRow row = new();
                rows.AddChild(row);
                row.RebindRequested += OnRebindRequested;
                row.Configure(_settings.GetActionDisplayName(action), action, _settings.GetBindingLabel(action));
                _bindingRows[action] = row;
            }

            _controlsHelpLabel = new Label
            {
                Text = "Click a binding, then press the next keyboard key or mouse button.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            panel.AddChild(_controlsHelpLabel);
            panel.AddChild(CreateMenuButton("Back", () => ShowPanel(_settingsPanel, "Settings")));
            return panel;
        }

        private Control BuildAudioPanel()
        {
            VBoxContainer panel = CreatePanelRoot();
            panel.AddChild(CreateHeader("Audio"));

            _audioValueLabel = new Label();
            panel.AddChild(_audioValueLabel);

            _audioSlider = new HSlider
            {
                MinValue = -40.0,
                MaxValue = 6.0,
                Step = 1.0,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _audioSlider.ValueChanged += OnAudioSliderChanged;
            panel.AddChild(_audioSlider);

            HBoxContainer actions = new();
            actions.AddThemeConstantOverride("separation", 10);
            actions.AddChild(CreateMenuButton("Apply", ApplyAudioSettings));
            actions.AddChild(CreateMenuButton("Back", () => ShowPanel(_settingsPanel, "Settings")));
            panel.AddChild(actions);
            return panel;
        }

        private Control BuildVideoPanel()
        {
            VBoxContainer panel = CreatePanelRoot();
            panel.AddChild(CreateHeader("Video / Graphics"));

            _fullscreenToggle = new CheckButton
            {
                Text = "Fullscreen",
            };
            panel.AddChild(_fullscreenToggle);

            Label scaleLabel = new()
            {
                Text = "Window Scale",
            };
            panel.AddChild(scaleLabel);

            _windowScaleOptions = new OptionButton();
            _windowScaleOptions.AddItem("100%", 100);
            _windowScaleOptions.AddItem("125%", 125);
            _windowScaleOptions.AddItem("150%", 150);
            _windowScaleOptions.AddItem("200%", 200);
            panel.AddChild(_windowScaleOptions);

            HBoxContainer actions = new();
            actions.AddThemeConstantOverride("separation", 10);
            actions.AddChild(CreateMenuButton("Apply", ApplyVideoSettings));
            actions.AddChild(CreateMenuButton("Back", () => ShowPanel(_settingsPanel, "Settings")));
            panel.AddChild(actions);
            return panel;
        }

        private VBoxContainer CreatePanelRoot()
        {
            VBoxContainer panel = new()
            {
                Visible = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            panel.AddThemeConstantOverride("separation", 14);
            return panel;
        }

        private static Label CreateHeader(string text)
        {
            Label label = new()
            {
                Text = text,
            };
            label.AddThemeFontSizeOverride("font_size", 24);
            return label;
        }

        private static Button CreateMenuButton(string text, Action onPressed)
        {
            Button button = new()
            {
                Text = text,
                CustomMinimumSize = new Vector2(0.0f, 36.0f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.Pressed += onPressed;
            return button;
        }

        private void RefreshAllState()
        {
            foreach ((string action, KeybindRow row) in _bindingRows)
            {
                row.SetBindingText(_settings.GetBindingLabel(action));
            }

            _audioSlider.Value = _settings.MasterVolumeDb;
            _audioValueLabel.Text = $"Master Volume: {_settings.MasterVolumeDb:0} dB";
            _fullscreenToggle.ButtonPressed = _settings.Fullscreen;

            int desired = Mathf.RoundToInt(_settings.WindowScale * 100.0f);
            int index = 0;
            for (int i = 0; i < _windowScaleOptions.ItemCount; i++)
            {
                if (_windowScaleOptions.GetItemId(i) == desired)
                {
                    index = i;
                    break;
                }
            }

            _windowScaleOptions.Select(index);
            UpdateSessionButtons();
            RefreshSaveSlots();
        }

        private void ShowPanel(Control panel, string status)
        {
            foreach (Node child in _panelHost.GetChildren())
            {
                if (child is CanvasItem item)
                {
                    item.Visible = child == panel;
                }
            }

            _statusLabel.Text = status;
            UpdateSessionButtons();
        }

        private void OnNewGamePressed()
        {
            _gameManager.StartRun();
        }

        private void OnContinuePressed()
        {
            if (_isOverlayMode)
            {
                _gameManager?.HidePauseMenu();
            }
        }

        private void OnSaveGamePressed()
        {
            ShowPanel(_loadPanel, "Save / Load");
        }

        private void OnLoadSlotPressed(int slot)
        {
            if (_gameManager == null || !_gameManager.HasSaveSlot(slot))
            {
                _statusLabel.Text = $"Slot {slot} is empty.";
                return;
            }

            _gameManager.LoadGame(slot);
        }

        private void OnSaveSlotPressed(int slot)
        {
            if (_gameManager?.GameActive != true)
            {
                _statusLabel.Text = "Saving is only available in-game.";
                return;
            }

            _gameManager.SaveGame(slot);
            RefreshSaveSlots();
            _statusLabel.Text = $"Saved to slot {slot}.";
        }

        private void OnDeleteSlotPressed(int slot)
        {
            if (_gameManager == null || !_gameManager.HasSaveSlot(slot))
            {
                _statusLabel.Text = $"Slot {slot} is already empty.";
                return;
            }

            _gameManager.DeleteSaveSlot(slot);
            RefreshSaveSlots();
            _statusLabel.Text = $"Deleted slot {slot}.";
        }

        private void OnRebindRequested(string actionName)
        {
            _pendingAction = actionName;
            _controlsHelpLabel.Text = $"Press a key or mouse button for {_settings.GetActionDisplayName(actionName)}.";
            if (_bindingRows.TryGetValue(actionName, out KeybindRow row))
            {
                row.SetBindingText("Press input...");
            }
        }

        private void OnAudioSliderChanged(double value)
        {
            _audioValueLabel.Text = $"Master Volume: {value:0} dB";
        }

        private void ApplyAudioSettings()
        {
            _settings.SetMasterVolumeDb((float)_audioSlider.Value);
            _settings.ApplyAudioSettings();
            _settings.SaveSettings();
            ShowPanel(_settingsPanel, "Audio Applied");
        }

        private void ApplyVideoSettings()
        {
            float scale = _windowScaleOptions.GetSelectedId() / 100.0f;
            _settings.SetVideoSettings(_fullscreenToggle.ButtonPressed, scale);
            _settings.ApplyVideoSettings();
            _settings.SaveSettings();
            ShowPanel(_settingsPanel, "Video Applied");
        }

        private static bool TryExtractBindableEvent(InputEvent source, out InputEvent bindEvent)
        {
            bindEvent = null;

            if (source is InputEventKey key && key.Pressed && !key.Echo)
            {
                bindEvent = new InputEventKey
                {
                    PhysicalKeycode = key.PhysicalKeycode,
                    Keycode = key.Keycode,
                    ShiftPressed = key.ShiftPressed,
                    CtrlPressed = key.CtrlPressed,
                    AltPressed = key.AltPressed,
                    MetaPressed = key.MetaPressed,
                };
                return true;
            }

            if (source is InputEventMouseButton mouse && mouse.Pressed)
            {
                bindEvent = new InputEventMouseButton
                {
                    ButtonIndex = mouse.ButtonIndex,
                };
                return true;
            }

            return false;
        }

        public void SetOverlayMode(bool overlayMode)
        {
            _isOverlayMode = overlayMode;
            if (IsNodeReady())
            {
                ShowPanel(_mainPanel, _isOverlayMode ? "Paused" : "Main Menu");
            }
        }

        private void UpdateSessionButtons()
        {
            if (_saveButton == null || _continueButton == null)
            {
                return;
            }

            bool canResume = _isOverlayMode && _gameManager != null && _gameManager.GamePaused;
            bool canSave = _gameManager != null && _gameManager.GameActive;

            _continueButton.Visible = canResume;
            _continueButton.Disabled = !canResume;

            _saveButton.Visible = canSave;
            _saveButton.Disabled = !canSave;
            RefreshSaveSlots();
        }

        private void RefreshSaveSlots()
        {
            if (_gameManager == null || _slotLabels.Count == 0)
            {
                return;
            }

            bool canSave = _gameManager.GameActive;

            for (int i = 0; i < _slotLabels.Count; i++)
            {
                int slot = i + 1;
                _slotLabels[i].Text = _gameManager.GetSaveSlotLabel(slot);
                bool hasSave = _gameManager.HasSaveSlot(slot);
                _slotLoadButtons[i].Disabled = !hasSave;
                _slotSaveButtons[i].Visible = canSave;
                _slotSaveButtons[i].Disabled = !canSave;
                _slotDeleteButtons[i].Disabled = !hasSave;
            }
        }
    }
}
