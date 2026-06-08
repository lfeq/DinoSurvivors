using System.Numerics;

namespace DinoSurvivors.Core;

public class Enemy {
    public EnemyType Type { get; }
    public Vector2 Position { get; set; }
    public float Speed { get; }
    public float Radius { get; }
    public float Damage { get; }
    public float ContactCooldown { get; } = 1.0f;
    public float ContactCooldownTimer { get; set; } = 0f;
    public float Hp { get; set; }
    public float MaxHp { get; }
    public float HitFlashTimer { get; set; } = 0f;

    public Enemy(Vector2 position, EnemyType type = EnemyType.Compy) {
        Position = position;
        Type = type;
        switch (type) {
            case EnemyType.Raptor:
                Speed = 160f; Radius = 14f; Damage = 15f; MaxHp = 35f; Hp = 35f; break;
            case EnemyType.Triceratops:
                Speed = 60f; Radius = 20f; Damage = 20f; MaxHp = 120f; Hp = 120f; break;
            default:
                Speed = 120f; Radius = 12f; Damage = 10f; MaxHp = 20f; Hp = 20f; break;
        }
    }
}
