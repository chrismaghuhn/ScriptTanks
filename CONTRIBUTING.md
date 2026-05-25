# Contributing to ScriptTanks

Thank you for your interest in the project. ScriptTanks is an in-development deterministic simulation and scripting runtime; contributions should preserve testability and reproducibility.

## Before you start

1. Read [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for local setup and workflow.
2. Skim the relevant integration checkpoint under [docs/](docs/) for the area you are changing.
3. Do not change runtime behavior in `src/` without corresponding tests in `tests/`.

## Build and test

From the repository root:

```bash
dotnet build ScriptTanks.sln -warnaserror
dotnet test ScriptTanks.sln
```

Warnings are treated as errors in this project. Both commands must pass before opening a pull request.

## Pull requests

- Keep changes focused; separate documentation-only PRs from behavior changes when possible.
- Behavior changes require tests that lock the intended semantics.
- Update or add an integration checkpoint doc when completing a multi-task architectural track (see DEVELOPMENT.md).
- Do not edit existing checkpoint files to rewrite history unless you are explicitly reconciling documentation with a new authoritative checkpoint.

## Documentation

- English for new public-facing docs (README, CONTRIBUTING, DEVELOPMENT hub).
- Existing German technical checkpoints and plans should remain unless you are adding cross-links from the English hub.
- Link to checkpoints from PR descriptions when the change completes or extends a documented track.

## Godot client

The experimental client lives in [`script-tanks/`](script-tanks/). Core simulation changes should remain in `ScriptTanks.Core`; client work is welcome but is not required for most core PRs.

## Questions

Open a GitHub issue for design questions or use the discussion area if enabled on the repository.
