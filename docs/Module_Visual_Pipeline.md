# Module & Panzer — visuelle Pipeline

> Bezug: GDD (Slots MVP: Chassis, Weapon, Sensor, CPU; später mehr Module / Engineering Grid). Ziel: **lesbare Panzer im Top-Down**, wartbare **Kombinatorik**, klare **PixelLab-Produktion**.

## Grundregel: zusammensetzen, nicht multiplizieren

**Nicht:** Für jede Kombination aus Chassis × Waffe × Sensor × CPU ein fertiges 8-Richtungs-Sprite — das explodiert schnell und ist bei Balance-Änderungen teuer.

**Stattdessen:** Der sichtbare Panzer ist ein **Stack aus wenigen Layern**, jeder Layer kommt aus **einem Modul-Asset-Set**. Die **Simulation** (Core) bleibt von Pixeln getrennt; Godot (oder ein späteres `VisualProfile`) mappt **Modul-IDs → Sprites + Offsets**.

```text
Hull (Chassis, 8 dirs)     → Boden-Silhouette, Laufwerke
Turret-Base (optional)     → Aufsatz auf Hull; Drehpunkt = Turmlager
Weapon / Barrel (8 dirs oder 1× rotiert) → Rohr, Mündung
Decals (Sensor, CPU, …)   → kleine Aufkleber / Antennen / Glow (often 1× oder 8 dirs)
```

Später (Engineering Grid): zusätzliche **Overlay-Slots** mit festen Ankerpunkten auf dem Hull — gleiches Konzept, mehr `Sprite2D`-Kinder.

## Was sich wie verhält

| Modultyp | Typische Darstellung |
|----------|----------------------|
| **Chassis** | Voller **Rumpf** inkl. Ketten/Laufwerk; bestimmt die **Bounding-Silhouette** und Hitbox-Nähe visuell. |
| **Weapon** | **Rohr + Aufbau** am Turm; oft nur **ein** Sprite pro Waffe, Rotation = `turret_angle` (oder 8-Dir wie jetzt). |
| **Sensor** | Oft **kleines Detail** (Radarantenne, Blister) — kann **Decal** auf Hull/Turm sein. |
| **CPU** | Im Kampf oft **kaum sichtbar** → stark **UI** (Slot-Icon); optional kleiner Gehäuse-Chip auf Hull. |

So bleiben Sensor/CPU **billig in der Produktion**: große **512 oder 64 px Icons** fürs Bau-Menü, kleine **16–32 px Decals** optional im Welt-Sprite.

## Technisch (Godot-Seite, Zielbild)

- **Szene:** z. B. `TankVisual` als `Node2D` mit Kindern `Hull`, `Turret`, `Weapon`, `Decals…`.
- **Pivot Hull:** Mitte der Zelle / Fahrzeugmittelpunkt (wie Core-Hit-Circle).
- **Pivot Turm:** Drehpunkt auf dem Rumpf (einmal pro Chassis-Familie kalibrieren).
- **Pivot Waffe:** Mündung für Effekte optional als **Marker-Node** (`Muzzle`).
- **Daten:** Pro Modul-ID eine kleine **Resource** oder JSON-Eintrag: `texture`-Pfad(e), `offset`, `z_index`, `rotation_inherited` (ja/nein).

Der **Core** kennt weiter nur **TankDefinition** / später Loadout-IDs — die **Zuordnung Modul → Sprite** lebt im Client.

## Pixel-Größen (an `Target_Look_v0.1.md` ausrichten)

- **Hull / Gesamt-Silhouette:** eine kanonische Größe (bei euch **64×64** Raster).
- **Waffen-Rohr:** gleicher Raster oder halbe Höhe, aber **Anbindung** am Turm-Kreis ausrichten.
- **Icons** (Bau-Menü, Forschung): **64×64** oder **128×128**, gleiche Outline/Shading wie Welt-Sprites.

## PixelLab — wie ihr Module „im Stil“ haltet

1. **Referenz festlegen:** Ein genehmigtes Hull oder Panzer (`reference_image_base64` beim nächsten Objekt, oder `variation_of` vom bestehenden Spieler-Panzer).
2. **Pro Modul-Kategorie generieren**, nicht pro Kombination:
   - `create_object` — **Chassis** „light / medium / heavy“ jeweils **8 dirs**.
   - **Weapon** — oft **side view** oder **top barrel** + Rotation im Spiel; Railgun länger, Shotgun breiter — separates Asset pro `WeaponDefinition.Id`.
   - **Sensor** — kleines Objekt oder nur **Icon** + optional Decal.
   - **CPU** — priorisiert **Icons** (klare Lesbarkeit im UI).
3. **Konsistenz:** gleicher Prompt-Baustein pro Batch (**„same sci-fi lab tank style as ScriptTanks reference, worn metal, teal accents player / rust enemy“**).
4. **Naming:** Ordner nach IDs aus dem Spiel (`railgun`, `basic_radar`, `fast_reflex_cpu`), nicht nach Dateiname „sprite17“.

## Ordner-Konvention (Vorschlag unter `script-tanks/assets/`)

```text
sprites/modules/chassis/<id>/rotations/*.png
sprites/modules/weapons/<id>/      # barrel.png oder rotations/
sprites/modules/sensors/<id>/       # decal + optional ui_icon.png
sprites/modules/cpu/<id>/ui_icon.png
sprites/ui/module_icons/            # Fallback zentrale Icons
```

`manifest_pixellab_v*.json` kann pro Eintrag `role: weapon_railgun` usw. tragen — wie schon bei Panzer/Boden.

## MVP vs. später

- **Operator Demo / MVP2:** Chassis-Varianten + 3 Waffen + 3 Sensoren + 3 CPUs — überschaubar; **Icons + ein Hull-Set pro Größe** reicht oft.
- **Volles Grid später:** nur neue **Slot-Typen** + **Anker** auf Hull-Mesh — keine neue Rendering-Philosophie.

## Kurz-Checkliste

- [ ] Einheitliche **Zellgröße** und **Outline-Stil** für alle neuen Module.
- [ ] **Keine** kombinatorischen Gesamt-Renders als Hauptweg.
- [ ] **Modul-ID** im Code = Ordnername / Manifest-Eintrag.
- [ ] Turm vs. Hull **Farbe/Ton** unterscheiden (bereits im Target Look).

## Stand Repo (Chassis + Standard-Rohr)

Chassis **`hull_only_v2`**: nur Rumpf mit **leerem Turmlager**. Standardkanone **`barrel_only_v2`**: **Lauf + Verschluss**, ohne großen Turmkorb — gemeinsam stackbar. Details und Backup alter Sets: `script-tanks/assets/manifest_pixellab_v1.json` und `sprites/modules/README.md`.
