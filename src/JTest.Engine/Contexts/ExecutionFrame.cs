using System.Text.Json.Nodes;
using JTest.Language.Scopes;

namespace JTest.Engine.Contexts;

/// <summary>
/// One execution frame: the named scopes visible to expressions at a point
/// of execution. Case frames own a fresh scratch scope; loop frames share
/// their parent's scratch scope and add bindings; template frames isolate
/// everything except the read-only <c>env</c>, <c>globals</c>, and
/// <c>case</c> scopes and their declared parameters.
/// </summary>
public sealed class ExecutionFrame
{
    private readonly Dictionary<string, JsonNode?> bindings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonNode?> stepResults = new(StringComparer.Ordinal);

    private ExecutionFrame(
        FrameKind kind,
        ExecutionFrame? parent,
        JsonObject env,
        JsonObject globals,
        JsonObject caseValues,
        JsonObject ctx)
    {
        Kind = kind;
        Parent = parent;
        Env = env;
        Globals = globals;
        Case = caseValues;
        Ctx = ctx;
    }

    /// <summary>The frame kind.</summary>
    public FrameKind Kind { get; }

    /// <summary>The parent frame; null for case and template frames.</summary>
    public ExecutionFrame? Parent { get; }

    /// <summary>Immutable run-level values.</summary>
    public JsonObject Env { get; }

    /// <summary>Suite-scoped mutable values.</summary>
    public JsonObject Globals { get; }

    /// <summary>The current dataset row.</summary>
    public JsonObject Case { get; }

    /// <summary>The frame's scratch scope.</summary>
    public JsonObject Ctx { get; }

    /// <summary>The previous step result in this frame.</summary>
    public JsonNode? This { get; private set; }

    /// <summary>Creates the root frame of a case or dataset run.</summary>
    /// <param name="env">Immutable run-level values.</param>
    /// <param name="globals">Suite-scoped mutable values (shared across cases).</param>
    /// <param name="caseValues">The dataset row, or an empty object.</param>
    public static ExecutionFrame CreateCase(JsonObject env, JsonObject globals, JsonObject caseValues) =>
        new(FrameKind.Case, null, env, globals, caseValues, []);

    /// <summary>Creates a loop pass frame sharing the parent's scratch scope.</summary>
    /// <param name="parent">The enclosing frame.</param>
    /// <param name="loopBindings">The loop's item and index bindings.</param>
    public static ExecutionFrame CreateLoop(
        ExecutionFrame parent,
        IReadOnlyDictionary<string, JsonNode?> loopBindings)
    {
        var frame = new ExecutionFrame(FrameKind.Loop, parent, parent.Env, parent.Globals, parent.Case, parent.Ctx);
        foreach (var binding in loopBindings)
        {
            frame.bindings[binding.Key] = binding.Value;
        }

        return frame;
    }

    /// <summary>Creates an isolated template invocation frame.</summary>
    /// <param name="caller">The invoking frame.</param>
    /// <param name="parameters">The resolved template arguments and defaults.</param>
    public static ExecutionFrame CreateTemplate(
        ExecutionFrame caller,
        IReadOnlyDictionary<string, JsonNode?> parameters)
    {
        var frame = new ExecutionFrame(FrameKind.Template, null, caller.Env, caller.Globals, caller.Case, []);
        foreach (var parameter in parameters)
        {
            frame.bindings[parameter.Key] = parameter.Value;
        }

        return frame;
    }

    /// <summary>Records a completed step result under its id and, for result-producing steps, as <c>$.this</c>.</summary>
    /// <param name="stepId">Optional frame-unique step id.</param>
    /// <param name="result">The step result value.</param>
    /// <param name="updateThis">Whether the step produces data (<c>assert</c> and <c>wait</c> are transparent to <c>$.this</c>).</param>
    public void SetStepResult(string? stepId, JsonNode? result, bool updateThis = true)
    {
        if (updateThis)
        {
            This = result;
        }

        if (stepId is not null)
        {
            stepResults[stepId] = result;
        }
    }

    /// <summary>
    /// Resolves a non-reserved name: loop bindings and step results of this
    /// frame, then enclosing loop parents. Template and case frames are
    /// resolution boundaries.
    /// </summary>
    /// <param name="name">The name to resolve.</param>
    /// <param name="value">The resolved value when found.</param>
    public bool TryResolveName(string name, out JsonNode? value)
    {
        var frame = this;
        while (frame is not null)
        {
            if (frame.bindings.TryGetValue(name, out value) ||
                frame.stepResults.TryGetValue(name, out value))
            {
                return true;
            }

            frame = frame.Parent;
        }

        value = null;
        return false;
    }

    /// <summary>Resolves a reserved scope name to its scope root, if the name is reserved.</summary>
    /// <param name="name">The scope name.</param>
    /// <param name="scope">The scope root when the name is reserved.</param>
    public bool TryResolveScope(string name, out JsonNode? scope)
    {
        switch (name)
        {
            case ScopeNames.Env:
                scope = Env;
                return true;
            case ScopeNames.Globals:
                scope = Globals;
                return true;
            case ScopeNames.Case:
                scope = Case;
                return true;
            case ScopeNames.Ctx:
                scope = Ctx;
                return true;
            case ScopeNames.This:
                scope = This;
                return true;
            default:
                scope = null;
                return false;
        }
    }
}
