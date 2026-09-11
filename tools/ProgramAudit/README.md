# Program audit probes

Independent diagnostic probes introduced for the 0.11.7 program audit and extended for 0.11.8. They invoke the production game, input handlers, renderer, persistence and window geometry in isolated forms. They record observations; **exit code 0 means the probes completed, not that the program is correct**. They can record both the original defects and their corrected behavior.

Run from the repository root on Windows with the .NET 10 SDK:

```powershell
dotnet run --project tools/ProgramAudit/ProgramAudit.csproj -c Release
```

Output goes to `artifacts/program-audit/results.json` and adjacent PNGs. The save-conflict and long-session probes use unique subdirectories there. All other forms use ephemeral state. No visible windows are shown, no desktop input is sent, and the normal user save directory is not used. The resource probe creates and destroys hidden native handles.

To run a subset, pass its names after `--`, for example:

```powershell
dotnet run --project tools/ProgramAudit/ProgramAudit.csproj -c Release -- foundation-underlay long-session-cost
```

Subset output is `selected-results.json`. Pass `--output=artifacts/my-audit` to retain a run separately. Available names: `historical-active-rules`, `spider-active-difficulty`, `foundation-underlay`, `victory-shortcuts`, `small-screen-fitting`, `two-open-instances`, `accessibility-and-keyboard`, `resource-soak`, `long-session-cost`, `hint-cold-vs-warm`.

The accessibility observations inspect the program's accessible tree; they do not replace Narrator testing. The small-screen probe supplies a synthetic work area; it does not change monitor settings. The two-instance probe uses two independent `GameWindow`/`Store` pairs sharing one isolated directory and reproduces sequential stale saves, without relying on a simultaneous file-write race. Performance timings are local observations, not guarantees.

After fixes, convert the relevant observations into behavioral regression tests that assert the intended result. Do not turn the currently observed failures into expected-success tests.

The cold/warm hint comparison clears the cache between cold samples in the same build, then compares immediate warm samples and verifies identical choices. It is not a benchmark of the previous executable.
