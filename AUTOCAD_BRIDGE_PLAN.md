# AutoCAD Bridge App - Implementation Plan

**Goal**: Transform DXF Extractor into an interactive bridge that extracts data from AutoCAD and exports as JSON for SiteTrack.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ AutoCAD 2007+ with DXF file open                            │
├─────────────────────────────────────────────────────────────┤
│ User selects objects + runs "getData" command               │
│ (or right-click context menu)                               │
└────────────────────┬────────────────────────────────────────┘
                     │
        ┌────────────▼────────────┐
        │ AutoCAD LISP/VBA Script │
        │ - Reads selected items  │
        │ - Extracts coordinates  │
        │ - Sends to app via COM  │
        └────────────┬────────────┘
                     │
        ┌────────────▼──────────────────────┐
        │ WinForms App (COM Server)         │
        │ - Receives data from AutoCAD      │
        │ - Stores in memory                │
        │ - Displays in grid                │
        │ - Allows editing/review           │
        │ - Exports JSON                    │
        └────────────┬──────────────────────┘
                     │
        ┌────────────▼────────────┐
        │ JSON File               │
        │ {                       │
        │   points: [...],        │
        │   lines: [...],         │
        │   curves: [...],        │
        │   texts: [...]          │
        │ }                       │
        └────────────┬────────────┘
                     │
        ┌────────────▼────────────┐
        │ SiteTrack Import        │
        │ Convert to database     │
        └─────────────────────────┘
```

---

## Implementation Approach

### Option 1: Named Pipes (Recommended for AutoCAD 2007+)
```
AutoCAD ─[LISP/VBA]─> Named Pipe ─> WinForms App
```
- Simple IPC mechanism
- No COM registration needed
- Works reliably with AutoCAD 2007+
- Best for standalone app

### Option 2: COM Interop (Direct)
```
AutoCAD ─[LISP]─> COM Object ─> WinForms App
```
- More complex
- Requires COM registration
- More robust but harder to debug

**We'll use Option 1: Named Pipes** ✓

---

## Data Flow

### Step 1: User Action in AutoCAD
```lisp
; In AutoCAD, user runs:
(getData)
; or right-clicks on selection

; LISP script:
; - Gets selected objects
; - For each object:
;   - Point: extract E, N, text nearby
;   - Line/Polyline: extract endpoints, properties
;   - Arc: extract start/end, radius
;   - Text/MText: extract content, position
; - Send JSON over named pipe to app
```

### Step 2: App Receives Data
```csharp
// WinForms app listening on named pipe
// When data arrives:
// 1. Deserialize JSON
// 2. Populate internal data structures
// 3. Display in grid/tree view
// 4. Allow user review/edit
```

### Step 3: User Reviews & Exports
```
App Grid:
┌──────────────────────────────────────────┐
│ Points (5)        Lines (8)    Curves (2)│
├──────────────────────────────────────────┤
│ MH-001  35.123  PIPE-001  0.5% slope   │
│ MH-002  35.124  PIPE-002  0.3% slope   │
│ ...                                      │
│                                          │
│ [Export JSON] [Clear] [Review]          │
└──────────────────────────────────────────┘
```

### Step 4: JSON Export
```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-05-03T10:30:00Z",
  "source": "AutoCAD",
  "points": [
    {
      "id": "pt-1",
      "name": "MH-001",
      "E": 335684.045,
      "N": 2678151.885,
      "layer": "MANHOLE",
      "properties": {"depth": 2.5}
    }
  ],
  "lines": [
    {
      "id": "line-1",
      "fromPointId": "pt-1",
      "toPointId": "pt-2",
      "startE": 335684.045,
      "startN": 2678151.885,
      "endE": 335750.000,
      "endN": 2678200.000,
      "length": 93.97,
      "layer": "PIPE",
      "properties": {"slope": 0.5, "diameter": 500}
    }
  ],
  "curves": [
    {
      "id": "arc-1",
      "centerE": 335700.000,
      "centerN": 2678175.000,
      "radius": 50.0,
      "startAngle": 0.0,
      "endAngle": 180.0,
      "layer": "CURVE",
      "properties": {}
    }
  ],
  "texts": [
    {
      "id": "text-1",
      "content": "MH-001",
      "E": 335684.045,
      "N": 2678151.885,
      "layer": "ANNOTATION"
    }
  ]
}
```

---

## Technical Stack

### For AutoCAD Integration
- **AutoCAD 2007+**: Built-in LISP support
- **LISP script** or **VBA macro** to run "getData" command
- **Named Pipes** for IPC (System.IO.Pipes in C#)

### For WinForms App
- **C#** (existing)
- **System.IO.Pipes** (Named Pipes)
- **Json.NET** (Newtonsoft.Json) for JSON
- **DataGridView** for displaying extracted data
- **.NET Framework 4.8** (existing)

---

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- [ ] Create named pipe listener in WinForms app
- [ ] Design data models (Point, Line, Curve, Text classes)
- [ ] Create AutoCAD LISP script with "getData" command
- [ ] Test data transmission from AutoCAD to app

### Phase 2: Data Extraction (Week 2)
- [ ] Implement LISP logic to extract points from AutoCAD selection
- [ ] Implement LISP logic to extract lines
- [ ] Implement LISP logic to extract curves (arcs)
- [ ] Implement LISP logic to extract text labels

### Phase 3: UI & Display (Week 3)
- [ ] Create grid/tree view to display extracted data
- [ ] Add data review/edit functionality
- [ ] Add validation before export
- [ ] Create visual feedback for data quality

### Phase 4: JSON Export (Week 4)
- [ ] Update JSON export to use new structure (points/lines/curves/texts)
- [ ] Add schema validation
- [ ] Test with SiteTrack import
- [ ] Document for users

---

## Detailed Implementation Steps

### Step 1: Create AutoCAD LISP Command

File: `GetData.lsp` (to be loaded in AutoCAD)

```lisp
(defun c:getData (/)
  (princ "Getting data from selection...")
  
  ; Get selected objects
  (setq ss (ssget))
  
  ; If nothing selected, exit
  (if (null ss)
    (progn
      (princ "No objects selected.\n")
      (exit)
    )
  )
  
  ; Initialize collections
  (setq points (list))
  (setq lines (list))
  (setq curves (list))
  (setq texts (list))
  
  ; Process each selected object
  (setq i 0)
  (repeat (sslength ss)
    (setq ent (ssname ss i))
    (setq obj (entget ent))
    (setq type (cdr (assoc 0 obj)))
    
    ; Extract based on type
    (cond
      ((= type "POINT")
        ; Extract point
        (setq pt (cdr (assoc 10 obj)))
        ; Add to points collection
      )
      ((or (= type "LINE") (= type "LWPOLYLINE"))
        ; Extract line endpoints and properties
      )
      ((= type "ARC")
        ; Extract arc center, radius, angles
      )
      ((or (= type "TEXT") (= type "MTEXT"))
        ; Extract text content and position
      )
    )
    
    (setq i (+ i 1))
  )
  
  ; Send data to WinForms app via named pipe
  ; [Code to serialize and send JSON]
  
  (princ "Data sent to helper app.\n")
  (exit)
)

; Load this file in AutoCAD with: (load "GetData.lsp")
```

### Step 2: Add Named Pipe Listener to App

File: `Form1.cs` (new method)

```csharp
private NamedPipeServerStream _pipeServer;
private Thread _pipeListenerThread;

private void StartNamedPipeListener()
{
    _pipeListenerThread = new Thread(ListenForAutoCAD)
    {
        IsBackground = true
    };
    _pipeListenerThread.Start();
}

private void ListenForAutoCAD()
{
    const string pipeName = "SiteTrackDxfExtractorPipe";
    
    try
    {
        _pipeServer = new NamedPipeServerStream(
            pipeName,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Message
        );
        
        while (true)
        {
            _pipeServer.WaitForConnection();
            
            using (var reader = new StreamReader(_pipeServer))
            {
                string jsonData = reader.ReadToEnd();
                
                // Process received JSON
                ProcessAutoCADData(jsonData);
            }
            
            _pipeServer.Disconnect();
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Pipe error: {ex.Message}");
    }
}

private void ProcessAutoCADData(string jsonData)
{
    try
    {
        var data = JsonConvert.DeserializeObject<AutoCADExportData>(jsonData);
        
        // Populate UI with extracted data
        DisplayExtractedData(data);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error processing data: {ex.Message}");
    }
}
```

### Step 3: Define Data Models

File: `DataModels.cs` (new file)

```csharp
public class AutoCADExportData
{
    public List<PointData> Points { get; set; } = new();
    public List<LineData> Lines { get; set; } = new();
    public List<CurveData> Curves { get; set; } = new();
    public List<TextData> Texts { get; set; } = new();
}

public class PointData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double E { get; set; }
    public double N { get; set; }
    public string Layer { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}

public class LineData
{
    public string Id { get; set; }
    public double StartE { get; set; }
    public double StartN { get; set; }
    public double EndE { get; set; }
    public double EndN { get; set; }
    public double Length { get; set; }
    public string Layer { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}

public class CurveData
{
    public string Id { get; set; }
    public double CenterE { get; set; }
    public double CenterN { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
    public string Layer { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}

public class TextData
{
    public string Id { get; set; }
    public string Content { get; set; }
    public double E { get; set; }
    public double N { get; set; }
    public string Layer { get; set; }
}
```

### Step 4: Update JSON Export

Modify `WriteNetworkJson()` to handle the new structure with points, lines, curves, texts sections.

---

## Setup Instructions for User

### 1. Install LISP Script in AutoCAD

```
1. Copy GetData.lsp to Documents folder
2. In AutoCAD:
   - Type: (load "GetData.lsp")
   - Or add to acaddoc.lsp for auto-load
3. Close AutoCAD
```

### 2. Run Helper App

```
1. Start DxfCoordinateExtractor.exe
2. App listens on named pipe
3. Open DXF file in AutoCAD
4. Select objects
5. Type: getData
   (or right-click if configured)
6. App receives and displays data
7. Click Export JSON
```

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| AutoCAD not responding | Add timeout to pipe listener |
| LISP script errors | Comprehensive error handling in LISP |
| Data format mismatch | Validate JSON schema in app |
| Coordinate system | Always request and include system hint |
| Large selections | Implement progress bar + chunking |

---

## Testing Strategy

1. **Unit Tests**: Data model serialization
2. **Integration Tests**: LISP ↔ App via named pipes
3. **End-to-End Tests**: AutoCAD → App → JSON → SiteTrack
4. **Performance Tests**: Large selection handling

---

## Deliverables

1. ✓ Updated Form1.cs with named pipe listener
2. ✓ New DataModels.cs with data classes
3. ✓ GetData.lsp LISP script for AutoCAD
4. ✓ Updated JSON export structure
5. ✓ User setup documentation
6. ✓ Example DXF files for testing

---

## Timeline

- **Week 1**: Named pipe infrastructure + LISP basics
- **Week 2**: Data extraction from AutoCAD objects
- **Week 3**: UI improvements + data review
- **Week 4**: JSON export + end-to-end testing
- **Week 5**: Documentation + user testing

---

## Success Criteria

- ✓ Engineer can select objects in AutoCAD
- ✓ App receives data within 5 seconds
- ✓ JSON exports with correct structure
- ✓ SiteTrack successfully imports JSON
- ✓ Complete workflow tested end-to-end
- ✓ Documentation clear for users

---

**Ready to start implementation?** Confirm and I'll begin Phase 1!
