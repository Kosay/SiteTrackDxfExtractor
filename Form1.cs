using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using netDxf;
using netDxf.Blocks;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
using Newtonsoft.Json;
using DxfPoint = netDxf.Entities.Point;

namespace DxfCoordinateExtractor;

// ──────────────────────────────────────────────────────────────
//  Layer role assigned by the user in the UI
// ──────────────────────────────────────────────────────────────
public enum LayerRole { None, Node, Pipe, Label }

// ──────────────────────────────────────────────────────────────
//  Internal models used by the topology engine
// ──────────────────────────────────────────────────────────────
internal sealed class NodeRecord
{
    public string Id { get; set; } = "";
    public double E { get; set; }
    public double N { get; set; }
    public string Layer { get; set; } = "";
    public List<string> Connections { get; set; } = new();
}

internal sealed class ConnectionRecord
{
    public string Id { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public double Length { get; set; }
    public double? Slope { get; set; }
    public double? Diameter { get; set; }
}

// FIX Bug 7: SourceLayout added so proximity matching stays within the same layout
internal sealed class TextEntity
{
    public double E { get; set; }
    public double N { get; set; }
    public string Value { get; set; } = "";
    public string SourceLayout { get; set; } = "";
}

internal sealed class PipeLabelEntity
{
    public double E { get; set; }
    public double N { get; set; }
    public string SourceLayout { get; set; } = "";
    public double? Length { get; set; }
    public double? Slope { get; set; }
    public double? Diameter { get; set; }
}

// ──────────────────────────────────────────────────────────────
//  Main form
// ──────────────────────────────────────────────────────────────
public partial class Form1 : Form
{
    private static readonly Regex LengthRx   = new(@"L=([\d.]+)m", RegexOptions.IgnoreCase);
    private static readonly Regex SlopeRx    = new(@"S=([\d.]+)%", RegexOptions.IgnoreCase);
    private static readonly Regex DiameterRx = new(@"%%[Cc](\d+)", RegexOptions.IgnoreCase);

    // FIX Bug 6: captures text payload after MText format codes instead of stripping entire block
    // Before: @"\{\\[^}]*\}" removed {\ ... } blocks entirely, stripping content like "MH-001"
    // After:  matches {\ format; content} and replaces with just content
    private static readonly Regex MTextFormatBlock = new(@"\{\\[^;]*;([^}]*)\}", RegexOptions.IgnoreCase);

    private DxfDocument? _dxfDoc;
    private string _dxfFilePath = "";
    private readonly List<EntityRow> _allRows = new();
    private readonly List<TextEntity> _textEntities = new();
    private readonly List<PipeLabelEntity> _pipeLabels = new();
    private readonly Dictionary<string, LayerRole> _layerRoles = new();

    // AutoCAD Bridge - File-based handshake
    private FileSystemWatcher? _autoCADWatcher;
    private AutoCADExportData? _extractedAutoCADData;
    private readonly string _autoCADJsonPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "sitetrack_data.json"
    );

    public Form1()
    {
        InitializeComponent();
        SetupGrid();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StartAutoCADFileWatcher();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_autoCADWatcher != null)
        {
            _autoCADWatcher.EnableRaisingEvents = false;
            _autoCADWatcher.Dispose();
        }
    }

    // ─── AutoCAD Bridge - File Watcher ────────────────────────
    private void StartAutoCADFileWatcher()
    {
        try
        {
            var docsPath = Path.GetDirectoryName(_autoCADJsonPath) ?? "";
            if (string.IsNullOrEmpty(docsPath)) return;

            _autoCADWatcher = new FileSystemWatcher(docsPath, "sitetrack_data.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _autoCADWatcher.Changed += (s, e) => OnAutoCADDataFileChanged(e.FullPath);
            _autoCADWatcher.Created += (s, e) => OnAutoCADDataFileChanged(e.FullPath);

            lblStatus.Text = "Listening for AutoCAD data...";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error: Failed to start AutoCAD watcher: {ex.Message}";
            Debug.WriteLine($"Failed to start AutoCAD watcher: {ex.Message}");
        }
    }

    private void OnAutoCADDataFileChanged(string filePath)
    {
        try
        {
            // Retry reading file in case it's still being written
            string? jsonData = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(50 * (i + 1)); // Progressive delay
                    if (File.Exists(filePath))
                    {
                        jsonData = File.ReadAllText(filePath, Encoding.UTF8);
                        break;
                    }
                }
                catch (IOException)
                {
                    if (i == 4) throw; // Throw on last attempt
                }
            }

            if (!string.IsNullOrEmpty(jsonData))
            {
                if (IsHandleCreated)
                {
                    Invoke(() => ProcessAutoCADData(jsonData));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading AutoCAD data file: {ex.Message}");
        }
    }

    private void ProcessAutoCADData(string jsonData)
    {
        try
        {
            _extractedAutoCADData = JsonConvert.DeserializeObject<AutoCADExportData>(jsonData);

            if (_extractedAutoCADData != null)
            {
                DisplayAutoCADData(_extractedAutoCADData);
                lblStatus.Text = $"✓ Received from AutoCAD: {_extractedAutoCADData.Points.Count} points, " +
                                 $"{_extractedAutoCADData.Lines.Count} lines, " +
                                 $"{_extractedAutoCADData.Curves.Count} curves, " +
                                 $"{_extractedAutoCADData.Texts.Count} texts";
            }
        }
        catch (JsonException ex)
        {
            MessageBox.Show($"JSON Parse Error:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error Processing AutoCAD Data:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DisplayAutoCADData(AutoCADExportData data)
    {
        // NOTE: Each new data load completely replaces the old data.
        // Running getData twice will replace the previous extraction, not merge.
        // This ensures you always see current data from the latest AutoCAD selection.
        _allRows.Clear();

        // Add points
        foreach (var point in data.Points)
        {
            _allRows.Add(new EntityRow
            {
                EntityType = "POINT",
                Layer = point.Layer,
                Label = $"{point.Name} ({point.E:F2}, {point.N:F2})",
                SourceLayout = "AutoCAD",
                X = point.E.ToString("F6"),
                Y = point.N.ToString("F6"),
                Z = "0",
                IsSelected = false
            });
        }

        // Add lines
        foreach (var line in data.Lines)
        {
            var length = Math.Sqrt(
                Math.Pow(line.EndE - line.StartE, 2) +
                Math.Pow(line.EndN - line.StartN, 2)
            );

            _allRows.Add(new EntityRow
            {
                EntityType = "LINE",
                Layer = line.Layer,
                Label = $"{line.Name} ({length:F2}m)",
                SourceLayout = "AutoCAD",
                X = line.StartE.ToString("F6"),
                Y = line.StartN.ToString("F6"),
                Z = "0",
                IsSelected = false
            });
        }

        // Add curves
        foreach (var curve in data.Curves)
        {
            // Determine if ARC or CIRCLE based on angle difference
            var curveType = !string.IsNullOrEmpty(curve.EntityType) ? curve.EntityType :
                           (Math.Abs(curve.EndAngle - curve.StartAngle) > 0.0001 ? "ARC" : "CIRCLE");

            _allRows.Add(new EntityRow
            {
                EntityType = curveType,
                Layer = curve.Layer,
                Label = $"{curve.Name} (R:{curve.Radius:F2})",
                SourceLayout = "AutoCAD",
                X = curve.CenterE.ToString("F6"),
                Y = curve.CenterN.ToString("F6"),
                Z = "0",
                IsSelected = false
            });
        }

        // Add texts
        foreach (var text in data.Texts)
        {
            _allRows.Add(new EntityRow
            {
                EntityType = "TEXT",
                Layer = text.Layer,
                Label = text.Content,
                SourceLayout = "AutoCAD",
                X = text.E.ToString("F6"),
                Y = text.N.ToString("F6"),
                Z = "0",
                IsSelected = false
            });
        }

        // Refresh display
        PopulateLayerFilter();
        BindGrid(null, null);
    }

    // ─── Grid setup ───────────────────────────────────────────
    private void SetupGrid()
    {
        dgvEntities.AutoGenerateColumns = false;
        dgvEntities.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvEntities.MultiSelect = true;
        dgvEntities.AllowUserToAddRows = false;
        dgvEntities.RowHeadersVisible = false;
        dgvEntities.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);

        dgvEntities.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "colSelect", HeaderText = "\u2713", Width = 35,
            DataPropertyName = nameof(EntityRow.IsSelected)
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colType", HeaderText = "Type", Width = 85,
            DataPropertyName = nameof(EntityRow.EntityType), ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colRole", HeaderText = "Role", Width = 60,
            DataPropertyName = nameof(EntityRow.LayerRoleDisplay), ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLayoutTab", HeaderText = "Layout", Width = 80,
            DataPropertyName = nameof(EntityRow.SourceLayout), ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLayer", HeaderText = "Layer", Width = 160,
            DataPropertyName = nameof(EntityRow.Layer), ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLabel", HeaderText = "Label / Description", Width = 200,
            DataPropertyName = nameof(EntityRow.Label), ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colCoords", HeaderText = "Coordinates", Width = 260,
            DataPropertyName = nameof(EntityRow.CoordSummary), ReadOnly = true
        });
    }

    // ─── File loading ─────────────────────────────────────────
    private void btnOpen_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open DXF File",
            Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        LoadDxf(dlg.FileName);
    }

    private void LoadDxf(string path)
    {
        try
        {
            lblStatus.Text = "Loading...";
            Application.DoEvents();

            var version = DxfDocument.CheckDxfFileVersion(path);
            if (version < DxfVersion.AutoCad2000)
            {
                MessageBox.Show(
                    "This DXF file was saved in a version older than AutoCAD 2000.\n" +
                    "Open it in AutoCAD and re-save as AutoCAD 2007 DXF.",
                    "Unsupported Version", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _dxfDoc = DxfDocument.Load(path);
            if (_dxfDoc == null)
            {
                MessageBox.Show("Failed to load DXF file. The file may be corrupted.",
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _dxfFilePath = path;
            _layerRoles.Clear();

            lblFile.Text = Path.GetFileName(path);
            ParseAllEntities();
            PopulateLayerFilter();
            PopulateLayerRoleList();
            ApplyFilters();

            lblStatus.Text = $"Loaded: {_allRows.Count} entities found.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading DXF:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Load failed.";
        }
    }

    private void btnBrowseJSON_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select AutoCAD JSON Data File",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        LoadAutoCADJSON(dlg.FileName);
    }

    private void LoadAutoCADJSON(string path)
    {
        try
        {
            lblStatus.Text = "Loading JSON...";
            Application.DoEvents();

            if (!File.Exists(path))
            {
                MessageBox.Show($"File not found: {path}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "File not found.";
                return;
            }

            var jsonData = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrEmpty(jsonData))
            {
                MessageBox.Show("JSON file is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "JSON file is empty.";
                return;
            }

            ProcessAutoCADData(jsonData);
            lblStatus.Text = $"✓ Loaded JSON from {Path.GetFileName(path)}";
        }
        catch (JsonException ex)
        {
            MessageBox.Show($"JSON Parse Error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "JSON parse error.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading JSON:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Load failed.";
        }
    }

    // ─── Parsing ──────────────────────────────────────────────
    private void ParseAllEntities()
    {
        _allRows.Clear();
        _textEntities.Clear();
        _pipeLabels.Clear();
        if (_dxfDoc == null) return;

        foreach (var layout in OrderLayouts(_dxfDoc.Layouts))
        {
            var block = layout.AssociatedBlock;
            if (block == null) continue;
            ParseEntitiesInBlock(layout.Name, block);
        }
    }

    private static IEnumerable<netDxf.Objects.Layout> OrderLayouts(netDxf.Collections.Layouts layouts)
    {
        return layouts
            .Cast<netDxf.Objects.Layout>()
            .OrderBy(l => string.Equals(l.Name, netDxf.Objects.Layout.ModelSpaceName,
                StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(l => l.TabOrder)
            .ThenBy(l => l.Name, StringComparer.OrdinalIgnoreCase);
    }

    private void ParseEntitiesInBlock(string layoutName, Block block)
    {
        foreach (var entity in block.Entities)
        {
            switch (entity)
            {
                case Line line:           AddLine(layoutName, line);          break;
                case DxfPoint pt:         AddPoint(layoutName, pt);           break;
                case Circle circle:       AddCircle(layoutName, circle);      break;
                case Polyline2D poly:     AddPolyline2D(layoutName, poly);    break;
                case Insert ins:          AddInsert(layoutName, ins);         break;
                case netDxf.Entities.Text txt:
                    CollectText(txt.Value, txt.Position.X, txt.Position.Y, layoutName);
                    break;
                case MText mtext:
                    CollectText(mtext.Value, mtext.Position.X, mtext.Position.Y, layoutName);
                    break;

            }
        }
    }

    // FIX Bug 7: text entities tagged with SourceLayout for layout-scoped matching
    private void CollectText(string value, double x, double y, string layoutName)
    {
        var clean = CleanMTextCodes(value).Trim();
        if (!string.IsNullOrEmpty(clean))
            _textEntities.Add(new TextEntity { E = x, N = y, Value = clean, SourceLayout = layoutName });
    }

    // FIX Bug 6: preserve text payload after MText format codes
    // Before: stripped entire {\...} block — removed content like "MH-001"
    // After: replaces {\ format; content} with just content, keeping the label text
    private static string CleanMTextCodes(string s)
    {
        s = MTextFormatBlock.Replace(s, "$1");
        s = Regex.Replace(s, @"\\[A-Za-z][^;]*;", "");
        return s.Replace(@"\P", "\n").Replace(@"\~", " ")
                .Replace(@"%%c", "Ø").Replace(@"%%C", "Ø")
                .Replace(@"%%d", "°").Replace(@"%%p", "±");
    }

    private void AddLine(string layoutName, Line line)
    {
        var notes = NotesColumn(layoutName, null);
        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType   = "Line",
            Layer        = line.Layer.Name,
            Label        = $"Start({line.StartPoint.X:F3}, {line.StartPoint.Y:F3}) → End({line.EndPoint.X:F3}, {line.EndPoint.Y:F3})",
            CoordSummary = $"X1={line.StartPoint.X:F4}, Y1={line.StartPoint.Y:F4}, X2={line.EndPoint.X:F4}, Y2={line.EndPoint.Y:F4}",
            CsvRows      = new List<string>
            {
                $"Line,{EscapeCsv(line.Layer.Name)},StartPoint,{line.StartPoint.X:F6},{line.StartPoint.Y:F6},{notes}",
                $"Line,{EscapeCsv(line.Layer.Name)},EndPoint,{line.EndPoint.X:F6},{line.EndPoint.Y:F6},{notes}"
            }
        });
    }

    private void AddPoint(string layoutName, DxfPoint pt)
    {
        var notes = NotesColumn(layoutName, null);
        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType   = "Point",
            Layer        = pt.Layer.Name,
            Label        = $"({pt.Position.X:F3}, {pt.Position.Y:F3})",
            CoordSummary = $"X={pt.Position.X:F4}, Y={pt.Position.Y:F4}",
            CsvRows      = new List<string>
            {
                $"Point,{EscapeCsv(pt.Layer.Name)},Point,{pt.Position.X:F6},{pt.Position.Y:F6},{notes}"
            }
        });
    }

    private void AddCircle(string layoutName, Circle circle)
    {
        var notes = NotesColumn(layoutName, $"Radius={circle.Radius:F6}");
        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType   = "Circle",
            Layer        = circle.Layer.Name,
            Label        = $"Center({circle.Center.X:F3}, {circle.Center.Y:F3}) R={circle.Radius:F3}",
            CoordSummary = $"CX={circle.Center.X:F4}, CY={circle.Center.Y:F4}, R={circle.Radius:F4}",
            CsvRows      = new List<string>
            {
                $"Circle,{EscapeCsv(circle.Layer.Name)},Center,{circle.Center.X:F6},{circle.Center.Y:F6},{notes}"
            }
        });
    }

    // FIX Bug 1: raw numeric vertices stored directly on EntityRow (RawVertices)
    // Before: only serialized to CsvRows strings; BuildTopology re-parsed with Split(',') — broke on quoted layer names
    // After: numeric list populated here, consumed directly by BuildTopology
    private void AddPolyline2D(string layoutName, Polyline2D poly)
    {
        var csvRows     = new List<string>();
        var vertexParts = new List<string>();
        var rawVertices = new List<(double E, double N)>();
        var verts = poly.Vertexes;
        var notes = NotesColumn(layoutName, null);

        for (var i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            csvRows.Add(
                $"Polyline2D,{EscapeCsv(poly.Layer.Name)},Vertex{i + 1}," +
                $"{v.Position.X.ToString("F6", CultureInfo.InvariantCulture)}," +
                $"{v.Position.Y.ToString("F6", CultureInfo.InvariantCulture)},{notes}");
            vertexParts.Add($"V{i + 1}({v.Position.X:F2},{v.Position.Y:F2})");
            rawVertices.Add((v.Position.X, v.Position.Y));
        }

        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType   = "Polyline2D",
            Layer        = poly.Layer.Name,
            Label        = $"{verts.Count} vertices" + (poly.IsClosed ? " [Closed]" : " [Open]"),
            CoordSummary = string.Join(" → ", vertexParts.Take(3)) +
                           (vertexParts.Count > 3 ? $" ... +{vertexParts.Count - 3} more" : ""),
            CsvRows      = csvRows,
            RawVertices  = rawVertices
        });
    }

    // FIX Bug 2: raw E/N coordinates stored directly on EntityRow (RawE, RawN)
    // Before: topology engine parsed CoordSummary display string — failed on negative coords and comma-decimal locales
    // After: numeric values stored at parse time, no string re-parsing needed
    private void AddInsert(string layoutName, Insert ins)
    {
        var blockName     = ins.Block?.Name ?? "";
        var pipeLabelMeta = TryParsePipeLabel(blockName, layoutName);

        var attrText = ins.Attributes.Count > 0
            ? string.Join("; ", ins.Attributes.Select(a => $"{a.Tag}={a.Value}"))
            : "";

        var notes = NotesColumn(layoutName, string.IsNullOrEmpty(blockName) ? null : $"Block={blockName}");

        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType   = "Insert",
            Layer        = ins.Layer.Name,
            Label        = $"Block \"{blockName}\" @ ({ins.Position.X:F3}, {ins.Position.Y:F3})" +
                           (string.IsNullOrEmpty(attrText) ? "" : $"  [{attrText}]"),
            CoordSummary = $"X={ins.Position.X:F4}, Y={ins.Position.Y:F4}, Z={ins.Position.Z:F4}",
            RawE         = ins.Position.X,
            RawN         = ins.Position.Y,
            CsvRows      = new List<string>
            {
                $"Insert,{EscapeCsv(ins.Layer.Name)},InsertionPoint," +
                $"{ins.Position.X.ToString("F6", CultureInfo.InvariantCulture)}," +
                $"{ins.Position.Y.ToString("F6", CultureInfo.InvariantCulture)},{notes}"
            }
        });

        if (pipeLabelMeta != null)
        {
            pipeLabelMeta.E = ins.Position.X;
            pipeLabelMeta.N = ins.Position.Y;
            _pipeLabels.Add(pipeLabelMeta);
        }
    }

    // FIX Bug 3: use InvariantCulture + TryParse — no FormatException on comma-decimal locales
    // Before: double.Parse(...) without culture threw on systems using ',' as decimal separator
    private static PipeLabelEntity? TryParsePipeLabel(string name, string layoutName)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var mL = LengthRx.Match(name);
        var mS = SlopeRx.Match(name);
        var mD = DiameterRx.Match(name);

        if (!mL.Success && !mS.Success && !mD.Success) return null;

        double? length = null, slope = null, diameter = null;

        if (mL.Success && double.TryParse(mL.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var l)) length   = l;
        if (mS.Success && double.TryParse(mS.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var s)) slope    = s;
        if (mD.Success && double.TryParse(mD.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var d)) diameter = d;

        if (length == null && slope == null && diameter == null) return null;

        return new PipeLabelEntity
        {
            Length = length, Slope = slope, Diameter = diameter,
            SourceLayout = layoutName
        };
    }

    // ─── Layer role UI ────────────────────────────────────────
    private void PopulateLayerRoleList()
    {
        lstLayers.Items.Clear();
        var layers = _allRows.Select(r => r.Layer).Distinct().OrderBy(l => l).ToList();
        foreach (var layer in layers)
        {
            lstLayers.Items.Add(layer);
            if (!_layerRoles.ContainsKey(layer))
                _layerRoles[layer] = LayerRole.None;
        }
        if (lstLayers.Items.Count > 0) lstLayers.SelectedIndex = 0;
    }

    private void lstLayers_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstLayers.SelectedItem is string layer && _layerRoles.TryGetValue(layer, out var role))
            cmbRole.SelectedIndex = (int)role;
    }

    private void cmbRole_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstLayers.SelectedItem is not string layer) return;
        _layerRoles[layer] = (LayerRole)cmbRole.SelectedIndex;
        foreach (DataGridViewRow row in dgvEntities.Rows)
        {
            if (row.DataBoundItem is EntityRow er && er.Layer == layer)
                er.LayerRole = _layerRoles[layer];
        }
        dgvEntities.Refresh();
    }

    // ─── Layer / type filter ──────────────────────────────────
    private void PopulateLayerFilter()
    {
        cmbLayer.SelectedIndexChanged -= cmbLayer_SelectedIndexChanged;
        cmbLayer.Items.Clear();
        cmbLayer.Items.Add("All Layers");
        foreach (var layer in _allRows.Select(r => r.Layer).Distinct().OrderBy(l => l))
            cmbLayer.Items.Add(layer);
        cmbLayer.SelectedIndex = 0;
        cmbLayer.SelectedIndexChanged += cmbLayer_SelectedIndexChanged;
    }

    private void cmbLayer_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilters();
    private void cmbType_SelectedIndexChanged(object? sender, EventArgs e)  => ApplyFilters();

    private void ApplyFilters()
    {
        var layerFilter = cmbLayer.SelectedIndex <= 0 ? null : cmbLayer.SelectedItem?.ToString();
        var typeFilter  = cmbType.SelectedIndex  <= 0 ? null : cmbType.SelectedItem?.ToString();
        foreach (var r in _allRows)
            r.LayerRole = _layerRoles.TryGetValue(r.Layer, out var role) ? role : LayerRole.None;
        BindGrid(layerFilter, typeFilter);
    }

    private void BindGrid(string? layerFilter, string? typeFilter = null)
    {
        IEnumerable<EntityRow> filtered = _allRows;
        if (!string.IsNullOrEmpty(layerFilter))
            filtered = filtered.Where(r => r.Layer == layerFilter);
        if (!string.IsNullOrEmpty(typeFilter))
            filtered = filtered.Where(r => r.EntityType == typeFilter);

        var bindingList = filtered.ToList();
        dgvEntities.DataSource = null;
        dgvEntities.DataSource = bindingList;
        lblStatus.Text = $"Showing {bindingList.Count} of {_allRows.Count} entities.";
    }

    // ─── Selection helpers ────────────────────────────────────
    private void btnSelectAll_Click(object sender, EventArgs e)  => SetAllCheckboxes(true);
    private void btnSelectNone_Click(object sender, EventArgs e) => SetAllCheckboxes(false);

    private void SetAllCheckboxes(bool value)
    {
        foreach (DataGridViewRow row in dgvEntities.Rows)
        {
            if (row.DataBoundItem is EntityRow entity)
                entity.IsSelected = value;
        }
        dgvEntities.Refresh();
    }

    // ─── Standard CSV export ──────────────────────────────────
    private void btnExport_Click(object sender, EventArgs e)
    {
        var selected = _allRows.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("No entities selected.\nTick the checkbox column to select entities.",
                "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Save CSV", Filter = "CSV Files (*.csv)|*.csv",
            FileName = "dxf_coordinates.csv"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("EntityType,Layer,PointRole,X,Y,Notes");
            foreach (var entity in selected)
                foreach (var row in entity.CsvRows)
                    sb.AppendLine(row);

            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(false));
            lblStatus.Text = $"Exported {selected.Count} entities → {Path.GetFileName(dlg.FileName)}";

            if (MessageBox.Show("CSV saved. Open it now?", "Done",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ─── SiteTrack export ─────────────────────────────────────
    private void btnExportSiteTrack_Click(object sender, EventArgs e)
    {
        var hasNodes = _layerRoles.Values.Any(r => r == LayerRole.Node);
        var hasPipes = _layerRoles.Values.Any(r => r == LayerRole.Pipe);

        if (!hasNodes)
        {
            MessageBox.Show(
                "No Node layers assigned.\nSelect each manhole layer and set its role to 'Node'.",
                "Missing Node Layers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!hasPipes)
        {
            MessageBox.Show(
                "No Pipe layers assigned.\nSelect each pipe/polyline layer and set its role to 'Pipe'.",
                "Missing Pipe Layers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title    = "Save SiteTrack Export (base name)",
            Filter   = "CSV Files (*.csv)|*.csv",
            FileName = Path.GetFileNameWithoutExtension(_dxfFilePath) + "_nodes.csv"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            lblStatus.Text = "Building network topology...";
            Application.DoEvents();

            var dir      = Path.GetDirectoryName(dlg.FileName)!;
            var baseName = Path.GetFileNameWithoutExtension(dlg.FileName).Replace("_nodes", "");
            var basePath = Path.Combine(dir, baseName);

            var tolerance  = (double)nudTolerance.Value;
            var textRadius = (double)nudTextRadius.Value;

            var (nodes, connections) = BuildTopology(tolerance, textRadius);

            File.WriteAllText(basePath + "_nodes.csv",       BuildNodesCsv(nodes),            new UTF8Encoding(false));
            File.WriteAllText(basePath + "_connections.csv", BuildConnectionsCsv(connections), new UTF8Encoding(false));
            WriteNetworkJson(basePath + "_network.json",     nodes, connections);

            lblStatus.Text = $"Exported {nodes.Count} nodes, {connections.Count} connections.";
            MessageBox.Show(
                $"SiteTrack export complete:\n\n" +
                $"  Nodes:       {baseName}_nodes.csv\n" +
                $"  Connections: {baseName}_connections.csv\n" +
                $"  JSON:        {baseName}_network.json\n\n" +
                $"{nodes.Count} nodes · {connections.Count} connections",
                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Export failed.";
        }
    }

    // ─── Topology engine ──────────────────────────────────────
    private (List<NodeRecord> nodes, List<ConnectionRecord> connections) BuildTopology(
        double snapToleranceM, double textRadiusM)
    {
        // 1. Collect nodes — FIX Bug 2: use RawE/RawN (no CoordSummary string parsing)
        var nodes     = new List<NodeRecord>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var autoIndex = 0;

        foreach (var row in _allRows.Where(r => GetRole(r.Layer) == LayerRole.Node && r.EntityType == "Insert"))
        {
            // FIX Bug 7: text search restricted to same layout
            var name = FindNearestText(row.RawE, row.RawN, textRadiusM, row.SourceLayout);

            // FIX Bug 5: deduplicate — if text already used or empty, generate unique sequential ID
            if (string.IsNullOrEmpty(name) || usedNames.Contains(name))
            {
                do { autoIndex++; name = $"MH-{autoIndex:D3}"; }
                while (usedNames.Contains(name));
            }

            usedNames.Add(name);
            nodes.Add(new NodeRecord { Id = name, E = row.RawE, N = row.RawN, Layer = row.Layer });
        }

        // 2. Derive connections — FIX Bug 1: use RawVertices (no CSV re-parsing)
        var connections = new List<ConnectionRecord>();
        var pipeIndex   = 0;

        foreach (var row in _allRows.Where(r => GetRole(r.Layer) == LayerRole.Pipe && r.EntityType == "Polyline2D"))
        {
            var verts = row.RawVertices;
            if (verts == null || verts.Count < 2) continue;

            var first    = verts[0];
            var last     = verts[verts.Count - 1];
            var fromNode = FindNearestNode(nodes, first.E, first.N, snapToleranceM);
            var toNode   = FindNearestNode(nodes, last.E,  last.N,  snapToleranceM);

            if (fromNode == null || toNode == null || fromNode.Id == toNode.Id) continue;

            var arcLength = 0.0;
            for (var i = 1; i < verts.Count; i++)
            {
                var dx = verts[i].E - verts[i - 1].E;
                var dy = verts[i].N - verts[i - 1].N;
                arcLength += Math.Sqrt(dx * dx + dy * dy);
            }

            // FIX Bug 7: pipe label search restricted to same layout
            var midE  = (first.E + last.E) / 2;
            var midN  = (first.N + last.N) / 2;
            var label = FindNearestPipeLabel(midE, midN, snapToleranceM * 20, row.SourceLayout);

            pipeIndex++;
            var conn = new ConnectionRecord
            {
                Id         = $"PIPE-{pipeIndex:D3}",
                FromNodeId = fromNode.Id,
                ToNodeId   = toNode.Id,
                Length     = Math.Round(label?.Length ?? arcLength, 3),
                Slope      = label?.Slope,
                Diameter   = label?.Diameter
            };
            connections.Add(conn);

            var ic         = CultureInfo.InvariantCulture;
            var connStr    = $"To:{toNode.Id}|L:{conn.Length.ToString("F3", ic)}";
            var reverseStr = $"To:{fromNode.Id}|L:{conn.Length.ToString("F3", ic)}";

            if (!fromNode.Connections.Contains(connStr))    fromNode.Connections.Add(connStr);
            if (!toNode.Connections.Contains(reverseStr))   toNode.Connections.Add(reverseStr);
        }

        return (nodes, connections);
    }

    // ─── Topology helpers ─────────────────────────────────────
    private LayerRole GetRole(string layer) =>
        _layerRoles.TryGetValue(layer, out var r) ? r : LayerRole.None;

    // FIX Bug 7: only matches text entities in the same layout
    private string FindNearestText(double e, double n, double radius, string layoutName)
    {
        return _textEntities
            .Where(t => t.SourceLayout == layoutName)
            .Select(t => (dist: Dist(t.E, t.N, e, n), t.Value))
            .Where(x => x.dist <= radius)
            .OrderBy(x => x.dist)
            .Select(x => x.Value)
            .FirstOrDefault() ?? "";
    }

    private static NodeRecord? FindNearestNode(List<NodeRecord> nodes, double e, double n, double tolerance)
    {
        return nodes
            .Select(nd => (dist: Dist(nd.E, nd.N, e, n), nd))
            .Where(x => x.dist <= tolerance)
            .OrderBy(x => x.dist)
            .Select(x => x.nd)
            .FirstOrDefault();
    }

    // FIX Bug 7: only matches pipe labels in the same layout
    private PipeLabelEntity? FindNearestPipeLabel(double e, double n, double radius, string layoutName)
    {
        return _pipeLabels
            .Where(pl => pl.SourceLayout == layoutName)
            .Select(pl => (dist: Dist(pl.E, pl.N, e, n), pl))
            .Where(x => x.dist <= radius)
            .OrderBy(x => x.dist)
            .Select(x => x.pl)
            .FirstOrDefault();
    }

    private static double Dist(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2; var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // ─── AutoCAD Export Button Handler ────────────────────────
    private void btnExportAutoCAD_Click(object sender, EventArgs e)
    {
        if (_extractedAutoCADData == null)
        {
            MessageBox.Show(
                "No AutoCAD data to export.\n\n" +
                "Steps:\n" +
                "1. Open a DXF file in AutoCAD\n" +
                "2. Select objects in AutoCAD\n" +
                "3. Run 'getData' command in AutoCAD\n" +
                "4. This app will receive the data\n" +
                "5. Click this button to export",
                "No AutoCAD Data",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Export AutoCAD Data as JSON",
            Filter = "JSON Files (*.json)|*.json",
            FileName = "sitetrack_data.json"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        ExportAutoCADDataAsJson(dlg.FileName);
    }

    // ─── AutoCAD Export ───────────────────────────────────────
    private void ExportAutoCADDataAsJson(string baseFileName)
    {
        if (_extractedAutoCADData == null)
        {
            MessageBox.Show("No AutoCAD data to export.", "No Data",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var path = baseFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? baseFileName
                : baseFileName + ".json";

            WriteAutoCADJson(path, _extractedAutoCADData);

            lblStatus.Text = $"✓ Exported to {Path.GetFileName(path)}";

            if (MessageBox.Show("JSON exported. Open it now?", "Done",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void WriteAutoCADJson(string path, AutoCADExportData data)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine("{");
        sb.AppendLine("  \"schemaVersion\": 1,");
        sb.AppendLine($"  \"exportedAt\": \"{DateTime.UtcNow:O}\",");
        sb.AppendLine($"  \"source\": \"{JsonEscape(data.Source)}\",");
        sb.AppendLine($"  \"coordinateSystemHint\": \"{JsonEscape(data.CoordinateSystemHint)}\",");

        // Points
        sb.AppendLine("  \"points\": [");
        for (var i = 0; i < data.Points.Count; i++)
        {
            var pt = data.Points[i];
            var comma = i < data.Points.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(pt.Id)}\",");
            sb.AppendLine($"      \"name\": \"{JsonEscape(pt.Name)}\",");
            sb.AppendLine($"      \"E\": {pt.E.ToString("F6", ic)},");
            sb.AppendLine($"      \"N\": {pt.N.ToString("F6", ic)},");
            sb.AppendLine($"      \"layer\": \"{JsonEscape(pt.Layer)}\",");
            sb.AppendLine("      \"properties\": {}");
            sb.AppendLine($"    }}{comma}");
        }
        sb.AppendLine("  ],");

        // Lines (with explicit coordinates)
        sb.AppendLine("  \"lines\": [");
        for (var i = 0; i < data.Lines.Count; i++)
        {
            var ln = data.Lines[i];
            ln.CalculateLength();
            var comma = i < data.Lines.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(ln.Id)}\",");
            sb.AppendLine($"      \"name\": \"{JsonEscape(ln.Name)}\",");
            sb.AppendLine($"      \"startE\": {ln.StartE.ToString("F6", ic)},");
            sb.AppendLine($"      \"startN\": {ln.StartN.ToString("F6", ic)},");
            sb.AppendLine($"      \"endE\": {ln.EndE.ToString("F6", ic)},");
            sb.AppendLine($"      \"endN\": {ln.EndN.ToString("F6", ic)},");
            sb.AppendLine($"      \"length\": {ln.Length.ToString("F3", ic)},");
            sb.AppendLine($"      \"layer\": \"{JsonEscape(ln.Layer)}\",");
            sb.AppendLine("      \"properties\": {}");
            sb.AppendLine($"    }}{comma}");
        }
        sb.AppendLine("  ],");

        // Curves
        sb.AppendLine("  \"curves\": [");
        for (var i = 0; i < data.Curves.Count; i++)
        {
            var cv = data.Curves[i];
            var comma = i < data.Curves.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(cv.Id)}\",");
            sb.AppendLine($"      \"name\": \"{JsonEscape(cv.Name)}\",");
            sb.AppendLine($"      \"centerE\": {cv.CenterE.ToString("F6", ic)},");
            sb.AppendLine($"      \"centerN\": {cv.CenterN.ToString("F6", ic)},");
            sb.AppendLine($"      \"radius\": {cv.Radius.ToString("F3", ic)},");
            sb.AppendLine($"      \"startAngle\": {cv.StartAngle.ToString("F2", ic)},");
            sb.AppendLine($"      \"endAngle\": {cv.EndAngle.ToString("F2", ic)},");
            sb.AppendLine($"      \"layer\": \"{JsonEscape(cv.Layer)}\",");
            sb.AppendLine("      \"properties\": {}");
            sb.AppendLine($"    }}{comma}");
        }
        sb.AppendLine("  ],");

        // Texts
        sb.AppendLine("  \"texts\": [");
        for (var i = 0; i < data.Texts.Count; i++)
        {
            var tx = data.Texts[i];
            var comma = i < data.Texts.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(tx.Id)}\",");
            sb.AppendLine($"      \"content\": \"{JsonEscape(tx.Content)}\",");
            sb.AppendLine($"      \"E\": {tx.E.ToString("F6", ic)},");
            sb.AppendLine($"      \"N\": {tx.N.ToString("F6", ic)},");
            sb.AppendLine($"      \"layer\": \"{JsonEscape(tx.Layer)}\",");
            sb.AppendLine("      \"properties\": {}");
            sb.AppendLine($"    }}{comma}");
        }
        sb.AppendLine("  ]");

        sb.AppendLine("}");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    // ─── CSV / JSON builders ──────────────────────────────────
    private static string BuildNodesCsv(List<NodeRecord> nodes)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("name,E,N,Zone,connections");
        foreach (var nd in nodes)
        {
            var connStr = nd.Connections.Count > 0
                ? EscapeCsvField(string.Join("; ", nd.Connections))
                : "";
            sb.AppendLine(
                $"{EscapeCsvField(nd.Id)}," +
                $"{nd.E.ToString("F6", ic)}," +
                $"{nd.N.ToString("F6", ic)}," +
                $"{EscapeCsvField(nd.Layer)},{connStr}");
        }
        return sb.ToString();
    }

    private static string BuildConnectionsCsv(List<ConnectionRecord> connections)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("id,from,to,length,slope,diameter");
        foreach (var c in connections)
        {
            sb.AppendLine(
                $"{EscapeCsvField(c.Id)},{EscapeCsvField(c.FromNodeId)},{EscapeCsvField(c.ToNodeId)}," +
                $"{c.Length.ToString("F3", ic)}," +
                $"{(c.Slope.HasValue    ? c.Slope.Value.ToString("F2", ic)    : "")}," +
                $"{(c.Diameter.HasValue ? c.Diameter.Value.ToString("F0", ic) : "")}");
        }
        return sb.ToString();
    }

    // FIX Bug 4: all numeric values use InvariantCulture — guarantees '.' decimal separator in JSON output
    // Before: {nd.E:F6} used current culture — produced invalid JSON on comma-decimal locales (e.g. "E": 335684,045)
    private void WriteNetworkJson(string path, List<NodeRecord> nodes, List<ConnectionRecord> connections)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"schemaVersion\": 1,");
        sb.AppendLine($"  \"exportedAt\": \"{DateTime.UtcNow:O}\",");
        sb.AppendLine($"  \"sourceFileName\": \"{JsonEscape(Path.GetFileName(_dxfFilePath))}\",");
        sb.AppendLine("  \"coordinateSystemHint\": \"UTM\",");
        sb.AppendLine("  \"points\": [");

        for (var i = 0; i < nodes.Count; i++)
        {
            var nd    = nodes[i];
            var comma = i < nodes.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(nd.Id)}\",");
            sb.AppendLine($"      \"name\": \"{JsonEscape(nd.Id)}\",");
            sb.AppendLine($"      \"E\": {nd.E.ToString("F6", ic)},");
            sb.AppendLine($"      \"N\": {nd.N.ToString("F6", ic)},");
            sb.AppendLine($"      \"layer\": \"{JsonEscape(nd.Layer)}\",");
            sb.AppendLine("      \"properties\": {}");
            sb.AppendLine($"    }}{comma}");
        }

        sb.AppendLine("  ],");
        sb.AppendLine("  \"connections\": [");

        for (var i = 0; i < connections.Count; i++)
        {
            var c     = connections[i];
            var comma = i < connections.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(c.Id)}\",");
            sb.AppendLine($"      \"fromPointId\": \"{JsonEscape(c.FromNodeId)}\",");
            sb.AppendLine($"      \"toPointId\": \"{JsonEscape(c.ToNodeId)}\",");
            sb.AppendLine($"      \"length\": {c.Length.ToString("F3", ic)},");
            sb.AppendLine("      \"properties\": {");
            sb.AppendLine($"        \"slope\": {(c.Slope.HasValue    ? c.Slope.Value.ToString("F2", ic)    : "null")},");
            sb.AppendLine($"        \"diameter\": {(c.Diameter.HasValue ? c.Diameter.Value.ToString("F0", ic) : "null")}");
            sb.AppendLine("      }");
            sb.AppendLine($"    }}{comma}");
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static string JsonEscape(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                case '\b': sb.Append("\\b");  break;
                case '\f': sb.Append("\\f");  break;
                default:
                    if (c < 32) sb.AppendFormat("\\u{0:X4}", (int)c);
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    // ─── CSV escaping ─────────────────────────────────────────
    private static string NotesColumn(string layoutName, string? extraSuffix)
    {
        var raw = string.IsNullOrEmpty(extraSuffix)
            ? $"Layout={layoutName}"
            : $"Layout={layoutName};{extraSuffix}";
        return EscapeCsvField(raw);
    }

    private static string EscapeCsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static string EscapeCsv(string layer)
    {
        if (layer.Contains(',') || layer.Contains('"'))
            return "\"" + layer.Replace("\"", "\"\"") + "\"";
        return layer;
    }

    // ─── Drag & drop ──────────────────────────────────────────
    private void Form1_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void Form1_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
        if (files[0].EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
            LoadDxf(files[0]);
    }

    private void dgvEntities_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dgvEntities.IsCurrentCellDirty && dgvEntities.CurrentCell is DataGridViewCheckBoxCell)
            dgvEntities.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }
}

// ──────────────────────────────────────────────────────────────
//  Entity row (data-bound to grid)
// ──────────────────────────────────────────────────────────────
public sealed class EntityRow : INotifyPropertyChanged
{
    private bool _isSelected;
    private LayerRole _layerRole;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public LayerRole LayerRole
    {
        get => _layerRole;
        set
        {
            if (_layerRole == value) return;
            _layerRole = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LayerRole)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LayerRoleDisplay)));
        }
    }

    public string LayerRoleDisplay => _layerRole == LayerRole.None ? "" : _layerRole.ToString();

    public string SourceLayout { get; set; } = "";
    public string EntityType   { get; set; } = "";
    public string Layer        { get; set; } = "";
    public string Label        { get; set; } = "";
    public string CoordSummary { get; set; } = "";
    public List<string> CsvRows { get; set; } = new();

    // AutoCAD Bridge: coordinates for display from AutoCAD data
    public string X { get; set; } = "";
    public string Y { get; set; } = "";
    public string Z { get; set; } = "";

    // FIX Bug 2: raw insert position — used directly by topology engine (no CoordSummary parsing)
    public double RawE { get; set; }
    public double RawN { get; set; }

    // FIX Bug 1: raw polyline vertices — used directly by topology engine (no CSV re-parsing)
    public List<(double E, double N)>? RawVertices { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
