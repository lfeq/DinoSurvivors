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
    public List<PassiveInstance> EquippedPassives { get; } = new();
    public bool IsPausedForLevelUp { get; private set; } = false;
    public List<UpgradeOption> PendingLevelUpOptions { get; } = new();
    public int UnbankedJurassicCash { get; private set; } = 0;
    public int BankedJurassicCash { get; private set; } = 0;
    public List<JurassicCashDrop> JurassicCashDrops { get; } = new();
    private const float CashDropChance = 0.20f;

    public float PlayerEffectivePickupRadius => PlayerPickupRadius * GetPassiveMultiplier(PassiveStat.PickupRadius);
    public float PlayerEffectiveSpeed => PlayerSpeed * GetPassiveMultiplier(PassiveStat.MoveSpeed);
    public float PlayerEffectiveMaxHp => PlayerMaxHp * GetPassiveMultiplier(PassiveStat.MaxHp);

    private float GetPassiveMultiplier(PassiveStat stat) {
        var passive = EquippedPassives.Find(p => p.Definition.Stat == stat);
        return passive?.CurrentLevelData.Multiplier ?? 1f;
    }

    public int StageNumber { get; set; } = 1;
    public float StageTimeElapsed { get; set; } = 0f;
    public int LiveEnemyCap => StageNumber switch { 2 => 100, 3 => 120, _ => 80 };

    private readonly IRng _rng;
    private readonly IContentProvider _content;
    public List<Enemy> Enemies { get; } = new();
    private float _spawnTimer = 0f;
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

    public bool TryAddOrUpgradePassive(string passiveId) {
        var existing = EquippedPassives.Find(p => p.Definition.Id == passiveId);
        if (existing != null) {
            if (existing.Level >= 3) return false;
            existing.Level++;
            return true;
        }
        if (EquippedPassives.Count >= 3) return false;
        var def = _content.GetPassiveDefinition(passiveId);
        if (def == null) return false;
        EquippedPassives.Add(new PassiveInstance(def, 1));
        return true;
    }

    public void SpawnEnemyAt(Vector2 position) {
        Enemies.Add(new Enemy(position));
    }

    public void SelectLevelUpOption(int index) {
        if (!IsPausedForLevelUp || index < 0 || index >= PendingLevelUpOptions.Count) return;

        var option = PendingLevelUpOptions[index];
        var oldThreshold = XpToNextLevel;

        switch (option.Type) {
            case UpgradeType.NewWeapon:
            case UpgradeType.WeaponUpgrade:
                if (option.ItemId != null) TryAddOrUpgradeWeapon(option.ItemId);
                break;
            case UpgradeType.NewPassive:
            case UpgradeType.PassiveUpgrade:
                if (option.ItemId != null) TryAddOrUpgradePassive(option.ItemId);
                break;
            case UpgradeType.CashFallback:
                UnbankedJurassicCash += 25;
                break;
        }

        PlayerXp -= oldThreshold;
        PlayerLevel++;
        PendingLevelUpOptions.Clear();
        IsPausedForLevelUp = false;

        if (PlayerXp >= XpToNextLevel) {
            PendingLevelUpOptions.AddRange(GenerateLevelUpOptions());
            IsPausedForLevelUp = true;
        }
    }

    private List<UpgradeOption> GenerateLevelUpOptions() {
        var pool = new List<UpgradeOption>();

        foreach (var weaponDef in _content.GetAllWeapons()) {
            var existing = EquippedWeapons.Find(w => w.Definition.Id == weaponDef.Id);
            if (existing == null) {
                if (EquippedWeapons.Count < 3)
                    pool.Add(new UpgradeOption { Type = UpgradeType.NewWeapon, ItemId = weaponDef.Id });
            } else if (existing.Level < 5) {
                pool.Add(new UpgradeOption { Type = UpgradeType.WeaponUpgrade, ItemId = weaponDef.Id });
            }
        }

        foreach (var passiveDef in _content.GetAllPassives()) {
            var existing = EquippedPassives.Find(p => p.Definition.Id == passiveDef.Id);
            if (existing == null) {
                if (EquippedPassives.Count < 3)
                    pool.Add(new UpgradeOption { Type = UpgradeType.NewPassive, ItemId = passiveDef.Id });
            } else if (existing.Level < 3) {
                pool.Add(new UpgradeOption { Type = UpgradeType.PassiveUpgrade, ItemId = passiveDef.Id });
            }
        }

        var remaining = new List<UpgradeOption>(pool);
        var selected = new List<UpgradeOption>();

        while (selected.Count < 3 && remaining.Count > 0) {
            int idx = _rng.Next(0, remaining.Count);
            selected.Add(remaining[idx]);
            remaining.RemoveAt(idx);
        }

        // Category balancing: avoid all 3 from the same category when an alternative exists
        if (selected.Count == 3) {
            bool allWeapon = selected.All(o => o.Type is UpgradeType.NewWeapon or UpgradeType.WeaponUpgrade);
            bool allPassive = selected.All(o => o.Type is UpgradeType.NewPassive or UpgradeType.PassiveUpgrade);

            if (allWeapon) {
                var alt = remaining.FirstOrDefault(o => o.Type is UpgradeType.NewPassive or UpgradeType.PassiveUpgrade);
                if (alt != null) selected[2] = alt;
            } else if (allPassive) {
                var alt = remaining.FirstOrDefault(o => o.Type is UpgradeType.NewWeapon or UpgradeType.WeaponUpgrade);
                if (alt != null) selected[2] = alt;
            }
        }

        while (selected.Count < 3)
            selected.Add(new UpgradeOption { Type = UpgradeType.CashFallback });

        return selected;
    }

    private WavePhase GetCurrentPhase() {
        if (!DefaultContent.WaveSchedules.TryGetValue(StageNumber, out var phases))
            return DefaultContent.WaveSchedules[1][0];
        WavePhase current = phases[0];
        foreach (var phase in phases)
            if (StageTimeElapsed >= phase.StartTime) current = phase;
        return current;
    }

    private EnemyType SelectEnemyType(WavePhase phase) {
        int total = 0;
        foreach (var (_, w) in phase.Weights) total += w;
        int roll = _rng.Next(0, total);
        int acc = 0;
        foreach (var (type, weight) in phase.Weights) {
            acc += weight;
            if (roll < acc) return type;
        }
        return phase.Weights[0].Type;
    }

    public void Step(ControlActions actions, float deltaTime) {
        if (IsRunLost || IsPausedForLevelUp) {
            return;
        }

        StageTimeElapsed += deltaTime;

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
                        var effectiveDamage = data.Damage * GetPassiveMultiplier(PassiveStat.Damage);
                        Projectiles.Add(new Projectile(
                            PlayerPosition,
                            aimDir,
                            data.ProjectileSpeed,
                            data.ProjectileRadius,
                            effectiveDamage,
                            data.PierceCount,
                            data.ExplosionRadius
                        ));
                        weapon.CooldownTimer = data.Cooldown * GetPassiveMultiplier(PassiveStat.WeaponCooldown);
                    }
                } else {
                    // Autonomous area zapper behavior
                    var data = weapon.CurrentLevelData;
                    var autonomousDamage = data.Damage * GetPassiveMultiplier(PassiveStat.Damage);
                    for (int j = Enemies.Count - 1; j >= 0; j--) {
                        var enemy = Enemies[j];
                        var dist = Vector2.Distance(PlayerPosition, enemy.Position);
                        if (dist <= data.RangeOrRadius) {
                            enemy.Hp -= autonomousDamage;
                            enemy.HitFlashTimer = 0.15f;
                            OnEnemyHit?.Invoke(enemy.Position, autonomousDamage);
                            if (enemy.Hp <= 0f) {
                                Enemies.RemoveAt(j);
                                OnEnemyKilled?.Invoke(enemy.Position);
                                XpGems.Add(new XpGem(enemy.Position));
                                if (_rng.NextDouble() < CashDropChance)
                                    JurassicCashDrops.Add(new JurassicCashDrop(enemy.Position));
                            }
                        }
                    }
                    weapon.CooldownTimer = data.Cooldown * GetPassiveMultiplier(PassiveStat.WeaponCooldown);
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
                                    if (_rng.NextDouble() < CashDropChance)
                                        JurassicCashDrops.Add(new JurassicCashDrop(targetEnemy.Position));
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
                            if (_rng.NextDouble() < CashDropChance)
                                JurassicCashDrops.Add(new JurassicCashDrop(enemy.Position));
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
            PlayerPosition += moveDir * PlayerEffectiveSpeed * deltaTime;
        }

        PlayerPosition = Vector2.Clamp(PlayerPosition, Vector2.Zero, ArenaSize);

        // Check player-to-XP-gem collection
        for (int i = XpGems.Count - 1; i >= 0; i--) {
            var gem = XpGems[i];
            var dist = Vector2.Distance(PlayerPosition, gem.Position);
            if (dist <= PlayerEffectivePickupRadius) {
                PlayerXp += gem.XpValue;
                OnXpGemCollected?.Invoke(gem);
                XpGems.RemoveAt(i);
                if (!IsPausedForLevelUp && PlayerXp >= XpToNextLevel) {
                    PendingLevelUpOptions.AddRange(GenerateLevelUpOptions());
                    IsPausedForLevelUp = true;
                }
            }
        }

        // Check player-to-Jurassic-Cash-drop collection
        for (int i = JurassicCashDrops.Count - 1; i >= 0; i--) {
            var drop = JurassicCashDrops[i];
            if (Vector2.Distance(PlayerPosition, drop.Position) <= PlayerEffectivePickupRadius) {
                UnbankedJurassicCash += drop.CashValue;
                JurassicCashDrops.RemoveAt(i);
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
                        UnbankedJurassicCash = 0;
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
        var phase = GetCurrentPhase();
        if (_spawnTimer >= phase.SpawnInterval) {
            _spawnTimer -= phase.SpawnInterval;
            if (Enemies.Count < LiveEnemyCap)
                SpawnEnemy();
        }

        MergePickups();
    }

    private void MergePickups() {
        const float mergeRadius = 30f;
        const int mergeThreshold = 5;

        bool anyMerged = true;
        while (anyMerged) {
            anyMerged = false;
            for (int i = 0; i < XpGems.Count && !anyMerged; i++) {
                var indices = new List<int> { i };
                for (int j = 0; j < XpGems.Count; j++) {
                    if (j != i && Vector2.Distance(XpGems[i].Position, XpGems[j].Position) <= mergeRadius)
                        indices.Add(j);
                }
                if (indices.Count >= mergeThreshold) {
                    float total = 0f;
                    foreach (int idx in indices) total += XpGems[idx].XpValue;
                    var pos = XpGems[i].Position;
                    indices.Sort((a, b) => b.CompareTo(a));
                    foreach (int idx in indices) XpGems.RemoveAt(idx);
                    XpGems.Add(new XpGem(pos, total));
                    anyMerged = true;
                }
            }
        }

        anyMerged = true;
        while (anyMerged) {
            anyMerged = false;
            for (int i = 0; i < JurassicCashDrops.Count && !anyMerged; i++) {
                var indices = new List<int> { i };
                for (int j = 0; j < JurassicCashDrops.Count; j++) {
                    if (j != i && Vector2.Distance(JurassicCashDrops[i].Position, JurassicCashDrops[j].Position) <= mergeRadius)
                        indices.Add(j);
                }
                if (indices.Count >= mergeThreshold) {
                    int total = 0;
                    foreach (int idx in indices) total += JurassicCashDrops[idx].CashValue;
                    var pos = JurassicCashDrops[i].Position;
                    indices.Sort((a, b) => b.CompareTo(a));
                    foreach (int idx in indices) JurassicCashDrops.RemoveAt(idx);
                    JurassicCashDrops.Add(new JurassicCashDrop(pos, total));
                    anyMerged = true;
                }
            }
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
            var type = SelectEnemyType(GetCurrentPhase());
            Enemies.Add(new Enemy(spawnPos, type));
        }
    }
}
