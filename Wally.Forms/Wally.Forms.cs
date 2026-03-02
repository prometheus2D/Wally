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
        private readonly WelcomePanel _welcomePanel;

        // ?? Splitters ???????????????????????????????????????????????????????

        private readonly ThemedSplitter _leftSplitter;
        private readonly ThemedSplitter _rightSplitter;
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
                ForeColor = WallyTheme.TextSecondary,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
            _lblActorCount = new ToolStripStatusLabel("Actors: 0")
            {
                ForeColor = WallyTheme.TextSecondary,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                Padding = new Padding(6, 0, 6, 0)
            };
            _lblSessionId = new ToolStripStatusLabel($"\u25CF {_environment.Logger.SessionId.ToString("N")[..8]}")
            {
                ForeColor = WallyTheme.TextMuted,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                ToolTipText = $"Session ID: {_environment.Logger.SessionId:N}",
                Padding = new Padding(6, 0, 6, 0)
            };

            _statusBar = new StatusStrip
            {
                BackColor = WallyTheme.StatusBarInactive,
                SizingGrip = true,
                Renderer = WallyTheme.CreateRenderer()
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

            // ?? Chat Panel (right side) ??
            _chatPanel = new ChatPanel
            {
                Dock = DockStyle.Right,
                Width = 420,
                MinimumSize = new Size(280, 0)
            };

            _rightSplitter = new ThemedSplitter
            {
                Dock = DockStyle.Right,
                Width = 3,
                BackColor = WallyTheme.Splitter,
                MinSize = 280
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

            // ?? Welcome Panel (fills remaining centre space) ??
            _welcomePanel = new WelcomePanel
            {
                Dock = DockStyle.Fill
            };

            // ?? Layout inside ToolStripContainer ??
            // WinForms docking order matters: Fill last, edges first.
            // Add order (reverse Z): Fill ? Bottom ? Right ? Left ? edges.
            var content = toolStripContainer1.ContentPanel;
            content.BackColor = WallyTheme.Surface0;
            content.Controls.Add(_welcomePanel);       // Fill (centre)
            content.Controls.Add(_bottomSplitter);     // Bottom splitter
            content.Controls.Add(_commandPanel);       // Bottom
            content.Controls.Add(_rightSplitter);      // Right splitter
            content.Controls.Add(_chatPanel);          // Right
            content.Controls.Add(_leftSplitter);       // Left splitter
            content.Controls.Add(_fileExplorer);       // Left

            Controls.Add(_statusBar);

            // ?? Wire child events ??
            _commandPanel.BindEnvironment(_environment);
            _chatPanel.BindEnvironment(_environment);

            _commandPanel.WorkspaceChanged += OnWorkspaceChanged;
            _chatPanel.CommandIssued += (_, cmd) =>
                _commandPanel.AppendLine($"  \u2192 {cmd}", WallyTheme.TextMuted);
            _fileExplorer.FileDoubleClicked += OnFileDoubleClicked;
            _fileExplorer.FileSelected += OnFileSelected;

            // ?? File menu ??
            openWorkspaceMenuItem.Click += OnOpenWorkspace;
            setupWorkspaceMenuItem.Click += OnSetupWorkspace;
            saveWorkspaceMenuItem.Click += OnSaveWorkspace;
            closeWorkspaceMenuItem.Click += OnCloseWorkspace;
            exitMenuItem.Click += (_, _) => Close();

            // ?? View menu ??
            refreshMenuItem.Click += (_, _) => _fileExplorer.Refresh();
            showExplorerMenuItem.CheckedChanged += (_, _) =>
                TogglePanel(_fileExplorer, _leftSplitter, showExplorerMenuItem.Checked);
            showChatMenuItem.CheckedChanged += (_, _) =>
                TogglePanel(_chatPanel, _rightSplitter, showChatMenuItem.Checked);
            showCommandMenuItem.CheckedChanged += (_, _) =>
                TogglePanel(_commandPanel, _bottomSplitter, showCommandMenuItem.Checked);

            // ?? Workspace menu ??
            reloadActorsMenuItem.Click += (_, _) =>
                _commandPanel.ExecuteCommand("reload-actors");
            listActorsMenuItem.Click += (_, _) =>
                _commandPanel.ExecuteCommand("list");
            workspaceInfoMenuItem.Click += (_, _) =>
                _commandPanel.ExecuteCommand("info");
            verifyWorkspaceMenuItem.Click += OnVerifyWorkspace;
            openWorkspaceFolderMenuItem.Click += OnOpenWorkspaceFolder;

            // ?? Main ToolStrip ??
            tsbOpen.Click += OnOpenWorkspace;
            tsbSetup.Click += OnSetupWorkspace;
            tsbSave.Click += OnSaveWorkspace;
            tsbRefresh.Click += (_, _) => _fileExplorer.Refresh();
            tsbReloadActors.Click += (_, _) =>
                _commandPanel.ExecuteCommand("reload-actors");
            tsbInfo.Click += (_, _) =>
                _commandPanel.ExecuteCommand("info");
            tsbClearChat.Click += (_, _) => _chatPanel.ClearMessages();

            // ?? Global shortcuts ??
            KeyPreview = true;
            KeyDown += OnGlobalKeyDown;

            // ?? Initial state — workspace panels hidden until loaded ??
            _fileExplorer.Visible = false;
            _leftSplitter.Visible = false;
            _chatPanel.Visible = false;
            _rightSplitter.Visible = false;
            showExplorerMenuItem.Checked = false;
            showChatMenuItem.Checked = false;
            UpdateWorkspaceGating();
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
            else if (e.KeyCode == Keys.F5 && _environment.HasWorkspace)
            {
                e.Handled = true;
                _fileExplorer.Refresh();
            }
            else if (e.Control && e.KeyCode == Keys.D1 && _environment.HasWorkspace)
            {
                e.Handled = true;
                if (!_fileExplorer.Visible) { showExplorerMenuItem.Checked = true; }
                _fileExplorer.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.D2 && _environment.HasWorkspace)
            {
                e.Handled = true;
                if (!_chatPanel.Visible) { showChatMenuItem.Checked = true; }
                _chatPanel.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.D3)
            {
                e.Handled = true;
                if (!_commandPanel.Visible) { showCommandMenuItem.Checked = true; }
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

        private void OnSaveWorkspace(object? sender, EventArgs e)
        {
            if (!_environment.HasWorkspace) return;

            try
            {
                _environment.SaveWorkspace();
                _commandPanel.AppendLine(
                    $"Workspace saved: {_environment.WorkspaceFolder}",
                    WallyTheme.Green);
            }
            catch (Exception ex)
            {
                _commandPanel.AppendLine($"Save failed: {ex.Message}", WallyTheme.Red);
            }
        }

        private void OnCloseWorkspace(object? sender, EventArgs e)
        {
            if (!_environment.HasWorkspace) return;

            string closedPath = _environment.WorkSource ?? "workspace";
            _environment.CloseWorkspace();

            // Clear and hide the file explorer.
            _fileExplorer.ClearTree();
            _fileExplorer.Visible = false;
            _leftSplitter.Visible = false;
            showExplorerMenuItem.Checked = false;

            // Hide the chat panel.
            _chatPanel.ClearMessages();
            _chatPanel.RefreshActorList();
            _chatPanel.RefreshModelList();
            _chatPanel.Visible = false;
            _rightSplitter.Visible = false;
            showChatMenuItem.Checked = false;

            RefreshAllPanels();

            _commandPanel.AppendLine(
                $"Closed workspace: {closedPath}", WallyTheme.TextMuted);
        }

        private void OnVerifyWorkspace(object? sender, EventArgs e)
        {
            if (!_environment.HasWorkspace) return;
            _commandPanel.ExecuteCommand($"setup \"{_environment.WorkSource}\" --verify");
        }

        private void OnOpenWorkspaceFolder(object? sender, EventArgs e)
        {
            if (!_environment.HasWorkspace || _environment.WorkspaceFolder == null) return;
            try
            {
                System.Diagnostics.Process.Start("explorer.exe",
                    $"\"{_environment.WorkspaceFolder}\"");
            }
            catch (Exception ex)
            {
                _commandPanel.AppendLine(
                    $"Could not open folder: {ex.Message}", WallyTheme.Red);
            }
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

                // Show the explorer and chat panels if they were hidden.
                if (!_fileExplorer.Visible)
                {
                    showExplorerMenuItem.Checked = true;
                    _fileExplorer.Visible = true;
                    _leftSplitter.Visible = true;
                }
                if (!_chatPanel.Visible)
                {
                    showChatMenuItem.Checked = true;
                    _chatPanel.Visible = true;
                    _rightSplitter.Visible = true;
                }

                // Update welcome panel to show workspace info.
                string? defaultModel = _environment.Workspace?.Config?.DefaultModel;
                _welcomePanel.SetWorkspaceInfo(true, _environment.WorkSource,
                    _environment.Actors.Count, defaultModel);

                _lblWorkspaceStatus.Text = _environment.WorkSource!;
                _lblWorkspaceStatus.ForeColor = Color.White;
                _lblActorCount.Text = $"Actors: {_environment.Actors.Count}";
                _statusBar.BackColor = WallyTheme.StatusBarActive;
            }
            else
            {
                Text = "Wally \u2014 AI Actor Environment";
                _welcomePanel.SetWorkspaceInfo(false);
                _lblWorkspaceStatus.Text = "No workspace loaded \u2014 use File \u2192 Open or Setup";
                _lblWorkspaceStatus.ForeColor = WallyTheme.TextSecondary;
                _lblActorCount.Text = "Actors: 0";
                _statusBar.BackColor = WallyTheme.StatusBarInactive;
            }

            UpdateWorkspaceGating();
        }

        // ?? Workspace-gated UI ??????????????????????????????????????????????

        /// <summary>
        /// Enables or disables UI elements that require a loaded workspace.
        /// Called on startup and after every workspace state change.
        /// Workspace panels (explorer, chat) are hidden when no workspace;
        /// their View menu toggles are disabled. The command panel and
        /// welcome panel remain always visible.
        /// </summary>
        private void UpdateWorkspaceGating()
        {
            bool loaded = _environment.HasWorkspace;

            // ?? File menu ??
            saveWorkspaceMenuItem.Enabled = loaded;
            closeWorkspaceMenuItem.Enabled = loaded;

            // ?? View menu — panel toggles only when workspace loaded ??
            showExplorerMenuItem.Enabled = loaded;
            showChatMenuItem.Enabled = loaded;
            refreshMenuItem.Enabled = loaded;

            // ?? Workspace menu (entire menu disabled when no workspace) ??
            workspaceToolStripMenuItem.Enabled = loaded;

            // ?? ToolStrip buttons ??
            tsbSave.Enabled = loaded;
            tsbRefresh.Enabled = loaded;
            tsbReloadActors.Enabled = loaded;
            tsbInfo.Enabled = loaded;
            tsbClearChat.Enabled = loaded;

            // ?? Chat panel workspace awareness ??
            _chatPanel.SetWorkspaceLoaded(loaded);
        }

        // ?? File events ?????????????????????????????????????????????????????

        private void OnFileDoubleClicked(object? sender, FileSelectedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(e.FilePath)
                    { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                _commandPanel.AppendLine(
                    $"Could not open file: {ex.Message}", WallyTheme.Red);
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
