# GitHub presentation checklist

Manual steps to polish how the repository appears on GitHub. No code changes required beyond what is already in the repo.

## Repository settings

### Description (suggested)

> Deterministic programming autobattler — C# simulation core, script-driven tanks, replayable matches. Godot client in progress.

### Topics / tags

Add these repository topics:

- `programming-game`
- `autobattler`
- `godot`
- `csharp`
- `deterministic-simulation`
- `game-ai`
- `scripting`
- `replay-system`

Optional: `dotnet`, `xunit`, `game-development`

### Website

Link to the Godot demo folder or a future project page when available.

---

## README badges

CI badge (already in root README):

```markdown
[![.NET CI](https://github.com/chrismaghuhn/ScriptTanks/actions/workflows/dotnet.yml/badge.svg)](https://github.com/chrismaghuhn/ScriptTanks/actions/workflows/dotnet.yml)
```

Optional shields (add only if accurate):

```markdown
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
```

Update test count in README after major milestones (`dotnet test` summary line).

---

## Screenshots and media

Capture when the Godot client or logging output is presentable:

| Asset | Suggestion |
|-------|------------|
| Arena screenshot | Top-down match with tanks and obstacles |
| Script / terminal UI | Routine list or command selection (if exposed in client) |
| Architecture diagram | Export mermaid from README or draw.io |
| Replay / log snippet | Terminal showing tick log or replay summary (sanitized) |
| GIF (short) | One deterministic replay loop, 5–10 seconds |

Store under `docs/images/` or `.github/` and link from README once assets exist.

---

## Releases and milestones

Suggested GitHub milestones:

| Milestone | Scope |
|-----------|--------|
| Core simulation v0.1 | Deterministic match tick, projectiles, bounds |
| Script runtime integrated | Combined composer, runner, replay |
| Aim & turret tracks | Full-angle, turn-rate, alignment status |
| CI green | `.github/workflows/dotnet.yml` passing on main |
| Godot playable slice | Client runs a match against core (future) |

Tag releases only when there is a meaningful snapshot (e.g. `v0.1.0-core` after a stable checkpoint), not on every doc commit.

---

## Social proof for recruiters / portfolio

- Pin the repository on your GitHub profile if this is a flagship project.
- Enable **Issues** for serious bug reports; **Discussions** optional for design Q&A.
- Add a 2–3 sentence portfolio blurb linking to README architecture section.
- In interviews, emphasize: deterministic pipelines, 3000+ tests, checkpoint-driven integration, replay tooling—not “finished game on Steam.”

---

## Done in repo (reference)

- [x] Root README rewritten (English, architecture, limitations)
- [x] [docs/README.md](README.md) navigation hub
- [x] [CONTRIBUTING.md](../CONTRIBUTING.md)
- [x] [DEVELOPMENT.md](DEVELOPMENT.md)
- [x] [SECURITY.md](../SECURITY.md)
- [x] GitHub Actions [.github/workflows/dotnet.yml](../.github/workflows/dotnet.yml)
