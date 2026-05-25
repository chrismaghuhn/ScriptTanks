using System;
using ScriptTanks.Core.Ids;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Outcome of <see cref="ScriptMappedFireRequestConstructionPipeline.Construct"/>: per-record
/// construction classifications plus the final <see cref="ProjectileIdSequence"/> state.
/// </summary>
/// <remarks>
/// Pure data wrapper. Does not construct fire requests, execute gameplay, mutate match state,
/// log, replay, or integrate Godot.
/// </remarks>
public sealed class ScriptMappedFireRequestConstructionPipelineResult
{
    public ScriptMappedFireRequestConstructionResult ConstructionResult { get; }

    public ProjectileIdSequence FinalProjectileIdSequence { get; }

    public ScriptMappedFireRequestConstructionPipelineResult(
        ScriptMappedFireRequestConstructionResult constructionResult,
        ProjectileIdSequence finalProjectileIdSequence)
    {
        ArgumentNullException.ThrowIfNull(constructionResult);

        ConstructionResult = constructionResult;
        FinalProjectileIdSequence = finalProjectileIdSequence;
    }
}
