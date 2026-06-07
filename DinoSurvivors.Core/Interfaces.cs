namespace DinoSurvivors.Core;

public interface IRng {
    double NextDouble();
    int Next(int minValue, int maxValue);
}

public interface IPersistence {
}

public class SystemRng : IRng {
    private readonly System.Random _random;

    public SystemRng(int seed) {
        _random = new System.Random(seed);
    }

    public double NextDouble() => _random.NextDouble();
    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
}

public class NullPersistence : IPersistence {
}

public class WeaponLevelData {
    public float Cooldown { get; set; }
    public float Damage { get; set; }
    public float RangeOrRadius { get; set; }
    public float ProjectileSpeed { get; set; }
    public float ProjectileRadius { get; set; }
    public int PierceCount { get; set; }
    public float ExplosionRadius { get; set; }
}

public class WeaponDefinition {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsAimed { get; set; }
    public List<WeaponLevelData> Levels { get; set; } = new();
}

public interface IContentProvider {
    WeaponDefinition? GetWeaponDefinition(string id);
    IEnumerable<WeaponDefinition> GetAllWeapons();
}

public class NullContentProvider : IContentProvider {
    private readonly Dictionary<string, WeaponDefinition> _weapons;

    public NullContentProvider() {
        _weapons = DefaultContent.Weapons;
    }

    public WeaponDefinition? GetWeaponDefinition(string id) {
        return _weapons.TryGetValue(id, out var def) ? def : null;
    }

    public IEnumerable<WeaponDefinition> GetAllWeapons() => _weapons.Values;
}

public static class DefaultContent {
    public static Dictionary<string, WeaponDefinition> Weapons { get; } = new() {
        {
            "TranqPistol", new WeaponDefinition {
                Id = "TranqPistol",
                Name = "Tranq Pistol",
                IsAimed = true,
                Levels = new List<WeaponLevelData> {
                    new() { Cooldown = 0.8f, Damage = 10f, ProjectileSpeed = 600f, ProjectileRadius = 6f, PierceCount = 1 },
                    new() { Cooldown = 0.7f, Damage = 12f, ProjectileSpeed = 600f, ProjectileRadius = 6f, PierceCount = 1 },
                    new() { Cooldown = 0.6f, Damage = 15f, ProjectileSpeed = 600f, ProjectileRadius = 6f, PierceCount = 1 },
                    new() { Cooldown = 0.5f, Damage = 18f, ProjectileSpeed = 600f, ProjectileRadius = 6f, PierceCount = 1 },
                    new() { Cooldown = 0.4f, Damage = 22f, ProjectileSpeed = 600f, ProjectileRadius = 6f, PierceCount = 1 }
                }
            }
        },
        {
            "FlareGun", new WeaponDefinition {
                Id = "FlareGun",
                Name = "Flare Gun",
                IsAimed = true,
                Levels = new List<WeaponLevelData> {
                    new() { Cooldown = 1.5f, Damage = 20f, ProjectileSpeed = 500f, ProjectileRadius = 10f, PierceCount = 3 },
                    new() { Cooldown = 1.4f, Damage = 25f, ProjectileSpeed = 500f, ProjectileRadius = 10f, PierceCount = 4 },
                    new() { Cooldown = 1.3f, Damage = 32f, ProjectileSpeed = 500f, ProjectileRadius = 10f, PierceCount = 5 },
                    new() { Cooldown = 1.2f, Damage = 40f, ProjectileSpeed = 500f, ProjectileRadius = 10f, PierceCount = 6 },
                    new() { Cooldown = 1.0f, Damage = 50f, ProjectileSpeed = 500f, ProjectileRadius = 10f, PierceCount = 8 }
                }
            }
        },
        {
            "BugZapper", new WeaponDefinition {
                Id = "BugZapper",
                Name = "Bug Zapper",
                IsAimed = false,
                Levels = new List<WeaponLevelData> {
                    new() { Cooldown = 2.0f, Damage = 15f, RangeOrRadius = 120f },
                    new() { Cooldown = 1.8f, Damage = 20f, RangeOrRadius = 140f },
                    new() { Cooldown = 1.6f, Damage = 26f, RangeOrRadius = 160f },
                    new() { Cooldown = 1.4f, Damage = 34f, RangeOrRadius = 180f },
                    new() { Cooldown = 1.2f, Damage = 45f, RangeOrRadius = 200f }
                }
            }
        },
        {
            "Laser", new WeaponDefinition {
                Id = "Laser",
                Name = "Laser",
                IsAimed = true,
                Levels = new List<WeaponLevelData> {
                    new() { Cooldown = 1.0f, Damage = 5f, ProjectileSpeed = 800f, ProjectileRadius = 4f, PierceCount = 1 }
                }
            }
        }
    };
}

public class WeaponInstance {
    public WeaponDefinition Definition { get; }
    public int Level { get; set; }
    public float CooldownTimer { get; set; }

    public WeaponInstance(WeaponDefinition definition, int level = 1) {
        Definition = definition;
        Level = level;
        CooldownTimer = 0f;
    }

    public WeaponLevelData CurrentLevelData => Definition.Levels[Level - 1];
}

