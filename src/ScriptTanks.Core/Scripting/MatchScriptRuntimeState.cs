using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable ordered list of <see cref="ScriptRuntimeEvaluationRequest"/> values representing script
/// runtime inputs for a match snapshot.
/// </summary>
/// <remarks>
/// Defensively copies the supplied request sequence. This type does not validate against match state,
/// tank index uniqueness, evaluate scripts, execute commands, generate
/// match, sensor, weapon, or movement requests, mutate match state, log, diagnose, or integrate Godot.
/// </remarks>
public sealed class MatchScriptRuntimeState
{
    private readonly ScriptRuntimeEvaluationRequest[] _requests;

    public IReadOnlyList<ScriptRuntimeEvaluationRequest> Requests =>
        Array.AsReadOnly(_requests);

    public int Count => Requests.Count;

    public MatchScriptRuntimeState(
        IEnumerable<ScriptRuntimeEvaluationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        ScriptRuntimeEvaluationRequest[] copy = requests.ToArray();

        for (int i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null)
            {
                throw new ArgumentException(
                    "Request sequence must not contain null elements.",
                    nameof(requests));
            }
        }

        _requests = copy;
    }

    public ScriptRuntimeEvaluationRequest GetRequestAtIndex(
        int requestIndex)
    {
        ValidateIndex(requestIndex);

        return _requests[requestIndex];
    }

    public MatchScriptRuntimeState WithRequestAtIndex(
        int requestIndex,
        ScriptRuntimeEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateIndex(requestIndex);

        ScriptRuntimeEvaluationRequest[] next =
            new ScriptRuntimeEvaluationRequest[_requests.Length];

        Array.Copy(_requests, next, _requests.Length);
        next[requestIndex] = request;

        return new MatchScriptRuntimeState(next);
    }

    private void ValidateIndex(int requestIndex)
    {
        if (requestIndex < 0 || requestIndex >= _requests.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestIndex),
                requestIndex,
                "Request index is outside the match script runtime state range.");
        }
    }
}
