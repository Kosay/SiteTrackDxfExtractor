#nullable enable
namespace DxfCoordinateExtractor;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;

    // Top panel controls
    private Panel pnlTop = null!;
    private Button btnOpen = null!;
    private Button btnBrowseJSON = null!;
    private Label lblFile = null!;
    private Label label1 = null!;
    private ComboBox cmbLayer = null!;
    private Label label2 = null!;
    private ComboBox cmbType = null!;
    private Button btnSelectAll = null!;
    private Button btnSelectNone = null!;

    // Main area: grid + side panel
    private Panel pnlMain = null!;
    private DataGridView dgvEntities = null!;
    private Panel pnlSide = null!;

    // Side panel controls (layer roles)
    private Label lblRolesTitle = null!;
    private ListBox lstLayers = null!;
    private Label lblRoleLabel = null!;
    private ComboBox cmbRole = null!;
    private Label lblTolLabel = null!;
    private NumericUpDown nudTolerance = null!;
    private Label lblTolUnit = null!;
    private Label lblTextRadLabel = null!;
    private NumericUpDown nudTextRadius = null!;
    private Label lblTextRadUnit = null!;
    private Button btnExportSiteTrack = null!;
    private Button btnExportAutoCAD = null!;

    // Bottom panel
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
        // ── Instantiate ──────────────────────────────────────
        pnlTop             = new Panel();
        btnOpen            = new Button();
        lblFile            = new Label();
        label1             = new Label();
        cmbLayer           = new ComboBox();
        label2             = new Label();
        cmbType            = new ComboBox();
        btnSelectAll       = new Button();
        btnSelectNone      = new Button();

        pnlMain            = new Panel();
        dgvEntities        = new DataGridView();
        pnlSide            = new Panel();

        lblRolesTitle      = new Label();
        lstLayers          = new ListBox();
        lblRoleLabel       = new Label();
        cmbRole            = new ComboBox();
        lblTolLabel        = new Label();
        nudTolerance       = new NumericUpDown();
        lblTolUnit         = new Label();
        lblTextRadLabel    = new Label();
        nudTextRadius      = new NumericUpDown();
        lblTextRadUnit     = new Label();
        btnExportSiteTrack = new Button();
        btnExportAutoCAD   = new Button();

        pnlBottom          = new Panel();
        btnExport          = new Button();
        lblStatus          = new Label();

        pnlTop.SuspendLayout();
        pnlMain.SuspendLayout();
        pnlSide.SuspendLayout();
        pnlBottom.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvEntities).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudTolerance).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudTextRadius).BeginInit();
        SuspendLayout();

        // ── TOP PANEL ────────────────────────────────────────
        pnlTop.BackColor = Color.FromArgb(30, 42, 58);
        pnlTop.Dock      = DockStyle.Top;
        pnlTop.Height    = 110;
        pnlTop.Padding   = new Padding(12, 10, 12, 10);
        pnlTop.Controls.AddRange(new Control[]
        {
            btnOpen, btnBrowseJSON, lblFile, label1, cmbLayer, label2, cmbType, btnSelectAll, btnSelectNone
        });

        btnOpen.Text                         = "Open DXF File";
        btnOpen.Location                     = new Point(12, 12);
        btnOpen.Size                         = new Size(150, 36);
        btnOpen.FlatStyle                    = FlatStyle.Flat;
        btnOpen.FlatAppearance.BorderColor   = Color.FromArgb(0, 120, 215);
        btnOpen.BackColor                    = Color.FromArgb(0, 120, 215);
        btnOpen.ForeColor                    = Color.White;
        btnOpen.Font                         = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnOpen.Cursor                       = Cursors.Hand;
        btnOpen.Click                       += btnOpen_Click;

        btnBrowseJSON.Text                   = "Browse JSON";
        btnBrowseJSON.Location               = new Point(172, 12);
        btnBrowseJSON.Size                   = new Size(120, 36);
        btnBrowseJSON.FlatStyle              = FlatStyle.Flat;
        btnBrowseJSON.FlatAppearance.BorderColor = Color.FromArgb(150, 150, 150);
        btnBrowseJSON.BackColor              = Color.FromArgb(50, 65, 80);
        btnBrowseJSON.ForeColor              = Color.White;
        btnBrowseJSON.Font                   = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnBrowseJSON.Cursor                 = Cursors.Hand;
        btnBrowseJSON.Click                  += btnBrowseJSON_Click;

        lblFile.Text      = "No file loaded — drag and drop a DXF here or click Open";
        lblFile.Location  = new Point(312, 20);
        lblFile.Size      = new Size(460, 20);
        lblFile.ForeColor = Color.FromArgb(180, 200, 220);
        lblFile.Font      = new Font("Segoe UI", 9F);

        label1.Text      = "Layer:";
        label1.Location  = new Point(12, 62);
        label1.Size      = new Size(45, 22);
        label1.ForeColor = Color.FromArgb(180, 200, 220);
        label1.Font      = new Font("Segoe UI", 9F);

        cmbLayer.Location            = new Point(60, 60);
        cmbLayer.Size                = new Size(200, 24);
        cmbLayer.DropDownStyle       = ComboBoxStyle.DropDownList;
        cmbLayer.Font                = new Font("Segoe UI", 9F);
        cmbLayer.SelectedIndexChanged += cmbLayer_SelectedIndexChanged;

        label2.Text      = "Type:";
        label2.Location  = new Point(272, 62);
        label2.Size      = new Size(40, 22);
        label2.ForeColor = Color.FromArgb(180, 200, 220);
        label2.Font      = new Font("Segoe UI", 9F);

        cmbType.Location  = new Point(315, 60);
        cmbType.Size      = new Size(140, 24);
        cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbType.Font      = new Font("Segoe UI", 9F);
        cmbType.Items.AddRange(new object[]
        {
            "All Types", "Line", "Point", "Circle", "Polyline2D", "Insert"
        });
        cmbType.SelectedIndex          = 0;
        cmbType.SelectedIndexChanged  += cmbType_SelectedIndexChanged;

        btnSelectAll.Text                       = "Select All";
        btnSelectAll.Location                   = new Point(470, 57);
        btnSelectAll.Size                       = new Size(100, 28);
        btnSelectAll.FlatStyle                  = FlatStyle.Flat;
        btnSelectAll.BackColor                  = Color.FromArgb(50, 65, 80);
        btnSelectAll.ForeColor                  = Color.White;
        btnSelectAll.Font                       = new Font("Segoe UI", 8.5F);
        btnSelectAll.Cursor                     = Cursors.Hand;
        btnSelectAll.Click                     += btnSelectAll_Click;

        btnSelectNone.Text                      = "Select None";
        btnSelectNone.Location                  = new Point(578, 57);
        btnSelectNone.Size                      = new Size(105, 28);
        btnSelectNone.FlatStyle                 = FlatStyle.Flat;
        btnSelectNone.BackColor                 = Color.FromArgb(50, 65, 80);
        btnSelectNone.ForeColor                 = Color.White;
        btnSelectNone.Font                      = new Font("Segoe UI", 8.5F);
        btnSelectNone.Cursor                    = Cursors.Hand;
        btnSelectNone.Click                    += btnSelectNone_Click;

        // ── MAIN PANEL (grid + side) ─────────────────────────
        pnlMain.Dock = DockStyle.Fill;
        pnlMain.Controls.AddRange(new Control[] { pnlSide, dgvEntities });

        // ── GRID ─────────────────────────────────────────────
        dgvEntities.Dock                                          = DockStyle.Fill;
        dgvEntities.BackgroundColor                               = Color.White;
        dgvEntities.BorderStyle                                   = BorderStyle.None;
        dgvEntities.ColumnHeadersHeight                           = 32;
        dgvEntities.ColumnHeadersDefaultCellStyle.BackColor       = Color.FromArgb(235, 240, 248);
        dgvEntities.ColumnHeadersDefaultCellStyle.Font            = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvEntities.DefaultCellStyle.Font                         = new Font("Segoe UI", 9F);
        dgvEntities.RowTemplate.Height                            = 26;
        dgvEntities.CurrentCellDirtyStateChanged                 += dgvEntities_CurrentCellDirtyStateChanged;

        // ── SIDE PANEL (Layer Roles) ─────────────────────────
        pnlSide.Dock      = DockStyle.Right;
        pnlSide.Width     = 230;
        pnlSide.BackColor = Color.FromArgb(245, 248, 252);
        pnlSide.Padding   = new Padding(8);
        pnlSide.Controls.AddRange(new Control[]
        {
            lblRolesTitle,
            lstLayers,
            lblRoleLabel, cmbRole,
            lblTolLabel, nudTolerance, lblTolUnit,
            lblTextRadLabel, nudTextRadius, lblTextRadUnit,
            btnExportSiteTrack,
            btnExportAutoCAD
        });

        lblRolesTitle.Text      = "Layer Roles";
        lblRolesTitle.Location  = new Point(8, 8);
        lblRolesTitle.Size      = new Size(214, 20);
        lblRolesTitle.Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblRolesTitle.ForeColor = Color.FromArgb(30, 42, 58);

        lstLayers.Location              = new Point(8, 32);
        lstLayers.Size                  = new Size(214, 200);
        lstLayers.Font                  = new Font("Segoe UI", 8.5F);
        lstLayers.BorderStyle           = BorderStyle.FixedSingle;
        lstLayers.SelectedIndexChanged += lstLayers_SelectedIndexChanged;

        lblRoleLabel.Text      = "Role:";
        lblRoleLabel.Location  = new Point(8, 242);
        lblRoleLabel.Size      = new Size(40, 22);
        lblRoleLabel.Font      = new Font("Segoe UI", 9F);

        cmbRole.Location             = new Point(52, 240);
        cmbRole.Size                 = new Size(170, 24);
        cmbRole.DropDownStyle        = ComboBoxStyle.DropDownList;
        cmbRole.Font                 = new Font("Segoe UI", 9F);
        cmbRole.Items.AddRange(new object[] { "None", "Node", "Pipe", "Label" });
        cmbRole.SelectedIndex        = 0;
        cmbRole.SelectedIndexChanged += cmbRole_SelectedIndexChanged;

        lblTolLabel.Text      = "Snap tolerance:";
        lblTolLabel.Location  = new Point(8, 278);
        lblTolLabel.Size      = new Size(100, 20);
        lblTolLabel.Font      = new Font("Segoe UI", 8.5F);
        lblTolLabel.ForeColor = Color.FromArgb(60, 80, 100);

        nudTolerance.Location       = new Point(110, 275);
        nudTolerance.Size           = new Size(70, 22);
        nudTolerance.Font           = new Font("Segoe UI", 9F);
        nudTolerance.DecimalPlaces  = 1;
        nudTolerance.Minimum        = 0.1M;
        nudTolerance.Maximum        = 50M;
        nudTolerance.Value          = 2.0M;
        nudTolerance.Increment      = 0.5M;

        lblTolUnit.Text      = "m";
        lblTolUnit.Location  = new Point(185, 278);
        lblTolUnit.Size      = new Size(20, 20);
        lblTolUnit.Font      = new Font("Segoe UI", 8.5F);
        lblTolUnit.ForeColor = Color.FromArgb(60, 80, 100);

        lblTextRadLabel.Text      = "Text search radius:";
        lblTextRadLabel.Location  = new Point(8, 308);
        lblTextRadLabel.Size      = new Size(120, 20);
        lblTextRadLabel.Font      = new Font("Segoe UI", 8.5F);
        lblTextRadLabel.ForeColor = Color.FromArgb(60, 80, 100);

        nudTextRadius.Location      = new Point(130, 305);
        nudTextRadius.Size          = new Size(55, 22);
        nudTextRadius.Font          = new Font("Segoe UI", 9F);
        nudTextRadius.DecimalPlaces = 1;
        nudTextRadius.Minimum       = 0.5M;
        nudTextRadius.Maximum       = 100M;
        nudTextRadius.Value         = 5.0M;
        nudTextRadius.Increment     = 0.5M;

        lblTextRadUnit.Text      = "m";
        lblTextRadUnit.Location  = new Point(190, 308);
        lblTextRadUnit.Size      = new Size(20, 20);
        lblTextRadUnit.Font      = new Font("Segoe UI", 8.5F);
        lblTextRadUnit.ForeColor = Color.FromArgb(60, 80, 100);

        btnExportSiteTrack.Text                      = "Export for SiteTrack";
        btnExportSiteTrack.Location                  = new Point(8, 345);
        btnExportSiteTrack.Size                      = new Size(214, 38);
        btnExportSiteTrack.FlatStyle                 = FlatStyle.Flat;
        btnExportSiteTrack.BackColor                 = Color.FromArgb(0, 150, 136);
        btnExportSiteTrack.ForeColor                 = Color.White;
        btnExportSiteTrack.Font                      = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnExportSiteTrack.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 110);
        btnExportSiteTrack.Cursor                    = Cursors.Hand;
        btnExportSiteTrack.Click                    += btnExportSiteTrack_Click;

        btnExportAutoCAD.Text                        = "Export AutoCAD Data";
        btnExportAutoCAD.Location                    = new Point(8, 392);
        btnExportAutoCAD.Size                        = new Size(214, 38);
        btnExportAutoCAD.FlatStyle                   = FlatStyle.Flat;
        btnExportAutoCAD.BackColor                   = Color.FromArgb(63, 81, 181);
        btnExportAutoCAD.ForeColor                   = Color.White;
        btnExportAutoCAD.Font                        = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnExportAutoCAD.FlatAppearance.BorderColor  = Color.FromArgb(48, 63, 159);
        btnExportAutoCAD.Cursor                      = Cursors.Hand;
        btnExportAutoCAD.Click                      += btnExportAutoCAD_Click;

        // ── BOTTOM PANEL ─────────────────────────────────────
        pnlBottom.BackColor = Color.FromArgb(240, 243, 248);
        pnlBottom.Dock      = DockStyle.Bottom;
        pnlBottom.Height    = 52;
        pnlBottom.Padding   = new Padding(12, 8, 12, 8);
        pnlBottom.Controls.AddRange(new Control[] { btnExport, lblStatus });

        btnExport.Text                       = "Export Selected to CSV";
        btnExport.Location                   = new Point(12, 10);
        btnExport.Size                       = new Size(200, 34);
        btnExport.FlatStyle                  = FlatStyle.Flat;
        btnExport.BackColor                  = Color.FromArgb(0, 160, 100);
        btnExport.ForeColor                  = Color.White;
        btnExport.Font                       = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnExport.FlatAppearance.BorderColor = Color.FromArgb(0, 140, 85);
        btnExport.Cursor                     = Cursors.Hand;
        btnExport.Click                     += btnExport_Click;

        lblStatus.Text      = "Ready. Open a DXF file to begin.";
        lblStatus.Location  = new Point(225, 18);
        lblStatus.Size      = new Size(700, 20);
        lblStatus.ForeColor = Color.FromArgb(80, 100, 120);
        lblStatus.Font      = new Font("Segoe UI", 9F);

        // ── FORM ─────────────────────────────────────────────
        Text            = "DXF Coordinate Extractor — SiteTrack Edition";
        Size            = new Size(1200, 750);
        MinimumSize     = new Size(900, 550);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = Color.White;
        Font            = new Font("Segoe UI", 9F);
        AllowDrop       = true;
        DragEnter      += Form1_DragEnter;
        DragDrop       += Form1_DragDrop;

        Controls.Add(pnlMain);
        Controls.Add(pnlTop);
        Controls.Add(pnlBottom);

        pnlTop.ResumeLayout(false);
        pnlMain.ResumeLayout(false);
        pnlSide.ResumeLayout(false);
        pnlBottom.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvEntities).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudTolerance).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudTextRadius).EndInit();
        ResumeLayout(false);
    }
}
