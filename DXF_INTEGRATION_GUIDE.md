# DXF to SiteTrack Integration Guide

A complete guide for exporting AutoCAD DXF files as JSON compatible with SiteTrack's network import system.

---

## JSON Schema Specification

The JSON export must follow this structure for SiteTrack compatibility:

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T10:30:00Z",
  "sourceFileName": "network.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "pt-1",
      "name": "Junction A",
      "E": 234567.89,
      "N": 4567890.12,
      "layer": "MANHOLE_ACCESS",
      "properties": {}
    }
  ],
  "connections": [
    {
      "id": "conn-1",
      "fromPointId": "pt-1",
      "toPointId": "pt-2",
      "length": 145.67,
      "properties": {
        "slope": 0.5,
        "diameter": 500
      }
    }
  ]
}
```

---

## Field Reference Tables

### Root Level Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `schemaVersion` | Integer | Yes | Always `1`. Used by SiteTrack to detect format version. |
| `exportedAt` | ISO-8601 String | Yes | Timestamp when export was generated (UTC, e.g., `"2026-05-03T10:30:00Z"`). |
| `sourceFileName` | String | Yes | Original DXF filename (e.g., `"network.dxf"`). Helps track source in SiteTrack. |
| `coordinateSystemHint` | String | Yes | One of: `"UTM"`, `"WGS84"`, `"Local"`. Guides SiteTrack's coordinate handling. |
| `points` | Array | Yes | Network junctions/nodes. Must contain at least one point. |
| `connections` | Array | Yes | Links between points. May be empty if no pipes/edges. |

### Points (Network Junctions/Nodes)

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | String | Yes | Unique identifier within the export. Used to reference in connections. Pattern: `pt-1`, `MH-001`, etc. Must be unique. |
| `name` | String | Yes | Human-readable name. Appears in SiteTrack UI. Examples: `"Junction A"`, `"MH-001"`, `"Pump Station 1"`. |
| `E` | Number | Yes | Easting coordinate (numeric, not string). For UTM: meters. For WGS84: decimal degrees. Precision: 2-6 decimals. |
| `N` | Number | Yes | Northing coordinate (numeric, not string). For UTM: meters. For WGS84: decimal degrees. Precision: 2-6 decimals. |
| `layer` | String | No | Original DXF layer name. Optional; helps identify point source in SiteTrack. Examples: `"MANHOLE_ACCESS"`, `"JUNCTION"`, `"NODE_LAYER"`. |
| `properties` | Object | No | Optional metadata. Key-value pairs (strings, numbers, or booleans). Examples: `{"type": "manhole", "depth": 2.5}`. |

### Connections (Links Between Points)

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | String | Yes | Unique identifier. Pattern: `conn-1`, `PIPE-001`, etc. |
| `fromPointId` | String | Yes | References a point's `id`. Must exist in the `points` array. |
| `toPointId` | String | Yes | References a point's `id`. Must exist in the `points` array. |
| `length` | Number | No | Link length (numeric). For UTM/Local: meters. For WGS84: kilometers or degrees (application-dependent). If omitted, SiteTrack may calculate from point coordinates. |
| `properties` | Object | No | Optional metadata. Common fields: `{"slope": 0.5, "diameter": 500, "material": "PVC"}`. All values are optional. |

---

## Validation Rules

SiteTrack's import validator enforces these rules. Violations cause rejection:

### Critical (Import Fails)

| Rule | Error Example | Fix |
|---|---|---|
| **schemaVersion must be 1** | `"schemaVersion": 2` | Use `"schemaVersion": 1` |
| **E and N must be numbers** | `"E": "234567.89"` (string) | Use `"E": 234567.89` (no quotes) |
| **fromPointId must reference existing point** | `"fromPointId": "pt-999"` (doesn't exist) | Ensure all referenced IDs exist in `points` array |
| **toPointId must reference existing point** | `"toPointId": "pt-999"` | Ensure all referenced IDs exist in `points` array |
| **Circular references not allowed** | `pt-1 → pt-2 → pt-1` | Use acyclic graph or allow in app logic |
| **No duplicate point IDs** | Two points with `"id": "pt-1"` | Make each point ID unique |
| **No duplicate connection IDs** | Two connections with `"id": "conn-1"` | Make each connection ID unique |
| **points array cannot be empty** | `"points": []` | Add at least one point |

### Warnings (Import Proceeds; May Flag Issues)

| Rule | Example | Recommendation |
|---|---|---|
| **Orphaned points** | Point with no connections | Add connections or mark as isolated junction |
| **Missing coordinates** | `"E": null` | Provide valid numeric coordinates |
| **Unknown coordinateSystemHint** | `"coordinateSystemHint": "EPSG:4326"` | Use one of: `"UTM"`, `"WGS84"`, `"Local"` |
| **length = 0 or negative** | `"length": -10.5` | Use positive values or omit |

---

## Coordinate Systems

### UTM (Recommended for Infrastructure Networks)

- **Format**: Numeric meters
- **Example**: `E: 234567.89, N: 4567890.12`
- **Precision**: Typically 0-6 decimals (varies by zone size)
- **Use**: Regional surveys, large infrastructure networks
- **SiteTrack Hint**: `"coordinateSystemHint": "UTM"`

### WGS84 (GPS-Based)

- **Format**: Decimal degrees (latitude, longitude)
- **Example**: `E: 35.0123456, N: 31.9876543`
- **Precision**: 5-8 decimals recommended (1-11 cm accuracy)
- **Mapping**: Longitude → E, Latitude → N
- **Use**: Mobile-collected data, international projects
- **SiteTrack Hint**: `"coordinateSystemHint": "WGS84"`

### Local (Project-Specific)

- **Format**: Numeric, arbitrary origin
- **Example**: `E: 1000.5, N: 2500.3`
- **Precision**: Application-dependent
- **Use**: Internal CAD grids, site-specific surveys
- **SiteTrack Hint**: `"coordinateSystemHint": "Local"`

**Important**: SiteTrack uses the `coordinateSystemHint` to interpret coordinates. Ensure accuracy; mismatches cause location errors in the network map.

---

## Layer Filtering Strategy

When exporting from DXF, apply intelligent filtering to include only infrastructure network data and exclude annotations, boundaries, and non-network geometry.

### Include These Network Layers

| Pattern | Examples | Network Type |
|---|---|---|
| `SEWER*` | SEWER_MAIN, SEWER_SECONDARY, SEWER_LATERAL | Sanitary/stormwater sewer |
| `STORMWATER*` | STORMWATER_PRIMARY, STORMWATER_COLLECTION | Storm drain system |
| `WATER*` | WATER_MAIN, WATER_SERVICE, WATER_DISTRIBUTION | Water supply |
| `GAS*` | GAS_TRANSMISSION, GAS_DISTRIBUTION, GAS_SERVICE | Gas utility |
| `ELECTRICAL*` | ELECTRICAL_MAIN, ELECTRICAL_SECONDARY, ELECTRICAL_FEEDER | Electrical utility |
| `ROAD*` | ROAD_SURFACE, ROAD_CENTER, ROAD_NETWORK | Road/street network |
| `STREET*` | STREET_EDGE, STREET_PAVEMENT, STREET_LANES | Street infrastructure |
| `PIPELINE*` | PIPELINE_OIL, PIPELINE_WATER, PIPELINE_GAS | Pressure pipelines |
| `UTILITY*` | UTILITY_LINES, UTILITY_POLES, UTILITY_CORRIDOR | Generic utilities |
| `JUNCTION*` | JUNCTION_NODE, JUNCTION_POINT | Network nodes/junctions |
| `MANHOLE*` | MANHOLE_ACCESS, MANHOLE_INSPECTION, MANHOLE_CLEAN | Access points |

### Exclude These Non-Network Layers

| Pattern | Examples | Reason |
|---|---|---|
| `ANNOTATION*` | ANNOTATION_TEXT, ANNOTATION_SYMBOL | Not network data |
| `LABEL*` | LABEL_STREET, LABEL_ZONE | Not network data |
| `TEXT*` | TEXT_GENERAL, TEXT_DIMENSION | Formatting/annotations |
| `DIMENSION*` | DIMENSION_LINEAR, DIMENSION_ANGULAR | CAD measurements |
| `BUILDING*` | BUILDING_OUTLINE, BUILDING_FOOTPRINT | Not infrastructure network |
| `PROPERTY*` | PROPERTY_BOUNDARY, PROPERTY_PARCEL | Land boundaries |
| `PARCEL*` | PARCEL_LINE, PARCEL_EDGE | Land divisions |
| `SURVEY*` | SURVEY_POINT, SURVEY_CONTROL | Reference geometry |
| `TOPO*` | TOPO_CONTOUR, TOPO_ELEVATION | Terrain, not network |
| `CONSTRUCTION*` | CONSTRUCTION_NOTES, CONSTRUCTION_ZONE | Project-specific, temporary |
| `REFERENCE*` | REFERENCE_GRID, REFERENCE_AXIS | CAD axes/grids |

**Filtering Strategy**:
1. Load DXF; scan all layer names
2. Match layer name against "Include" patterns (case-insensitive wildcard match)
3. If match → include layer; if no match → exclude
4. User can override (e.g., include a custom layer like `CLIENT_SEWERS`)

---

## Data Mapping Examples

### Example 1: Sewer Network with Manholes and Pipes

**DXF Source:**
- Layer `MANHOLE_ACCESS` (Insert blocks) → Network nodes
- Layer `SEWER_MAIN` (Polyline2D) → Pipe connections
- Layer `SEWER_LABEL` (Insert blocks with text like "L=100m S=0.5%") → Metadata

**Expected JSON:**
```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T10:30:00Z",
  "sourceFileName": "sewer_network.dxf",
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
    },
    {
      "id": "MH-003",
      "name": "MH-003",
      "E": 335800.000,
      "N": 2678250.000,
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
    },
    {
      "id": "PIPE-002",
      "fromPointId": "MH-002",
      "toPointId": "MH-003",
      "length": 66.12,
      "properties": {
        "slope": 0.3,
        "diameter": 500
      }
    }
  ]
}
```

### Example 2: Water Distribution with Pump Stations

**DXF Source:**
- Layer `WATER_JUNCTION` (Insert blocks) → Junctions
- Layer `WATER_PUMP` (Insert blocks) → Pump stations (special nodes)
- Layer `WATER_MAIN` (Polyline2D) → Water pipes

**Expected JSON:**
```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T11:00:00Z",
  "sourceFileName": "water_system.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "J-001",
      "name": "Distribution Junction A",
      "E": 245000.50,
      "N": 5125000.75,
      "layer": "WATER_JUNCTION",
      "properties": {
        "type": "junction"
      }
    },
    {
      "id": "PS-001",
      "name": "Pump Station 1",
      "E": 245100.00,
      "N": 5125050.00,
      "layer": "WATER_PUMP",
      "properties": {
        "type": "pump_station",
        "capacity_l_s": 50.0
      }
    }
  ],
  "connections": [
    {
      "id": "MAIN-001",
      "fromPointId": "J-001",
      "toPointId": "PS-001",
      "length": 141.42,
      "properties": {
        "diameter": 200,
        "material": "PVC"
      }
    }
  ]
}
```

### Example 3: GPS-Based Road Network (WGS84)

**DXF Source:**
- Layer `ROAD_CENTER` (Polyline2D) → Road segments
- Collected with GPS (WGS84 coordinates)

**Expected JSON:**
```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T12:00:00Z",
  "sourceFileName": "road_survey.dxf",
  "coordinateSystemHint": "WGS84",
  "points": [
    {
      "id": "RJ-001",
      "name": "Road Junction 1",
      "E": 35.123456,
      "N": 31.987654,
      "layer": "ROAD_CENTER",
      "properties": {}
    },
    {
      "id": "RJ-002",
      "name": "Road Junction 2",
      "E": 35.124500,
      "N": 31.988000,
      "layer": "ROAD_CENTER",
      "properties": {}
    }
  ],
  "connections": [
    {
      "id": "ROAD-001",
      "fromPointId": "RJ-001",
      "toPointId": "RJ-002",
      "properties": {
        "width": 8.0,
        "surface": "asphalt"
      }
    }
  ]
}
```

---

## Implementation Checklist for DXF Tool Developers

Use this checklist when updating the DXF exporter:

- [ ] **JSON Structure**
  - [ ] Root object contains `schemaVersion`, `exportedAt`, `sourceFileName`, `coordinateSystemHint`
  - [ ] `schemaVersion` is always `1` (integer)
  - [ ] `exportedAt` is ISO-8601 format with timezone (e.g., `"2026-05-03T10:30:00Z"`)
  - [ ] `points` array is present (may be empty in edge cases, but best if ≥1)
  - [ ] `connections` array is present (may be empty)

- [ ] **Points (Nodes)**
  - [ ] Each point has unique `id`
  - [ ] Each point has human-readable `name`
  - [ ] `E` and `N` are numbers (not strings), with appropriate precision
  - [ ] `layer` field is populated (optional but recommended)
  - [ ] `properties` object exists (can be empty `{}`)

- [ ] **Connections (Links)**
  - [ ] Each connection has unique `id`
  - [ ] `fromPointId` references an existing point `id`
  - [ ] `toPointId` references an existing point `id`
  - [ ] `length` is numeric or omitted (not string or null)
  - [ ] `properties` object exists and may contain `slope`, `diameter`, etc. (all optional)

- [ ] **Data Quality**
  - [ ] No duplicate point IDs
  - [ ] No duplicate connection IDs
  - [ ] All point references resolve (no broken links)
  - [ ] Coordinates match the declared `coordinateSystemHint`
  - [ ] Layer names are preserved in the `layer` field

- [ ] **User Interface (DXF Tool)**
  - [ ] User can select layer roles (Node, Pipe, Label)
  - [ ] User can set coordinate system hint (dropdown: UTM, WGS84, Local)
  - [ ] User can adjust snap tolerance for topology matching
  - [ ] User can specify text search radius for label proximity
  - [ ] Export button generates valid JSON file

- [ ] **Testing**
  - [ ] Validate JSON syntax (no parsing errors)
  - [ ] Validate against schema (all required fields present)
  - [ ] Test with empty layers (graceful handling)
  - [ ] Test with large DXF files (performance acceptable)
  - [ ] Test coordinate systems (UTM, WGS84, Local)
  - [ ] Test with SiteTrack import (actual end-to-end validation)

- [ ] **Documentation**
  - [ ] Integration guide provided to users
  - [ ] Example DXF files included
  - [ ] Expected output JSON samples provided
  - [ ] Layer filtering strategy documented
  - [ ] Troubleshooting guide included

---

## Error Troubleshooting

### Import Rejected: "schemaVersion must be 1"
**Cause**: JSON has `"schemaVersion": 0` or missing.  
**Fix**: Ensure root object has `"schemaVersion": 1` (integer, not string).

### Import Rejected: "E must be numeric, not string"
**Cause**: Coordinates are JSON strings instead of numbers.  
**Example**: `"E": "234567.89"` (invalid) vs. `"E": 234567.89` (valid)  
**Fix**: Remove quotes around numeric coordinate values.

### Import Rejected: "Point pt-1 referenced in connection but not found in points array"
**Cause**: A connection references a point ID that doesn't exist.  
**Example**: 
```json
"points": [{"id": "pt-1", ...}],
"connections": [{"fromPointId": "pt-999", ...}]  // pt-999 doesn't exist
```
**Fix**: 
1. Verify all referenced point IDs exist in the `points` array
2. Check for typos in IDs (case-sensitive)
3. Ensure topology is built before export

### Import Rejected: "Duplicate point ID: pt-1"
**Cause**: Two or more points have the same `id`.  
**Fix**: Ensure each point has a globally unique ID within the export.

### Import Warning: "Orphaned point pt-1 has no connections"
**Cause**: A point exists but no connection references it (may be intentional).  
**Recommendation**: 
- If isolated junctions are valid, ignore warning
- If point should be connected, add connections or remove the point

### Import Warning: "Coordinate system UTM detected, but coordinates appear to be WGS84"
**Cause**: Mismatch between declared hint and actual data.  
**Example**: Declared `"coordinateSystemHint": "UTM"` but coordinates are `E: 35.123, N: 31.987` (WGS84 range)  
**Fix**: 
1. Inspect coordinate ranges
2. Update `coordinateSystemHint` to match actual data
3. If conversion needed, transform coordinates before export

### File Too Large or Slow Import
**Cause**: Export contains thousands of points/connections.  
**Mitigation**:
- Filter unnecessary layers before export
- Simplify polylines (reduce vertex count)
- Split large networks into zone-based sub-projects
- Use chunked import (if SiteTrack supports)

---

## References

- **SiteTrack Project**: https://github.com/Kosay/SiteTrack
- **netDxf Library**: https://github.com/haplokuon/netDxf
- **JSON Specification**: https://www.json.org
- **ISO-8601 Dates**: https://en.wikipedia.org/wiki/ISO_8601

---

**Document Version**: 1.0  
**Last Updated**: 2026-05-03  
**Status**: Ready for Integration
