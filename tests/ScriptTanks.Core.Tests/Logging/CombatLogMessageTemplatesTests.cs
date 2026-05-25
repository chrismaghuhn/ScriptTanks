using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombatLogMessageTemplatesTests
{
    private static string[] AllTemplates()
        => new[]
        {
            CombatLogMessageTemplates.MatchStarted,
            CombatLogMessageTemplates.MatchEnded,
            CombatLogMessageTemplates.FireRequested,
            CombatLogMessageTemplates.FireSucceeded,
            CombatLogMessageTemplates.FireNotReady,
            CombatLogMessageTemplates.FireFailed,
            CombatLogMessageTemplates.ProjectileSpawned,
            CombatLogMessageTemplates.ProjectileHit,
            CombatLogMessageTemplates.ProjectileExpired,
            CombatLogMessageTemplates.ProjectileCleanedUp,
            CombatLogMessageTemplates.DamageDealt,
            CombatLogMessageTemplates.TankDestroyed,
        };

    [Fact]
    public void AllTemplates_AreNonEmpty()
    {
        foreach (string value in AllTemplates())
        {
            Assert.False(string.IsNullOrWhiteSpace(value));
        }
    }

    [Fact]
    public void AllTemplates_AreUnique()
    {
        string[] values = AllTemplates();
        HashSet<string> distinct = new HashSet<string>(values);

        Assert.Equal(values.Length, distinct.Count);
    }

    [Fact]
    public void SelectedTemplates_HaveExpectedValues()
    {
        Assert.Equal("Match started.", CombatLogMessageTemplates.MatchStarted);
        Assert.Equal("Match ended.", CombatLogMessageTemplates.MatchEnded);
        Assert.Equal(
            "Tank {TankId} fired weapon slot {WeaponSlot}.",
            CombatLogMessageTemplates.FireSucceeded);
        Assert.Equal(
            "Projectile {ProjectileId} spawned from tank {TankId}.",
            CombatLogMessageTemplates.ProjectileSpawned);
        Assert.Equal(
            "Projectile {ProjectileId} was cleaned up.",
            CombatLogMessageTemplates.ProjectileCleanedUp);
        Assert.Equal(
            "Tank {TankId} took {Damage} damage.",
            CombatLogMessageTemplates.DamageDealt);
        Assert.Equal(
            "Tank {TankId} was destroyed.",
            CombatLogMessageTemplates.TankDestroyed);
    }

    [Fact]
    public void Templates_DoNotContainNewlinesOrTabs()
    {
        foreach (string value in AllTemplates())
        {
            foreach (char c in value)
            {
                Assert.True(
                    c != '\n' && c != '\r' && c != '\t',
                    $"Unexpected control character in template '{value}'.");
            }
        }
    }

    [Fact]
    public void Templates_WithPlaceholders_UseNamedBracePlaceholders()
    {
        foreach (string value in AllTemplates())
        {
            int depth = 0;
            foreach (char c in value)
            {
                if (c == '{')
                {
                    Assert.True(
                        depth == 0,
                        $"Nested placeholder in template '{value}'.");
                    depth++;
                }
                else if (c == '}')
                {
                    Assert.True(
                        depth > 0,
                        $"Stray '}}' in template '{value}'.");
                    depth--;
                }
            }
            Assert.Equal(0, depth);

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '{')
                {
                    continue;
                }

                int close = value.IndexOf('}', i + 1);
                Assert.True(
                    close > i + 1,
                    $"Empty or unclosed placeholder in template '{value}'.");

                string placeholder = value.Substring(i + 1, close - i - 1);

                Assert.False(
                    char.IsDigit(placeholder[0]),
                    $"Numeric placeholder '{placeholder}' in template '{value}'.");
                Assert.True(
                    char.IsUpper(placeholder[0]),
                    $"Placeholder '{placeholder}' in template '{value}' must start with an uppercase letter.");

                foreach (char c in placeholder)
                {
                    Assert.True(
                        char.IsLetterOrDigit(c),
                        $"Invalid character in placeholder '{placeholder}' in template '{value}'.");
                }

                i = close;
            }
        }
    }
}
