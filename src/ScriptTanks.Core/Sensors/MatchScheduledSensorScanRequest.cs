using System;
using System.Globalization;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Sensors;

/// <summary>
/// Immutable value object that pairs a simulation tick with a
/// <see cref="MatchSensorScanRequest"/> for a conceptually scheduled scan.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure scheduled scan request model. It does not validate the tick
/// against a current match or runtime tick, and it does not execute scans.
/// </para>
/// <para>
/// It does not queue, sort, or schedule work by itself, and it does not
/// integrate with AI or script runtimes, match runners, replay systems,
/// logging, diagnostics, or Godot.
/// </para>
/// </remarks>
public readonly struct MatchScheduledSensorScanRequest
    : IEquatable<MatchScheduledSensorScanRequest>
{
    public SimTick Tick { get; }

    public MatchSensorScanRequest Request { get; }

    public MatchScheduledSensorScanRequest(
        SimTick tick,
        MatchSensorScanRequest request)
    {
        Tick = tick;
        Request = request;
    }

    public bool Equals(MatchScheduledSensorScanRequest other)
        => Tick == other.Tick && Request == other.Request;

    public override bool Equals(object? obj)
        => obj is MatchScheduledSensorScanRequest other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Tick, Request);

    public static bool operator ==(
        MatchScheduledSensorScanRequest left,
        MatchScheduledSensorScanRequest right)
        => left.Equals(right);

    public static bool operator !=(
        MatchScheduledSensorScanRequest left,
        MatchScheduledSensorScanRequest right)
        => !left.Equals(right);

    public override string ToString()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"MatchScheduledSensorScanRequest {{ Tick = {Tick}, Request = {Request} }}");
}
