using Godot;
using Insanity.Scripts.Audio;
using Insanity.Scripts.Animation;
using Insanity.Scripts.Enemies;
using PlayerCharacter = Insanity.Scripts.Player.Player;

namespace Insanity.Scripts.Tests
{
    public partial class AnimationSystemTest : Node2D
    {
        private PlayerCharacter _player;
        private MeleeEnemy _enemy;
        private AnimationController _playerController;
        private AnimationController _enemyController;

        public override void _Ready()
        {
            _player = GetNodeOrNull<PlayerCharacter>("Player");
            _enemy = GetNodeOrNull<MeleeEnemy>("MeleeEnemy");
            _playerController = _player?.GetNodeOrNull<AnimationController>("AnimationController");
            _enemyController = _enemy?.GetNodeOrNull<AnimationController>("AnimationController");

            DisableGameplayLogic(_player);
            DisableGameplayLogic(_enemy);
            EnableDebug(_playerController);
            EnableDebug(_enemyController);

            GD.Print("AnimationSfxTest ready. Keys: 1 idle, 2 run, 3 jump, 4 attack, 5 hit, 6 die.");

            if (DisplayServer.GetName() == "headless")
            {
                RunHeadlessSmokeTest();
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventKey key || !key.Pressed || key.Echo)
            {
                return;
            }

            switch (key.Keycode)
            {
                case Key.Key1:
                    ApplyState(AnimationStates.Idle, false);
                    break;
                case Key.Key2:
                    ApplyState(AnimationStates.Run, false);
                    break;
                case Key.Key3:
                    ApplyState(AnimationStates.Jump, false);
                    break;
                case Key.Key4:
                    ApplyState(AnimationStates.AttackMelee, true);
                    break;
                case Key.Key5:
                    ApplyState(AnimationStates.Hit, true);
                    break;
                case Key.Key6:
                    ApplyState(AnimationStates.Die, true);
                    break;
            }
        }

        private static void DisableGameplayLogic(Node node)
        {
            if (node == null)
            {
                return;
            }

            node.SetPhysicsProcess(false);
            node.SetProcess(false);
            node.SetProcessInput(false);
            node.SetProcessUnhandledInput(false);
        }

        private static void EnableDebug(AnimationController controller)
        {
            if (controller == null)
            {
                return;
            }

            controller.LogEvents = true;
            controller.LogStateChanges = true;
            if (controller.GetNodeOrNull<SfxPlayer>("SfxPlayer") is { } sfx)
            {
                sfx.LogPlayback = true;
                sfx.UseGeneratedFallbackSfx = true;
            }
        }

        private void ApplyState(string stateName, bool oneShot)
        {
            GD.Print($"[AnimationSfxTest] {(oneShot ? "one-shot" : "state")} -> {stateName}");

            if (oneShot)
            {
                _playerController?.PlayOneShot(stateName);
                _enemyController?.PlayOneShot(stateName);
                return;
            }

            _playerController?.PlayState(stateName);
            _enemyController?.PlayState(stateName);
        }

        private void RunHeadlessSmokeTest()
        {
            ApplyState(AnimationStates.Run, false);
            _playerController?.OnAnimEvent(AnimationEvents.Footstep);
            _enemyController?.OnAnimEvent(AnimationEvents.Footstep);
            ApplyState(AnimationStates.AttackMelee, true);
            _playerController?.OnAnimEvent(AnimationEvents.Swing);
            _enemyController?.OnAnimEvent(AnimationEvents.Swing);
            ApplyState(AnimationStates.Hit, true);
            _playerController?.OnAnimEvent(AnimationEvents.Hit);
            _enemyController?.OnAnimEvent(AnimationEvents.Hit);
            ApplyState(AnimationStates.Die, true);
            GD.Print("AnimationSfxTest headless smoke passed.");
            GetTree().Quit();
        }
    }
}
