using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;

namespace ScriptTanks.Core.Arena;

/// <summary>
/// Immutable, deterministic arena description. Aggregates metadata
/// (<see cref="Id"/>, <see cref="DisplayName"/>, <see cref="Description"/>,
/// <see cref="Tags"/>), the rectangular world <see cref="Bounds"/>, the
/// per-<see cref="PlayerSlot"/> spawn positions, the wall-block layout,
/// and patrol points used by later AI/test scenarios. The class performs
/// strict input validation in the constructor — any caller change after
/// construction is impossible because every collection is defensively
/// copied and exposed only as <see cref="IReadOnlyList{T}"/>. No
/// simulation, no movement blocking, no collision response, no
/// gameplay-level cross-validation (e.g. start-in-wall) is performed
/// here; those concerns belong to later tasks.
/// </summary>
public sealed class ArenaDefinition
{
    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public ArenaBounds Bounds { get; }

    public IReadOnlyList<ArenaStartPosition> StartPositions { get; }

    public IReadOnlyList<WallBlock> WallBlocks { get; }

    public IReadOnlyList<FixedVec2> PatrolPoints { get; }

    public IReadOnlyList<string> Tags { get; }

    public ArenaDefinition(
        string id,
        string displayName,
        string description,
        ArenaBounds bounds,
        IEnumerable<ArenaStartPosition> startPositions,
        IEnumerable<WallBlock> wallBlocks,
        IEnumerable<FixedVec2> patrolPoints,
        IEnumerable<string> tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(startPositions);
        ArgumentNullException.ThrowIfNull(wallBlocks);
        ArgumentNullException.ThrowIfNull(patrolPoints);
        ArgumentNullException.ThrowIfNull(tags);

        ArenaStartPosition[] startArray = startPositions.ToArray();
        WallBlock[] wallArray = wallBlocks.ToArray();
        FixedVec2[] patrolArray = patrolPoints.ToArray();
        string[] tagArray = tags.ToArray();

        if (startArray.Length == 0)
        {
            throw new ArgumentException(
                "At least one start position is required.",
                nameof(startPositions));
        }

        HashSet<PlayerSlot> seenSlots = new HashSet<PlayerSlot>();
        foreach (ArenaStartPosition sp in startArray)
        {
            if (!seenSlots.Add(sp.Slot))
            {
                throw new ArgumentException(
                    $"Duplicate start position slot: {sp.Slot}.",
                    nameof(startPositions));
            }

            if (!bounds.Contains(sp.Position))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startPositions),
                    sp.Position,
                    "Start position is outside arena bounds.");
            }
        }

        HashSet<string> seenWallIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (WallBlock wall in wallArray)
        {
            if (!seenWallIds.Add(wall.Id))
            {
                throw new ArgumentException(
                    $"Duplicate wall block id: {wall.Id}.",
                    nameof(wallBlocks));
            }

            if (!bounds.Contains(wall.Bounds.Min))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wallBlocks),
                    wall.Bounds.Min,
                    $"Wall block '{wall.Id}' min is outside arena bounds.");
            }

            if (!bounds.Contains(wall.Bounds.Max))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wallBlocks),
                    wall.Bounds.Max,
                    $"Wall block '{wall.Id}' max is outside arena bounds.");
            }
        }

        foreach (FixedVec2 point in patrolArray)
        {
            if (!bounds.Contains(point))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(patrolPoints),
                    point,
                    "Patrol point is outside arena bounds.");
            }
        }

        foreach (string tag in tagArray)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException(
                    "Tag must not be null, empty, or whitespace.",
                    nameof(tags));
            }
        }

        Id = id;
        DisplayName = displayName;
        Description = description;
        Bounds = bounds;
        StartPositions = startArray;
        WallBlocks = wallArray;
        PatrolPoints = patrolArray;
        Tags = tagArray;
    }
}
