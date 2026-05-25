using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable per-tank outcome for one tank bounds correction step produced by
/// <see cref="MatchStateTankBoundsPipeline"/>.
/// </summary>
/// <remarks>
/// Pure data carrier. Does not execute clamping itself, apply wall or obstacle
/// collision, resolve tank-vs-tank collision, resolve projectile hits, advance ticks,
/// log, replay, or integrate Godot.
/// </remarks>
public sealed class MatchStateTankBoundsRecord
{
    public int TankIndex { get; }

    public TankId TankId { get; }

    public MatchStateTankBoundsStatus Status { get; }

    public FixedVec2 InitialPosition { get; }

    public FixedVec2 FinalPosition { get; }

    public bool DidClamp =>
        Status == MatchStateTankBoundsStatus.Clamped;

    public MatchStateTankBoundsRecord(
        int tankIndex,
        TankId tankId,
        MatchStateTankBoundsStatus status,
        FixedVec2 initialPosition,
        FixedVec2 finalPosition)
    {
        if (tankIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index must not be negative.");
        }

        if (!Enum.IsDefined(typeof(MatchStateTankBoundsStatus), status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Tank bounds status must be a defined enum value.");
        }

        ValidatePositionInvariants(status, initialPosition, finalPosition);

        TankIndex = tankIndex;
        TankId = tankId;
        Status = status;
        InitialPosition = initialPosition;
        FinalPosition = finalPosition;
    }

    public static MatchStateTankBoundsRecord SkippedDestroyed(
        int tankIndex,
        TankId tankId,
        FixedVec2 position)
    {
        return new MatchStateTankBoundsRecord(
            tankIndex,
            tankId,
            MatchStateTankBoundsStatus.SkippedDestroyed,
            position,
            position);
    }

    public static MatchStateTankBoundsRecord InsideBounds(
        int tankIndex,
        TankId tankId,
        FixedVec2 position)
    {
        return new MatchStateTankBoundsRecord(
            tankIndex,
            tankId,
            MatchStateTankBoundsStatus.InsideBounds,
            position,
            position);
    }

    public static MatchStateTankBoundsRecord Clamped(
        int tankIndex,
        TankId tankId,
        FixedVec2 initialPosition,
        FixedVec2 finalPosition)
    {
        return new MatchStateTankBoundsRecord(
            tankIndex,
            tankId,
            MatchStateTankBoundsStatus.Clamped,
            initialPosition,
            finalPosition);
    }

    private static void ValidatePositionInvariants(
        MatchStateTankBoundsStatus status,
        FixedVec2 initialPosition,
        FixedVec2 finalPosition)
    {
        switch (status)
        {
            case MatchStateTankBoundsStatus.SkippedDestroyed:
            case MatchStateTankBoundsStatus.InsideBounds:
                if (!initialPosition.Equals(finalPosition))
                {
                    throw new ArgumentException(
                        "Skipped or inside-bounds tank records must have equal initial and final position.",
                        nameof(finalPosition));
                }

                break;

            case MatchStateTankBoundsStatus.Clamped:
                if (initialPosition.Equals(finalPosition))
                {
                    throw new ArgumentException(
                        "Clamped tank bounds records must have different initial and final position.",
                        nameof(finalPosition));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Tank bounds status must be a defined enum value.");
        }
    }
}
