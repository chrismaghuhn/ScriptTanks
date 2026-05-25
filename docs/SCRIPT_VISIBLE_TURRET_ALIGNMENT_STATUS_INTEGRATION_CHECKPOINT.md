# Task 5.155 — Script-visible Turret Alignment / Aim Status Integration Checkpoint

> **Nur Dokumentation.** Keine Implementierung, keine Tests, keine Änderungen an `src/`, `tests/`, `game/`, Projektdateien oder bestehenden Checkpoints.
>
> Stand nach Task 5.153: `dotnet build ScriptTanks.sln -warnaserror` ohne Warnungen, `dotnet test ScriptTanks.sln` mit 3231 bestandenen Tests.
>
> Sprache: Erklärungen überwiegend auf Deutsch; Typnamen und API-Namen wie im Code auf Englisch.

## 1. Purpose

Der **Aim-Status-Track 5.148–5.153** ist abgeschlossen. Scripts können Turm-Aim- und Alignment-Status über neue `ScriptConditionType`-Werte beobachten. Der Core-Loop unterstützt das Script-Muster: **drehen solange `Turning`, feuern sobald `Aligned`**.

**Ziel von 5.155:** Autoritative Checkpoint-Referenz **nach** Abschluss von 5.148–5.153. Dieses Dokument ändert **kein** Laufzeitverhalten.

**Baseline:** **3231** Tests, **0** Warnungen.

### Supersedes plan-only narrative

[SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md](SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md) (Task 5.148) beschrieb den geplanten Track. **Dieser Checkpoint** ist die authoritative Referenz für den **implementierten** Stand nach 5.153. Die Plan-Datei wird **nicht** editiert.

| Thema | Authoritative Quelle |
| ----- | -------------------- |
| Script-sichtbarer Turm-Aim-/Alignment-Status | **Dieses Dokument** |
| Gradual rotation + Fire-while-turning (System-Policy) | [TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md](TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md) |
| Full-Angle Aim / Inverse Lookup | [FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md](FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md) |
| Combined-Tick-Reihenfolge | [WALL_OBSTACLE_COLLISION_INTEGRATION_CHECKPOINT.md](WALL_OBSTACLE_COLLISION_INTEGRATION_CHECKPOINT.md) §3 |

Querverweise: [SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md](SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md), [TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md](TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md), [FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md](FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md).

## 2. Completed Milestone Summary

| Task | Ergebnis |
| ---- | -------- |
| **5.148** | [SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md](SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md) — Plan für script-sichtbaren Turm-Aim-/Alignment-Status |
| **5.149** | [`ScriptVisibleTurretAimStatus`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatus.cs) + [`ScriptVisibleTurretAimStatusResult`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatusResult.cs); `OwnerDestroyed`-Policy gesperrt |
| **5.150** | [`ScriptVisibleTurretAimStatusResolver`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatusResolver.cs) — `Resolve(MatchState, tankIndex)` |
| **5.151** | [`ScriptConditionType`](../src/ScriptTanks.Core/Scripting/ScriptConditionType.cs)-Erweiterungen + Condition-Evaluator-Wiring |
| **5.152** | Composer-/Tick-Pipeline aim-until-fire-Regressionen |
| **5.153** | Runner-/Replay aim-until-fire-Regressionen |

### Test-Fortschrittsleiter (5.148–5.153)

```text
3148 → 3179 → 3201 → 3223 → 3228 → 3231
```

| Meilenstein | Tests |
| ----------- | ----- |
| 5.148 plan (docs-only) | 3148 |
| 5.149 result models | 3179 |
| 5.150 resolver | 3201 |
| 5.151 condition wiring | 3223 |
| 5.152 composer regression | 3228 |
| 5.153 runner/replay regression | **3231** |

## 3. Current Authoritative Aim Status Architecture

Aim-Status wird **pro Tick und pro Tank** beim Aufbau des `ScriptEvaluationContext` berechnet. Er fließt in die Routine-Auswahl ein, **bevor** `AimAtEnemy` oder `Fire` angewendet werden.

### Kernpfad

```text
MatchSensorRuntimeState
  → ScriptRuntimeContextBuilder.Build(runtime, tankIndex)
  → ScriptVisibleTurretAimStatusResolver.Resolve(runtime.State, tankIndex)
  → ScriptEvaluationContext.TurretAimStatus
  → ScriptConditionEvaluator.Evaluate(condition, context)
  → ScriptRoutineSelector.SelectFirstValid(...)
  → ausgewählter ScriptCommand (z. B. AimAtEnemy, Fire, NoOp)
```

```mermaid
flowchart LR
  runtime[MatchSensorRuntimeState]
  builder[ScriptRuntimeContextBuilder]
  resolver[ScriptVisibleTurretAimStatusResolver]
  context[ScriptEvaluationContext.TurretAimStatus]
  evaluator[ScriptConditionEvaluator]
  selector[ScriptRoutineSelector]
  command[Selected ScriptCommand]

  runtime --> builder
  builder --> resolver
  resolver --> context
  context --> evaluator
  evaluator --> selector
  selector --> command
```

### Wichtige Eigenschaften

| Eigenschaft | Semantik |
| ----------- | -------- |
| Eingabe | `MatchState` + `tankIndex` — **kein** `ScriptEvaluationContext` |
| Zielauswahl | Positionsbasiert, spiegelt Turret-Pipeline — **kein** Sensor-Scan |
| Mutationsfrei | Resolver mutiert `MatchState` nicht |
| Fire-Gate | Aim-Status **gated Fire nicht** auf Systemebene; Scripts **können** wählen, nicht zu feuern |
| Sensor-Divergenz | `EnemyVisible` ≠ `HasAimTarget` (bewusst getrennt) |

## 4. API Inventory

| API | Rolle |
| --- | ----- |
| [`ScriptVisibleTurretAimStatus`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatus.cs) | Enum: `NoTarget`, `MissingAimSolution`, `Aligned`, `Turning`, `OwnerDestroyed` |
| [`ScriptVisibleTurretAimStatusResult`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatusResult.cs) | Result-Modell: Owner, Target, current/desired rotation, delta, magnitude, `HasTarget` |
| [`ScriptVisibleTurretAimStatusResolver`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatusResolver.cs) | Reiner computed Resolver aus `MatchState + tankIndex` |
| [`ScriptConditionType`](../src/ScriptTanks.Core/Scripting/ScriptConditionType.cs) | Erweiterung: `TurretAligned`, `TurretTurning`, `HasAimTarget`, `MissingAimSolution` |
| [`ScriptEvaluationContext`](../src/ScriptTanks.Core/Scripting/ScriptEvaluationContext.cs) | Speichert `TurretAimStatus`-Snapshot |
| [`ScriptRuntimeContextBuilder`](../src/ScriptTanks.Core/Scripting/ScriptRuntimeContextBuilder.cs) | Befüllt `TurretAimStatus` über Resolver |
| [`ScriptConditionEvaluator`](../src/ScriptTanks.Core/Scripting/ScriptConditionEvaluator.cs) | Wertet neue Aim-Status-Condition-Typen aus |
| [`ScriptRoutineSelector`](../src/ScriptTanks.Core/Scripting/ScriptRoutineSelector.cs) | First-match Routine-Auswahl |
| [`CombinedScriptRuntimeComposer`](../src/ScriptTanks.Core/Scripting/CombinedScriptRuntimeComposer.cs) | Script-Entscheidungen in normaler Composer-Phase |
| [`CombinedRuntimeRunner`](../src/ScriptTanks.Core/Scripting/CombinedRuntimeRunner.cs) | Multi-Tick aim-until-fire über `RunUntilEnd` |
| [`CombinedRuntimeReplayRecorder`](../src/ScriptTanks.Core/Replay/CombinedRuntimeReplayRecorder.cs) | Zeichnet sichtbare `MatchState`-Frames auf |

## 5. Status and Resolver Semantics

### `ScriptVisibleTurretAimStatus`

| Status | Bedeutung |
| ------ | --------- |
| `OwnerDestroyed` | Owner-Tank ist zerstört und kann nicht sinnvoll zielen |
| `NoTarget` | Owner kann zielen, aber kein lebender Ziel-Tank existiert |
| `MissingAimSolution` | Ziel existiert, aber Aim kann nicht aufgelöst werden (z. B. gleiche Position) |
| `Aligned` | Normalisierte aktuelle Turmrotation entspricht `desiredRotation` |
| `Turning` | Ziel existiert, desired ist aufgelöst, `current != desired` |

### Resolver-Reihenfolge

```text
validate tankIndex
  → owner destroyed? → OwnerDestroyed
  → nearest alive enemy by position (lower index tie-break)
  → no target? → NoTarget
  → FixedRotationAimResolver.ResolveFromDirection(delta)
  → missing aim solution? → MissingAimSolution
  → FixedRotationTurnStepResolver.ResolveStep(current, desired, Fixed.One)
  → normalized current == desired ? Aligned : Turning
```

### Wichtige Details

| Detail | Semantik |
| ------ | -------- |
| `OwnerDestroyed` | Explizit — **nicht** als `NoTarget` behandelt |
| Zielauswahl | `DistanceSquaredTo`; bei Gleichstand niedrigerer Tank-Index |
| Aligned/Turning | Vergleich **normalisierter** `CurrentRotation` vs. `DesiredRotation` |
| Status-Mapping | Nutzt **nicht** `step.IsAligned` direkt als Enum-Wert |
| `Turning` | `AlignmentDelta` darf positiv oder negativ sein |
| `Turning` | `AlignmentErrorMagnitude` muss `> Fixed.Zero` sein |
| `HasTarget` | `TargetTankId.HasValue` — auch bei `MissingAimSolution` |

## 6. ScriptEvaluationContext and Condition Wiring

Task **5.151** verdrahtet Aim-Status in den Script-Kontext:

```text
ScriptRuntimeContextBuilder.Build
  → ScriptVisibleTurretAimStatusResolver.Resolve (immer, vor destroyed/alive-Zweig)
  → Übergabe an ScriptEvaluationContext
  → ScriptConditionEvaluator liest context.TurretAimStatus
```

| Pfad | `TurretAimStatus` |
| ---- | ----------------- |
| Owner zerstört | Resolver liefert `OwnerDestroyed`; Context mit `weaponReady: false`, `sensorReady: false`, … |
| Owner lebend | Voller Resolver-Output; Sensor/Weapon-Felder separat |

**Hinweis:** `TurretAimStatus` ist ein **Condition-Snapshot pro Tick**, kein persistierter Target-Lock.

[`ScriptEvaluationContext`](../src/ScriptTanks.Core/Scripting/ScriptEvaluationContext.cs) vergleicht `TurretAimStatus` in `Equals`/`GetHashCode` auf **Status-Ebene** (nicht alle numerischen Felder).

## 7. Script Condition Semantics

Neue Condition-Typen (ab Index 6):

| `ScriptConditionType` | True wenn |
| --------------------- | --------- |
| `TurretAligned` | `TurretAimStatus.Status == Aligned` |
| `TurretTurning` | `TurretAimStatus.Status == Turning` |
| `HasAimTarget` | `TurretAimStatus.HasTarget == true` |
| `MissingAimSolution` | `TurretAimStatus.Status == MissingAimSolution` |

### Wahrheitstabelle

| Aim status | `TurretAligned` | `TurretTurning` | `HasAimTarget` | `MissingAimSolution` |
| ---------- | --------------- | --------------- | -------------- | -------------------- |
| `OwnerDestroyed` | false | false | false | false |
| `NoTarget` | false | false | false | false |
| `MissingAimSolution` | false | false | **true** | **true** |
| `Aligned` | **true** | false | **true** | false |
| `Turning` | false | **true** | **true** | false |

### Kritische Abgrenzung

```text
HasAimTarget ist nicht EnemyVisible.
EnemyVisible ist sensor-basiert (ScriptRuntimeContextBuilder).
HasAimTarget folgt der Turret-Pipeline-Zielauswahl (positionsbasiert).
```

Regression **5.152** (`Run_HasAimTarget_WhenEnemyBeyondSensorRange_StillSelectsAimAtEnemy`) beweist die Divergenz absichtlich.

## 8. Aim-Until-Fire Script Policy

Standard-Script-Muster (verifiziert in 5.152/5.153):

```text
Routine 0: TurretAligned  → Fire
Routine 1: HasAimTarget   → AimAtEnemy
Routine 2: Always         → NoOp
```

| Guardrail | Bedeutung |
| --------- | --------- |
| Eine Condition pro Routine | `ScriptRoutine` unterstützt kein AND |
| Kein `WeaponReady` in Track-Proof | Waffenbereitschaft ist Fixture-State, nicht Teil der Aim-Status-Routine-Auswahl in 5.152/5.153 |
| First-match | `ScriptRoutineSelector` wählt erste passende Routine |

### Erwartetes Tick-Verhalten (Fixture: Owner `(10,10)`, Enemy `(20,20)`, Turret `0`)

```text
Tick 1: Turning → HasAimTarget → AimAtEnemy → Turret 0 → 100
Tick 2: Turning → HasAimTarget → AimAtEnemy → Turret 100 → 124
Tick 3: Aligned → Fire → Projektil erscheint
```

### Timing-Klarstellung

Script-Conditions werden **vor** der Command-Anwendung des Ticks ausgewertet. Wenn der Turm am **Ende** von Tick 2 aligned ist (`124`), wird `Fire` erst in **Tick 3** selektiert — nicht in derselben Tick-Evaluation, in der noch `Turning` galt.

## 9. Verified Behavior

### 5.149 — Result models

| Datei | Beweist |
| ----- | ------- |
| [`ScriptVisibleTurretAimStatusResultTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptVisibleTurretAimStatusResultTests.cs) | Enum-Stabilität, `OwnerDestroyed`-Policy, Factory-Invarianten, Aligned/Turning numerische Form |

### 5.150 — Resolver

| Datei | Beweist |
| ----- | ------- |
| [`ScriptVisibleTurretAimStatusResolverTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptVisibleTurretAimStatusResolverTests.cs) | Resolver-Reihenfolge, Zielauswahl, kein Sensor-Kontext, signed delta, Aligned/Turning |

### 5.151 — Condition wiring

| Datei | Beweist |
| ----- | ------- |
| [`ScriptConditionTypeTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptConditionTypeTests.cs) | Enum-Werte angehängt |
| [`ScriptEvaluationContextTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptEvaluationContextTests.cs) | `TurretAimStatus`-Speicherung und Equality |
| [`ScriptRuntimeContextBuilderTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptRuntimeContextBuilderTests.cs) | Builder befüllt Aim-Status; Sensor-Divergenz-Guardrail |
| [`ScriptConditionEvaluatorTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptConditionEvaluatorTests.cs) | Condition-Wahrheitstabelle |

### 5.152 — Composer / tick pipeline

| Datei | Beweist |
| ----- | ------- |
| [`CombinedScriptRuntimeComposerTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/CombinedScriptRuntimeComposerTests.cs) | Routine-Auswahl + drei-Schritt aim→aim→fire |

Repräsentative Tests:

```text
Run_TurretTurning_SelectsAimAtEnemyInsteadOfFire
Run_TurretAligned_SelectsFireInsteadOfAimAtEnemy
Run_HasAimTarget_WhenEnemyBeyondSensorRange_StillSelectsAimAtEnemy
Run_MissingAimSolution_SelectsFallbackRoutine
Step_AimUntilAlignedThenFire_FiresOnlyAfterAlignment
```

### 5.153 — Runner / replay

| Datei | Beweist |
| ----- | ------- |
| [`CombinedRuntimeRunnerTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/CombinedRuntimeRunnerTests.cs) | Kein Fire vor Alignment (inkl. `LastTickResult` → `AimAtEnemy`); Fire in Tick 3 |
| [`CombinedRuntimeReplayRecorderTests.cs`](../tests/ScriptTanks.Core.Tests/Replay/CombinedRuntimeReplayRecorderTests.cs) | Frames: Turret `0→100→124→124`, Projektil auf Frame 3 |

Repräsentative Tests:

```text
RunUntilEnd_AimUntilAlignedThenFire_NoFireBeforeAlignment
RunUntilEnd_AimUntilAlignedThenFire_FiresOnThirdTick
RecordUntilEnd_AimUntilAlignedThenFire_FramesShowAimThenFire
```

## 10. Replay and Logging Policy

| Aspekt | Stand nach 5.153 |
| ------ | ---------------- |
| Replay-Schema | Unverändert — Frames sind **MatchState-only** |
| Aim-Status in Replay | **Nicht** gespeichert |
| Sichtbare Outcomes | `TurretRotation`, `ProjectileState` in Frames |
| Combat-Log | Keine neuen `CombatLogEventTypes` für Aim-Status |
| Diagnostics | **Noch nicht** implementiert |

Optionaler Follow-up: Task **5.154** kann Aim-Status-Diagnostics/Logging planen, ohne diesen Checkpoint zu invalidieren.

## 11. Stable Invariants

```text
ScriptVisibleTurretAimStatusResolver ist pure (keine Mutation, kein I/O).
Resolver-Eingabe: MatchState + tankIndex.
Resolver nutzt ScriptEvaluationContext nicht.
Resolver nutzt EnemyVisible / Sensor-Reichweite nicht.
OwnerDestroyed ist distinct von NoTarget.
NoTarget trägt keine Target-/Alignment-Felder.
MissingAimSolution trägt Target-Felder, aber keine desired/alignment-Felder.
Aligned erfordert delta == 0 und magnitude == 0.
Turning erfordert magnitude > 0; delta darf signed sein.
HasAimTarget bedeutet Target selektiert — auch bei MissingAimSolution.
ScriptRoutine: genau eine Condition pro Routine.
ScriptRoutineSelector: first-match-wins.
Fire bleibt auf Systemebene ungated durch Alignment.
Scripts können wählen, während Turning nicht zu feuern.
Composer/Tick-Reihenfolge unverändert gegenüber Turn-Rate-Checkpoint.
Replay-/Logging-Schema unverändert.
```

## 12. Out of Scope / Remaining Gaps

Nicht Teil von 5.148–5.153:

| Lücke | Bedeutung |
| ----- | --------- |
| Persistenter Target-Lock | Kein gespeichertes Ziel über Ticks hinweg |
| `DesiredTurretRotation` auf `TankState` | Kein persistierter Soll-Winkel im Match-State |
| Multi-Condition AND-Routinen | `ScriptRoutine` hat nur eine Condition |
| `WeaponReady` AND `TurretAligned` | Kein Compound-Condition-Modell |
| Sensor-basierte Aim-Ziel-Policy | Aim-Status folgt Turret-Pipeline, nicht Sensor |
| LOS / Wall obstruction für Aim-Status | Keine Hindernis-Awareness im Resolver |
| Aim prediction / leading shots | Nicht implementiert |
| Fire accuracy/spread vs. alignment error | Nicht implementiert |
| Godot Turret UI / Debug-Visualisierung | Nicht implementiert |
| Rich aim-status Combat-Logs | Geplant optional als 5.154 |
| Replay-Overlay / Debug-Frame-Typen | Schema unverändert |
| Network reconciliation | Außerhalb Scope |

## 13. Recommended Roadmap After 5.155

| Task | Scope |
| ---- | ----- |
| **5.154** | Optional: Aim-Status-Logging / Diagnostics-Plan |
| **5.156** | Multi-Condition Script-Routine-Plan (`AND` / Condition Groups) |
| **5.157** | Persistenter Target-Lock / Desired-Rotation-Plan |
| **5.158** | LOS-aware Aim-Status-Plan |
| **5.159** | Godot-/Debug-Visualisierung |
| **5.160** | Checkpoint-Update falls Diagnostics (5.154) umgesetzt werden |

**Empfehlung:**

```text
Wenn Diagnostics nicht dringend sind, sollte der nächste Core-Design-Schritt
Multi-Condition-Routine-Support oder persistente Target-Policy sein.
Godot-Visualisierung erst, wenn Core-State/Diagnostic-Shape feststeht.
```

## 14. Verification / Definition of Done

Vom Repository-Root ausführen:

```powershell
dotnet build ScriptTanks.sln -warnaserror
dotnet test ScriptTanks.sln
```

**Erwartung:**

- 0 Warnungen
- **3231** Tests bestanden
- Keine Änderungen außer dieser neuen Checkpoint-Datei

### Definition of Done

- [x] `docs/SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_INTEGRATION_CHECKPOINT.md` existiert mit Abschnitten **1–15**
- [x] Tasks 5.148–5.153 zusammengefasst (§2)
- [x] Architektur dokumentiert (§3)
- [x] API-Inventar dokumentiert (§4)
- [x] Resolver-/Status-Semantik dokumentiert (§5)
- [x] Condition-Wiring dokumentiert (§6–§7)
- [x] Aim-until-fire-Policy dokumentiert (§8)
- [x] Verifiziertes Verhalten auf Tests gemappt (§9)
- [x] Replay-/Logging-Policy dokumentiert (§10)
- [x] Stable Invariants gelistet (§11)
- [x] Verbleibende Lücken dokumentiert (§12)
- [x] Roadmap dokumentiert (§13)
- [x] Keine `src/`-, `tests/`- oder bestehenden Docs-Änderungen in Task 5.155
- [x] Keine lokalen Host-Pfade im Deliverable

## 15. Links Appendix

### Sibling documentation (`docs/`)

| Dokument | Beschreibung |
| -------- | ------------ |
| [SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md](SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md) | Plan 5.148 |
| [TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md](TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md) | Gradual rotation nach 5.147 |
| [TURRET_TURN_RATE_GRADUAL_ROTATION_PLAN.md](TURRET_TURN_RATE_GRADUAL_ROTATION_PLAN.md) | Turn-Rate-Architekturplan |
| [FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md](FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md) | Full-Angle Aim nach 5.138 |
| [COMBINED_RUNTIME_LOGGING_CHECKPOINT.md](COMBINED_RUNTIME_LOGGING_CHECKPOINT.md) | Logging-Policy |

### Production code (`../src/`)

| Bereich | Pfad |
| ------- | ---- |
| Aim status enum | [`Scripting/ScriptVisibleTurretAimStatus.cs`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatus.cs) |
| Aim status result | [`Scripting/ScriptVisibleTurretAimStatusResult.cs`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatusResult.cs) |
| Aim status resolver | [`Scripting/ScriptVisibleTurretAimStatusResolver.cs`](../src/ScriptTanks.Core/Scripting/ScriptVisibleTurretAimStatusResolver.cs) |
| Conditions | [`Scripting/ScriptConditionType.cs`](../src/ScriptTanks.Core/Scripting/ScriptConditionType.cs), [`Scripting/ScriptConditionEvaluator.cs`](../src/ScriptTanks.Core/Scripting/ScriptConditionEvaluator.cs) |
| Context | [`Scripting/ScriptEvaluationContext.cs`](../src/ScriptTanks.Core/Scripting/ScriptEvaluationContext.cs), [`Scripting/ScriptRuntimeContextBuilder.cs`](../src/ScriptTanks.Core/Scripting/ScriptRuntimeContextBuilder.cs) |
| Selection / composer | [`Scripting/ScriptRoutineSelector.cs`](../src/ScriptTanks.Core/Scripting/ScriptRoutineSelector.cs), [`Scripting/CombinedScriptRuntimeComposer.cs`](../src/ScriptTanks.Core/Scripting/CombinedScriptRuntimeComposer.cs) |

### Tests (`../tests/`)

| Bereich | Pfad |
| ------- | ---- |
| Result models (5.149) | [`Scripting/ScriptVisibleTurretAimStatusResultTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptVisibleTurretAimStatusResultTests.cs) |
| Resolver (5.150) | [`Scripting/ScriptVisibleTurretAimStatusResolverTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptVisibleTurretAimStatusResolverTests.cs) |
| Conditions (5.151) | [`Scripting/ScriptConditionEvaluatorTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptConditionEvaluatorTests.cs), [`Scripting/ScriptRuntimeContextBuilderTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/ScriptRuntimeContextBuilderTests.cs) |
| Composer (5.152) | [`Scripting/CombinedScriptRuntimeComposerTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/CombinedScriptRuntimeComposerTests.cs) |
| Runner (5.153) | [`Scripting/CombinedRuntimeRunnerTests.cs`](../tests/ScriptTanks.Core.Tests/Scripting/CombinedRuntimeRunnerTests.cs) |
| Replay (5.153) | [`Replay/CombinedRuntimeReplayRecorderTests.cs`](../tests/ScriptTanks.Core.Tests/Replay/CombinedRuntimeReplayRecorderTests.cs) |
