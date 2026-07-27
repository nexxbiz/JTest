using System.Text.Json;
using JTest.Language.Assertions;
using JTest.Language.Diagnostics;
using JTest.Language.Documents;
using JTest.Language.Expressions;

namespace JTest.Language.Reading;

/// <summary>Static binding of assertion objects.</summary>
internal static class AssertionBinder
{
    private static readonly HashSet<string> KnownProperties =
        new(StringComparer.Ordinal) { "op", "actual", "expected", "description" };

    internal static AssertionDefinition? Bind(
        JsonElement element,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Diag.Error(sink, DiagnosticCodes.WrongPropertyType, "An assertion must be an object.", source, jsonPointer);
            return null;
        }

        ElementShape.RejectUnknownProperties(element, KnownProperties, source, jsonPointer, sink);

        var op = ElementShape.RequiredString(element, "op", source, jsonPointer, sink);
        if (op is null)
        {
            return null;
        }

        if (!AssertionOperatorCatalog.IsKnown(op))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.UnknownAssertionOperator,
                $"Unknown assertion operator '{op}'.",
                source,
                $"{jsonPointer}/op",
                $"Known operators: {string.Join(", ", AssertionOperatorCatalog.Names)}.");
            return null;
        }

        JsonElement? actual = element.TryGetProperty("actual", out var actualElement)
            ? actualElement.Clone()
            : null;
        JsonElement? expected = element.TryGetProperty("expected", out var expectedElement)
            ? expectedElement.Clone()
            : null;

        if (actual is null && AssertionOperatorCatalog.RequiresActual(op))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingAssertionOperand,
                $"Operator '{op}' requires the 'actual' operand.",
                source,
                jsonPointer);
        }

        if (expected is null && AssertionOperatorCatalog.RequiresExpected(op))
        {
            Diag.Error(
                sink,
                DiagnosticCodes.MissingAssertionOperand,
                $"Operator '{op}' requires the 'expected' operand.",
                source,
                jsonPointer);
        }

        if (actual is not null)
        {
            ExpressionSyntax.ValidateValue(actual.Value, source, $"{jsonPointer}/actual", sink);
        }

        if (expected is not null)
        {
            ExpressionSyntax.ValidateValue(expected.Value, source, $"{jsonPointer}/expected", sink);
        }

        var description = ElementShape.OptionalString(element, "description", source, jsonPointer, sink);
        return new AssertionDefinition(op, actual, expected, description);
    }

    internal static List<AssertionDefinition> BindList(
        JsonElement? arrayElement,
        string source,
        string jsonPointer,
        ICollection<LanguageDiagnostic> sink)
    {
        var result = new List<AssertionDefinition>();
        if (arrayElement is null)
        {
            return result;
        }

        var index = 0;
        foreach (var item in arrayElement.Value.EnumerateArray())
        {
            var bound = Bind(item, source, $"{jsonPointer}/{index}", sink);
            if (bound is not null)
            {
                result.Add(bound);
            }

            index++;
        }

        return result;
    }
}
