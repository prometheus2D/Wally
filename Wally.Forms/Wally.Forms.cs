using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Core.Providers;
using Wally.Forms.Controls;
using Wally.Forms.Controls.Editors;
using Wally.Forms.Theme;

namespace Wally.Forms
{
    public partial class WallyForms : Form
    {
        // -- Panels ----------------------------------------------------------

        private readonly ExplorerTabPanel  _explorerTabPanel;
        private readonly ChatPanel         _chatPanel;
        private readonly CommandPanel      _commandPanel;
        private readonly WelcomePanel      _welcomePanel;
        private readonly DocumentTabHost   _tabHost;

        // -- Splitters -------------------------------------------------------

        private readonly ThemedSplitter _leftSplitter;
        private readonly ThemedSplitter _rightSplitter;
        private readonly ThemedSplitter _bottomSplitter;

        // -- Status bar ------------------------------------------------------

        private readonly StatusStrip            _statusBar;
        private readonly ToolStripStatusLabel   _lblWorkspaceStatus;
        private readonly ToolStripStatusLabel   _lblActorCount;
        private readonly ToolStripStatusLabel   _lblSessionId;
        private readonly ToolStripProgressBar   _progressBar;

        // -- Runtime ---------------------------------------------------------

        private readonly WallyEnvironment _environment;

        /// <summary>
        /// Reference to the content panel — the parent into which workspace
        /// panels are added and removed dynamically.
        /// </summary>
        private Panel _content = null!;

        // -- Tab key constants -----------------------------------------------

        private const string TabKeyWelcome         = "__welcome__";
        private const string TabKeyConfig          = "__config__";
        private const string TabKeyLogs            = "__logs__";
        private const string TabKeyChatHistory     = "__chat_history__";
        private const string TabKeyPromptViewer    = "__prompt_viewer__";
        private const string TabKeyWorkspaceViewer = "__workspace_viewer__";

        // -- Constructor -----------------------------------------------------

        public WallyForms()
        {
            InitializeComponent();

            _environment = new WallyEnvironment();

            // -- Status bar --
            _lblWorkspaceStatus = new ToolStripStatusLabel("No workspace")
            {
                ForeColor = WallyTheme.TextSecondary,
                Spring    = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };
            _lblActorCount = new ToolStripStatusLabel("Actors: 0")
            {
                ForeColor   = WallyTheme.TextSecondary,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                Padding     = new Padding(6, 0, 6, 0)
            };
            _lblSessionId = new ToolStripStatusLabel(
                $"\u25CF {_environment.Logger.SessionId.ToString("N")[..8]}")
            {
                ForeColor   = WallyTheme.TextMuted,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                ToolTipText = $"Session ID: {_environment.Logger.SessionId:N}",
                Padding     = new Padding(6, 0, 6, 0)
            };

            _progressBar = new ToolStripProgressBar
            {
                Name                  = "progressBar",
                Width                 = 120,
                Minimum               = 0,
                Maximum               = 100,
                Style                 = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible               = false,
                Alignment             = ToolStripItemAlignment.Right,
                ToolTipText           = "Operation in progress\u2026"
            };

            _statusBar = new StatusStrip
            {
                BackColor  = WallyTheme.StatusBarInactive,
                SizingGrip = true,
                Renderer   = WallyTheme.CreateRenderer()
            };
            _statusBar.Items.AddRange(new ToolStripItem[]
            {
                _lblWorkspaceStatus, _lblActorCount, _lblSessionId, _progressBar
            });

            // -- Explorer Tab Panel (left) — created but NOT added to Controls yet --
            _explorerTabPanel = new ExplorerTabPanel
            {
                Dock        = DockStyle.Left,
                Width       = 280,
                MinimumSize = new Size(200, 0)
            };
            _explorerTabPanel.BindEnvironment(_environment);

            _leftSplitter = new ThemedSplitter
            {
                Dock      = DockStyle.Left,
                Width     = 3,
                BackColor = WallyTheme.Splitter,
                MinSize   = 200
            };

            // -- Chat Panel (right) — created but NOT added to Controls yet --
            _chatPanel = new ChatPanel
            {
                Dock        = DockStyle.Right,
                Width       = 420,
                MinimumSize = new Size(280, 0)
            };

            _rightSplitter = new ThemedSplitter
            {
                Dock      = DockStyle.Right,
                Width     = 3,
                BackColor = WallyTheme.Splitter,
                MinSize   = 280
            };

            // -- Command Panel (bottom) — always present --
            _commandPanel = new CommandPanel
            {
                Dock        = DockStyle.Bottom,
                Height      = 200,
                MinimumSize = new Size(0, 80)
            };

            _bottomSplitter = new ThemedSplitter
            {
                Dock      = DockStyle.Bottom,
                Height    = 3,
                BackColor = WallyTheme.Splitter,
                MinSize   = 80
            };

            // -- Welcome Panel (lives as a tab) --
            _welcomePanel = new WelcomePanel { Dock = DockStyle.Fill };

            // -- Document Tab Host (fills remaining centre space) --
            _tabHost = new DocumentTabHost { Dock = DockStyle.Fill };
            _tabHost.OpenTab(TabKeyWelcome, "Welcome", "\U0001F9E0", _welcomePanel);

            // -- Bind content panel --
            _content           = contentPanel;
            _content.BackColor = WallyTheme.Surface0;

            _content.Controls.Add(_tabHost);        // Fill (centre)
            _content.Controls.Add(_bottomSplitter); // Bottom splitter
            _content.Controls.Add(_commandPanel);   // Bottom

            Controls.Add(_statusBar);

            // -- Bind environments --
            _commandPanel.BindEnvironment(_environment);
            _chatPanel.BindEnvironment(_environment);

            // -- Wire child events --
            _commandPanel.WorkspaceChanged  += OnWorkspaceChanged;
            _commandPanel.RunningChanged    += OnRunningChanged;
            _chatPanel.RunningChanged       += OnRunningChanged;
            _chatPanel.CommandIssued        += OnChatCommandIssued;

            _explorerTabPanel.FileDoubleClicked += OnFileDoubleClicked;
            _explorerTabPanel.FileSelected      += OnFileSelected;
            _explorerTabPanel.ActorActivated    += (_, name) => { var a = _environment.GetActor(name); if (a != null) OpenActorEditor(a); };
            _explorerTabPanel.LoopActivated     += (_, name) => { var l = _environment.GetLoop(name);  if (l != null) OpenLoopEditor(l);  };
            _explorerTabPanel.WrapperActivated  += (_, name) =>
            {
                var w = _environment.Workspace?.LlmWrappers
                    .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (w != null) OpenWrapperEditor(w);
            };
            _explorerTabPanel.RunbookActivated  += (_, name) => { var r = _environment.GetRunbook(name); if (r != null) OpenRunbookEditor(r); };

            // -- File menu --
            openWorkspaceMenuItem.Click  += OnOpenWorkspace;
            setupWorkspaceMenuItem.Click += OnSetupWorkspace;
            saveWorkspaceMenuItem.Click  += OnSaveWorkspace;
            closeWorkspaceMenuItem.Click += OnCloseWorkspace;
            exitMenuItem.Click           += OnExit;

            // -- View menu --
            refreshMenuItem.Click               += OnRefreshExplorer;
            showExplorerMenuItem.CheckedChanged  += OnShowExplorerCheckedChanged;
            showChatMenuItem.CheckedChanged      += OnShowChatCheckedChanged;
            showCommandMenuItem.CheckedChanged   += OnShowCommandCheckedChanged;

            // -- Options menu --
            wordWrapMenuItem.CheckedChanged += OnWordWrapCheckedChanged;

            // -- Editors menu --
            editActorsMenuItem.Click          += OnEditActors;
            editLoopsMenuItem.Click           += OnEditLoops;
            editWrappersMenuItem.Click        += OnEditWrappers;
            editRunbooksMenuItem.Click        += OnEditRunbooks;
            editConfigMenuItem.Click          += OnEditConfig;
            viewLogsMenuItem.Click            += OnViewLogs;
            viewChatHistoryMenuItem.Click     += OnViewChatHistory;
            viewPromptViewerMenuItem.Click    += OnViewPromptViewer;
            viewWorkspaceViewerMenuItem.Click += OnViewWorkspaceViewer;
            closeAllEditorsMenuItem.Click     += OnCloseAllEditors;

            // -- Workspace menu --
            reloadActorsMenuItem.Click        += OnReloadActors;
            listActorsMenuItem.Click          += OnListActors;
            workspaceInfoMenuItem.Click       += OnWorkspaceInfo;
            verifyWorkspaceMenuItem.Click     += OnVerifyWorkspace;
            cleanupWorkspaceMenuItem.Click    += OnCleanupWorkspace;
            openWorkspaceFolderMenuItem.Click += OnOpenWorkspaceFolder;

            // -- File ToolStrip --
            tsbOpen.Click  += OnOpenWorkspace;
            tsbSetup.Click += OnSetupWorkspace;
            tsbSave.Click  += OnSaveWorkspace;
            tsbClose.Click += OnCloseWorkspace;

            // -- Workspace ToolStrip --
            tsbRefresh.Click      += OnRefreshExplorer;
            tsbReloadActors.Click += OnReloadActors;
            tsbInfo.Click         += OnWorkspaceInfo;
            tsbVerify.Click       += OnVerifyWorkspace;
            tsbStop.Click         += OnStopClick;

            // -- Editors ToolStrip --
            tsbEditActors.Click += OnEditActors;
            tsbConfig.Click     += OnEditConfig;
            tsbLogs.Click       += OnViewLogs;
            tsbClearChat.Click  += OnClearChat;

            // -- Global shortcuts --
            KeyPreview = true;
            KeyDown   += OnGlobalKeyDown;

            // -- Initial state --
            showExplorerMenuItem.Checked = false;
            showChatMenuItem.Checked     = false;
            UpdateWorkspaceGating();
            TryAutoSetup();
        }

        // -- Workspace panel add/remove --------------------------------------

        private void ShowWorkspacePanels()
        {
            _content.SuspendLayout();

            if (!_content.Controls.Contains(_chatPanel))
            {
                _content.Controls.Add(_rightSplitter);
                _content.Controls.Add(_chatPanel);
            }

            if (!_content.Controls.Contains(_explorerTabPanel))
            {
                _content.Controls.Add(_leftSplitter);
                _content.Controls.Add(_explorerTabPanel);
            }

            _content.ResumeLayout(true);

            showExplorerMenuItem.Checked = true;
            showChatMenuItem.Checked     = true;
        }

        private void HideWorkspacePanels()
        {
            _content.SuspendLayout();

            _content.Controls.Remove(_explorerTabPanel);
            _content.Controls.Remove(_leftSplitter);
            _content.Controls.Remove(_chatPanel);
            _content.Controls.Remove(_rightSplitter);

            _content.ResumeLayout(true);

            showExplorerMenuItem.Checked = false;
            showChatMenuItem.Checked     = false;
        }

        // -- Auto-setup ------------------------------------------------------

        private void TryAutoSetup()
        {
            string defaultWs  = WallyHelper.GetDefaultWorkspaceFolder();
            string configPath = Path.Combine(defaultWs, WallyHelper.ConfigFileName);
            if (!File.Exists(configPath)) return;

            try
            {
                _environment.LoadWorkspace(defaultWs);
                RefreshAllPanels();
                _commandPanel.AppendLine(
                    $"Loaded workspace: {_environment.WorkspaceFolder}",
                    WallyTheme.TextSecondary);
            }
            catch (Exception ex)
            {
                _commandPanel.AppendLine($"Auto-load failed: {ex.Message}", WallyTheme.Red);
            }
        }

        // -- Global keyboard shortcuts ---------------------------------------

        private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && tsbStop.Enabled)
            {
                e.Handled = true;
                OnStopClick(this, EventArgs.Empty);
            }
            else if (e.Control && e.KeyCode == Keys.Oem3) // Ctrl+`
            {
                e.Handled = true;
                _commandPanel.FocusInput();
            }
            else if (e.KeyCode == Keys.F5 && _environment.HasWorkspace)
            {
                e.Handled = true;
                _explorerTabPanel.Refresh();
            }
            else if (e.Control && e.KeyCode == Keys.D1 && _environment.HasWorkspace)
            {
                e.Handled = true;
                if (!showExplorerMenuItem.Checked) showExplorerMenuItem.Checked = true;
                _explorerTabPanel.FocusFiles();
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
            else if (e.Control && e.KeyCode == Keys.W)
            {
                e.Handled = true;
                var key = _tabHost.ActiveTabKey;
                if (key != null && key != TabKeyWelcome)
                    _tabHost.CloseTab(key);
            }
        }

        // -- Menu handlers ---------------------------------------------------

        private void OnEditCopy(object? sender, EventArgs e)
        {
            if (ActiveControl is RichTextBox rtb) rtb.Copy();
        }

        private void OnEditSelectAll(object? sender, EventArgs e)
        {
            if (ActiveControl is RichTextBox rtb) rtb.SelectAll();
        }

        private void OnExit(object? sender, EventArgs e)              => Close();
        private void OnRefreshExplorer(object? sender, EventArgs e)   => _explorerTabPanel.Refresh();

        private void OnShowExplorerCheckedChanged(object? sender, EventArgs e) =>
            TogglePanel(_explorerTabPanel, _leftSplitter, DockStyle.Left, showExplorerMenuItem.Checked);

        private void OnShowChatCheckedChanged(object? sender, EventArgs e) =>
            TogglePanel(_chatPanel, _rightSplitter, DockStyle.Right, showChatMenuItem.Checked);

        private void OnShowCommandCheckedChanged(object? sender, EventArgs e) =>
            TogglePanel(_commandPanel, _bottomSplitter, DockStyle.Bottom, showCommandMenuItem.Checked);

        private void OnWordWrapCheckedChanged(object? sender, EventArgs e) =>
            _tabHost.WordWrap = wordWrapMenuItem.Checked;

        private void OnEditActors(object? sender, EventArgs e)        => OpenActorPicker();
        private void OnEditLoops(object? sender, EventArgs e)         => OpenLoopPicker();
        private void OnEditWrappers(object? sender, EventArgs e)      => OpenWrapperPicker();
        private void OnEditRunbooks(object? sender, EventArgs e)      => OpenRunbookPicker();
        private void OnEditConfig(object? sender, EventArgs e)        => OpenConfigEditor();
        private void OnViewLogs(object? sender, EventArgs e)          => OpenLogViewer();
        private void OnViewChatHistory(object? sender, EventArgs e)   => OpenChatHistoryViewer();
        private void OnViewPromptViewer(object? sender, EventArgs e)  => OpenPromptViewer();
        private void OnViewWorkspaceViewer(object? sender, EventArgs e) => OpenWorkspaceViewer();
        private void OnCloseAllEditors(object? sender, EventArgs e)   => _tabHost.CloseAllTabs();
        private void OnReloadActors(object? sender, EventArgs e)      => _commandPanel.ExecuteCommand("reload-actors");
        private void OnListActors(object? sender, EventArgs e)        => _commandPanel.ExecuteCommand("list");
        private void OnWorkspaceInfo(object? sender, EventArgs e)     => _commandPanel.ExecuteCommand("info");
        private void OnClearChat(object? sender, EventArgs e)         => _chatPanel.ClearMessages();

        private void OnOpenWorkspace(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description          = "Select a .wally workspace folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton  = false
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
                    WallyTheme.TextSecondary);
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

            _explorerTabPanel.ClearAll();
            _chatPanel.ClearMessages();
            _chatPanel.RefreshActorList();
            _chatPanel.RefreshLoopList();
            _chatPanel.RefreshModelList();
            HideWorkspacePanels();
            _tabHost.CloseAllTabs();
            RefreshAllPanels();

            _commandPanel.AppendLine($"Closed workspace: {closedPath}", WallyTheme.TextMuted);
        }

        private void OnVerifyWorkspace(object? sender, EventArgs e)
        {
            if (!_environment.HasWorkspace) return;
            _commandPanel.ExecuteCommand($"setup \"{_environment.WorkSource}\" --verify");
        }

        private void OnCleanupWorkspace(object? sender, EventArgs e)
        {
            string wsFolder = _environment.HasWorkspace
                ? _environment.WorkspaceFolder!
                : WallyHelper.GetDefaultWorkspaceFolder();

            var result = MessageBox.Show(this,
                $"This will delete the workspace folder:\n\n{wsFolder}\n\n" +
                "All actors, config, docs, and logs inside it will be removed.\n" +
                "You can run Setup again afterwards to create a fresh workspace.\n\nContinue?",
                "Cleanup Workspace",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            string? workSource = _environment.HasWorkspace ? _environment.WorkSource : null;

            if (_environment.HasWorkspace)
            {
                _explorerTabPanel.ClearAll();
                _chatPanel.ClearMessages();
                _chatPanel.RefreshActorList();
                _chatPanel.RefreshLoopList();
                _chatPanel.RefreshModelList();
                HideWorkspacePanels();
                _tabHost.CloseAllTabs();
            }

            _commandPanel.ExecuteCommand(workSource != null
                ? $"cleanup \"{workSource}\""
                : "cleanup");
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
                _commandPanel.AppendLine($"Could not open folder: {ex.Message}", WallyTheme.Red);
            }
        }

        // -- Panel sync ------------------------------------------------------

        private void OnChatCommandIssued(object? sender, string cmd) =>
            _commandPanel.AppendLine($"  \u2192 {cmd}", WallyTheme.TextMuted);

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
                _explorerTabPanel.SetRootPath(_environment.WorkSource!);
                _chatPanel.RefreshActorList();
                _chatPanel.RefreshLoopList();
                _chatPanel.RefreshModelList();

                if (!_content.Controls.Contains(_chatPanel))
                    ShowWorkspacePanels();

                string? defaultModel = _environment.Workspace?.Config?.DefaultModel;
                _welcomePanel.SetWorkspaceInfo(true, _environment.WorkSource,
                    _environment.Actors.Count, defaultModel);

                _lblWorkspaceStatus.Text      = _environment.WorkSource!;
                _lblWorkspaceStatus.ForeColor = WallyTheme.TextPrimary;
                _lblActorCount.Text           = $"Actors: {_environment.Actors.Count}";
                _statusBar.BackColor          = WallyTheme.StatusBarActive;
            }
            else
            {
                Text = "Wally \u2014 AI Actor Environment";
                _welcomePanel.SetWorkspaceInfo(false);
                _lblWorkspaceStatus.Text      = "No workspace loaded \u2014 use File \u2192 Open or Setup";
                _lblWorkspaceStatus.ForeColor = WallyTheme.TextSecondary;
                _lblActorCount.Text           = "Actors: 0";
                _statusBar.BackColor          = WallyTheme.StatusBarInactive;
            }

            UpdateWorkspaceGating();
        }

        // -- Workspace-gated UI ----------------------------------------------

        private void UpdateWorkspaceGating()
        {
            bool loaded = _environment.HasWorkspace;

            saveWorkspaceMenuItem.Enabled  = loaded;
            closeWorkspaceMenuItem.Enabled = loaded;
            showExplorerMenuItem.Enabled   = loaded;
            showChatMenuItem.Enabled       = loaded;
            refreshMenuItem.Enabled        = loaded;

            workspaceToolStripMenuItem.Enabled = loaded;
            editorsToolStripMenuItem.Enabled   = loaded;

            // File toolbar
            tsbSave.Enabled  = loaded;
            tsbClose.Enabled = loaded;

            // Workspace toolbar
            tsbRefresh.Enabled      = loaded;
            tsbReloadActors.Enabled = loaded;
            tsbInfo.Enabled         = loaded;
            tsbVerify.Enabled       = loaded;

            // Editors toolbar
            tsbEditActors.Enabled = loaded;
            tsbConfig.Enabled     = loaded;
            tsbLogs.Enabled       = loaded;
            tsbClearChat.Enabled  = loaded;

            bool anyRunning = _chatPanel.IsRunning || _commandPanel.IsRunning;
            tsbStop.Enabled   = anyRunning;
            tsbStop.ForeColor = anyRunning ? WallyTheme.Red : WallyTheme.TextDisabled;

            _chatPanel.SetWorkspaceLoaded(loaded);
        }

        // -- File events / intelligent open ----------------------------------

        private void OnFileDoubleClicked(object? sender, FileSelectedEventArgs e)
        {
            if (_environment.HasWorkspace && TryOpenFileInEditor(e.FilePath)) return;

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(e.FilePath)
                    { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                _commandPanel.AppendLine($"Could not open file: {ex.Message}", WallyTheme.Red);
            }
        }

        private bool TryOpenFileInEditor(string filePath)
        {
            if (!_environment.HasWorkspace) return false;

            string wsFolder  = _environment.WorkspaceFolder!;
            var    config    = _environment.Workspace!.Config;
            string relative  = Path.GetRelativePath(wsFolder, filePath);

            string actorsPrefix = config.ActorsFolderName + Path.DirectorySeparatorChar;
            if (relative.StartsWith(actorsPrefix, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(filePath).Equals(WallyHelper.ActorFileName, StringComparison.OrdinalIgnoreCase))
            {
                string afterActors = relative[actorsPrefix.Length..];
                string actorName   = afterActors.Split(Path.DirectorySeparatorChar)[0];
                var    actor       = _environment.GetActor(actorName);
                if (actor != null) { OpenActorEditor(actor); return true; }
            }

            string loopsPrefix = config.LoopsFolderName + Path.DirectorySeparatorChar;
            if (relative.StartsWith(loopsPrefix, StringComparison.OrdinalIgnoreCase) &&
                filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var loop = _environment.GetLoop(Path.GetFileNameWithoutExtension(filePath));
                if (loop != null) { OpenLoopEditor(loop); return true; }
            }

            string wrappersPrefix = config.WrappersFolderName + Path.DirectorySeparatorChar;
            if (relative.StartsWith(wrappersPrefix, StringComparison.OrdinalIgnoreCase) &&
                filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                string wrapperName = Path.GetFileNameWithoutExtension(filePath);
                var wrapper = _environment.Workspace.LlmWrappers
                    .FirstOrDefault(w => string.Equals(w.Name, wrapperName, StringComparison.OrdinalIgnoreCase));
                if (wrapper != null) { OpenWrapperEditor(wrapper); return true; }
            }

            string runbooksPrefix = config.RunbooksFolderName + Path.DirectorySeparatorChar;
            if (relative.StartsWith(runbooksPrefix, StringComparison.OrdinalIgnoreCase) &&
                filePath.EndsWith(".wrb", StringComparison.OrdinalIgnoreCase))
            {
                var runbook = _environment.GetRunbook(Path.GetFileNameWithoutExtension(filePath));
                if (runbook != null) { OpenRunbookEditor(runbook); return true; }
            }

            if (Path.GetFileName(filePath).Equals(WallyHelper.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                OpenConfigEditor();
                return true;
            }

            return false;
        }

        private void OnFileSelected(object? sender, FileSelectedEventArgs e)
        {
            if (_environment.HasWorkspace)
                _lblWorkspaceStatus.Text = Path.GetRelativePath(_environment.WorkSource!, e.FilePath);
        }

        // -- Editor open helpers ---------------------------------------------

        private void OpenActorEditor(Actor actor)
        {
            string key = $"actor:{actor.Name}";
            if (_tabHost.SelectTab(key)) return;
            var editor = new ActorEditorPanel();
            editor.BindEnvironment(_environment);
            editor.LoadActor(actor);
            editor.DirtyChanged += (_, _) => _tabHost.SetTabDirty(key, editor.IsDirty);
            _tabHost.OpenTab(key, actor.Name, "\U0001F3AD", editor);
        }

        private void OpenLoopEditor(WallyLoopDefinition loop)
        {
            string key = $"loop:{loop.Name}";
            if (_tabHost.SelectTab(key)) return;
            var editor = new LoopEditorPanel();
            editor.BindEnvironment(_environment);
            editor.LoadLoop(loop);
            editor.DirtyChanged += (_, _) => _tabHost.SetTabDirty(key, editor.IsDirty);
            _tabHost.OpenTab(key, loop.Name, "\u267B", editor);
        }

        private void OpenWrapperEditor(LLMWrapper wrapper)
        {
            string key = $"wrapper:{wrapper.Name}";
            if (_tabHost.SelectTab(key)) return;
            var editor = new WrapperEditorPanel();
            editor.BindEnvironment(_environment);
            editor.LoadWrapper(wrapper);
            editor.DirtyChanged += (_, _) => _tabHost.SetTabDirty(key, editor.IsDirty);
            _tabHost.OpenTab(key, wrapper.Name, "\u2699", editor);
        }

        private void OpenRunbookEditor(WallyRunbook runbook)
        {
            string key = $"runbook:{runbook.Name}";
            if (_tabHost.SelectTab(key)) return;
            var editor = new RunbookEditorPanel();
            editor.LoadRunbook(runbook);
            editor.DirtyChanged += (_, _) => _tabHost.SetTabDirty(key, editor.IsDirty);
            _tabHost.OpenTab(key, runbook.Name, "\uD83D\uDCDC", editor);
        }

        private void OpenConfigEditor()
        {
            if (!_environment.HasWorkspace || _tabHost.SelectTab(TabKeyConfig)) return;
            var editor = new ConfigEditorPanel();
            editor.BindEnvironment(_environment);
            editor.LoadConfig();
            editor.DirtyChanged += (_, _) => _tabHost.SetTabDirty(TabKeyConfig, editor.IsDirty);
            _tabHost.OpenTab(TabKeyConfig, "Config", "\u2699", editor);
        }

        private void OpenLogViewer()
        {
            if (!_environment.HasWorkspace || _tabHost.SelectTab(TabKeyLogs)) return;
            var viewer = new LogViewerPanel();
            viewer.BindEnvironment(_environment);
            viewer.RefreshSessions();
            _tabHost.OpenTab(TabKeyLogs, "Logs", "\uD83D\uDCCB", viewer);
        }

        private void OpenChatHistoryViewer()
        {
            if (!_environment.HasWorkspace || _tabHost.SelectTab(TabKeyChatHistory)) return;
            var viewer = new ChatHistoryViewerPanel();
            viewer.BindEnvironment(_environment);
            viewer.RefreshHistory();
            _tabHost.OpenTab(TabKeyChatHistory, "Chat History", "\uD83D\uDCAC", viewer);
        }

        private void OpenPromptViewer()
        {
            if (!_environment.HasWorkspace || _tabHost.SelectTab(TabKeyPromptViewer)) return;
            var viewer = new ChatPanelPromptViewer();
            viewer.BindEnvironment(_environment);
            _tabHost.OpenTab(TabKeyPromptViewer, "Prompt Viewer", "\uD83D\uDD0D", viewer);
        }

        private void OpenWorkspaceViewer()
        {
            if (!_environment.HasWorkspace || _tabHost.SelectTab(TabKeyWorkspaceViewer)) return;
            var viewer = new WorkspaceViewerPanel();
            viewer.BindEnvironment(_environment);
            viewer.BuildView();
            _tabHost.OpenTab(TabKeyWorkspaceViewer, "Workspace", "\uD83D\uDCCA", viewer);
        }

        // -- Entity pickers --------------------------------------------------

        private void OpenActorPicker()
        {
            if (!_environment.HasWorkspace) return;
            var actors = _environment.Actors;
            if (actors.Count == 0) { _commandPanel.AppendLine("No actors loaded.", WallyTheme.TextMuted); return; }
            if (actors.Count == 1) { OpenActorEditor(actors[0]); return; }
            string? chosen = ShowQuickPicker("Select Actor", actors.Select(a => a.Name).ToArray());
            if (chosen != null) { var a = _environment.GetActor(chosen); if (a != null) OpenActorEditor(a); }
        }

        private void OpenLoopPicker()
        {
            if (!_environment.HasWorkspace) return;
            var loops = _environment.Loops;
            if (loops.Count == 0) { _commandPanel.AppendLine("No loops loaded.", WallyTheme.TextMuted); return; }
            if (loops.Count == 1) { OpenLoopEditor(loops[0]); return; }
            string? chosen = ShowQuickPicker("Select Loop", loops.Select(l => l.Name).ToArray());
            if (chosen != null) { var l = _environment.GetLoop(chosen); if (l != null) OpenLoopEditor(l); }
        }

        private void OpenWrapperPicker()
        {
            if (!_environment.HasWorkspace) return;
            var wrappers = _environment.Workspace!.LlmWrappers;
            if (wrappers.Count == 0) { _commandPanel.AppendLine("No wrappers loaded.", WallyTheme.TextMuted); return; }
            if (wrappers.Count == 1) { OpenWrapperEditor(wrappers[0]); return; }
            string? chosen = ShowQuickPicker("Select Wrapper", wrappers.Select(w => w.Name).ToArray());
            if (chosen != null)
            {
                var w = wrappers.FirstOrDefault(x => string.Equals(x.Name, chosen, StringComparison.OrdinalIgnoreCase));
                if (w != null) OpenWrapperEditor(w);
            }
        }

        private void OpenRunbookPicker()
        {
            if (!_environment.HasWorkspace) return;
            var runbooks = _environment.Runbooks;
            if (runbooks.Count == 0) { _commandPanel.AppendLine("No runbooks loaded.", WallyTheme.TextMuted); return; }
            if (runbooks.Count == 1) { OpenRunbookEditor(runbooks[0]); return; }
            string? chosen = ShowQuickPicker("Select Runbook", runbooks.Select(r => r.Name).ToArray());
            if (chosen != null) { var r = _environment.GetRunbook(chosen); if (r != null) OpenRunbookEditor(r); }
        }

        private string? ShowQuickPicker(string title, string[] items)
        {
            using var dlg = new Form
            {
                Text            = title,
                Size            = new Size(340, 320),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = WallyTheme.Surface1,
                ForeColor       = WallyTheme.TextPrimary,
                Font            = WallyTheme.FontUI
            };

            var list = new ListBox
            {
                Dock          = DockStyle.Fill,
                Font          = WallyTheme.FontUI,
                BackColor     = WallyTheme.Surface1,
                ForeColor     = WallyTheme.TextPrimary,
                BorderStyle   = BorderStyle.None,
                IntegralHeight = false
            };
            list.Items.AddRange(items);
            if (list.Items.Count > 0) list.SelectedIndex = 0;

            string? result = null;

            list.DoubleClick += (_, _) => { result = list.SelectedItem?.ToString(); dlg.DialogResult = DialogResult.OK; };
            list.KeyDown     += (_, ke) =>
            {
                if      (ke.KeyCode == Keys.Enter)  { result = list.SelectedItem?.ToString(); dlg.DialogResult = DialogResult.OK; }
                else if (ke.KeyCode == Keys.Escape) { dlg.DialogResult = DialogResult.Cancel; }
            };

            var btnOk = new Button
            {
                Text      = "Open",
                Dock      = DockStyle.Bottom,
                Height    = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3,
                ForeColor = WallyTheme.TextPrimary,
                Font      = WallyTheme.FontUIBold
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (_, _) => { result = list.SelectedItem?.ToString(); dlg.DialogResult = DialogResult.OK; };

            dlg.Controls.Add(list);
            dlg.Controls.Add(btnOk);

            return dlg.ShowDialog(this) == DialogResult.OK ? result : null;
        }

        // -- Panel toggles ---------------------------------------------------

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

        // -- Cleanup ---------------------------------------------------------

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _environment.Logger.Dispose();
            base.OnFormClosing(e);
        }

        // -- Stop button -----------------------------------------------------

        private void OnRunningChanged(object? sender, EventArgs e)
        {
            if (InvokeRequired) { Invoke(() => OnRunningChanged(sender, e)); return; }

            bool anyRunning = _chatPanel.IsRunning || _commandPanel.IsRunning;
            tsbStop.Enabled   = anyRunning;
            tsbStop.ForeColor = anyRunning ? WallyTheme.Red : WallyTheme.TextDisabled;

            _progressBar.Visible = anyRunning;
            _commandPanel.SetExternallyBusy(_chatPanel.IsRunning);
        }

        private void OnStopClick(object? sender, EventArgs e)
        {
            if (_chatPanel.IsRunning)    _chatPanel.Cancel();
            if (_commandPanel.IsRunning) _commandPanel.Cancel();
        }
    }
}
