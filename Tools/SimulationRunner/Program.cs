using System;
using Insanity.Scripts.Shared;

internal static class Program
{
    private static int Main(string[] args)
    {
        bool selfTest = Array.Exists(args, arg => arg == "--self-test");
        return selfTest ? RunSelfTest() : RunSimulation();
    }

    private static int RunSelfTest()
    {
        AssertEqual(450.0f, GameplayRules.ResolveMoveSpeed(300.0f, 1.5f, true), "Sprint multiplier");
        AssertEqual(300.0f, GameplayRules.ResolveHorizontalVelocity(0.0f, 1.0f, 300.0f, 1.5f, false), "Move right");
        AssertEqual(-1.0f, GameplayRules.ResolveFacingDirection(1.0f, -0.4f), "Facing left");

        EnemyConfig melee = new(EnemyKind.Melee, 40, 8, 110.0f, 360.0f, 28.0f, 42.0f, 0.9f, 0.0f, 0.0f, 0);
        EnemyStepResult meleeStep = GameplayRules.StepEnemy(melee, 30.0f, 0.0f, 1.2f);
        if (!meleeStep.ShouldAttack)
        {
            Console.Error.WriteLine("Self-test failed: melee enemy should attack inside range.");
            return 1;
        }

        EnemyConfig explosive = new(EnemyKind.Explosive, 18, 12, 140.0f, 400.0f, 12.0f, 26.0f, 0.1f, 0.0f, 26.0f, 20);
        EnemyStepResult explosiveStep = GameplayRules.StepEnemy(explosive, 10.0f, 0.0f, 1.0f);
        if (!explosiveStep.ShouldExplode)
        {
            Console.Error.WriteLine("Self-test failed: explosive enemy should detonate inside trigger range.");
            return 1;
        }

        int health = GameplayRules.ApplyDamage(40, 7);
        AssertEqual(33, health, "Damage application");

        Console.WriteLine("Simulation self-test passed.");
        return 0;
    }

    private static int RunSimulation()
    {
        float playerPosition = -520.0f;
        float velocityX = 0.0f;
        float delta = 1.0f / 60.0f;
        float facing = 1.0f;

        EnemyConfig ranged = new(EnemyKind.Ranged, 28, 4, 60.0f, 520.0f, 200.0f, 420.0f, 1.25f, 300.0f, 0.0f, 0);
        float enemyX = 430.0f;
        float cooldown = 99.0f;
        int shots = 0;

        for (int frame = 0; frame < 300; frame++)
        {
            bool sprinting = frame < 90;
            float inputX = frame < 180 ? 1.0f : 0.0f;
            velocityX = GameplayRules.ResolveHorizontalVelocity(velocityX, inputX, 300.0f, 1.5f, sprinting);
            facing = GameplayRules.ResolveFacingDirection(facing, inputX);
            playerPosition += velocityX * delta;

            cooldown += delta;
            EnemyStepResult step = GameplayRules.StepEnemy(ranged, enemyX, playerPosition, cooldown);
            enemyX += step.VelocityX * delta;
            if (step.ShouldAttack)
            {
                shots++;
                cooldown = 0.0f;
            }
        }

        Console.WriteLine($"player_x={playerPosition:F2}");
        Console.WriteLine($"enemy_x={enemyX:F2}");
        Console.WriteLine($"facing={(facing < 0.0f ? "left" : "right")}");
        Console.WriteLine($"enemy_shots={shots}");
        return 0;
    }

    private static void AssertEqual(float expected, float actual, string label)
    {
        if (MathF.Abs(expected - actual) > 0.001f)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }

    private static void AssertEqual(int expected, int actual, string label)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }
}
