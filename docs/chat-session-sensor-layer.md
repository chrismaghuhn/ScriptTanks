# Chat-Session: Sensor-Layer & Match-Sensor-Laufzeit (Überblick)

Diese Datei fasst zusammen, was in einer Cursor-Chat-Session rund um den Combat-Kernel-Sensor-Stack umgesetzt wurde (Tasks 4.4–4.7 und die genannten Vorarbeiten aus dem Kontext).

## Kontext & Prinzipien

- **Deterministische Kern-Simulation** (`ScriptTanks.Core`): Fixed-Point, keine Float-Logik im Scan-Kern.
- **Immutability**: Snapshot-Typen, `With`*-Updates, keine Mutation von `MatchState` durch die Sensor-Composer.
- **Schichtung**: Datenmodelle → Resolver → Loadout-Adapter → Match-Runtime-Paar → ein Scan pro Aufruf.

## Bereits abgeschlossen (aus Konversationskontext / früheren Tasks)

- **Task 3.45** — `MatchDebugSnapshotTextFormatter` (Diagnostics-Textausgabe).
- **Task 4.0–4.3** — Sensor-Definitionen, Katalog, Scan-Ergebnis-Typen, `SensorScanResolver` (Radius), `SensorSlot`, `TankSensorLoadout`.
- **Task 4.4** — `LoadoutSensorScanResolver` / `LoadoutSensorScanOutcome`: ein Sensor aus einem `TankSensorLoadout` scannen, delegiert an `SensorScanResolver`.

## Task 4.5 — `MatchSensorLoadoutState`

**Ziel:** Pro Tank eine Liste von `TankSensorLoadout` nach Index (ohne `MatchState`-Integration).

**Neu:**

- `src/ScriptTanks.Core/Sensors/MatchSensorLoadoutState.cs`
- `tests/ScriptTanks.Core.Tests/Sensors/MatchSensorLoadoutStateTests.cs`

**Kern:** `IReadOnlyList<TankSensorLoadout>`, defensive Kopie, leere/null-Elemente abweisen, `GetLoadoutAtIndex` / `WithLoadoutAtIndex` mit Validierung (`tankIndex`). XML-Doc: Paarung mit `MatchState.Tanks` nur konzeptionell, keine Validierung gegen `MatchState` yet.

## Task 4.6 — `MatchSensorRuntimeState`

**Ziel:** `MatchState` und `MatchSensorLoadoutState` zusammenhalten und **Count-Kompatibilität** prüfen (`state.Tanks.Count == sensorLoadouts.Count`), ohne `MatchState` zu ändern.

**Neu:**

- `src/ScriptTanks.Core/Sensors/MatchSensorRuntimeState.cs`
- `tests/ScriptTanks.Core.Tests/Sensors/MatchSensorRuntimeStateTests.cs`

**Kern:** `WithState`, `WithSensorLoadouts` (jeweils ParamName bei Count-Mismatch), `WithTankSensorLoadoutAtIndex` delegiert Null/Index an `MatchSensorLoadoutState`.

## Task 4.7 — `MatchSensorScanSystem` / `MatchSensorScanOutcome`

**Ziel:** Erster **Match-Level**-Composer: genau **ein** Tank-Index, genau **ein** Sensor-Slot pro Aufruf.

**Neu:**

- `src/ScriptTanks.Core/Sensors/MatchSensorScanOutcome.cs` — `SensorScanResult` + aktualisierte `MatchSensorRuntimeState`.
- `src/ScriptTanks.Core/Sensors/MatchSensorScanSystem.cs` — `ScanSensorAtIndex(runtime, tankIndex, sensorSlot)`:
  - validiert `runtime` und `tankIndex`;
  - nimmt Scanner aus `runtime.State.Tanks[tankIndex]`, Loadout aus `runtime.SensorLoadouts.GetLoadoutAtIndex(tankIndex)`;
  - delegiert an `LoadoutSensorScanResolver.Scan(..., scanner.Id, ...)`;
  - schreibt zurück mit `WithTankSensorLoadoutAtIndex`.
- `tests/ScriptTanks.Core.Tests/Sensors/MatchSensorScanSystemTests.cs` (18 Tests inkl. Outcome-Tests, Cooldown, Keine-Mutation des Original-Runtime).

## Verifikation (Stand nach Umsetzung)

- `dotnet build ScriptTanks.sln -warnaserror` — ohne Warnungen/Fehler.
- `dotnet test ScriptTanks.sln` — alle Tests grün (Baseline + neue Tests pro Task; Task 4.7 Endstand: **1312** Tests).

## Bewusst nicht umgesetzt (laut Task-Grenzen)

- Keine Scan-Loops, kein Scheduling, keine AI/Script-, Runner-, Replay-, Logging-, Diagnostics- oder Godot-Anbindung.
- Keine Änderungen an `MatchState`-Definition, keine Sensor-Eigenschaften auf `MatchState`.

---

*Hinweis: Diese Datei ist eine manuelle Zusammenfassung für das Repository; sie ersetzt keine offizielle Taskplan-Datei.*