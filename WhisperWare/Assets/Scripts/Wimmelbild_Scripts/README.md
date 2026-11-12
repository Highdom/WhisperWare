# Wimmelbild

Die Szene `Assets/Scenes/Wimmelbild.unity` ist spielbereit. Im Play-Modus die Objekte mit der linken Maustaste oder per Touch auf ihre nummerierten Zielplaetze ziehen. Falsche Ablagen setzen das Objekt an seine Startposition zurueck. Richtig abgelegte Objekte bleiben gesperrt. **Neu starten** setzt das ganze Raetsel zurueck; **Escape** bricht das aktuelle Ziehen ab.

## Anzahl und Reihenfolge einstellen

1. Ausserhalb des Play-Modus in der Hierarchy **WimmelbildPuzzle** auswaehlen.
2. Im Inspector unter **Gesuchte Objekte - von oben nach unten** mit **+ / -** Eintraege hinzufuegen oder entfernen. Die Listengroesse ist die Anzahl gesuchter Objekte.
3. Objekte aus **WimmelbildPuzzle > Objekte** in die Listenfelder ziehen. Jedes Objekt darf nur einmal eingetragen sein.
4. Die Zeilen am linken Griff verschieben: Der oberste Eintrag gehoert auf Ziel 1, der zweite auf Ziel 2 usw.
5. **Zielvorschau aktualisieren** anklicken und die Szene speichern. Beim Spielstart wird die Zielanzahl automatisch aktualisiert.

**Nacheinander ablegen** erzwingt auch die zeitliche Reihenfolge: erst Ziel 1, dann Ziel 2 usw. Ohne Haekchen duerfen die richtigen Zielplaetze in beliebiger zeitlicher Reihenfolge belegt werden. **Objektnamen an Zielen anzeigen** blendet die Loesungsnamen ein oder aus.

Aenderungen waehrend Play gelten erst nach **Aenderungen anwenden / Neu starten** und werden beim Beenden von Play von Unity verworfen.

## Eigene Objekte verwenden

- Ein vorhandenes Objekt unter **Objekte** duplizieren, seinen Namen und im **Sprite Renderer** den Sprite ersetzen, Position und Groesse anpassen. Optional im **Wimmelbild Item** einen Anzeigenamen eintragen.
- Die **Box Collider 2D**-Flaeche muss den sichtbaren Sprite abdecken. **Edit Collider** verwenden, wenn der neue Sprite andere Abmessungen hat. Fuer neue GameObjects die Komponente **Wimmelbild Item** hinzufuegen; sie ergaenzt SpriteRenderer und BoxCollider2D automatisch.
- Fuer diese durch Ziehen bewegten 2D-Objekte keinen dynamischen Rigidbody2D und kein zusaetzliches Drag-Skript verwenden.
- Das Objekt in der Loesungsliste zuweisen. Objekte unter **Objekte**, die nicht in der Liste stehen, dienen als Ablenkung und werden nie als Loesung akzeptiert.
- **Zielplaetze** verschiebt die ganze Zielreihe. **Columns** und **Slot Spacing** bestimmen Spaltenzahl und Abstaende. Bei vielen Objekten mehrere Reihen einplanen und die Kamera/Objektpositionen entsprechend anpassen. **Zielvorlage** bleibt deaktiviert und bestimmt das Aussehen sowie die Trefferflaeche aller Ziele.

## Reaktionen und Tests

Die Inspector-Ereignisse **On Correct Placement**, **On Wrong Placement** und **On Puzzle Completed** koennen mit Sound, Animationen oder eigener Folgelogik verbunden werden. `ResetPuzzle()` ist auch aus anderen Skripten oder UnityEvents aufrufbar. `IsCompleted`, `PlacedCount` und `RequiredCount` liefern den aktuellen Zustand.

**Tools > Wimmelbild > Spielregeln pruefen (Play-Modus)** testet im Play-Modus unter anderem falsche Reihenfolge, falsches Ziel, Ablenkungsobjekte, Neustart, Abschlussereignis, umsortierte Listen, variable Anzahl und ungueltige Konfigurationen. Dabei wird die Runde zurueckgesetzt; die urspruengliche Konfiguration bleibt erhalten. Das Ergebnis erscheint in der Console und in `Temp/WimmelbildVerification.txt`.

**Tools > Wimmelbild > Szene einrichten** ist ein einmaliges Einrichtungswerkzeug fuer die urspruengliche Szene mit Triangle, Square, Capsule und Circle. Eine bereits eingerichtete Szene wird nicht erneut aufgebaut.
