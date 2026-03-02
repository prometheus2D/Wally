using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms
{
    public partial class WallyForms : Form
    {
        // ?? Panels ??????????????????????????????????????????????????????????

        private readonly FileExplorerPanel _fileExplorer;
        private readonly ChatPanel _chatPanel;
        private readonly CommandPanel _commandPanel;

        // ?? Splitters ???????????????????????????????????????????????????????

        private readonly ThemedSplitter _leftSplitter;
        private readonly ThemedSplitter _bottomSplitter;

        // ?? Status bar ??????????????????????????????????????????????????????

        private readonly StatusStrip _statusBar;
        private readonly ToolStripStatusLabel _lblWorkspaceStatus;
        private readonly ToolStripStatusLabel _lblActorCount;
        private readonly ToolStripStatusLabel _lblSessionId;

        // ?? Runtime ?????????????????????????????????????????????????????????

        private readonly WallyEnvironment _environment;

        // ?? Constructor ?????????????????????????????????????????????????????

        public WallyForms()
        {
            InitializeComponent();

            _environment = new WallyEnvironment();

            // ?? Status bar ??
            _lblWorkspaceStatus = new ToolStripStatusLabel("No workspace")
            {
                ForeColor = Color.FromArgb(200, 200, 200),
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
            _lblActorCount = new ToolStripStatusLabel("Actors: 0")
            {
                ForeColor = Color.FromArgb(180, 180, 180),
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                Padding = new Padding(6, 0, 6, 0)
            };
            _lblSessionId = new ToolStripStatusLabel($"\u25CF {_environment.Logger.SessionId.ToString("N")[..8]}")
            {
                ForeColor = Color.FromArgb(140, 140, 140),
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                ToolTipText = $"Session ID: {_environment.Logger.SessionId:N}",
                Padding = new Padding(6, 0, 6, 0)
            };

            _statusBar = new StatusStrip
            {
                BackColor = WallyTheme.StatusBarActive,
                SizingGrip = true,
                Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
            };
            _statusBar.Items.AddRange(new ToolStripItem[]
            {
                _lblWorkspaceStatus, _lblActorCount, _lblSessionId
            });

            // ?? File Explorer (left) ??
            _fileExplorer = new FileExplorerPanel
            {
                Dock = DockStyle.Left,
                Width = 260,
                MinimumSize = new Size(180, 0)
            };

            _leftSplitter = new ThemedSplitter
            {
                Dock = DockStyle.Left,
                Width = 3,
                BackColor = WallyTheme.Splitter,
                MinSize = 180
            };

            // ?? Command Panel (bottom) ??
            _commandPanel = new CommandPanel
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                MinimumSize = new Size(0, 80)
            };

            _bottomSplitter = new ThemedSplitter
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = WallyTheme.Splitter,
                MinSize = 80
            };

            // ?? Chat Panel (fills remaining space) ??
            _chatPanel = new ChatPanel
            {
                Dock = DockStyle.Fill
            };

            // ?? Layout inside ToolStripContainer ??
            var content = toolStripContainer1.ContentPanel;
            content.BackColor = WallyTheme.Surface0;
            content.Controls.Add(_chatPanel);
            content.Controls.Add(_bottomSplitter);
            content.Controls.Add(_commandPanel);
            content.Controls.Add(_leftSplitter);
            content.Controls.Add(_fileExplorer);

            Controls.Add(_statusBar);

            // ?? Wire events ??
            _commandPanel.BindEnvironment(_environment);
            _chatPanel.BindEnvironment(_environment);

            _commandPanel.WorkspaceChanged += OnWorkspaceChanged;
            _chatPanel.CommandIssued += (_, cmd) =>
                _commandPanel.AppendLine($"  \u2192 {cmd}", WallyTheme.TextMuted);
            _fileExplorer.FileDoubleClicked += OnFileDoubleClicked;
            _fileExplorer.FileSelected += OnFileSelected;

            // ?? Menu events ??
            openWorkspaceMenuItem.Click += OnOpenWorkspace;
            setupWorkspaceMenuItem.Click += OnSetupWorkspace;
            exitMenuItem.Click += (_, _) => Close();
            refreshMenuItem.Click += (_, _) => _fileExplorer.Refresh();

            showExplorerMenuItem.CheckedChanged += (_, _) =>
                TogglePanel(_fileExplorer, _leftSplitter, showExplorerMenuItem.Checked);
            showChatMenuItem.CheckedChanged += (_, _) =>
                _chatPanel.Visible = showChatMenuItem.Checked;
            showCommandMenuItem.CheckedChanged += (_, _) =>
                TogglePanel(_commandPanel, _bottomSplitter, showCommandMenuItem.Checked);

            // ?? Global shortcuts ??
            KeyPreview = true;
            KeyDown += OnGlobalKeyDown;

            // ?? Auto-setup ??
            TryAutoSetup();
        }

        // ?? Auto-setup ?????????????????????????????????????????????????????

        private void TryAutoSetup()
        {
            string defaultWs = WallyHelper.GetDefaultWorkspaceFolder();
            string configPath = Path.Combine(defaultWs, WallyHelper.ConfigFileName);
            if (!File.Exists(configPath)) return;

            try
            {
                _environment.LoadWorkspace(defaultWs);
                RefreshAllPanels();
                _commandPanel.AppendLine(
                    $"Loaded workspace: {_environment.WorkspaceFolder}",
                    WallyTheme.Green);
            }
            catch (Exception ex)
            {
                _commandPanel.AppendLine($"Auto-load failed: {ex.Message}", WallyTheme.Red);
            }
        }

        // ?? Global keyboard shortcuts ???????????????????????????????????????

        private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Oem3) // Ctrl+`
            {
                e.Handled = true;
                _commandPanel.FocusInput();
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                _fileExplorer.Refresh();
            }
            else if (e.Control && e.KeyCode == Keys.D1)
            {
                e.Handled = true;
                _fileExplorer.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.D2)
            {
                e.Handled = true;
                _chatPanel.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.D3)
            {
                e.Handled = true;
                _commandPanel.FocusInput();
            }
        }

        // ?? Menu handlers ???????????????????????????????????????????????????

        private void OnOpenWorkspace(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select a .wally workspace folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                _commandPanel.ExecuteCommand($"load \"{dlg.SelectedPath}\"");
        }

        private void OnSetupWorkspace(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select your codebase root. A .wally/ workspace will be created inside it.",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                _commandPanel.ExecuteCommand($"setup \"{dlg.SelectedPath}\"");
        }

        // ?? Panel sync ??????????????????????????????????????????????????????

        private void OnWorkspaceChanged(object? sender, EventArgs e)
        {
            if (InvokeRequired) { Invoke(() => OnWorkspaceChanged(sender, e)); return; }
            RefreshAllPanels();
        }

        private void RefreshAllPanels()
        {
            if (_environment.HasWorkspace)
            {
                Text = $"Wally \u2014 {_environment.WorkSource}";
                _fileExplorer.SetRootPath(_environment.WorkSource!);
                _chatPanel.RefreshActorList();
                _chatPanel.RefreshModelList();

                _lblWorkspaceStatus.Text = _environment.WorkSource!;
                _lblWorkspaceStatus.ForeColor = Color.White;
                _lblActorCount.Text = $"Actors: {_environment.Actors.Count}";
                _statusBar.BackColor = WallyTheme.StatusBarActive;
            }
            else
            {
                Text = "Wally \u2014 AI Actor Environment";
                _lblWorkspaceStatus.Text = "No workspace loaded";
                _lblWorkspaceStatus.ForeColor = Color.FromArgb(200, 200, 200);
                _lblActorCount.Text = "Actors: 0";
                _statusBar.BackColor = WallyTheme.StatusBarInactive;
            }
        }

        private void OnFileDoubleClicked(object? sender, FileSelectedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(e.FilePath) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                _commandPanel.AppendLine($"Could not open file: {ex.Message}", WallyTheme.Red);
            }
        }

        private void OnFileSelected(object? sender, FileSelectedEventArgs e)
        {
            if (_environment.HasWorkspace)
            {
                string relative = Path.GetRelativePath(_environment.WorkSource!, e.FilePath);
                _lblWorkspaceStatus.Text = relative;
            }
        }

        // ?? Panel toggles ???????????????????????????????????????????????????

        private static void TogglePanel(Control panel, Splitter splitter, bool visible)
        {
            panel.Visible = visible;
            splitter.Visible = visible;
        }

        // ?? Cleanup ?????????????????????????????????????????????????????????

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _environment.Logger.Dispose();
            base.OnFormClosing(e);
        }
    }
}
