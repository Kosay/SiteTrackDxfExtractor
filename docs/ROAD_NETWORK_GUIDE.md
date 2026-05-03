# How to Use DXF Extractor with Multiple Road Layers

## Your Scenario: Road Network with Multiple Layers

When you have a road DXF with layers like:
- `CENTRE_ROAD` (centerline)
- `KERBSTONE_LEFT` (left edge)
- `KERBSTONE_RIGHT` (right edge)
- `LEFT_SIDE` (left shoulder)
- `RIGHT_SIDE` (right shoulder)
- `ROAD_INTERSECTIONS` (junctions)
- `ROAD_ANNOTATIONS` (text labels)

**You don't need to merge them!** The tool can work with all of them.

---

## Strategy: Combine Multiple Layers with Same Role

### Option 1: Use Centre Road Only (Simplest)

If you want to extract just the main road network:

1. **Open the DXF file** in the tool
2. **Assign layer roles**:
   - `CENTRE_ROAD` → set role to **Pipe**
   - `ROAD_INTERSECTIONS` → set role to **Node**
   - `ROAD_ANNOTATIONS` → set role to **Label**
   - All other road layers → leave as **None** (they'll be ignored)

3. **Export for SiteTrack**
   - Result: A network of road segments + intersections

**Pros**: Clean, simple network
**Cons**: Misses detail about road width/edges

### Option 2: Include Multiple Road Geometry Layers (Recommended)

To capture the full road geometry (width, kerbs):

1. **Assign Node role**:
   - `ROAD_INTERSECTIONS` → **Node**

2. **Assign Pipe role to ALL road geometry layers**:
   - `CENTRE_ROAD` → **Pipe**
   - `KERBSTONE_LEFT` → **Pipe**
   - `KERBSTONE_RIGHT` → **Pipe**
   - `LEFT_SIDE` → **Pipe** (optional)
   - `RIGHT_SIDE` → **Pipe** (optional)

3. **Assign Label role**:
   - `ROAD_ANNOTATIONS` → **Label**

4. **Export for SiteTrack**
   - Result: Multiple connections per intersection (one for each road layer)
   - SiteTrack will create separate links for center, left kerb, right kerb, etc.

**Pros**: Captures complete road geometry
**Cons**: More complex network (more connections)

---

## How the Tool Processes Multiple Layers with Same Role

### When you assign multiple layers to "Pipe" role:

```
DXF File:
├── CENTRE_ROAD (Polyline2D)
├── KERBSTONE_LEFT (Polyline2D)
├── KERBSTONE_RIGHT (Polyline2D)
├── ROAD_INTERSECTIONS (Insert blocks)
└── (other layers)

↓ [Assign Roles] ↓

Layer Assignments:
├── CENTRE_ROAD → Pipe ✓
├── KERBSTONE_LEFT → Pipe ✓
├── KERBSTONE_RIGHT → Pipe ✓
├── ROAD_INTERSECTIONS → Node ✓
└── (others) → None (ignored)

↓ [Export] ↓

Result JSON:
{
  "points": [
    {"id": "J-001", "name": "Intersection 1", ...},
    {"id": "J-002", "name": "Intersection 2", ...}
  ],
  "connections": [
    {"id": "CONN-1", "fromPointId": "J-001", "toPointId": "J-002", "name": "CENTRE_ROAD"},
    {"id": "CONN-2", "fromPointId": "J-001", "toPointId": "J-002", "name": "KERBSTONE_LEFT"},
    {"id": "CONN-3", "fromPointId": "J-001", "toPointId": "J-002", "name": "KERBSTONE_RIGHT"}
  ]
}
```

**Each polyline becomes a separate connection!** This is exactly what you want.

---

## Step-by-Step: Extract Your Road Network

### Step 1: Open DXF File
```
1. Run DxfCoordinateExtractor.exe
2. Click "Open DXF File" or drag your road network DXF
3. All layers appear in the main grid
```

### Step 2: Review Layers
```
Use Layer dropdown to filter by layer:
- Filter by "CENTRE_ROAD" → see all centerline segments
- Filter by "KERBSTONE_LEFT" → see all left kerbs
- Filter by "ROAD_INTERSECTIONS" → see all junctions
```

### Step 3: Assign Roles in the Right Panel

In the **Layer Roles** section (right side):

```
Layer List:
├── CENTRE_ROAD ▼                    [Dropdown: None | Node | Pipe | Label]
│   → Select "Pipe"
├── KERBSTONE_LEFT ▼
│   → Select "Pipe"
├── KERBSTONE_RIGHT ▼
│   → Select "Pipe"
├── LEFT_SIDE ▼
│   → Select "None" (or Pipe if you want it)
├── RIGHT_SIDE ▼
│   → Select "None" (or Pipe if you want it)
├── ROAD_INTERSECTIONS ▼
│   → Select "Node"
├── ROAD_ANNOTATIONS ▼
│   → Select "Label"
└── (other layers) ▼
    → Leave as "None"
```

### Step 4: Adjust Settings (if needed)

```
Snap Tolerance: 2.0 m
  → How far a polyline endpoint can be from a node to connect
  → For road networks: 2-5 m is typical

Text Search Radius: 5.0 m
  → How far to search for nearby road name text
  → For road networks: 5-10 m is typical
```

### Step 5: Click "Export for SiteTrack"

```
Choose save location: C:\exports\roads
Three files are created:
  ✓ roads_nodes.csv (intersections)
  ✓ roads_connections.csv (all road segments)
  ✓ roads_network.json (SiteTrack import file)
```

### Step 6: Import into SiteTrack

```
In SiteTrack Admin:
1. Projects → Create New Project
2. Upload roads_network.json
3. SiteTrack validates and displays the network
4. Confirm to create project
```

---

## What Gets Extracted

### Nodes (Intersections)
From `ROAD_INTERSECTIONS` layer (Insert blocks):
```
J-001: Intersection of Main St & 5th Ave (E: 35.123, N: 31.987)
J-002: Intersection of Main St & 6th Ave (E: 35.124, N: 31.988)
```

### Connections (Road Segments)
From all Pipe layers:
```
CENTRE-001: J-001 → J-002 (CENTRE_ROAD, length: 123.5 m)
LEFT_KERB-001: J-001 → J-002 (KERBSTONE_LEFT, length: 125.2 m)
RIGHT_KERB-001: J-001 → J-002 (KERBSTONE_RIGHT, length: 124.8 m)
```

---

## Example: Complete Road Network Export

### Input DXF:
```
CENTRE_ROAD layer:
  Polyline: (35.123, 31.987) → (35.124, 31.988) → (35.125, 31.989)

KERBSTONE_LEFT layer:
  Polyline: (35.123, 31.990) → (35.124, 31.991) → (35.125, 31.992)

KERBSTONE_RIGHT layer:
  Polyline: (35.123, 31.985) → (35.124, 31.986) → (35.125, 31.987)

ROAD_INTERSECTIONS layer:
  Insert block at (35.123, 31.987) → "J-001"
  Insert block at (35.124, 31.988) → "J-002"
  Insert block at (35.125, 31.989) → "J-003"

ROAD_ANNOTATIONS layer:
  Text "Main Street" at (35.124, 31.990)
```

### Output JSON:
```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T10:30:00Z",
  "sourceFileName": "road_network.dxf",
  "coordinateSystemHint": "WGS84",
  "points": [
    {
      "id": "J-001",
      "name": "J-001",
      "E": 35.123,
      "N": 31.987,
      "layer": "ROAD_INTERSECTIONS",
      "properties": {}
    },
    {
      "id": "J-002",
      "name": "J-002",
      "E": 35.124,
      "N": 31.988,
      "layer": "ROAD_INTERSECTIONS",
      "properties": {}
    },
    {
      "id": "J-003",
      "name": "J-003",
      "E": 35.125,
      "N": 31.989,
      "layer": "ROAD_INTERSECTIONS",
      "properties": {}
    }
  ],
  "connections": [
    {
      "id": "CENTRE-1-2",
      "fromPointId": "J-001",
      "toPointId": "J-002",
      "length": 123.45,
      "properties": {"layer": "CENTRE_ROAD"}
    },
    {
      "id": "CENTRE-2-3",
      "fromPointId": "J-002",
      "toPointId": "J-003",
      "length": 118.90,
      "properties": {"layer": "CENTRE_ROAD"}
    },
    {
      "id": "LEFT_KERB-1-2",
      "fromPointId": "J-001",
      "toPointId": "J-002",
      "length": 125.20,
      "properties": {"layer": "KERBSTONE_LEFT"}
    },
    {
      "id": "LEFT_KERB-2-3",
      "fromPointId": "J-002",
      "toPointId": "J-003",
      "length": 120.50,
      "properties": {"layer": "KERBSTONE_LEFT"}
    },
    {
      "id": "RIGHT_KERB-1-2",
      "fromPointId": "J-001",
      "toPointId": "J-002",
      "length": 124.80,
      "properties": {"layer": "KERBSTONE_RIGHT"}
    },
    {
      "id": "RIGHT_KERB-2-3",
      "fromPointId": "J-002",
      "toPointId": "J-003",
      "length": 119.70,
      "properties": {"layer": "KERBSTONE_RIGHT"}
    }
  ]
}
```

---

## Common Questions

### Q: Why do I get multiple connections between the same junctions?
**A:** Because you assigned multiple layers to "Pipe" role. Each polyline becomes a separate connection. This is correct for representing road geometry (center, left kerb, right kerb).

### Q: What if I only want the centerline?
**A:** 
1. Assign only `CENTRE_ROAD` layer to "Pipe" role
2. Assign other road layers to "None"
3. Export will only include centerline connections

### Q: What if my road intersections are not marked as blocks?
**A:**
Option 1: Mark them in CAD (create Insert blocks at intersections)
Option 2: Use a text label layer and set role to "Label" — the tool will snap polyline endpoints to nearby labels

### Q: Can I combine layers before export?
**A:** Yes, but not necessary. The tool handles multiple layers automatically. You can merge in CAD if you prefer, but it's extra work.

### Q: What about road names, widths, speed limits?
**A:** 
- Names: Use Label layer with text near the road
- Width/properties: Add to properties object in JSON after export (or extend the tool)

---

## Best Practice Workflow

### For Maximum Fidelity:
```
1. Keep all road geometry layers separate in DXF
2. Include all layers in export:
   - CENTRE_ROAD → Pipe
   - KERBSTONE_LEFT → Pipe
   - KERBSTONE_RIGHT → Pipe
3. Result: SiteTrack network shows full road geometry
4. Each road is represented by multiple parallel connections
```

### For Simplified Network:
```
1. Only use centerline layer:
   - CENTRE_ROAD → Pipe
2. Set all other layers to None
3. Result: SiteTrack network shows just the road centerline
4. Cleaner, easier to work with
```

### For Mixed Approach:
```
1. Main geometry only:
   - CENTRE_ROAD → Pipe
   - KERBSTONE_LEFT → Pipe
   - KERBSTONE_RIGHT → Pipe
   - LEFT_SIDE, RIGHT_SIDE → None
2. Balance between detail and complexity
```

---

## Summary

**You DO NOT need to merge layers.**

- ✅ Assign multiple layers the **same role**
- ✅ Each layer becomes a separate connection in the network
- ✅ Tool automatically handles topology for all layers
- ✅ Result: Complete road geometry in SiteTrack

Simply assign roles to each layer and export — the tool does the rest!
