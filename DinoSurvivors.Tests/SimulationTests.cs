using System.Numerics;
using DinoSurvivors.Core;
using Xunit;

namespace DinoSurvivors.Tests;

public class SimulationTests {
    [Fact]
    public void PlayerStartsAtCenterOfArena() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();

        // Act
        var sim = new Simulation(rng, persistence, content);

        // Assert
        var expectedCenter = new Vector2(1000, 1000);
        Assert.Equal(expectedCenter, sim.PlayerPosition);
    }

    [Fact]
    public void PlayerMovesHorizontalAndVertical() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Move Right
        var rightActions = new ControlActions(new Vector2(1, 0), Vector2.Zero);
        // Act
        sim.Step(rightActions, 1.0f);

        // Assert
        var expectedPosition = new Vector2(1000 + sim.PlayerSpeed, 1000);
        Assert.Equal(expectedPosition, sim.PlayerPosition);

        // Move Up/Down (Y-axis)
        var upActions = new ControlActions(new Vector2(0, -1), Vector2.Zero);
        sim.Step(upActions, 1.0f);

        expectedPosition += new Vector2(0, -sim.PlayerSpeed);
        Assert.Equal(expectedPosition, sim.PlayerPosition);
    }

    [Fact]
    public void PlayerDiagonalMovementIsNormalized() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Move Diagonally (1, 1)
        var diagonalActions = new ControlActions(new Vector2(1, 1), Vector2.Zero);
        
        // Act
        sim.Step(diagonalActions, 1.0f);

        // Assert
        var direction = Vector2.Normalize(new Vector2(1, 1));
        var expectedPosition = new Vector2(1000, 1000) + direction * sim.PlayerSpeed;
        
        Assert.Equal(expectedPosition.X, sim.PlayerPosition.X, 3);
        Assert.Equal(expectedPosition.Y, sim.PlayerPosition.Y, 3);
    }

    [Fact]
    public void PlayerIsClampedToArenaBoundaries() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Move Right way past the boundary
        var rightActions = new ControlActions(new Vector2(1, 0), Vector2.Zero);
        
        // Act
        sim.Step(rightActions, 100.0f);

        // Assert
        Assert.Equal(sim.ArenaSize.X, sim.PlayerPosition.X);
        Assert.Equal(1000, sim.PlayerPosition.Y);

        // Move Left way past the boundary
        var leftActions = new ControlActions(new Vector2(-1, 0), Vector2.Zero);
        sim.Step(leftActions, 100.0f);

        Assert.Equal(0, sim.PlayerPosition.X);
        Assert.Equal(1000, sim.PlayerPosition.Y);
    }

    [Fact]
    public void AutomaticEnemySpawningEnforcesConstraints() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act - step past the spawn interval of 1.5 seconds
        sim.Step(actions, 1.6f);

        // Assert
        Assert.Single(sim.Enemies);
        var enemy = sim.Enemies[0];

        // Check distance to player is at least MinSpawnRadius (450f)
        float distance = Vector2.Distance(enemy.Position, sim.PlayerPosition);
        Assert.True(distance >= 450f, $"Spawned enemy too close to player: {distance}px (expected >= 450px)");

        // Check within arena boundaries
        Assert.True(enemy.Position.X >= 0 && enemy.Position.X <= sim.ArenaSize.X);
        Assert.True(enemy.Position.Y >= 0 && enemy.Position.Y <= sim.ArenaSize.Y);
    }

    [Fact]
    public void EnemiesMoveTowardsPlayerDirectPursuit() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn a Compy at (1000, 1200) - 200 units south of player at (1000, 1000)
        sim.SpawnEnemyAt(new Vector2(1000, 1200));

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act - step for 1.0 seconds (below spawn threshold to avoid auto-spawning another enemy)
        sim.Step(actions, 1.0f);

        // Assert
        Assert.Single(sim.Enemies);
        var enemy = sim.Enemies[0];

        // Expected new position is (1000, 1200 - Speed * 1.0f) = (1000, 1080)
        var expectedPos = new Vector2(1000, 1080);
        Assert.Equal(expectedPos.X, enemy.Position.X, 3);
        Assert.Equal(expectedPos.Y, enemy.Position.Y, 3);
    }

    [Fact]
    public void EnemiesResolveSoftCollisionWithEachOther() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn two enemies close together (dist = 1)
        sim.SpawnEnemyAt(new Vector2(1000, 1200));
        sim.SpawnEnemyAt(new Vector2(1001, 1200));

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act - step with very small deltaTime
        sim.Step(actions, 0.001f);

        // Assert
        Assert.Equal(2, sim.Enemies.Count);
        var e1 = sim.Enemies[0];
        var e2 = sim.Enemies[1];

        // The distance between them should be resolved to at least their combined radius (12 + 12 = 24)
        float distance = Vector2.Distance(e1.Position, e2.Position);
        Assert.True(distance >= 24f, $"Enemies did not separate: distance was {distance} (expected >= 24)");
    }

    [Fact]
    public void PlayerCanPushThroughEnemiesSoftCollision() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn a Compy slightly to the right of the player, creating an overlap
        // Player is at (1000, 1000), PlayerRadius = 16f
        // Enemy is at (1010, 1000), EnemyRadius = 12f
        // Total radius = 28f, distance = 10f. Overlap = 18f.
        sim.SpawnEnemyAt(new Vector2(1010, 1000));

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act - step with deltaTime = 0 to run collision resolution only
        sim.Step(actions, 0f);

        // Assert
        // The distance between them should be resolved to their combined radius (16 + 12 = 28)
        var enemy = sim.Enemies[0];
        float distance = Vector2.Distance(sim.PlayerPosition, enemy.Position);
        Assert.Equal(28f, distance, 3);

        // With a 20% player push / 80% enemy push ratio:
        // Player (at 1000) is pushed left by 18 * 0.2 = 3.6f -> 996.4
        // Enemy (at 1010) is pushed right by 18 * 0.8 = 14.4f -> 1024.4
        Assert.Equal(996.4f, sim.PlayerPosition.X, 3);
        Assert.Equal(1024.4f, enemy.Position.X, 3);
    }

    [Fact]
    public void EnemiesAreClampedToArenaBoundaries() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Move player to (20, 1000)
        var leftActions = new ControlActions(new Vector2(-1, 0), Vector2.Zero);
        sim.Step(leftActions, 4.9f); // Player moves 200f/s * 4.9s = 980 units left -> player ends up at 20, 1000.
        Assert.Equal(20f, sim.PlayerPosition.X);

        // Spawn a Compy at (8, 1000) - to the left of the player.
        // Player at 20, enemy at 8. Distance = 12. Overlap = 16f.
        sim.SpawnEnemyAt(new Vector2(8, 1000));

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act - step with deltaTime = 0 to trigger soft collision push
        sim.Step(actions, 0f);

        // Assert
        // Without clamping, the enemy's X position would be: 8 - 16 * 0.8 = -4.8.
        // With clamping, the enemy's X position must be exactly 0.
        var enemy = sim.Enemies[0];
        Assert.Equal(0f, enemy.Position.X);
    }

    [Fact]
    public void PlayerStartsWithMaxHpAndIsNotRunLost() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();

        // Act
        var sim = new Simulation(rng, persistence, content);

        // Assert
        Assert.Equal(100f, sim.PlayerMaxHp);
        Assert.Equal(100f, sim.PlayerCurrentHp);
        Assert.False(sim.IsRunLost);
    }

    [Fact]
    public void EnemyContactDealsDamageAndFiresEvent() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn enemy right at player's position (1000, 1000)
        sim.SpawnEnemyAt(new Vector2(1000, 1000));

        bool eventFired = false;
        sim.OnPlayerDamaged += () => eventFired = true;

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act
        sim.Step(actions, 0.1f);

        // Assert
        Assert.Equal(90f, sim.PlayerCurrentHp);
        Assert.True(eventFired);
        Assert.Equal(1.0f, sim.Enemies[0].ContactCooldownTimer);
    }

    [Fact]
    public void EnemyContactDamageIsCooldownGated() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn enemy right at player's position (1000, 1000)
        sim.SpawnEnemyAt(new Vector2(1000, 1000));

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act & Assert:
        // 1st step: deals damage
        sim.Step(actions, 0.5f);
        Assert.Equal(90f, sim.PlayerCurrentHp);
        Assert.Equal(1.0f, sim.Enemies[0].ContactCooldownTimer);

        // Reposition enemy on top of player to ensure they stay in contact
        sim.Enemies[0].Position = sim.PlayerPosition;

        // 2nd step: 0.4s later. Cooldown timer becomes 1.0 - 0.4 = 0.6. No new damage.
        sim.Step(actions, 0.4f);
        Assert.Equal(90f, sim.PlayerCurrentHp);
        Assert.Equal(0.6f, sim.Enemies[0].ContactCooldownTimer, 3);

        // Reposition enemy on top of player to ensure they stay in contact
        sim.Enemies[0].Position = sim.PlayerPosition;

        // 3rd step: 0.6s later. Cooldown timer decremented by 0.6 to 0, which triggers damage again and resets.
        sim.Step(actions, 0.6f);
        Assert.Equal(80f, sim.PlayerCurrentHp);
        Assert.Equal(1.0f, sim.Enemies[0].ContactCooldownTimer);
    }

    [Fact]
    public void MultipleEnemiesDealDamageIndependently() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn two enemies on top of the player
        sim.SpawnEnemyAt(new Vector2(1000, 1000));
        sim.SpawnEnemyAt(new Vector2(1000, 1000));

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act
        sim.Step(actions, 0.1f);

        // Assert: both enemies should have dealt 10 damage, so player HP is 80.
        Assert.Equal(80f, sim.PlayerCurrentHp);
        Assert.Equal(1.0f, sim.Enemies[0].ContactCooldownTimer);
        Assert.Equal(1.0f, sim.Enemies[1].ContactCooldownTimer);
    }

    [Fact]
    public void PlayerHpReachesZeroTransitionsToRunLost() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn 10 enemies on top of the player to deal 10 * 10 = 100 damage
        for (int i = 0; i < 10; i++) {
            sim.SpawnEnemyAt(new Vector2(1000, 1000));
        }

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act
        sim.Step(actions, 0.1f);

        // Assert
        Assert.Equal(0f, sim.PlayerCurrentHp);
        Assert.True(sim.IsRunLost);
    }

    [Fact]
    public void NoPlayHappensAfterRunIsLost() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn two enemies
        sim.SpawnEnemyAt(new Vector2(1000, 1000));
        sim.SpawnEnemyAt(new Vector2(1000, 1000));

        var actions = new ControlActions(new Vector2(1, 0), Vector2.Zero);

        // Step multiple times, resetting positions and cooldowns to ensure they hit every step
        for (int i = 0; i < 5; i++) {
            foreach (var enemy in sim.Enemies) {
                enemy.Position = sim.PlayerPosition;
                enemy.ContactCooldownTimer = 0f;
            }
            sim.Step(actions, 0.1f);
        }

        Assert.True(sim.IsRunLost);
        var deathPos = sim.PlayerPosition;

        var initialEnemyPositions = new List<Vector2>();
        foreach (var enemy in sim.Enemies) {
            initialEnemyPositions.Add(enemy.Position);
        }

        // Act - step again
        sim.Step(actions, 1.0f);

        // Assert
        Assert.Equal(deathPos, sim.PlayerPosition);
        for (int i = 0; i < sim.Enemies.Count; i++) {
            Assert.Equal(initialEnemyPositions[i], sim.Enemies[i].Position);
        }
    }

    [Fact]
    public void TranqPistolFiresOnCooldown() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        var aimRight = new ControlActions(Vector2.Zero, new Vector2(1, 0));
        var aimZero = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act & Assert:
        // 1. Initial fire happens immediately when there is a valid aim direction
        sim.Step(aimRight, 0f);
        Assert.Single(sim.Projectiles);
        var p1 = sim.Projectiles[0];
        Assert.Equal(sim.PlayerPosition, p1.Position);
        Assert.Equal(new Vector2(1, 0), p1.Direction);

        // Clear projectiles to easily count the next spawns
        sim.Projectiles.Clear();

        // 2. Step by 0.4 seconds (less than 0.8s cooldown) - should not fire again
        sim.Step(aimRight, 0.4f);
        Assert.Empty(sim.Projectiles);

        // 3. Step by another 0.4 seconds (total 0.8s cooldown reached/exceeded) - should fire again
        sim.Step(aimRight, 0.4f);
        Assert.Single(sim.Projectiles);
        var p2 = sim.Projectiles[0];
        Assert.Equal(sim.PlayerPosition + new Vector2(1, 0) * p2.Speed * 0.4f, p2.Position);

        sim.Projectiles.Clear();

        // 4. If aim direction is zero, it should not fire even after cooldown
        sim.Step(aimZero, 1.0f);
        Assert.Empty(sim.Projectiles);
    }

    [Fact]
    public void ProjectilesMoveInAimDirectionAtExpectedSpeed() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        var aimUp = new ControlActions(Vector2.Zero, new Vector2(0, -1));

        // Act
        sim.Step(aimUp, 0.1f); // Fires projectile pointing up at player position (1000, 1000)
        Assert.Single(sim.Projectiles);
        var projectile = sim.Projectiles[0];
        var initialPos = projectile.Position;

        // Move simulation forward by 0.5s - projectile speed is 600f, so it should move up by 300f
        var aimZero = new ControlActions(Vector2.Zero, Vector2.Zero);
        sim.Step(aimZero, 0.5f);

        // Assert
        var expectedPos = initialPos + new Vector2(0, -1) * projectile.Speed * 0.5f;
        Assert.Equal(expectedPos.X, projectile.Position.X, 3);
        Assert.Equal(expectedPos.Y, projectile.Position.Y, 3);
    }

    [Fact]
    public void ProjectileCollisionDealsDamageAndRemovesProjectile() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Spawn enemy at (1000, 1100)
        sim.SpawnEnemyAt(new Vector2(1000, 1100));
        var enemy = sim.Enemies[0];
        enemy.Hp = 20f;

        // Manually place overlapping projectile at (1000, 1095)
        var proj = new Projectile(new Vector2(1000, 1095), new Vector2(0, 1));
        sim.Projectiles.Add(proj);

        bool hitEventFired = false;
        Vector2 hitPos = Vector2.Zero;
        float hitDmg = 0f;
        sim.OnEnemyHit += (pos, dmg) => {
            hitEventFired = true;
            hitPos = pos;
            hitDmg = dmg;
        };

        var aimZero = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act
        sim.Step(aimZero, 0f);

        // Assert
        Assert.Empty(sim.Projectiles);
        Assert.Equal(10f, enemy.Hp);
        Assert.Equal(0.15f, enemy.HitFlashTimer);
        Assert.True(hitEventFired);
        Assert.Equal(new Vector2(1000, 1095), hitPos);
        Assert.Equal(10f, hitDmg);
    }

    [Fact]
    public void EnemyDiesAtZeroHpAndTriggersKilledEvent() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        sim.SpawnEnemyAt(new Vector2(1000, 1100));
        var enemy = sim.Enemies[0];
        enemy.Hp = 10f; // 1 shot away from death

        var proj = new Projectile(new Vector2(1000, 1095), new Vector2(0, 1));
        sim.Projectiles.Add(proj);

        bool killedEventFired = false;
        Vector2 killedPos = Vector2.Zero;
        sim.OnEnemyKilled += (pos) => {
            killedEventFired = true;
            killedPos = pos;
        };

        var aimZero = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act
        sim.Step(aimZero, 0f);

        // Assert
        Assert.Empty(sim.Enemies);
        Assert.Empty(sim.Projectiles);
        Assert.True(killedEventFired);
        Assert.Equal(new Vector2(1000, 1100), killedPos);
    }

    [Fact]
    public void ProjectilesOutOfBoundsAreRemoved() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Manually place projectile pointing right, near right edge (X=2000)
        var projRight = new Projectile(new Vector2(1999, 1000), new Vector2(1, 0));
        sim.Projectiles.Add(projRight);

        // Manually place projectile pointing left, near left edge (X=0)
        var projLeft = new Projectile(new Vector2(1, 1000), new Vector2(-1, 0));
        sim.Projectiles.Add(projLeft);

        var aimZero = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act - step 0.01s (speed = 600f, movement is 6px, taking them out of bounds)
        sim.Step(aimZero, 0.01f);

        // Assert
        Assert.Empty(sim.Projectiles);
    }

    [Fact]
    public void EnemyKilledDropsXpGemAtItsPosition() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        sim.SpawnEnemyAt(new Vector2(1000, 1100));
        var enemy = sim.Enemies[0];
        enemy.Hp = 10f; // 1 shot away from death

        var proj = new Projectile(new Vector2(1000, 1095), new Vector2(0, 1));
        sim.Projectiles.Add(proj);

        // Act
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);

        // Assert
        Assert.Empty(sim.Enemies);
        Assert.Single(sim.XpGems);
        Assert.Equal(new Vector2(1000, 1100), sim.XpGems[0].Position);
    }

    [Fact]
    public void PlayerWithinPickupRadiusCollectsXpGem() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Drop a gem close to the player (within default pickup radius of 45)
        // Player is at (1000, 1000)
        var gemPos = new Vector2(1000, 1030); // 30 units away
        var gem = new XpGem(gemPos, 10f);
        sim.XpGems.Add(gem);

        // Act - step simulation
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        // Assert
        Assert.Empty(sim.XpGems);
        Assert.Equal(10f, sim.PlayerXp);
    }

    [Fact]
    public void PlayerOutsidePickupRadiusDoesNotCollectXpGem() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Drop a gem outside the default pickup radius of 45
        // Player is at (1000, 1000)
        var gemPos = new Vector2(1000, 1050); // 50 units away
        var gem = new XpGem(gemPos, 10f);
        sim.XpGems.Add(gem);

        // Act - step simulation
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        // Assert
        Assert.Single(sim.XpGems);
        Assert.Equal(0f, sim.PlayerXp);
    }

    [Fact]
    public void PlayerAccumulatesXpAndLevelsUp() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Default XP to next level should be 100f (100 * Level)
        // Drop 11 gems of value 10 each (total 110 XP)
        for (int i = 0; i < 11; i++) {
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        }

        // Act
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        // Assert
        Assert.Empty(sim.XpGems);
        Assert.Equal(2, sim.PlayerLevel);
        Assert.Equal(10f, sim.PlayerXp); // 110 - 100 = 10 XP carried over
    }

    [Fact]
    public void UncollectedGemsPersistOnGround() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        var gemPos = new Vector2(1000, 1200); // far away
        sim.XpGems.Add(new XpGem(gemPos, 10f));

        // Act - run simulation step multiple times
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1.0f);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1.0f);

        // Assert
        Assert.Single(sim.XpGems);
        Assert.Equal(0f, sim.PlayerXp);
    }

    [Fact]
    public void PlayerStartsWithTranqPistolLevel1() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();

        // Act
        var sim = new Simulation(rng, persistence, content);

        // Assert
        Assert.Single(sim.EquippedWeapons);
        var weapon = sim.EquippedWeapons[0];
        Assert.Equal("TranqPistol", weapon.Definition.Id);
        Assert.Equal(1, weapon.Level);
    }

    [Fact]
    public void TryAddOrUpgradeWeaponManagesInventoryCorrectly() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Act & Assert
        // Slot cap testing:
        Assert.True(sim.TryAddOrUpgradeWeapon("FlareGun"));  // 2nd weapon
        Assert.True(sim.TryAddOrUpgradeWeapon("BugZapper"));  // 3rd weapon
        Assert.False(sim.TryAddOrUpgradeWeapon("Laser"));    // 4th weapon should fail (slot cap = 3)
        Assert.Equal(3, sim.EquippedWeapons.Count);

        // Level cap testing:
        var pistol = sim.EquippedWeapons[0];
        Assert.Equal("TranqPistol", pistol.Definition.Id);
        Assert.Equal(1, pistol.Level);

        Assert.True(sim.TryAddOrUpgradeWeapon("TranqPistol")); // Lvl 2
        Assert.Equal(2, pistol.Level);
        Assert.True(sim.TryAddOrUpgradeWeapon("TranqPistol")); // Lvl 3
        Assert.True(sim.TryAddOrUpgradeWeapon("TranqPistol")); // Lvl 4
        Assert.True(sim.TryAddOrUpgradeWeapon("TranqPistol")); // Lvl 5
        Assert.Equal(5, pistol.Level);

        Assert.False(sim.TryAddOrUpgradeWeapon("TranqPistol")); // Lvl 6 should fail (max level = 5)
        Assert.Equal(5, pistol.Level);
    }

    [Fact]
    public void AimedWeaponFlareGunPierceBehavior() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        // Clear default starting weapon so we can focus only on FlareGun
        sim.EquippedWeapons.Clear();
        sim.TryAddOrUpgradeWeapon("FlareGun");

        // Set player position at center (1000, 1000)
        // Spawn 3 enemies in a line along X-axis:
        // Enemy 1 at (1050, 1000), Enemy 2 at (1100, 1000), Enemy 3 at (1150, 1000)
        sim.SpawnEnemyAt(new Vector2(1050, 1000));
        sim.SpawnEnemyAt(new Vector2(1100, 1000));
        sim.SpawnEnemyAt(new Vector2(1150, 1000));

        foreach (var enemy in sim.Enemies) {
            enemy.Hp = 50f;
        }

        // Act & Assert 1: Flare Gun fires aimed shot in +X direction
        var aimRight = new ControlActions(Vector2.Zero, new Vector2(1, 0));
        sim.Step(aimRight, 0f);

        Assert.Single(sim.Projectiles);
        var projectile = sim.Projectiles[0];
        Assert.Equal(500f, projectile.Speed);
        Assert.Equal(10f, projectile.Radius);
        Assert.Equal(20f, projectile.Damage);
        Assert.Equal(3, projectile.PierceCount);

        // Move projectile to hit Enemy 1 (1050, 1000)
        projectile.Position = new Vector2(1050, 1000);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);

        // Enemy 1 hit, pierce count should decrement to 2
        Assert.Equal(30f, sim.Enemies[0].Hp); // 50 - 20 = 30
        Assert.Equal(2, projectile.PierceCount);
        Assert.Single(sim.Projectiles); // Still alive because pierce count > 0

        // Step again with projectile at the same position. It should NOT hit Enemy 1 again.
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);
        Assert.Equal(30f, sim.Enemies[0].Hp); // Still 30
        Assert.Equal(2, projectile.PierceCount); // Still 2

        // Move to Enemy 2 (1100, 1000)
        projectile.Position = new Vector2(1100, 1000);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);
        Assert.Equal(30f, sim.Enemies[1].Hp); // 50 - 20 = 30
        Assert.Equal(1, projectile.PierceCount);
        Assert.Single(sim.Projectiles); // Still alive

        // Move to Enemy 3 (1150, 1000)
        projectile.Position = new Vector2(1150, 1000);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);
        Assert.Equal(30f, sim.Enemies[2].Hp); // 50 - 20 = 30
        // Pierce count reaches 0, so projectile should be removed
        Assert.Empty(sim.Projectiles);
    }

    [Fact]
    public void AutonomousWeaponBugZapperDamageBehavior() {
        // Arrange
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var content = new StubContentProvider();
        var sim = new Simulation(rng, persistence, content);

        sim.EquippedWeapons.Clear();
        sim.TryAddOrUpgradeWeapon("BugZapper");

        // Player is at (1000, 1000)
        // Spawn Enemy 1 inside Bug Zapper radius (120f): (1050, 1000) -> dist = 50
        sim.SpawnEnemyAt(new Vector2(1050, 1000));
        sim.Enemies[0].Hp = 30f;

        // Spawn Enemy 2 outside Bug Zapper radius: (1150, 1000) -> dist = 150
        sim.SpawnEnemyAt(new Vector2(1150, 1000));
        sim.Enemies[1].Hp = 30f;

        var actions = new ControlActions(Vector2.Zero, Vector2.Zero);

        // Act & Assert 1: Step. Bug Zapper triggers on first step because timer <= 0
        sim.Step(actions, 0.1f);

        // Enemy 1 inside radius should be damaged by 15f. Hp: 30 -> 15.
        // Enemy 2 outside radius should not be damaged. Hp: 30 -> 30.
        Assert.Equal(15f, sim.Enemies[0].Hp);
        Assert.Equal(30f, sim.Enemies[1].Hp);

        // Cooldown timer of Bug Zapper should be set to 2.0f.
        var zapper = sim.EquippedWeapons[0];
        Assert.Equal(2.0f, zapper.CooldownTimer);

        // Step again by 1.0s. Bug Zapper cooldown decreases to 1.0s, does not trigger.
        sim.Step(actions, 1.0f);
        Assert.Equal(15f, sim.Enemies[0].Hp); // unchanged
        Assert.Equal(1.0f, zapper.CooldownTimer);

        bool killedEventFired = false;
        sim.OnEnemyKilled += (pos) => {
            killedEventFired = true;
        };

        // Step by another 1.1s (cooldown is exceeded). Bug Zapper triggers again!
        sim.Step(actions, 1.1f);
        
        Assert.True(killedEventFired);
        // Enemy 1 should be killed. Enemy 2 should have taken 15 damage and have 15 HP left.
        Assert.Contains(sim.Enemies, e => e.Hp == 15f);
        Assert.DoesNotContain(sim.Enemies, e => e.Hp == 30f);
    }
}




