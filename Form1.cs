using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using netDxf;
using netDxf.Blocks;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
using DxfPoint = netDxf.Entities.Point;

namespace DxfCoordinateExtractor;

public partial class Form1 : Form
{
    private DxfDocument? _dxfDoc;
    private readonly List<EntityRow> _allRows = new();

    public Form1()
    {
        InitializeComponent();
        SetupGrid();
    }

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
            Name = "colLayoutTab",
            HeaderText = "Layout",
            DataPropertyName = nameof(EntityRow.SourceLayout),
            Width = 90,
            ReadOnly = true
        });
        dgvEntities.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLayer",
            HeaderText = "Layer",
            DataPropertyName = nameof(EntityRow.Layer),
            Width = 100,
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
                    "Open it in AutoCAD 2007 and re-save as AutoCAD 2007 DXF.",
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

            lblFile.Text = Path.GetFileName(path);
            ParseAllEntities();
            PopulateLayerFilter();
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

    private void ParseAllEntities()
    {
        _allRows.Clear();
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
            .OrderBy(l => string.Equals(l.Name, netDxf.Objects.Layout.ModelSpaceName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(l => l.TabOrder)
            .ThenBy(l => l.Name, StringComparer.OrdinalIgnoreCase);
    }

    private void ParseEntitiesInBlock(string layoutName, Block block)
    {
        var entities = block.Entities;
        for (var i = 0; i < entities.Count; i++)
        {
            switch (entities[i])
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
            }
        }
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
        var notes = NotesColumn(layoutName, string.IsNullOrEmpty(blockName) ? null : $"Block={blockName}");
        _allRows.Add(new EntityRow
        {
            SourceLayout = layoutName,
            EntityType = "Insert",
            Layer = ins.Layer.Name,
            Label = $"Block \"{blockName}\" @ ({ins.Position.X:F3}, {ins.Position.Y:F3})",
            CoordSummary = $"X={ins.Position.X:F4}, Y={ins.Position.Y:F4}, Z={ins.Position.Z:F4}",
            CsvRows = new List<string>
            {
                $"Insert,{EscapeCsv(ins.Layer.Name)},InsertionPoint,{ins.Position.X:F6},{ins.Position.Y:F6},{notes}"
            }
        });
    }

    /// <summary>Last CSV column: Layout=tab[;key=value…], quoted if needed.</summary>
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
        var typeFilter = cmbType.SelectedIndex <= 0 ? null : cmbType.SelectedItem?.ToString();
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

    private void btnSelectAll_Click(object sender, EventArgs e)
    {
        SetAllCheckboxes(true);
    }

    private void btnSelectNone_Click(object sender, EventArgs e)
    {
        SetAllCheckboxes(false);
    }

    private void SetAllCheckboxes(bool value)
    {
        foreach (DataGridViewRow row in dgvEntities.Rows)
        {
            if (row.DataBoundItem is EntityRow entity)
                entity.IsSelected = value;
        }

        dgvEntities.Refresh();
    }

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
            {
                foreach (var row in entity.CsvRows)
                    sb.AppendLine(row);
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            lblStatus.Text = $"Exported {selected.Count} entities → {Path.GetFileName(dlg.FileName)}";

            if (MessageBox.Show("CSV saved. Open it now?", "Done",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed:\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

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

public sealed class EntityRow : INotifyPropertyChanged
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public string SourceLayout { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string Layer { get; set; } = "";
    public string Label { get; set; } = "";
    public string CoordSummary { get; set; } = "";
    public List<string> CsvRows { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}
