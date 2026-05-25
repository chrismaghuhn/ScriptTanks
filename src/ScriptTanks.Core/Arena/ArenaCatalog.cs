using System;
using System.Collections.Generic;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Arena;

/// <summary>
/// Built-in catalog of deterministic <see cref="ArenaDefinition"/> instances
/// used by movement, wall, projectile, patrol and replay/debug tests as well
/// as the upcoming Godot replay viewer. The catalog exposes a stable,
/// deterministic order of arenas through <see cref="All"/> and a strict
/// ordinal id lookup through <see cref="GetById"/>. No file/JSON loading,
/// no procedural generation, and no simulation behavior is performed here.
/// </summary>
public static class ArenaCatalog
{
    public static ArenaDefinition OpenTestArena { get; } = CreateOpenTestArena();

    public static ArenaDefinition ObstacleTestArena { get; } = CreateObstacleTestArena();

    public static IReadOnlyList<ArenaDefinition> All { get; } =
        Array.AsReadOnly(new[]
        {
            OpenTestArena,
            ObstacleTestArena,
        });

    public static ArenaDefinition GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        foreach (ArenaDefinition arena in All)
        {
            if (string.Equals(arena.Id, id, StringComparison.Ordinal))
            {
                return arena;
            }
        }

        throw new KeyNotFoundException($"No arena definition found for id '{id}'.");
    }

    private static ArenaDefinition CreateOpenTestArena()
    {
        return new ArenaDefinition(
            id: "open_test_arena",
            displayName: "Open Test Arena",
            description: "Open arena for validating basic movement, sensors, weapons, and replay/debug behavior.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 30)),
                new ArenaStartPosition(new PlayerSlot(1), FixedVec2.FromInts(90, 30)),
            },
            wallBlocks: Array.Empty<WallBlock>(),
            patrolPoints: new[]
            {
                FixedVec2.FromInts(25, 30),
                FixedVec2.FromInts(50, 30),
                FixedVec2.FromInts(75, 30),
            },
            tags: new[] { "test", "open" });
    }

    private static ArenaDefinition CreateObstacleTestArena()
    {
        return new ArenaDefinition(
            id: "obstacle_test_arena",
            displayName: "Obstacle Test Arena",
            description: "Arena with simple rectangular wall blocks for validating deterministic wall, projectile, patrol, and replay/debug behavior.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(10, 10)),
                new ArenaStartPosition(new PlayerSlot(1), FixedVec2.FromInts(90, 50)),
            },
            wallBlocks: new[]
            {
                new WallBlock(
                    "center_wall",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(45, 25),
                        Fixed.FromInt(10),
                        Fixed.FromInt(10))),
                new WallBlock(
                    "north_block",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(25, 45),
                        Fixed.FromInt(15),
                        Fixed.FromInt(5))),
                new WallBlock(
                    "south_block",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(60, 10),
                        Fixed.FromInt(15),
                        Fixed.FromInt(5))),
            },
            patrolPoints: new[]
            {
                FixedVec2.FromInts(20, 20),
                FixedVec2.FromInts(50, 15),
                FixedVec2.FromInts(80, 40),
            },
            tags: new[] { "test", "obstacle" });
    }
}
