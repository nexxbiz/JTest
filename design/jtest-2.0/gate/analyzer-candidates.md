# Analyzer candidates — input to the deferred second gate iteration

Status: working inventory only. Not part of the digest-bound host-rebuild
review set; feeds the future `design-csharp-build-gate` iteration the
human deferred on 2026-07-27 (open decision JT2H-D001).

Repository scan of 2026-07-27: zero magic exit-code integers, zero
literal `JT****` codes outside the registries, zero unencoded writer file
writes; one direct environment read outside `Ports/`
(`src/JTest.Cli.Host/Commands/JTestConsoleCommandDispatcher.cs` — removed
by the host rebuild; its replacement must use `IProcessEnvironment`).

## Bucket A — generic rules; request as PUBLIC Program Kit contract analyzers

All four are implemented in Program Kit's **private** `CSharpGate`
(`PKCS`, forbidden on consumer source). They are generic engineering
policy, not JTest semantics — the right move is an upstream request to
publish them as public contract analyzers (`PKCC`-style, exact manifest
digests), consumable by any Program Kit consumer:

1. One named type per physical file.
2. Fresh-receiver invocation ban (`new X().Do(...)` and its disguises).
3. Uncontracted helpers static; behavioral collaborators behind
   ctor-injected narrow interfaces; composition-root-only construction.
4. Determinism purity: no clock/env/culture/RNG symbol reads inside
   declared deterministic paths — generic rule, consumer-scoped
   applicability (JTest would scope it to `JTest.Reporting` writers,
   `JTest.Language` validation/binding, and `JTest.Engine.Expressions`,
   with `Ports/` implementations exempt).

## Bucket B — available today with zero authoring (adopt in the next iteration)

- `Microsoft.CodeAnalysis.BannedApiAnalyzers` + per-project
  `BannedSymbols.txt` covers the symbol-ban half of rule A4 immediately
  (`DateTime.Now/UtcNow`, `DateTimeOffset.Now/UtcNow`,
  `Environment.GetEnvironmentVariable`, `CultureInfo.CurrentCulture`,
  `System.Random`, `Guid.NewGuid` in the scoped projects).
- Raise the globalization rule severities (`CA1305`, `CA1307`, `CA1309`,
  `CA1310`) to error in `.editorconfig` — invariant-culture and ordinal
  comparison discipline without any new analyzer.

## Bucket C — truly JTest-specific (small, likely test-enforced is enough)

- Exit codes returned from command executors must be `CliExitCodes`
  constants (scan shows zero violations; a test or tiny analyzer both work).
- Diagnostics constructed only through the `Diag` helpers with codes from
  the registries (scan shows zero violations; fixture coverage already
  guards the registry).

## Recommended shape of the second iteration

Upstream request for Bucket A (one prompt to the maintenance agent),
adopt Bucket B immediately as gate components (both are existing public
packages/rules — `reuse-existing` disposition), and keep Bucket C
test-enforced unless violations ever appear.
