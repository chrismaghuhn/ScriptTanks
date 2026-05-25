using System;
using System.Globalization;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable value object that schedules one explicit
/// <see cref="MatchFireRequest"/> for one specific simulation tick.
/// </summary>
/// <remarks>
/// <para>
/// This type is a pure data carrier. It performs no scheduling consistency
/// checks (duplicate ticks, ordering, future ticks, etc.) - those concerns
/// belong to a future runner-integration task. It does not run gameplay
/// systems, mutate match state, allocate projectile ids, derive muzzle
/// positions or fire velocities, or queue intents.
/// </para>
/// <para>
/// Out of scope: AI / script execution, command queues, fire-intent
/// collection, request-list processing, <see cref="MatchRunner"/> or
/// <see cref="MatchTickFireThenProjectilePipeline"/> integration,
/// projectile-id generation, combat logs, replay frames, serialization,
/// and Godot integration.
/// </para>
/// <para>
/// Equality is value-based: two scheduled requests are equal iff both
/// fields are equal. <see cref="GetHashCode"/> is consistent with
/// <see cref="Equals(MatchScheduledFireRequest)"/>.
/// </para>
/// </remarks>
public readonly struct MatchScheduledFireRequest : IEquatable<MatchScheduledFireRequest>
{
    public SimTick Tick { get; }

    public MatchFireRequest Request { get; }

    public MatchScheduledFireRequest(SimTick tick, MatchFireRequest request)
    {
        Tick = tick;
        Request = request;
    }

    public bool Equals(MatchScheduledFireRequest other)
        => Tick == other.Tick && Request == other.Request;

    public override bool Equals(object? obj)
        => obj is MatchScheduledFireRequest other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Tick, Request);

    public static bool operator ==(MatchScheduledFireRequest left, MatchScheduledFireRequest right)
        => left.Equals(right);

    public static bool operator !=(MatchScheduledFireRequest left, MatchScheduledFireRequest right)
        => !left.Equals(right);

    public override string ToString()
        => string.Format(
            CultureInfo.InvariantCulture,
            "MatchScheduledFireRequest(Tick={0}, Request={1})",
            Tick,
            Request);
}
