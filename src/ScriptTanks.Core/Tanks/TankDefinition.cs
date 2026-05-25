using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Tanks;

/// <summary>
/// Immutable, deterministic tank archetype description. Bundles
/// metadata (<see cref="Id"/>, <see cref="DisplayName"/>,
/// <see cref="Description"/>, <see cref="Tags"/>) with a
/// <see cref="BasicTankStats"/> value used by later spawn, hitbox,
/// movement, rotation, and damage-reduction systems. The class is a
/// pure data carrier: no runtime state, no weapons, no sensors, no
/// CPU/scripts, no movement or damage simulation. Tags are
/// defensively copied on construction and exposed only as
/// <see cref="IReadOnlyList{T}"/>.
/// </summary>
public sealed class TankDefinition
{
    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public BasicTankStats Stats { get; }

    public IReadOnlyList<string> Tags { get; }

    public TankDefinition(
        string id,
        string displayName,
        string description,
        BasicTankStats stats,
        IEnumerable<string> tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(tags);

        string[] tagArray = tags.ToArray();

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
        Stats = stats;
        Tags = tagArray;
    }
}
