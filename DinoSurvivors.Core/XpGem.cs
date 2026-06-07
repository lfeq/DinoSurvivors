using System.Numerics;

namespace DinoSurvivors.Core;

public class XpGem {
    public Vector2 Position { get; }
    public float XpValue { get; }

    public XpGem(Vector2 position, float xpValue = 10f) {
        Position = position;
        XpValue = xpValue;
    }
}
