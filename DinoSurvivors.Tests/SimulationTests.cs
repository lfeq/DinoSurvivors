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

        // Default XP to next level: 100f (100 * Level).
        // Drop 11 gems of value 10 each (total 110 XP) to cross the threshold.
        for (int i = 0; i < 11; i++) {
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        }

        // Act
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        // Assert — sim is paused waiting for player to pick a level-up option
        Assert.Empty(sim.XpGems);
        Assert.True(sim.IsPausedForLevelUp);
        Assert.Equal(3, sim.PendingLevelUpOptions.Count);
        Assert.Equal(1, sim.PlayerLevel); // not yet incremented

        // Selecting an option applies the level-up and rolls XP over
        sim.SelectLevelUpOption(0);

        Assert.False(sim.IsPausedForLevelUp);
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

    // --- Issue #8: Passive Items + Player Stat Composition ---

    [Fact]
    public void SouvenirMagnet_Level1_IncreasesPickupRadius() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        var baseRadius = sim.PlayerPickupRadius;

        sim.TryAddOrUpgradePassive("SouvenirMagnet");

        Assert.True(sim.PlayerEffectivePickupRadius > baseRadius);
        Assert.Equal(baseRadius * 1.15f, sim.PlayerEffectivePickupRadius, 3);
    }

    [Fact]
    public void PassiveSlotCapIsThree() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        Assert.True(sim.TryAddOrUpgradePassive("SouvenirMagnet"));
        Assert.True(sim.TryAddOrUpgradePassive("RunningShoes"));
        Assert.True(sim.TryAddOrUpgradePassive("FirstAidFannyPack"));
        Assert.False(sim.TryAddOrUpgradePassive("FoamDinoClaw")); // 4th passive: slot cap
        Assert.Equal(3, sim.EquippedPassives.Count);
    }

    [Fact]
    public void PassiveLevelCapIsThree() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        Assert.True(sim.TryAddOrUpgradePassive("SouvenirMagnet"));  // Level 1
        Assert.True(sim.TryAddOrUpgradePassive("SouvenirMagnet"));  // Level 2
        Assert.True(sim.TryAddOrUpgradePassive("SouvenirMagnet"));  // Level 3
        Assert.False(sim.TryAddOrUpgradePassive("SouvenirMagnet")); // Level 4: cap
        Assert.Equal(3, sim.EquippedPassives[0].Level);
    }

    [Fact]
    public void RunningShoes_IncreasesEffectiveSpeed() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.TryAddOrUpgradePassive("RunningShoes");

        var startX = sim.PlayerPosition.X;
        sim.Step(new ControlActions(new Vector2(1, 0), Vector2.Zero), 0.1f);

        // With RunningShoes L1 (1.15x): 200 * 1.15 * 0.1 = 23f
        Assert.Equal(startX + 200f * 1.15f * 0.1f, sim.PlayerPosition.X, 2);
    }

    [Fact]
    public void FoamDinoClaw_IncreasesEffectiveDamage() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.TryAddOrUpgradePassive("FoamDinoClaw");

        sim.SpawnEnemyAt(new Vector2(1100, 1000));
        sim.Enemies[0].Hp = 100f;

        // Let sim fire the projectile so damage multiplier is baked in at spawn
        sim.Step(new ControlActions(Vector2.Zero, new Vector2(1, 0)), 0f);
        Assert.Single(sim.Projectiles);

        // Move projectile onto the enemy and resolve collision
        sim.Projectiles[0].Position = new Vector2(1100, 1000);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);

        // TranqPistol base damage 10f × 1.15 (FoamDinoClaw L1) = 11.5
        Assert.Equal(88.5f, sim.Enemies[0].Hp, 2);
    }

    [Fact]
    public void EnergyDrink_DecreasesEffectiveCooldown() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.TryAddOrUpgradePassive("EnergyDrink");

        var aim = new ControlActions(Vector2.Zero, new Vector2(1, 0));

        // Fire initial shot (cooldown timer = 0 initially so it fires immediately)
        sim.Step(aim, 0f);
        sim.Projectiles.Clear();

        // TranqPistol base cooldown is 0.8s; with EnergyDrink L1 (0.85x) effective cooldown = 0.68s.
        // Step 0.7s: base cooldown not yet reached, but effective cooldown (0.68s) IS reached → fires.
        sim.Step(aim, 0.7f);
        Assert.Single(sim.Projectiles);
    }

    [Fact]
    public void FirstAidFannyPack_IncreasesEffectiveMaxHp() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        var baseMaxHp = sim.PlayerMaxHp; // 100f

        sim.TryAddOrUpgradePassive("FirstAidFannyPack");

        // PlayerMaxHp stays at base; PlayerEffectiveMaxHp applies the multiplier
        Assert.Equal(100f, sim.PlayerMaxHp);
        Assert.Equal(baseMaxHp * 1.15f, sim.PlayerEffectiveMaxHp, 3);
    }

    [Fact]
    public void CombinedPassiveStatComposition() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        sim.TryAddOrUpgradePassive("SouvenirMagnet");
        sim.TryAddOrUpgradePassive("RunningShoes");
        sim.TryAddOrUpgradePassive("FirstAidFannyPack");

        Assert.Equal(45f * 1.15f, sim.PlayerEffectivePickupRadius, 3);
        Assert.Equal(200f * 1.15f, sim.PlayerEffectiveSpeed, 3);
        Assert.Equal(100f * 1.15f, sim.PlayerEffectiveMaxHp, 3);
    }

    // --- Issue #9: Level Up Choice ---

    [Fact]
    public void CrossingXpThresholdPausesSimWithThreeOptions() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // 10 gems × 10 XP = 100 XP; threshold at level 1 is 100 * 1 = 100
        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        Assert.Equal(3, sim.PendingLevelUpOptions.Count);
        Assert.Equal(1, sim.PlayerLevel); // not yet incremented — player must choose
    }

    [Fact]
    public void SimDoesNotStepWhilePausedForLevelUp() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // Trigger pause
        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);
        Assert.True(sim.IsPausedForLevelUp);

        sim.SpawnEnemyAt(new Vector2(1200, 1000));
        var enemyPosBefore = sim.Enemies[0].Position;
        var playerPosBefore = sim.PlayerPosition;

        // Step with movement input while paused — nothing should move
        sim.Step(new ControlActions(new Vector2(1, 0), Vector2.Zero), 1.0f);

        Assert.Equal(enemyPosBefore, sim.Enemies[0].Position);
        Assert.Equal(playerPosBefore, sim.PlayerPosition);
    }

    [Fact]
    public void SelectLevelUpOption_ApplyingAnyOption_LevelsUpAndUnpauses() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        int levelBefore = sim.PlayerLevel;

        sim.SelectLevelUpOption(0);

        Assert.False(sim.IsPausedForLevelUp);
        Assert.Equal(levelBefore + 1, sim.PlayerLevel);
        Assert.Empty(sim.PendingLevelUpOptions);
    }

    [Fact]
    public void SelectLevelUpOption_NewWeapon_AddsWeaponToInventory() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        int idx = sim.PendingLevelUpOptions.FindIndex(o => o.Type == UpgradeType.NewWeapon);
        Assert.NotEqual(-1, idx);

        string weaponId = sim.PendingLevelUpOptions[idx].ItemId!;
        int countBefore = sim.EquippedWeapons.Count;

        sim.SelectLevelUpOption(idx);

        Assert.Equal(countBefore + 1, sim.EquippedWeapons.Count);
        Assert.Contains(sim.EquippedWeapons, w => w.Definition.Id == weaponId);
    }

    [Fact]
    public void SelectLevelUpOption_PassiveUpgrade_IncreasesPassiveLevel() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // Fill all weapon slots with max-level weapons → no weapon options in pool
        sim.TryAddOrUpgradeWeapon("FlareGun");
        sim.TryAddOrUpgradeWeapon("BugZapper");
        sim.EquippedWeapons.ForEach(w => w.Level = 5);

        // Fill 2 passive slots with max-level passives; SouvenirMagnet at L1 → only upgrade available
        sim.TryAddOrUpgradePassive("FirstAidFannyPack");
        sim.EquippedPassives[0].Level = 3;
        sim.TryAddOrUpgradePassive("FoamDinoClaw");
        sim.EquippedPassives[1].Level = 3;
        sim.TryAddOrUpgradePassive("SouvenirMagnet"); // L1, upgradeable
        var sm = sim.EquippedPassives[2];
        Assert.Equal(1, sm.Level);

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        // Only SouvenirMagnet upgrade is eligible; it must be the first option
        Assert.Equal(UpgradeType.PassiveUpgrade, sim.PendingLevelUpOptions[0].Type);
        Assert.Equal("SouvenirMagnet", sim.PendingLevelUpOptions[0].ItemId);

        sim.SelectLevelUpOption(0);

        Assert.False(sim.IsPausedForLevelUp);
        Assert.Equal(2, sm.Level);
    }

    [Fact]
    public void FullWeaponSlots_ExcludeNewWeaponOptions() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        sim.TryAddOrUpgradeWeapon("FlareGun");
        sim.TryAddOrUpgradeWeapon("BugZapper"); // 3 slots full

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        Assert.DoesNotContain(sim.PendingLevelUpOptions, o => o.Type == UpgradeType.NewWeapon);
    }

    [Fact]
    public void FullPassiveSlots_ExcludeNewPassiveOptions() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        sim.TryAddOrUpgradePassive("SouvenirMagnet");
        sim.TryAddOrUpgradePassive("RunningShoes");
        sim.TryAddOrUpgradePassive("EnergyDrink"); // 3 slots full

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        Assert.DoesNotContain(sim.PendingLevelUpOptions, o => o.Type == UpgradeType.NewPassive);
    }

    [Fact]
    public void CategoryBalancing_WhenAllPicksAreWeapons_SwapsOneForPassive() {
        // StubRng.NextValue = 0 always picks index 0 from the remaining pool.
        // Pool with all 3 weapon slots full (upgrades only) + 5 passive options:
        //   [TP-Upgrade, FG-Upgrade, BZ-Upgrade, P1-New, P2-New, P3-New, P4-New, P5-New]
        // Picking index 0 three times yields [TP-Upgrade, FG-Upgrade, BZ-Upgrade] — all weapons.
        // Category balancing must swap one for the first available passive.
        var rng = new StubRng { NextValue = 0 };
        var sim = new Simulation(rng, new StubPersistence(), new StubContentProvider());

        sim.TryAddOrUpgradeWeapon("FlareGun");
        sim.TryAddOrUpgradeWeapon("BugZapper"); // 3 slots full, all at L1

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        bool hasPassive = sim.PendingLevelUpOptions.Any(o =>
            o.Type == UpgradeType.NewPassive || o.Type == UpgradeType.PassiveUpgrade);
        Assert.True(hasPassive, "Category balancing should have introduced at least one passive option");
    }

    [Fact]
    public void CashFallback_WhenNoEligibleUpgradesRemain_AllOptionsAreFallback() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // Max out all weapons
        sim.TryAddOrUpgradeWeapon("FlareGun");
        sim.TryAddOrUpgradeWeapon("BugZapper");
        sim.EquippedWeapons.ForEach(w => w.Level = 5);

        // Max out all passives
        sim.TryAddOrUpgradePassive("SouvenirMagnet");
        sim.TryAddOrUpgradePassive("RunningShoes");
        sim.TryAddOrUpgradePassive("EnergyDrink");
        sim.EquippedPassives.ForEach(p => p.Level = 3);

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsPausedForLevelUp);
        Assert.All(sim.PendingLevelUpOptions, o => Assert.Equal(UpgradeType.CashFallback, o.Type));
    }

    [Fact]
    public void SelectingCashFallback_GrantsCashNotHealing() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // Force all fallback options
        sim.TryAddOrUpgradeWeapon("FlareGun");
        sim.TryAddOrUpgradeWeapon("BugZapper");
        sim.EquippedWeapons.ForEach(w => w.Level = 5);
        sim.TryAddOrUpgradePassive("SouvenirMagnet");
        sim.TryAddOrUpgradePassive("RunningShoes");
        sim.TryAddOrUpgradePassive("EnergyDrink");
        sim.EquippedPassives.ForEach(p => p.Level = 3);

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        float hpBefore = sim.PlayerCurrentHp;
        Assert.Equal(0, sim.UnbankedJurassicCash);

        sim.SelectLevelUpOption(0);

        Assert.False(sim.IsPausedForLevelUp);
        Assert.Equal(25, sim.UnbankedJurassicCash);
        Assert.Equal(hpBefore, sim.PlayerCurrentHp); // no healing
    }

    // --- Issue #11: Jurassic Cash ---

    [Fact]
    public void EnemyKilledWithLowRng_DropsJurassicCash() {
        // NextDoubleValue = 0.1 < 0.20 drop threshold → cash should drop
        var rng = new StubRng { NextDoubleValue = 0.1 };
        var sim = new Simulation(rng, new StubPersistence(), new StubContentProvider());

        sim.SpawnEnemyAt(new Vector2(1000, 1100));
        sim.Enemies[0].Hp = 10f;

        var proj = new Projectile(new Vector2(1000, 1095), new Vector2(0, 1));
        sim.Projectiles.Add(proj);

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);

        Assert.Empty(sim.Enemies);
        Assert.Single(sim.JurassicCashDrops);
        Assert.Equal(new Vector2(1000, 1100), sim.JurassicCashDrops[0].Position);
    }

    [Fact]
    public void EnemyKilledWithHighRng_NoJurassicCashDrop() {
        // NextDoubleValue = 0.9 >= 0.20 threshold → no cash drop
        var rng = new StubRng { NextDoubleValue = 0.9 };
        var sim = new Simulation(rng, new StubPersistence(), new StubContentProvider());

        sim.SpawnEnemyAt(new Vector2(1000, 1100));
        sim.Enemies[0].Hp = 10f;

        var proj = new Projectile(new Vector2(1000, 1095), new Vector2(0, 1));
        sim.Projectiles.Add(proj);

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);

        Assert.Empty(sim.Enemies);
        Assert.Empty(sim.JurassicCashDrops);
    }

    [Fact]
    public void JurassicCashDropCollectedWithinPickupRadius_IncreasesUnbankedCash() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // Place a drop 30 units from player (within default pickup radius of 45)
        sim.JurassicCashDrops.Add(new JurassicCashDrop(new Vector2(1000, 1030), 5));

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.Empty(sim.JurassicCashDrops);
        Assert.Equal(5, sim.UnbankedJurassicCash);
    }

    [Fact]
    public void JurassicCashDropNotCollectedOutsidePickupRadius_PersistsOnGround() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // Place a drop 60 units from player (outside default pickup radius of 45)
        sim.JurassicCashDrops.Add(new JurassicCashDrop(new Vector2(1000, 1060), 5));

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.Single(sim.JurassicCashDrops);
        Assert.Equal(0, sim.UnbankedJurassicCash);
    }

    [Fact]
    public void DyingClearsUnbankedJurassicCash() {
        var rng = new StubRng { NextDoubleValue = 0.1 }; // low RNG to allow cash drops
        var sim = new Simulation(rng, new StubPersistence(), new StubContentProvider());

        // Kill an enemy to earn some cash (low RNG → drops), then collect it
        sim.SpawnEnemyAt(new Vector2(1000, 1100));
        sim.Enemies[0].Hp = 10f;
        var proj = new Projectile(new Vector2(1000, 1095), new Vector2(0, 1));
        sim.Projectiles.Add(proj);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);

        // Move the drop to player's feet and collect it
        sim.JurassicCashDrops[0] = new JurassicCashDrop(sim.PlayerPosition, 5);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);
        Assert.Equal(5, sim.UnbankedJurassicCash); // cash accumulated

        // Now kill the player: spawn 10 enemies on top
        for (int i = 0; i < 10; i++)
            sim.SpawnEnemyAt(sim.PlayerPosition);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.True(sim.IsRunLost);
        Assert.Equal(0, sim.UnbankedJurassicCash);
    }

    // --- Issue #13: Wave Schedule + Live Enemy Cap + Pickup Merging ---

    [Fact]
    public void Raptor_HasHigherSpeedDamageAndHpThanCompy() {
        var compy = new Enemy(Vector2.Zero, EnemyType.Compy);
        var raptor = new Enemy(Vector2.Zero, EnemyType.Raptor);
        Assert.True(raptor.Speed > compy.Speed);
        Assert.True(raptor.Damage > compy.Damage);
        Assert.True(raptor.Hp > compy.Hp);
    }

    [Fact]
    public void Triceratops_IsSlowerWithHigherHpAndDamage() {
        var compy = new Enemy(Vector2.Zero, EnemyType.Compy);
        var trice = new Enemy(Vector2.Zero, EnemyType.Triceratops);
        Assert.True(trice.Speed < compy.Speed);
        Assert.True(trice.Hp > compy.Hp);
        Assert.True(trice.Damage > compy.Damage);
    }

    [Fact]
    public void Stage1Early_AutoSpawn_ProducesCompy() {
        var rng = new StubRng { NextValue = 0, NextDoubleValue = 0.5 };
        var sim = new Simulation(rng, new StubPersistence(), new StubContentProvider());
        // StageTimeElapsed = 0: Stage 1 early phase — Compies only
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1.6f);
        Assert.Single(sim.Enemies);
        Assert.Equal(EnemyType.Compy, sim.Enemies[0].Type);
    }

    [Fact]
    public void Stage1Late_AutoSpawn_ProducesRaptor_WhenRngSelectsRaptorWeight() {
        // Stage 1 late phase: Compy weight=70, Raptor weight=30, total=100
        // NextValue=80 → roll 80 >= 70 cumulative Compy weight → picks Raptor
        var rng = new StubRng { NextValue = 80, NextDoubleValue = 0.5 };
        var sim = new Simulation(rng, new StubPersistence(), new StubContentProvider());
        sim.StageTimeElapsed = 8 * 60f; // 8 minutes = late phase
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1.6f);
        Assert.Single(sim.Enemies);
        Assert.Equal(EnemyType.Raptor, sim.Enemies[0].Type);
    }

    [Fact]
    public void LiveEnemyCap_PreventsSpawningBeyondCap() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        // Fill enemies to the stage-1 cap (80), spread out so no damage occurs
        for (int i = 0; i < sim.LiveEnemyCap; i++)
            sim.Enemies.Add(new Enemy(new Vector2(i * 30f, 0)));
        int countAtCap = sim.Enemies.Count;

        // Step past spawn interval — no new enemy should appear
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1.6f);

        Assert.Equal(countAtCap, sim.Enemies.Count);
    }

    [Fact]
    public void FiveOrMoreNearbyXpGems_MergeIntoOne() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        // Place 5 gems clustered within 30f of each other, far from player (no collection)
        for (int i = 0; i < 5; i++)
            sim.XpGems.Add(new XpGem(new Vector2(500 + i * 5f, 500), 10f));

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.Single(sim.XpGems);
        Assert.Equal(50f, sim.XpGems[0].XpValue);
    }

    [Fact]
    public void FiveOrMoreNearbyCashDrops_MergeIntoOne() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        for (int i = 0; i < 5; i++)
            sim.JurassicCashDrops.Add(new JurassicCashDrop(new Vector2(500 + i * 5f, 500), 5));

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.Single(sim.JurassicCashDrops);
        Assert.Equal(25, sim.JurassicCashDrops[0].CashValue);
    }

    [Fact]
    public void FourOrFewerNearbyXpGems_DoNotMerge() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        for (int i = 0; i < 4; i++)
            sim.XpGems.Add(new XpGem(new Vector2(500 + i * 5f, 500), 10f));

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);

        Assert.Equal(4, sim.XpGems.Count);
    }

    [Fact]
    public void XpToNextLevelRisesAfterEachLevelUp() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        Assert.Equal(100f, sim.XpToNextLevel); // Level 1

        for (int i = 0; i < 10; i++)
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);
        sim.SelectLevelUpOption(0);

        Assert.Equal(2, sim.PlayerLevel);
        Assert.Equal(200f, sim.XpToNextLevel); // Level 2: 100 * 2
    }

    // --- Issue #14: Stage lifecycle ---

    [Fact]
    public void StageExitRevealedAfterTenMinutes() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        Assert.False(sim.IsStageExitRevealed);

        sim.StageTimeElapsed = 599f;
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0f);
        Assert.False(sim.IsStageExitRevealed);

        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1f);
        Assert.True(sim.IsStageExitRevealed);
    }

    [Fact]
    public void SafehouseRevealedAtArenaBoundary() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.StageTimeElapsed = 599f;
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1f);

        Assert.True(sim.IsStageExitRevealed);
        var pos = sim.SafehousePosition;
        bool onBoundary = pos.X <= 0 || pos.X >= sim.ArenaSize.X ||
                          pos.Y <= 0 || pos.Y >= sim.ArenaSize.Y;
        Assert.True(onBoundary, $"Safehouse at {pos} is not on arena boundary");
        Assert.True(pos.X >= 0 && pos.X <= sim.ArenaSize.X);
        Assert.True(pos.Y >= 0 && pos.Y <= sim.ArenaSize.Y);
    }

    [Fact]
    public void EnemiesKeepSpawningAfterStageExitReveals() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.StageTimeElapsed = 599f;
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1.6f); // past reveal + spawn interval
        Assert.True(sim.IsStageExitRevealed);
        Assert.NotEmpty(sim.Enemies);
    }

    [Fact]
    public void ExitMarkerDirectionPointsTowardSafehouse() {
        // StubRng: NextValue=0 → edge 0 (top), NextDoubleValue=0.5 → t=0.5 → safehouse at (1000, 0)
        // Player at (1000, 1000). Direction = (0, -1).
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        Assert.Equal(Vector2.Zero, sim.ExitMarkerDirection);

        sim.StageTimeElapsed = 599f;
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1f);
        Assert.True(sim.IsStageExitRevealed);

        var dir = sim.ExitMarkerDirection;
        Assert.Equal(0f, dir.X, 3);
        Assert.Equal(-1f, dir.Y, 3);
    }

    // Helper: reveals stage exit and moves player into the safehouse zone.
    // With StubRng (NextValue=0, NextDoubleValue=0.5) safehouse is placed at (1000, 0) (top edge center).
    private static void RevealAndEnterSafehouse(Simulation sim) {
        sim.StageTimeElapsed = 599f;
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 1f);
        // Player at (1000, 1000), safehouse at (1000, 0). Move -Y by 5.1s.
        sim.Step(new ControlActions(new Vector2(0, -1), Vector2.Zero), 5.1f);
    }

    [Fact]
    public void EnteringSafehouseZoneConvertsCashBeforeBreak() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.JurassicCashDrops.Add(new JurassicCashDrop(sim.PlayerPosition, 50));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);
        Assert.Equal(50, sim.UnbankedJurassicCash);
        Assert.Equal(0, sim.BankedJurassicCash);

        RevealAndEnterSafehouse(sim);

        Assert.True(sim.IsPausedForSafehouseBreak);
        Assert.Equal(0, sim.UnbankedJurassicCash);
        Assert.Equal(50, sim.BankedJurassicCash);
    }

    [Fact]
    public void EnteringSafehouseZonePausesWithThreeRewardOptions() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        RevealAndEnterSafehouse(sim);

        Assert.True(sim.IsPausedForSafehouseBreak);
        Assert.Equal(3, sim.PendingSafehouseBreakOptions.Count);
    }

    [Fact]
    public void SafehouseBreakOffersAllThreeRewardTypes() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        RevealAndEnterSafehouse(sim);

        var types = sim.PendingSafehouseBreakOptions.Select(o => o.Type).ToList();
        Assert.Contains(SafehouseRewardType.PartialHeal, types);
        Assert.Contains(SafehouseRewardType.BankedCashBonus, types);
        Assert.Contains(SafehouseRewardType.BonusXp, types);
    }

    [Fact]
    public void SimDoesNotStepWhilePausedForSafehouseBreak() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        RevealAndEnterSafehouse(sim);
        Assert.True(sim.IsPausedForSafehouseBreak);

        sim.SpawnEnemyAt(new Vector2(1200, 1200));
        var enemyPosBefore = sim.Enemies[0].Position;
        var playerPosBefore = sim.PlayerPosition;

        sim.Step(new ControlActions(new Vector2(1, 0), Vector2.Zero), 1f);

        Assert.Equal(enemyPosBefore, sim.Enemies[0].Position);
        Assert.Equal(playerPosBefore, sim.PlayerPosition);
    }

    [Fact]
    public void SelectingPartialHealRestoresHpButNotFull() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        // Deal 40 HP damage (4 enemies × 10) so the 20 HP partial heal cannot fully restore
        for (int i = 0; i < 4; i++)
            sim.SpawnEnemyAt(sim.PlayerPosition);
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);
        float damagedHp = sim.PlayerCurrentHp; // 100 - 40 = 60 HP
        Assert.True(damagedHp < sim.PlayerMaxHp);
        sim.Enemies.Clear(); // remove so they don't interfere during safehouse movement

        RevealAndEnterSafehouse(sim);

        int idx = sim.PendingSafehouseBreakOptions.FindIndex(o => o.Type == SafehouseRewardType.PartialHeal);
        sim.SelectSafehouseRewardOption(idx);

        Assert.False(sim.IsPausedForSafehouseBreak);
        Assert.True(sim.PlayerCurrentHp > damagedHp);
        Assert.True(sim.PlayerCurrentHp < sim.PlayerEffectiveMaxHp, "Partial heal should not fully restore HP");
    }

    [Fact]
    public void SelectingBankedCashBonusIncreasesBankedCash() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        RevealAndEnterSafehouse(sim);
        int bankedBefore = sim.BankedJurassicCash;

        int idx = sim.PendingSafehouseBreakOptions.FindIndex(o => o.Type == SafehouseRewardType.BankedCashBonus);
        sim.SelectSafehouseRewardOption(idx);

        Assert.False(sim.IsPausedForSafehouseBreak);
        Assert.True(sim.BankedJurassicCash > bankedBefore);
    }

    [Fact]
    public void SelectingBonusXpAddsXpToPlayer() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        RevealAndEnterSafehouse(sim);
        float xpBefore = sim.PlayerXp;

        int idx = sim.PendingSafehouseBreakOptions.FindIndex(o => o.Type == SafehouseRewardType.BonusXp);
        sim.SelectSafehouseRewardOption(idx);

        Assert.False(sim.IsPausedForSafehouseBreak);
        Assert.True(sim.PlayerXp > xpBefore || sim.IsPausedForLevelUp);
    }

    [Fact]
    public void AfterSafehouseBreak_StageAdvancesWithCarryForward() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.TryAddOrUpgradePassive("RunningShoes");
        sim.TryAddOrUpgradeWeapon("FlareGun");
        var hpBefore = sim.PlayerCurrentHp;

        RevealAndEnterSafehouse(sim);
        sim.SelectSafehouseRewardOption(0); // any reward

        Assert.Equal(2, sim.StageNumber);
        Assert.Equal(0f, sim.StageTimeElapsed, 1);
        Assert.False(sim.IsStageExitRevealed);
        Assert.False(sim.IsPausedForSafehouseBreak);
        // Weapons and passives carried forward
        Assert.True(sim.EquippedWeapons.Count >= 2);
        Assert.Single(sim.EquippedPassives);
    }

    [Fact]
    public void UncollectedPickupsAbandonedOnStageExit() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.XpGems.Add(new XpGem(new Vector2(500, 500), 10f));
        sim.JurassicCashDrops.Add(new JurassicCashDrop(new Vector2(600, 600), 5));

        RevealAndEnterSafehouse(sim);

        Assert.Empty(sim.XpGems);
        Assert.Empty(sim.JurassicCashDrops);
    }

    [Fact]
    public void AfterStage3SafehouseBreak_AdvancesToHeliportStage() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.StageNumber = 3;

        RevealAndEnterSafehouse(sim);
        sim.SelectSafehouseRewardOption(0);

        Assert.False(sim.IsRunComplete);
        Assert.Equal(Simulation.HeliportStageNumber, sim.StageNumber);
        Assert.NotNull(sim.TRex);
        Assert.False(sim.TRex!.IsDefeated);
    }

    // Helper: advance simulation through stage 3 into the Heliport stage.
    // With StubRng, safehouse is at (1000, 0) so player ends at top boundary before stage 4.
    private static Simulation AdvanceToHeliportStage() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.StageNumber = 3;
        RevealAndEnterSafehouse(sim);
        sim.SelectSafehouseRewardOption(0);
        return sim;
    }

    // Helper: step the sim many times with small dt for predictable state-machine progression.
    private static readonly ControlActions NoInput = new(Vector2.Zero, Vector2.Zero);
    private static void StepMany(Simulation sim, float totalTime, ControlActions? actions = null) {
        var act = actions ?? NoInput;
        const float dt = 0.05f;
        for (float t = 0f; t < totalTime; t += dt)
            sim.Step(act, MathF.Min(dt, totalTime - t));
    }

    // --- Issue #16: Heliport + Boss Arena Lock-In + T-Rex boss + win ---

    [Fact]
    public void HeliportStage_TRexSpawnsAtBossArenaCenter() {
        var sim = AdvanceToHeliportStage();

        Assert.NotNull(sim.TRex);
        Assert.Equal(sim.BossArenaCenter, sim.TRex!.Position);
        Assert.Equal(1500f, sim.TRex.MaxHp);
        Assert.False(sim.TRex.IsDefeated);
    }

    [Fact]
    public void EnteringBossArenaRadius_TriggersBossArenaLockIn() {
        var sim = AdvanceToHeliportStage();
        // Player at (1000, 0) after stage-3 safehouse. BossArenaCenter=(1000,1000), radius=600.
        // Must walk south ~400 units to enter arena.
        Assert.False(sim.IsBossArenaLocked);

        // 2.1s at 200f/s = 420 units south → (1000, 420), dist to center = 580 < 600 → locked
        StepMany(sim, 2.1f, new ControlActions(new Vector2(0, 1), Vector2.Zero));

        Assert.True(sim.IsBossArenaLocked);
    }

    [Fact]
    public void BossArenaLockIn_ClampsPlayerInsideRadius() {
        var sim = AdvanceToHeliportStage();
        sim.Enemies.Clear(); // avoid enemy interference

        StepMany(sim, 2.1f, new ControlActions(new Vector2(0, 1), Vector2.Zero)); // enter arena
        Assert.True(sim.IsBossArenaLocked);

        // Walk north (away from center) for a long time — clamp must keep player inside
        StepMany(sim, 10f, new ControlActions(new Vector2(0, -1), Vector2.Zero));

        var dist = Vector2.Distance(sim.PlayerPosition, sim.BossArenaCenter);
        Assert.True(dist <= sim.BossArenaRadius + 1f,
            $"Player {dist} units from center, max allowed: {sim.BossArenaRadius}");
    }

    [Fact]
    public void ProjectileHitsTRex_DealsDamage() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        float hpBefore = trex.Hp;

        // Place projectile at T-Rex position; it should register as a hit on step
        sim.Projectiles.Add(new Projectile(trex.Position, Vector2.UnitX, damage: 100f));
        sim.Step(NoInput, 0f);

        Assert.Equal(hpBefore - 100f, trex.Hp, 2f);
        Assert.Empty(sim.Projectiles); // consumed
    }

    [Fact]
    public void TRexContactDamagesPlayer() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        float hpBefore = sim.PlayerCurrentHp;

        // Place T-Rex on top of player
        trex.Position = sim.PlayerPosition;
        sim.Step(NoInput, 0f);

        Assert.True(sim.PlayerCurrentHp < hpBefore, "T-Rex contact should damage player");
    }

    [Fact]
    public void TRex_StartsInIdle_ThenTransitionsToCharging() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;

        Assert.Equal(TRexAttackState.Idle, trex.AttackState);

        // Phase 1 sequence: Idle(1.5s) then Charging. Use 2.0s for float-accumulation margin.
        StepMany(sim, 2.0f);

        Assert.Equal(TRexAttackState.Charging, trex.AttackState);
    }

    [Fact]
    public void TRex_Charging_MovesPositionTowardPlayer() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        // Player at (1000, 0), T-Rex at (1000, 1000) → charge direction is north (-Y)
        float yBefore = trex.Position.Y;

        StepMany(sim, 2.0f); // past Idle(1.5s) into Charging
        Assert.Equal(TRexAttackState.Charging, trex.AttackState);

        float yAfter = trex.Position.Y;
        Assert.True(yAfter < yBefore, "T-Rex should move toward player (north) during charge");
    }

    [Fact]
    public void TRex_PhaseBasedOnHpThresholds() {
        var trex = new TRex(Vector2.Zero, 1500f);
        // Phase boundaries: 66% = 990f, 33% = 495f. Float precision means exact multiples
        // of MaxHp * 0.66f and MaxHp * 0.33f are fractionally above 990 / 495.
        trex.Hp = 1500f; Assert.Equal(1, trex.Phase); // full HP = Phase 1
        trex.Hp = 991f;  Assert.Equal(1, trex.Phase); // clearly above 66%
        trex.Hp = 989f;  Assert.Equal(2, trex.Phase); // clearly below 66%
        trex.Hp = 496f;  Assert.Equal(2, trex.Phase); // clearly above 33%
        trex.Hp = 494f;  Assert.Equal(3, trex.Phase); // clearly below 33%
        trex.Hp = 1f;    Assert.Equal(3, trex.Phase);
    }

    [Fact]
    public void TRex_Phase2_SequenceIncludesRoaring() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        trex.Hp = 989f; // trigger Phase 2 on next UpdateTRex call

        // Phase 2 sequence: Idle(1.5) Charging(2.0) Idle(1.0) Roaring(2.5) ...
        // Time to reach Roaring: 1.5 + 2.0 + 1.0 = 4.5s
        StepMany(sim, 4.6f);

        Assert.Equal(TRexAttackState.Roaring, trex.AttackState);
    }

    [Fact]
    public void TRex_Roaring_DamagesPlayerWithinRoarRadius() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        // Force T-Rex directly into Roaring state to avoid contact-damage death before Roar
        trex.Hp = 989f; trex.LastPhase = 2;
        trex.AttackSequenceIndex = 3; // Roaring = index 3 in Phase 2 sequence
        trex.AttackState = TRexAttackState.Roaring;
        // Place within RoarRadius (200f) but outside contact range (66f)
        trex.Position = sim.PlayerPosition + new Vector2(100f, 0f);

        float hpBefore = sim.PlayerCurrentHp;
        sim.Step(NoInput, 1.0f); // 1s at 15 DPS = 15 damage

        Assert.True(sim.PlayerCurrentHp < hpBefore, "Roar should deal damage within RoarRadius");
    }

    [Fact]
    public void TRex_Phase3_SequenceIncludesTailSweep() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        trex.Hp = 494f; // trigger Phase 3

        // Phase 3: Idle(1.0) Charging(2.0) Idle(0.5) Roaring(2.5) Idle(0.5) TailSweeping(1.5)
        // Time to TailSweeping: 1.0 + 2.0 + 0.5 + 2.5 + 0.5 = 6.5s
        StepMany(sim, 6.6f);

        Assert.Equal(TRexAttackState.TailSweeping, trex.AttackState);
    }

    [Fact]
    public void TRex_TailSweep_DamagesPlayerWithinRadius() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        // Force T-Rex into TailSweeping via state transition (AttackState != targetState triggers OnEnter)
        trex.Hp = 494f; trex.LastPhase = 3;
        trex.AttackSequenceIndex = 5; // TailSweeping = index 5 in Phase 3 sequence
        trex.AttackState = TRexAttackState.Idle; // triggers transition on next step
        // Place within TailSweepRadius (250f) but outside contact range (66f)
        trex.Position = sim.PlayerPosition + new Vector2(100f, 0f);

        float hpBefore = sim.PlayerCurrentHp;
        sim.Step(NoInput, 0f); // trigger state transition only; dt=0 avoids contact damage

        Assert.Equal(TRexAttackState.TailSweeping, trex.AttackState);
        Assert.Equal(hpBefore - trex.TailSweepDamage, sim.PlayerCurrentHp, 0.01f);
    }

    [Fact]
    public void TRex_TailSweep_DoesNotDamagePlayerOutsideRadius() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        // Force T-Rex into TailSweeping state with player beyond TailSweepRadius (250f)
        trex.Hp = 494f; trex.LastPhase = 3;
        trex.AttackSequenceIndex = 5;
        trex.AttackState = TRexAttackState.Idle;
        trex.Position = sim.PlayerPosition + new Vector2(300f, 0f); // 300 > 250

        float hpBefore = sim.PlayerCurrentHp;
        sim.Step(NoInput, 0f); // trigger state transition only

        Assert.Equal(TRexAttackState.TailSweeping, trex.AttackState);
        Assert.Equal(hpBefore, sim.PlayerCurrentHp, 0.01f); // no tail sweep damage
    }

    [Fact]
    public void TRex_SummonWaves_SpawnsRaptorsNearTRex() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;
        // Force into SummoningWaves via state transition; count Raptors before and after
        trex.AttackSequenceIndex = 3; // SummoningWaves = index 3 in Phase 1 sequence
        trex.AttackState = TRexAttackState.Idle; // triggers transition on next step
        int raptorsBefore = sim.Enemies.Count(e => e.Type == EnemyType.Raptor);

        sim.Step(NoInput, 0f); // trigger OnEnter which spawns Raptors

        Assert.Equal(TRexAttackState.SummoningWaves, trex.AttackState);
        int raptorsAfter = sim.Enemies.Count(e => e.Type == EnemyType.Raptor);
        Assert.Equal(trex.SummonCount, raptorsAfter - raptorsBefore);
    }

    [Fact]
    public void TRex_Defeat_ReleasesLockInAndRevealsChopperZone() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;

        // Lock the player in
        StepMany(sim, 2.1f, new ControlActions(new Vector2(0, 1), Vector2.Zero));
        Assert.True(sim.IsBossArenaLocked);
        Assert.False(sim.IsChopperZoneRevealed);

        // Kill T-Rex with a high-damage projectile
        sim.Projectiles.Add(new Projectile(trex.Position, Vector2.UnitX, damage: 99999f));
        sim.Step(NoInput, 0f);

        Assert.True(trex.IsDefeated);
        Assert.False(sim.IsBossArenaLocked); // lock-in released on defeat
        Assert.True(sim.IsChopperZoneRevealed);
    }

    [Fact]
    public void TouchingChopperZoneAfterDefeat_WinsRun() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;

        // Kill T-Rex instantly
        sim.Projectiles.Add(new Projectile(trex.Position, Vector2.UnitX, damage: 99999f));
        sim.Step(NoInput, 0f);
        Assert.True(trex.IsDefeated);
        Assert.False(sim.IsRunComplete);

        // Move player to chopper zone (BossArenaCenter)
        // Player at (1000, 0); ChopperZonePosition = BossArenaCenter = (1000, 1000)
        // Walk south; no lock-in clamp because T-Rex is defeated
        StepMany(sim, 5.1f, new ControlActions(new Vector2(0, 1), Vector2.Zero));

        Assert.True(sim.IsRunComplete, "Touching chopper zone after T-Rex defeat should win the run");
    }

    [Fact]
    public void DyingDuringBossFight_LosesRun() {
        var sim = AdvanceToHeliportStage();
        var trex = sim.TRex!;

        // Place T-Rex on player and remove contact cooldown
        trex.Position = sim.PlayerPosition;
        trex.ContactCooldownTimer = 0f;

        // Reduce player HP to near zero
        var field = typeof(Simulation).GetField("PlayerCurrentHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        // Use many damage hits instead of reflection
        sim.Step(NoInput, 0f); // T-Rex contact: -30 HP
        sim.Step(NoInput, 0.6f); // T-Rex contact after cooldown: -30 HP again ...

        // Flood with contact damage by putting many enemies on top of player
        for (int i = 0; i < 5; i++)
            sim.SpawnEnemyAt(sim.PlayerPosition);
        sim.Step(NoInput, 0f);
        sim.Step(NoInput, 1.1f);

        Assert.True(sim.IsRunLost, "Player should lose run when HP reaches 0 during boss fight");
    }

    [Fact]
    public void BossArenaLockIn_BlocksEscapeBeforeTRexDefeated() {
        var sim = AdvanceToHeliportStage();
        sim.Enemies.Clear();

        // Enter arena
        StepMany(sim, 2.1f, new ControlActions(new Vector2(0, 1), Vector2.Zero));
        Assert.True(sim.IsBossArenaLocked);

        // Attempt to escape north
        StepMany(sim, 10f, new ControlActions(new Vector2(0, -1), Vector2.Zero));

        // Player must remain inside the boss arena boundary
        var dist = Vector2.Distance(sim.PlayerPosition, sim.BossArenaCenter);
        Assert.True(dist <= sim.BossArenaRadius + 1f);
        Assert.False(sim.IsRunComplete); // cannot win without defeating T-Rex
    }

    // --- Issue #10: Pause Menu + Fire Mode Setting ---

    [Fact]
    public void QuitRunKeepsBankedCashAndClearsTempUpgrades() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.TryAddOrUpgradePassive("RunningShoes");
        sim.TryAddOrUpgradeWeapon("FlareGun");

        // Bank some cash by reaching a safehouse
        RevealAndEnterSafehouse(sim);
        int idx = sim.PendingSafehouseBreakOptions.FindIndex(o => o.Type == SafehouseRewardType.BankedCashBonus);
        sim.SelectSafehouseRewardOption(idx);
        var bankedBefore = sim.BankedJurassicCash;
        Assert.True(bankedBefore > 0);

        // Accumulate unbanked cash in the new stage by collecting a drop
        sim.JurassicCashDrops.Add(new JurassicCashDrop(sim.PlayerPosition, 10));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);
        Assert.True(sim.UnbankedJurassicCash > 0);

        sim.QuitRun();

        Assert.True(sim.IsRunLost);
        Assert.Equal(bankedBefore, sim.BankedJurassicCash);
        Assert.Equal(0, sim.UnbankedJurassicCash);
        Assert.Empty(sim.EquippedPassives);
    }

    [Fact]
    public void AutoFireFiresWithoutFireHeld() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        Assert.True(sim.IsAutoFireEnabled);

        // Aim right without holding Fire — auto-fire should still shoot
        var actions = new ControlActions(Vector2.Zero, new Vector2(1, 0), Fire: false);
        sim.Step(actions, 0f);

        Assert.Single(sim.Projectiles);
    }

    [Fact]
    public void ManualFireRequiresFireHeld() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.IsAutoFireEnabled = false;

        // Aim right but don't hold Fire — manual fire should NOT shoot
        sim.Step(new ControlActions(Vector2.Zero, new Vector2(1, 0), Fire: false), 0f);
        Assert.Empty(sim.Projectiles);

        // Hold Fire — now it should shoot
        sim.Step(new ControlActions(Vector2.Zero, new Vector2(1, 0), Fire: true), 0f);
        Assert.Single(sim.Projectiles);
    }

    [Fact]
    public void ManualFireCooldownGatingPreventsRateBypass() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.IsAutoFireEnabled = false;

        var fireActions = new ControlActions(Vector2.Zero, new Vector2(1, 0), Fire: true);

        // First shot fires immediately (cooldown starts at 0)
        sim.Step(fireActions, 0f);
        Assert.Single(sim.Projectiles);

        // Calling Step again immediately with Fire held — cooldown not expired, no extra shot
        sim.Step(fireActions, 0f);
        Assert.Single(sim.Projectiles); // still just one projectile
    }

    [Fact]
    public void TogglingFireModeDoesNotResetCooldownTimers() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());

        // Fire once in auto-fire mode; cooldown timer is now set
        sim.Step(new ControlActions(Vector2.Zero, new Vector2(1, 0)), 0f);
        var cooldownAfterFire = sim.EquippedWeapons[0].CooldownTimer;
        Assert.True(cooldownAfterFire > 0f);

        // Toggle to manual fire — cooldown should be unchanged
        sim.IsAutoFireEnabled = false;
        Assert.Equal(cooldownAfterFire, sim.EquippedWeapons[0].CooldownTimer);

        // Toggle back to auto-fire — still unchanged
        sim.IsAutoFireEnabled = true;
        Assert.Equal(cooldownAfterFire, sim.EquippedWeapons[0].CooldownTimer);
    }

    [Fact]
    public void PauseMenuFreezesSimulation() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        sim.SpawnEnemyAt(new Vector2(1200, 1000));
        var enemyPosBefore = sim.Enemies[0].Position;
        var playerPosBefore = sim.PlayerPosition;

        sim.TogglePause();
        Assert.True(sim.IsPaused);

        sim.Step(new ControlActions(new Vector2(1, 0), Vector2.Zero), 1f);

        Assert.Equal(enemyPosBefore, sim.Enemies[0].Position);
        Assert.Equal(playerPosBefore, sim.PlayerPosition);
    }

    [Fact]
    public void GetUpgradeOptionLabel_NewWeapon_ReturnsCorrectLabel() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        var option = new UpgradeOption { Type = UpgradeType.NewWeapon, ItemId = "TranqPistol" };

        var label = sim.GetUpgradeOptionLabel(option);

        Assert.Equal("TRANQ PISTOL - NEW", label);
    }

    [Fact]
    public void GetUpgradeOptionLabel_WeaponUpgrade_ReturnsCorrectLabel() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        var option = new UpgradeOption { Type = UpgradeType.WeaponUpgrade, ItemId = "TranqPistol" };

        var label = sim.GetUpgradeOptionLabel(option);

        Assert.Equal("TRANQ PISTOL - LV 1->2", label);
    }

    [Fact]
    public void GetUpgradeOptionLabel_PassivesAndFallback_ReturnsCorrectLabels() {
        var sim = new Simulation(new StubRng(), new StubPersistence(), new StubContentProvider());
        
        // Test NewPassive
        var newPassiveOpt = new UpgradeOption { Type = UpgradeType.NewPassive, ItemId = "RunningShoes" };
        Assert.Equal("RUNNING SHOES - NEW", sim.GetUpgradeOptionLabel(newPassiveOpt));

        // Test PassiveUpgrade
        // Equip RunningShoes first
        sim.TryAddOrUpgradePassive("RunningShoes");
        var upgradePassiveOpt = new UpgradeOption { Type = UpgradeType.PassiveUpgrade, ItemId = "RunningShoes" };
        Assert.Equal("RUNNING SHOES - LV 1->2", sim.GetUpgradeOptionLabel(upgradePassiveOpt));

        // Test CashFallback
        var fallbackOpt = new UpgradeOption { Type = UpgradeType.CashFallback };
        Assert.Equal("+25 JURASSIC CASH", sim.GetUpgradeOptionLabel(fallbackOpt));
    }

    // --- Issue #15: Save Data + Souvenir Shop + Permanent Upgrades ---

    [Fact]
    public void SaveDataRoundTripsCorrectly() {
        var persistence = new StubPersistence();
        var data = new SaveData {
            BankedJurassicCash = 350,
            IsAutoFireEnabled = false,
            ShowFloatingDamageNumbers = false,
            PermanentUpgradeRanks = new System.Collections.Generic.Dictionary<string, int> {
                { "MaxHp", 2 },
                { "Damage", 1 }
            },
            BestRun = new BestRunSummary {
                MaxStageReached = 3,
                MaxLevelReached = 12,
                MaxTimeSurvived = 605.5f,
                MaxCashCollected = 240
            }
        };

        persistence.Save(data);
        var loaded = persistence.Load();

        Assert.NotNull(loaded);
        Assert.Equal(350, loaded.BankedJurassicCash);
        Assert.False(loaded.IsAutoFireEnabled);
        Assert.False(loaded.ShowFloatingDamageNumbers);
        Assert.Equal(2, loaded.PermanentUpgradeRanks["MaxHp"]);
        Assert.Equal(1, loaded.PermanentUpgradeRanks["Damage"]);
        Assert.Equal(3, loaded.BestRun.MaxStageReached);
        Assert.Equal(12, loaded.BestRun.MaxLevelReached);
        Assert.Equal(605.5f, loaded.BestRun.MaxTimeSurvived);
        Assert.Equal(240, loaded.BestRun.MaxCashCollected);
    }

    [Fact]
    public void SimulationLoadsSaveDataOnStartup() {
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var data = new SaveData {
            BankedJurassicCash = 420,
            IsAutoFireEnabled = false,
            ShowFloatingDamageNumbers = false,
            PermanentUpgradeRanks = new System.Collections.Generic.Dictionary<string, int> {
                { "MaxHp", 2 }
            }
        };
        persistence.Save(data);

        var sim = new Simulation(rng, persistence, new StubContentProvider());

        Assert.Equal(420, sim.BankedJurassicCash);
        Assert.False(sim.IsAutoFireEnabled);
        Assert.False(sim.ShowFloatingDamageNumbers);
        Assert.Equal(2, sim.GetPermanentUpgradeRank("MaxHp"));
        Assert.Equal(0, sim.GetPermanentUpgradeRank("Damage"));
    }

    [Fact]
    public void SouvenirShopPurchaseDeductsCashAndEscalatesCost() {
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var data = new SaveData {
            BankedJurassicCash = 1000
        };
        persistence.Save(data);

        var sim = new Simulation(rng, persistence, new StubContentProvider());

        Assert.Equal(0, sim.GetPermanentUpgradeRank("MaxHp"));
        Assert.Equal(100, sim.GetPermanentUpgradeCost("MaxHp", 1));

        // Buy Rank 1
        bool success1 = sim.BuyPermanentUpgrade("MaxHp");
        Assert.True(success1);
        Assert.Equal(1, sim.GetPermanentUpgradeRank("MaxHp"));
        Assert.Equal(900, sim.BankedJurassicCash);
        Assert.Equal(250, sim.GetPermanentUpgradeCost("MaxHp", 2));

        // Buy Rank 2
        bool success2 = sim.BuyPermanentUpgrade("MaxHp");
        Assert.True(success2);
        Assert.Equal(2, sim.GetPermanentUpgradeRank("MaxHp"));
        Assert.Equal(650, sim.BankedJurassicCash);
        Assert.Equal(500, sim.GetPermanentUpgradeCost("MaxHp", 3));

        // Buy Rank 3
        bool success3 = sim.BuyPermanentUpgrade("MaxHp");
        Assert.True(success3);
        Assert.Equal(3, sim.GetPermanentUpgradeRank("MaxHp"));
        Assert.Equal(150, sim.BankedJurassicCash);
        Assert.Equal(-1, sim.GetPermanentUpgradeCost("MaxHp", 4));

        // Try to buy Rank 4 (should fail because rank cap is 3)
        bool success4 = sim.BuyPermanentUpgrade("MaxHp");
        Assert.False(success4);
        Assert.Equal(3, sim.GetPermanentUpgradeRank("MaxHp"));
        Assert.Equal(150, sim.BankedJurassicCash);

        // Try to buy a different upgrade with insufficient cash (cost 100, we have 150)
        // Let's buy "Damage" rank 1
        bool successDamage1 = sim.BuyPermanentUpgrade("Damage");
        Assert.True(successDamage1);
        Assert.Equal(1, sim.GetPermanentUpgradeRank("Damage"));
        Assert.Equal(50, sim.BankedJurassicCash);

        // Try to buy "Damage" rank 2 with insufficient cash (cost 250, we have 50)
        bool successDamage2 = sim.BuyPermanentUpgrade("Damage");
        Assert.False(successDamage2);
        Assert.Equal(1, sim.GetPermanentUpgradeRank("Damage"));
        Assert.Equal(50, sim.BankedJurassicCash);
    }

    [Fact]
    public void PermanentUpgradesRaisePlayerBaselineStats() {
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var data = new SaveData {
            BankedJurassicCash = 1000
        };
        persistence.Save(data);

        var sim = new Simulation(rng, persistence, new StubContentProvider());

        // Buy rank 1 of each upgrade
        Assert.True(sim.BuyPermanentUpgrade("MaxHp"));
        Assert.True(sim.BuyPermanentUpgrade("MoveSpeed"));
        Assert.True(sim.BuyPermanentUpgrade("PickupRadius"));
        Assert.True(sim.BuyPermanentUpgrade("Damage"));
        Assert.True(sim.BuyPermanentUpgrade("WeaponCooldown"));

        // Verify effective stats:
        // Max HP: base 100 * 1.10 = 110
        Assert.Equal(110f, sim.PlayerEffectiveMaxHp);
        // Move Speed: base 200 * 1.10 = 220
        Assert.Equal(220f, sim.PlayerEffectiveSpeed);
        // Pickup Radius: base 45 * 1.10 = 49.5
        Assert.Equal(49.5f, sim.PlayerEffectivePickupRadius);

        // Verify compounding with passive items:
        // Add running shoes (Multiplier = 1.15)
        sim.TryAddOrUpgradePassive("RunningShoes");
        // Speed: base 200 * permanent 1.10 * passive 1.15 = 253
        Assert.Equal(253f, sim.PlayerEffectiveSpeed);
    }

    [Fact]
    public void PermanentUpgradesModifyProjectileDamageAndWeaponCooldown() {
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var data = new SaveData {
            BankedJurassicCash = 1000
        };
        persistence.Save(data);

        var sim = new Simulation(rng, persistence, new StubContentProvider());

        // Buy Damage and Weapon Cooldown
        Assert.True(sim.BuyPermanentUpgrade("Damage"));
        Assert.True(sim.BuyPermanentUpgrade("WeaponCooldown"));

        // Step once to fire a projectile
        sim.Step(new ControlActions(Vector2.Zero, new Vector2(1, 0)), 0f);

        // Projectile damage: 10 base * 1.10 = 11
        Assert.Single(sim.Projectiles);
        Assert.Equal(11f, sim.Projectiles[0].Damage);

        // Weapon cooldown: 0.8s base * 0.90 = 0.72
        Assert.Equal(0.72f, sim.EquippedWeapons[0].CooldownTimer, 3);
    }

    [Fact]
    public void BestRunSummaryUpdatesOnRunEnd() {
        var rng = new StubRng();
        var persistence = new StubPersistence();
        var data = new SaveData {
            BankedJurassicCash = 100
        };
        persistence.Save(data);

        var sim = new Simulation(rng, persistence, new StubContentProvider());

        // Simulate a run where we collect some cash, level up, survive time, and advance stage
        sim.StageNumber = 2;
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 150f); // survive 150s

        // Add 10 XP gems at player position to trigger level up
        for (int i = 0; i < 10; i++) {
            sim.XpGems.Add(new XpGem(sim.PlayerPosition, 10f));
        }
        // Step once to collect them
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f);
        Assert.True(sim.IsPausedForLevelUp);
        sim.SelectLevelUpOption(0); // Level-up completed to Level 2

        // Add 2 cash drops at player position
        sim.JurassicCashDrops.Add(new JurassicCashDrop(sim.PlayerPosition, 10));
        sim.JurassicCashDrops.Add(new JurassicCashDrop(sim.PlayerPosition, 15));
        sim.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 0.1f); // collect cash

        // Now quit the run
        sim.QuitRun();

        // Check that best run summary in persistence was updated
        var loaded = persistence.Load();
        Assert.Equal(2, loaded.BestRun.MaxStageReached);
        Assert.Equal(2, loaded.BestRun.MaxLevelReached);
        Assert.Equal(150.2f, loaded.BestRun.MaxTimeSurvived, 1);
        Assert.Equal(25, loaded.BestRun.MaxCashCollected);

        // Run again with lesser stats and check that it doesn't overwrite with lesser values
        var sim2 = new Simulation(rng, persistence, new StubContentProvider());
        sim2.StageNumber = 1;
        sim2.Step(new ControlActions(Vector2.Zero, Vector2.Zero), 50f);
        sim2.QuitRun();

        var loaded2 = persistence.Load();
        Assert.Equal(2, loaded2.BestRun.MaxStageReached);
        Assert.Equal(2, loaded2.BestRun.MaxLevelReached);
        Assert.Equal(150.2f, loaded2.BestRun.MaxTimeSurvived, 1);
        Assert.Equal(25, loaded2.BestRun.MaxCashCollected);
    }

    [Fact]
    public void FilePersistenceRoundTripsThroughDisk() {
        string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString() + "_save.json");
        try {
            var persistence = new FilePersistence(tempPath);

            // Load empty
            var loadedEmpty = persistence.Load();
            Assert.NotNull(loadedEmpty);
            Assert.Equal(0, loadedEmpty.BankedJurassicCash);

            // Save some data
            var data = new SaveData {
                BankedJurassicCash = 150,
                IsAutoFireEnabled = false,
                ShowFloatingDamageNumbers = true,
                PermanentUpgradeRanks = new System.Collections.Generic.Dictionary<string, int> {
                    { "MoveSpeed", 1 }
                },
                BestRun = new BestRunSummary {
                    MaxStageReached = 2
                }
            };
            persistence.Save(data);

            // Read it back
            var loaded = persistence.Load();
            Assert.NotNull(loaded);
            Assert.Equal(150, loaded.BankedJurassicCash);
            Assert.False(loaded.IsAutoFireEnabled);
            Assert.True(loaded.ShowFloatingDamageNumbers);
            Assert.Equal(1, loaded.PermanentUpgradeRanks["MoveSpeed"]);
            Assert.Equal(2, loaded.BestRun.MaxStageReached);
        } finally {
            if (System.IO.File.Exists(tempPath)) {
                System.IO.File.Delete(tempPath);
            }
        }
    }
}




