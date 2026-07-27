using JTest.Engine.Tracing;

namespace JTest.Reporting.Canonical;

/// <summary>Produces the canonical result document from a sealed run trace.</summary>
public interface IResultDocumentWriter
{
    /// <summary>Writes the canonical document; identical traces yield identical bytes.</summary>
    /// <param name="run">The sealed run node.</param>
    ResultDocument Write(TraceNode run);
}
