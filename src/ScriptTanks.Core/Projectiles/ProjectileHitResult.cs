using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Projectiles;

/// <summary>
/// Immutable result of a projectile hit query produced by
/// <see cref="ProjectileHitDetection"/>. Contains only the hit category and a
/// minimal identifying payload (<see cref="TankId"/> for tank hits,
/// <see cref="WallBlockId"/> for wall hits). The class does not apply
/// damage, deactivate projectiles, raise events, or mutate any match state.
/// </summary>
public sealed class ProjectileHitResult
{
    public static ProjectileHitResult None { get; } =
        new ProjectileHitResult(ProjectileHitKind.None, tankId: null, wallBlockId: null);

    public ProjectileHitKind Kind { get; }

    public bool HasHit => Kind != ProjectileHitKind.None;

    public TankId? TankId { get; }

    public string? WallBlockId { get; }

    private ProjectileHitResult(
        ProjectileHitKind kind,
        TankId? tankId,
        string? wallBlockId)
    {
        Kind = kind;
        TankId = tankId;
        WallBlockId = wallBlockId;
    }

    public static ProjectileHitResult Wall(WallBlock wall)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wall.Id);

        return new ProjectileHitResult(
            ProjectileHitKind.Wall,
            tankId: null,
            wallBlockId: wall.Id);
    }

    public static ProjectileHitResult Tank(TankState tank)
        => new ProjectileHitResult(
            ProjectileHitKind.Tank,
            tankId: tank.Id,
            wallBlockId: null);
}
