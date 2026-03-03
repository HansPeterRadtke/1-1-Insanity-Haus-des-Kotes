using Godot;
using Insanity.Scripts.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Insanity.Scripts.Game
{
    public partial class GameManager : Node
    {
        private const string MenuScenePath = "res://Scenes/Menu/MainMenu.tscn";
        private const string DefaultGameScenePath = "res://Scenes/Tests/MechanicsTest.tscn";
        private const int SaveSlotCount = 3;

        private PackedScene _menuScene;
        private CanvasLayer _pauseMenuLayer;
        private MainMenu _pauseMenu;
        private RuntimeGameState _runtimeState = RuntimeGameState.Empty;

        public bool GameActive { get; private set; }
        public bool GamePaused { get; private set; }

        public override void _Ready()
        {
            _menuScene = GD.Load<PackedScene>(MenuScenePath);
        }

        public override void _Process(double delta)
        {
            if (GameActive && GetTree().CurrentScene != null)
            {
                RefreshRuntimeState();
            }
        }

        public void StartRun()
        {
            if (GamePaused)
            {
                HidePauseMenu();
            }

            GameActive = true;
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(DefaultGameScenePath);
            _runtimeState = RuntimeGameState.Empty;
        }

        public void TogglePauseMenu()
        {
            if (!GameActive)
            {
                return;
            }

            if (GamePaused)
            {
                HidePauseMenu();
                return;
            }

            ShowPauseMenu();
        }

        public void HidePauseMenu()
        {
            if (!GamePaused)
            {
                return;
            }

            GetTree().Paused = false;

            if (IsInstanceValid(_pauseMenu))
            {
                _pauseMenu.QueueFree();
            }

            if (IsInstanceValid(_pauseMenuLayer))
            {
                _pauseMenuLayer.QueueFree();
            }

            _pauseMenuLayer = null;
            _pauseMenu = null;
            GamePaused = false;
            GameActive = true;
        }

        public void QuitToMainMenu()
        {
            if (GamePaused)
            {
                HidePauseMenu();
            }

            GameActive = false;
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(MenuScenePath);
            _runtimeState = RuntimeGameState.Empty;
        }

        public void SaveGame()
        {
            SaveGame(1);
        }

        public void SaveGame(int slot)
        {
            if (!IsValidSlot(slot))
            {
                return;
            }

            Node currentScene = GetTree().CurrentScene;
            if (currentScene == null)
            {
                return;
            }

            RefreshRuntimeState();
            ConfigFile config = new();
            WriteRuntimeState(config, _runtimeState);
            config.Save(GetSlotPath(slot));
        }

        public async void LoadGame(int slot)
        {
            if (!HasSaveSlot(slot))
            {
                return;
            }

            ConfigFile config = new();
            if (config.Load(GetSlotPath(slot)) != Error.Ok)
            {
                return;
            }

            _runtimeState = ReadRuntimeState(config);

            if (GamePaused)
            {
                HidePauseMenu();
            }

            GameActive = _runtimeState.GameActive;
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile(string.IsNullOrEmpty(_runtimeState.ScenePath) ? DefaultGameScenePath : _runtimeState.ScenePath);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            ApplyRuntimeStateToCurrentScene();
        }

        public bool RestoreSaveIntoCurrentScene(int slot)
        {
            if (!HasSaveSlot(slot))
            {
                return false;
            }

            ConfigFile config = new();
            if (config.Load(GetSlotPath(slot)) != Error.Ok)
            {
                return false;
            }

            _runtimeState = ReadRuntimeState(config);
            GameActive = _runtimeState.GameActive;
            return ApplyRuntimeStateToCurrentScene();
        }

        public bool HasSaveSlot(int slot)
        {
            return IsValidSlot(slot) && FileAccess.FileExists(GetSlotPath(slot));
        }

        public void DeleteSaveSlot(int slot)
        {
            if (!IsValidSlot(slot) || !HasSaveSlot(slot))
            {
                return;
            }

            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GetSlotPath(slot)));
        }

        public string GetSaveSlotLabel(int slot)
        {
            if (!IsValidSlot(slot))
            {
                return $"Slot {slot}: Invalid";
            }

            if (!HasSaveSlot(slot))
            {
                return $"Slot {slot}: Empty";
            }

            ConfigFile config = new();
            if (config.Load(GetSlotPath(slot)) != Error.Ok)
            {
                return $"Slot {slot}: Unreadable";
            }

            string savedAt = config.GetValue("meta", "saved_at", "Unknown").AsString();
            string scenePath = config.GetValue("meta", "scene", DefaultGameScenePath).AsString();
            int savedNodes = config.GetValue("meta", "saved_nodes", 0).AsInt32();
            string sceneName = GetSceneDisplayName(scenePath);
            return $"Slot {slot}: {savedAt} / {sceneName} / {savedNodes} nodes";
        }

        private void ShowPauseMenu()
        {
            if (GamePaused || _menuScene == null)
            {
                return;
            }

            Node currentScene = GetTree().CurrentScene;
            if (currentScene == null)
            {
                return;
            }

            _pauseMenuLayer = new CanvasLayer
            {
                ProcessMode = ProcessModeEnum.WhenPaused,
                Layer = 100,
            };
            currentScene.AddChild(_pauseMenuLayer);

            GamePaused = true;

            _pauseMenu = _menuScene.Instantiate<MainMenu>();
            _pauseMenu.SetOverlayMode(true);
            _pauseMenu.ProcessMode = ProcessModeEnum.WhenPaused;
            _pauseMenuLayer.AddChild(_pauseMenu);

            GetTree().Paused = true;
        }

        private static bool IsValidSlot(int slot)
        {
            return slot >= 1 && slot <= SaveSlotCount;
        }

        private static string GetSlotPath(int slot)
        {
            return $"user://save_slot_{slot}.cfg";
        }

        private const string NodeSectionPrefix = "node:";
        private const string PropertyKeyPrefix = "property:";
        private const string StateKeyPrefix = "state:";
        private const string BuiltinKeyPrefix = "builtin:";
        private const string FieldKeyPrefix = "field:";
        private const string CustomKeyPrefix = "custom:";

        private static string GetNodeSectionName(int index)
        {
            return $"{NodeSectionPrefix}{index:0000}";
        }

        private static string GetSceneDisplayName(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return "Unknown";
            }

            string fileName = scenePath.Split('/').LastOrDefault() ?? scenePath;
            return fileName.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
                ? fileName[..^5]
                : fileName;
        }

        private void RefreshRuntimeState()
        {
            Node currentScene = GetTree().CurrentScene;
            if (currentScene == null)
            {
                _runtimeState = RuntimeGameState.Empty;
                return;
            }

            string scenePath = string.IsNullOrEmpty(currentScene.SceneFilePath) ? DefaultGameScenePath : currentScene.SceneFilePath;
            List<SaveStateSnapshot> nodes = CaptureCurrentSceneState(currentScene).OrderBy(n => n.Path, StringComparer.Ordinal).ToList();
            _runtimeState = new RuntimeGameState(
                Time.GetDatetimeStringFromSystem(),
                scenePath,
                GameActive,
                nodes
            );
        }

        private IEnumerable<SaveStateSnapshot> CaptureCurrentSceneState(Node currentScene)
        {
            foreach (Node node in EnumerateSceneNodes(currentScene))
            {
                if (node == currentScene || ShouldSkipNode(node))
                {
                    continue;
                }

                Node parentNode = node.GetParent();
                string parentPath = parentNode == null || parentNode == currentScene
                    ? string.Empty
                    : currentScene.GetPathTo(parentNode).ToString();

                yield return new SaveStateSnapshot(
                    currentScene.GetPathTo(node).ToString(),
                    parentPath,
                    node.Name,
                    node.GetClass(),
                    node.SceneFilePath,
                    GetScriptTypeName(node),
                    CaptureNodeState(node)
                );
            }
        }

        private static void WriteRuntimeState(ConfigFile config, RuntimeGameState runtimeState)
        {
            config.SetValue("meta", "saved_at", runtimeState.SavedAt);
            config.SetValue("meta", "scene", runtimeState.ScenePath);
            config.SetValue("meta", "game_active", runtimeState.GameActive);
            config.SetValue("meta", "saved_nodes", runtimeState.Nodes.Count);

            for (int i = 0; i < runtimeState.Nodes.Count; i++)
            {
                SaveStateSnapshot snapshot = runtimeState.Nodes[i];
                string section = GetNodeSectionName(i);
                config.SetValue(section, "path", snapshot.Path);
                config.SetValue(section, "parent_path", snapshot.ParentPath);
                config.SetValue(section, "name", snapshot.Name);
                config.SetValue(section, "class_name", snapshot.ClassName);
                config.SetValue(section, "scene_file", snapshot.SceneFilePath);
                config.SetValue(section, "script_type", snapshot.ScriptTypeName);

                foreach (var entry in snapshot.State)
                {
                    config.SetValue(section, $"{StateKeyPrefix}{entry.Key}", entry.Value);
                }
            }
        }

        private static RuntimeGameState ReadRuntimeState(ConfigFile config)
        {
            List<SaveStateSnapshot> nodes = new();
            foreach (string section in config.GetSections())
            {
                if (!section.StartsWith(NodeSectionPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                Godot.Collections.Dictionary<string, Variant> state = new();
                foreach (string key in config.GetSectionKeys(section))
                {
                    if (key.StartsWith(StateKeyPrefix, StringComparison.Ordinal))
                    {
                        state[key[StateKeyPrefix.Length..]] = config.GetValue(section, key);
                    }
                }

                nodes.Add(new SaveStateSnapshot(
                    config.GetValue(section, "path", string.Empty).AsString(),
                    config.GetValue(section, "parent_path", string.Empty).AsString(),
                    config.GetValue(section, "name", string.Empty).AsString(),
                    config.GetValue(section, "class_name", string.Empty).AsString(),
                    config.GetValue(section, "scene_file", string.Empty).AsString(),
                    config.GetValue(section, "script_type", string.Empty).AsString(),
                    state
                ));
            }

            nodes = nodes.OrderBy(n => n.Path, StringComparer.Ordinal).ToList();
            return new RuntimeGameState(
                config.GetValue("meta", "saved_at", "Unknown").AsString(),
                config.GetValue("meta", "scene", DefaultGameScenePath).AsString(),
                config.GetValue("meta", "game_active", true).AsBool(),
                nodes
            );
        }

        private bool ApplyRuntimeStateToCurrentScene()
        {
            Node currentScene = GetTree().CurrentScene;
            if (currentScene == null)
            {
                return false;
            }

            return RestoreSnapshotIntoScene(currentScene, _runtimeState.Nodes);
        }

        private static bool RestoreSnapshotIntoScene(Node currentScene, IReadOnlyList<SaveStateSnapshot> snapshots)
        {
            if (currentScene == null)
            {
                return false;
            }

            HashSet<string> savedPaths = new(StringComparer.Ordinal);
            foreach (SaveStateSnapshot snapshot in snapshots)
            {
                if (string.IsNullOrEmpty(snapshot.Path))
                {
                    continue;
                }

                savedPaths.Add(snapshot.Path);
                EnsureSnapshotNodeExists(currentScene, snapshot);
            }

            bool restoredAny = false;
            foreach (SaveStateSnapshot snapshot in snapshots)
            {
                if (string.IsNullOrEmpty(snapshot.Path))
                {
                    continue;
                }

                Node node = currentScene.GetNodeOrNull(new NodePath(snapshot.Path));
                if (node == null)
                {
                    continue;
                }

                RestoreNodeState(node, snapshot.State);
                restoredAny = true;
            }

            List<Node> nodesToRemove = new();
            foreach (Node node in EnumerateSceneNodes(currentScene))
            {
                if (node == currentScene)
                {
                    continue;
                }

                string nodePath = currentScene.GetPathTo(node).ToString();
                if (!savedPaths.Contains(nodePath))
                {
                    nodesToRemove.Add(node);
                }
            }

            foreach (Node node in nodesToRemove
                .OrderByDescending(n => n.GetPath().GetNameCount())
                .ToList())
            {
                if (GodotObject.IsInstanceValid(node))
                {
                    node.QueueFree();
                }
            }

            return restoredAny;
        }

        private static void EnsureSnapshotNodeExists(Node currentScene, SaveStateSnapshot snapshot)
        {
            if (currentScene.GetNodeOrNull(new NodePath(snapshot.Path)) != null)
            {
                return;
            }

            Node parentNode = string.IsNullOrEmpty(snapshot.ParentPath)
                ? currentScene
                : currentScene.GetNodeOrNull(new NodePath(snapshot.ParentPath));
            if (parentNode == null)
            {
                return;
            }

            Node instance = InstantiateSnapshotNode(snapshot);
            if (instance == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(snapshot.Name))
            {
                instance.Name = snapshot.Name;
            }

            parentNode.AddChild(instance);
        }

        private static Node InstantiateSnapshotNode(SaveStateSnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(snapshot.SceneFilePath))
            {
                PackedScene packedScene = GD.Load<PackedScene>(snapshot.SceneFilePath);
                if (packedScene != null)
                {
                    Node packedInstance = packedScene.Instantiate();
                    if (packedInstance != null)
                    {
                        return packedInstance;
                    }
                }
            }

            if (!string.IsNullOrEmpty(snapshot.ScriptTypeName))
            {
                Type scriptType = Type.GetType(snapshot.ScriptTypeName, false);
                if (scriptType != null &&
                    typeof(Node).IsAssignableFrom(scriptType) &&
                    Activator.CreateInstance(scriptType) is Node scriptedNode)
                {
                    return scriptedNode;
                }
            }

            if (!string.IsNullOrEmpty(snapshot.ClassName))
            {
                Type engineType = typeof(Node).Assembly.GetType($"Godot.{snapshot.ClassName}", false);
                if (engineType != null &&
                    typeof(Node).IsAssignableFrom(engineType) &&
                    Activator.CreateInstance(engineType) is Node engineNode)
                {
                    return engineNode;
                }
            }

            return null;
        }

        private bool ShouldSkipNode(Node node)
        {
            return _pauseMenuLayer != null &&
                   (node == _pauseMenuLayer || _pauseMenuLayer.IsAncestorOf(node));
        }

        private static IEnumerable<Node> EnumerateSceneNodes(Node root)
        {
            Queue<Node> queue = new();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                yield return current;

                foreach (Node child in current.GetChildren())
                {
                    queue.Enqueue(child);
                }
            }
        }

        private static Godot.Collections.Dictionary<string, Variant> CaptureNodeState(Node node)
        {
            Godot.Collections.Dictionary<string, Variant> state = new();

            foreach (var entry in CapturePropertyState(node))
            {
                state[$"{PropertyKeyPrefix}{entry.Key}"] = entry.Value;
            }

            foreach (var entry in CaptureBuiltInState(node))
            {
                state[$"{BuiltinKeyPrefix}{entry.Key}"] = entry.Value;
            }

            foreach (var entry in CaptureScriptFields(node))
            {
                state[$"{FieldKeyPrefix}{entry.Key}"] = entry.Value;
            }

            if (node is ISaveStateNode saveStateNode)
            {
                foreach (var entry in saveStateNode.CaptureSaveState())
                {
                    state[$"{CustomKeyPrefix}{entry.Key}"] = entry.Value;
                }
            }

            return state;
        }

        private static void RestoreNodeState(Node node, Godot.Collections.Dictionary<string, Variant> state)
        {
            Godot.Collections.Dictionary<string, Variant> customState = new();

            foreach (var entry in state)
            {
                string key = entry.Key;
                Variant value = entry.Value;

                if (key.StartsWith(BuiltinKeyPrefix, StringComparison.Ordinal))
                {
                    ApplyBuiltInState(node, key[BuiltinKeyPrefix.Length..], value);
                    continue;
                }

                if (key.StartsWith(PropertyKeyPrefix, StringComparison.Ordinal))
                {
                    TrySetProperty(node, key[PropertyKeyPrefix.Length..], value);
                    continue;
                }

                if (key.StartsWith(FieldKeyPrefix, StringComparison.Ordinal))
                {
                    TrySetScriptField(node, key[FieldKeyPrefix.Length..], value);
                    continue;
                }

                if (key.StartsWith(CustomKeyPrefix, StringComparison.Ordinal))
                {
                    customState[key[CustomKeyPrefix.Length..]] = value;
                    continue;
                }

                // Backward compatibility for the previous custom-only format.
                customState[key] = value;
            }

            if (node is ISaveStateNode saveStateNode)
            {
                saveStateNode.RestoreSaveState(customState);
            }
        }

        private static IEnumerable<KeyValuePair<string, Variant>> CapturePropertyState(Node node)
        {
            if (node is Control control)
            {
                yield return new KeyValuePair<string, Variant>("control.position", control.Position);
                yield return new KeyValuePair<string, Variant>("control.size", control.Size);
                yield return new KeyValuePair<string, Variant>("control.custom_minimum_size", control.CustomMinimumSize);
            }

            if (node is Button button)
            {
                yield return new KeyValuePair<string, Variant>("button.text", button.Text);
            }

            if (node is Godot.Range range)
            {
                yield return new KeyValuePair<string, Variant>("range.value", (float)range.Value);
            }

            if (node is OptionButton optionButton)
            {
                yield return new KeyValuePair<string, Variant>("option_button.selected", optionButton.Selected);
            }

            if (node is Sprite2D sprite)
            {
                yield return new KeyValuePair<string, Variant>("sprite2d.flip_h", sprite.FlipH);
                yield return new KeyValuePair<string, Variant>("sprite2d.flip_v", sprite.FlipV);
                yield return new KeyValuePair<string, Variant>("sprite2d.frame", sprite.Frame);
                yield return new KeyValuePair<string, Variant>("sprite2d.frame_coords", sprite.FrameCoords);
            }
        }

        private static IEnumerable<KeyValuePair<string, Variant>> CaptureBuiltInState(Node node)
        {
            if (node is Node2D node2D)
            {
                yield return new KeyValuePair<string, Variant>("node2d.global_position", node2D.GlobalPosition);
                yield return new KeyValuePair<string, Variant>("node2d.rotation", node2D.Rotation);
                yield return new KeyValuePair<string, Variant>("node2d.scale", node2D.Scale);
            }

            if (node is CharacterBody2D body2D)
            {
                yield return new KeyValuePair<string, Variant>("character_body2d.velocity", body2D.Velocity);
            }

            if (node is CanvasItem canvasItem)
            {
                yield return new KeyValuePair<string, Variant>("canvas_item.visible", canvasItem.Visible);
                yield return new KeyValuePair<string, Variant>("canvas_item.modulate", canvasItem.Modulate);
            }

            if (node is Label label)
            {
                yield return new KeyValuePair<string, Variant>("label.text", label.Text);
            }

            if (node is BaseButton button)
            {
                yield return new KeyValuePair<string, Variant>("button.button_pressed", button.ButtonPressed);
            }
        }

        private static void ApplyBuiltInState(Node node, string key, Variant value)
        {
            switch (key)
            {
                case "node2d.global_position" when node is Node2D node2D:
                    node2D.GlobalPosition = value.AsVector2();
                    break;
                case "node2d.rotation" when node is Node2D node2DRotation:
                    node2DRotation.Rotation = value.AsSingle();
                    break;
                case "node2d.scale" when node is Node2D node2DScale:
                    node2DScale.Scale = value.AsVector2();
                    break;
                case "character_body2d.velocity" when node is CharacterBody2D body2D:
                    body2D.Velocity = value.AsVector2();
                    break;
                case "canvas_item.visible" when node is CanvasItem canvasItem:
                    canvasItem.Visible = value.AsBool();
                    break;
                case "canvas_item.modulate" when node is CanvasItem canvasItemModulate:
                    canvasItemModulate.Modulate = value.AsColor();
                    break;
                case "label.text" when node is Label label:
                    label.Text = value.AsString();
                    break;
                case "button.button_pressed" when node is BaseButton button:
                    button.ButtonPressed = value.AsBool();
                    break;
            }
        }

        private static bool TrySetProperty(Node node, string propertyName, Variant value)
        {
            switch (propertyName)
            {
                case "control.position" when node is Control control:
                    control.Position = value.AsVector2();
                    return true;
                case "control.size" when node is Control control:
                    control.Size = value.AsVector2();
                    return true;
                case "control.custom_minimum_size" when node is Control control:
                    control.CustomMinimumSize = value.AsVector2();
                    return true;
                case "button.text" when node is Button button:
                    button.Text = value.AsString();
                    return true;
                case "range.value" when node is Godot.Range range:
                    range.Value = value.AsSingle();
                    return true;
                case "option_button.selected" when node is OptionButton optionButton:
                    optionButton.Select(value.AsInt32());
                    return true;
                case "sprite2d.flip_h" when node is Sprite2D sprite:
                    sprite.FlipH = value.AsBool();
                    return true;
                case "sprite2d.flip_v" when node is Sprite2D sprite:
                    sprite.FlipV = value.AsBool();
                    return true;
                case "sprite2d.frame" when node is Sprite2D sprite:
                    sprite.Frame = value.AsInt32();
                    return true;
                case "sprite2d.frame_coords" when node is Sprite2D sprite:
                    sprite.FrameCoords = value.AsVector2I();
                    return true;
                default:
                    return false;
            }
        }

        private static IEnumerable<KeyValuePair<string, Variant>> CaptureScriptFields(Node node)
        {
            Assembly gameAssembly = typeof(GameManager).Assembly;
            Type currentType = node.GetType();

            while (currentType != null && currentType != typeof(object) && currentType.Assembly == gameAssembly)
            {
                foreach (FieldInfo field in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.IsInitOnly || field.IsLiteral)
                    {
                        continue;
                    }

                    if (Attribute.IsDefined(field, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)))
                    {
                        continue;
                    }

                    if (!TryConvertObjectToVariant(field.GetValue(node), out Variant variant))
                    {
                        continue;
                    }

                    yield return new KeyValuePair<string, Variant>($"{currentType.FullName}|{field.Name}", variant);
                }

                currentType = currentType.BaseType;
            }
        }

        private static bool TrySetScriptField(Node node, string encodedField, Variant value)
        {
            int separatorIndex = encodedField.LastIndexOf('|');
            if (separatorIndex <= 0 || separatorIndex >= encodedField.Length - 1)
            {
                return false;
            }

            string typeName = encodedField[..separatorIndex];
            string fieldName = encodedField[(separatorIndex + 1)..];

            Type currentType = node.GetType();
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.FullName == typeName)
                {
                    FieldInfo field = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (field == null || field.IsStatic || field.IsInitOnly || field.IsLiteral)
                    {
                        return false;
                    }

                    if (!TryConvertVariantToObject(value, field.FieldType, out object convertedValue))
                    {
                        return false;
                    }

                    field.SetValue(node, convertedValue);
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        private static bool TryConvertObjectToVariant(object value, out Variant variant)
        {
            variant = default;

            if (value == null)
            {
                return false;
            }

            Type valueType = value.GetType();
            Type underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;

            if (underlyingType.IsEnum)
            {
                variant = Convert.ToInt32(value);
                return true;
            }

            if (value is GodotObject || value is Delegate)
            {
                return false;
            }

            switch (value)
            {
                case bool boolValue:
                    variant = boolValue;
                    return true;
                case byte byteValue:
                    variant = (int)byteValue;
                    return true;
                case sbyte sbyteValue:
                    variant = (int)sbyteValue;
                    return true;
                case short shortValue:
                    variant = (int)shortValue;
                    return true;
                case ushort ushortValue:
                    variant = (int)ushortValue;
                    return true;
                case int intValue:
                    variant = intValue;
                    return true;
                case uint uintValue when uintValue <= int.MaxValue:
                    variant = (int)uintValue;
                    return true;
                case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                    variant = (int)longValue;
                    return true;
                case ulong ulongValue when ulongValue <= int.MaxValue:
                    variant = (int)ulongValue;
                    return true;
                case float floatValue:
                    variant = floatValue;
                    return true;
                case double doubleValue when doubleValue >= float.MinValue && doubleValue <= float.MaxValue:
                    variant = (float)doubleValue;
                    return true;
                case string stringValue:
                    variant = stringValue;
                    return true;
                case StringName stringNameValue:
                    variant = stringNameValue;
                    return true;
                case NodePath nodePathValue:
                    variant = nodePathValue;
                    return true;
                case Vector2 vector2Value:
                    variant = vector2Value;
                    return true;
                case Vector2I vector2IValue:
                    variant = vector2IValue;
                    return true;
                case Color colorValue:
                    variant = colorValue;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryConvertVariantToObject(Variant variant, Type targetType, out object convertedValue)
        {
            convertedValue = null;
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType.IsEnum)
            {
                convertedValue = Enum.ToObject(underlyingType, variant.AsInt32());
                return true;
            }

            if (underlyingType == typeof(bool))
            {
                convertedValue = variant.AsBool();
                return true;
            }

            if (underlyingType == typeof(byte))
            {
                convertedValue = (byte)variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(sbyte))
            {
                convertedValue = (sbyte)variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(short))
            {
                convertedValue = (short)variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(ushort))
            {
                convertedValue = (ushort)variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(int))
            {
                convertedValue = variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(uint))
            {
                convertedValue = (uint)variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(long))
            {
                convertedValue = (long)variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(ulong))
            {
                convertedValue = (ulong)variant.AsInt32();
                return true;
            }

            if (underlyingType == typeof(float))
            {
                convertedValue = variant.AsSingle();
                return true;
            }

            if (underlyingType == typeof(double))
            {
                convertedValue = (double)variant.AsSingle();
                return true;
            }

            if (underlyingType == typeof(string))
            {
                convertedValue = variant.AsString();
                return true;
            }

            if (underlyingType == typeof(StringName))
            {
                convertedValue = new StringName(variant.AsString());
                return true;
            }

            if (underlyingType == typeof(NodePath))
            {
                convertedValue = new NodePath(variant.AsString());
                return true;
            }

            if (underlyingType == typeof(Vector2))
            {
                convertedValue = variant.AsVector2();
                return true;
            }

            if (underlyingType == typeof(Vector2I))
            {
                convertedValue = variant.AsVector2I();
                return true;
            }

            if (underlyingType == typeof(Color))
            {
                convertedValue = variant.AsColor();
                return true;
            }

            return false;
        }

        private static string GetScriptTypeName(Node node)
        {
            Type nodeType = node.GetType();
            return nodeType.Assembly == typeof(GameManager).Assembly
                ? nodeType.AssemblyQualifiedName ?? string.Empty
                : string.Empty;
        }

        private readonly record struct SaveStateSnapshot(
            string Path,
            string ParentPath,
            string Name,
            string ClassName,
            string SceneFilePath,
            string ScriptTypeName,
            Godot.Collections.Dictionary<string, Variant> State
        );

        private readonly record struct RuntimeGameState(
            string SavedAt,
            string ScenePath,
            bool GameActive,
            List<SaveStateSnapshot> Nodes
        )
        {
            public static RuntimeGameState Empty => new(
                "Unknown",
                string.Empty,
                false,
                new List<SaveStateSnapshot>()
            );
        }
    }
}
