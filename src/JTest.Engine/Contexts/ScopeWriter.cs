using System.Text.Json.Nodes;
using JTest.Language.Scopes;

namespace JTest.Engine.Contexts;

/// <summary>
/// Static application of validated <c>save</c> targets. Targets always
/// address <c>$.ctx.*</c> or <c>$.globals.*</c>; intermediate objects are
/// created on demand and non-object intermediates fail closed.
/// </summary>
public static class ScopeWriter
{
    /// <summary>Applies one save operation to the frame.</summary>
    /// <param name="frame">The executing frame.</param>
    /// <param name="target">The validated target path, e.g. <c>$.ctx.order.id</c>.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>Whether the write succeeded.</returns>
    public static bool TryApply(ExecutionFrame frame, string target, JsonNode? value)
    {
        var segments = target[2..].Split('.');
        if (segments.Length < 2)
        {
            return false;
        }

        JsonObject root;
        if (segments[0] == ScopeNames.Ctx)
        {
            root = frame.Ctx;
        }
        else if (segments[0] == ScopeNames.Globals)
        {
            root = frame.Globals;
        }
        else
        {
            return false;
        }

        var current = root;
        for (var i = 1; i < segments.Length - 1; i++)
        {
            if (current[segments[i]] is JsonObject next)
            {
                current = next;
            }
            else if (current[segments[i]] is null)
            {
                var created = new JsonObject();
                current[segments[i]] = created;
                current = created;
            }
            else
            {
                return false;
            }
        }

        current[segments[^1]] = value;
        return true;
    }
}
