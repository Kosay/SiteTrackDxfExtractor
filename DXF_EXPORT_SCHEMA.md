
# DXF Export Schema - Quick Reference

For SiteTrackDxfExtractor: Output format required by SiteTrack.

## Minimal Valid Example

```json
{
  "schemaVersion": 1,
  "exportedAt": "2025-05-03T14:30:45.123Z",
  "sourceFileName": "roads.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "pt-1",
      "name": "Junction A",
      "E": 234567.89,
      "N": 4567890.12
    }
  ],
  "connections": []
}
```

## Required Fields

| Field | Type | Exact Value / Rules |
|-------|------|---|
| `schemaVersion` | number | **Always `1`** (not string) |
| `exportedAt` | string | ISO 8601: `2025-05-03T14:30:45.123Z` |
| `sourceFileName` | string | Original DXF filename |
| `coordinateSystemHint` | string | One of: `"UTM"`, `"WGS84"`, `"Local"` |
| `points[].id` | string | Unique per file (e.g., `pt-1`, `J-001`, `NODE-A`) |
| `points[].name` | string | Human-readable name |
| `points[].E` | **number** | NOT a string. X or Easting coordinate. |
| `points[].N` | **number** | NOT a string. Y or Northing coordinate. |
| `connections[].id` | string | Unique identifier |
| `connections[].fromPointId` | string | Must match existing point id |
| `connections[].toPointId` | string | Must match existing point id |

## Optional Fields

```json
{
  "points": [
    {
      "id": "pt-1",
      "name": "MH-001",
      "E": 234567.89,
      "N": 4567890.12,
      "layer": "MANHOLE_ACCESS",
      "properties": {
        "elevation": 125.5,
        "material": "concrete"
      }
    }
  ],
  "connections": [
    {
      "id": "conn-1",
      "fromPointId": "pt-1",
      "toPointId": "pt-2",
      "length": 145.67,
      "properties": {
        "diameter": "300mm",
        "type": "gravity_sewer"
      }
    }
  ]
}
```

## Validation Rules (SiteTrack Enforces)

- `schemaVersion` must be exactly `1` ✓
- `coordinateSystemHint` must be `"UTM"`, `"WGS84"`, or `"Local"` ✓
- E and N must be numbers, not strings ✓
- All `fromPointId` and `toPointId` must reference existing point ids ✓
- points array must have at least 1 point ✓
- connections array can be empty ✓

## Implementation Tips

1. **IDs**: Keep them simple and unique within file (e.g., `pt-001`, `pt-002`, ...)
2. **Names**: Extract from DXF attribute or entity name
3. **E/N**: Ensure output as `parseFloat()` result, not string
4. **Layer Filtering**: Only include points/connections from network layers
   - Include: `SEWER*`, `WATER*`, `STORMWATER*`, `MANHOLE*`, `JUNCTION*`, etc.
   - Exclude: `ANNOTATION*`, `HATCH*`, `BUILDING*`, `PROPERTY*`, etc.
5. **Timestamp**: Use `new Date().toISOString()`
6. **Coordinate System**: Detect from DXF metadata or prompt user

## Example from Road Network

```json
{
  "schemaVersion": 1,
  "exportedAt": "2025-05-03T15:00:00.000Z",
  "sourceFileName": "City_Roads_2025.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "intersection-1",
      "name": "Main St & Oak Ave",
      "E": 450000.0,
      "N": 5500000.0,
      "layer": "ROAD_INTERSECTIONS"
    },
    {
      "id": "intersection-2",
      "name": "Main St & Elm Ave",
      "E": 450150.0,
      "N": 5500100.0,
      "layer": "ROAD_INTERSECTIONS"
    }
  ],
  "connections": [
    {
      "id": "road-segment-1",
      "fromPointId": "intersection-1",
      "toPointId": "intersection-2",
      "length": 158.11,
      "properties": {
        "road_name": "Main Street",
        "speed_limit_mph": 35
      }
    }
  ]
}
```

## File Output

Save as `connection.json` (or `{filename}_network.json`)

Then upload to SiteTrack admin panel:
- Projects > Networks > Import Infrastructure > Select connection.json

## Validation Endpoint (Optional Pre-Check)

```bash
curl -X POST http://sitetrack.local/api/network/validate-json \
  -H "Content-Type: application/json" \
  -d @connection.json
```

Response: `{ valid: true, pointsCount: 45, connectionsCount: 52, ... }`

---

**Reference**: Full validation logic in `src/lib/import-connection.ts`

