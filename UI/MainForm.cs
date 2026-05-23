using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using XboxImageExtractor.Core;
using System.Globalization;
using System.Diagnostics;

namespace XboxImageExtractor.UI
{
    public partial class MainForm : Form
    {
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem openIsoMenuItem;
        private ToolStripMenuItem openDvdMenuItem;
        private ToolStripMenuItem closeIsoMenuItem;
        private ToolStripMenuItem exitMenuItem;
        
        private SplitContainer splitContainer;
        private TreeView treeView;
        private ListView listView;
        private TabControl tabControl;
        private ColumnHeader colName;
        private ColumnHeader colSize;
        private ColumnHeader colAttr;
        private ColumnHeader colSector;
        
        // Classic Xbox Tab Components
        private SplitContainer splitContainerClassic;
        private TreeView treeViewClassic;
        private ListView listViewClassic;
        private GdfxArchive _currentClassicArchive;
        private Button btnLoadClassicIso;
        
        // Softmod Tab Components
        private ComboBox cbUsbDrives;
        private Button btnInstallSoftmod;
        
        // Game Downloader Tab Components
        private ListView lvGames;
        private TextBox txtSearchGames;
        private ComboBox cbSystemFilter;
        private Button btnLoadGameList;
        private Button btnDownloadGame;
        private Button btnDownloadsMenu; // The Chrome style icon button
        private Panel pnlDownloadNotification;
        private Label lblDownloadStatus;
        private ProgressBar pbDownload;
        private List<VimmGame> _allGames = new List<VimmGame>();
        private CancellationTokenSource _downloadCts;
        private bool _gamesLoaded = false;
        private System.Windows.Forms.Timer _searchDebounceTimer;
        private CancellationTokenSource _searchCts;
        
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusFilesLabel;
        private ToolStripStatusLabel statusSizeLabel;
        
        private ToolStripStatusLabel currentFileLabelText;
        private ToolStripProgressBar currentFileProgress;
        
        private ToolStripStatusLabel overallLabelText;
        private ToolStripProgressBar overallProgress;
        
        private ToolStripStatusLabel currentFileNameLabel;

        private ContextMenuStrip itemContextMenu;
        private ToolStripMenuItem extractMenuItem;
        private ToolStripMenuItem burnMenuItem;

        private GdfxArchive _currentArchive;

        public MainForm()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.Text = "Xbox Image Extractor by Dromex (GDFX/XISO)";
            this.Width = 1000;
            this.Height = 650;
            this.MinimumSize = new Size(800, 500);

            // Menu
            menuStrip = new MenuStrip();
            fileMenu = new ToolStripMenuItem("File");
            openIsoMenuItem = new ToolStripMenuItem("Open ISO Image...");
            openDvdMenuItem = new ToolStripMenuItem("Open from DVD/CD Drive");
            closeIsoMenuItem = new ToolStripMenuItem("Close Image");
            exitMenuItem = new ToolStripMenuItem("Exit");
            
            closeIsoMenuItem.Enabled = false;

            fileMenu.DropDownItems.Add(openIsoMenuItem);
            fileMenu.DropDownItems.Add(openDvdMenuItem);
            fileMenu.DropDownItems.Add(closeIsoMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenuItem);
            menuStrip.Items.Add(fileMenu);

            // Context Menu
            itemContextMenu = new ContextMenuStrip();
            extractMenuItem = new ToolStripMenuItem("Extract...");
            burnMenuItem = new ToolStripMenuItem("Burn Folder to CD/DVD (RGH format)...");
            itemContextMenu.Items.Add(extractMenuItem);
            itemContextMenu.Items.Add(burnMenuItem);

            // Split Container
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 300
            };

            // Tree View
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false
            };
            
            // List View
            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                ContextMenuStrip = itemContextMenu
            };

            colName = new ColumnHeader { Text = "Name", Width = 250 };
            colSize = new ColumnHeader { Text = "File Size", Width = 100 };
            colAttr = new ColumnHeader { Text = "Attributes", Width = 80 };
            colSector = new ColumnHeader { Text = "Absolute Offset", Width = 150 };

            listView.Columns.AddRange(new ColumnHeader[] { colName, colSize, colAttr, colSector });

            splitContainer.Panel1.Controls.Add(treeView);
            splitContainer.Panel2.Controls.Add(listView);

            // Status Strip
            statusStrip = new StatusStrip();
            statusFilesLabel = new ToolStripStatusLabel("Directories: 0  Files: 0") { Width = 160 };
            statusSizeLabel = new ToolStripStatusLabel("Total Size: 0 Bytes") { Width = 150 };
            
            currentFileNameLabel = new ToolStripStatusLabel("Idle") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            
            currentFileLabelText = new ToolStripStatusLabel("File Progress: 0%");
            currentFileProgress = new ToolStripProgressBar() { Width = 100, Visible = true };
            
            overallLabelText = new ToolStripStatusLabel("Total Progress: 0%");
            overallProgress = new ToolStripProgressBar() { Width = 100, Visible = true };

            statusStrip.Items.AddRange(new ToolStripItem[] 
            { 
                statusFilesLabel, 
                new ToolStripSeparator(), 
                statusSizeLabel,
                currentFileNameLabel,
                currentFileLabelText,
                currentFileProgress, 
                overallLabelText,
                overallProgress 
            });

            // Tabs creation
            tabControl = new TabControl { Dock = DockStyle.Fill };
            var tab360 = new TabPage("Xbox 360 (XGD2/XGD3)");
            tab360.Controls.Add(splitContainer);
            
            var tabClassic = new TabPage("Classic Xbox (XISO)");
            
            splitContainerClassic = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 300, Visible = false };
            treeViewClassic = new TreeView { Dock = DockStyle.Fill, HideSelection = false, ContextMenuStrip = itemContextMenu };
            listViewClassic = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, ContextMenuStrip = itemContextMenu };
            
            listViewClassic.Columns.AddRange(new ColumnHeader[] { 
                new ColumnHeader { Text = "Name", Width = 250 }, 
                new ColumnHeader { Text = "File Size", Width = 100 }, 
                new ColumnHeader { Text = "Attributes", Width = 80 }, 
                new ColumnHeader { Text = "Absolute Offset", Width = 150 } 
            });
            
            splitContainerClassic.Panel1.Controls.Add(treeViewClassic);
            splitContainerClassic.Panel2.Controls.Add(listViewClassic);
            
            btnLoadClassicIso = new Button { Text = "Load Original Xbox ISO (XISO)...", Width = 250, Height = 50 };
            btnLoadClassicIso.Location = new Point(350, 250);
            btnLoadClassicIso.Click += BtnLoadClassicIso_Click;
            
            tabClassic.Controls.Add(btnLoadClassicIso);
            tabClassic.Controls.Add(splitContainerClassic);

            var tabGod = new TabPage("Games On Demand (ISO2GOD)");
            var btnGodTest = new Button { Text = "Load ISO and extract Title ID from default.xex...", Width = 300, Height = 50, Location = new Point(20, 20) };
            btnGodTest.Click += BtnGodTest_Click;
            var lblGodStatus = new Label { Name = "lblGodStatus", Text = "1. XEX Parser: Ready\n2. STFS/CON Generator: Awaiting cryptography implementation.", AutoSize = true, Location = new Point(20, 80) };
            tabGod.Controls.Add(btnGodTest);
            tabGod.Controls.Add(lblGodStatus);

            var tabSoftmod = new TabPage("Create USB Softmod (RGH)");
            var lblDrives = new Label { Text = "Select USB Drive:", Location = new Point(20, 20), AutoSize = true };
            cbUsbDrives = new ComboBox { Location = new Point(20, 45), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            cbUsbDrives.DropDown += (s, e) => RefreshUsbDrives();
            
            btnInstallSoftmod = new Button { Text = "Create / Update USB Drive (Softmod)", Location = new Point(20, 80), Width = 300, Height = 45 };
            btnInstallSoftmod.Click += BtnInstallSoftmod_Click;

            var rtbInstructions = new RichTextBox
            {
                Location = new Point(350, 20),
                Size = new Size(600, 400),
                ReadOnly = true,
                BackColor = Color.White,
                Text = "INSTALLATION INSTRUCTIONS (RGH SOFTMOD) 🎮\n" +
                       "--------------------------------------------------\n" +
                       "1. Insert the USB drive into your Xbox 360 console.\n" +
                       "2. Launch the game 'Rock Band Blitz' from your library.\n" +
                       "3. Press (A) and wait for the 'running exploit' message.\n" +
                       "4. A screen with yellow text will appear. Press (X) and then (Y) to save XexMenu system files.\n" +
                       "5. Press the 'Back' button on your controller - the console will smoothly boot into Aurora Dashboard!\n\n\n" +
                       "HOW TO ADD NEW GAMES FROM ISO TO YOUR LIBRARY 💿\n" +
                       "--------------------------------------------------\n" +
                       "1. Download a ready-to-play game in ISO format (e.g. from Vimm's Lair).\n" +
                       "2. Open the game with our program, use the 'Xbox 360 Extractor' and extract it to this USB drive or directly to your PC Hard Drive.\n" +
                       "3. Plug the USB drive back into your Xbox 360 console.\n" +
                       "4. Launch 'FileManager' natively inside the Aurora overlay (Back button on dashboard desktop).\n" +
                       "5. Locate the extracted game folder on 'Usb0' (USB Drive), select [Cut] operation on the folder, navigate to your internal hard drive ('Hdd1'), and use [Paste].\n" +
                       "6. After transfer completes, go to Aurora settings: Press Start -> Settings -> Content -> Paths -> Click 'Scan'. The script will map the new folder path, download covers, and the game will appear directly on your dashboard next to the others!"
            };

            tabSoftmod.Controls.Add(lblDrives);
            tabSoftmod.Controls.Add(cbUsbDrives);
            tabSoftmod.Controls.Add(btnInstallSoftmod);
            tabSoftmod.Controls.Add(rtbInstructions);

            RefreshUsbDrives();

            // === TAB 5: GAME DOWNLOADER ===
            var tabDownloader = new TabPage("Game Downloader");
            
            var lblFilter = new Label { Text = "System:", Location = new Point(20, 18), AutoSize = true };
            cbSystemFilter = new ComboBox { Location = new Point(80, 15), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cbSystemFilter.Items.AddRange(new object[] { "All", "Xbox 360", "Xbox" });
            cbSystemFilter.SelectedIndex = 0;
            cbSystemFilter.SelectedIndexChanged += (s, e) => FilterGameList();
            
            var lblSearch = new Label { Text = "Search:", Location = new Point(220, 18), AutoSize = true };
            txtSearchGames = new TextBox { Location = new Point(275, 15), Width = 250 };
            txtSearchGames.PlaceholderText = "Type to search Vimm's Lair...";
            
            // Debounced live search - queries Vimm API after 500ms of no typing
            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _searchDebounceTimer.Tick += async (s2, e2) => {
                _searchDebounceTimer.Stop();
                string query = txtSearchGames.Text.Trim();
                if (query.Length < 2) { FilterGameList(); return; }
                
                // Cancel previous search
                _searchCts?.Cancel();
                _searchCts = new CancellationTokenSource();
                var token = _searchCts.Token;
                
                currentFileNameLabel.Text = $"Searching Vimm for '{query}'...";
                try
                {
                    string systemFilter = cbSystemFilter.SelectedItem?.ToString() ?? "All";
                    string vimmSystem = systemFilter == "Xbox 360" ? "Xbox360" : (systemFilter == "Xbox" ? "Xbox" : "All");
                    var results = await VimmScraper.SearchGamesAsync(query, vimmSystem);
                    
                    if (token.IsCancellationRequested) return;
                    
                    // Merge search results into master list
                    foreach (var g in results)
                    {
                        if (!_allGames.Any(x => x.VaultId == g.VaultId))
                            _allGames.Add(g);
                    }
                    
                    // Show ONLY search results in the list (direct display)
                    lvGames.Items.Clear();
                    lvGames.BeginUpdate();
                    foreach (var game in results.OrderBy(g => g.Title))
                    {
                        var item = new ListViewItem(game.Title);
                        item.SubItems.Add(game.System == "Xbox360" ? "Xbox 360" : "Xbox");
                        item.SubItems.Add(game.VaultId);
                        item.Tag = game;
                        lvGames.Items.Add(item);
                    }
                    lvGames.EndUpdate();
                    currentFileNameLabel.Text = $"Found {results.Count} results for '{query}'.";
                }
                catch { if (!token.IsCancellationRequested) currentFileNameLabel.Text = "Search failed."; }
            };
            txtSearchGames.TextChanged += (s, e) => {
                _searchDebounceTimer.Stop();
                if (string.IsNullOrWhiteSpace(txtSearchGames.Text)) { FilterGameList(); return; }
                _searchDebounceTimer.Start();
            };
            
            btnLoadGameList = new Button { Text = "Fetch All Games", Location = new Point(550, 12), Width = 130, Height = 28 };
            btnLoadGameList.Click += BtnLoadGameList_Click;
            
            btnDownloadGame = new Button { Text = "Download Selected", Location = new Point(690, 12), Width = 140, Height = 28, Enabled = false };
            btnDownloadGame.Click += BtnDownloadGame_Click;

            // Chrome-style Download Icon Button
            btnDownloadsMenu = new Button 
            { 
                Text = "⏬", 
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(850, 10), 
                Width = 36, 
                Height = 36, 
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnDownloadsMenu.FlatAppearance.BorderSize = 0;
            btnDownloadsMenu.Click += (s, e) => { pnlDownloadNotification.Visible = !pnlDownloadNotification.Visible; pnlDownloadNotification.BringToFront(); };
            
            lvGames = new ListView 
            { 
                Location = new Point(20, 50), 
                Size = new Size(920, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details, 
                FullRowSelect = true, 
                GridLines = true,
                MultiSelect = false
            };
            lvGames.Columns.Add("Title", 500);
            lvGames.Columns.Add("System", 120);
            lvGames.Columns.Add("Vault ID", 100);
            lvGames.SelectedIndexChanged += (s, e) => { btnDownloadGame.Enabled = lvGames.SelectedItems.Count > 0; };
            
            // Download notification panel (Chrome-style dropdown menu)
            pnlDownloadNotification = new Panel 
            { 
                Size = new Size(320, 70), 
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblDownloadStatus = new Label 
            { 
                ForeColor = Color.Black, 
                Location = new Point(10, 10), 
                Size = new Size(300, 20), 
                Text = "No active downloads."
            };
            pbDownload = new ProgressBar 
            { 
                Location = new Point(10, 35), 
                Size = new Size(300, 20), 
                Style = ProgressBarStyle.Continuous 
            };
            pnlDownloadNotification.Controls.Add(lblDownloadStatus);
            pnlDownloadNotification.Controls.Add(pbDownload);
            
            tabDownloader.Controls.Add(lblFilter);
            tabDownloader.Controls.Add(cbSystemFilter);
            tabDownloader.Controls.Add(lblSearch);
            tabDownloader.Controls.Add(txtSearchGames);
            tabDownloader.Controls.Add(btnLoadGameList);
            tabDownloader.Controls.Add(btnDownloadGame);
            tabDownloader.Controls.Add(btnDownloadsMenu);
            tabDownloader.Controls.Add(pnlDownloadNotification);
            tabDownloader.Controls.Add(lvGames);
            
            // Position notification panel strictly below the dropdown button
            tabDownloader.Resize += (s, e) => {
                btnDownloadsMenu.Location = new Point(tabDownloader.ClientSize.Width - 60, 10);
                pnlDownloadNotification.Location = new Point(tabDownloader.ClientSize.Width - 340, 50);
            };
            
            // Auto-load trigger
            tabControl.SelectedIndexChanged += (s, e) => {
                if (tabControl.SelectedTab == tabDownloader && !_gamesLoaded)
                {
                    _gamesLoaded = true;
                    btnLoadGameList.PerformClick();
                }
            };
            // === TAB 6: ABOUT ME ===
            var tabAbout = new TabPage("About Me");
            var lblTitle = new Label { Text = "Xbox Image Extractor (GDFX/XISO)", Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20) };
            var lblCreator = new Label { Text = "Created by: Dromex", Font = new Font("Segoe UI", 12), AutoSize = true, Location = new Point(20, 60) };
            var lblIgTitle = new Label { Text = "Follow me on Instagram:", Font = new Font("Segoe UI", 12), AutoSize = true, Location = new Point(20, 100) };
            
            var linkIg = new LinkLabel { Text = "@dromex__", Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Location = new Point(190, 100) };
            linkIg.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo("https://www.instagram.com/dromex__/") { UseShellExecute = true });
            
            var lblThanks = new Label { Text = "Thank you for using my tool to preserve Xbox history!", Font = new Font("Segoe UI", 10, FontStyle.Italic), AutoSize = true, Location = new Point(20, 150) };
            
            tabAbout.Controls.Add(lblTitle);
            tabAbout.Controls.Add(lblCreator);
            tabAbout.Controls.Add(lblIgTitle);
            tabAbout.Controls.Add(linkIg);
            tabAbout.Controls.Add(lblThanks);

            tabControl.TabPages.Add(tab360);
            tabControl.TabPages.Add(tabClassic);
            tabControl.TabPages.Add(tabGod);
            tabControl.TabPages.Add(tabSoftmod);
            tabControl.TabPages.Add(tabDownloader);
            tabControl.TabPages.Add(tabAbout);

            // Layout
            this.Controls.Add(tabControl);
            this.Controls.Add(statusStrip);
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;
        }

        private void SetupEventHandlers()
        {
            openIsoMenuItem.Click += OpenIsoMenuItem_Click;
            closeIsoMenuItem.Click += CloseIsoMenuItem_Click;
            exitMenuItem.Click += (s, e) => this.Close();
            
            openDvdMenuItem.DropDownOpening += OpenDvdMenuItem_DropDownOpening;

            treeView.AfterSelect += TreeView_AfterSelect;
            treeView.NodeMouseClick += (s, e) => { if (e.Button == MouseButtons.Right) treeView.SelectedNode = e.Node; };
            treeView.ContextMenuStrip = itemContextMenu;
            
            treeViewClassic.AfterSelect += TreeViewClassic_AfterSelect;
            treeViewClassic.NodeMouseClick += (s, e) => { if (e.Button == MouseButtons.Right) treeViewClassic.SelectedNode = e.Node; };

            extractMenuItem.Click += ExtractMenuItem_Click;
            burnMenuItem.Click += BurnMenuItem_Click;
        }

        private void OpenDvdMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            openDvdMenuItem.DropDownItems.Clear();
            var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.CDRom).ToList();
            
            foreach (var drive in drives)
            {
                var item = new ToolStripMenuItem($"Zgraj z napędu {drive.Name}");
                item.Click += async (ss, ee) => await LoadRawDriveAsync(@"\\.\" + drive.Name.TrimEnd('\\'));
                openDvdMenuItem.DropDownItems.Add(item);
            }
            if (openDvdMenuItem.DropDownItems.Count == 0)
            {
                var empty = new ToolStripMenuItem("Brak czytników CD/DVD");
                empty.Enabled = false;
                openDvdMenuItem.DropDownItems.Add(empty);
            }
        }

        private async void OpenIsoMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Xbox ISO Images (*.iso)|*.iso|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await LoadIsoAsync(ofd.FileName);
                }
            }
        }

        private async Task LoadRawDriveAsync(string drivePath)
        {
            try
            {
                if (!IsAdministrator())
                {
                    MessageBox.Show("Dostęp do surowych sektorów napędu wymaga uprawnień Administratora. Proszę uruchomić program jako Administrator by czytać płyty Xbox 360 bezpośrednio z interfejsu omijając blokady systemowe.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                await LoadIsoAsync(drivePath);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Brak uprawnień. Odpal program jako Administrator.", "Odmowa Dostępu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch(Exception e)
            {
                MessageBox.Show($"Błąd z napędem: {e.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsAdministrator()
        {
            using (System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }

        private async Task LoadIsoAsync(string filePath)
        {
            try
            {
                CloseCurrentIso();
                
                this.UseWaitCursor = true;
                currentFileNameLabel.Text = "Parsing structure...";
                
                _currentArchive = new GdfxArchive(filePath);
                await _currentArchive.LoadAsync();
                
                PopulateTree(_currentArchive.RootDirectory);
                
                statusFilesLabel.Text = $"Directories: {_currentArchive.DirectoryCount}  Files: {_currentArchive.FileCount}";
                statusSizeLabel.Text = $"Total Size: {FormatBytes(_currentArchive.TotalImageSize)}";
                
                _ = Analytics.LogEventAsync("IsoOpened", $"Otwaro obraz ISO z partycjami XGD. Plików: {_currentArchive.FileCount}");
                
                openIsoMenuItem.Enabled = false;
                openDvdMenuItem.Enabled = false;
                closeIsoMenuItem.Enabled = true;
                
                string displayName = filePath.StartsWith(@"\\.\") ? filePath.Substring(4) + " (Optical Drive)" : Path.GetFileName(filePath);
                this.Text = $"Xbox Image Extractor by Dromex - {displayName}";
                currentFileNameLabel.Text = "Idle";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loading failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CloseCurrentIso();
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }

        private async void BtnLoadClassicIso_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Original Xbox ISO Images (*.iso)|*.iso|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (_currentClassicArchive != null) _currentClassicArchive.Dispose();
                        
                        this.UseWaitCursor = true;
                        currentFileNameLabel.Text = "Parsing Classic structure...";
                        
                        _currentClassicArchive = new GdfxArchive(ofd.FileName);
                        await _currentClassicArchive.LoadAsync();
                        
                        treeViewClassic.Nodes.Clear();
                        var rootNode = new TreeNode(Path.GetFileName(ofd.FileName)) { Tag = _currentClassicArchive.RootDirectory };
                        AddChildrenToNode(rootNode, _currentClassicArchive.RootDirectory);
                        treeViewClassic.Nodes.Add(rootNode);
                        rootNode.Expand();

                        statusFilesLabel.Text = $"Dirs: {_currentClassicArchive.DirectoryCount} Files: {_currentClassicArchive.FileCount}";
                        _ = Analytics.LogEventAsync("IsoOpened", $"Classic Xbox ISO Opened. Files: {_currentClassicArchive.FileCount}");
                        
                        btnLoadClassicIso.Visible = false;
                        splitContainerClassic.Visible = true;
                        currentFileNameLabel.Text = "Idle";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Classic ISO Load Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        this.UseWaitCursor = false;
                    }
                }
            }
        }

        private async void BtnGodTest_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Xbox 360 ISO (*.iso)|*.iso|All Files (*.*)|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var lbl = (Label)tabControl.TabPages[2].Controls["lblGodStatus"];
                        lbl.Text = "Scanning ISO structure for default.xex...";
                        this.UseWaitCursor = true;

                        using (var tempArchive = new GdfxArchive(ofd.FileName))
                        {
                            await tempArchive.LoadAsync();
                            var defaultXexNode = tempArchive.RootDirectory.Children.FirstOrDefault(c => c.Name.Equals("default.xex", StringComparison.OrdinalIgnoreCase));
                            
                            if (defaultXexNode == null) 
                            {
                                lbl.Text = "Error: default.xex file could not be found in the root directory.";
                                return;
                            }

                            lbl.Text = $"Found default.xex ({(defaultXexNode.Size / 1024 / 1024)} MB). Copying to RAM for virtual parsing...";
                            
                            byte[] buffer = new byte[defaultXexNode.Size];
                            using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                fs.Position = defaultXexNode.AbsoluteOffset;
                                await fs.ReadAsync(buffer, 0, (int)defaultXexNode.Size);
                            }

                            var xexInfo = XexParser.Parse(buffer);
                            lbl.Text = $"SUCCESS!\nXEX2 Architecture Decoded.\nTitle ID: {xexInfo.TitleId}\nMedia ID: {xexInfo.MediaId}\n\nNext step is STFS cryptography wrapping using this key.";
                            _ = Analytics.LogEventAsync("IsoToGod_XexParsed", $"Decoded Title ID: {xexInfo.TitleId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"XEX module parsing error: {ex.Message}", "XEX Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        this.UseWaitCursor = false;
                    }
                }
            }
        }

        private void RefreshUsbDrives()
        {
            cbUsbDrives.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable))
            {
                if (drive.IsReady)
                    cbUsbDrives.Items.Add($"{drive.Name} ({drive.DriveFormat}, {FormatBytes(drive.TotalSize)})");
                else
                    cbUsbDrives.Items.Add($"{drive.Name} (Not Ready)");
            }
            if (cbUsbDrives.Items.Count > 0) cbUsbDrives.SelectedIndex = 0;
        }

        private async void BtnInstallSoftmod_Click(object sender, EventArgs e)
        {
            if (cbUsbDrives.SelectedItem == null) return;
            string selected = cbUsbDrives.SelectedItem.ToString();
            string driveLetter = selected.Substring(0, 3);
            
            if (selected.Contains("(Not Ready)"))
            {
                MessageBox.Show("The selected drive is not ready. Please verify connection and try again.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            if (!selected.Contains("FAT32") && !selected.Contains("FAT"))
            {
                var askFormat = MessageBox.Show($"The USB Drive ({driveLetter}) is not formatted in FAT32 (required by Xbox 360). Do you want to format it now? WARNING: ALL DATA ON THE DRIVE WILL BE PERMANENTLY ERASED!", "Format Required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (askFormat == DialogResult.Yes)
                {
                    if (!IsAdministrator())
                    {
                        MessageBox.Show("Administrator privileges are required to format system drives directly via this application. Please restart as Administrator.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    var formatProgress = new Progress<ExtractionProgress>(p => currentFileNameLabel.Text = p.CurrentFileName);
                    btnInstallSoftmod.Enabled = false;
                    bool result = await SoftmodInstaller.FormatDriveToFat32Async(driveLetter, formatProgress);
                    if (!result)
                    {
                        MessageBox.Show("Formatting failed or was interrupted by the system.", "FAT32 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnInstallSoftmod.Enabled = true;
                        currentFileNameLabel.Text = "Idle";
                        return;
                    }
                }
                else return;
            }
            
            btnInstallSoftmod.Enabled = false;
            var installProgress = new Progress<ExtractionProgress>(p => 
            {
                currentFileNameLabel.Text = p.CurrentFileName;
                if (p.OverallTotalBytes > 0)
                {
                    overallProgress.Value = (int)((double)p.OverallExtractedBytes / p.OverallTotalBytes * 100);
                }
            });
            try
            {
                await SoftmodInstaller.DownloadAndInstallAsync(driveLetter, installProgress);
                MessageBox.Show($"RGH Softmod injection completed successfully on drive {driveLetter}!\nYou may safely remove the USB device.", "Mod Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _ = Analytics.LogEventAsync("SoftmodInstalled", "Successfully burned RGH Softmod files onto USB");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while downloading or extracting mod files: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                overallProgress.Value = 0;
                btnInstallSoftmod.Enabled = true;
                currentFileNameLabel.Text = "Idle";
            }
        }

        private async void BtnLoadGameList_Click(object sender, EventArgs e)
        {
            btnLoadGameList.Enabled = false;
            btnLoadGameList.Text = "Fetching...";
            lvGames.Items.Clear();
            _allGames.Clear();

            var progress = new Progress<string>(s => currentFileNameLabel.Text = s);
            var gamesFound = new Progress<List<VimmGame>>(batch => 
            {
                // We add the newly discovered batch, deduplicate
                _allGames.AddRange(batch);
                var dedup = _allGames.GroupBy(g => g.VaultId).Select(g => g.First()).OrderBy(g => g.Title).ToList();
                _allGames.Clear();
                _allGames.AddRange(dedup);
                // Only refresh the list if user is NOT actively searching via the search box
                if (string.IsNullOrWhiteSpace(txtSearchGames.Text))
                    FilterGameList();
            });
            
            try
            {
                // Scrape Xbox 360 first
                await VimmScraper.ScrapeGamesAsync("Xbox360", progress, gamesFound);
                // Scrape Original Xbox second
                await VimmScraper.ScrapeGamesAsync("Xbox", progress, gamesFound);
                
                currentFileNameLabel.Text = $"Loaded {_allGames.Count} games total.";
                _ = Analytics.LogEventAsync("GameListLoaded", $"Fetched {_allGames.Count} games from Vimm");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch game list: {ex.Message}", "Scraper Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLoadGameList.Enabled = true;
                btnLoadGameList.Text = "Fetch All Games";
            }
        }

        private void FilterGameList()
        {
            string search = txtSearchGames.Text.Trim().ToLowerInvariant();
            string system = cbSystemFilter.SelectedItem?.ToString() ?? "All";

            lvGames.Items.Clear();
            lvGames.BeginUpdate();

            foreach (var game in _allGames)
            {
                bool matchSystem = system == "All" 
                    || (system == "Xbox 360" && game.System == "Xbox360")
                    || (system == "Xbox" && game.System == "Xbox");
                    
                bool matchSearch = string.IsNullOrEmpty(search) || game.Title.ToLowerInvariant().Contains(search);

                if (matchSystem && matchSearch)
                {
                    var item = new ListViewItem(game.Title);
                    item.SubItems.Add(game.System == "Xbox360" ? "Xbox 360" : "Xbox");
                    item.SubItems.Add(game.VaultId);
                    item.Tag = game;
                    lvGames.Items.Add(item);
                }
            }
            lvGames.EndUpdate();
        }

        private async void BtnDownloadGame_Click(object sender, EventArgs e)
        {
            if (lvGames.SelectedItems.Count == 0) return;
            var game = (VimmGame)lvGames.SelectedItems[0].Tag;

            string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            
            pnlDownloadNotification.Visible = true;
            pnlDownloadNotification.BringToFront();
            lblDownloadStatus.Text = $"Starting: {game.Title}";
            pbDownload.Value = 0;
            btnDownloadGame.Enabled = false;

            _downloadCts = new CancellationTokenSource();
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.HasError)
                {
                    lblDownloadStatus.Text = $"Error: {p.ErrorMessage}";
                    lblDownloadStatus.ForeColor = Color.Red;
                    return;
                }
                if (p.IsComplete)
                {
                    lblDownloadStatus.Text = $"Done! Saved to Downloads.";
                    lblDownloadStatus.ForeColor = Color.Green;
                    pbDownload.Value = 100;
                    _ = Analytics.LogEventAsync("GameDownloaded", $"{game.Title} ({game.System})");
                    MessageBox.Show($"Successfully downloaded {game.Title} to your Downloads folder!", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                lblDownloadStatus.ForeColor = Color.Black;
                string sizeInfo = p.TotalBytes > 0
                    ? $"{FormatBytes(p.DownloadedBytes)} / {FormatBytes(p.TotalBytes)}"
                    : $"{FormatBytes(p.DownloadedBytes)}";
                lblDownloadStatus.Text = $"{game.Title}\n{sizeInfo} ({p.PercentComplete}%)";
                pbDownload.Value = Math.Min(100, Math.Max(0, p.PercentComplete));
            });

            try
            {
                await GameDownloader.DownloadGameAsync(game, downloadPath, progress, _downloadCts.Token);
            }
            finally
            {
                btnDownloadGame.Enabled = true;
                // Auto-hide notification after 15 seconds
                _ = Task.Delay(15000).ContinueWith(_ => 
                {
                    if (!this.IsDisposed)
                        this.Invoke((Action)(() => { pnlDownloadNotification.Visible = false; lblDownloadStatus.ForeColor = Color.Black; }));
                });
            }
        }

        private void CloseIsoMenuItem_Click(object sender, EventArgs e)
        {
            CloseCurrentIso();
        }

        private void CloseCurrentIso()
        {
            if (_currentArchive != null)
            {
                _currentArchive.Dispose();
                _currentArchive = null;
            }
            
            treeView.Nodes.Clear();
            listView.Items.Clear();
            
            openIsoMenuItem.Enabled = true;
            openDvdMenuItem.Enabled = true;
            closeIsoMenuItem.Enabled = false;
            
            statusFilesLabel.Text = "Directories: 0  Files: 0";
            statusSizeLabel.Text = "Total Size: 0 Bytes";
            this.Text = "Xbox Image Extractor by Dromex (GDFX/XISO)";
            currentFileNameLabel.Text = "Idle";
            ResetProgressBars();
        }

        private void PopulateTree(GdfxEntry root)
        {
            treeView.Nodes.Clear();
            var rootNode = new TreeNode(RootNameDisplay());
            rootNode.Tag = root;
            
            AddChildrenToNode(rootNode, root);
            
            treeView.Nodes.Add(rootNode);
            rootNode.Expand();
        }

        private string RootNameDisplay()
        {
            if (_currentArchive.ImagePath.StartsWith(@"\\.\")) return _currentArchive.ImagePath.Substring(4);
            return Path.GetFileName(_currentArchive.ImagePath);
        }

        private void AddChildrenToNode(TreeNode parentNode, GdfxEntry directory)
        {
            foreach (var child in directory.Children)
            {
                if (child.IsDirectory && child.Name != ".." && child.Name != ".")
                {
                    var childNode = new TreeNode(child.Name);
                    childNode.Tag = child;
                    AddChildrenToNode(childNode, child);
                    parentNode.Nodes.Add(childNode);
                }
            }
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null) return;
            
            GdfxEntry entry = (GdfxEntry)e.Node.Tag;
            PopulateListView(entry);
        }

        private void PopulateListView(GdfxEntry directory)
        {
            listView.Items.Clear();
            listView.BeginUpdate();
            
            foreach (var child in directory.Children)
            {
                var item = new ListViewItem(child.Name);
                item.SubItems.Add(child.IsDirectory ? "" : FormatBytes(child.Size));
                item.SubItems.Add("0x" + child.Attributes.ToString("X2"));
                item.SubItems.Add("0x" + child.AbsoluteOffset.ToString("X8"));
                item.Tag = child;
                
                if (child.IsDirectory)
                {
                    item.Font = new Font(item.Font, FontStyle.Bold);
                }
                
                listView.Items.Add(item);
            }
            
            listView.EndUpdate();
        }

        private void TreeViewClassic_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null) return;
            PopulateListViewClassic((GdfxEntry)e.Node.Tag);
        }

        private void PopulateListViewClassic(GdfxEntry directory)
        {
            listViewClassic.Items.Clear();
            listViewClassic.BeginUpdate();
            
            foreach (var child in directory.Children)
            {
                var item = new ListViewItem(child.Name);
                item.SubItems.Add(child.IsDirectory ? "" : FormatBytes(child.Size));
                item.SubItems.Add("0x" + child.Attributes.ToString("X2"));
                item.SubItems.Add("0x" + child.AbsoluteOffset.ToString("X8"));
                item.Tag = child;
                if (child.IsDirectory) item.Font = new Font(item.Font, FontStyle.Bold);
                listViewClassic.Items.Add(item);
            }
            listViewClassic.EndUpdate();
        }

        private async void BurnMenuItem_Click(object sender, EventArgs e)
        {
            GdfxEntry targetEntry = GetSelectedEntry();
            if (targetEntry == null) return;
            
            if (!targetEntry.IsDirectory)
            {
                MessageBox.Show("You must select a directory (e.g. the root) to burn to a new disc. Individual files cannot be burned.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show($"The system will first extract the contents ('{targetEntry.Name}') to a temporary path (~{FormatBytes(targetEntry.Size)}), and then burn the game to a blank Xbox 360 disc using native COM libraries.\n\nDo you want to continue and take control of the CD/DVD drive?", "Burn to Disc", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string tempPath = Path.Combine(Path.GetTempPath(), "XboxImageExtractorBurn_" + Guid.NewGuid().ToString());
            
            try
            {
                Directory.CreateDirectory(tempPath);
                
                string srcPath = tabControl.SelectedTab.Text.Contains("Classic") ? _currentClassicArchive?.ImagePath : _currentArchive?.ImagePath;
                await ExecuteActionWithProgressAsync(
                    targetEntry, tempPath, "Buffering files before burning...", async (extractor, entry, dest, prog) => {
                        await extractor.ExtractAsync(srcPath, entry, dest, prog);
                    }
                );
                
                currentFileNameLabel.Text = "Initializing IMAPI2 module...";
                var burnProgress = new Progress<string>(s => currentFileNameLabel.Text = s);
                await DiscBurner.BurnFolderToDiscAsync(tempPath, burnProgress);
                
                MessageBox.Show("Success! The game was successfully burned to an optical disc in a format intended for RGH.", "Burn Completed!", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Burning operation failed: {ex.Message}", "Error IMAPI2 / Extractor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { Directory.Delete(tempPath, true); } catch { } 
                currentFileNameLabel.Text = "Idle";
                ResetProgressBars();
                EnableUI(true);
            }
        }

        private async void ExtractMenuItem_Click(object sender, EventArgs e)
        {
            GdfxEntry targetEntry = GetSelectedEntry();
            if (targetEntry == null) return;

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a folder on your disk for the Xbox files:";
                
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var timer = Stopwatch.StartNew();
                        string srcPath = tabControl.SelectedTab.Text.Contains("Classic") ? _currentClassicArchive?.ImagePath : _currentArchive?.ImagePath;
                        await ExecuteActionWithProgressAsync(
                            targetEntry, fbd.SelectedPath, "Extracting in background (Asynchronous)...", async (extractor, entry, dest, prog) => {
                                await extractor.ExtractAsync(srcPath, entry, dest, prog);
                            }
                        );
                        timer.Stop();
                        currentFileNameLabel.Text = $"Finished in: {timer.Elapsed.TotalSeconds:F1}s";
                        MessageBox.Show($"Extraction successfully completed.\nDuration: {timer.Elapsed.TotalSeconds:F1} seconds", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Decoder error: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EnableUI(true);
                        ResetProgressBars();
                    }
                }
            }
        }

        private GdfxEntry GetSelectedEntry()
        {
            if (_currentArchive == null) return null;
            if (listView.Focused && listView.SelectedItems.Count > 0)
                return (GdfxEntry)listView.SelectedItems[0].Tag;
                
            if (treeView.Focused && treeView.SelectedNode != null)
                return (GdfxEntry)treeView.SelectedNode.Tag;

            if (listViewClassic.Focused && listViewClassic.SelectedItems.Count > 0)
                return (GdfxEntry)listViewClassic.SelectedItems[0].Tag;
                
            if (treeViewClassic.Focused && treeViewClassic.SelectedNode != null)
                return (GdfxEntry)treeViewClassic.SelectedNode.Tag;

            return null;
        }

        private async Task ExecuteActionWithProgressAsync(GdfxEntry entry, string dest, string statusInit, Func<GdfxExtractor, GdfxEntry, string, IProgress<ExtractionProgress>, Task> action)
        {
            EnableUI(false);
            currentFileNameLabel.Text = statusInit;

            var progressIndicator = new Progress<ExtractionProgress>(p => 
            {
                currentFileNameLabel.Text = $"Streaming bytes: {FormatBytes(p.OverallExtractedBytes)} of {FormatBytes(p.OverallTotalBytes)} | Extracted {p.ExtractedFiles} / {p.TotalFiles} files";
                
                int filePct = p.CurrentFileTotalBytes > 0 ? (int)((double)p.CurrentFileExtractedBytes / p.CurrentFileTotalBytes * 100) : 100;
                currentFileProgress.Value = Math.Clamp(filePct, 0, 100);
                currentFileLabelText.Text = $"File Progress: {currentFileProgress.Value}%";
                
                int overallPct = p.OverallTotalBytes > 0 ? (int)((double)p.OverallExtractedBytes / p.OverallTotalBytes * 100) : 100;
                overallProgress.Value = Math.Clamp(overallPct, 0, 100);
                overallLabelText.Text = $"Total Progress: {overallProgress.Value}%";
            });

            var extractor = new GdfxExtractor();
            await action(extractor, entry, dest, progressIndicator);
        }

        private void ResetProgressBars()
        {
            currentFileProgress.Value = 0;
            currentFileLabelText.Text = "File Progress: 0%";
            overallProgress.Value = 0;
            overallLabelText.Text = "Total Progress: 0%";
        }

        private void EnableUI(bool enable)
        {
            menuStrip.Enabled = enable;
            treeView.Enabled = enable;
            listView.Enabled = enable;
            this.UseWaitCursor = !enable;
        }

        private string FormatBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; 
            if (bytes == 0) return "0 B";
            long bytesAbs = Math.Abs(bytes);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytesAbs, 1024)));
            double num = Math.Round(bytesAbs / Math.Pow(1024, place), 1);
            return (Math.Sign(bytes) * num).ToString(CultureInfo.InvariantCulture) + " " + suf[place];
        }
    }
}
