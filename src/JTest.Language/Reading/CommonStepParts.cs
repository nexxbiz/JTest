using System.Text.Json;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;
using JTest.Language.Expressions;
using JTest.Language.Scopes;

namespace JTest.Language.Reading;

/// <summary>The bound common properties shared by every step kind.</summary>
/// <param name="Id">Optional frame-unique id.</param>
/// <param name="Name">Optional display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Save">Save operations.</param>
/// <param name="Assert">Assertions.</param>
internal sealed record CommonStepParts(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyDictionary<string, JsonElement> Save,
    IReadOnlyList<AssertionDefinition> Assert)
{
    internal static readonly string[] PropertyNames = ["type", "id", "name", "description", "save", "assert"];

    internal static CommonStepParts Bind(
        JsonElement element,
        string source,
        string jsonPointer,
        bool insideTemplate,
        ICollection<LanguageDiagnostic> sink)
    {
        var id = ElementShape.OptionalString(element, "id", source, jsonPointer, sink);
        if (id is not null && ScopeNames.All.Contains(id))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.ReservedName,
                $"Step id '{id}' is a reserved scope name.",
                source,
                $"{jsonPointer}/id");
            id = null;
        }

        var name = ElementShape.OptionalString(element, "name", source, jsonPointer, sink);
        var description = ElementShape.OptionalString(element, "description", source, jsonPointer, sink);

        var save = ElementShape.ElementMap(element, "save", source, jsonPointer, sink);
        foreach (var entry in save)
        {
            var entryPointer = $"{jsonPointer}/save/{ElementShape.PointerEscape(entry.Key)}";
            SaveTargetRules.Validate(entry.Key, insideTemplate, source, entryPointer, sink);
            ExpressionSyntax.ValidateValue(entry.Value, source, entryPointer, sink);
        }

        var assertArray = ElementShape.OptionalArray(element, "assert", source, jsonPointer, sink);
        var assertions = AssertionBinder.BindList(assertArray, source, $"{jsonPointer}/assert", sink);

        return new CommonStepParts(id, name, description, save, assertions);
    }
}
