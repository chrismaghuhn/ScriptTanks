using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Immutable result returned by <see cref="MatchSetupValidator.Validate(MatchInitialState)"/>.
/// Holds zero or more <see cref="MatchSetupValidationIssue"/> values in the
/// exact order in which the validator emitted them. The class is a plain
/// data carrier — it does not deduplicate, sort, classify by severity, or
/// override equality. <see cref="IsValid"/> is true iff <see cref="Issues"/>
/// is empty.
/// </summary>
public sealed class MatchSetupValidationResult
{
    public IReadOnlyList<MatchSetupValidationIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;

    public MatchSetupValidationResult(IEnumerable<MatchSetupValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Issues = issues.ToArray();
    }
}
