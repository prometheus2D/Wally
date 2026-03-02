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

        /// <summary>
        /// Reference to the ToolStripContainer's ContentPanel — the parent
        /// into which workspace panels are added and removed dynamically.
        /// Cached once so every Add/Remove call doesn't re-navigate the tree.
        /// </summary>
        private ToolStripContentPanel _content = null!;

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

            // ?? File Explorer (left) — created but NOT added to Controls yet ??
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

            // ?? Chat Panel (right) — created but NOT added to Controls yet ??
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

            // ?? Command Panel (bottom) — always present ??
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

            // ?? Welcome Panel (fills remaining centre space) — always present ??
            _welcomePanel = new WelcomePanel
            {
                Dock = DockStyle.Fill
            };

            // ?? Layout inside ToolStripContainer ??
            // Only the always-visible panels are added at startup.
            // Explorer, chat, and their splitters are injected/removed
            // dynamically by ShowWorkspacePanels / HideWorkspacePanels.
            _content = toolStripContainer1.ContentPanel;
            _content.BackColor = WallyTheme.Surface0;

            _content.Controls.Add(_welcomePanel);      // Fill (centre)
            _content.Controls.Add(_bottomSplitter);    // Bottom splitter
            _content.Controls.Add(_commandPanel);      // Bottom

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
                TogglePanel(_fileExplorer, _leftSplitter, DockStyle.Left, showExplorerMenuItem.Checked);
            showChatMenuItem.CheckedChanged += (_, _) =>
                TogglePanel(_chatPanel, _rightSplitter, DockStyle.Right, showChatMenuItem.Checked);
            showCommandMenuItem.CheckedChanged += (_, _) =>
                TogglePanel(_commandPanel, _bottomSplitter, DockStyle.Bottom, showCommandMenuItem.Checked);

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

            // ?? Initial state ??
            showExplorerMenuItem.Checked = false;
            showChatMenuItem.Checked = false;
            UpdateWorkspaceGating();
            TryAutoSetup();
        }

        // ?? Workspace panel add/remove ??????????????????????????????????????

        /// <summary>
        /// Injects the explorer, chat, and their splitters into the control
        /// tree. WinForms docked controls only participate in layout when
        /// they are in the Controls collection — <c>Visible = false</c> on a
        /// docked child does not reclaim its space in ToolStripContentPanel.
        /// </summary>
        private void ShowWorkspacePanels()
        {
            _content.SuspendLayout();

            // Right side: chat + splitter (added first so they dock before left).
            if (!_content.Controls.Contains(_chatPanel))
            {
                _content.Controls.Add(_rightSplitter);
                _content.Controls.Add(_chatPanel);
            }

            // Left side: explorer + splitter.
            if (!_content.Controls.Contains(_fileExplorer))
            {
                _content.Controls.Add(_leftSplitter);
                _content.Controls.Add(_fileExplorer);
            }

            _content.ResumeLayout(true);

            showExplorerMenuItem.Checked = true;
            showChatMenuItem.Checked = true;
        }

        /// <summary>
        /// Removes the explorer, chat, and their splitters from the control
        /// tree so they leave no layout footprint. The controls themselves
        /// stay alive in memory and are re-added on the next workspace load.
        /// </summary>
        private void HideWorkspacePanels()
        {
            _content.SuspendLayout();

            _content.Controls.Remove(_fileExplorer);
            _content.Controls.Remove(_leftSplitter);
            _content.Controls.Remove(_chatPanel);
            _content.Controls.Remove(_rightSplitter);

            _content.ResumeLayout(true);

            showExplorerMenuItem.Checked = false;
            showChatMenuItem.Checked = false;
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
                if (!showExplorerMenuItem.Checked) showExplorerMenuItem.Checked = true;
                _fileExplorer.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.D2 && _environment.HasWorkspace)
            {
                e.Handled = true;
                if (!showChatMenuItem.Checked) showChatMenuItem.Checked = true;
                _chatPanel.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.D3)
            {
                e.Handled = true;
                if (!showCommandMenuItem.Checked) showCommandMenuItem.Checked = true;
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
            using var dlg = new SetupDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedPath != null)
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

            // Remove workspace panels from the control tree.
            _fileExplorer.ClearTree();
            _chatPanel.ClearMessages();
            _chatPanel.RefreshActorList();
            _chatPanel.RefreshModelList();
            HideWorkspacePanels();

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

                // Inject workspace panels into the control tree if not present.
                if (!_content.Controls.Contains(_chatPanel))
                    ShowWorkspacePanels();

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
        /// Workspace panels (explorer, chat) are physically absent from the
        /// control tree when no workspace is loaded — their View menu toggles
        /// are disabled so users can see the options but can't activate them.
        /// </summary>
        private void UpdateWorkspaceGating()
        {
            bool loaded = _environment.HasWorkspace;

            // ?? File menu ??
            saveWorkspaceMenuItem.Enabled = loaded;
            closeWorkspaceMenuItem.Enabled = loaded;

            // ?? View menu — panel toggles disabled when no workspace ??
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

        /// <summary>
        /// Adds or removes a docked panel and its splitter from the content
        /// area. Using Controls.Add/Remove instead of Visible ensures the
        /// layout engine fully reclaims the space.
        /// </summary>
        private void TogglePanel(Control panel, Splitter splitter, DockStyle dock, bool show)
        {
            _content.SuspendLayout();

            if (show && !_content.Controls.Contains(panel))
            {
                _content.Controls.Add(splitter);
                _content.Controls.Add(panel);
            }
            else if (!show && _content.Controls.Contains(panel))
            {
                _content.Controls.Remove(panel);
                _content.Controls.Remove(splitter);
            }

            _content.ResumeLayout(true);
        }

        // ?? Cleanup ?????????????????????????????????????????????????????????

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _environment.Logger.Dispose();
            base.OnFormClosing(e);
        }
    }
}
