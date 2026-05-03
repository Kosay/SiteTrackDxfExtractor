
# DXF to SiteTrack Integration Guide

This document specifies the exact JSON schema and requirements for integrating DXF files into SiteTrack via the **SiteTrackDxfExtractor** tool.

## Overview

SiteTrack imports infrastructure networks (roads, sewers, water lines, etc.) from DXF files by converting them to a standardized **connection.json** format. The DXF extraction tool must output JSON files that conform to this exact schema.

---

## Required JSON Schema (connection.json)

### Complete Structure

```json
{
  "schemaVersion": 1,
  "exportedAt": "2025-05-03T14:30:45.123Z",
  "sourceFileName": "Infrastructure_Network_v2.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "pt-001",
      "name": "Junction A",
      "E": 234567.89,
      "N": 4567890.12,
      "layer": "NETWORK_JUNCTIONS",
      "properties": {
        "elevation": 125.5,
        "material": "concrete",
        "diameter": "300mm"
      }
    }
  ],
  "connections": [
    {
      "id": "conn-001",
      "fromPointId": "pt-001",
      "toPointId": "pt-002",
      "length": 145.67,
      "properties": {
        "pipe_type": "gravity_sewer",
        "material": "PVC",
        "slope": 0.005
      }
    }
  ]
}
```

### Field Specifications

#### Root Object
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schemaVersion` | number | **Yes** | Must be exactly `1`. No other versions accepted. |
| `exportedAt` | string | **Yes** | ISO 8601 timestamp (e.g., `2025-05-03T14:30:45.123Z`). Use `new Date().toISOString()` in JavaScript or equivalent. |
| `sourceFileName` | string | **Yes** | Original DXF filename (e.g., `"Infrastructure_Network.dxf"`). Used for tracking and UI display. |
| `coordinateSystemHint` | string | **Yes** | Must be one of: `"UTM"`, `"WGS84"`, or `"Local"`. Describes the coordinate system of E/N values. |
| `points` | array | **Yes** | Array of connection points (junctions, endpoints, etc.). Must have at least 1 point. |
| `connections` | array | **Yes** | Array of connections between points. Can be empty if only creating points. |

#### Points Array
Each point object must contain:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | **Yes** | Unique identifier within this file (e.g., `"pt-001"`, `"J-101"`, `"NODE-A"`). Used to reference in connections. |
| `name` | string | **Yes** | Human-readable name (e.g., `"Junction A"`, `"Main Line Start"`, `"Manhole 5"`). Displayed in UI. |
| `E` | number | **Yes** | Easting coordinate. Numeric value (integer or float). **Must be a number, not string.** |
| `N` | number | **Yes** | Northing coordinate. Numeric value (integer or float). **Must be a number, not string.** |
| `layer` | string | No | DXF layer name. Helps identify point type. E.g., `"NETWORK_JUNCTIONS"`, `"MANHOLE_ACCESS"`, `"WATER_INTAKE"`. |
| `properties` | object | No | Additional metadata as key-value pairs. Can include elevation, material, diameter, etc. No validation applied. |

#### Connections Array
Each connection object must contain:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | **Yes** | Unique identifier for this connection (e.g., `"conn-001"`, `"PIPE-A1-A2"`, `"LINK-N01-N02"`). |
| `fromPointId` | string | **Yes** | **Must reference an existing point ID.** Error thrown if point doesn't exist. |
| `toPointId` | string | **Yes** | **Must reference an existing point ID.** Error thrown if point doesn't exist. |
| `length` | number | No | Distance between points in the coordinate system units. If omitted, automatically calculated using Euclidean distance: `√((E₂-E₁)² + (N₂-N₁)²)` |
| `properties` | object | No | Additional metadata (pipe type, material, slope, etc.). No validation applied. |

---

## Validation Rules & Error Conditions

SiteTrack strictly validates the JSON schema. The import will **fail** if:

### Critical Errors (Import Rejected)
1. **Missing or invalid `schemaVersion`**: Must be `1`. Examples of failures:
   - Missing entirely
   - Value is `"1"` (string instead of number)
   - Value is `2` or any other number
   
2. **Missing or invalid `exportedAt`**: Must be valid ISO 8601 string
   - Invalid format → error: "Missing or invalid 'exportedAt' field"
   
3. **Missing or invalid `sourceFileName`**: Must be non-empty string
   - Empty string or non-string → error: "Missing or invalid 'sourceFileName'"
   
4. **Invalid `coordinateSystemHint`**: Must be exactly `"UTM"`, `"WGS84"`, or `"Local"` (case-sensitive)
   - Example invalid: `"utm"`, `"EPSG:32634"`, `"NAD83"` → error
   
5. **Invalid `points` array**: Must be array, not null/undefined/object
   - Missing or not array → error: "Missing or invalid 'points' array"
   
6. **Invalid `connections` array**: Must be array, not null/undefined/object
   - Missing or not array → error: "Missing or invalid 'connections' array"

### Point Validation Errors
For each point in the points array:
- **Missing `id` or `name`**: Error message: `"Point N: Missing required fields. Each point must have 'id' and 'name'"`
- **Non-numeric E or N**: Error message: `"Point N (id): Invalid coordinates. Expected numeric E and N, got: E=abc, N=xyz"`
  - E and N must be numeric (not strings like `"123.45"`)

### Connection Validation Errors
For each connection in the connections array:
- **Missing `id`**: Error message: `"Connection N: Missing required 'id'"`
- **Invalid `fromPointId`**: Error message: `"Connection conn-X: References unknown fromPointId 'pt-999'"`
  - Must match an existing point id exactly
- **Invalid `toPointId`**: Same as fromPointId validation

---

## Coordinate System Guidance

### UTM (Universal Transverse Mercator)
- **When to use**: Large geographic areas in metric units
- **E values**: "Easting" (meters from zone meridian), typically 200,000–900,000
- **N values**: "Northing" (meters from equator), typically 0–10,000,000
- **Example**: Zone 31N, E=234567.89, N=4567890.12

### WGS84 (GPS/Latitude-Longitude)
- **When to use**: Global coordinates, GPS data
- **E values**: Longitude, range -180 to +180
- **N values**: Latitude, range -90 to +90
- **Example**: E=2.3522 (Paris longitude), N=48.8566 (Paris latitude)

### Local (Project-Local Coordinates)
- **When to use**: Arbitrary local coordinate system with no geographic reference
- **E values**: Arbitrary meters/units from local origin
- **N values**: Arbitrary meters/units from local origin
- **Example**: E=100.0, N=250.5 (relative to site origin)

---

## Layer Filtering for DXF Files

CAD files often contain multiple layers representing different infrastructure elements. When extracting roads and networks, **filter out non-network layers**.

### Recommended Network Layers (Include)
- `SEWER*` (SEWER_LINES, SEWER_GRAVITY, SEWER_FORCE_MAIN)
- `STORMWATER*` (STORMWATER_LINES, STORM_DRAINAGE)
- `WATER*` (WATER_MAINS, WATER_SERVICE)
- `GAS*` (GAS_LINES, GAS_MAIN)
- `ELECTRICAL*` (POWER_LINES, ELECTRICAL_CONDUIT)
- `ROAD*` (ROAD_CENTERLINE, ROAD_NETWORK)
- `STREET*` (STREET_NETWORK, STREET_LINES)
- `PIPELINE*` (PIPELINE, OIL_LINE)
- `UTILITY*` (UTILITY_LINE, UTILITY_NETWORK)
- `NETWORK*` (NETWORK_LINE, INFRASTRUCTURE)
- `JUNCTION*` (JUNCTIONS, NODES, CONNECTION_POINTS)
- `MANHOLE*` (MANHOLE_ACCESS, MANHOLE_LOCATION)

### Layers to Exclude (Ignore)
- `ANNOTATION*`, `LABEL*`, `TEXT*` - Text labels
- `DIMENSION*`, `HATCH*` - Drawing annotations
- `CONSTRUCTION*`, `DEFPOINT*` - Construction geometry
- `XREF*`, `BLOCK*`, `LAYER_FILTERS*` - CAD structure
- `BUILDING*`, `STRUCTURE*`, `FOUNDATION*` - Building elements
- `PAVEMENT*`, `ASPHALT*`, `CURB*` - Road surface (not network topology)
- `PROPERTY*`, `PARCEL*`, `BOUNDARY*` - Legal boundaries
- `VEGETATION*`, `LANDSCAPE*`, `CONTOUR*` - Natural features
- `SURVEY*`, `TOPO*` - Survey marks and topography
- `0` (Default layer) - Usually contains misc. elements

### Filtering Strategy
1. When parsing DXF, track the layer of each element
2. Only include entities from whitelisted network layers
3. For entities without explicit layers, skip them or prompt user
4. Store the layer name in the `layer` field of each point for traceability

---

## Data Mapping Examples

### Example 1: Sewer Network from DXF

**DXF Input (conceptual):**
- POINT entity at (234567.89, 4567890.12) on layer MANHOLE_ACCESS, name "MH-001"
- POINT entity at (234600.00, 4567950.00) on layer MANHOLE_ACCESS, name "MH-002"
- LINE entity from first to second point on layer SEWER_LINES

**Output JSON:**
```json
{
  "schemaVersion": 1,
  "exportedAt": "2025-05-03T14:30:45.123Z",
  "sourceFileName": "Sewer_Network.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "pt-001",
      "name": "MH-001",
      "E": 234567.89,
      "N": 4567890.12,
      "layer": "MANHOLE_ACCESS",
      "properties": {}
    },
    {
      "id": "pt-002",
      "name": "MH-002",
      "E": 234600.00,
      "N": 4567950.00,
      "layer": "MANHOLE_ACCESS",
      "properties": {}
    }
  ],
  "connections": [
    {
      "id": "conn-001",
      "fromPointId": "pt-001",
      "toPointId": "pt-002",
      "length": 64.23,
      "properties": {
        "type": "sewer_gravity",
        "material": "PVC"
      }
    }
  ]
}
```

### Example 2: Water Network with Properties

**Output JSON:**
```json
{
  "schemaVersion": 1,
  "exportedAt": "2025-05-03T15:45:00.000Z",
  "sourceFileName": "Water_Distribution_Network.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "pump-station-1",
      "name": "Main Pump Station",
      "E": 450000.0,
      "N": 5500000.0,
      "layer": "WATER_INTAKE",
      "properties": {
        "type": "pump_station",
        "capacity_gallons_per_minute": 5000,
        "elevation_feet": 145.5
      }
    },
    {
      "id": "tank-1",
      "name": "Storage Tank A",
      "E": 450500.0,
      "N": 5500100.0,
      "layer": "WATER_STORAGE",
      "properties": {
        "capacity_gallons": 500000,
        "material": "concrete"
      }
    }
  ],
  "connections": [
    {
      "id": "main-line-1",
      "fromPointId": "pump-station-1",
      "toPointId": "tank-1",
      "properties": {
        "pipe_diameter_inches": 24,
        "pipe_material": "cast_iron"
      }
    }
  ]
}
```

### Example 3: GPS Coordinates (WGS84)

```json
{
  "schemaVersion": 1,
  "exportedAt": "2025-05-03T12:00:00Z",
  "sourceFileName": "GPS_Survey.dxf",
  "coordinateSystemHint": "WGS84",
  "points": [
    {
      "id": "gps-001",
      "name": "North Survey Marker",
      "E": -122.4194,
      "N": 37.7749,
      "properties": {}
    },
    {
      "id": "gps-002",
      "name": "South Survey Marker",
      "E": -122.4180,
      "N": 37.7730,
      "properties": {}
    }
  ],
  "connections": []
}
```

---

## Implementation Checklist for DXF Tool

- [ ] Parse DXF file and extract entities (POINT, LINE, POLYLINE, ARC, CIRCLE)
- [ ] Filter entities by layer name (include only network layers)
- [ ] For each network point entity:
  - [ ] Generate unique id (e.g., `pt-001`, `pt-002`, etc.)
  - [ ] Extract name from DXF entity name or attribute
  - [ ] Extract X, Y coordinates as numbers (not strings)
  - [ ] Store original layer name
  - [ ] Extract optional properties from DXF attributes
- [ ] For each network connection (LINE, POLYLINE segments):
  - [ ] Generate unique id (e.g., `conn-001`, `conn-002`, etc.)
  - [ ] Map start point to fromPointId
  - [ ] Map end point to toPointId
  - [ ] Calculate length if not already available
  - [ ] Extract optional properties from DXF entity
- [ ] Detect coordinate system (prompt user if ambiguous):
  - [ ] Check coordinate ranges (WGS84: -180 to +180 for E, -90 to +90 for N)
  - [ ] Check for UTM zone information in DXF metadata
  - [ ] Default to "Local" if unable to determine
- [ ] Generate JSON with:
  - [ ] schemaVersion: 1 (always)
  - [ ] exportedAt: current timestamp (ISO 8601)
  - [ ] sourceFileName: original DXF filename
  - [ ] coordinateSystemHint: detected coordinate system
  - [ ] points array: all extracted network points
  - [ ] connections array: all extracted network connections
- [ ] Validate before export:
  - [ ] All points have valid numeric E and N
  - [ ] All connection fromPointId/toPointId exist
  - [ ] No duplicate point or connection IDs
- [ ] Export as connection.json text file

---

## Integration Steps in SiteTrack UI

1. User navigates to **Projects > Networks > Import Infrastructure**
2. User selects **"Import from DXF"** and uploads DXF file or connection.json
3. System detects file type (JSON vs. DXF)
4. If DXF: Extracts using SiteTrackDxfExtractor tool
5. If JSON: Validates against schema
6. Displays preview: "X points, Y connections from layer SEWER_LINES"
7. User confirms and clicks "Import"
8. Server imports into Firestore:
   - Creates `projects/{projectId}/networks/{networkId}` document
   - Creates `projects/{projectId}/networks/{networkId}/points/{pointId}` documents
   - Creates `projects/{projectId}/networks/{networkId}/connections/{connId}` documents
9. Success message: "Imported X points and Y connections from [filename]"

---

## Error Messages & Troubleshooting

### Common User Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `Invalid schemaVersion: 2` | Wrong version in JSON | Ensure schemaVersion is exactly `1` |
| `References unknown fromPointId "pt-999"` | Connection point doesn't exist | Verify all fromPointId/toPointId match point ids |
| `Invalid coordinates. Expected numeric E and N, got: E=abc, N=xyz` | Coordinates as strings or text | Ensure E and N are numeric values, not strings |
| `Missing or invalid 'coordinateSystemHint'` | Invalid coordinate system | Use exactly `"UTM"`, `"WGS84"`, or `"Local"` |
| `Invalid coordinates at index 2: E and N must be numbers` | Type error in points array | Check data type of all E and N values |
| `No valid points found in CSV` | Empty points array or all rows invalid | Verify points array has at least one valid point |

### DXF Layer Issues

| Issue | Solution |
|-------|----------|
| No networks imported | Check layer names - they may not match network layer filter |
| Too many extra points | Some non-network layers were included - refine layer filter |
| Missing connections | Connection entities (LINEs) may be on different layer than points |
| Duplicate IDs | DXF entities need unique identifiers - ensure ID generation is unique per file |

---

## Testing Connection.json Validity

Use this API endpoint to validate a JSON file before importing:

```bash
curl -X POST http://localhost:3000/api/network/validate \
  -H "Content-Type: application/json" \
  -d @connection.json
```

Expected response on success:
```json
{
  "valid": true,
  "pointsCount": 45,
  "connectionsCount": 52,
  "sourceFileName": "Infrastructure_Network.dxf",
  "coordinateSystem": "UTM"
}
```

Expected response on validation error:
```json
{
  "valid": false,
  "pointsCount": 0,
  "connectionsCount": 0,
  "sourceFileName": "",
  "coordinateSystem": "",
  "errors": [
    "Point 5 (pt-005): Invalid coordinates. Expected numeric E and N, got: E=abc, N=123"
  ]
}
```

---

## Contact & Support

For questions about SiteTrack's expected format:
- Review `src/lib/import-connection.ts` for validation logic
- Check `src/lib/network-import-handlers.ts` for alternative import formats
- Contact: [SiteTrack project maintainer]

