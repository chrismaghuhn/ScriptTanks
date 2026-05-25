using System;
using ScriptTanks.Core.Match;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable snapshot pair of a <see cref="MatchState"/> and its
/// matching <see cref="MatchSensorLoadoutState"/>.
/// </summary>
/// <remarks>
/// Pure pair + replacement helper only. <see cref="MatchSensorLoadoutState.SensorLoadouts"/>
/// at index <c>i</c> is expected to belong to <see cref="MatchState.Tanks"/> at index
/// <c>i</c>, paired by list order. The constructor validates count compatibility only
/// (<c>state.Tanks.Count == sensorLoadouts.Count</c>); it does not validate
/// <see cref="ScriptTanks.Core.Ids.TankId"/> values, mutate <see cref="MatchState"/>,
/// execute scans, perform visibility checks, integrate with AI/script runtimes,
/// command queues, CPU schedulers, runners, replay systems, logging, diagnostics,
/// Godot, serialization, JSON loading, or UI. Equality is reference-based.
/// </remarks>
public sealed class MatchSensorRuntimeState
{
    public MatchState State { get; }

    public MatchSensorLoadoutState SensorLoadouts { get; }

    public MatchSensorRuntimeState(
        MatchState state,
        MatchSensorLoadoutState sensorLoadouts)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(sensorLoadouts);
        ValidateCountMatch(state, sensorLoadouts, paramName: "sensorLoadouts");

        State = state;
        SensorLoadouts = sensorLoadouts;
    }

    public MatchSensorRuntimeState WithState(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateCountMatch(state, SensorLoadouts, paramName: "state");

        return new MatchSensorRuntimeState(state, SensorLoadouts);
    }

    public MatchSensorRuntimeState WithSensorLoadouts(
        MatchSensorLoadoutState sensorLoadouts)
    {
        ArgumentNullException.ThrowIfNull(sensorLoadouts);
        ValidateCountMatch(State, sensorLoadouts, paramName: "sensorLoadouts");

        return new MatchSensorRuntimeState(State, sensorLoadouts);
    }

    public MatchSensorRuntimeState WithTankSensorLoadoutAtIndex(
        int tankIndex,
        TankSensorLoadout sensorLoadout)
    {
        MatchSensorLoadoutState updated = SensorLoadouts.WithLoadoutAtIndex(
            tankIndex,
            sensorLoadout);

        return new MatchSensorRuntimeState(State, updated);
    }

    private static void ValidateCountMatch(
        MatchState state,
        MatchSensorLoadoutState sensorLoadouts,
        string paramName)
    {
        if (state.Tanks.Count != sensorLoadouts.Count)
        {
            throw new ArgumentException(
                "Sensor loadout count must match the match state tank count.",
                paramName);
        }
    }
}
