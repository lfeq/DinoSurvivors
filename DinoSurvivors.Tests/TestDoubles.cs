using DinoSurvivors.Core;

namespace DinoSurvivors.Tests;

public class StubRng : IRng {
    public double NextDoubleValue { get; set; } = 0.5;
    public int NextValue { get; set; } = 0;

    public double NextDouble() => NextDoubleValue;
    public int Next(int minValue, int maxValue) => NextValue;
}

public class StubPersistence : IPersistence {
}

public class StubContentProvider : IContentProvider {
    private readonly NullContentProvider _provider = new();

    public WeaponDefinition? GetWeaponDefinition(string id) => _provider.GetWeaponDefinition(id);
    public IEnumerable<WeaponDefinition> GetAllWeapons() => _provider.GetAllWeapons();
}
