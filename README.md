# Challenge: Wissensdatenbank mit semantischem Netz (C#/.NET)

## 1) Kurzbeschreibung
Prototyp einer Wissensdatenbank auf Basis eines semantischen Netzes. Knoten repräsentieren Konzepte, Kanten die Beziehungen. Traversierung erfolgt rekursiv (wie im Unterrichtsbeispiel). Persistenz via JSON.

## 2) Muss-Ziele ✓
- [x] Datenstruktur: allgemeiner Baum (`Knoten` mit `List<Kante>`, `Kante` zeigt auf nächsten `Knoten`)
- [x] Wissensstruktur aus Beispieltext vollständig erfasst
- [x] Abfragen vom Startknoten (direkte Nachbarn & tiefe Suche per DFS)
- [x] Persistenz (Speichern/Laden als JSON)
- [x] Interaktive Abfragen (Startknoten & Modus wählbar)
- [x] Persistenz auch als Datei (graph.json) mit URL-Ausgabe

## 3) Optionale Ziele
- [ ] Graphische Darstellung (nicht notwendig für Abgabe)
- [x] Zwei Suchvarianten (schnell: direkte Nachbarn; vertieft: DFS-Pfade)
- [x] ASCII-Darstellung des Baums
- [x] Export als Graphviz DOT-Datei

## 4) Anwendungsfälle (Use Cases)
- UC-1: Direkte Beziehungen zu einem Knoten anzeigen (z. B. ab „Katze“).
- UC-2: Vertiefte Suche (DFS) ab einem Knoten bis definierter Tiefe.
- UC-3: Persistenz – Graph als JSON exportieren und wieder laden.
- UC-4: Interaktive Abfrage (Startknoten eingeben, Modus „schnell“/„tief“ wählen)
- UC-5: Ressourcen-URLs zu Knoten ausgeben

## 5) Testfälle & Ergebnisse
**T1 – Nachbarn ab „Katze“ (erwartet: ist→Tier, jagt→Maus, schläft im→Haus)**  
```
[Schnelle Suche] Direkte Nachbarn von 'Katze':
  Katze -[ist]-> Tier
  Katze -[jagt]-> Maus
  Katze -[schläft im]-> Haus
```

**T2 – Gefiltert „ist“-Beziehungen ab „Katze“ (erwartet: ist→Tier)**  
```
[Schnelle Suche] Nur 'ist'-Beziehungen ab 'Katze':
  Katze -[ist]-> Tier
```

**T3 – Tiefe Suche (bis 3) ab „Katze“ (erwartete Pfade inkl. Maus→Lebewesen)**  
```
[Vertiefte Suche] Tiefe Suche (bis 3) ab 'Katze':
  - Katze-[ist]->Tier
  - Katze-[jagt]->Maus
  - Katze-[jagt]->Maus -> Maus-[ist]->Lebewesen
  - Katze-[schläft im]->Haus
  - Katze-[schläft im]->Haus -> Haus-[ist]->Gebäude
```

**T4 – Persistenz: JSON (Ausschnitt) & Reload-Check**  
```
JSON: {...}
Reload Check (Nachbarn von 'Katze'):
  Katze -[ist]-> Tier
  Katze -[jagt]-> Maus
  Katze -[schläft im]-> Haus
```

**T5 – Rekursive Traversierung (Durchforsten) wie im Unterricht**  
```
Durchforsten (ab 'Katze'):
Knoteninhalt: Katze
Kanteninhalt: ist
Knoteninhalt: Tier
Kanteninhalt: jagt
Knoteninhalt: Maus
Kanteninhalt: ist
Knoteninhalt: Lebewesen
Kanteninhalt: schläft im
Knoteninhalt: Haus
Kanteninhalt: ist
Knoteninhalt: Gebäude
```

**T6 – Interaktive Abfrage (Startknoten „Katze“, Modus „schnell“)**
```
Startknoten eingeben (oder leer zum Beenden): Katze
Suchmodus [schnell|tief]: schnell

[Schnelle Suche] Direkte Nachbarn von 'Katze':
  Katze -[ist]-> Tier
  Katze -[jagt]-> Maus
  Katze -[schläft im]-> Haus
```

**T7 – Export DOT-Datei**
```
Graphviz-Datei geschrieben: graph.dot  (optional rendern mit: dot -Tpng graph.dot -o graph.png)
```

## 6) Architektur / Klassen
- `Knoten`: hält `List<Kante>` und String `Bedeutung`
- `Kante`: hält String `Bedeutung` (Relation) und Referenz auf nächsten `Knoten`
- `Graph`: Komfortfunktionen `AddTriple`, `Neighbors`, `DeepSearchDFS`, `ToJson`, `FromJson`, `ToDot`, `PrintAscii`

## 7) Installation & Start
1. .NET SDK 8 installieren (macOS)
2. Im Projektordner:
   ```bash
   dotnet run
   ```

## 8) Zeitplanung (Soll) & Controlling (Ist)
**Soll (Beispiel):**
- Modelle 10min, Graph-Methoden 15min, Daten einpflegen 10min,
- Abfragen 15min, Persistenz 10min, Tests 10min, Doku 10min (Summe ~80min)

**Ist (ausfüllen):**
- Modelle: __min
- Graph-Methoden: __min
- Daten einpflegen: __min
- Abfragen: __min
- Persistenz: __min
- Tests: __min
- Doku: __min
- Menü/Interaktiv: __min
- DOT-Export: __min

## 9) Hinweise
- Zeichenkodierung: Konsole ggf. auf UTF-8 setzen (siehe Tipp unten).
- Erweiterungen (optional): BFS für kürzeste Pfade, einfache Visualisierung.

## 10) Tipp: Umlaute korrekt ausgeben
Am Anfang von `Main()` ergänzen:
```csharp
Console.OutputEncoding = System.Text.Encoding.UTF8;
```
Für „unschöne“ Unicode-Escapes im JSON kann optional serialisiert werden mit:
```csharp
var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
var json = JsonSerializer.Serialize(dto, options);
```
