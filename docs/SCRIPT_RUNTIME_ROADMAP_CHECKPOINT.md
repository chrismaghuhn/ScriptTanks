# Task 5.101 — Script Runtime Roadmap Checkpoint

> **Nur Dokumentation.** Keine Implementierung, keine Tests, keine Änderungen an `src/`, `tests/`, Projekten, Solution oder `game/`.
>
> Sprache: Erklärungen überwiegend auf Deutsch; Typnamen und API-Namen wie im Code auf Englisch.

## 1. Purpose

Nach Tasks **5.71–5.100** ist ein langer Combined-Runtime-Bogen abgeschlossen: Script-Fire von Intent bis Anwendung, Per-Tick-Pipeline, Runner/Replay, opt-in Rich-Logging, SpawnTick-Owner-Filter und Geometrie-Checkpoint.

**Ziel von 5.101:** Eine autoritative Roadmap-Referenz, die vor weiteren Systemen beantwortet:

1. Was ist heute fertig?
2. Welche öffentlichen APIs existieren?
3. Was fehlt noch architektonisch?
4. Welcher Track kommt als Nächstes?
5. Worauf sollen Tasks **5.102–5.110** fokussieren?

Dieses Dokument ändert **kein** Verhalten. Es trifft Planungsentscheidungen für 5.102+.

**Task-Nummerierung:** [MUZZLE_RADIUS_TUNING_CHECKPOINT.md](MUZZLE_RADIUS_TUNING_CHECKPOINT.md) §6 nannte optionales Katalog-Tuning „5.101 deferred“. **Dieser Checkpoint definiert 5.101 neu** als Roadmap; **Muzzle-/Radius-Tuning bleibt zurückgestellt**. Implementierung startet bei **5.102**.

Querverweise: [COMBINED_RUNTIME_LOGGING_CHECKPOINT.md](COMBINED_RUNTIME_LOGGING_CHECKPOINT.md), [PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md](PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md), [PROJECTILE_SPAWN_TICK_INVENTORY.md](PROJECTILE_SPAWN_TICK_INVENTORY.md), [MUZZLE_RADIUS_TUNING_CHECKPOINT.md](MUZZLE_RADIUS_TUNING_CHECKPOINT.md).

**Test-Baseline (Repo):** 2669 Tests nach 5.100.

## 2. Completed Milestone Summary

| Bereich | Ergebnis | Referenz |
| ------- | -------- | -------- |
| **5.71–5.74** | Script-gemappte Fire-Request-Konstruktion und -Anwendung | [`ScriptMappedFireRequestConstructionPipeline.cs`](../src/ScriptTanks.Core/Scripting/ScriptMappedFireRequestConstructionPipeline.cs), [`ScriptMappedFireRequestApplicationPipeline.cs`](../src/ScriptTanks.Core/Scripting/ScriptMappedFireRequestApplicationPipeline.cs) |
| **5.75–5.80** | Combined Script Runtime Composer + Per-Tick-Pipeline (Option A) | [COMBINED_SCRIPT_RUNTIME_TICK_PLAN.md](COMBINED_SCRIPT_RUNTIME_TICK_PLAN.md), [`CombinedScriptRuntimeComposer.cs`](../src/ScriptTanks.Core/Scripting/CombinedScriptRuntimeComposer.cs), [`CombinedRuntimeTickPipeline.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickPipeline.cs) |
| **5.81–5.84** | Combined Runner + Replay Recorder | [COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md), [`CombinedRuntimeRunner.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs), [`CombinedRuntimeReplayRecorder.cs`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs) |
| **5.85–5.95** | Logged Combined Runner/Replay, Rich opt-in Tick-Logs, Logging-Checkpoint | [LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md), [COMBINED_RUNTIME_LOGGING_CHECKPOINT.md](COMBINED_RUNTIME_LOGGING_CHECKPOINT.md) |
| **5.96–5.100** | SpawnTick Owner-Self-Hit-Policy, Combined-Regressionen, Muzzle-/Radius-Checkpoint | [PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md](PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md), [PROJECTILE_SPAWN_TICK_INVENTORY.md](PROJECTILE_SPAWN_TICK_INVENTORY.md), [MUZZLE_RADIUS_TUNING_CHECKPOINT.md](MUZZLE_RADIUS_TUNING_CHECKPOINT.md) |

## 3. Current API Inventory

Kompakte Referenz der Combined-Runtime-Schicht (erweitert [COMBINED_RUNTIME_LOGGING_CHECKPOINT.md](COMBINED_RUNTIME_LOGGING_CHECKPOINT.md) §3).

### Script composition and tick

| API | Datei | Rolle |
| --- | ----- | ----- |
| `CombinedScriptRuntimeComposer.Run` | [`CombinedScriptRuntimeComposer.cs`](../src/ScriptTanks.Core/Scripting/CombinedScriptRuntimeComposer.cs) | Integration → Mapping → Sensor apply → Fire construct/apply → merged runtime |
| `CombinedScriptRuntimeComposerResult` | [`CombinedScriptRuntimeComposerResult.cs`](../src/ScriptTanks.Core/Scripting/CombinedScriptRuntimeComposerResult.cs) | Träger aller Zwischenergebnisse |
| `CombinedRuntimeTickPipeline.Step` | [`CombinedRuntimeTickPipeline.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickPipeline.cs) | Composer, dann `MatchTickPipeline.Step` |
| `CombinedRuntimeTickResult` | [`CombinedRuntimeTickResult.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickResult.cs) | Ein-Tick-Ergebnis inkl. `ScriptResult` und finaler Runtime |

### Fire and sensor sub-pipelines (innerhalb Composer)

| API | Datei |
| --- | ----- |
| `ScriptMappedSensorRequestApplicationPipeline` | [`ScriptMappedSensorRequestApplicationPipeline.cs`](../src/ScriptTanks.Core/Scripting/ScriptMappedSensorRequestApplicationPipeline.cs) |
| `ScriptMappedFireRequestConstructionPipeline` | [`ScriptMappedFireRequestConstructionPipeline.cs`](../src/ScriptTanks.Core/Scripting/ScriptMappedFireRequestConstructionPipeline.cs) |
| `ScriptMappedFireRequestApplicationPipeline` | [`ScriptMappedFireRequestApplicationPipeline.cs`](../src/ScriptTanks.Core/Scripting/ScriptMappedFireRequestApplicationPipeline.cs) |

### Ungeloggt: Runner und Replay

| API | Returns | Datei |
| --- | ------- | ----- |
| `CombinedRuntimeRunner.RunUntilEnd` | `CombinedRuntimeRunResult` | [`CombinedRuntimeRunner.cs`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs) |
| `CombinedRuntimeReplayRecorder.RecordUntilEnd` | `MatchRecordedRunResult` | [`CombinedRuntimeReplayRecorder.cs`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs) |

### Logged: MVP und Rich (opt-in)

| API | Datei |
| --- | ----- |
| `LoggedCombinedRuntimeRunner` | [`LoggedCombinedRuntimeRunner.cs`](../src/ScriptTanks.Core/Scripting/LoggedCombinedRuntimeRunner.cs) |
| `LoggedCombinedRuntimeReplayRecorder` | [`LoggedCombinedRuntimeReplayRecorder.cs`](../src/ScriptTanks.Core/Replay/LoggedCombinedRuntimeReplayRecorder.cs) |
| `LoggedCombinedRuntimeTickPipeline` | [`LoggedCombinedRuntimeTickPipeline.cs`](../src/ScriptTanks.Core/Scripting/LoggedCombinedRuntimeTickPipeline.cs) |
| `CombinedRuntimeTickCombatLogFactory` | [`CombinedRuntimeTickCombatLogFactory.cs`](../src/ScriptTanks.Core/Logging/CombinedRuntimeTickCombatLogFactory.cs) |

### Self-hit policy (5.98+)

| API | Datei |
| --- | ----- |
| `ProjectileState.SpawnTick` | [`ProjectileState.cs`](../src/ScriptTanks.Core/Projectiles/ProjectileState.cs) |
| `MatchStateProjectileHitSystem.ResolveProjectileHits` | [`MatchStateProjectileHitSystem.cs`](../src/ScriptTanks.Core/Match/MatchStateProjectileHitSystem.cs) |

### Test-Anker (Auswahl)

| Bereich | Datei |
| ------- | ----- |
| Combined tick / fire | [`CombinedRuntimeTickPipelineTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/CombinedRuntimeTickPipelineTests.cs) |
| Logged runner | [`LoggedCombinedRuntimeRunnerTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/LoggedCombinedRuntimeRunnerTests.cs) |
| Logged replay | [`LoggedCombinedRuntimeReplayRecorderTests.cs`](../tests/ScriptTanks.Core.Tests/Replay/LoggedCombinedRuntimeReplayRecorderTests.cs) |

## 4. Current Gameplay Capability

| Fähigkeit | Status | Beleg (Typ-/Dateinamen, keine Zeilennummern) |
| --------- | ------ | --------------------------------------------- |
| Sensor-Scan-Anwendung | **Implementiert** | [`CombinedScriptRuntimeComposer`](../src/ScriptTanks.Core/Scripting/CombinedScriptRuntimeComposer.cs) verdrahtet [`ScriptMappedSensorRequestApplicationPipeline`](../src/ScriptTanks.Core/Scripting/ScriptMappedSensorRequestApplicationPipeline.cs) |
| Fire-Konstruktion + -Anwendung | **Implementiert** | Derselbe Composer verdrahtet [`ScriptMappedFireRequestConstructionPipeline`](../src/ScriptTanks.Core/Scripting/ScriptMappedFireRequestConstructionPipeline.cs) und [`ScriptMappedFireRequestApplicationPipeline`](../src/ScriptTanks.Core/Scripting/ScriptMappedFireRequestApplicationPipeline.cs) |
| Projektil Tick / Hit / Cleanup | **Implementiert** | [`CombinedRuntimeTickPipeline`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickPipeline.cs) delegiert nach der Script-Phase an [`MatchTickPipeline`](../src/ScriptTanks.Core/Match/MatchTickPipeline.cs) |
| Runner / Replay / Logging | **Implementiert** | APIs in §3; Tests z. B. [`LoggedCombinedRuntimeRunnerTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/LoggedCombinedRuntimeRunnerTests.cs) |
| Movement-Script-Anwendung | **Fehlt** | [`MovementPlaceholder`](../src/ScriptTanks.Core/Scripting/ScriptDomainRequestMappingRecord.cs) im Mapper; Composer-Remarks schließen Movement-Ausführung aus |
| Turret-Script-Anwendung | **Fehlt** | [`TurretPlaceholder`](../src/ScriptTanks.Core/Scripting/ScriptDomainRequestMappingRecord.cs); `AimAtEnemy` → `ScriptDomainRequestCategory.Turret` ohne Application-Pipeline |
| Rich verbose Diagnostics | **Teilweise** | MVP + opt-in `script_tick` / `fire_*` ([`CombatLogEventTypes.cs`](../src/ScriptTanks.Core/Logging/CombatLogEventTypes.cs)); Projektil-/Damage-Konstanten werden im Rich-Combined-Pfad nicht emittiert |

[`ScriptCommandType`](../src/ScriptTanks.Core/Scripting/ScriptCommandType.cs) kennt `MoveToPatrolPoint`, `Retreat` und `AimAtEnemy`; die Domain-Mapper-Produktion endet bei Placeholdern ([`ScriptTranslatedCommandDomainMapper.cs`](../src/ScriptTanks.Core/Scripting/ScriptTranslatedCommandDomainMapper.cs)).

## 5. Stable Invariants Now Locked

Diese Regeln gelten für alle weiteren Script-Domain-Erweiterungen:

| Invariante | Kurzbeschreibung |
| ---------- | ---------------- |
| **Tick-Reihenfolge (Option A)** | Script-Lauf (`CombinedScriptRuntimeComposer`) **vor** [`MatchTickPipeline.Step`](../src/ScriptTanks.Core/Match/MatchTickPipeline.cs) — siehe [`CombinedRuntimeTickPipeline`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeTickPipeline.cs) |
| **Fire-Zielrichtung** | Feuer nutzt `TurretRotation` ([`FireMuzzlePositionResolver`](../src/ScriptTanks.Core/Combat/FireMuzzlePositionResolver.cs), [PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md](PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md)) |
| **Muzzle / Geschwindigkeit** | Mündung = Zentrum + Vorwärts × Offset; Geschwindigkeit = Vorwärts × Projektilgeschwindigkeit |
| **`SpawnTick` Erhalt** | `SpawnTick` bleibt über alle `ProjectileState.With*`-Pfade erhalten |
| **Owner-Filter** | Owner nur ausgeschlossen, wenn `SpawnTick == CurrentTick` ([`MatchStateProjectileHitSystem`](../src/ScriptTanks.Core/Match/MatchStateProjectileHitSystem.cs)) |
| **Replay-Frames** | Nur [`MatchState`](../src/ScriptTanks.Core/Match/MatchState.cs) — [COMBINED_RUNTIME_LOGGING_CHECKPOINT.md](COMBINED_RUNTIME_LOGGING_CHECKPOINT.md) §5 |
| **Rich Logging** | Opt-in über `*WithTickLogs`; MVP-Default nur `match_started` / `match_ended` |

```mermaid
flowchart LR
  script[CombinedScriptRuntimeComposer]
  tick[MatchTickPipeline.Step]
  script --> tick
```

## 6. Open Architecture Gaps

1. **Movement Request Application** — `MoveToPatrolPoint` / `Retreat` werden klassifiziert, aber es gibt keine Anwendungs-Pipeline, die `MovementState` mutiert.
2. **Turret Request Application** — `AimAtEnemy` landet in `ScriptDomainRequestCategory.Turret` ohne Turn-Rate-Policy und ohne Rotation-Mutation.
3. **Multi-Domain-Priorisierung** — Ein Tick kann Sensor-, Weapon-, Movement- und Turret-Intents mappen; der Composer führt heute nur Sensor und Fire aus; Reihenfolge und Konfliktregeln für neue Domains fehlen.
4. **Runner/Replay-Produktform** — `CombinedRuntime*` existiert parallel zu [`MatchRunner`](../src/ScriptTanks.Core/Match/MatchRunner.cs) / [`MatchReplayRecorder`](../src/ScriptTanks.Core/Replay/MatchReplayRecorder.cs); keine vereinheitlichte Produkt-API.
5. **Godot-Grenze** — `game/` ist nicht an den Combined-Runtime-Loop angebunden.
6. **Rich Diagnostics** — [`CombatLogEventTypes`](../src/ScriptTanks.Core/Logging/CombatLogEventTypes.cs) definiert Projektil-/Damage-Events; der Rich-Combined-Pfad emittiert sie nicht (bewusst enger Scope).
7. **Balance / Geometrie-Tuning** — laut [MUZZLE_RADIUS_TUNING_CHECKPOINT.md](MUZZLE_RADIUS_TUNING_CHECKPOINT.md) zurückgestellt; sinnvoll erst nach Movement/Turret.

## 7. Candidate Next Tracks

| Track | Spielwert | Risiko / Aufwand |
| ----- | --------- | ---------------- |
| **A — Movement Application** | Hoch: nächste echte `MatchState`-Mutation neben Fire | Deterministische Movement-Request-Semantik, Integration in Composer + Tests |
| **B — Turret Application** | Hoch für Aim/Fire-Qualität (`AimAtEnemy`) | Turn-Rate-Policy, Reihenfolge zu Movement/Fire |
| **C — Rich Diagnostics** | Nützlich für Debugging | Log-/Event-Creep; weniger Gameplay-Wert |
| **D — Godot Integration** | Visuell überzeugend | Zu früh ohne Movement/Turret-Loop |
| **E — Balance / Muzzle Tuning** | Später relevant | Zu früh ohne steuerbare Panzer |

## 8. Recommended Track

**Gesperrte Empfehlung:**

```text
Nächster Haupt-Track: Script Movement / Turret Command Application

5.102–5.106: Movement Application Track (zuerst)
5.107–5.110: Turret Application Track (danach, nach Movement-Landung)
```

**Reihenfolge:**

1. **Movement zuerst** — nächste echte Zustandsänderung in `MatchState` neben Fire; validiert das Domain-Expansion-Muster (map → apply → composer → tick → runner/replay) auf einer neuen Kategorie.
2. **Turret danach** — wichtig für Zielrichtung und Fire-Qualität, aber Movement beweist zuerst den Combined-Runtime-Integrationsloop, bevor Turn-Rate-Policy dazukommt.
3. **Erst danach:** Godot-Visualisierung, zusätzliche CombatLog-Events, Balance/Muzzle-Tuning.

Fire-Pfad und Sensor-Anwendung sind abgeschlossen. Godot, Log-Ausbau und Balance bleiben zurückgestellt, bis Movement und Turret den Script-Action-Loop schließen.

## 9. Proposed Follow-up Tasks

| Task | Inhalt |
| ---- | ------ |
| **5.102** | Plan: Script Movement Request Application |
| **5.103** | Movement Application Result Models |
| **5.104** | Movement Application Pipeline |
| **5.105** | Combined Composer/Tick-Integration für Movement |
| **5.106** | Runner/Replay/Logging-Regression für Movement |
| **5.107** | Plan: Turret Command Application |
| **5.108** | Turret Result Models |
| **5.109** | Turret Application Pipeline |
| **5.110** | Combined Runtime Turret Integration |

## 10. What Not to Do Next

Explizit **nicht** als Nächstes:

- Godot-Integration oder Render-Collision-Anbindung
- Weitere [`CombatLog`](../src/ScriptTanks.Core/Logging/CombatLog.cs)-Event-Typen im Combined-Rich-Pfad
- Muzzle-/Radius-Tuning in [`WeaponCatalog`](../src/ScriptTanks.Core/Weapons/WeaponCatalog.cs) (weiter deferred)
- Replay-Schema-Änderungen oder neue Frame-Typen
- Vollständiger Ersatz von [`MatchRunner`](../src/ScriptTanks.Core/Match/MatchRunner.cs) ohne Migrationsplan
- AI- oder Scripting-Sprach-Erweiterungen

## 11. Verification

Nach dem Schreiben dieses Dokuments (docs-only):

```powershell
dotnet build ScriptTanks.sln -warnaserror
dotnet test ScriptTanks.sln --no-build
```

**Erwartung:** 2669 Tests bestanden, 0 Warnungen.

## 12. Definition of Done

- [x] [`docs/SCRIPT_RUNTIME_ROADMAP_CHECKPOINT.md`](SCRIPT_RUNTIME_ROADMAP_CHECKPOINT.md) existiert mit **§1–§12**
- [x] Meilensteine 5.71–5.100 zusammengefasst (§2)
- [x] APIs inventarisiert mit repo-relativen Links: `../src/`, `../tests/`, Peer-Docs ohne `../`
- [x] Keine absoluten Pfade (`C:\...`) und keine Zeilennummern-Belege
- [x] Offene Lücken dokumentiert (§6); nächster Track Movement → Turret gesperrt (§8)
- [x] Follow-up-Tasks 5.102–5.110 gelistet (§9)
- [x] `dotnet build` + vollständiger Testlauf: **2669** unverändert
