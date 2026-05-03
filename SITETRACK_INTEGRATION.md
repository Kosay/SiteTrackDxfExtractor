# SiteTrack Integration Summary

## Overview

The DXF Extractor tool is now fully integrated to export infrastructure network data as JSON compatible with **SiteTrack's project import system**. Users can seamlessly extract AutoCAD DXF files and import them directly into SiteTrack.

---

## Workflow

```
DXF File
   ↓
Open in DXF Extractor
   ↓
Assign layer roles (Node, Pipe, Label)
   ↓
Export for SiteTrack → _network.json
   ↓
SiteTrack Admin UI
   ↓
Import JSON → Create New Project
   ↓
Network visualization & management in SiteTrack
```

---

## JSON Export Format

The tool exports a **SiteTrack-compatible JSON** file with the following structure:

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T10:30:00Z",
  "sourceFileName": "network.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "MH-001",
      "name": "MH-001",
      "E": 335684.045,
      "N": 2678151.885,
      "layer": "MANHOLE_ACCESS",
      "properties": {}
    },
    {
      "id": "MH-002",
      "name": "MH-002",
      "E": 335750.000,
      "N": 2678200.000,
      "layer": "MANHOLE_ACCESS",
      "properties": {}
    }
  ],
  "connections": [
    {
      "id": "PIPE-001",
      "fromPointId": "MH-001",
      "toPointId": "MH-002",
      "length": 93.97,
      "properties": {
        "slope": 0.5,
        "diameter": 500
      }
    }
  ]
}
```

### Key Features

- **schemaVersion: 1** — SiteTrack's supported schema version
- **exportedAt** — ISO-8601 timestamp for audit trail
- **sourceFileName** — Original DXF filename for tracking
- **coordinateSystemHint** — Coordinate system used (UTM, WGS84, Local)
- **points** — Network junctions/nodes (manholes, intersections, pump stations, etc.)
- **connections** — Links between points (pipes, roads, utility lines)
- **properties** — Optional metadata (slope, diameter, material, etc.)

---

## Validation & Import Rules

SiteTrack's import validator enforces:

### Critical (Import Fails)
- ✓ schemaVersion must be `1`
- ✓ E, N coordinates must be **numeric** (not strings)
- ✓ All point IDs must be unique
- ✓ All connection IDs must be unique
- ✓ fromPointId and toPointId must reference existing points
- ✓ points array cannot be empty

### Warnings (Import Proceeds)
- No orphaned points (points with no connections)
- No negative or zero lengths
- Coordinate system matches actual data

**Full validation rules**: See [DXF_INTEGRATION_GUIDE.md](DXF_INTEGRATION_GUIDE.md#validation-rules)

---

## Export Files

When you click **"Export for SiteTrack"** in the tool, it saves **three files**:

| File | Purpose | Format |
|---|---|---|
| `{name}_nodes.csv` | Legacy SiteTrack CSV import | CSV with columns: name, E, N, Zone, connections |
| `{name}_connections.csv` | Legacy connection metadata | CSV with columns: id, from, to, length, slope, diameter |
| `{name}_network.json` | **New JSON import** | JSON compatible with SiteTrack project creation |

---

## How to Use with SiteTrack

### Step 1: Export from DXF Extractor
```
1. Run DxfCoordinateExtractor.exe
2. Open a DXF file
3. Assign layer roles:
   - Node layers → set role to "Node"
   - Pipe layers → set role to "Pipe"
   - Label layers → set role to "Label"
4. Click "Export for SiteTrack"
5. Save to a location (e.g., C:\exports\sewer_network)
```

### Step 2: Import into SiteTrack
```
1. Log into SiteTrack as Admin
2. Navigate to Projects → Create New Project
3. Upload {name}_network.json
4. SiteTrack validates and displays the network
5. Review and confirm to create the project
```

---

## Supported Network Types

The tool works with any infrastructure network:

- **Sewer Systems** — Manholes, pipes, laterals, treatment plants
- **Water Distribution** — Junctions, pipes, pump stations, storage tanks
- **Stormwater** — Catch basins, outfalls, detention ponds
- **Gas Networks** — Regulators, distribution lines, service points
- **Electrical** — Substations, feeders, distribution lines
- **Roads & Streets** — Intersections, road segments, traffic signals
- **Utilities** — Generic lines, poles, service areas
- **Pipelines** — Transmission, distribution, directional drill paths

---

## Layer Filtering

The tool intelligently filters DXF layers. It **includes**:
- `SEWER*`, `STORMWATER*`, `WATER*`, `GAS*`, `ELECTRICAL*`
- `ROAD*`, `STREET*`, `PIPELINE*`, `UTILITY*`
- `JUNCTION*`, `MANHOLE*`, `NODE*`

It **excludes**:
- `ANNOTATION*`, `LABEL*`, `TEXT*`, `DIMENSION*`
- `BUILDING*`, `PROPERTY*`, `PARCEL*`
- `SURVEY*`, `TOPO*`, `CONSTRUCTION*`

**User can override** any layer role in the UI.

---

## Configuration

### Adjustable Settings
- **Snap Tolerance** (default: 2.0 m) — Max distance to match polyline endpoints to nodes
- **Text Search Radius** (default: 5.0 m) — Max distance to find nearby text labels for manhole names
- **Coordinate System Hint** (hardcoded: UTM) — Can be changed manually in JSON if needed

---

## Data Quality & Best Practices

### Before Exporting
1. **Clean the DXF**:
   - Remove unused/broken layers
   - Fix overlapping geometry
   - Ensure all pipes connect properly
   - Add text labels near manholes

2. **Verify coordinates**:
   - All coordinates should be in the same coordinate system (UTM, WGS84, etc.)
   - No coordinates at origin (0, 0) unless intentional
   - Check for units mismatch (e.g., feet vs meters)

3. **Check topology**:
   - Each pipe should connect two nodes
   - Nodes should represent actual junctions/endpoints
   - Avoid self-loops (pipe from node to itself)

### Export Validation
- Review the generated CSV files first (`_nodes.csv`, `_connections.csv`)
- Check that node counts and connection counts are correct
- Verify a few sample coordinates to ensure accuracy
- Open the JSON in a text editor to spot-check structure

### Import Troubleshooting
If import fails, check:
1. **JSON syntax** — Use a JSON validator (jsonlint.com)
2. **Numeric coordinates** — Ensure E, N are not quoted strings
3. **Referential integrity** — All point IDs referenced in connections must exist
4. **Unique IDs** — No duplicate point or connection IDs
5. **Coordinate system** — Verify coordinates match the declared hint

**See [DXF_INTEGRATION_GUIDE.md](DXF_INTEGRATION_GUIDE.md#error-troubleshooting) for detailed troubleshooting.**

---

## Documentation

### Main Guides
- **[DXF_INTEGRATION_GUIDE.md](DXF_INTEGRATION_GUIDE.md)** — Comprehensive 584-line guide covering:
  - Complete JSON schema specification
  - Field reference tables with validation rules
  - Coordinate system guidance (UTM, WGS84, Local)
  - Layer filtering strategy
  - Real-world data mapping examples
  - Implementation checklist for tool developers
  - Error troubleshooting guide

- **[docs/DXF_EXPORT_SCHEMA.md](docs/DXF_EXPORT_SCHEMA.md)** — Quick reference guide with:
  - Minimal valid JSON example
  - Validation rules table
  - Implementation tips
  - Real-world road network example
  - Common troubleshooting

### README
- **[README.md](README.md)** — Original tool documentation

---

## Code Changes

### Modified Files
- **Form1.cs** — Enhanced JSON export to include `properties` field in points for schema consistency

### New Documentation
- **DXF_INTEGRATION_GUIDE.md** — 584-line comprehensive integration guide
- **docs/DXF_EXPORT_SCHEMA.md** — Quick reference for developers
- **SITETRACK_INTEGRATION.md** — This file (summary and usage guide)

---

## Examples

### Example 1: Sewer Network Export
See [DXF_INTEGRATION_GUIDE.md — Data Mapping Examples](DXF_INTEGRATION_GUIDE.md#example-1-sewer-network-with-manholes-and-pipes)

Input: DXF with manhole blocks and polyline pipes  
Output: JSON with 3 nodes and 2 connections

### Example 2: Water System
See [DXF_INTEGRATION_GUIDE.md — Data Mapping Examples](DXF_INTEGRATION_GUIDE.md#example-2-water-distribution-with-pump-stations)

Input: DXF with pump stations and distribution lines  
Output: JSON with pump station properties

### Example 3: GPS-Based Road Network (WGS84)
See [DXF_INTEGRATION_GUIDE.md — Data Mapping Examples](DXF_INTEGRATION_GUIDE.md#example-3-gps-based-road-network-wgs84)

Input: DXF with WGS84 coordinates  
Output: JSON marked with `coordinateSystemHint: "WGS84"`

---

## Next Steps

1. **Test the export** — Generate JSON from a test DXF file
2. **Validate JSON** — Use https://jsonlint.com to verify syntax
3. **Import into SiteTrack** — Test the end-to-end workflow
4. **Gather feedback** — Refine based on real-world use cases
5. **Document edge cases** — Add any site-specific requirements to the guide

---

## Contact & Support

For questions or issues:
- Review [DXF_INTEGRATION_GUIDE.md](DXF_INTEGRATION_GUIDE.md)
- Check error messages in the troubleshooting section
- Verify JSON syntax with https://jsonlint.com
- Inspect the generated CSV files for data quality

---

**Status**: Ready for SiteTrack Integration  
**Last Updated**: 2026-05-03  
**Schema Version**: 1
