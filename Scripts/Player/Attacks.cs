using Godot;
using Insanity.Scripts.Animation;
using Insanity.Scripts.Enemies;
using Insanity.Scripts.Shared;
using System;

namespace Insanity.Scripts.Player
{
    public partial class Attacks : Marker2D
    {
        [ExportGroup("Kick")]
        [Export] public int KickDamage = 5;
        [Export] public float MeleeAreaOffset = 28.0f;

        [ExportGroup("Blue Balls")]
        [Export] public float BlueBallRate = 0.2f;
        [Export] public float BlueBallAngle = 22.5f;
        [Export] public int AmmoPerShot = 1;
        [Export] private PackedScene _blueBallPrefab;

        private RayCast2D _kickRaycast;
        private Area2D _meleeArea;
        private Player _player;
        private AnimationController _animationController;
        private float _timeSinceBall;

        public override void _Ready()
        {
            _player = GetParent<Player>();
            _animationController = _player?.GetNodeOrNull<AnimationController>("AnimationController");
            _kickRaycast = GetNode<RayCast2D>("KickRaycast");
            _meleeArea = GetNode<Area2D>("MeleeArea");
        }

        public override void _Process(double delta)
        {
            Vector2 relativeMousePos = GetGlobalMousePosition() - GlobalPosition;
            Rotation = Mathf.Atan2(relativeMousePos.Y, relativeMousePos.X);
            UpdateMeleeArea();

            if (Input.IsActionJustPressed("melee"))
            {
                _animationController?.PlayOneShot(AnimationStates.AttackMelee);
                Kick();
            }

            if (Input.IsActionPressed("shoot") && CanSpawnBall())
            {
                _animationController?.PlayOneShot(AnimationStates.AttackRanged);
                TrySpawnBall(Rotation);
            }

            if (Input.IsActionJustPressed("shoot_forward") && CanSpawnBall())
            {
                _animationController?.PlayOneShot(AnimationStates.AttackRanged);
                float facingRotation = _player == null || _player.FacingDirection >= 0.0f ? 0.0f : MathF.PI;
                TrySpawnBall(facingRotation);
            }

            _timeSinceBall += (float)delta;
        }

        private void UpdateMeleeArea()
        {
            if (_meleeArea == null)
            {
                return;
            }

            float facingDirection = _player == null ? 1.0f : _player.FacingDirection;
            _meleeArea.Position = new Vector2(
                MeleeAreaOffset * facingDirection,
                _player != null && _player.IsDucking ? 10.0f : 0.0f
            );
        }

        private void Kick()
        {
            if (_meleeArea != null)
            {
                foreach (Node2D bodyNode in _meleeArea.GetOverlappingBodies())
                {
                    if (bodyNode is EnemyBody2D closeEnemy)
                    {
                        closeEnemy.Hurt(KickDamage);
                        return;
                    }
                }
            }

            _kickRaycast.ForceRaycastUpdate();
            if (_kickRaycast.GetCollider() is EnemyBody2D body)
            {
                body.Hurt(KickDamage);
            }
        }

        private void SpawnBall(float rotation)
        {
            if (_blueBallPrefab.Instantiate() is not BlueBall blueBall)
            {
                return;
            }

            _timeSinceBall = 0.0f;
            blueBall.Rotation = rotation + float.DegreesToRadians((float)GD.RandRange(-BlueBallAngle, BlueBallAngle));
            blueBall.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene?.AddChild(blueBall);
        }

        private void TrySpawnBall(float rotation)
        {
            if (_blueBallPrefab == null)
            {
                return;
            }

            if (_player != null && !_player.TryConsumeAmmo(AmmoPerShot))
            {
                return;
            }

            SpawnBall(rotation);
        }

        private bool CanSpawnBall()
        {
            return GameplayRules.CanUseCooldown(_timeSinceBall, BlueBallRate);
        }
    }
}
