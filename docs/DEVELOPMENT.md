# Development guide

Local workflow for working on the ScriptTanks deterministic core.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (required for `ScriptTanks.Core.Tests`, which targets `net10.0`)
- Optional: Godot 4 for [`../script-tanks/`](../script-tanks/) client work

The core library multi-targets `net8.0` and `net10.0`; CI and tests run on `net10.0`.

## Commands

From the repository root:

```bash
# Restore (implicit on build)
dotnet restore ScriptTanks.sln

# Build — warnings fail the build
dotnet build ScriptTanks.sln -warnaserror

# Run all core tests
dotnet test ScriptTanks.sln

# Run a single test class (example)
dotnet test ScriptTanks.sln --filter "FullyQualifiedName~CombinedRuntime"
```

Run `dotnet test` to see the current test count; the suite grows with each integration track.

## Project layout

| Project | Path |
|---------|------|
| Core | `src/ScriptTanks.Core/` |
| Tests | `tests/ScriptTanks.Core.Tests/` |

Key subsystems (by folder):

- `Scripting/` — routines, conditions, combined composer and tick pipeline
- `Match/` — movement, bounds, obstacle collision, match tick
- `Projectiles/` — projectile state and hit resolution
- `Replay/` — runner and replay recording

## Philosophy

### Deterministic core

Given the same initial match state, script programs, and tick count, the simulation should produce the same results. When changing tick ordering or pipeline semantics, add or update tests that would fail on accidental nondeterminism or ordering regressions.

### Pure pipelines

Prefer small pipelines with explicit input/output types over ad-hoc mutation. The combined script runtime and match tick layers follow this pattern; new features should extend it rather than bypass it.

### Test-driven development

1. Describe the behavior (often in a `*_PLAN.md` first for larger tracks).
2. Add failing tests.
3. Implement in `src/ScriptTanks.Core`.
4. Green build with `-warnaserror`.
5. Capture the implemented state in an `*_INTEGRATION_CHECKPOINT.md` when the track is complete.

### Integration checkpoints

Checkpoints are the authoritative record of **what is implemented today**. They include:

- Completed task ranges
- Public API tables
- Combined tick flow diagrams
- Locked invariants and known gaps

Plans (`*_PLAN.md`) are design artifacts; they may be superseded and are not edited to match code retroactively—new checkpoints supersede narrative instead.

## Script evaluation flow (reference)

```text
ScriptRuntimeContextBuilder.Build(runtime, tankIndex)
  → ScriptEvaluationContext
  → ScriptRoutineSelector.SelectFirstValid(program, context)
  → ScriptDecisionPipeline / command translation
  → one ScriptCommand per tank per tick (today)
```

See [MULTI_CONDITION_SCRIPT_ROUTINE_PLAN.md](MULTI_CONDITION_SCRIPT_ROUTINE_PLAN.md) for the planned multi-condition extension.

## Combined tick flow (reference)

After script composition, `CombinedRuntimeTickPipeline` runs:

```text
movement → arena bounds → wall obstacle → projectiles → advance
```

See [WALL_OBSTACLE_COLLISION_INTEGRATION_CHECKPOINT.md](WALL_OBSTACLE_COLLISION_INTEGRATION_CHECKPOINT.md) §3.

## Godot client

- Active project: [`../script-tanks/`](../script-tanks/)
- [`../game/`](../game/) is reserved for a future canonical client path

Core development does not require Godot. Wire client features against the documented runner/replay APIs when ready.

## CI

GitHub Actions runs the same build and test commands on push and pull request. See [`.github/workflows/dotnet.yml`](../.github/workflows/dotnet.yml).

## Further reading

- [Documentation hub](README.md)
- [Contributing](../CONTRIBUTING.md)
- [Root README](../README.md)
