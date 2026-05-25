# ScriptTanks — Target Look v0.1

> Kurzfassung für PixelArt, UI und Godot-Theming. Ergänzt das Document Package; keine Änderung am Gameplay-Design.

## Kern

**Sci‑Fi Kampflabor + Retro‑Terminal.** Kühl, lesbar, technisch. Der Hook ist sichtbar: **Kampf + Log + Code** (nicht nur “nur Tanks von oben”).

## Farbwelt (Richtwerte)

| Rolle            | Richtung |
|------------------|----------|
| Hintergrund / UI | Tiefes Neutralgrau bis fast Schwarz; wenig reine #000, damit Schatten Platz haben. |
| Primär-Akzent    | **Terminal-Grün** (z. B. #3ddc84–#00ff9d Band) für Status, wichtige Labels, “OK”-Zustand. |
| Sekundär         | Kaltblau / Cyan für Fokus, Auswahl, Links. |
| Warnung / Fehler | Amber/Orange für Warnings, Rot sparsam für harte Fehler (nicht alles eintönig rot). |
| Panzer / Arena   | Sättigung moderat: Metall, beton, abgenutzt; **eine** Akzentfarbe pro Fraktion/Skin später, im MVP reicht ein Satz. |

**Kontrast:** Text und UI-Elemente erfüllen praktisch **WCAG-ähnlich** genug Abstand (hell auf dunkel), damit Log und Code lange lesbar bleiben.

## Pixel-Größen (MVP / Operator Demo)

| Element        | Ziel        | Anmerkung |
|----------------|------------|-----------|
| Panzer (Höhe)  | **48–64 px** | 48 = schneller, 64 = etwas mehr Lesbarkeit für Turm/Barrel. Ein Satz wählen und **durchziehen**. |
| Projektile     | 4–12 px    | Klar erkennbar, nicht “Nadeln”. |
| UI-Schrift     | System/Monospace: **14–16 px** äquivalent; Log ggf. etwas kleiner, aber nicht unleserlich. |
| Tile-Größe     | 32×32 **oder** 64×64 | Mit Panzer-Größe abstimmen; **keine** gemischten Tile-Größen im selben Set. |

**Regel:** Eine **kanonische “Cell”-Größe** fürs Grid (z. B. 32) und daraus Panzer- und Prop-Größen ableiten, damit alles “auf dem Raster” sitzt.

## Arena & Kamera

- **Top-Down** (eher “low top-down”): leicht erkennbare **Silhouette** des Panzers; Turm vom Rumpf trennbar.
- arena: **klare Kanten** (Wände, Blöcke), wenig visuelles Rauschen im Boden (Debugging und Projektile sollen im Vordergrund bleiben).

## Panzer & Lesbarkeit

- **Umriss:** dezenter Outline (1 px) oder kantenscharfe Trennung; kein “Pudding”-Look.
- **Turret vs. Hull:** leicht unterschiedliche Helligkeit oder Farbton, damit Drehung sofort klar ist.
- **Gegner:** Form oder Farbakzent vom Spieler unterscheidbar; Boss (PROTOTYPE_BRUTE) **größer/ breiter**, nicht nur “mehr HP-Balken”.

## UI / Terminal

- **Monospace** für Skript, Log, Tabellen; **eine** UI-Sans für Buttons/Titel optional, sonst durchgehend Mono für konsistenten “Lab”-Look.
- Panels: dunkel, dezente **1px** Trennlinien, kein Glassmorphism-Overload.
- Status: Farbe + kurzer Text (z. B. `weapon.ready`, `turret.aligned`) — nie nur Farbe.

## Animation & Effekte

- Kurz und **lesbar**: Muzzle-Flash, Treffer-Flash, kleine Rauch/Sparks — **kein** Vollbild-Partikel, der Log und Code verdeckt.
- Transitions: schnell (100–200 ms), sachlich.

## Was wir vermeiden (v0.1)

- Generische “Mobile-Game”-Palette (knallig, alles glow).
- Voll-3D-Look in der 2D-Arena (Schatten/ perspektivische Verwirrung).
- Unleserliche “Matrix”-Grünflut (Akzente setzen, nicht alles grün).

## PixelLab / Asset-Briefs (Kurz)

- **View:** `low top-down` oder `high top-down` (an Godot-Kamera anpassen, einmal festlegen).
- **Panzer / Props:** `create_object`, **8 Richtungen** wenn Turm/Body getrennt gedreht werden; sonst konsequent **1** Richtung + getrennte Turm-Sprites in Godot.
- **Stil-Prompts:** “sci-fi combat lab”, “worn metal”, “compact tank”, “readable silhouette”, “limited palette”.

## Abnahme-Check (für “sieht aus wie ScriptTanks”)

- [ ] Man erkennt **sofort** Panzer, Turm und Projektil.
- [ ] **Log + Code** bliben neben dem Gefecht nutzbar.
- [ ] Stimmung: **Labor**, nicht fantasy / cartoon-bunt.
- [ ] Eine feste **px-Größe** und **Tile-Größe** sind dokumentiert und im Projekt eingehalten.
