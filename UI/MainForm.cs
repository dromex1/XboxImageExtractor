using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
        private ColumnHeader colName;
        private ColumnHeader colSize;
        private ColumnHeader colAttr;
        private ColumnHeader colSector;
        
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
            this.Text = "Xbox Image Extractor (GDFX/XISO)";
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
            extractMenuItem = new ToolStripMenuItem("Extract to HDD...");
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

            // Layout
            this.Controls.Add(splitContainer);
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
                
                openIsoMenuItem.Enabled = false;
                openDvdMenuItem.Enabled = false;
                closeIsoMenuItem.Enabled = true;
                
                string displayName = filePath.StartsWith(@"\\.\") ? filePath.Substring(4) + " (Optical Drive)" : Path.GetFileName(filePath);
                this.Text = $"Xbox Image Extractor - {displayName}";
                currentFileNameLabel.Text = "Idle";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wczytywanie nie powiodło się: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CloseCurrentIso();
            }
            finally
            {
                this.UseWaitCursor = false;
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
            this.Text = "Xbox Image Extractor (GDFX/XISO)";
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

        private async void BurnMenuItem_Click(object sender, EventArgs e)
        {
            GdfxEntry targetEntry = GetSelectedEntry();
            if (targetEntry == null) return;
            
            if (!targetEntry.IsDirectory)
            {
                MessageBox.Show("Musisz zaznaczyć katalog (np. główny / ROOT napędu lub obrazu), aby wypalić go na nową płytę. Nie można nagrać pojedynczego pliku.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show($"System najpierw wypakuje całą zawartość ('{targetEntry.Name}') do pamięci tymczasowej na dysku twardym (~{FormatBytes(targetEntry.Size)} limitów), a docelowo nagra grę na czystą płytę Xbox 360 przy użyciu natywnych bibliotek COM.\n\nCzy chcesz kontynuować i przejąć kontrolę nad napędem CD/DVD?", "Nagrywanie na płytę", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string tempPath = Path.Combine(Path.GetTempPath(), "XboxImageExtractorBurn_" + Guid.NewGuid().ToString());
            
            try
            {
                Directory.CreateDirectory(tempPath);
                
                // Etap 1: Ekstrakcja asynchroniczna ze statusem
                await ExecuteActionWithProgressAsync(
                    targetEntry, tempPath, "Ekstrakcja buforująca przed wypalaniem...", async (extractor, entry, dest, prog) => {
                        await extractor.ExtractAsync(_currentArchive.ImagePath, entry, dest, prog);
                    }
                );
                
                // Etap 2: Nagrywanie
                currentFileNameLabel.Text = "Inicjalizacja modułu IMAPI2...";
                var burnProgress = new Progress<string>(s => currentFileNameLabel.Text = s);
                await DiscBurner.BurnFolderToDiscAsync(tempPath, burnProgress);
                
                MessageBox.Show("Sukces! Gra została pomyślnie wypalona bezpośrednio na nośnik optyczny w formacie przeznaczonym pod RGH.", "Wypalanie Błyskawiczne Ukończone!", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Niepowodzenie operacji nagrywania: {ex.Message}", "Error IMAPI2 / Extractor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                fbd.Description = "Wybierz folder na dysku dla plików z Xboxa:";
                
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var timer = Stopwatch.StartNew();
                        await ExecuteActionWithProgressAsync(
                            targetEntry, fbd.SelectedPath, "Zgrywanie w tle (Asynchroniczne)...", async (extractor, entry, dest, prog) => {
                                await extractor.ExtractAsync(_currentArchive.ImagePath, entry, dest, prog);
                            }
                        );
                        timer.Stop();
                        currentFileNameLabel.Text = $"Zakończono w: {timer.Elapsed.TotalSeconds:F1}s";
                        MessageBox.Show($"Wypakowywanie pomyślnie zakończone.\nCzas trwania: {timer.Elapsed.TotalSeconds:F1} sekund", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd dekodera: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            return null;
        }

        private async Task ExecuteActionWithProgressAsync(GdfxEntry entry, string dest, string statusInit, Func<GdfxExtractor, GdfxEntry, string, IProgress<ExtractionProgress>, Task> action)
        {
            EnableUI(false);
            currentFileNameLabel.Text = statusInit;

            var progressIndicator = new Progress<ExtractionProgress>(p => 
            {
                currentFileNameLabel.Text = $"Rozmiar w locie: {FormatBytes(p.OverallExtractedBytes)} z {FormatBytes(p.OverallTotalBytes)} | Wypakowano {p.ExtractedFiles} z {p.TotalFiles} plików z ISO";
                
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
