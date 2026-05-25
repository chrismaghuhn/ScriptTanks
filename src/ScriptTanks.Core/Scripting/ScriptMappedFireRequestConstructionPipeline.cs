using System;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Classifies script domain mapping records and constructs <see cref="MatchFireRequest"/>
/// values for weapon fire commands when both muzzle and velocity resolvers succeed.
/// </summary>
/// <remarks>
/// For <see cref="ScriptDomainRequestCategory.Weapon"/> fire commands, calls
/// <see cref="FireMuzzlePositionResolver"/> then <see cref="FireVelocityResolver"/> using
/// <see cref="WeaponSlot"/> index 0. <see cref="ProjectileIdSequence.AllocateNext"/> runs only
/// when both resolvers return <c>Resolved</c>. Does not execute fire, spawn projectiles,
/// mutate match or sensor runtime state, log, replay, or integrate Godot. The
/// <paramref name="runtime"/> snapshot may differ from
/// <c>mappingResult.IntegrationResult.Runtime</c> when an earlier pass (for example turret
/// application) updated <see cref="Match.MatchState"/> in the same script tick.
/// </remarks>
public static class ScriptMappedFireRequestConstructionPipeline
{
    /// <summary>
    /// Produces one <see cref="ScriptMappedFireRequestConstructionRecord"/> per mapping record
    /// and the final <see cref="ProjectileIdSequence"/> after any constructed requests.
    /// </summary>
    /// <param name="runtime">
    /// Non-null runtime snapshot whose <see cref="MatchSensorRuntimeState.State"/> supplies
    /// tank geometry for muzzle and velocity resolvers. May differ from
    /// <c>mappingResult.IntegrationResult.Runtime</c> when state was updated earlier in the
    /// same composition pass.
    /// </param>
    public static ScriptMappedFireRequestConstructionPipelineResult Construct(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mappingResult,
        ProjectileIdSequence projectileIdSequence)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(mappingResult);

        ProjectileIdSequence currentSequence = projectileIdSequence;

        var constructionRecords =
            new ScriptMappedFireRequestConstructionRecord[mappingResult.Count];

        for (int recordIndex = 0; recordIndex < mappingResult.Count; recordIndex++)
        {
            ScriptDomainRequestMappingRecord mappingRecord =
                mappingResult.GetRecordAtIndex(recordIndex);

            constructionRecords[recordIndex] = Classify(
                runtime,
                recordIndex,
                mappingRecord,
                ref currentSequence);
        }

        var constructionResult = new ScriptMappedFireRequestConstructionResult(
            mappingResult,
            constructionRecords);

        return new ScriptMappedFireRequestConstructionPipelineResult(
            constructionResult,
            currentSequence);
    }

    private static ScriptMappedFireRequestConstructionRecord Classify(
        MatchSensorRuntimeState runtime,
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        ref ProjectileIdSequence currentSequence)
    {
        switch (mappingRecord.Category)
        {
            case ScriptDomainRequestCategory.Unsupported:
                return ScriptMappedFireRequestConstructionRecord.Unsupported(
                    recordIndex,
                    mappingRecord);

            case ScriptDomainRequestCategory.Weapon:
                if (mappingRecord.TranslationOutput.Request.Kind
                    != ScriptTranslatedCommandRequestKind.Fire)
                {
                    return ScriptMappedFireRequestConstructionRecord.NoFireCommand(
                        recordIndex,
                        mappingRecord);
                }

                return ConstructWeaponFire(
                    runtime,
                    recordIndex,
                    mappingRecord,
                    ref currentSequence);

            default:
                return ScriptMappedFireRequestConstructionRecord.NotWeapon(
                    recordIndex,
                    mappingRecord);
        }
    }

    private static ScriptMappedFireRequestConstructionRecord ConstructWeaponFire(
        MatchSensorRuntimeState runtime,
        int recordIndex,
        ScriptDomainRequestMappingRecord mappingRecord,
        ref ProjectileIdSequence currentSequence)
    {
        WeaponSlot weaponSlot = new WeaponSlot(0);

        FireMuzzlePositionResult muzzleResult = FireMuzzlePositionResolver.Resolve(
            runtime,
            mappingRecord.TankIndex,
            weaponSlot);

        if (!muzzleResult.IsResolved)
        {
            return new ScriptMappedFireRequestConstructionRecord(
                recordIndex,
                mappingRecord,
                ScriptMappedFireRequestConstructionStatus.MissingMuzzleResolver,
                fireRequest: null);
        }

        FireVelocityResult velocityResult = FireVelocityResolver.Resolve(
            runtime,
            mappingRecord.TankIndex,
            weaponSlot);

        if (!velocityResult.IsResolved)
        {
            return new ScriptMappedFireRequestConstructionRecord(
                recordIndex,
                mappingRecord,
                ScriptMappedFireRequestConstructionStatus.MissingVelocityResolver,
                fireRequest: null);
        }

        ProjectileIdAllocation allocation = currentSequence.AllocateNext();
        currentSequence = allocation.NextSequence;

        MatchFireRequest fireRequest = new MatchFireRequest(
            mappingRecord.TankId,
            weaponSlot,
            allocation.ProjectileId,
            muzzleResult.MuzzlePosition!.Value,
            velocityResult.FireVelocity!.Value);

        return ScriptMappedFireRequestConstructionRecord.Constructed(
            recordIndex,
            mappingRecord,
            fireRequest);
    }
}
