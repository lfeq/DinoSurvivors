using System.Numerics;

namespace DinoSurvivors.Core;

public class JurassicCashDrop {
    public Vector2 Position { get; }
    public int CashValue { get; }

    public JurassicCashDrop(Vector2 position, int cashValue = 5) {
        Position = position;
        CashValue = cashValue;
    }
}
