# DXF Export Schema - Quick Reference

Minimal guide for exporting DXF files as JSON for SiteTrack network import.

---

## Minimal Valid Example

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T10:30:00Z",
  "sourceFileName": "network.dxf",
  "coordinateSystemHint": "UTM",
  "points": [
    {"id": "pt-1", "name": "Junction A", "E": 234567.89, "N": 4567890.12},
    {"id": "pt-2", "name": "Junction B", "E": 234600.00, "N": 4567950.00}
  ],
  "connections": [
    {"id": "conn-1", "fromPointId": "pt-1", "toPointId": "pt-2", "length": 75.5}
  ]
}
```

---

## Validation Rules

| Rule | Requirement | Impact |
|---|---|---|
| schemaVersion | Must be `1` | Import fails if not 1 |
| E, N | Numeric only (not strings) | Import fails if strings |
| points array | At least 1 point | Import fails if empty |
| fromPointId | Must reference existing point | Import fails if not found |
| toPointId | Must reference existing point | Import fails if not found |
| Unique IDs | No duplicate point or connection IDs | Import fails if duplicates |

---

## Implementation Tips

1. **Coordinates**: Use at least 2 decimals for UTM/Local, 5-8 for WGS84
2. **Names**: Keep human-readable and descriptive (e.g., "MH-001", "Junction A")
3. **Metadata**: Use `properties` object for optional fields (slope, diameter, etc.)
4. **Dates**: Always use ISO-8601 with timezone (e.g., `"2026-05-03T10:30:00Z"`)
5. **Layer Names**: Include original DXF layer names in the `layer` field for traceability

---

## Real-World Example: Road Network

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T14:00:00Z",
  "sourceFileName": "city_roads.dxf",
  "coordinateSystemHint": "WGS84",
  "points": [
    {
      "id": "RJ-001",
      "name": "Main St & 5th Ave",
      "E": 35.123456,
      "N": 31.987654,
      "layer": "ROAD_INTERSECTION",
      "properties": {"type": "intersection"}
    },
    {
      "id": "RJ-002",
      "name": "Main St & 6th Ave",
      "E": 35.124500,
      "N": 31.988000,
      "layer": "ROAD_INTERSECTION",
      "properties": {"type": "intersection"}
    },
    {
      "id": "RJ-003",
      "name": "5th Ave & Park St",
      "E": 35.123500,
      "N": 31.989000,
      "layer": "ROAD_INTERSECTION",
      "properties": {"type": "intersection"}
    }
  ],
  "connections": [
    {
      "id": "ROAD-001",
      "fromPointId": "RJ-001",
      "toPointId": "RJ-002",
      "length": 123.5,
      "properties": {
        "name": "Main Street",
        "width": 12.0,
        "surface": "asphalt",
        "lanes": 2
      }
    },
    {
      "id": "ROAD-002",
      "fromPointId": "RJ-001",
      "toPointId": "RJ-003",
      "length": 98.3,
      "properties": {
        "name": "5th Avenue",
        "width": 10.0,
        "surface": "asphalt",
        "lanes": 2
      }
    }
  ]
}
```

---

## File Output Instructions

When exporting from the DXF tool:

1. **File naming**: Use pattern `<dxf-name>_network.json`
   - Example: `sewer_network.dxf` → `sewer_network_network.json`

2. **Encoding**: UTF-8 without BOM

3. **Formatting**: Pretty-printed JSON (2-space indentation) for readability

4. **Character escaping**: 
   - Quotes in strings: `\"` 
   - Newlines: `\n`
   - Backslashes: `\\`

5. **Validation**: Before writing, ensure:
   - All coordinates are valid numbers
   - All point references are valid
   - No duplicate IDs
   - JSON syntax is correct

---

## Common Fields Reference

**Points:**
- `id`: Unique identifier (required)
- `name`: Human-readable name (required)
- `E`: Easting/Longitude (required, numeric)
- `N`: Northing/Latitude (required, numeric)
- `layer`: DXF source layer (optional)
- `properties`: Additional metadata as object (optional)

**Connections:**
- `id`: Unique identifier (required)
- `fromPointId`: Start point reference (required)
- `toPointId`: End point reference (required)
- `length`: Distance in meters/degrees (optional)
- `properties`: Metadata object with optional fields:
  - `slope`: Decimal percentage
  - `diameter`: Numeric (usually mm)
  - `material`: String (e.g., "PVC", "concrete")
  - Any other custom fields

---

## Troubleshooting

| Issue | Check |
|---|---|
| Import fails immediately | Validate JSON syntax using https://jsonlint.com |
| "Invalid schemaVersion" | Ensure `"schemaVersion": 1` (integer) |
| "Coordinate must be numeric" | Remove quotes: `"E": 234567.89` not `"E": "234567.89"` |
| "Point not found" | Verify all `fromPointId` and `toPointId` reference existing point `id` |
| "Duplicate ID" | Check point and connection `id` fields for duplicates |

---

**For detailed guidance**, see [DXF_INTEGRATION_GUIDE.md](../DXF_INTEGRATION_GUIDE.md)
