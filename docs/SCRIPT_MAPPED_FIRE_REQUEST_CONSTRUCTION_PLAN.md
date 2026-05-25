# Task 5.54 — Script-Mapped Fire Request Construction Plan

> **Nur Dokumentation.** Keine Implementierung, keine Tests, keine Änderungen an `src/`, `tests/`, Projekten, Solution oder `game/`.
>
> Sprache: Erklärungen überwiegend auf Deutsch; Typnamen und API-Skizzen wie im Code auf Englisch.

## 1. Purpose

Dieses Dokument definiert die **geplante nächste Grenze** nach Domain-Mapping: wie aus einem als **Weapon/Fire** klassifizierten Script-Mapping-Eintrag später ein konkreter `[MatchFireRequest](../src/ScriptTanks.Core/Match/MatchFireRequest.cs)` entsteht — **ohne** ihn auszuführen.

Die Schicht soll später die Frage beantworten:

**Gegeben Tank-Zustand, Sensor-/Match-Kontext und ein übersetztes Fire-Kommando — lässt sich ein deterministischer, gültiger `MatchFireRequest` konstruieren?**

- **Keine** Ausführung von Feuern, keine Projektile erzeugen, keinen `MatchState` mutieren.
- **Keine** „erfundenen“ Koordinaten oder IDs nur damit etwas schießt.

## 2. Current Upstream Flow

Aktuelle Kette (reine Komponenten):

```text
ScriptProgram[]
→ MatchScriptIntentIntegrationComposer.EvaluateIntents(MatchSensorRuntimeState, programs)
→ MatchScriptIntentIntegrationResult
→ ScriptTranslatedCommandDomainMapper.MapAll(...)
→ MatchScriptDomainRequestMappingResult
→ ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mappingResult)   // nur Sensor
```

End-to-End-Orchestrierung Sensor-only:

```text
MatchScriptSensorRuntimeComposer.Run(runtime, programs)
```

**Fire-Zweig heute:** `[ScriptTranslatedCommandDomainMapper](../src/ScriptTanks.Core/Scripting/ScriptTranslatedCommandDomainMapper.cs)` mappt `ScriptTranslatedCommandRequestKind.Fire` auf `[ScriptDomainRequestCategory.Weapon](../src/ScriptTanks.Core/Scripting/ScriptDomainRequestCategory.cs)` über `[ScriptDomainRequestMappingRecord.Weapon(..., fireRequest: null)](../src/ScriptTanks.Core/Scripting/ScriptDomainRequestMappingRecord.cs)`. Das Feld `**FireRequest`** bleibt `**null**` — korrekt, weil den fünf Pflichtfeldern von `MatchFireRequest` noch keine deterministische Herleitung zugeordnet ist.

## 3. Existing Fire-Related Type Inventory

Aus dem Ist-Stand des Kerns (keine erfundenen Typen):


| Bereich                   | Typ / API                                                                                                                                                             | Rolle (Kurz)                                                                                                                                                                                                                   |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Fire-Request              | `[MatchFireRequest](../src/ScriptTanks.Core/Match/MatchFireRequest.cs)`                                                                                               | `TankId`, `WeaponSlot`, `ProjectileId`, `MuzzlePosition`, `FireVelocity` — reiner Daten-Container                                                                                                                              |
| Fire-Ausführung auf Match | `[MatchStateFireSystem.ResolveFire](../src/ScriptTanks.Core/Match/MatchStateFireSystem.cs)`                                                                           | Delegiert u.a. an `[LoadoutFireResolver.Resolve](../src/ScriptTanks.Core/Combat/LoadoutFireResolver.cs)`; mutiert Match bei Erfolg (Projektil anhängen, Loadout ersetzen) — **nicht** Teil der geplanten Konstruktions-Schicht |
| Ergebnis Feuern           | `[MatchStateFireOutcome](../src/ScriptTanks.Core/Match/MatchStateFireOutcome.cs)`, `[LoadoutFireResolutionOutcome](../src/ScriptTanks.Core/Combat/)` (siehe Resolver) | Ausführungs-/Auflösungsergebnisse                                                                                                                                                                                              |
| Waffe Slot                | `[WeaponSlot](../src/ScriptTanks.Core/Weapons/WeaponSlot.cs)`                                                                                                         | Nicht-negativer Slot-Index                                                                                                                                                                                                     |
| Waffe Laufzeit            | `[WeaponState](../src/ScriptTanks.Core/Weapons/WeaponState.cs)`, `[TankWeaponLoadout](../src/ScriptTanks.Core/Weapons/TankWeaponLoadout.cs)`                          | `WeaponState.IsReady(SimTick)` für Cooldown-Logik                                                                                                                                                                              |
| Waffen-Definition         | `[WeaponDefinition](../src/ScriptTanks.Core/Weapons/)` (Kataloge z.B. `WeaponCatalog`)                                                                                | Archetyp-Daten                                                                                                                                                                                                                 |
| Projektil-ID              | `[ProjectileId](../src/ScriptTanks.Core/Ids/ProjectileId.cs)`                                                                                                         | Stark typisierte ID                                                                                                                                                                                                            |
| Projektil-Archetyp        | `[ProjectileDefinition](../src/ScriptTanks.Core/Projectiles/ProjectileDefinition.cs)`                                                                                 | u.a. `SpeedPerTick`, `MaxRange`, Schaden                                                                                                                                                                                       |
| Projektil Spawn (Daten)   | `[ProjectileSpawnFactory.Create](../src/ScriptTanks.Core/Projectiles/ProjectileSpawnFactory.cs)`                                                                      | Baut `[ProjectileState](../src/ScriptTanks.Core/Projectiles/ProjectileState.cs)` aus Definition + Position/Geschwindigkeit — **nicht** automatisch Teil der Request-Konstruktion                                               |
| Tank / Match              | `[TankState](../src/ScriptTanks.Core/Tanks/TankState.cs)` (`BodyRotation`, `TurretRotation`, `Movement`), `[MatchState](../src/ScriptTanks.Core/Match/MatchState.cs)` | Geometrie/Ausrichtung für spätere Muzzle-/Velocity-Ableitung                                                                                                                                                                   |
| Tick                      | `[SimTick](../src/ScriptTanks.Core/Simulation/SimTick.cs)`                                                                                                            | Zeitbasis für Readiness                                                                                                                                                                                                        |


**Hinweis:** Ein dedizierter Typ „FireResolver“ als eigene Klasse ist nicht als eigenständiger Name aufgeführt; die relevante Auflösung liegt in `**LoadoutFireResolver`** und `**MatchStateFireSystem**`.

## 4. Why the Mapper Cannot Construct Fire Yet

`[ScriptDomainRequestMappingRecord](../src/ScriptTanks.Core/Scripting/ScriptDomainRequestMappingRecord.cs)` trägt pro Tank u.a. `TankIndex`, `TankId`, `TranslationOutput` und optional Sensor/Fire-Felder. Für Fire ist `**FireRequest` absichtlich `null**`.

Um einen gültigen `[MatchFireRequest](../src/ScriptTanks.Core/Match/MatchFireRequest.cs)` zu füllen, fehlen selbst bei bekannter `TankId` mindestens:


| Feld                                    | Problem                                                                                                                                                               |
| --------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **WeaponSlot**                          | Script sagt „Fire“, aber kein Slot — Policy fehlt (welche Kanone?).                                                                                                   |
| **ProjectileId**                        | Keine deterministische Vergabe-Regel im Mapper; keine globale Zufalls-ID.                                                                                             |
| **MuzzlePosition**                      | Benötigt Tankposition + Turm-/Geometrie-Modell in Fixed-Point, nicht Godot.                                                                                           |
| **FireVelocity**                        | Richtung × Geschwindigkeit pro Tick; Bezug zu `[ProjectileDefinition.SpeedPerTick](../src/ScriptTanks.Core/Projectiles/ProjectileDefinition.cs)` und Turmausrichtung. |
| **ProjectileDefinition / ProjectileId** | Welches Projektil zur Waffe gehört — aus `[WeaponDefinition](../src/ScriptTanks.Core/Weapons/)` / Spawn-Pipeline zu klären.                                           |
| **SimTick / Readiness**                 | `[WeaponState.IsReady(SimTick)](../src/ScriptTanks.Core/Weapons/WeaponState.cs)` — Konstruktion vs. Ausführung klären.                                                |
| **Turmausrichtung**                     | `[TankState.TurretRotation](../src/ScriptTanks.Core/Tanks/TankState.cs)`; Alignment-Politik „aligned with intent“ fehlt als definierter Check.                        |


Ohne diese Inputs würde ein Mapper **Werte erfinden** — das ist für den deterministischen Kern inakzeptabel.

## 5. Boundary Rule

Drei getrennte Schichten:


| Schicht                                 | Verantwortung                                                                                                                                                 |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Domain mapping**                      | Klassifiziert Fire als **Weapon** (`ScriptDomainRequestCategory.Weapon`).                                                                                     |
| **Fire request construction** (geplant) | Erzeugt optional einen **konkreten** `MatchFireRequest` aus Runtime + Mapping + Policies + Allocators.                                                        |
| **Fire execution**                      | `[MatchStateFireSystem.ResolveFire](../src/ScriptTanks.Core/Match/MatchStateFireSystem.cs)` / Tick-Pipelines wenden den Request an und mutieren `MatchState`. |


Diese Grenzen dürfen nicht verschmolzen werden.

## 6. Proposed Future Component

Nur **Skizze**:

```csharp
public static class ScriptMappedFireRequestFactory
{
    public static ScriptMappedFireRequestConstructionResult ConstructAll(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mappingResult,
        IProjectileIdAllocator projectileIds,
        SimTick currentTick);
}
```

Alternative **pro Record**:

```csharp
public static ScriptMappedFireRequestConstructionRecord TryConstructForRecord(
    MatchSensorRuntimeState runtime,
    ScriptDomainRequestMappingRecord record,
    int recordIndex,
    IProjectileIdAllocator projectileIds,
    SimTick currentTick);
```

Die konkrete Signatur hängt davon ab, ob Batch oder sequentieller Bau mit gemeinsamem Allocator bevorzugt wird.

## 7. Proposed Output Model

Beispiel für ein **reines** Ergebnis pro Mapping-Zeile:

```text
ScriptMappedFireRequestConstructionRecord
├─ RecordIndex
├─ MappingRecord (Referenz oder eingebettete Kopie — Implementierungsdetail)
├─ DidConstruct
├─ MatchFireRequest?
└─ Status / Reason (Konzept-Enum oder String)
```

**Konzeptuelle Statuswerte** (nicht als Code verpflichtend):

- `NotWeaponCategory`
- `NoFireCommandInMapping`
- `MissingWeaponSlotPolicy`
- `WeaponNotReady`
- `TurretNotAligned` / `TurretStateUnavailable`
- `MissingProjectileId`
- `MissingMuzzleResolver`
- `MissingVelocityResolver`
- `Constructed`

Bei `**DidConstruct == false`** bleibt `**MatchFireRequest?**` `**null**`.

## 8. WeaponSlot Policy (MVP)

**Empfohlene MVP-Policy:** Für Script-`Fire` ohne Argument `**WeaponSlot`** mit `**WeaponSlot.Zero**` (erster Slot), sofern das Loadout diesen Slot hat — analog zu Sensor mit `[SensorSlot.Zero](../src/ScriptTanks.Core/Sensors/SensorSlot.cs)`.

**Später:** Payload/Syntax z.B. `fire(primary)`, `fire(secondary)`, `fire(slot=n)` → explizite Slot-Wahl; Übersetzungsschicht müsste Payload parsen (separater Task).

## 9. ProjectileId Policy

Determinismus ist Pflicht — **keine** Zufalls-IDs, GUIDs, Wall-Clock, nicht-deterministische statische Zähler.

**Option A (empfohlen):** Caller stellt einen `**IProjectileIdAllocator`** (oder festes Interface) bereit, der aus Tick + Tank + sequentiellem Index **deterministisch** die nächste ID erzeugt.

**Option B:** Ableitung aus `SimTick` + `TankIndex` + laufender **Batch-Index** innerhalb eines Ticks — dokumentiert und testbar.

Die Factory nutzt **keine** globalen versteckten Zähler ohne definierte Domäne.

## 10. Muzzle Position Policy

Spätere Ableitung typischerweise:

- Tank-Position aus `[TankState.Movement](../src/ScriptTanks.Core/Tanks/TankState.cs)` / `[MovementState](../src/ScriptTanks.Core/Movement/MovementState.cs)`
- Plus Turmrichtung `[TankState.TurretRotation](../src/ScriptTanks.Core/Tanks/TankState.cs)`
- Plus geometrischer Offset der Mündung relativ zum Tank (noch zu definieren in Fixed-Point)

**Regeln:**

- Nur **Fixed-Point** (`[FixedVec2](../src/ScriptTanks.Core/Math/FixedVec2.cs)`) als Simulations-Wahrheit.
- **Keine** Godot-Transforms als Quelle der Wahrheit.
- Keine float-Werte für Kern-Simulation.

## 11. Fire Velocity Policy

Ansatz:

- Richtungsvektor aus Turmausrichtung (und ggf. definierter „forward“-Konvention für den Panzer).
- Betrag aus `[ProjectileDefinition.SpeedPerTick](../src/ScriptTanks.Core/Projectiles/ProjectileDefinition.cs)` der zum gewählten Projektil gehört — gekoppelt an die gewählte Waffe / Munition.

Konkrete Formeln und Einheiten sind **Implementierungs-Tasks**, nicht Teil dieses Dokuments.

## 12. Readiness and Alignment Policy

- **Readiness:** `[WeaponState.IsReady(currentTick)](../src/ScriptTanks.Core/Weapons/WeaponState.cs)` — klären, ob die Konstruktions-Schicht bereits „nicht schussbereit“ als `**DidConstruct false`** abbildet oder die Ausführungsschicht ablehnt. **Empfehlung MVP:** Kein `MatchFireRequest`, wenn nicht ready → Status `WeaponNotReady`.
- **Turm-Ausrichtung:** Keine synthetische „perfekte“ Ausrichtung. Wenn es keinen definierten „aligned“-Zustand gibt oder Script sagt „Fire“ ohne Aim-Daten, dann `**DidConstruct false`** mit Grund `**TurretNotAligned**` oder `**TurretStateUnavailable**`.

**Empfehlung MVP:** Nur konstruieren, wenn **alle** deterministischen Eingaben vorliegen; sonst sauberes „nicht konstruiert“.

## 13. Deterministic Ordering

Batch-Verarbeitung:

- Reihenfolge **identisch** zu `MatchScriptDomainRequestMappingResult.Records` (`[MatchScriptDomainRequestMappingResult](../src/ScriptTanks.Core/Scripting/MatchScriptDomainRequestMappingResult.cs)`) (Index `0 .. Count-1`).
- **ProjectileId-Vergabe** in **derselben** Reihenfolge (bei mehreren Fire-Kandidaten im gleichen Tick).
- **Kein** Sortieren nach `TankId` oder `PlayerSlot`.
- **Keine** Dictionary-Iteration ohne feste Ordnung.

## 14. Purity Boundary

Die geplante **Konstruktions**-Schicht darf **nicht**:

- Projektile spawnen oder `[ProjectileSpawnFactory](../src/ScriptTanks.Core/Projectiles/ProjectileSpawnFactory.cs)` zur Mutation aufrufen,
- Waffen-Cooldown oder Munition ändern,
- Treffer oder Schaden ausführen,
- `[MatchState](../src/ScriptTanks.Core/Match/MatchState.cs)` mutieren,
- CombatLog oder Replay schreiben,
- Scheduler/CPU-Budget anfassen,
- Godot/Client-Code aufrufen.

Sie **darf** reine Daten lesen und immutable Ausgabeobjekte erzeugen.

## 15. Failure Semantics

- **Normale** „kann nicht konstruieren“-Fälle → `**DidConstruct false`** + Statusgrund, **keine** Exceptions für Business-Regeln.
- **Exceptions** nur bei Programmierfehlern oder Verletzung harter Verträge, z.B.:
  - `runtime == null`, `mappingResult == null`
  - Mapping stammt von anderem Runtime-Snapshot (analog `[ScriptMappedSensorRequestApplicationPipeline](../src/ScriptTanks.Core/Scripting/ScriptMappedSensorRequestApplicationPipeline.cs)`)
  - Pflicht-Allocator `null`, wenn die gewählte Policy einen verlangt

## 16. Relationship to Sensor Composer

`[MatchScriptSensorRuntimeComposer](../src/ScriptTanks.Core/Scripting/MatchScriptSensorRuntimeComposer.cs)` bleibt eine **reine Sensor-Orchestrierung** und soll **nicht** zum Fire-Executor werden.

Mögliche spätere Gesamtarchitektur (konzeptionell):

```text
Script integration
→ Domain mapping
→ Sensor request application        // bestehend
→ Fire request construction         // neu
→ Fire request application          // später, eigenes Apply analog Sensor
```

Jede Stufe bleibt **einzeln testbar**.

## 17. Recommended Follow-up Tasks


| Task     | Inhalt                                                                                                           |
| -------- | ---------------------------------------------------------------------------------------------------------------- |
| **5.55** | Modelle: `ScriptMappedFireRequestConstructionRecord`, Batch-`Result`, Status-Enum                                |
| **5.56** | Deterministischer `ProjectileId`-Allocator / Sequenz (Plan oder Implementierung)                                 |
| **5.57** | Erste Implementierung `ScriptMappedFireRequestFactory` (nur Konstruktion)                                        |
| **5.58** | Reine Pipeline „Apply script-mapped fire requests“ auf `MatchState` (optional getrennt vom Sensor-Pfad)          |
| **5.59** | Kombinierter Composer: Sensor-Anwendung + Fire-Konstruktion (+ später Fire-Apply), ohne Verletzung der Schichten |


Nummern anpassen, falls sich das Projekt bereits anderweitig vergeben hat.

## 18. Out of Scope

Für Task 5.54 ausdrücklich **nicht** enthalten:

- Implementierung oder Tests im Repo
- Änderungen an Quellcode, Projektdateien, Solution, Godot-Spiel
- Fire-Ausführung, Projektil-Erzeugung im Match
- Treffererkennung, Schaden
- Cooldown-/Ammo-Mutation
- Turm-/Bewegungs-Ausführung nur zwecks Schießen
- `MatchRunner`-Integration
- CombatLog / Replay
- Godot-Integration

---

## Definition of Done (Task 5.54)

- `[docs/SCRIPT_MAPPED_FIRE_REQUEST_CONSTRUCTION_PLAN.md](SCRIPT_MAPPED_FIRE_REQUEST_CONSTRUCTION_PLAN.md)` existiert mit allen Abschnitten 1–18.
- `dotnet build ScriptTanks.sln -warnaserror` und `dotnet test ScriptTanks.sln` bleiben grün (nur Markdown hinzugefügt).

