using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure builder that derives a <see cref="ScriptEvaluationContext"/> from a
/// <see cref="MatchSensorRuntimeState"/> snapshot and evaluating tank index.
/// </summary>
/// <remarks>
/// This type computes hit points, weapon readiness, sensor readiness, and a
/// minimal enemy visibility/distance view from existing runtime state only.
/// It does not execute scripts, execute sensor scans, fire weapons, move tanks,
/// mutate <see cref="MatchState"/> or sensor runtime state, create translated
/// command requests, log, diagnose, replay, or integrate Godot.
/// </remarks>
public static class ScriptRuntimeContextBuilder
{
    /// <summary>
    /// Builds a script evaluation context from match sensor runtime state for the
    /// tank at <paramref name="tankIndex"/>.
    /// </summary>
    /// <param name="runtime">Sensor runtime snapshot; must not be null.</param>
    /// <param name="tankIndex">Index into <see cref="MatchSensorRuntimeState.State"/> tanks.</param>
    /// <returns>A derived <see cref="ScriptEvaluationContext"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="runtime"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tankIndex"/> is out of range.</exception>
    public static ScriptEvaluationContext Build(
        MatchSensorRuntimeState runtime,
        int tankIndex)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        ValidateTankIndex(runtime, tankIndex);

        ScriptVisibleTurretAimStatusResult turretAimStatus =
            ScriptVisibleTurretAimStatusResolver.Resolve(runtime.State, tankIndex);

        TankState tank = runtime.State.Tanks[tankIndex];
        int myHitPoints = tank.CurrentHitPoints;

        if (tank.IsDestroyed)
        {
            return new ScriptEvaluationContext(
                enemyVisible: false,
                weaponReady: false,
                myHitPoints: myHitPoints,
                enemyDistance: Fixed.Zero,
                sensorReady: false,
                turretAimStatus);
        }

        bool weaponReady = HasReadyWeapon(runtime, tankIndex);
        bool sensorReady = HasReadySensor(runtime, tankIndex);
        EnemyVisibilityResult visibility = FindNearestVisibleEnemy(runtime, tankIndex);

        return new ScriptEvaluationContext(
            visibility.EnemyVisible,
            weaponReady,
            myHitPoints,
            visibility.EnemyDistance,
            sensorReady,
            turretAimStatus);
    }

    private static void ValidateTankIndex(MatchSensorRuntimeState runtime, int tankIndex)
    {
        if (tankIndex < 0 || tankIndex >= runtime.State.Tanks.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(tankIndex), tankIndex, null);
        }
    }

    private static bool HasReadyWeapon(MatchSensorRuntimeState runtime, int tankIndex)
    {
        SimTick tick = runtime.State.CurrentTick;
        TankWeaponLoadout loadout = runtime.State.Loadouts[tankIndex];

        for (int i = 0; i < loadout.Weapons.Count; i++)
        {
            if (loadout.GetWeapon(new WeaponSlot(i)).IsReady(tick))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasReadySensor(MatchSensorRuntimeState runtime, int tankIndex)
    {
        SimTick tick = runtime.State.CurrentTick;
        TankSensorLoadout loadout = runtime.SensorLoadouts.GetLoadoutAtIndex(tankIndex);

        for (int i = 0; i < loadout.Sensors.Count; i++)
        {
            if (loadout.GetSensor(new SensorSlot(i)).IsReady(tick))
            {
                return true;
            }
        }

        return false;
    }

    private static EnemyVisibilityResult FindNearestVisibleEnemy(
        MatchSensorRuntimeState runtime,
        int tankIndex)
    {
        MatchState state = runtime.State;
        SimTick tick = state.CurrentTick;
        TankState self = state.Tanks[tankIndex];
        TankSensorLoadout sensorLoadout = runtime.SensorLoadouts.GetLoadoutAtIndex(tankIndex);
        FixedVec2 selfPos = self.Movement.Position;

        int bestEnemyIndex = -1;
        Fixed bestDistance = default;

        for (int e = 0; e < state.Tanks.Count; e++)
        {
            if (e == tankIndex)
            {
                continue;
            }

            TankState enemy = state.Tanks[e];

            if (enemy.IsDestroyed)
            {
                continue;
            }

            if (enemy.OwnerSlot == self.OwnerSlot)
            {
                continue;
            }

            Fixed distanceSquared =
                selfPos.DistanceSquaredTo(enemy.Movement.Position);

            if (!IsEnemyWithinAnyReadySensorRange(sensorLoadout, tick, distanceSquared))
            {
                continue;
            }

            Fixed distance = FloorSqrt(distanceSquared);

            if (bestEnemyIndex < 0
                || distance < bestDistance
                || (distance == bestDistance && e < bestEnemyIndex))
            {
                bestEnemyIndex = e;
                bestDistance = distance;
            }
        }

        if (bestEnemyIndex < 0)
        {
            return new EnemyVisibilityResult(false, Fixed.Zero);
        }

        return new EnemyVisibilityResult(true, bestDistance);
    }

    private static bool IsEnemyWithinAnyReadySensorRange(
        TankSensorLoadout sensorLoadout,
        SimTick tick,
        Fixed distanceSquared)
    {
        for (int i = 0; i < sensorLoadout.Sensors.Count; i++)
        {
            SensorState sensor = sensorLoadout.GetSensor(new SensorSlot(i));

            if (!sensor.IsReady(tick))
            {
                continue;
            }

            Fixed rangeSquared =
                sensor.Definition.Range * sensor.Definition.Range;

            if (distanceSquared <= rangeSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static Fixed FloorSqrt(Fixed value)
    {
        if (value <= Fixed.Zero)
        {
            return Fixed.Zero;
        }

        long low = 0;
        long high = System.Math.Max(value.Raw, Fixed.FromInt(1).Raw);
        long best = 0;

        while (low <= high)
        {
            long mid = low + ((high - low) / 2);
            Fixed midFixed = Fixed.FromRaw(mid);
            Fixed squared = midFixed * midFixed;

            if (squared <= value)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return Fixed.FromRaw(best);
    }

    private readonly struct EnemyVisibilityResult
    {
        public bool EnemyVisible { get; }

        public Fixed EnemyDistance { get; }

        public EnemyVisibilityResult(bool enemyVisible, Fixed enemyDistance)
        {
            EnemyVisible = enemyVisible;
            EnemyDistance = enemyDistance;
        }
    }
}
