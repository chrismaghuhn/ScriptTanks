using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Resolves script movement command kinds to a deterministic per-tick velocity from
/// body-forward direction and tank movement limits.
/// </summary>
/// <remarks>
/// Uses <see cref="TankState.BodyRotation"/> only (not <see cref="TankState.TurretRotation"/>).
/// Does not mutate <see cref="Match.MatchState"/>, call <see cref="Movement.MovementIntegrator"/>,
/// or advance simulation ticks.
/// </remarks>
internal static class ScriptMovementVelocityResolver
{
    public static bool TryResolve(
        TankState tank,
        ScriptTranslatedCommandRequestKind kind,
        out FixedVec2 velocity)
    {
        FixedRotationDirectionResult forwardResult =
            FixedRotationDirectionResolver.ResolveForward(tank.BodyRotation);

        if (!forwardResult.IsResolved)
        {
            velocity = default;
            return false;
        }

        FixedVec2 forward = forwardResult.Forward!.Value;
        Fixed maxSpeed = tank.Definition.Stats.MaxVelocityPerTick;

        switch (kind)
        {
            case ScriptTranslatedCommandRequestKind.MoveToPatrolPoint:
                velocity = forward * maxSpeed;
                return true;

            case ScriptTranslatedCommandRequestKind.Retreat:
                velocity = -forward * maxSpeed;
                return true;

            default:
                velocity = default;
                return false;
        }
    }
}
