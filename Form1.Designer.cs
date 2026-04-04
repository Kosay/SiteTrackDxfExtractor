#nullable enable
namespace DxfCoordinateExtractor;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;
    private Panel pnlTop = null!;
    private Button btnOpen = null!;
    private Label lblFile = null!;
    private Label label1 = null!;
    private ComboBox cmbLayer = null!;
    private Label label2 = null!;
    private ComboBox cmbType = null!;
    private Button btnSelectAll = null!;
    private Button btnSelectNone = null!;
    private DataGridView dgvEntities = null!;
    private Panel pnlBottom = null!;
    private Button btnExport = null!;
    private Label lblStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlTop = new Panel();
        btnOpen = new Button();
        lblFile = new Label();
        label1 = new Label();
        cmbLayer = new ComboBox();
        label2 = new Label();
        cmbType = new ComboBox();
        btnSelectAll = new Button();
        btnSelectNone = new Button();
        dgvEntities = new DataGridView();
        pnlBottom = new Panel();
        btnExport = new Button();
        lblStatus = new Label();
        pnlTop.SuspendLayout();
        pnlBottom.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvEntities).BeginInit();
        SuspendLayout();

        pnlTop.BackColor = Color.FromArgb(30, 42, 58);
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Height = 110;
        pnlTop.Padding = new Padding(12, 10, 12, 10);
        pnlTop.Controls.AddRange(new Control[] {
            btnOpen, lblFile, label1, cmbLayer, label2, cmbType, btnSelectAll, btnSelectNone
        });

        btnOpen.Text = "Open DXF File";
        btnOpen.Location = new Point(12, 12);
        btnOpen.Size = new Size(150, 36);
        btnOpen.FlatStyle = FlatStyle.Flat;
        btnOpen.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
        btnOpen.BackColor = Color.FromArgb(0, 120, 215);
        btnOpen.ForeColor = Color.White;
        btnOpen.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnOpen.Cursor = Cursors.Hand;
        btnOpen.Click += btnOpen_Click;

        lblFile.Text = "No file loaded — drag and drop a DXF here or click Open";
        lblFile.Location = new Point(172, 20);
        lblFile.Size = new Size(500, 20);
        lblFile.ForeColor = Color.FromArgb(180, 200, 220);
        lblFile.Font = new Font("Segoe UI", 9F);

        label1.Text = "Layer:";
        label1.Location = new Point(12, 62);
        label1.Size = new Size(45, 22);
        label1.ForeColor = Color.FromArgb(180, 200, 220);
        label1.Font = new Font("Segoe UI", 9F);

        cmbLayer.Location = new Point(60, 60);
        cmbLayer.Size = new Size(180, 24);
        cmbLayer.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLayer.Font = new Font("Segoe UI", 9F);
        cmbLayer.SelectedIndexChanged += cmbLayer_SelectedIndexChanged;

        label2.Text = "Type:";
        label2.Location = new Point(255, 62);
        label2.Size = new Size(40, 22);
        label2.ForeColor = Color.FromArgb(180, 200, 220);
        label2.Font = new Font("Segoe UI", 9F);

        cmbType.Location = new Point(298, 60);
        cmbType.Size = new Size(140, 24);
        cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbType.Font = new Font("Segoe UI", 9F);
        cmbType.Items.AddRange(new object[] { "All Types", "Line", "Point", "Circle", "Polyline2D", "Insert" });
        cmbType.SelectedIndex = 0;
        cmbType.SelectedIndexChanged += cmbType_SelectedIndexChanged;

        btnSelectAll.Text = "Select All";
        btnSelectAll.Location = new Point(455, 57);
        btnSelectAll.Size = new Size(100, 28);
        btnSelectAll.FlatStyle = FlatStyle.Flat;
        btnSelectAll.BackColor = Color.FromArgb(50, 65, 80);
        btnSelectAll.ForeColor = Color.White;
        btnSelectAll.Font = new Font("Segoe UI", 8.5F);
        btnSelectAll.Cursor = Cursors.Hand;
        btnSelectAll.Click += btnSelectAll_Click;

        btnSelectNone.Text = "Select None";
        btnSelectNone.Location = new Point(562, 57);
        btnSelectNone.Size = new Size(105, 28);
        btnSelectNone.FlatStyle = FlatStyle.Flat;
        btnSelectNone.BackColor = Color.FromArgb(50, 65, 80);
        btnSelectNone.ForeColor = Color.White;
        btnSelectNone.Font = new Font("Segoe UI", 8.5F);
        btnSelectNone.Cursor = Cursors.Hand;
        btnSelectNone.Click += btnSelectNone_Click;

        dgvEntities.Dock = DockStyle.Fill;
        dgvEntities.BackgroundColor = Color.White;
        dgvEntities.BorderStyle = BorderStyle.None;
        dgvEntities.ColumnHeadersHeight = 32;
        dgvEntities.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 240, 248);
        dgvEntities.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvEntities.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        dgvEntities.RowTemplate.Height = 26;
        dgvEntities.CurrentCellDirtyStateChanged += dgvEntities_CurrentCellDirtyStateChanged;

        pnlBottom.BackColor = Color.FromArgb(240, 243, 248);
        pnlBottom.Dock = DockStyle.Bottom;
        pnlBottom.Height = 52;
        pnlBottom.Padding = new Padding(12, 8, 12, 8);
        pnlBottom.Controls.AddRange(new Control[] { btnExport, lblStatus });

        btnExport.Text = "Export Selected to CSV";
        btnExport.Location = new Point(12, 10);
        btnExport.Size = new Size(200, 34);
        btnExport.FlatStyle = FlatStyle.Flat;
        btnExport.BackColor = Color.FromArgb(0, 160, 100);
        btnExport.ForeColor = Color.White;
        btnExport.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnExport.FlatAppearance.BorderColor = Color.FromArgb(0, 140, 85);
        btnExport.Cursor = Cursors.Hand;
        btnExport.Click += btnExport_Click;

        lblStatus.Text = "Ready. Open a DXF file to begin.";
        lblStatus.Location = new Point(225, 18);
        lblStatus.Size = new Size(600, 20);
        lblStatus.ForeColor = Color.FromArgb(80, 100, 120);
        lblStatus.Font = new Font("Segoe UI", 9F);

        Text = "DXF Coordinate Extractor";
        Size = new Size(1000, 700);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        AllowDrop = true;
        DragEnter += Form1_DragEnter;
        DragDrop += Form1_DragDrop;

        Controls.Add(dgvEntities);
        Controls.Add(pnlTop);
        Controls.Add(pnlBottom);

        pnlTop.ResumeLayout(false);
        pnlBottom.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvEntities).EndInit();
        ResumeLayout(false);
    }
}
