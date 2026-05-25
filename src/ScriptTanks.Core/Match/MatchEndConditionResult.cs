using System;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable result of <see cref="MatchEndConditionEvaluator.Evaluate(MatchState, int)"/>.
/// Bundles whether the match has ended (<see cref="IsEnded"/>), why
/// (<see cref="Reason"/>), and, if applicable, the winning tank's identity
/// (<see cref="WinnerTankId"/>, <see cref="WinnerSlot"/>).
/// <para>
/// Construction is restricted to the named factory methods
/// <see cref="Running"/>, <see cref="TankDestroyed(TankState)"/>,
/// <see cref="TimeoutHpAdvantage(TankState)"/>, and <see cref="TimeoutDraw"/>.
/// The private constructor is the single source of truth for invariants and
/// throws <see cref="ArgumentException"/> for any invalid combination of
/// <c>isEnded</c>, <c>reason</c>, <c>winnerTankId</c>, and <c>winnerSlot</c>.
/// </para>
/// <para>
/// This type is a pure data carrier. It does not mutate match state, run
/// gameplay systems, advance ticks, create combat logs, create replay
/// frames, determine teams, compute rankings, serialize state, or interact
/// with Godot.
/// </para>
/// </summary>
public sealed class MatchEndConditionResult
{
    public bool IsEnded { get; }

    public MatchEndReason Reason { get; }

    public TankId? WinnerTankId { get; }

    public PlayerSlot? WinnerSlot { get; }

    private MatchEndConditionResult(
        bool isEnded,
        MatchEndReason reason,
        TankId? winnerTankId,
        PlayerSlot? winnerSlot)
    {
        if (winnerTankId.HasValue != winnerSlot.HasValue)
        {
            throw new ArgumentException(
                "WinnerTankId and WinnerSlot must both be set or both be null.");
        }

        bool hasWinner = winnerTankId.HasValue;

        if (!isEnded)
        {
            if (reason != MatchEndReason.None || hasWinner)
            {
                throw new ArgumentException(
                    "A non-ended result must have Reason=None and no winner.");
            }
        }
        else
        {
            switch (reason)
            {
                case MatchEndReason.None:
                    throw new ArgumentException(
                        "An ended result must not use Reason=None.");
                case MatchEndReason.TankDestroyed:
                case MatchEndReason.TimeoutHpAdvantage:
                    if (!hasWinner)
                    {
                        throw new ArgumentException(
                            "TankDestroyed and TimeoutHpAdvantage results must carry a winner.");
                    }
                    break;
                case MatchEndReason.TimeoutDraw:
                    if (hasWinner)
                    {
                        throw new ArgumentException(
                            "TimeoutDraw results must not carry a winner.");
                    }
                    break;
                default:
                    throw new ArgumentException(
                        $"Unknown MatchEndReason value: {reason}.");
            }
        }

        IsEnded = isEnded;
        Reason = reason;
        WinnerTankId = winnerTankId;
        WinnerSlot = winnerSlot;
    }

    public static MatchEndConditionResult Running()
        => new MatchEndConditionResult(
            isEnded: false,
            reason: MatchEndReason.None,
            winnerTankId: null,
            winnerSlot: null);

    public static MatchEndConditionResult TankDestroyed(TankState winner)
        => new MatchEndConditionResult(
            isEnded: true,
            reason: MatchEndReason.TankDestroyed,
            winnerTankId: winner.Id,
            winnerSlot: winner.OwnerSlot);

    public static MatchEndConditionResult TimeoutHpAdvantage(TankState winner)
        => new MatchEndConditionResult(
            isEnded: true,
            reason: MatchEndReason.TimeoutHpAdvantage,
            winnerTankId: winner.Id,
            winnerSlot: winner.OwnerSlot);

    public static MatchEndConditionResult TimeoutDraw()
        => new MatchEndConditionResult(
            isEnded: true,
            reason: MatchEndReason.TimeoutDraw,
            winnerTankId: null,
            winnerSlot: null);
}
