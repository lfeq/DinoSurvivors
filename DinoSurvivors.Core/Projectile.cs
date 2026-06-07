using System.Collections.Generic;
using System.Numerics;

namespace DinoSurvivors.Core;

public class Projectile {
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; }
    public float Speed { get; }
    public float Radius { get; }
    public float Damage { get; }
    public int PierceCount { get; set; }
    public float ExplosionRadius { get; }
    public HashSet<Enemy> HitEnemies { get; } = new();

    public Projectile(
        Vector2 position, 
        Vector2 direction, 
        float speed = 600f, 
        float radius = 6f, 
        float damage = 10f, 
        int pierceCount = 1,
        float explosionRadius = 0f) 
    {
        Position = position;
        Direction = direction;
        Speed = speed;
        Radius = radius;
        Damage = damage;
        PierceCount = pierceCount;
        ExplosionRadius = explosionRadius;
    }
}
