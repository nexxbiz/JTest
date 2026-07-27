# JTest C# source quality gate

Policy ID: `jtest:policy:csharp-source-quality-gate`
Policy version: 1.0.0
Status: adopted governance for all handwritten JTest 2.x C#
Derived from: Program Kit policy
`pkid:policy:program-kit:csharp-source-quality-gate` version 1.10.0
(decision D2 of the approved JTest 2.0 design).

The Program Kit `CSharpGate` analyzer is not distributable as a package, so
JTest adopts the gate's rules as governance: the subset expressible through
the compiler, built-in analyzers, and `.editorconfig` is enforced at error
severity in every build; the remainder is enforced in review. This delta is
deliberate and recorded here honestly.

Mandatory rules:

1. One named type per physical C# file; the file name matches the declared
   type. Type-free files are reserved for assembly metadata.
2. Every file lives below a logical intent folder; its namespace is exactly
   the project root namespace plus its folder segments (IDE0130 at error).
   Test folders mirror the tested source intent folders.
3. Never invoke behavior on a freshly constructed receiver
   (`new X().Do(...)` and equivalents). Construct, hold, then use — or make
   the helper static.
4. Uncontracted internal helpers are static. Stateful or replaceable
   behavior sits behind a narrow interface that declares real behavior and
   is supplied through constructor injection. Behavioral implementations
   are constructed only at explicit composition roots.
5. No warning suppression: `TreatWarningsAsErrors` in all variants stays
   on; `#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, and
   severity downgrades of shipped rules are prohibited without a recorded
   human approval appended to this document.
6. Determinism posture: no wall-clock, RNG, culture, or environment reads
   inside validation, generation, or rendering code paths; such inputs are
   parameters supplied by callers.

Review checklist additions (rules 1, 3, 4 and 6) apply to every work-unit
diff before commit.

Approved deviations: none.
