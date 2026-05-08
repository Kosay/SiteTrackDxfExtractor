# SiteTrack Integration Guide
## Complete Workflow from AutoCAD to SiteTrack Import

This guide explains how to use the **DXF Coordinate Extractor** with **SiteTrack** to extract infrastructure data from AutoCAD DXF files and import it into SiteTrack for network analysis and management.

---

## Table of Contents
1. [Overview](#overview)
2. [Complete Workflow](#complete-workflow)
3. [Step-by-Step Setup](#step-by-step-setup)
4. [Importing JSON into SiteTrack](#importing-json-into-sitetrack)
5. [Data Mapping](#data-mapping)
6. [Troubleshooting](#troubleshooting)

---

## Overview

### What is DXF Coordinate Extractor?

A two-part tool that bridges AutoCAD and SiteTrack:

1. **GetData.lsp** (AutoCAD Script)
   - Extracts infrastructure objects from AutoCAD DXF files
   - Supports: Points, Lines, Curves, Text annotations
   - Automatically detects object properties (coordinates, layer, attributes)
   - Compatible with AutoCAD 2007+

2. **DxfCoordinateExtractor App** (Windows WinForms)
   - Receives data from AutoCAD
   - Displays extracted data in grid format
   - Exports data in SiteTrack-compatible JSON format
   - Provides network topology analysis

### Workflow Overview

```
Your DXF File (in AutoCAD)
        ↓
  Select Objects
        ↓
  Run getData Command
        ↓
  Windows App Detects JSON
        ↓
  Review Data in Grid
        ↓
  Export as JSON
        ↓
  Import into SiteTrack
        ↓
  Build Network Model
```

---

## Complete Workflow

### Phase 1: Preparation
- Ensure DXF file has proper layer structure
- Objects should be on logical layers (e.g., PIPE_MAIN, MANHOLE_POINTS, LABELS)
- Coordinates should be in your project's coordinate system (UTM, WGS84, etc.)

### Phase 2: Data Extraction
- Load GetData.lsp into AutoCAD
- Open your DXF file
- Select infrastructure objects
- Run `getData` command
- AutoCAD validates and extracts objects

### Phase 3: Data Review
- Windows app automatically detects extracted data
- Grid displays all objects with properties
- Review coordinates, layers, text content
- Make corrections if needed (use original DXF if required)

### Phase 4: Data Export
- Click "Export AutoCAD Data" to save JSON
- Choose location and filename
- JSON file is formatted for SiteTrack import

### Phase 5: SiteTrack Import
- Create new project in SiteTrack
- Upload JSON file
- SiteTrack parses data and creates network model
- Review imported network
- Adjust topology if needed
- Ready for analysis and management

---

## Step-by-Step Setup

### Part 1: Install and Configure GetData.lsp

#### Step 1.1: Download GetData.lsp
```
Location: GetData.lsp in project root
Copy to: C:\Users\[YourName]\Documents\AutoCAD\LISP\
         (or your preferred LISP folder)
```

#### Step 1.2: Load in AutoCAD

**Option A: Manual Load (Each Session)**
```
1. Open AutoCAD
2. Type in command line: (load "C:\\path\\to\\GetData.lsp")
3. Press Enter
4. You should see: "✓ GetData.lsp loaded successfully!"
```

**Option B: Auto-Load (Every Session)**
```
1. In AutoCAD, type: appload
2. Click "Contents..."
3. Click "Add..."
4. Browse to GetData.lsp
5. Click OK and close
6. Restart AutoCAD - LISP will auto-load
```

**Option C: Permanent (Edit acaddoc.lsp)**
```
1. Find: C:\Users\[YourName]\AppData\Roaming\Autodesk\AutoCAD 2024\R24\enu\Support\acaddoc.lsp
2. Open with Notepad
3. Add at the end: (load "C:\\path\\to\\GetData.lsp")
4. Save and restart AutoCAD
```

#### Step 1.3: Verify Loading
```
Expected output in AutoCAD command line:
✓ GetData.lsp loaded successfully!
  Run 'getData' command to extract selected objects.
```

---

### Part 2: Install DxfCoordinateExtractor App

#### Step 2.1: Build the Application
```
1. Open DxfCoordinateExtractor.sln in Visual Studio
2. Build solution (Release or Debug)
3. Output: bin\Release\DxfCoordinateExtractor.exe
```

#### Step 2.2: Create Shortcuts (Optional)
```
Create desktop shortcut:
Target: C:\path\to\DxfCoordinateExtractor.exe
Start in: C:\path\to\
```

#### Step 2.3: Permissions Check
```
Ensure the app can:
✓ Read from %USERPROFILE%\Documents\sitetrack_data.json
✓ Write to selected export locations
✓ Access Windows temp folder for file operations
```

---

### Part 3: Prepare Your DXF File

#### Step 3.1: Organize by Layers
```
Recommended layer structure:

MANHOLE_POINTS
  - Blocks at junction/manhole locations
  - Attributes: MH-001, MH-002, etc.

PIPE_MAIN or SEWER_MAIN
  - Polylines representing pipes/conduits
  - Should connect between points

PIPE_SECONDARY
  - Smaller pipes or branches
  - Optional

ANNOTATION or LABELS
  - TEXT entities with pipe info
  - Example: "%%C315 uPVC" (pipe material/size)

MTEXT_LABELS
  - Multi-line text with additional info
  - Example: "S=0.5% L=100m" (slope and length)
```

#### Step 3.2: Verify Coordinates
```
Ensure all coordinates are:
✓ In consistent coordinate system
✓ Positive values (UTM, WGS84, local grid)
✓ At expected precision (6 decimals for meters)
```

#### Step 3.3: Check Object Properties
```
Points should have:
  - Position (X, Y)
  - Layer assignment
  - Optional: Block name or attribute

Lines/Polylines should have:
  - Start position
  - End position  
  - Layer assignment
  - Vertices (for polylines)

Text should have:
  - Position
  - Content (material, size, etc.)
  - Layer assignment
```

---

## Importing JSON into SiteTrack

### SiteTrack JSON Import Process

#### Step 1: Prepare JSON File
```
✓ Export from DxfCoordinateExtractor app
✓ Filename: network_name.json (e.g., "sewer_network.json")
✓ Verify file size is reasonable (typically < 10MB for most networks)
✓ Backup the file
```

#### Step 2: Create SiteTrack Project
```
In SiteTrack Admin Console:

1. Projects → New Project
2. Enter project name: (e.g., "Sewer Network - Central Zone")
3. Select coordinate system: UTM (or match your DXF)
4. Set zone/region if applicable
5. Click Create
```

#### Step 3: Import JSON Data
```
In SiteTrack Project:

1. Data → Import
2. Select "AutoCAD Bridge JSON" format
3. Choose your exported JSON file
4. Click Import
5. SiteTrack validates structure
6. Confirmation: "X points, Y lines, Z curves imported"
```

#### Step 4: Review Imported Network
```
After import, check:

Grid View:
✓ All points appear as junctions
✓ All lines appear as connections
✓ Coordinates match DXF
✓ Layer information preserved

Topology View:
✓ Network connections are logical
✓ No orphaned segments
✓ Proper upstream/downstream relationships

Properties:
✓ Pipe lengths calculated correctly
✓ Material and size info visible
✓ Slope information present (if in labels)
```

#### Step 5: Adjust Topology (If Needed)
```
If connections don't auto-detect:

1. Manually verify junction positions
2. Adjust tolerance settings for snapping
3. Create explicit connections if needed
4. Verify flow direction
5. Set upstream source if needed

Note: Most well-drawn DXF files import without adjustment
```

---

## Data Mapping

### How DXF Objects Map to SiteTrack

#### Points → Junctions/Nodes
```
AutoCAD POINT or INSERT Block
    ↓
SiteTrack Junction
    ├─ ID: auto-generated
    ├─ Position: (E, N) coordinates
    ├─ Name: from block attribute or label
    ├─ Type: derived from layer name
    │         (MANHOLE, CLEANOUT, JUNCTION, etc.)
    └─ Layer: preserved from DXF

Example:
Point at (335684.045, 2678151.885) on layer "MANHOLE"
→ Junction named "MH-001" at that position
```

#### Lines → Pipes/Connections
```
AutoCAD LINE or LWPOLYLINE
    ↓
SiteTrack Pipe/Connection
    ├─ ID: auto-generated
    ├─ From Junction: start point (snapped automatically)
    ├─ To Junction: end point (snapped automatically)
    ├─ Length: calculated from coordinates
    ├─ Diameter/Size: from associated text label
    ├─ Material: from label content
    ├─ Slope: extracted from MTEXT if present
    └─ Layer: preserved from DXF

Example:
Line from (335684, 2678151) to (335750, 2678200)
Label: "%%C315 uPVC" and "S=0.5%"
→ Pipe from MH-001 to MH-002
  Length: 93.97m
  Material: uPVC
  Diameter: 315mm
  Slope: 0.5%
```

#### Text → Attributes/Labels
```
AutoCAD TEXT or MTEXT
    ↓
SiteTrack Annotation
    ├─ Position: (E, N) coordinates
    ├─ Content: text value (MTEXT codes removed)
    ├─ Layer: preserved from DXF
    └─ Type: inferred from content
           "%%C" → diameter
           "S=" → slope
           "L=" → length

Example:
MTEXT: "S=0.5% L=100m"
→ Slope attribute: 0.5%
  Length attribute: 100m
```

#### Curves (Arcs/Circles) → Bends/Fittings
```
AutoCAD ARC or CIRCLE
    ↓
SiteTrack Bend or Fitting
    ├─ Center: (E, N) coordinates
    ├─ Radius: from ARC/CIRCLE
    ├─ Angles: start and end for arcs
    └─ Layer: preserved from DXF

Note: Curves are typically bends in gravity/pressure pipes
```

---

## Troubleshooting

### Problem: "JSON Parse Error" in Windows App

**Cause**: GetData.lsp syntax error or special characters not escaped properly

**Solution**:
```
1. Check AutoCAD command output for errors
2. Reload GetData.lsp: (load "GetData.lsp")
3. Try with fewer objects (simpler test)
4. Check for special characters in text (quotes, backslashes)
5. Verify MTEXT content doesn't have unusual formatting
```

### Problem: "No Data Detected" in Windows App

**Cause**: App not listening or JSON file not created

**Solution**:
```
1. Check that Windows app is running and visible
2. Status bar should show: "Listening for AutoCAD data..."
3. In AutoCAD, verify getData command executed:
   ✓ Points: X
   ✓ Lines: Y
   ✓ Curves: Z
   ✓ Texts: W
4. Check Documents folder for sitetrack_data.json
5. If file exists, try "Browse JSON" button in app
```

### Problem: "SiteTrack Import Failed" or "Invalid JSON"

**Cause**: JSON structure doesn't match SiteTrack schema

**Solution**:
```
1. Validate JSON at jsonlint.com
2. Check that JSON has these top-level sections:
   - "points": [ ... ]
   - "lines": [ ... ]
   - "curves": [ ... ]
   - "texts": [ ... ]
3. Ensure all coordinates are numbers, not strings
4. Verify no trailing commas or syntax errors
5. Re-export from app to regenerate clean JSON
```

### Problem: Coordinates Don't Match DXF

**Cause**: Coordinate system mismatch or rounding

**Solution**:
```
1. Verify DXF coordinate system matches SiteTrack
2. Check precision: 6 decimals is sufficient for meters
3. Small differences (<0.01m) are normal due to rounding
4. If systematic offset, check DXF origin/base point
5. Consider coordinate transformation in SiteTrack if needed
```

### Problem: "Unexpected Object Type" or Unrecognized Objects

**Cause**: Uncommon DXF entity types not supported

**Solution**:
```
Supported types:
✓ POINT (block instances)
✓ LINE
✓ LWPOLYLINE
✓ POLYLINE
✓ ARC
✓ CIRCLE
✓ TEXT
✓ MTEXT
✓ INSERT (blocks as points)

Unsupported (will be skipped):
✗ SPLINE
✗ 3DPOLYLINE
✗ HATCH
✗ IMAGE
✗ OLE objects

If needed, convert to supported types in AutoCAD
```

### Problem: "Layer Not Found" or Missing Attributes

**Cause**: DXF layer structure doesn't match expectations

**Solution**:
```
1. In GetData.lsp, all layers are preserved
2. Layer names are case-sensitive in AutoCAD
3. Use consistent naming convention:
   - MANHOLE_POINTS (not Manhole_Points)
   - SEWER_MAIN (not Sewer-Main)
4. Ensure objects are on intended layer (not layer "0")
5. In SiteTrack, layer becomes "Category" or "Type"
```

---

## FAQ

### Q: Can I update data after importing?
**A**: Yes. Re-run getData on updated DXF, export new JSON, and create new SiteTrack project (or import as update if SiteTrack supports versioning).

### Q: How do I handle manual edits in SiteTrack?
**A**: Changes made in SiteTrack are stored separately. Reimporting JSON will overwrite with DXF data. Keep DXF as source of truth.

### Q: What's the maximum network size?
**A**: Tested with networks of 1000+ junctions and 5000+ pipes. JSON files typically <10MB. Larger networks may require optimization.

### Q: Can I combine multiple DXF files?
**A**: Currently, export each DXF separately. Merge JSON files manually if needed, or create separate projects per DXF.

### Q: How do I document coordinate systems?
**A**: JSON includes "coordinateSystemHint": "UTM". Update this field if using different system (WGS84, Local Grid, etc.).

### Q: Can I add custom attributes?
**A**: Yes. Add properties to objects in DXF (using attributes on blocks). These will be preserved in the "Properties" section of JSON.

---

## Best Practices

### For DXF Creation
- Use consistent layer naming
- Place points at exact junction locations
- Use descriptive text labels
- Include slope and length information in MTEXT
- Verify coordinates before export
- Keep DXF well-organized

### For Data Export
- Review grid data before exporting
- Use meaningful filenames (e.g., "sewer_network_2026-05-08.json")
- Keep backup copies of JSON exports
- Document coordinate system used
- Note any manual corrections needed

### For SiteTrack Import
- Create test project first
- Import in non-production environment
- Verify against original DXF
- Document any transformations applied
- Keep import version history
- Back up imported projects

---

## Support and Feedback

For issues or suggestions:
1. Check the Troubleshooting section above
2. Review GetData.lsp comments for LISP-specific issues
3. Check app error messages (look at Debug output window)
4. Verify DXF file structure against examples
5. Test with a simple DXF first

---

## Version History

- **v1.0** (May 2026): Initial release
  - AutoCAD 2007+ support
  - Windows Forms GUI
  - JSON export for SiteTrack import
  - File-based data transfer (no named pipes)
  - Support for Points, Lines, Curves, Text

---

**Ready to import?** Start with the [Quick Start Guide](QUICKSTART.md)
