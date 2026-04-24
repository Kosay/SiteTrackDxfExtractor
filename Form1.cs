using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using netDxf;
using netDxf.Blocks;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
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

internal sealed class TextEntity
{
    public double E { get; set; }
    public double N { get; set; }
    public string Value { get; set; } = "";
}

internal sealed class PipeLabelEntity
{
    public double E { get; set; }
    public double N { get; set; }
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

    private DxfDocument? _dxfDoc;
    private string _dxfFilePath = "";
    private readonly List<EntityRow> _allRows = new();
    private readonly List<TextEntity> _textEntities = new();
    private readonly List<PipeLabelEntity> _pipeLabels = new();
    private readonly Dictionary<string, LayerRole> _layerRoles = new();

    public Form1()
    {
        InitializeComponent();
        SetupGrid();
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
            Name = "colSelect",
            HeaderText = "\u2713",
            Width = 35,
            DataPropertyName = nameof(EntityRow.IsSelected)
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colType",
            HeaderText = "Type",
            DataPropertyName = nameof(EntityRow.EntityType),
            Width = 85,
            ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colRole",
            HeaderText = "Role",
            DataPropertyName = nameof(EntityRow.LayerRoleDisplay),
            Width = 60,
            ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLayoutTab",
            HeaderText = "Layout",
            DataPropertyName = nameof(EntityRow.SourceLayout),
            Width = 80,
            ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLayer",
            HeaderText = "Layer",
            DataPropertyName = nameof(EntityRow.Layer),
            Width = 160,
            ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLabel",
            HeaderText = "Label / Description",
            DataPropertyName = nameof(EntityRow.Label),
            Width = 200,
            ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colCoords",
            HeaderText = "Coordinates",
            DataPropertyName = nameof(EntityRow.CoordSummary),
            Width = 260,
            ReadOnly = true
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
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

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

    // ─── Parsing ──────────────────────────────────────────────
    private void ParseAllEntities()
    {
        _allRows.Clear();
        _textEntities.Clear();
        _pipeLabels.Clear();

        if (_dxfDoc == null)
            return;

        foreach (var layout in OrderLayouts(_dxfDoc.Layouts))
        {
            var block = layout.AssociatedBlock;
            if (block == null)
                continue;
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
                case Line line:
                    AddLine(layoutName, line);
                    break;
                case DxfPoint pt:
                    AddPoint(layoutName, pt);
                    break;
                case Circle circle:
                    AddCircle(layoutName, circle);
                    break;
                case Polyline2D poly:
                    AddPolyline2D(layoutName, poly);
                    break;
                case Insert ins:
                    AddInsert(layoutName, ins);
                    break;
                case netDxf.Entities.Text txt:
                    CollectText(txt.Value, txt.Position.X, txt.Position.Y);
                    break;
                case MText mtext:
                    CollectText(mtext.Value, mtext.Position.X, mtext.Position.Y);
                    break;
            }
        }
    }

    // Collect text entities for spatial name-matching
    private void CollectText(string value, double x, double y)
    {
        var clean = CleanMTextCodes(value).Trim();
        if (!string.IsNullOrEmpty(clean))
            _textEntities.Add(new TextEntity { E = x, N = y, Value = clean });
    }

    // Strip basic MText formatting codes ({\f...; \H...; \C...; \P etc.)
    private static string CleanMTextCodes(string s)
    {
        s = Regex.Replace(s, @"\{\\[^}]*\}", "");  // {\ ... } blocks
        s = Regex.Replace(s, @"\\[A-Za-z][^;]*;", ""); // \code;
        s = s.Replace(@"\P", "\n").Replace(@"\~", " ").Replace(@"%%c", "Ø")
             .Replace(@"%%C", "Ø").Replace(@"%%d", "°").Replace(@"%%p", "±");
        return s;
    }

    private void AddLine(string layoutName, Line line)
    {
        var notes = NotesColumn(layoutName, null);
        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType = "Line",
            Layer = line.Layer.Name,
            Label = $"Start({line.StartPoint.X:F3}, {line.StartPoint.Y:F3}) → End({line.EndPoint.X:F3}, {line.EndPoint.Y:F3})",
            CoordSummary = $"X1={line.StartPoint.X:F4}, Y1={line.StartPoint.Y:F4}, X2={line.EndPoint.X:F4}, Y2={line.EndPoint.Y:F4}",
            CsvRows = new List<string>
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
            EntityType = "Point",
            Layer = pt.Layer.Name,
            Label = $"({pt.Position.X:F3}, {pt.Position.Y:F3})",
            CoordSummary = $"X={pt.Position.X:F4}, Y={pt.Position.Y:F4}",
            CsvRows = new List<string>
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
            EntityType = "Circle",
            Layer = circle.Layer.Name,
            Label = $"Center({circle.Center.X:F3}, {circle.Center.Y:F3}) R={circle.Radius:F3}",
            CoordSummary = $"CX={circle.Center.X:F4}, CY={circle.Center.Y:F4}, R={circle.Radius:F4}",
            CsvRows = new List<string>
            {
                $"Circle,{EscapeCsv(circle.Layer.Name)},Center,{circle.Center.X:F6},{circle.Center.Y:F6},{notes}"
            }
        });
    }

    private void AddPolyline2D(string layoutName, Polyline2D poly)
    {
        var csvRows = new List<string>();
        var vertexParts = new List<string>();
        var verts = poly.Vertexes;
        var notes = NotesColumn(layoutName, null);
        for (var i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            csvRows.Add(
                $"Polyline2D,{EscapeCsv(poly.Layer.Name)},Vertex{i + 1},{v.Position.X:F6},{v.Position.Y:F6},{notes}");
            vertexParts.Add($"V{i + 1}({v.Position.X:F2},{v.Position.Y:F2})");
        }

        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType = "Polyline2D",
            Layer = poly.Layer.Name,
            Label = $"{verts.Count} vertices" + (poly.IsClosed ? " [Closed]" : " [Open]"),
            CoordSummary = string.Join(" → ", vertexParts.Take(3)) +
                           (vertexParts.Count > 3 ? $" ... +{vertexParts.Count - 3} more" : ""),
            CsvRows = csvRows
        });
    }

    private void AddInsert(string layoutName, Insert ins)
    {
        var blockName = ins.Block?.Name ?? "";

        // Check if block name contains pipe-label metadata (FLOWARROW-type blocks)
        var pipeLabelMeta = TryParsePipeLabel(blockName);

        // Also collect the insert's own Name attribute (set from text near the block)
        var labelText = ins.Block?.Name ?? "";

        // Gather block attributes (ATTRIB entities embedded in the insert)
        var attrText = "";
        if (ins.Attributes.Count > 0)
        {
            var parts = ins.Attributes
                .Select(a => $"{a.Tag}={a.Value}")
                .ToList();
            attrText = string.Join("; ", parts);
            labelText = ins.Attributes.FirstOrDefault()?.Value?.ToString() ?? labelText;
        }

        var notes = NotesColumn(layoutName, string.IsNullOrEmpty(blockName) ? null : $"Block={blockName}");

        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType = "Insert",
            Layer = ins.Layer.Name,
            Label = $"Block \"{blockName}\" @ ({ins.Position.X:F3}, {ins.Position.Y:F3})" +
                    (string.IsNullOrEmpty(attrText) ? "" : $"  [{attrText}]"),
            CoordSummary = $"X={ins.Position.X:F4}, Y={ins.Position.Y:F4}, Z={ins.Position.Z:F4}",
            CsvRows = new List<string>
            {
                $"Insert,{EscapeCsv(ins.Layer.Name)},InsertionPoint,{ins.Position.X:F6},{ins.Position.Y:F6},{notes}"
            }
        });

        // If this insert carries pipe label data, store it separately for topology matching
        if (pipeLabelMeta != null)
        {
            pipeLabelMeta.E = ins.Position.X;
            pipeLabelMeta.N = ins.Position.Y;
            _pipeLabels.Add(pipeLabelMeta);
        }
    }

    // Parse pipe metadata from names like "L=93.97m S=0.50%" or "%%C500 GRP"
    private static PipeLabelEntity? TryParsePipeLabel(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var mL = LengthRx.Match(name);
        var mS = SlopeRx.Match(name);
        var mD = DiameterRx.Match(name);

        if (!mL.Success && !mS.Success && !mD.Success) return null;

        return new PipeLabelEntity
        {
            Length   = mL.Success ? double.Parse(mL.Groups[1].Value) : null,
            Slope    = mS.Success ? double.Parse(mS.Groups[1].Value) : null,
            Diameter = mD.Success ? double.Parse(mD.Groups[1].Value) : null
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
        if (lstLayers.Items.Count > 0)
            lstLayers.SelectedIndex = 0;
    }

    private void lstLayers_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstLayers.SelectedItem is string layer && _layerRoles.TryGetValue(layer, out var role))
            cmbRole.SelectedIndex = (int)role;
    }

    private void cmbRole_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstLayers.SelectedItem is string layer)
        {
            _layerRoles[layer] = (LayerRole)cmbRole.SelectedIndex;
            // Refresh the Role column in the grid
            foreach (DataGridViewRow row in dgvEntities.Rows)
            {
                if (row.DataBoundItem is EntityRow er && er.Layer == layer)
                    er.LayerRole = _layerRoles[layer];
            }
            dgvEntities.Refresh();
        }
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
    private void cmbType_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        var layerFilter = cmbLayer.SelectedIndex <= 0 ? null : cmbLayer.SelectedItem?.ToString();
        var typeFilter  = cmbType.SelectedIndex <= 0  ? null : cmbType.SelectedItem?.ToString();

        // Apply layer roles to all rows
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
            Title = "Save CSV",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = "dxf_coordinates.csv"
        };
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

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
        // Validate that at least one Node and one Pipe layer is assigned
        var hasNodes = _layerRoles.Values.Any(r => r == LayerRole.Node);
        var hasPipes = _layerRoles.Values.Any(r => r == LayerRole.Pipe);

        if (!hasNodes)
        {
            MessageBox.Show(
                "No Node layers assigned.\n\nIn the Layer Roles panel, select each manhole/node layer and set its role to 'Node'.",
                "Missing Node Layers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!hasPipes)
        {
            MessageBox.Show(
                "No Pipe layers assigned.\n\nIn the Layer Roles panel, select each pipe/polyline layer and set its role to 'Pipe'.",
                "Missing Pipe Layers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Save SiteTrack Export (base name)",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = Path.GetFileNameWithoutExtension(_dxfFilePath) + "_nodes.csv"
        };
        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            lblStatus.Text = "Building network topology...";
            Application.DoEvents();

            var basePath = Path.Combine(
                Path.GetDirectoryName(dlg.FileName)!,
                Path.GetFileNameWithoutExtension(dlg.FileName).Replace("_nodes", ""));

            var tolerance = (double)nudTolerance.Value;
            var textRadius = (double)nudTextRadius.Value;

            var (nodes, connections) = BuildTopology(tolerance, textRadius);

            // Write nodes CSV
            var nodesCsv = BuildNodesCsv(nodes);
            var nodesPath = basePath + "_nodes.csv";
            File.WriteAllText(nodesPath, nodesCsv, new UTF8Encoding(false));

            // Write connections CSV
            var connCsv = BuildConnectionsCsv(connections);
            var connPath = basePath + "_connections.csv";
            File.WriteAllText(connPath, connCsv, new UTF8Encoding(false));

            // Write network JSON
            var jsonPath = basePath + "_network.json";
            WriteNetworkJson(jsonPath, nodes, connections);

            lblStatus.Text = $"Exported {nodes.Count} nodes, {connections.Count} connections.";
            MessageBox.Show(
                $"SiteTrack export complete:\n\n" +
                $"  Nodes:       {Path.GetFileName(nodesPath)}\n" +
                $"  Connections: {Path.GetFileName(connPath)}\n" +
                $"  JSON:        {Path.GetFileName(jsonPath)}\n\n" +
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
        // 1. Collect node insert positions from Node-role layers
        var nodes = new List<NodeRecord>();
        var nodeIndex = 0;
        var nodeRows = _allRows
            .Where(r => GetRole(r.Layer) == LayerRole.Node && r.EntityType == "Insert")
            .ToList();

        foreach (var row in nodeRows)
        {
            // Parse E/N from the CoordSummary (stored as "X=..., Y=..., Z=...")
            if (!TryParseXY(row.CoordSummary, out var e, out var n)) continue;

            // Find nearest text label within radius for the node name
            var name = FindNearestText(e, n, textRadiusM);
            if (string.IsNullOrEmpty(name))
            {
                nodeIndex++;
                name = $"MH-{nodeIndex:D3}";
            }

            nodes.Add(new NodeRecord { Id = name, E = e, N = n, Layer = row.Layer });
        }

        // 2. Collect polyline pipe geometries from Pipe-role layers
        var connections = new List<ConnectionRecord>();
        var pipeRows = _allRows
            .Where(r => GetRole(r.Layer) == LayerRole.Pipe && r.EntityType == "Polyline2D")
            .ToList();

        var pipeIndex = 0;
        foreach (var row in pipeRows)
        {
            // Re-parse the CsvRows to get all vertices
            var vertices = row.CsvRows
                .Select(csv => ParseCsvVertex(csv))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (vertices.Count < 2) continue;

            var first = vertices[0];
            var last  = vertices[vertices.Count - 1];

            var fromNode = FindNearestNode(nodes, first.e, first.n, snapToleranceM);
            var toNode   = FindNearestNode(nodes, last.e, last.n, snapToleranceM);

            if (fromNode == null || toNode == null) continue;
            if (fromNode.Id == toNode.Id) continue;

            // Calculate polyline arc length
            var arcLength = 0.0;
            for (var i = 1; i < vertices.Count; i++)
            {
                var dx = vertices[i].e - vertices[i - 1].e;
                var dy = vertices[i].n - vertices[i - 1].n;
                arcLength += Math.Sqrt(dx * dx + dy * dy);
            }

            // Find nearest pipe label (FLOWARROW) for slope/diameter metadata
            var midE = (first.e + last.e) / 2;
            var midN = (first.n + last.n) / 2;
            var label = FindNearestPipeLabel(midE, midN, snapToleranceM * 20);

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

            // Register connection in both nodes' connection lists
            var connStr = $"To:{toNode.Id}|L:{conn.Length:F3}";
            if (!fromNode.Connections.Contains(connStr))
                fromNode.Connections.Add(connStr);

            var reverseStr = $"To:{fromNode.Id}|L:{conn.Length:F3}";
            if (!toNode.Connections.Contains(reverseStr))
                toNode.Connections.Add(reverseStr);
        }

        return (nodes, connections);
    }

    // ─── Topology helpers ─────────────────────────────────────
    private LayerRole GetRole(string layer) =>
        _layerRoles.TryGetValue(layer, out var r) ? r : LayerRole.None;

    private string FindNearestText(double e, double n, double radius)
    {
        return _textEntities
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

    private PipeLabelEntity? FindNearestPipeLabel(double e, double n, double radius)
    {
        return _pipeLabels
            .Select(pl => (dist: Dist(pl.E, pl.N, e, n), pl))
            .Where(x => x.dist <= radius)
            .OrderBy(x => x.dist)
            .Select(x => x.pl)
            .FirstOrDefault();
    }

    private static double Dist(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Parse "X=335684.045, Y=2678151.885, Z=0" from CoordSummary
    private static bool TryParseXY(string coordSummary, out double e, out double n)
    {
        e = 0; n = 0;
        var mX = Regex.Match(coordSummary, @"X=([\d.]+)");
        var mY = Regex.Match(coordSummary, @"Y=([\d.]+)");
        if (!mX.Success || !mY.Success) return false;
        e = double.Parse(mX.Groups[1].Value);
        n = double.Parse(mY.Groups[1].Value);
        return true;
    }

    // Parse vertex from CSV row: "Polyline2D,layer,VertexN,E,N,notes"
    private static (double e, double n)? ParseCsvVertex(string csvRow)
    {
        var parts = csvRow.Split(',');
        if (parts.Length < 5) return null;
        if (!double.TryParse(parts[3], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var e)) return null;
        if (!double.TryParse(parts[4], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var n)) return null;
        return (e, n);
    }

    // ─── CSV / JSON builders ──────────────────────────────────
    private static string BuildNodesCsv(List<NodeRecord> nodes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("name,E,N,Zone,connections");
        foreach (var nd in nodes)
        {
            var connStr = nd.Connections.Count > 0
                ? EscapeCsvField(string.Join("; ", nd.Connections))
                : "";
            sb.AppendLine($"{EscapeCsvField(nd.Id)},{nd.E:F6},{nd.N:F6},{EscapeCsvField(nd.Layer)},{connStr}");
        }
        return sb.ToString();
    }

    private static string BuildConnectionsCsv(List<ConnectionRecord> connections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("id,from,to,length,slope,diameter");
        foreach (var c in connections)
        {
            sb.AppendLine(
                $"{EscapeCsvField(c.Id)},{EscapeCsvField(c.FromNodeId)},{EscapeCsvField(c.ToNodeId)}," +
                $"{c.Length:F3},{c.Slope?.ToString("F2") ?? ""},{c.Diameter?.ToString("F0") ?? ""}");
        }
        return sb.ToString();
    }

    private void WriteNetworkJson(string path, List<NodeRecord> nodes, List<ConnectionRecord> connections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"schemaVersion\": 1,");
        sb.AppendLine($"  \"exportedAt\": \"{DateTime.UtcNow:O}\",");
        sb.AppendLine($"  \"sourceFileName\": \"{JsonEscape(Path.GetFileName(_dxfFilePath))}\",");
        sb.AppendLine($"  \"coordinateSystemHint\": \"UTM\",");

        // Points array
        sb.AppendLine("  \"points\": [");
        for (var i = 0; i < nodes.Count; i++)
        {
            var nd = nodes[i];
            var comma = i < nodes.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(nd.Id)}\",");
            sb.AppendLine($"      \"name\": \"{JsonEscape(nd.Id)}\",");
            sb.AppendLine($"      \"E\": {nd.E:F6},");
            sb.AppendLine($"      \"N\": {nd.N:F6},");
            sb.AppendLine($"      \"layer\": \"{JsonEscape(nd.Layer)}\"");
            sb.AppendLine($"    }}{comma}");
        }
        sb.AppendLine("  ],");

        // Connections array
        sb.AppendLine("  \"connections\": [");
        for (var i = 0; i < connections.Count; i++)
        {
            var c = connections[i];
            var comma = i < connections.Count - 1 ? "," : "";
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": \"{JsonEscape(c.Id)}\",");
            sb.AppendLine($"      \"fromPointId\": \"{JsonEscape(c.FromNodeId)}\",");
            sb.AppendLine($"      \"toPointId\": \"{JsonEscape(c.ToNodeId)}\",");
            sb.AppendLine($"      \"length\": {c.Length:F3},");
            sb.AppendLine("      \"properties\": {");
            sb.AppendLine($"        \"slope\": {(c.Slope.HasValue ? c.Slope.Value.ToString("F2") : "null")},");
            sb.AppendLine($"        \"diameter\": {(c.Diameter.HasValue ? c.Diameter.Value.ToString("F0") : "null")}");
            sb.AppendLine("      }");
            sb.AppendLine($"    }}{comma}");
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static string JsonEscape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

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
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return;
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
    public string EntityType { get; set; } = "";
    public string Layer { get; set; } = "";
    public string Label { get; set; } = "";
    public string CoordSummary { get; set; } = "";
    public List<string> CsvRows { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}
