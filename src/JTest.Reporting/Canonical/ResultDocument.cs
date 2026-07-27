namespace JTest.Reporting.Canonical;

/// <summary>One canonical run result: the evidence every report projects.</summary>
/// <param name="RunId">The stable run identity derived from start time and trace digest.</param>
/// <param name="CanonicalBytes">The exact canonical UTF-8 JSON bytes of the document.</param>
/// <param name="Digest">The <c>sha256:</c> digest of the canonical bytes.</param>
public sealed record ResultDocument(
    string RunId,
    byte[] CanonicalBytes,
    string Digest);
