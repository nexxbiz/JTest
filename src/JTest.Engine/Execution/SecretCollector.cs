using System.Text.Json;
using System.Text.Json.Nodes;
using JTest.Engine.Contexts;
using JTest.Engine.Expressions;
using JTest.Engine.Redaction;

namespace JTest.Engine.Execution;

/// <summary>Static collection of declared secret values at run start.</summary>
internal static class SecretCollector
{
    internal static void CollectDeclared(
        ExecutionFrame frame,
        IReadOnlyList<string> secretPaths,
        SecretSet secrets,
        string source)
    {
        foreach (var path in secretPaths)
        {
            var resolved = ExpressionResolver.ResolvePath(path, frame, source);
            if (resolved.Success)
            {
                RegisterLeaves(resolved.Value, secrets);
            }
        }
    }

    internal static void RegisterLeaves(JsonNode? value, SecretSet secrets)
    {
        switch (value)
        {
            case null:
                return;
            case JsonObject jsonObject:
                foreach (var property in jsonObject)
                {
                    RegisterLeaves(property.Value, secrets);
                }

                return;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    RegisterLeaves(item, secrets);
                }

                return;
            default:
                if (value.GetValueKind() == JsonValueKind.String)
                {
                    secrets.Register(value.GetValue<string>());
                }

                return;
        }
    }
}
