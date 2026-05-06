# AutoCAD Bridge Setup & Testing Guide

Complete instructions for setting up and testing the AutoCAD Bridge app.

---

## What is AutoCAD Bridge?

A WinForms helper app that:
1. **Listens** for data from AutoCAD via Named Pipes
2. **Receives** selected object data (points, lines, curves, texts)
3. **Displays** extracted data in a grid
4. **Exports** as JSON for SiteTrack import

### Workflow
```
AutoCAD (select objects + getData) 
    ↓ [Named Pipe IPC]
WinForms App (receives + displays data)
    ↓ [Click Export]
JSON File (points/lines/curves/texts)
    ↓ [Upload to SiteTrack]
SiteTrack (import as network)
```

---

## Part 1: Setup

### Prerequisites
- AutoCAD 2007 or later
- Windows 10 or later (for Named Pipes support)
- .NET Framework 4.8

### Step 1: Get the LISP Script

The `GetData.lsp` file is included in the project:
- **Location**: `GetData.lsp` (in project root)
- **Copy to**: A folder where you keep AutoCAD LISP files

Recommended locations:
```
C:\Users\[YourName]\Documents\AutoCAD\LISP
C:\Program Files\Autodesk\AutoCAD 2024\Support
```

### Step 2: Load LISP in AutoCAD

#### Method 1: Manual Load (Each Session)
```
1. Open AutoCAD
2. Open DXF file
3. Type in command line: (load "C:\\path\\to\\GetData.lsp")
4. Press Enter
5. You should see: "✓ GetData.lsp loaded successfully!"
```

#### Method 2: Auto-Load (Every Session)
```
1. In AutoCAD, type: appload
2. Click "Contents..."
3. Click "Add..."
4. Browse to GetData.lsp
5. Click "OK"
6. Make sure it's checked
7. Click "Close"
8. Click "OK"
9. LISP will auto-load on next AutoCAD start
```

#### Method 3: Add to acaddoc.lsp (Permanent)
```
1. Find acaddoc.lsp:
   C:\Users\[YourName]\AppData\Roaming\Autodesk\AutoCAD [version]\R[version]\enu\Support

2. Open with Notepad
3. Add at the end:
   (load "C:\\path\\to\\GetData.lsp")

4. Save and restart AutoCAD
```

### Step 3: Start the Helper App

```
1. Run DxfCoordinateExtractor.exe
2. App starts listening on named pipe: "SiteTrackDxfBridgePipe"
3. Status bar shows: "Listening for AutoCAD data..."
4. Keep app running while using AutoCAD
```

---

## Part 2: Basic Workflow

### Step 1: Open DXF in AutoCAD

```
1. Start AutoCAD
2. Open a DXF file (File → Open)
3. Make sure it has objects to extract:
   - Points (blocks at manhole/junction locations)
   - Lines/Polylines (pipes, roads)
   - Arcs/Circles (curves)
   - Text/MText (labels)
```

### Step 2: Start Helper App

```
1. Run DxfCoordinateExtractor.exe
2. App listens for AutoCAD on background thread
3. Keep app visible (even if minimized)
```

### Step 3: Select Objects in AutoCAD

```
In AutoCAD command line:

Option A - Select all objects:
  Command: > select all
  > getData

Option B - Window select objects:
  Command: > w
  (draw window around objects to extract)
  > getData

Option C - Pick individual objects:
  Command: > single click objects
  (hold Shift to multi-select)
  > getData
```

### Step 4: App Receives Data

When you run `getData` in AutoCAD:
```
AutoCAD prints:
  >>> SiteTrack Bridge - Getting Data from Selection...
  Processing 15 objects...
  ✓ Points: 5
  ✓ Lines: 8
  ✓ Curves: 2
  ✓ Total: 15
  ✓ Data sent to SiteTrack Bridge App!

Helper App displays:
  ✓ Received from AutoCAD: 5 points, 8 lines, 2 curves, 0 texts
```

### Step 5: Review Data in App

The app's grid shows all extracted objects:
```
Grid Columns:
  ✓  Type   | Role  | Layout   | Layer           | Label / Description
  └─ POINT  | None  | AutoCAD  | MANHOLE         | MH-001 (335684.045, 2678151.885)
  └─ LINE   | None  | AutoCAD  | SEWER_MAIN      | Line (93.97m)
  └─ ARC    | None  | AutoCAD  | CURVE_LAYER     | Arc (R:50.0)
  └─ TEXT   | None  | AutoCAD  | ANNOTATION      | "MH-001"
```

### Step 6: Export as JSON

```
1. In Helper App, click "Export AutoCAD Data" button (blue)
2. Save dialog appears
3. Choose location and filename (e.g., "sewer_network.json")
4. Click "Save"
5. File is exported with structure:
   {
     "points": [...],
     "lines": [...],
     "curves": [...],
     "texts": [...]
   }
```

---

## Part 3: Testing

### Test 1: LISP Script Loading

**Objective**: Verify LISP script loads correctly

**Steps**:
```
1. Open AutoCAD
2. Type in command line: (load "GetData.lsp")
3. Expected output:
   "✓ GetData.lsp loaded successfully!
    Run 'getData' command to extract selected objects."
```

**Pass Criteria**: Script loads without errors

---

### Test 2: Object Selection & Extraction

**Objective**: Extract objects from AutoCAD selection

**Prepare**:
```
Create a test DXF with:
- 3 Points (blocks at different coordinates)
- 2 Lines (from point to point)
- 1 Arc/Circle (curve geometry)
- 2 Text labels (annotations)
```

**Steps**:
```
1. Open test DXF in AutoCAD
2. Start Helper App (listening)
3. Select all objects in AutoCAD:
   Command: > a (for all)
4. Run: getData
5. Check Helper App grid
```

**Pass Criteria**:
- Helper App receives data
- Grid shows 3 points, 2 lines, 1 curve, 2 texts
- Status bar displays: "✓ Received from AutoCAD: 3 points, 2 lines, 1 curve, 2 texts"

---

### Test 3: Data Accuracy

**Objective**: Verify extracted coordinates match DXF

**Setup**:
```
1. Create simple line in AutoCAD:
   Start point: (0, 0)
   End point: (100, 100)

2. Add text label:
   Text: "TEST"
   Position: (50, 50)
```

**Steps**:
```
1. Select objects + run getData
2. Check grid for:
   - Start coordinates (0, 0)
   - End coordinates (100, 100)
   - Text content ("TEST")
   - Text position (50, 50)
```

**Pass Criteria**:
- Coordinates match DXF exactly (within rounding)
- Text content and position correct

---

### Test 4: JSON Export

**Objective**: Export data and verify JSON format

**Steps**:
```
1. Receive data from AutoCAD (Test 2)
2. Click "Export AutoCAD Data" button
3. Save as "test_export.json"
4. Open file in text editor
5. Verify structure:
```

**Expected JSON**:
```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T14:30:00Z",
  "source": "AutoCAD",
  "coordinateSystemHint": "UTM",
  "points": [
    {
      "id": "pt-1",
      "name": "pt-1",
      "E": 335684.045,
      "N": 2678151.885,
      "layer": "MANHOLE",
      "properties": {}
    }
  ],
  "lines": [
    {
      "id": "line-1",
      "name": "line-1",
      "startE": 335684.045,
      "startN": 2678151.885,
      "endE": 335750.000,
      "endN": 2678200.000,
      "length": 93.970,
      "layer": "SEWER",
      "properties": {}
    }
  ],
  "curves": [],
  "texts": []
}
```

**Pass Criteria**:
- JSON parses without errors (validate at jsonlint.com)
- schemaVersion = 1
- All coordinates are numeric (not strings)
- Sections exist: points, lines, curves, texts

---

### Test 5: End-to-End Workflow

**Objective**: Complete workflow from AutoCAD to SiteTrack import

**Steps**:
```
1. Open sewer network DXF in AutoCAD
2. Start Helper App
3. Select manholes, pipes, and labels
4. Run getData command
5. App receives and displays data
6. Click Export button
7. Save as "sewer_network.json"
8. Validate JSON (jsonlint.com)
9. Upload to SiteTrack
10. SiteTrack imports as network
11. Verify network appears in SiteTrack UI
```

**Pass Criteria**:
- All 5 previous tests pass
- SiteTrack successfully imports JSON
- Network visualization appears in SiteTrack

---

## Part 4: Troubleshooting

### Problem: "No objects selected" in AutoCAD

**Cause**: Nothing selected when running getData

**Solution**:
```
1. Select objects first (click in viewport or use "a" for all)
2. Then run: getData
```

---

### Problem: Helper App doesn't receive data

**Cause**: Named pipe connection failed or app not running

**Solution**:
```
1. Verify app is running and visible
2. Check status bar shows "Listening for AutoCAD data..."
3. Restart app if needed
4. Make sure AutoCAD is same user account as app
5. Check Windows Firewall isn't blocking Named Pipes
```

---

### Problem: "JSON Parse Error" in Helper App

**Cause**: LISP script produced invalid JSON

**Solution**:
```
1. Check AutoCAD command output for errors
2. Reload LISP: (load "GetData.lsp")
3. Try with fewer objects (simpler test)
4. Check for special characters in text that might break JSON
```

---

### Problem: Coordinates don't match DXF

**Cause**: Coordinate system mismatch or rounding

**Solution**:
```
1. Verify DXF coordinate system (UTM, WGS84, Local)
2. App uses 6 decimals for E/N (sufficient for any system)
3. Small rounding differences (<0.01m) are normal
```

---

### Problem: "Export AutoCAD Data" button grayed out

**Cause**: No data received from AutoCAD yet

**Solution**:
```
1. Follow workflow steps: AutoCAD → getData → receive data
2. Try with test DXF if unsure
3. Check AutoCAD command output for errors
```

---

## Part 5: Sewer Network Example

Complete example using actual sewer network.

### DXF Setup

**Layers**:
```
MANHOLE_ACCESS      - Insert blocks at manhole locations
SEWER_MAIN          - Polylines representing pipes
SEWER_LABEL         - Text labels with "L=...m S=...%" format
```

**Objects**:
```
3 Manholes (MANHOLE_ACCESS):
  - MH-001 at (335684.045, 2678151.885)
  - MH-002 at (335750.000, 2678200.000)
  - MH-003 at (335800.000, 2678250.000)

2 Pipes (SEWER_MAIN):
  - Polyline: MH-001 → MH-002 (length 93.97m, slope 0.5%)
  - Polyline: MH-002 → MH-003 (length 66.12m, slope 0.3%)

2 Labels (SEWER_LABEL):
  - "L=93.97m S=0.50%" near first pipe
  - "L=66.12m S=0.30%" near second pipe
```

### Extraction Steps

```
1. Open sewer.dxf in AutoCAD
2. Start Helper App
3. Select all (type: a)
4. Run getData
5. App receives:
   ✓ Points: 3 (MH-001, MH-002, MH-003)
   ✓ Lines: 2 (pipe1, pipe2)
   ✓ Texts: 2 (labels)
6. Click Export → save as "sewer_network.json"
7. JSON contains complete network
```

### Expected JSON Output

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T15:00:00Z",
  "source": "AutoCAD",
  "coordinateSystemHint": "UTM",
  "points": [
    {"id": "pt-1", "name": "MH-001", "E": 335684.045, "N": 2678151.885, "layer": "MANHOLE_ACCESS"},
    {"id": "pt-2", "name": "MH-002", "E": 335750.000, "N": 2678200.000, "layer": "MANHOLE_ACCESS"},
    {"id": "pt-3", "name": "MH-003", "E": 335800.000, "N": 2678250.000, "layer": "MANHOLE_ACCESS"}
  ],
  "lines": [
    {"id": "ln-1", "name": "PIPE-001", "startE": 335684.045, "startN": 2678151.885, "endE": 335750.000, "endN": 2678200.000, "length": 93.970, "layer": "SEWER_MAIN"},
    {"id": "ln-2", "name": "PIPE-002", "startE": 335750.000, "startN": 2678200.000, "endE": 335800.000, "endN": 2678250.000, "length": 66.120, "layer": "SEWER_MAIN"}
  ],
  "curves": [],
  "texts": [
    {"id": "tx-1", "content": "L=93.97m S=0.50%", "E": 335717.0, "N": 2678175.9, "layer": "SEWER_LABEL"},
    {"id": "tx-2", "content": "L=66.12m S=0.30%", "E": 335775.0, "N": 2678225.0, "layer": "SEWER_LABEL"}
  ]
}
```

### Import to SiteTrack

```
1. In SiteTrack Admin → Projects → Create New
2. Upload sewer_network.json
3. SiteTrack validates and displays:
   - 3 network junctions (MH-001, MH-002, MH-003)
   - 2 pipe connections
   - Network topology
4. Ready for analysis and management
```

---

## Summary

✅ **Setup Complete** when:
- LISP loads in AutoCAD
- Helper App listens for data
- getData command works
- JSON exports correctly

✅ **Ready for Production** when:
- All 5 tests pass
- SiteTrack imports JSON successfully
- Network appears correctly in SiteTrack UI

---

## Next Steps

1. Test with your actual DXF files
2. Adjust LISP script if needed (for custom properties)
3. Document your layer naming conventions
4. Train team on workflow
5. Import first network to SiteTrack

---

**Questions or Issues?** Check Troubleshooting section or review AutoCAD Bridge documentation.
