# Task 5.85 — Logged Combined Runner / Replay Plan

> **Nur Dokumentation.** Keine Implementierung, keine Tests, keine Änderungen an `src/`, `tests/`, Projekten, Solution oder `game/`.
>
> Sprache: Erklärungen überwiegend auf Deutsch; Typnamen und API-Skizzen wie im Code auf Englisch.

## 1. Purpose

Dieses Dokument definiert, wie die nach Tasks **5.83** und **5.84** implementierten Combined-Runtime-Pfade [`CombinedRuntimeRunner.RunUntilEnd`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs) und [`CombinedRuntimeReplayRecorder.RecordUntilEnd`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs) später mit dem bestehenden **CombatLog**-System integriert werden sollen — **ohne** in Task 5.85 bereits Logged-Wrapper, neue Event-Typen oder Schema-Erweiterungen zu implementieren.

### Warum Logging separat nach 5.84 geplant wird

Nach 5.84 existiert der vollständige ungeloggte Pfad:

```text
initialRuntime + programs + ProjectileIdSequence + maxTicks
→ CombinedRuntimeRunner.RunUntilEnd  → CombinedRuntimeRunResult
→ CombinedRuntimeReplayRecorder.RecordUntilEnd  → MatchRecordedRunResult
```

CombatLog-Integration ist ein **eigenes Vertragsstück**:

- Welche Events sind MVP vs. zurückgestellt?
- In welcher Reihenfolge werden Logs gemergt?
- Welche Result-Wrapper (`LoggedCombinedRuntimeRunResult`, …) braucht 5.86+?

Der Runner-/Replay-Plan ([`COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md`](COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md) §10) hat Logging bewusst auf **5.85** verschoben. **Dieses Dokument** ist die Referenz für die erste Logged-Combined-Integration.

**Ziel:** `CombinedRuntimeRunner` / `CombinedRuntimeReplayRecorder` wrappen und `match_started` / `match_ended` wie bei [`LoggedMatchRunner`](../src/ScriptTanks.Core/Match/LoggedMatchRunner.cs) — **ohne** instabile Script-Per-Tick-Events vorzeitig zu erfinden.

## 2. Current State

| Bereich | Status im Repo |
| ------- | -------------- |
| **Combined runner** | [`CombinedRuntimeRunner.RunUntilEnd`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs) — implementiert (5.83) |
| **Combined run result** | [`CombinedRuntimeRunResult`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunResult.cs) — `FinalRuntime`, `RunResult`, `FinalProjectileIdSequence`, optional `LastTickResult` |
| **Combined replay recorder** | [`CombinedRuntimeReplayRecorder.RecordUntilEnd`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs) — implementiert (5.84); Rückgabe [`MatchRecordedRunResult`](../src/ScriptTanks.Core/Replay/MatchRecordedRunResult.cs) |
| **Replay frames** | [`MatchReplayFrame`](../src/ScriptTanks.Core/Replay/MatchReplayFrame.cs) mit **nur** [`MatchState`](../src/ScriptTanks.Core/Match/MatchState.cs) (Policy aus [COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md) §8) |
| **Logged combined runner** | **Existiert nicht** (`LoggedCombinedRuntimeRunner`) |
| **Logged combined replay** | **Existiert nicht** (`LoggedCombinedRuntimeReplayRecorder`) |
| **Combined script combat events** | **Existieren nicht** (keine `script_tick`, `fire_applied`, … in [`CombatLogEventTypes`](../src/ScriptTanks.Core/Logging/CombatLogEventTypes.cs)) |

```text
Heute möglich (5.83 / 5.84):
  CombinedRuntimeRunner.RunUntilEnd(...)
  CombinedRuntimeReplayRecorder.RecordUntilEnd(...)

Noch nicht möglich:
  LoggedCombinedRuntimeRunner / LoggedCombinedRuntimeReplayRecorder
  Per-tick script combat-log events
  Godot-Anbindung
```

## 3. Existing Logging Inventory

| Component | Path | Role today |
| --------- | ---- | ---------- |
| Logged match runner | [`LoggedMatchRunner.cs`](../src/ScriptTanks.Core/Match/LoggedMatchRunner.cs) | No-fire: `CreateMatchStartedLog` → [`MatchRunner`](../src/ScriptTanks.Core/Match/MatchRunner.cs)-Loop → `CreateMatchEndedLog` → [`CombatLogMerger.Merge`](../src/ScriptTanks.Core/Logging/CombatLogMerger.cs)(start, end) |
| Logged run result | [`LoggedMatchRunResult.cs`](../src/ScriptTanks.Core/Match/LoggedMatchRunResult.cs) | `MatchRunResult` + [`CombatLog`](../src/ScriptTanks.Core/Logging/CombatLog.cs) |
| Logged replay recorder | [`LoggedMatchReplayRecorder.cs`](../src/ScriptTanks.Core/Replay/LoggedMatchReplayRecorder.cs) | Recorder-Loop + gleiche Start/End-Log-Merge (no-fire) |
| Logged recorded run result | [`LoggedMatchRecordedRunResult.cs`](../src/ScriptTanks.Core/Replay/LoggedMatchRecordedRunResult.cs) | `MatchRecordedRunResult` + `CombatLog` |
| Match log factory | [`MatchCombatLogFactory.cs`](../src/ScriptTanks.Core/Logging/MatchCombatLogFactory.cs) | `CreateMatchStartedLog(SimTick)`, `CreateMatchEndedLog(SimTick, MatchEndConditionResult)` |
| Log merger | [`CombatLogMerger.cs`](../src/ScriptTanks.Core/Logging/CombatLogMerger.cs) | Konkateniert Einträge; Reihenfolge validiert [`CombatLog`](../src/ScriptTanks.Core/Logging/CombatLog.cs)-Ctor |
| Log building blocks | [`CombatLogBuilder.cs`](../src/ScriptTanks.Core/Logging/CombatLogBuilder.cs), [`CombatLogEntry.cs`](../src/ScriptTanks.Core/Logging/CombatLogEntry.cs), [`CombatLogEventTypes.cs`](../src/ScriptTanks.Core/Logging/CombatLogEventTypes.cs) | Immutable Snapshots; MVP-Konstanten `MatchStarted` / `MatchEnded` |

**Scheduled-fire-Pfade** ([`LoggedMatchRunner`](../src/ScriptTanks.Core/Match/LoggedMatchRunner.cs) / [`LoggedMatchReplayRecorder`](../src/ScriptTanks.Core/Replay/LoggedMatchReplayRecorder.cs) mit `scheduledFireRequests`): zusätzliche **Per-Tick**-Fire-Logs via [`LoggedMatchTickFireThenProjectilePipeline`](../src/ScriptTanks.Core/Match/LoggedMatchTickFireThenProjectilePipeline.cs). Dieses Muster wird für **Combined MVP nicht** übernommen.

Tests als Referenz: [`LoggedMatchRunnerTests`](../tests/ScriptTanks.Core.Tests/Match/LoggedMatchRunnerTests.cs), [`LoggedMatchReplayRecorderTests`](../tests/ScriptTanks.Core.Tests/Replay/LoggedMatchReplayRecorderTests.cs), [`MatchCombatLogFactoryTests`](../tests/ScriptTanks.Core.Tests/Logging/MatchCombatLogFactoryTests.cs), [`CombatLogMergerTests`](../tests/ScriptTanks.Core.Tests/Logging/CombatLogMergerTests.cs).

## 4. Combined Runtime Inventory

| Component | Path | Role |
| --------- | ---- | ---- |
| Combined runner | [`CombinedRuntimeRunner.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs) | End check → `CombinedRuntimeTickPipeline.Step` → end check |
| Combined run result | [`CombinedRuntimeRunResult.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunResult.cs) | Bundelt `FinalRuntime`, `MatchRunResult`, Sequence, optional letztes Tick-Result |
| Combined replay recorder | [`CombinedRuntimeReplayRecorder.cs`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs) | Gleiche Schleife + `MatchReplayFrame`-Liste |
| Combined tick pipeline | [`CombinedRuntimeTickPipeline.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickPipeline.cs) | **Einzige** erlaubte Per-Tick-Primitive für Runner/Recorder |
| Combined tick result | [`CombinedRuntimeTickResult.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickResult.cs) | Script + Tick-Zwischenergebnisse (nicht automatisch ins CombatLog) |
| Script composer | [`CombinedScriptRuntimeComposer.cs`](../src/ScriptTanks.Core/Scripting/CombinedScriptRuntimeComposer.cs) | Nur innerhalb von `CombinedRuntimeTickPipeline` |

**Wichtig:** Logged-Wrapper (5.87 / 5.88) rufen die **bestehenden** statischen Runner/Recorder auf. Sie **implementieren die Schleife nicht neu** und rufen **nicht** direkt `CombinedScriptRuntimeComposer.Run` oder `MatchTickPipeline.Step` auf.

## 5. MVP Logged Runner Policy (Task 5.87)

Geplanter Ablauf — analog zum **no-fire**-Pfad von [`LoggedMatchRunner.RunUntilEnd`](../src/ScriptTanks.Core/Match/LoggedMatchRunner.cs):

```text
startTick = initialRuntime.State.CurrentTick
startLog = MatchCombatLogFactory.CreateMatchStartedLog(startTick)

runResult = CombinedRuntimeRunner.RunUntilEnd(
    initialRuntime, programs, initialProjectileIdSequence, maxTicks)

endLog = MatchCombatLogFactory.CreateMatchEndedLog(
    runResult.FinalRuntime.State.CurrentTick,
    runResult.RunResult.EndCondition)

mergedLog = CombatLogMerger.Merge(startLog, endLog)

return new LoggedCombinedRuntimeRunResult(runResult, mergedLog)
```

| Aspekt | Policy |
| ------ | ------ |
| Delegation | Gesamte Match-Schleife bleibt in [`CombinedRuntimeRunner`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs) |
| `programs`-Validation | Unverändert delegiert an ersten `CombinedRuntimeTickPipeline.Step` (siehe Runner-Tests: `maxTicks == 0` → kein Step → keine Program-Validation) |
| Tick für Logs | `MatchState.CurrentTick` auf dem gewrappten State (wie Match-Logged-Runner) |
| Per-Tick-Logs | **Keine** in MVP |

```mermaid
flowchart LR
    start[CreateMatchStartedLog]
    run[CombinedRuntimeRunner.RunUntilEnd]
    end[CreateMatchEndedLog]
    merge[CombatLogMerger.Merge]
    out[LoggedCombinedRuntimeRunResult]
    start --> run --> end --> merge --> out
```

## 6. MVP Logged Replay Policy (Task 5.88)

Geplanter Ablauf — Recorder zuerst, Logs aus `recorded.RunResult` (nicht aus Per-Tick-`CombinedRuntimeTickResult`):

```text
recorded = CombinedRuntimeReplayRecorder.RecordUntilEnd(
    initialRuntime, programs, initialProjectileIdSequence, maxTicks)

startLog = MatchCombatLogFactory.CreateMatchStartedLog(
    initialRuntime.State.CurrentTick)

endLog = MatchCombatLogFactory.CreateMatchEndedLog(
    recorded.RunResult.FinalState.CurrentTick,
    recorded.RunResult.EndCondition)

mergedLog = CombatLogMerger.Merge(startLog, endLog)

return new LoggedCombinedRuntimeRecordedRunResult(recorded, mergedLog)
```

| Aspekt | Policy |
| ------ | ------ |
| Replay frames | Unverändert: `MatchState`-only, Frame 0 = `initialRuntime.State`, Frame *k* nach *k* vollem Combined-Tick ([5.84](COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md) §8) |
| Logs | Nur aus `MatchRunResult` innerhalb von `MatchRecordedRunResult` |
| `FinalRuntime` / Sequence | **Nicht** auf dem Recording-Typ; letzter Frame + `RunResult.FinalState` reichen für MVP |

```mermaid
flowchart LR
    rec[CombinedRuntimeReplayRecorder.RecordUntilEnd]
    start[CreateMatchStartedLog]
    end[CreateMatchEndedLog]
    merge[CombatLogMerger.Merge]
    out[LoggedCombinedRuntimeRecordedRunResult]
    rec --> start
    rec --> end
    start --> merge
    end --> merge
    merge --> out
```

## 7. Event Scope

### MVP (nur diese Events)

| Event | Konstante | Quelle |
| ----- | --------- | ------ |
| Match start | [`CombatLogEventTypes.MatchStarted`](../src/ScriptTanks.Core/Logging/CombatLogEventTypes.cs) (`"match_started"`) | [`MatchCombatLogFactory.CreateMatchStartedLog`](../src/ScriptTanks.Core/Logging/MatchCombatLogFactory.cs) |
| Match end | [`CombatLogEventTypes.MatchEnded`](../src/ScriptTanks.Core/Logging/CombatLogEventTypes.cs) (`"match_ended"`) | [`MatchCombatLogFactory.CreateMatchEndedLog`](../src/ScriptTanks.Core/Logging/MatchCombatLogFactory.cs) |

### Explizit zurückgestellt (eigene Design-Tasks, z. B. 5.89)

- `script_tick`
- `script_intent_evaluated`
- `sensor_request_applied`
- `fire_request_constructed`
- `fire_request_applied`
- `fire_rejected`

**Begründung:** [`CombinedRuntimeTickResult`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickResult.cs) enthält bereits reichhaltige Script-/Fire-/Sensor-Diagnostics. Das CombatLog-Schema soll **nicht** ad hoc wachsen, bevor Event-Formen und Payloads stabil sind. Wahrscheinlicher späterer Weg: dediziertes `LoggedCombinedRuntimeTickPipeline` — analog zu [`LoggedMatchTickFireThenProjectilePipeline`](../src/ScriptTanks.Core/Match/LoggedMatchTickFireThenProjectilePipeline.cs).

## 8. Log Ordering

### MVP

| Eintrag | Tick | Position |
| ------- | ---- | -------- |
| `match_started` | `initialRuntime.State.CurrentTick` (bzw. Frame-0-Tick bei Replay) | Erster Eintrag |
| `match_ended` | Finaler `MatchState.CurrentTick` nach Run/Recording | Letzter Eintrag |

- Genau **zwei** Einträge im MVP.
- [`CombatLogMerger`](../src/ScriptTanks.Core/Logging/CombatLogMerger.cs) konkateniert; [`CombatLog`](../src/ScriptTanks.Core/Logging/CombatLog.cs) erzwingt **nicht absteigende** Tick-Reihenfolge über alle Einträge.

### Spätere Per-Tick-Script-Logs (Constraint für 5.89+)

Wenn zusätzliche Events kommen:

- Strikt **zwischen** `match_started` und `match_ended`
- Sortierung: `SimTick`, dann stabiler Record-Index innerhalb des Ticks
- Keine Dictionary-Iteration ohne feste Reihenfolge

## 9. Result Model Sketch (Task 5.86)

Geplante reine Data-Carrier (noch **nicht** implementiert):

```text
LoggedCombinedRuntimeRunResult
├─ CombinedRuntimeRunResult RunResult
└─ CombatLog Log

LoggedCombinedRuntimeRecordedRunResult
├─ MatchRecordedRunResult RecordedRunResult
└─ CombatLog Log
```

Ctor-Muster spiegeln [`LoggedMatchRunResult`](../src/ScriptTanks.Core/Match/LoggedMatchRunResult.cs) / [`LoggedMatchRecordedRunResult`](../src/ScriptTanks.Core/Replay/LoggedMatchRecordedRunResult.cs):

- `ArgumentNullException.ThrowIfNull` auf beiden Properties
- Kein `Equals`-Override (Referenz-Identität)
- Keine Logik im Result-Typ

**Nicht** in MVP: `CombinedRuntimeRecordedRunResult` (zusätzlicher Wrapper um Run + Recording + Log), außer ein späterer Task zeigt Bedarf.

## 10. Determinism Requirements

| Requirement | Mechanism |
| ----------- | --------- |
| Simulation | Gleiche Eingaben wie ungeloggter Pfad → identisches `CombinedRuntimeRunResult` / `MatchRecordedRunResult` |
| Combat log | Gleiche Factory-Aufrufe → gleiche Tick-, Event-Type- und Message-Einträge |
| Keine Entropie | Kein `Random`, keine GUIDs, keine Systemuhr in der Log-Schicht |
| Keine Seiteneffekte | Logging darf `initialRuntime`, Replay-Frames oder Runner-Ergebnisse **nicht** mutieren |
| Merge | [`CombatLogMerger.Merge`](../src/ScriptTanks.Core/Logging/CombatLogMerger.cs) nur auf deterministisch gebauten `CombatLog`-Snapshots |

Test-Präzedenz (ungeloggt): [`CombinedRuntimeRunnerTests`](../tests/ScriptTanks.Core.Tests/Scripting/CombinedRuntimeRunnerTests.cs), [`CombinedRuntimeReplayRecorderTests`](../tests/ScriptTanks.Core.Tests/Replay/CombinedRuntimeReplayRecorderTests.cs). Logged-Tests (5.87+): analog [`LoggedMatchRunnerTests`](../tests/ScriptTanks.Core.Tests/Match/LoggedMatchRunnerTests.cs).

## 11. Out of Scope (Task 5.85)

- Keine Implementierung in `src/` oder `tests/`
- Keine neuen [`CombatLogEventTypes`](../src/ScriptTanks.Core/Logging/CombatLogEventTypes.cs), keine `CombatLog`-Schema-Änderung
- Keine Replay-Schema-Erweiterung (`CombinedRuntimeReplayFrame`, Sensor-Loadouts in Frames, …)
- Kein `LoggedCombinedRuntimeTickPipeline`, kein CombatLog-UI, kein Godot, kein Netzwerk
- Keine Änderung an [`CombinedRuntimeRunner`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs), [`CombinedRuntimeReplayRecorder`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs), [`MatchRunner`](../src/ScriptTanks.Core/Match/MatchRunner.cs)

## 12. Recommended Follow-Up Tasks

| Task | Content |
| ---- | ------- |
| **5.86** | `LoggedCombinedRuntimeRunResult`, `LoggedCombinedRuntimeRecordedRunResult` Modelle + Tests (Ctor wie [`LoggedMatchRunResult`](../src/ScriptTanks.Core/Match/LoggedMatchRunResult.cs) / [`LoggedMatchRecordedRunResult`](../src/ScriptTanks.Core/Replay/LoggedMatchRecordedRunResult.cs)) |
| **5.87** | `LoggedCombinedRuntimeRunner` — Start/End-Logs, delegiert an [`CombinedRuntimeRunner`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs) |
| **5.88** | `LoggedCombinedRuntimeReplayRecorder` — Start/End-Logs um [`CombinedRuntimeReplayRecorder`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs) |
| **5.89** | Optional: Script-/Tick-Combat-Event-Design (`LoggedCombinedRuntimeTickPipeline`?, Event-Payloads, Ordering) |
| **Geometry / combat** *(orthogonal)* | Projektil Self-Hit / Mündungs-Overlap — siehe [COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md) §12; kein Logging-Blocker |

**Nach 5.85:** entweder 5.86+ implementieren oder Pause für Geometry-Follow-up.

## 13. Definition of Done (Task 5.85)

- [`docs/LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md`](LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md) existiert mit Abschnitten 1–12 (+ DoD)
- MVP-Policy: nur `match_started` / `match_ended`; deferred Script-Events explizit
- Alle Links repo-relativ von `docs/` (`../src/...`, `../tests/...`, Peer-`*.md`) — **keine** absoluten Windows-Pfade
- `dotnet build ScriptTanks.sln -warnaserror` und `dotnet test ScriptTanks.sln` unverändert grün (nur Markdown)
