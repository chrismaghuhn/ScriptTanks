using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;

namespace ScriptTanks.Core.Tanks;

/// <summary>
/// Deterministic helper for creating initial runtime tank states from a
/// tank definition and an arena start position. The factory is intentionally
/// minimal: it does not generate ids, validate arena occupancy, resolve
/// collisions, check player-slot uniqueness, or perform any match setup.
/// HP-range validation is delegated to the <see cref="TankState"/>
/// constructor.
/// </summary>
public static class TankSpawnFactory
{
    public static TankState Create(
        TankId id,
        TankDefinition definition,
        ArenaStartPosition startPosition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        MovementState movement = new MovementState(
            position: startPosition.Position,
            velocityPerTick: FixedVec2.Zero);

        return new TankState(
            id: id,
            ownerSlot: startPosition.Slot,
            definition: definition,
            movement: movement,
            currentHitPoints: definition.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }
}
