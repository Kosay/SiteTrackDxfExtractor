# SiteTrack DXF Extractor

A C# WinForms desktop tool that parses AutoCAD DXF files and exports infrastructure network data in formats ready for direct import into **SiteTrack**.

---

## What It Does

Engineers receive survey drawings in DXF format (AutoCAD). These drawings contain manholes, pipes, and annotation labels scattered across many layers. This tool reads those drawings and produces structured CSV and JSON files that SiteTrack's network importer accepts — no manual data entry required.

### Workflow

```
DXF File  →  Open in app  →  Assign layer roles  →  Export for SiteTrack  →  Import into SiteTrack
```

1. **Open** a DXF file (button or drag-and-drop)
2. **Review** all entities in the grid — filter by layer or type
3. **Assign roles** to each layer in the right panel (Node / Pipe / Label)
4. **Export for SiteTrack** — the app derives the network topology and writes three files

---

## Features

### DXF Entity Support

| Entity Type | What Is Extracted |
|---|---|
| **Insert** (block reference) | Insertion point (E, N) — used as manholes/nodes when layer role = Node |
| **Polyline2D** | All vertices in order — used as pipe geometry when layer role = Pipe |
| **Line** | Start and end coordinates |
| **Point** | Position |
| **Circle** | Center and radius |
| **Text / MText** | Text content and position — used to name manholes by proximity |

### Layer Role System

Each layer in the DXF is assigned a role in the right-side panel:

| Role | Meaning |
|---|---|
| **Node** | Insert blocks on this layer are manholes / junction points |
| **Pipe** | Polyline2D entities on this layer represent pipe runs |
| **Label** | Insert blocks on this layer are pipe annotation labels (e.g., `L=93.97m S=0.50%`) |
| **None** | Layer is visible in the grid but excluded from SiteTrack export |

### Manhole Naming

Manhole names (e.g., `MH-001`) come from nearby `TEXT` or `MTEXT` entities in the drawing. The **Text search radius** setting controls how far (in meters) the app searches around each manhole Insert block. If no text is found within that radius, the app auto-generates a sequential ID (`MH-001`, `MH-002`, …).

### Pipe Label Parsing

Pipe annotation blocks (e.g., FLOWARROW-type inserts) often carry metadata in their name field:

```
L=93.97m S=0.50%          →  length = 93.97 m, slope = 0.50%
%%C500 GRP                →  diameter = 500 mm (%%C = Ø symbol in AutoCAD)
S=0.30%  L=114.70m        →  slope = 0.30%, length = 114.70 m
```

The app parses these with regex and attaches them to the nearest pipe connection.

### Topology Engine

The app derives which manhole connects to which by spatial matching:

1. Collects all **Node** layer inserts as network nodes
2. For each **Pipe** layer polyline, takes its **first vertex** → finds the nearest node within the **Snap tolerance** → that is `fromNode`
3. Takes the **last vertex** → finds the nearest node → that is `toNode`
4. Calculates pipe length from polyline arc length (or uses the parsed `L=` label value if available)
5. Finds the nearest **Label** insert to attach slope and diameter metadata

Junction manholes (connecting to 3 or more pipes) are handled automatically — each matching pipe appends one entry to the node's connection list.

### Configurable Settings (in the UI)

| Setting | Default | Description |
|---|---|---|
| Snap tolerance | 2.0 m | Max distance from a polyline endpoint to a node to count as connected |
| Text search radius | 5.0 m | Max distance to search for a text label near a manhole insert |

---

## Export Output

Clicking **"Export for SiteTrack"** saves three files next to the chosen path:

### `_nodes.csv`
SiteTrack node import format. Each row is one manhole.

```
name,E,N,Zone,connections
MH-001,335684.045,2678151.885,Abu Samra_Network_SD$0$MH,"To:MH-002|L:93.970"
MH-002,335750.000,2678200.000,Abu Samra_Network_SD$0$MH,"To:MH-001|L:93.970; To:MH-003|L:66.120; To:MH-004|L:45.000"
MH-003,335800.000,2678250.000,Abu Samra_Network_SD$0$MH,"To:MH-002|L:66.120"
```

- `Zone` is populated from the layer name
- `connections` uses SiteTrack's `To:NodeName|L:length` syntax, semicolon-separated for multiple connections

### `_connections.csv`
SiteTrack connections import format. Each row is one pipe segment.

```
id,from,to,length,slope,diameter
PIPE-001,MH-001,MH-002,93.970,0.50,500
PIPE-002,MH-002,MH-003,66.120,0.30,
```

- `slope` and `diameter` are populated when a matching pipe label is found; otherwise blank

### `_network.json`
Structured JSON archive matching the SiteTrack `InfraNetwork` data model. Useful as a reference or for future direct JSON import.

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-04-24T10:00:00Z",
  "sourceFileName": "01-Abu Samra Layout.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    { "id": "MH-001", "name": "MH-001", "E": 335684.045, "N": 2678151.885, "layer": "..." }
  ],
  "connections": [
    {
      "id": "PIPE-001",
      "fromPointId": "MH-001",
      "toPointId": "MH-002",
      "length": 93.970,
      "properties": { "slope": 0.50, "diameter": 500 }
    }
  ]
}
```

### Standard CSV Export

The **"Export Selected to CSV"** button (green, bottom bar) exports the raw entity data as-is — useful for reviewing coordinates before running the SiteTrack export.

```
EntityType,Layer,PointRole,X,Y,Notes
Insert,Abu Samra_Network_SD$0$MH,InsertionPoint,335684.045,2678151.885,"Layout=Model;Block=MH"
```

---

## Requirements

- Windows 10 or later
- [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
- DXF files saved as AutoCAD 2000 format or newer

## Building from Source

```bash
git clone https://github.com/Kosay/SiteTrackDxfExtractor.git
cd SiteTrackDxfExtractor
dotnet build
```

Depends on [netDxf](https://github.com/haplokuon/netDxf) (pulled automatically via NuGet).

---

## How to Use — Step by Step

1. Run `DxfCoordinateExtractor.exe`
2. Click **Open DXF File** or drag a `.dxf` file onto the window
3. All entities appear in the main grid. Use the **Layer** and **Type** dropdowns to explore
4. In the **Layer Roles** panel (right side):
   - Select each manhole/node layer → set role to **Node**
   - Select each pipe/polyline layer → set role to **Pipe**
   - Select each annotation/label layer (FLOWARROW blocks) → set role to **Label**
5. Adjust **Snap tolerance** and **Text search radius** if needed
6. Click **Export for SiteTrack**
7. Choose a save location — three files are written automatically
8. In SiteTrack → Admin → Networks → Import CSV → import `_nodes.csv`
