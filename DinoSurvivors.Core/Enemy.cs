using System.Numerics;

namespace DinoSurvivors.Core;

public class Enemy {
    public Vector2 Position { get; set; }
    public float Speed { get; } = 120f;
    public float Radius { get; } = 12f;
    public float Damage { get; } = 10f;
    public float ContactCooldown { get; } = 1.0f;
    public float ContactCooldownTimer { get; set; } = 0f;
    public float Hp { get; set; } = 20f;
    public float MaxHp { get; } = 20f;
    public float HitFlashTimer { get; set; } = 0f;

    public Enemy(Vector2 position) {
        Position = position;
    }
}
