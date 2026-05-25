namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Categorizes the outcome of a single <see cref="ProjectileHitDetection"/>
/// query. The enum carries no payload; identifying details (e.g. the hit
/// tank or wall block) live on <see cref="ProjectileHitResult"/>.
/// </summary>
public enum ProjectileHitKind
{
    None,
    Wall,
    Tank,
}
