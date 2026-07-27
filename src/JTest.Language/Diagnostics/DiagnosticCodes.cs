namespace JTest.Language.Diagnostics;

/// <summary>
/// The stable diagnostic code registry. Codes are append-only; meanings
/// never change. Ranges: JT00xx document/syntax, JT01xx structure,
/// JT02xx expressions, JT03xx templates, JT04xx datasets, JT05xx
/// assertions, JT9xxx internal.
/// </summary>
public static class DiagnosticCodes
{
    /// <summary>The document is not syntactically valid JSON.</summary>
    public const string InvalidJson = "JT0001";

    /// <summary>The document root is not a JSON object.</summary>
    public const string RootNotObject = "JT0002";

    /// <summary>A required property is missing.</summary>
    public const string MissingProperty = "JT0101";

    /// <summary>A property has the wrong JSON type.</summary>
    public const string WrongPropertyType = "JT0102";

    /// <summary>An unknown property is present (documents are closed shapes).</summary>
    public const string UnknownProperty = "JT0103";

    /// <summary>A step declares an unknown <c>type</c>.</summary>
    public const string UnknownStepType = "JT0104";

    /// <summary>The <c>jtest</c> language discriminator is missing or unsupported.</summary>
    public const string UnsupportedLanguageVersion = "JT0105";

    /// <summary>An array that must contain at least one element is empty.</summary>
    public const string EmptyRequiredArray = "JT0106";

    /// <summary>A string property holds a value outside its allowed set.</summary>
    public const string InvalidEnumValue = "JT0107";

    /// <summary>A step id is declared more than once in the same frame.</summary>
    public const string DuplicateStepId = "JT0108";

    /// <summary>A save target addresses a scope that cannot be written.</summary>
    public const string InvalidSaveTarget = "JT0109";

    /// <summary>An http step declares more than one body source.</summary>
    public const string ConflictingBodySources = "JT0110";

    /// <summary>A numeric property is outside its allowed range.</summary>
    public const string ValueOutOfRange = "JT0111";

    /// <summary>A reserved scope name is used as a step id or loop binding.</summary>
    public const string ReservedName = "JT0112";

    /// <summary>An expression token is malformed.</summary>
    public const string MalformedExpression = "JT0201";

    /// <summary>An expression token is opened but never terminated.</summary>
    public const string UnterminatedExpression = "JT0202";

    /// <summary>An expression token has an empty path.</summary>
    public const string EmptyExpressionPath = "JT0203";

    /// <summary>A loop binding shadows a name already visible in the frame.</summary>
    public const string ShadowedBinding = "JT0204";

    /// <summary>A <c>use</c> step references a template that is not loaded.</summary>
    public const string UnknownTemplate = "JT0301";

    /// <summary>A required template parameter has no argument and no default.</summary>
    public const string MissingTemplateParameter = "JT0302";

    /// <summary>A <c>with</c> argument names a parameter the template does not declare.</summary>
    public const string UnknownTemplateParameter = "JT0303";

    /// <summary>A template step writes to <c>$.globals</c>, which templates may not do.</summary>
    public const string TemplateWritesGlobals = "JT0304";

    /// <summary>Two loaded templates share the same name.</summary>
    public const string DuplicateTemplateName = "JT0305";

    /// <summary>Template invocations form a cycle.</summary>
    public const string TemplateCycle = "JT0306";

    /// <summary>Two datasets of one test case share the same name.</summary>
    public const string DuplicateDatasetName = "JT0401";

    /// <summary>An assertion declares an unknown operator.</summary>
    public const string UnknownAssertionOperator = "JT0501";

    /// <summary>An assertion is missing an operand its operator requires.</summary>
    public const string MissingAssertionOperand = "JT0502";

    /// <summary>Validation itself failed unexpectedly; the document must be treated as invalid.</summary>
    public const string InternalValidationFailure = "JT9001";
}
