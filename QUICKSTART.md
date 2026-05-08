# SiteTrack DXF Extractor - Quick Start Guide

Get up and running with AutoCAD to SiteTrack data extraction in minutes.

---

## 1. Install GetData.lsp (One-Time Setup)

### Step 1: Copy the Script
```
Copy GetData.lsp to: C:\Users\[YourName]\Documents\AutoCAD\LISP\
```

### Step 2: Load in AutoCAD
```
Command line: (load "C:\\path\\to\\GetData.lsp")
Press Enter
Expected output: ✓ GetData.lsp loaded successfully!
```

**For auto-load each session:**
- Type `appload` in AutoCAD
- Click "Contents..." → "Add..."
- Browse to GetData.lsp → Click OK
- Restart AutoCAD

---

## 2. Prepare Your DXF File

Organize objects by layers:

```
MANHOLE_POINTS    (block instances at junctions)
PIPE_MAIN         (lines/polylines for main pipes)
ANNOTATION        (text labels with pipe info)
MTEXT_LABELS      (slope, length info)
```

**Example MTEXT content:**
```
S=0.5% L=100m        → Slope 0.5%, Length 100m
%%C315 uPVC          → Pipe diameter 315mm, Material uPVC
```

---

## 3. Extract Data from AutoCAD

1. **Select objects** in your DXF drawing
2. **Run command:** `getData`
3. **Verify output:**
   ```
   ✓ Points: X
   ✓ Lines: Y
   ✓ Curves: Z
   ✓ Texts: W
   ✓ Data sent to SiteTrack Bridge App!
   ```
4. **Check file created:** `%USERPROFILE%\Documents\sitetrack_data.json`

---

## 4. Review Data in Windows App

1. **Open DxfCoordinateExtractor.exe**
2. **Data automatically loads** from `sitetrack_data.json`
3. **Review grid:**
   - Verify point coordinates
   - Check line connections
   - Confirm text content
4. **[Browse JSON]** button to manually select file if needed

---

## 5. Export for SiteTrack

1. **Click [Export AutoCAD Data]** button
2. **Choose location and filename** (e.g., `sewer_network.json`)
3. **File is ready for SiteTrack import**

---

## 6. Import into SiteTrack

### In SiteTrack Admin:

1. **Projects → New Project**
2. **Enter project name** (e.g., "Sewer Network - Zone A")
3. **Select coordinate system** (UTM, WGS84, etc.)

### In Project:

1. **Data → Import**
2. **Select: "AutoCAD Bridge JSON" format**
3. **Choose exported JSON file**
4. **Click Import**
5. **Verify:** Points appear as junctions, lines as pipes

---

## What Gets Imported

| DXF Object | SiteTrack Element |
|------------|------------------|
| POINT / INSERT Block | Junction/Node |
| LINE | Pipe Connection |
| LWPOLYLINE | Pipe Path |
| TEXT | Label/Annotation |
| MTEXT | Attributes (diameter, slope, material) |
| ARC / CIRCLE | Bend/Fitting |

---

## Common Issues

**"No Data Detected"**
- Verify Windows app is running
- Check that getData executed successfully in AutoCAD
- Ensure sitetrack_data.json exists in Documents folder

**"JSON Parse Error"**
- Re-run getData in AutoCAD
- Close and reopen Windows app
- Try [Browse JSON] to manually reload

**"Coordinates Don't Match DXF"**
- Small differences (<0.01m) are normal due to rounding
- Verify coordinate system consistency between DXF and SiteTrack

---

## Tips

- **Start small:** Test with 5-10 objects first
- **Keep DXF organized:** Use consistent layer names
- **Include labels:** MTEXT improves data richness
- **Backup JSON:** Save exported files before importing to SiteTrack
- **Verify coordinates:** Check that DXF uses consistent coordinate system (UTM, WGS84, etc.)

---

**For detailed information:** See [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)

**Need help?** Check the Troubleshooting section in INTEGRATION_GUIDE.md
