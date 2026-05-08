# SiteTrack DXF Extractor

A **two-part tool** that bridges AutoCAD and SiteTrack for seamless infrastructure data extraction:

1. **GetData.lsp** — AutoCAD LISP script that extracts selected objects (points, lines, curves, text) and writes JSON
2. **DxfCoordinateExtractor** — Windows Forms app that reviews and exports the JSON in SiteTrack-ready formats

---

## Quick Start

**New to this tool?** Start here:
- 📖 [Quick Start Guide (5 min read)](QUICKSTART.md) — Get up and running in minutes
- 📚 [Complete Integration Guide](INTEGRATION_GUIDE.md) — Detailed workflow, setup, troubleshooting, and data mapping

---

## What It Does

AutoCAD drawings contain infrastructure data (manholes, pipes, annotations) scattered across many layers. This tool extracts that data directly from AutoCAD and produces JSON files that SiteTrack can import — no manual data entry required.

### Workflow

```
Your DXF in AutoCAD
        ↓
  Load GetData.lsp
        ↓
  Select objects → Run 'getData' command
        ↓
  JSON file created (sitetrack_data.json)
        ↓
  Windows app detects & displays data
        ↓
  Review in grid, then export
        ↓
  Import into SiteTrack
```

**Three simple phases:**
1. **Extract** — Run `getData` in AutoCAD to extract selected objects
2. **Review** — Windows app displays extracted data in grid format
3. **Export** — Click "Export" to create SiteTrack-ready JSON file

---

## How It Works

### Part 1: GetData.lsp (AutoCAD Script)

Run in AutoCAD to extract selected objects:

```
(load "C:\\path\\to\\GetData.lsp")
getData
```

✓ Supports: Points, Lines, Polylines, Arcs, Circles, Text, MText  
✓ Outputs: JSON file to `%USERPROFILE%\Documents\sitetrack_data.json`  
✓ Compatible: AutoCAD 2007+

### Part 2: DxfCoordinateExtractor (Windows App)

Review and export the extracted data:

✓ Auto-detects JSON file created by GetData.lsp  
✓ Displays extracted objects in grid view  
✓ Filter by type (points, lines, curves, text)  
✓ Export as JSON for SiteTrack import

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

## Getting Started

### For Users

**Fastest path (5 minutes):**
1. See [QUICKSTART.md](QUICKSTART.md) for essential steps

**Complete workflow with examples:**
1. See [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) for detailed setup, troubleshooting, and best practices

### Installation

**GetData.lsp:**
```
Copy to: C:\Users\[YourName]\Documents\AutoCAD\LISP\
Load in AutoCAD: (load "C:\\path\\to\\GetData.lsp")
```

**Windows App:**
```
Build: dotnet build
Run: bin\Release\DxfCoordinateExtractor.exe
```

### Basic Workflow

1. **In AutoCAD:**
   - Open your DXF file
   - Select objects you want to extract
   - Run `getData` command
   - Check Documents folder for `sitetrack_data.json`

2. **In Windows App:**
   - App auto-detects the JSON file
   - Review extracted data in the grid
   - Click **Export AutoCAD Data** to save SiteTrack-ready JSON
   - Verify coordinates and object types

3. **In SiteTrack:**
   - Create new project
   - Import the JSON file
   - Verify points appear as junctions, lines as pipes
   - Begin analysis and management

---

## Data Supported

| Source | Extracted As |
|--------|--------------|
| **POINT** entity | Junction/Node |
| **LINE** entity | Pipe connection |
| **LWPOLYLINE / POLYLINE** | Pipe path |
| **ARC / CIRCLE** | Bend or fitting |
| **TEXT / MTEXT** | Label or annotation |

Text formatting codes (MTEXT `\P`, `\A`, `\C` sequences) are automatically cleaned.

---

## Output Format

### JSON for SiteTrack

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-08T14:30:00Z",
  "source": "AutoCAD",
  "coordinateSystemHint": "UTM",
  "points": [
    { "type": "POINT", "E": 335684.045, "N": 2678151.885, "layer": "MANHOLE_POINTS" }
  ],
  "lines": [
    { "type": "LINE", "startE": 335684, "startN": 2678151, "endE": 335750, "endN": 2678200, "layer": "PIPE_MAIN" }
  ],
  "curves": [ ... ],
  "texts": [ ... ]
}
```

All coordinates and structure are optimized for direct SiteTrack import.
