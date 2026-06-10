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
    public int BankedJurassicCash {
        get => _saveData.BankedJurassicCash;
        private set => _saveData.BankedJurassicCash = value;
    }
    public List<JurassicCashDrop> JurassicCashDrops { get; } = new();
    private const float CashDropChance = 0.20f;

    public float PlayerEffectivePickupRadius => PlayerPickupRadius * GetPermanentUpgradeMultiplier(PassiveStat.PickupRadius) * GetPassiveMultiplier(PassiveStat.PickupRadius);
    public float PlayerEffectiveSpeed => PlayerSpeed * GetPermanentUpgradeMultiplier(PassiveStat.MoveSpeed) * GetPassiveMultiplier(PassiveStat.MoveSpeed);
    public float PlayerEffectiveMaxHp => PlayerMaxHp * GetPermanentUpgradeMultiplier(PassiveStat.MaxHp) * GetPassiveMultiplier(PassiveStat.MaxHp);

    private float GetPassiveMultiplier(PassiveStat stat) {
        var passive = EquippedPassives.Find(p => p.Definition.Stat == stat);
        return passive?.CurrentLevelData.Multiplier ?? 1f;
    }

    public float GetPermanentUpgradeMultiplier(PassiveStat stat) {
        string upgradeId = stat switch {
            PassiveStat.MaxHp => "MaxHp",
            PassiveStat.Damage => "Damage",
            PassiveStat.MoveSpeed => "MoveSpeed",
            PassiveStat.WeaponCooldown => "WeaponCooldown",
            PassiveStat.PickupRadius => "PickupRadius",
            _ => ""
        };
        if (string.IsNullOrEmpty(upgradeId)) return 1f;
        int rank = GetPermanentUpgradeRank(upgradeId);
        if (rank <= 0) return 1f;
        if (stat == PassiveStat.WeaponCooldown) {
            return 1f - (rank * 0.10f);
        } else {
            return 1f + (rank * 0.10f);
        }
    }

    public int StageNumber { get; set; } = 1;
    public float StageTimeElapsed { get; set; } = 0f;
    public int LiveEnemyCap => StageNumber switch { 2 => 100, 3 => 120, HeliportStageNumber => 60, _ => 80 };

    public const float StageExitRevealTime = 600f;
    public const float SafehouseZoneRadius = 60f;
    public bool IsStageExitRevealed { get; private set; }
    public Vector2 SafehousePosition { get; private set; }
    public Vector2 ExitMarkerDirection {
        get {
            if (!IsStageExitRevealed) return Vector2.Zero;
            var dir = SafehousePosition - PlayerPosition;
            return dir.LengthSquared() > 0f ? Vector2.Normalize(dir) : Vector2.Zero;
        }
    }

    public bool IsPausedForSafehouseBreak { get; private set; }
    public List<SafehouseRewardOption> PendingSafehouseBreakOptions { get; } = new();
    public bool IsRunComplete { get; private set; }

    public const int HeliportStageNumber = 4;
    public TRex? TRex { get; set; }
    public Vector2 BossArenaCenter => ArenaSize / 2f;
    public float BossArenaRadius { get; } = 600f;
    public bool IsBossArenaLocked { get; private set; }
    public Vector2 ChopperZonePosition => BossArenaCenter;
    public float ChopperZoneRadius { get; } = 80f;
    public bool IsChopperZoneRevealed => TRex?.IsDefeated ?? false;
    public event Action? OnTRexDefeated;

    public bool IsPaused { get; private set; } = false;
    public bool IsAutoFireEnabled {
        get => _saveData.IsAutoFireEnabled;
        set => _saveData.IsAutoFireEnabled = value;
    }
    public bool ShowFloatingDamageNumbers {
        get => _saveData.ShowFloatingDamageNumbers;
        set => _saveData.ShowFloatingDamageNumbers = value;
    }

    public void TogglePause() {
        IsPaused = !IsPaused;
    }

    public int GetPermanentUpgradeRank(string upgradeId) {
        if (_saveData.PermanentUpgradeRanks.TryGetValue(upgradeId, out var rank)) {
            return rank;
        }
        return 0;
    }

    public int GetPermanentUpgradeCost(string upgradeId, int rank) {
        if (rank < 1 || rank > 3) return -1;
        return rank switch {
            1 => 100,
            2 => 250,
            3 => 500,
            _ => -1
        };
    }

    public bool BuyPermanentUpgrade(string upgradeId) {
        int currentRank = GetPermanentUpgradeRank(upgradeId);
        if (currentRank >= 3) return false;

        int cost = GetPermanentUpgradeCost(upgradeId, currentRank + 1);
        if (cost < 0 || BankedJurassicCash < cost) return false;

        _saveData.BankedJurassicCash -= cost;
        _saveData.PermanentUpgradeRanks[upgradeId] = currentRank + 1;
        SaveSimulationData();
        return true;
    }

    public void SaveSimulationData() {
        _persistence.Save(_saveData);
    }

    private void UpdateBestRunSummary() {
        bool changed = false;
        if (StageNumber > _saveData.BestRun.MaxStageReached) {
            _saveData.BestRun.MaxStageReached = StageNumber;
            changed = true;
        }
        if (PlayerLevel > _saveData.BestRun.MaxLevelReached) {
            _saveData.BestRun.MaxLevelReached = PlayerLevel;
            changed = true;
        }
        if (StageTimeElapsed > _saveData.BestRun.MaxTimeSurvived) {
            _saveData.BestRun.MaxTimeSurvived = StageTimeElapsed;
            changed = true;
        }
        if (_cashCollectedThisRun > _saveData.BestRun.MaxCashCollected) {
            _saveData.BestRun.MaxCashCollected = _cashCollectedThisRun;
            changed = true;
        }
        if (changed) {
            SaveSimulationData();
        }
    }

    public void QuitRun() {
        if (!IsRunLost) {
            UpdateBestRunSummary();
            _wasRunLostLastFrame = true;
        }
        UnbankedJurassicCash = 0;
        EquippedPassives.Clear();
        EquippedWeapons.Clear();
        var startingWeaponDef = _content.GetWeaponDefinition("TranqPistol");
        if (startingWeaponDef != null)
            EquippedWeapons.Add(new WeaponInstance(startingWeaponDef, 1));
        IsRunLost = true;
    }

    private readonly IRng _rng;
    private readonly IPersistence _persistence;
    private readonly IContentProvider _content;
    private SaveData _saveData;
    private int _cashCollectedThisRun = 0;
    private bool _wasRunLostLastFrame = false;
    private bool _wasRunCompleteLastFrame = false;
    public List<Enemy> Enemies { get; } = new();
    private float _spawnTimer = 0f;
    private const float MinSpawnRadius = 450f;
    private const float MaxSpawnRadius = 800f;

    public Simulation(IRng rng, IPersistence persistence, IContentProvider content) {
        _rng = rng;
        _persistence = persistence;
        _content = content;
        _saveData = _persistence.Load() ?? new SaveData();
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

    public string GetUpgradeOptionLabel(UpgradeOption option) {
        if (option == null) return "";
        switch (option.Type) {
            case UpgradeType.NewWeapon: {
                var def = _content.GetWeaponDefinition(option.ItemId ?? "");
                var name = (def?.Name ?? option.ItemId ?? "Unknown Weapon").ToUpperInvariant();
                return $"{name} - NEW";
            }
            case UpgradeType.WeaponUpgrade: {
                var equipped = EquippedWeapons.Find(w => w.Definition.Id == option.ItemId);
                int currentLevel = equipped?.Level ?? 1;
                int targetLevel = currentLevel + 1;
                var def = _content.GetWeaponDefinition(option.ItemId ?? "");
                var name = (def?.Name ?? option.ItemId ?? "Unknown Weapon").ToUpperInvariant();
                return $"{name} - LV {currentLevel}->{targetLevel}";
            }
            case UpgradeType.NewPassive: {
                var def = _content.GetPassiveDefinition(option.ItemId ?? "");
                var name = (def?.Name ?? option.ItemId ?? "Unknown Passive").ToUpperInvariant();
                return $"{name} - NEW";
            }
            case UpgradeType.PassiveUpgrade: {
                var equipped = EquippedPassives.Find(p => p.Definition.Id == option.ItemId);
                int currentLevel = equipped?.Level ?? 1;
                int targetLevel = currentLevel + 1;
                var def = _content.GetPassiveDefinition(option.ItemId ?? "");
                var name = (def?.Name ?? option.ItemId ?? "Unknown Passive").ToUpperInvariant();
                return $"{name} - LV {currentLevel}->{targetLevel}";
            }
            case UpgradeType.CashFallback: {
                return "+25 JURASSIC CASH";
            }
            default:
                return "";
        }
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
                _cashCollectedThisRun += 25;
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
        if (IsRunLost || IsPaused || IsPausedForLevelUp || IsPausedForSafehouseBreak) {
            return;
        }

        StageTimeElapsed += deltaTime;

        if (!IsStageExitRevealed && StageTimeElapsed >= StageExitRevealTime && StageNumber < HeliportStageNumber)
            RevealSafehouse();

        // Firing logic for equipped weapons
        foreach (var weapon in EquippedWeapons) {
            if (weapon.CooldownTimer > 0f) {
                weapon.CooldownTimer -= deltaTime;
                if (weapon.CooldownTimer < 0f) {
                    weapon.CooldownTimer = 0f;
                }
            }

            if (weapon.CooldownTimer <= 0f) {
                bool canFire = IsAutoFireEnabled || actions.Fire;
                if (weapon.Definition.IsAimed) {
                    if (canFire && actions.AimDirection != Vector2.Zero) {
                        var aimDir = actions.AimDirection;
                        if (aimDir.LengthSquared() > 1f) {
                            aimDir = Vector2.Normalize(aimDir);
                        }
                        var data = weapon.CurrentLevelData;
                        var effectiveDamage = data.Damage * GetPermanentUpgradeMultiplier(PassiveStat.Damage) * GetPassiveMultiplier(PassiveStat.Damage);
                        Projectiles.Add(new Projectile(
                            PlayerPosition,
                            aimDir,
                            data.ProjectileSpeed,
                            data.ProjectileRadius,
                            effectiveDamage,
                            data.PierceCount,
                            data.ExplosionRadius
                        ));
                        weapon.CooldownTimer = data.Cooldown * GetPermanentUpgradeMultiplier(PassiveStat.WeaponCooldown) * GetPassiveMultiplier(PassiveStat.WeaponCooldown);
                    }
                } else {
                    // Autonomous area zapper behavior
                    var data = weapon.CurrentLevelData;
                    var autonomousDamage = data.Damage * GetPermanentUpgradeMultiplier(PassiveStat.Damage) * GetPassiveMultiplier(PassiveStat.Damage);
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
                    weapon.CooldownTimer = data.Cooldown * GetPermanentUpgradeMultiplier(PassiveStat.WeaponCooldown) * GetPassiveMultiplier(PassiveStat.WeaponCooldown);
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
                _cashCollectedThisRun += drop.CashValue;
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

        if (StageNumber < HeliportStageNumber && IsStageExitRevealed && !IsRunLost &&
            Vector2.Distance(PlayerPosition, SafehousePosition) <= SafehouseZoneRadius)
            EnterSafehouse();

        // Heliport stage: T-Rex boss fight
        if (StageNumber == HeliportStageNumber && TRex != null)
            UpdateHeliportStage(TRex, deltaTime);
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

    private void RevealSafehouse() {
        int edge = _rng.Next(0, 4);
        float t = (float)_rng.NextDouble();
        SafehousePosition = edge switch {
            0 => new Vector2(t * ArenaSize.X, 0),
            1 => new Vector2(ArenaSize.X, t * ArenaSize.Y),
            2 => new Vector2(t * ArenaSize.X, ArenaSize.Y),
            _ => new Vector2(0, t * ArenaSize.Y)
        };
        IsStageExitRevealed = true;
    }

    private void EnterSafehouse() {
        BankedJurassicCash += UnbankedJurassicCash;
        UnbankedJurassicCash = 0;
        SaveSimulationData();
        XpGems.Clear();
        JurassicCashDrops.Clear();
        Enemies.Clear();
        Projectiles.Clear();
        _spawnTimer = 0f;

        PendingSafehouseBreakOptions.Clear();
        PendingSafehouseBreakOptions.Add(new SafehouseRewardOption { Type = SafehouseRewardType.PartialHeal, Amount = 20f });
        PendingSafehouseBreakOptions.Add(new SafehouseRewardOption { Type = SafehouseRewardType.BankedCashBonus, Amount = 50f });
        PendingSafehouseBreakOptions.Add(new SafehouseRewardOption { Type = SafehouseRewardType.BonusXp, Amount = 50f });

        IsPausedForSafehouseBreak = true;
    }

    public void SelectSafehouseRewardOption(int index) {
        if (!IsPausedForSafehouseBreak || index < 0 || index >= PendingSafehouseBreakOptions.Count) return;

        var option = PendingSafehouseBreakOptions[index];
        switch (option.Type) {
            case SafehouseRewardType.PartialHeal:
                PlayerCurrentHp = System.Math.Min(PlayerCurrentHp + option.Amount, PlayerEffectiveMaxHp);
                break;
            case SafehouseRewardType.BankedCashBonus:
                BankedJurassicCash += (int)option.Amount;
                _cashCollectedThisRun += (int)option.Amount;
                SaveSimulationData();
                break;
            case SafehouseRewardType.BonusXp:
                PlayerXp += option.Amount;
                if (!IsPausedForLevelUp && PlayerXp >= XpToNextLevel) {
                    PendingLevelUpOptions.AddRange(GenerateLevelUpOptions());
                    IsPausedForLevelUp = true;
                }
                break;
        }

        PendingSafehouseBreakOptions.Clear();
        IsPausedForSafehouseBreak = false;

        if (StageNumber < 3) {
            StageNumber++;
            StageTimeElapsed = 0f;
            IsStageExitRevealed = false;
        } else if (StageNumber == 3) {
            StageNumber = HeliportStageNumber;
            StageTimeElapsed = 0f;
            IsStageExitRevealed = false;
            IsBossArenaLocked = false;
            TRex = new TRex(BossArenaCenter);
        } else {
            IsRunComplete = true;
        }
    }

    private static readonly (TRexAttackState State, float Duration)[][] TRexPhaseSequences = {
        new[] { // Phase 1: Idle → Charge → Idle → Summon
            (TRexAttackState.Idle, 1.5f),
            (TRexAttackState.Charging, 2.0f),
            (TRexAttackState.Idle, 1.5f),
            (TRexAttackState.SummoningWaves, 2.0f),
        },
        new[] { // Phase 2: adds Roar
            (TRexAttackState.Idle, 1.5f),
            (TRexAttackState.Charging, 2.0f),
            (TRexAttackState.Idle, 1.0f),
            (TRexAttackState.Roaring, 2.5f),
            (TRexAttackState.Idle, 1.0f),
            (TRexAttackState.SummoningWaves, 2.0f),
        },
        new[] { // Phase 3: adds TailSweep, faster pace
            (TRexAttackState.Idle, 1.0f),
            (TRexAttackState.Charging, 2.0f),
            (TRexAttackState.Idle, 0.5f),
            (TRexAttackState.Roaring, 2.5f),
            (TRexAttackState.Idle, 0.5f),
            (TRexAttackState.TailSweeping, 1.5f),
            (TRexAttackState.Idle, 0.5f),
            (TRexAttackState.SummoningWaves, 2.0f),
        }
    };

    private void UpdateHeliportStage(TRex trex, float deltaTime) {
        // Trigger lock-in when player enters boss arena
        if (!IsBossArenaLocked && !trex.IsDefeated &&
            Vector2.Distance(PlayerPosition, BossArenaCenter) <= BossArenaRadius)
            IsBossArenaLocked = true;

        if (!trex.IsDefeated) {
            UpdateTRex(trex, deltaTime);

            // Projectile hits on T-Rex
            for (int i = Projectiles.Count - 1; i >= 0; i--) {
                var proj = Projectiles[i];
                if (Vector2.Distance(proj.Position, trex.Position) < proj.Radius + trex.Radius) {
                    trex.Hp -= proj.Damage;
                    trex.HitFlashTimer = 0.15f;
                    Projectiles.RemoveAt(i);
                    if (trex.IsDefeated) {
                        IsBossArenaLocked = false;
                        OnTRexDefeated?.Invoke();
                        break;
                    }
                }
            }

            // T-Rex contact damage to player
            if (!trex.IsDefeated && !IsRunLost) {
                const float playerRadius = 16f;
                var distToPlayer = Vector2.Distance(PlayerPosition, trex.Position);
                if (distToPlayer < trex.Radius + playerRadius) {
                    if (trex.ContactCooldownTimer <= 0f) {
                        PlayerCurrentHp -= trex.Damage;
                        if (PlayerCurrentHp <= 0f) {
                            PlayerCurrentHp = 0f;
                            IsRunLost = true;
                            UnbankedJurassicCash = 0;
                        }
                        trex.ContactCooldownTimer = trex.ContactCooldown;
                        OnPlayerDamaged?.Invoke();
                    }
                    if (distToPlayer > 0.0001f) {
                        var pushDir = Vector2.Normalize(PlayerPosition - trex.Position);
                        PlayerPosition += pushDir * (trex.Radius + playerRadius - distToPlayer) * 0.2f;
                    }
                }
            }
        }

        // Boss arena clamp while locked in
        if (IsBossArenaLocked) {
            var fromCenter = PlayerPosition - BossArenaCenter;
            if (fromCenter.LengthSquared() > BossArenaRadius * BossArenaRadius)
                PlayerPosition = BossArenaCenter + Vector2.Normalize(fromCenter) * BossArenaRadius;
        }

        // Win condition: touch chopper zone after T-Rex defeated
        if (!IsRunLost && trex.IsDefeated &&
            Vector2.Distance(PlayerPosition, ChopperZonePosition) <= ChopperZoneRadius) {
            BankedJurassicCash += UnbankedJurassicCash;
            UnbankedJurassicCash = 0;
            IsRunComplete = true;
        }

        if (IsRunLost && !_wasRunLostLastFrame) {
            _wasRunLostLastFrame = true;
            UpdateBestRunSummary();
        }
        if (IsRunComplete && !_wasRunCompleteLastFrame) {
            _wasRunCompleteLastFrame = true;
            UpdateBestRunSummary();
        }
    }

    private void UpdateTRex(TRex trex, float deltaTime) {
        if (trex.ContactCooldownTimer > 0f)
            trex.ContactCooldownTimer = MathF.Max(0f, trex.ContactCooldownTimer - deltaTime);
        if (trex.HitFlashTimer > 0f)
            trex.HitFlashTimer = MathF.Max(0f, trex.HitFlashTimer - deltaTime);

        // Detect phase change and reset attack sequence
        int phase = trex.Phase;
        if (phase != trex.LastPhase) {
            trex.LastPhase = phase;
            trex.AttackSequenceIndex = 0;
            trex.AttackTimer = 0f;
            trex.AttackState = TRexAttackState.Idle;
        }

        var sequence = TRexPhaseSequences[phase - 1];
        var (targetState, duration) = sequence[trex.AttackSequenceIndex];

        // On state entry: fire enter logic
        if (trex.AttackState != targetState) {
            trex.AttackState = targetState;
            OnEnterTRexState(trex, targetState);
        }

        // Per-frame state behavior
        switch (trex.AttackState) {
            case TRexAttackState.Charging: {
                var toPlayer = PlayerPosition - trex.Position;
                if (toPlayer.LengthSquared() > 0.0001f)
                    trex.Position += Vector2.Normalize(toPlayer) * trex.ChargeSpeed * deltaTime;
                trex.Position = Vector2.Clamp(trex.Position, Vector2.Zero, ArenaSize);
                break;
            }
            case TRexAttackState.Roaring: {
                if (!IsRunLost && Vector2.Distance(PlayerPosition, trex.Position) <= trex.RoarRadius) {
                    PlayerCurrentHp -= trex.RoarDamagePerSecond * deltaTime;
                    if (PlayerCurrentHp <= 0f) {
                        PlayerCurrentHp = 0f;
                        IsRunLost = true;
                        UnbankedJurassicCash = 0;
                    }
                    OnPlayerDamaged?.Invoke();
                }
                break;
            }
        }

        // Advance attack timer
        trex.AttackTimer += deltaTime;
        if (trex.AttackTimer >= duration) {
            trex.AttackTimer -= duration;
            trex.AttackSequenceIndex = (trex.AttackSequenceIndex + 1) % sequence.Length;
        }
    }

    private void OnEnterTRexState(TRex trex, TRexAttackState state) {
        switch (state) {
            case TRexAttackState.TailSweeping: {
                // Instant radial damage to player within sweep radius
                var fromTrex = PlayerPosition - trex.Position;
                if (!IsRunLost && fromTrex.LengthSquared() <= trex.TailSweepRadius * trex.TailSweepRadius) {
                    PlayerCurrentHp -= trex.TailSweepDamage;
                    if (PlayerCurrentHp <= 0f) {
                        PlayerCurrentHp = 0f;
                        IsRunLost = true;
                        UnbankedJurassicCash = 0;
                    }
                    OnPlayerDamaged?.Invoke();
                }
                break;
            }
            case TRexAttackState.SummoningWaves: {
                for (int i = 0; i < trex.SummonCount; i++) {
                    double angle = _rng.NextDouble() * 2 * System.Math.PI;
                    var offset = new Vector2(
                        (float)(System.Math.Cos(angle) * 120f),
                        (float)(System.Math.Sin(angle) * 120f));
                    var spawnPos = Vector2.Clamp(trex.Position + offset, Vector2.Zero, ArenaSize);
                    Enemies.Add(new Enemy(spawnPos, EnemyType.Raptor));
                }
                break;
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
