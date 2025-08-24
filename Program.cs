using GameSaveLinkManager.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GameSaveLinkManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class GameRow
    {
        public bool Selected { get; set; }
        public string Alias { get; set; }             // GameName
        public string RealPath { get; set; }          // TargetPath (relative to OneDrive OR absolute)
        public string LocalPath { get; set; }         // Optional
        [Browsable(false)]
        public string ResolvedRealPath { get; set; }
    }

    public class MainForm : Form
    {
        string _title = "Game Save Link Manager";
        string _defaultMappingFileName = "GameSaveLinkMapping.txt";

        bool _ready = false;
        TextBox txtMapPath, txtOneDrive;
        Button btnBrowseMap, btnLoad, btnSave, btnAdd, btnRemove, btnOpenReal, btnCopyReal, btnRun;
        DataGridView grid;
        Label lblResolved, lblStatus;
        BindingList<GameRow> rows = new BindingList<GameRow>();

        public MainForm()
        {
            this.Text = _title;
            this.Width = 1280;
            this.Height = 720;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = Properties.Resources.AppIcon;

            // A master TableLayoutPanel to correctly manage the layout of the three main sections.
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Top controls
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // DataGridView
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Bottom controls

            // --- Top Panel for Buttons and TextBoxes ---
            // Note: Its Dock is now Fill to occupy the first row of mainPanel.
            var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 8, AutoSize = true, Padding = new Padding(8) };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

            //First row START
            top.Controls.Add(new Label { Text = "Mapping File", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            txtMapPath = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            top.Controls.Add(txtMapPath, 1, 0);

            btnBrowseMap = new Button { Text = "Browse..." };
            btnBrowseMap.Click += (s, e) => BrowseMap();
            top.Controls.Add(btnBrowseMap, 2, 0);

            btnLoad = new Button { Text = "Load" };
            btnLoad.Click += (s, e) => { LoadMap(); };
            top.Controls.Add(btnLoad, 3, 0);

            btnSave = new Button { Text = "Save" };
            btnSave.Click += (s, e) => SaveMap();
            top.Controls.Add(btnSave, 4, 0);
            //First row END

            //Second row START
            top.Controls.Add(new Label { Text = "OneDrive folder", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            txtOneDrive = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            txtOneDrive.TextChanged += (s, e) => { if (_ready) SaveConfig(); };
            txtOneDrive.Text = Environment.GetEnvironmentVariable("OneDrive") ?? "";
            top.Controls.Add(txtOneDrive, 1, 1);
            top.SetColumnSpan(txtOneDrive, 7);

            btnRun = new Button { Text = "Link folder", Width = 120, BackColor=System.Drawing.Color.LightGreen };
            btnRun.Click += (s, e) => LinkOneDriveLocal();
            top.Controls.Add(btnRun, 1, 2);

            btnOpenReal = new Button { Text = "OneDrive"};
            btnOpenReal.Click += (s, e) => OpenRealPathOfSelected();
            top.Controls.Add(btnOpenReal, 2, 2);

            //btnCopyReal = new Button { Text = "Copy Path"};
            //btnCopyReal.Click += (s, e) => CopyRealPathOfSelected();
            //top.Controls.Add(btnCopyReal, 3, 2);

            btnOpenReal = new Button { Text = "Local" };
            btnOpenReal.Click += (s, e) => OpenLocalPathOfSelected();
            top.Controls.Add(btnOpenReal, 3, 2);
            //Second row END

            // --- DataGridView ---
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            var colSel = new DataGridViewCheckBoxColumn { HeaderText = "", Width = 30, DataPropertyName = "Selected" };
            grid.Columns.Add(colSel);
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Alias (GameName)", DataPropertyName = "Alias", Width = 220 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OneDrive save folder (real files here)", DataPropertyName = "RealPath", Width = 600 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Local save folder (what the game sees)", DataPropertyName = "LocalPath", Width = 300 });

            grid.DataSource = rows;
            grid.CellEndEdit += (s, e) => UpdateResolvedLabel();
            grid.SelectionChanged += (s, e) => UpdateResolvedLabel();

            // --- Bottom Panel for Action Buttons ---
            // Note: Its Dock is now Fill to occupy the third row of mainPanel.
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 48, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
            btnAdd = new Button { Text = "Add Row" };
            btnAdd.Click += (s, e) => { rows.Add(new GameRow()); try { if (grid.Rows.Count > 0) { grid.FirstDisplayedScrollingRowIndex = grid.Rows.Count - 1; grid.ClearSelection(); grid.Rows[grid.Rows.Count - 1].Selected = true; } } catch { } };
            bottom.Controls.Add(btnAdd);

            btnRemove = new Button { Text = "Remove (checked or current)" };
            btnRemove.Click += (s, e) => RemoveCheckedOrCurrent();
            bottom.Controls.Add(btnRemove);

            lblResolved = new Label { AutoSize = true, Padding = new Padding(16, 8, 8, 8) };
            bottom.Controls.Add(lblResolved);

            lblStatus = new Label { AutoSize = true, Padding = new Padding(16, 8, 8, 8) };
            bottom.Controls.Add(lblStatus);

            // --- Add all sections to the master panel ---
            mainPanel.Controls.Add(top, 0, 0);
            mainPanel.Controls.Add(grid, 0, 1);
            mainPanel.Controls.Add(bottom, 0, 2);

            // --- Add ONLY the master panel to the form ---
            this.Controls.Add(mainPanel);

            // --- Final Initialization Logic ---
            // default map path next to exe if exists or to Documents
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var defaultMap = Path.Combine(exeDir, _defaultMappingFileName);
            txtMapPath.Text = File.Exists(defaultMap)
                ? defaultMap
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), _defaultMappingFileName);

            // Load persisted config now that controls exist
            LoadConfig();

            // Load mapping file
            LoadMap();

            _ready = true;
        }

        void BrowseMap()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Mapping File (.txt).";
                dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (!string.IsNullOrWhiteSpace(txtMapPath.Text))
                {
                    try { dlg.InitialDirectory = Path.GetDirectoryName(txtMapPath.Text); } catch { }
                }
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtMapPath.Text = dlg.FileName;
                    LoadMap();
                
                    if (_ready) SaveConfig();
                }
            }
        }
        
        void LoadMap()
        {
            rows.Clear();
            var path = txtMapPath.Text.Trim();
            if (!File.Exists(path))
            {
                MessageBox.Show(this, "File not found. A new file will be created when you save.", "Info");
                UpdateResolvedLabel();
                return;
            }

            string[] lines;
            try
            {
                // Use automatic encoding detection (BOM) first; fallback to UTF8
                lines = File.ReadAllLines(path);
            }
            catch
            {
                lines = File.ReadAllLines(path, new UTF8Encoding(true));
            }

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                // Split into up to 3 parts only
                var parts = line.Split(new[] { '|' }, 3);
                if (parts.Length < 2) continue;

                var row = new GameRow
                {
                    Selected = false,
                    Alias = parts[0].Trim(),
                    RealPath = parts[1].Trim(),
                    LocalPath = parts.Length >= 3 ? parts[2].Trim() : ""
                };

                rows.Add(row);
            }
            UpdateResolvedLabel();
            lblStatus.Text = $"Loaded {rows.Count} entr{(rows.Count == 1 ? "y" : "ies")}.";
            try { if (grid.Rows.Count > 0) { grid.FirstDisplayedScrollingRowIndex = 0; grid.ClearSelection(); grid.Rows[0].Selected = true; } } catch { }
        }

        void SaveMap()
        {
            var path = txtMapPath.Text.Trim();
            var sb = new StringBuilder();
            sb.AppendLine("# Mapping file managed by Game Save Link Manager");
            sb.AppendLine("# Format: GameName|TargetPath|LocalSavePath");
            sb.AppendLine("# TargetPath: path to save folder on OneDrive (where the real files live)");
            sb.AppendLine("# LocalSavePath: path to save folder of the game (the game will attempt to save to and load from this folder)");
            sb.AppendLine();

            foreach (var r in rows.Where(r => !string.IsNullOrWhiteSpace(r.Alias) && !string.IsNullOrWhiteSpace(r.RealPath)))
            {
                sb.Append(r.Alias).Append('|').Append(r.RealPath);
                if (!string.IsNullOrWhiteSpace(r.LocalPath))
                    sb.Append('|').Append(r.LocalPath);
                sb.AppendLine();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            lblStatus.Text = "Saved.";
            SaveConfig();
        }

        void RemoveCheckedOrCurrent()
        {
            var toRemove = rows.Where(r => r.Selected).ToList();
            if (toRemove.Count > 0)
            {
                foreach (var r in toRemove) rows.Remove(r);
            }
            else if (grid.CurrentRow != null && grid.CurrentRow.Index >= 0 && grid.CurrentRow.Index < rows.Count)
            {
                rows.RemoveAt(grid.CurrentRow.Index);
            }
            UpdateResolvedLabel();
        }

        void UpdateResolvedLabel()
        {
            var r = GetSelectedRow();
            if (r == null)
            {
                lblResolved.Text = "Resolved real path: (no selection)";
                return;
            }
            ComputeRealPath(r, out var resolved, out var leaf);
            r.ResolvedRealPath = resolved;
            lblResolved.Text = $"Resolved real path: {resolved}";
        }

        GameRow GetSelectedRow()
        {
            if (grid.CurrentRow == null) return null;
            int idx = grid.CurrentRow.Index;
            if (idx < 0 || idx >= rows.Count) return null;
            return rows[idx];
        }

        void ComputeRealPath(GameRow row, out string real, out string leaf)
        {
            string oneDrive = Environment.ExpandEnvironmentVariables(txtOneDrive.Text.Trim());
            string targetRaw = Environment.ExpandEnvironmentVariables(row.RealPath ?? "").Trim();
            string local = Environment.ExpandEnvironmentVariables(row.LocalPath ?? "").Trim();

            bool isAbsolute = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(targetRaw))
                {
                    if (Path.IsPathRooted(targetRaw) || targetRaw.StartsWith(@"\\") || (targetRaw.Length >= 2 && targetRaw[1] == ':'))
                        isAbsolute = true;
                }
            }
            catch { }

            if (isAbsolute)
                real = targetRaw;
            else
                real = string.IsNullOrWhiteSpace(oneDrive) ? "" : Path.Combine(oneDrive, targetRaw);

            // Leaf name from LocalPath last segment; fallback to real's last segment
            leaf = "";
            try { if (!string.IsNullOrWhiteSpace(local)) leaf = new DirectoryInfo(local).Name; } catch { }
            if (string.IsNullOrWhiteSpace(leaf))
            {
                try { leaf = new DirectoryInfo(string.IsNullOrWhiteSpace(real) ? "SaveFolder" : real).Name; } catch { leaf = "SaveFolder"; }
            }
        }

        /// <summary>
        /// Open OneDrive (real path) of selected game (OneDrive button click)
        /// </summary>
        void OpenRealPathOfSelected()
        {
            var r = GetSelectedRow();
            if (r == null) return;
            ComputeRealPath(r, out var real, out var _);
            if (string.IsNullOrWhiteSpace(real))
            {
                MessageBox.Show(this, "Cannot resolve real path. Ensure %OneDrive% is set or TargetPath is absolute.", "Error");
                return;
            }
            try
            {
                if (!Directory.Exists(real))
                {
                    var res = MessageBox.Show(this, $"Folder does not exist:\n{real}\n\nCreate it?", "Create folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                        Directory.CreateDirectory(real);
                    else
                        return;
                }
                Process.Start("explorer.exe", real);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Open Real Path");
            }
        }

        /// <summary>
        /// Open Local button click
        /// </summary>
        void OpenLocalPathOfSelected()
        {
            var r = GetSelectedRow();
            if (r == null) return;
            //ComputeRealPath(r, out var _, out var local);
            var local = Environment.ExpandEnvironmentVariables(r.LocalPath ?? "").Trim();
            if (string.IsNullOrWhiteSpace(local))
            {
                MessageBox.Show(this, "Cannot resolve real path. Ensure %OneDrive% is set or TargetPath is absolute.", "Error");
                return;
            }
            try
            {
                if (!Directory.Exists(local))
                {
                    var res = MessageBox.Show(this, $"Folder does not exist:\n{local}\n\nCreate it?", "Create folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                        Directory.CreateDirectory(local);
                    else
                        return;
                }
                Process.Start("explorer.exe", local);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Open Real Path");
            }
        }

        /// <summary>
        /// Copy OneDrive (real path) of selected game (Copy Path button click)
        /// </summary>
        void CopyRealPathOfSelected()
        {
            var r = GetSelectedRow();
            if (r == null) return;
            ComputeRealPath(r, out var real, out var _);
            if (string.IsNullOrWhiteSpace(real))
            {
                MessageBox.Show(this, "Cannot resolve real path to copy.", "Error");
                return;
            }
            try
            {
                Clipboard.SetText(real);
                lblStatus.Text = "Copied to clipboard.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Copy Real Path");
            }
        }

        string GetConfigPath()
        {
            var app = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(app, "GameSaveLinkManager");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "config.ini");
        }

        void LoadConfig()
        {
            try
            {
                var cfg = GetConfigPath();
                if (!File.Exists(cfg)) return;
                foreach (var raw in File.ReadAllLines(cfg, Encoding.UTF8))
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var key = line.Substring(0, idx).Trim();
                    var val = line.Substring(idx + 1).Trim();
                    if (key.Equals("MapPath", StringComparison.OrdinalIgnoreCase)) txtMapPath.Text = val;
                    //else if (key.Equals("ScriptPath", StringComparison.OrdinalIgnoreCase)) txtScriptPath.Text = val;
                    else if (key.Equals("OneDrive", StringComparison.OrdinalIgnoreCase)) txtOneDrive.Text = val;
                }
            }
            catch { }
        }

        void SaveConfig()
        {
            if (!_ready) return;

            try
            {
                var cfg = GetConfigPath();
                var sb = new StringBuilder();
                sb.AppendLine("# GameSaveLinkManager config");
                sb.AppendLine("MapPath=" + (txtMapPath.Text ?? ""));
                //sb.AppendLine("ScriptPath=" + (txtScriptPath.Text ?? ""));
                sb.AppendLine("OneDrive=" + (txtOneDrive.Text ?? ""));
                File.WriteAllText(cfg, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        /// <summary>
        /// Link save folders on OneDrive to save folder of the game (Link Folder button click)
        /// </summary>
        void LinkOneDriveLocal()
        {
            try
            {
                var gamesTxtPath = Path.Combine(txtMapPath.Text);
                if (!File.Exists(gamesTxtPath))
                {
                    MessageBox.Show("File does not exists: \n\n" + gamesTxtPath);
                    return;
                }

                LinkService.SetupJunctionsFromGamesTxt(gamesTxtPath); // logs to Console
                MessageBox.Show("Finished linking local save folders to OneDrive folders.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
