# ORBIT audit probes

Run from the repository root on Windows with .NET 10:

```powershell
dotnet run --project tools/OrbitAudit/OrbitAudit.csproj -c Release
dotnet run --project tools/OrbitAudit/OrbitAudit.csproj -c Release -- --performance
```

Results and production-renderer PNGs go to `artifacts/orbit-audit-20260911`. Forms are ephemeral and never shown; the tool does not operate a user's visible game or read/write their personal saves. Reflection invokes production handlers and rendering. These diagnostic probes report observed behavior, including defects; they are not regression assertions that those defects are desirable. Exceptions are recorded and produce a nonzero exit code.

Use `--output artifacts/orbit-fixes/probes` to retain a new run separately. The current hint probe calls ORBIT's dedicated guidance policy; retained September 11 baseline data predates that policy and used the historical `Hint()` method. Asserted fix regressions are in `tests/OrbitAuditFixChecks.cs` (`--orbit-audit-only`).

Long-column, collection and foundation-wave probes use constructed states accepted by the game's state validation. They are not recordings of naturally played deals. The hint probe follows the first suggested move across seeds 1–40, stopping at a repeated board or 300 actions. It validates each resulting state; this is a hint-strategy check, not a solver or a deal-solvability estimate.

The performance mode times offscreen painting for cold/palette frames, stock motion and the win sequence. Cold figures are individual samples and the first includes process warm-up. It does not measure physical display FPS. Use `tests/Solitude.Benchmarks.csproj` with `--orbit-only` for the complementary warm deal/settled rendering and frame-clock benchmark.

See [the audit report](../../docs/ORBIT-AUDIT-2026-09-11.md) for findings, limitations and retained measurements.
