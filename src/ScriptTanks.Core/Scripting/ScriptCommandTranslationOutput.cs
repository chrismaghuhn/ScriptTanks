using System;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Immutable bundle of <see cref="ScriptCommandTranslationResult"/> and
/// <see cref="ScriptTranslatedCommandRequest"/> for future translator v2 outputs.
/// </summary>
/// <remarks>
/// When <see cref="ScriptCommandTranslationResult.Status"/> is <see cref="ScriptCommandTranslationStatus.Translated"/>,
/// <see cref="Request"/> must carry a translated command (<see cref="ScriptTranslatedCommandRequest.HasRequest"/> is
/// <c>true</c>). For all other statuses, <see cref="Request"/> must be <see cref="ScriptTranslatedCommandRequest.None"/>.
/// This type does not run translation logic, execute commands, dispatch requests, mutate match state, log, diagnose,
/// or integrate Godot.
/// </remarks>
public sealed class ScriptCommandTranslationOutput
{
    public ScriptCommandTranslationResult Result { get; }

    public ScriptTranslatedCommandRequest Request { get; }

    public ScriptCommandTranslationOutput(
        ScriptCommandTranslationResult result,
        ScriptTranslatedCommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(request);

        bool statusExpectsRequest =
            result.Status == ScriptCommandTranslationStatus.Translated;

        if (statusExpectsRequest != request.HasRequest)
        {
            throw new ArgumentException(
                "Translated translation status requires a non-none translated command request; non-translated statuses require ScriptTranslatedCommandRequest.None().",
                nameof(request));
        }

        Result = result;
        Request = request;
    }
}
