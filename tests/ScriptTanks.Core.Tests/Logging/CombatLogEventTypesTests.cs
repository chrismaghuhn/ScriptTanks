using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombatLogEventTypesTests
{
    private static string[] AllEventTypes()
        => new[]
        {
            CombatLogEventTypes.MatchStarted,
            CombatLogEventTypes.MatchEnded,
            CombatLogEventTypes.FireRequested,
            CombatLogEventTypes.FireSucceeded,
            CombatLogEventTypes.FireNotReady,
            CombatLogEventTypes.FireFailed,
            CombatLogEventTypes.ProjectileSpawned,
            CombatLogEventTypes.ProjectileHit,
            CombatLogEventTypes.ProjectileExpired,
            CombatLogEventTypes.ProjectileCleanedUp,
            CombatLogEventTypes.DamageDealt,
            CombatLogEventTypes.TankDestroyed,
            CombatLogEventTypes.ScriptTick,
            CombatLogEventTypes.FireRequestApplied,
            CombatLogEventTypes.FireRejected,
        };

    [Fact]
    public void AllConstants_AreNonEmpty()
    {
        foreach (string value in AllEventTypes())
        {
            Assert.False(string.IsNullOrWhiteSpace(value));
        }
    }

    [Fact]
    public void AllConstants_AreLowercaseSnakeCase()
    {
        foreach (string value in AllEventTypes())
        {
            Assert.Equal(value.ToLowerInvariant(), value);
            Assert.NotEqual('_', value[0]);
            Assert.NotEqual('_', value[^1]);

            foreach (char c in value)
            {
                bool allowed =
                    (c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_';
                Assert.True(
                    allowed,
                    $"Unexpected character '{c}' in '{value}'.");
            }
        }
    }

    [Fact]
    public void AllConstants_AreUnique()
    {
        string[] values = AllEventTypes();
        HashSet<string> distinct = new HashSet<string>(values);

        Assert.Equal(values.Length, distinct.Count);
    }

    [Fact]
    public void SelectedConstants_HaveExpectedValues()
    {
        Assert.Equal("match_started", CombatLogEventTypes.MatchStarted);
        Assert.Equal("match_ended", CombatLogEventTypes.MatchEnded);
        Assert.Equal("fire_succeeded", CombatLogEventTypes.FireSucceeded);
        Assert.Equal("projectile_spawned", CombatLogEventTypes.ProjectileSpawned);
        Assert.Equal("projectile_cleaned_up", CombatLogEventTypes.ProjectileCleanedUp);
        Assert.Equal("damage_dealt", CombatLogEventTypes.DamageDealt);
        Assert.Equal("tank_destroyed", CombatLogEventTypes.TankDestroyed);
    }

    [Fact]
    public void CombatLogEventTypes_ScriptTick_has_expected_value()
    {
        Assert.Equal("script_tick", CombatLogEventTypes.ScriptTick);
    }

    [Fact]
    public void CombatLogEventTypes_FireRequestApplied_has_expected_value()
    {
        Assert.Equal("fire_request_applied", CombatLogEventTypes.FireRequestApplied);
    }

    [Fact]
    public void CombatLogEventTypes_FireRejected_has_expected_value()
    {
        Assert.Equal("fire_rejected", CombatLogEventTypes.FireRejected);
    }
}
