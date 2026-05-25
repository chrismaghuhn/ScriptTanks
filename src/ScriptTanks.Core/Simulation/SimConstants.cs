using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Simulation;

/// <summary>
/// Central deterministic time base for the simulation. All systems that depend
/// on time (movement, projectiles, cooldowns, CPU decision timing, replay
/// frames, combat logs) must build on these constants.
/// </summary>
public static class SimConstants
{
    public const int TicksPerSecond = 30;

    public const int DefaultMatchDurationSeconds = 180;

    public const int DefaultMaxTicks = TicksPerSecond * DefaultMatchDurationSeconds;

    /// <summary>
    /// Deterministic helper value: <c>1 / TicksPerSecond</c>.
    /// </summary>
    /// <remarks>
    /// Because <see cref="Fixed.Scale"/> = 1000 and 1/30 truncates to Raw = 33,
    /// repeated per-tick integration with this value accumulates truncation drift
    /// (e.g. 30 ticks sum to 0.990, not 1.000). Movement systems must therefore
    /// prefer tick-native velocities or an explicit deterministic accumulator rule
    /// instead of using <c>velocity * FixedDeltaTime * ticks</c> blindly.
    /// </remarks>
    public static readonly Fixed FixedDeltaTime = Fixed.FromRatio(1, TicksPerSecond);
}
