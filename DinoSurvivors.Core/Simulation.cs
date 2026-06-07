using System;
using System.Numerics;

namespace DinoSurvivors.Core;

public class Simulation {
    public Vector2 PlayerPosition { get; private set; }
    public float PlayerSpeed { get; } = 200f;
    public Vector2 ArenaSize { get; } = new(2000, 2000);
    public float PlayerMaxHp { get; } = 100f;
    public float PlayerCurrentHp { get; private set; } = 100f;
    public bool IsRunLost { get; private set; } = false;
    public event Action? OnPlayerDamaged;
    public List<Projectile> Projectiles { get; } = new();
    public float WeaponCooldown { get; } = 0.8f;
    public event Action<Vector2, float>? OnEnemyHit;
    public event Action<Vector2>? OnEnemyKilled;
    public List<XpGem> XpGems { get; } = new();
    public float PlayerPickupRadius { get; set; } = 45f;
    public int PlayerLevel { get; private set; } = 1;
    public float PlayerXp { get; private set; } = 0f;
    public float XpToNextLevel => 100f * PlayerLevel;
    public event Action<XpGem>? OnXpGemCollected;
    public List<WeaponInstance> EquippedWeapons { get; } = new();

    private readonly IRng _rng;
    private readonly IContentProvider _content;
    public List<Enemy> Enemies { get; } = new();
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 1.5f;
    private const float MinSpawnRadius = 450f;
    private const float MaxSpawnRadius = 800f;

    public Simulation(IRng rng, IPersistence persistence, IContentProvider content) {
        _rng = rng;
        _content = content;
        PlayerPosition = ArenaSize / 2f;

        var startingWeaponDef = _content.GetWeaponDefinition("TranqPistol");
        if (startingWeaponDef != null) {
            EquippedWeapons.Add(new WeaponInstance(startingWeaponDef, 1));
        }
    }

    public bool TryAddOrUpgradeWeapon(string weaponId) {
        var existing = EquippedWeapons.Find(w => w.Definition.Id == weaponId);
        if (existing != null) {
            if (existing.Level >= 5) {
                return false;
            }
            existing.Level++;
            return true;
        } else {
            if (EquippedWeapons.Count >= 3) {
                return false;
            }
            var def = _content.GetWeaponDefinition(weaponId);
            if (def == null) {
                return false;
            }
            EquippedWeapons.Add(new WeaponInstance(def, 1));
            return true;
        }
    }

    public void SpawnEnemyAt(Vector2 position) {
        Enemies.Add(new Enemy(position));
    }

    public void Step(ControlActions actions, float deltaTime) {
        if (IsRunLost) {
            return;
        }

        // Firing logic for equipped weapons
        foreach (var weapon in EquippedWeapons) {
            if (weapon.CooldownTimer > 0f) {
                weapon.CooldownTimer -= deltaTime;
                if (weapon.CooldownTimer < 0f) {
                    weapon.CooldownTimer = 0f;
                }
            }

            if (weapon.CooldownTimer <= 0f) {
                if (weapon.Definition.IsAimed) {
                    if (actions.AimDirection != Vector2.Zero) {
                        var aimDir = actions.AimDirection;
                        if (aimDir.LengthSquared() > 1f) {
                            aimDir = Vector2.Normalize(aimDir);
                        }
                        var data = weapon.CurrentLevelData;
                        Projectiles.Add(new Projectile(
                            PlayerPosition, 
                            aimDir, 
                            data.ProjectileSpeed, 
                            data.ProjectileRadius, 
                            data.Damage, 
                            data.PierceCount,
                            data.ExplosionRadius
                        ));
                        weapon.CooldownTimer = data.Cooldown;
                    }
                } else {
                    // Autonomous area zapper behavior
                    var data = weapon.CurrentLevelData;
                    for (int j = Enemies.Count - 1; j >= 0; j--) {
                        var enemy = Enemies[j];
                        var dist = Vector2.Distance(PlayerPosition, enemy.Position);
                        if (dist <= data.RangeOrRadius) {
                            enemy.Hp -= data.Damage;
                            enemy.HitFlashTimer = 0.15f;
                            OnEnemyHit?.Invoke(enemy.Position, data.Damage);
                            if (enemy.Hp <= 0f) {
                                Enemies.RemoveAt(j);
                                OnEnemyKilled?.Invoke(enemy.Position);
                                XpGems.Add(new XpGem(enemy.Position));
                            }
                        }
                    }
                    weapon.CooldownTimer = data.Cooldown;
                }
            }
        }

        // Move and clean up out-of-bounds projectiles
        for (int i = Projectiles.Count - 1; i >= 0; i--) {
            var projectile = Projectiles[i];
            projectile.Position += projectile.Direction * projectile.Speed * deltaTime;
            if (projectile.Position.X < 0 || projectile.Position.X > ArenaSize.X ||
                projectile.Position.Y < 0 || projectile.Position.Y > ArenaSize.Y) {
                Projectiles.RemoveAt(i);
            }
        }

        // Check projectile-to-enemy collisions
        for (int i = Projectiles.Count - 1; i >= 0; i--) {
            var projectile = Projectiles[i];
            for (int j = Enemies.Count - 1; j >= 0; j--) {
                var enemy = Enemies[j];
                var dist = Vector2.Distance(projectile.Position, enemy.Position);
                var minDist = projectile.Radius + enemy.Radius;
                if (dist < minDist && !projectile.HitEnemies.Contains(enemy)) {
                    projectile.HitEnemies.Add(enemy);
                    if (projectile.ExplosionRadius > 0f) {
                        for (int k = Enemies.Count - 1; k >= 0; k--) {
                            var targetEnemy = Enemies[k];
                            var targetDist = Vector2.Distance(projectile.Position, targetEnemy.Position);
                            if (targetDist <= projectile.ExplosionRadius) {
                                targetEnemy.Hp -= projectile.Damage;
                                targetEnemy.HitFlashTimer = 0.15f;
                                OnEnemyHit?.Invoke(targetEnemy.Position, projectile.Damage);
                                if (targetEnemy.Hp <= 0f) {
                                    Enemies.RemoveAt(k);
                                    OnEnemyKilled?.Invoke(targetEnemy.Position);
                                    XpGems.Add(new XpGem(targetEnemy.Position));
                                }
                            }
                        }
                        projectile.PierceCount = 0;
                        break;
                    } else {
                        enemy.Hp -= projectile.Damage;
                        enemy.HitFlashTimer = 0.15f;
                        OnEnemyHit?.Invoke(projectile.Position, projectile.Damage);
                        if (enemy.Hp <= 0f) {
                            Enemies.RemoveAt(j);
                            OnEnemyKilled?.Invoke(enemy.Position);
                            XpGems.Add(new XpGem(enemy.Position));
                        }
                        projectile.PierceCount--;
                        if (projectile.PierceCount <= 0) {
                            break;
                        }
                    }
                }
            }
            if (projectile.PierceCount <= 0) {
                Projectiles.RemoveAt(i);
            }
        }

        foreach (var enemy in Enemies) {
            if (enemy.ContactCooldownTimer > 0f) {
                enemy.ContactCooldownTimer -= deltaTime;
                if (enemy.ContactCooldownTimer < 0f) {
                    enemy.ContactCooldownTimer = 0f;
                }
            }
            if (enemy.HitFlashTimer > 0f) {
                enemy.HitFlashTimer -= deltaTime;
                if (enemy.HitFlashTimer < 0f) {
                    enemy.HitFlashTimer = 0f;
                }
            }
        }

        if (actions.MoveDirection != Vector2.Zero) {
            var moveDir = actions.MoveDirection;
            if (moveDir.LengthSquared() > 1f) {
                moveDir = Vector2.Normalize(moveDir);
            }
            PlayerPosition += moveDir * PlayerSpeed * deltaTime;
        }

        PlayerPosition = Vector2.Clamp(PlayerPosition, Vector2.Zero, ArenaSize);

        // Check player-to-XP-gem collection
        for (int i = XpGems.Count - 1; i >= 0; i--) {
            var gem = XpGems[i];
            var dist = Vector2.Distance(PlayerPosition, gem.Position);
            if (dist <= PlayerPickupRadius) {
                PlayerXp += gem.XpValue;
                while (PlayerXp >= XpToNextLevel) {
                    PlayerXp -= XpToNextLevel;
                    PlayerLevel++;
                }
                OnXpGemCollected?.Invoke(gem);
                XpGems.RemoveAt(i);
            }
        }

        // Move enemies towards player (Direct Pursuit)
        foreach (var enemy in Enemies) {
            var toPlayer = PlayerPosition - enemy.Position;
            if (toPlayer != Vector2.Zero) {
                toPlayer = Vector2.Normalize(toPlayer);
            }
            enemy.Position += toPlayer * enemy.Speed * deltaTime;
        }

        // Resolve enemy-to-enemy soft collisions
        for (int i = 0; i < Enemies.Count; i++) {
            for (int j = i + 1; j < Enemies.Count; j++) {
                var e1 = Enemies[i];
                var e2 = Enemies[j];
                var dist = Vector2.Distance(e1.Position, e2.Position);
                var minDist = e1.Radius + e2.Radius;
                if (dist < minDist) {
                    var overlap = minDist - dist;
                    var pushDir = Vector2.Zero;
                    if (dist > 0.0001f) {
                        pushDir = Vector2.Normalize(e1.Position - e2.Position);
                    } else {
                        pushDir = new Vector2(1f, 0f);
                    }
                    e1.Position += pushDir * overlap * 0.5f;
                    e2.Position -= pushDir * overlap * 0.5f;
                }
            }
        }

        // Resolve enemy-to-player soft collisions
        const float playerRadius = 16f;
        foreach (var enemy in Enemies) {
            var dist = Vector2.Distance(PlayerPosition, enemy.Position);
            var minDist = playerRadius + enemy.Radius;
            if (dist < minDist) {
                if (enemy.ContactCooldownTimer <= 0f) {
                    PlayerCurrentHp -= enemy.Damage;
                    if (PlayerCurrentHp <= 0f) {
                        PlayerCurrentHp = 0f;
                        IsRunLost = true;
                    }
                    enemy.ContactCooldownTimer = enemy.ContactCooldown;
                    OnPlayerDamaged?.Invoke();
                }

                var overlap = minDist - dist;
                var pushDir = Vector2.Zero;
                if (dist > 0.0001f) {
                    pushDir = Vector2.Normalize(PlayerPosition - enemy.Position);
                } else {
                    pushDir = new Vector2(1f, 0f);
                }
                PlayerPosition += pushDir * overlap * 0.2f;
                enemy.Position -= pushDir * overlap * 0.8f;
            }
        }

        // Clamp player and enemies to arena boundaries
        PlayerPosition = Vector2.Clamp(PlayerPosition, Vector2.Zero, ArenaSize);
        foreach (var enemy in Enemies) {
            enemy.Position = Vector2.Clamp(enemy.Position, Vector2.Zero, ArenaSize);
        }

        // Spawn logic
        _spawnTimer += deltaTime;
        if (_spawnTimer >= SpawnInterval) {
            _spawnTimer -= SpawnInterval;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy() {
        Vector2 spawnPos = Vector2.Zero;
        bool valid = false;
        for (int i = 0; i < 10; i++) {
            double angle = _rng.NextDouble() * 2 * System.Math.PI;
            double distance = MinSpawnRadius + _rng.NextDouble() * (MaxSpawnRadius - MinSpawnRadius);
            var offset = new Vector2((float)(System.Math.Cos(angle) * distance), (float)(System.Math.Sin(angle) * distance));
            var candidate = Vector2.Clamp(PlayerPosition + offset, Vector2.Zero, ArenaSize);
            if (Vector2.Distance(candidate, PlayerPosition) >= MinSpawnRadius) {
                spawnPos = candidate;
                valid = true;
                break;
            }
        }
        if (valid) {
            Enemies.Add(new Enemy(spawnPos));
        }
    }
}
