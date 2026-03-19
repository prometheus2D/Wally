using Wally.Forms.Theme;

namespace Wally.Forms
{
    partial class WallyForms
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            menuPanel = new Panel();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openWorkspaceMenuItem = new ToolStripMenuItem();
            setupWorkspaceMenuItem = new ToolStripMenuItem();
            fileSeparator1 = new ToolStripSeparator();
            saveWorkspaceMenuItem = new ToolStripMenuItem();
            closeWorkspaceMenuItem = new ToolStripMenuItem();
            fileSeparator2 = new ToolStripSeparator();
            exitMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            editCopyMenuItem = new ToolStripMenuItem();
            editSelectAllMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            wordWrapMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            showExplorerMenuItem = new ToolStripMenuItem();
            showChatMenuItem = new ToolStripMenuItem();
            showCommandMenuItem = new ToolStripMenuItem();
            viewSeparator1 = new ToolStripSeparator();
            refreshMenuItem = new ToolStripMenuItem();
            editorsToolStripMenuItem = new ToolStripMenuItem();
            editActorsMenuItem = new ToolStripMenuItem();
            editLoopsMenuItem = new ToolStripMenuItem();
            editWrappersMenuItem = new ToolStripMenuItem();
            editRunbooksMenuItem = new ToolStripMenuItem();
            editorsSeparator1 = new ToolStripSeparator();
            editConfigMenuItem = new ToolStripMenuItem();
            viewLogsMenuItem = new ToolStripMenuItem();
            viewChatHistoryMenuItem = new ToolStripMenuItem();
            viewPromptViewerMenuItem = new ToolStripMenuItem();
            viewWorkspaceViewerMenuItem = new ToolStripMenuItem();
            editorsSeparator2 = new ToolStripSeparator();
            closeAllEditorsMenuItem = new ToolStripMenuItem();
            workspaceToolStripMenuItem = new ToolStripMenuItem();
            reloadActorsMenuItem = new ToolStripMenuItem();
            listActorsMenuItem = new ToolStripMenuItem();
            workspaceSeparator1 = new ToolStripSeparator();
            workspaceInfoMenuItem = new ToolStripMenuItem();
            verifyWorkspaceMenuItem = new ToolStripMenuItem();
            repairWorkspaceMenuItem = new ToolStripMenuItem();
            workspaceSeparator2 = new ToolStripSeparator();
            cleanupWorkspaceMenuItem = new ToolStripMenuItem();
            openWorkspaceFolderMenuItem = new ToolStripMenuItem();
            toolbarPanel = new ToolStripPanel();
            fileToolStrip = new WallyToolStrip();
            tsbOpen = new ToolStripButton();
            tsbSetup = new ToolStripButton();
            tsbSave = new ToolStripButton();
            tsFileSep1 = new ToolStripSeparator();
            tsbClose = new ToolStripButton();
            workspaceToolStrip = new WallyToolStrip();
            tsbRefresh = new ToolStripButton();
            tsbReloadActors = new ToolStripButton();
            tsWsSep1 = new ToolStripSeparator();
            tsbInfo = new ToolStripButton();
            tsbVerify = new ToolStripButton();
            tsbRepair = new ToolStripButton();
            tsWsSep2 = new ToolStripSeparator();
            tsbStop = new ToolStripButton();
            editorsToolStrip = new WallyToolStrip();
            tsbEditActors = new ToolStripButton();
            tsbConfig = new ToolStripButton();
            tsbLogs = new ToolStripButton();
            tsEdSep1 = new ToolStripSeparator();
            tsbClearChat = new ToolStripButton();
            contentPanel = new Panel();
            menuPanel.SuspendLayout();
            menuStrip1.SuspendLayout();
            toolbarPanel.SuspendLayout();
            fileToolStrip.SuspendLayout();
            workspaceToolStrip.SuspendLayout();
            editorsToolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // menuPanel
            // 
            menuPanel.BackColor = Color.FromArgb(39, 39, 44);
            menuPanel.Controls.Add(menuStrip1);
            menuPanel.Dock = DockStyle.Top;
            menuPanel.Location = new Point(0, 0);
            menuPanel.Name = "menuPanel";
            menuPanel.Size = new Size(1280, 24);
            menuPanel.TabIndex = 2;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = Color.FromArgb(39, 39, 44);
            menuStrip1.Dock = DockStyle.Fill;
            menuStrip1.ForeColor = Color.FromArgb(228, 228, 233);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, optionsToolStripMenuItem, viewToolStripMenuItem, editorsToolStripMenuItem, workspaceToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Renderer = WallyTheme.CreateRenderer();
            menuStrip1.Size = new Size(1280, 24);
            menuStrip1.TabIndex = 0;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openWorkspaceMenuItem, setupWorkspaceMenuItem, fileSeparator1, saveWorkspaceMenuItem, closeWorkspaceMenuItem, fileSeparator2, exitMenuItem });
            fileToolStripMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // openWorkspaceMenuItem
            // 
            openWorkspaceMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            openWorkspaceMenuItem.Name = "openWorkspaceMenuItem";
            openWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openWorkspaceMenuItem.Size = new Size(276, 22);
            openWorkspaceMenuItem.Text = "&Open Workspace…";
            // 
            // setupWorkspaceMenuItem
            // 
            setupWorkspaceMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            setupWorkspaceMenuItem.Name = "setupWorkspaceMenuItem";
            setupWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.N;
            setupWorkspaceMenuItem.Size = new Size(276, 22);
            setupWorkspaceMenuItem.Text = "&Setup New Workspace…";
            // 
            // fileSeparator1
            // 
            fileSeparator1.Name = "fileSeparator1";
            fileSeparator1.Size = new Size(273, 6);
            // 
            // saveWorkspaceMenuItem
            // 
            saveWorkspaceMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            saveWorkspaceMenuItem.Name = "saveWorkspaceMenuItem";
            saveWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveWorkspaceMenuItem.Size = new Size(276, 22);
            saveWorkspaceMenuItem.Text = "&Save Workspace";
            // 
            // closeWorkspaceMenuItem
            // 
            closeWorkspaceMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            closeWorkspaceMenuItem.Name = "closeWorkspaceMenuItem";
            closeWorkspaceMenuItem.Size = new Size(276, 22);
            closeWorkspaceMenuItem.Text = "&Close Workspace";
            // 
            // fileSeparator2
            // 
            fileSeparator2.Name = "fileSeparator2";
            fileSeparator2.Size = new Size(273, 6);
            // 
            // exitMenuItem
            // 
            exitMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitMenuItem.Size = new Size(276, 22);
            exitMenuItem.Text = "E&xit";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { editCopyMenuItem, editSelectAllMenuItem });
            editToolStripMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "&Edit";
            // 
            // editCopyMenuItem
            // 
            editCopyMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editCopyMenuItem.Name = "editCopyMenuItem";
            editCopyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            editCopyMenuItem.Size = new Size(164, 22);
            editCopyMenuItem.Text = "&Copy";
            editCopyMenuItem.Click += OnEditCopy;
            // 
            // editSelectAllMenuItem
            // 
            editSelectAllMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editSelectAllMenuItem.Name = "editSelectAllMenuItem";
            editSelectAllMenuItem.ShortcutKeys = Keys.Control | Keys.A;
            editSelectAllMenuItem.Size = new Size(164, 22);
            editSelectAllMenuItem.Text = "Select &All";
            editSelectAllMenuItem.Click += OnEditSelectAll;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { wordWrapMenuItem });
            optionsToolStripMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(61, 20);
            optionsToolStripMenuItem.Text = "&Options";
            // 
            // wordWrapMenuItem
            // 
            wordWrapMenuItem.CheckOnClick = true;
            wordWrapMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            wordWrapMenuItem.Name = "wordWrapMenuItem";
            wordWrapMenuItem.ShortcutKeys = Keys.Alt | Keys.Z;
            wordWrapMenuItem.Size = new Size(171, 22);
            wordWrapMenuItem.Text = "&Word Wrap";
            wordWrapMenuItem.ToolTipText = "Toggle word wrap in editor tabs to avoid horizontal scrollbars";
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showExplorerMenuItem, showChatMenuItem, showCommandMenuItem, viewSeparator1, refreshMenuItem });
            viewToolStripMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            // 
            // showExplorerMenuItem
            // 
            showExplorerMenuItem.Checked = true;
            showExplorerMenuItem.CheckOnClick = true;
            showExplorerMenuItem.CheckState = CheckState.Checked;
            showExplorerMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            showExplorerMenuItem.Name = "showExplorerMenuItem";
            showExplorerMenuItem.Size = new Size(189, 22);
            showExplorerMenuItem.Text = "File &Explorer\tCtrl+1";
            // 
            // showChatMenuItem
            // 
            showChatMenuItem.Checked = true;
            showChatMenuItem.CheckOnClick = true;
            showChatMenuItem.CheckState = CheckState.Checked;
            showChatMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            showChatMenuItem.Name = "showChatMenuItem";
            showChatMenuItem.Size = new Size(189, 22);
            showChatMenuItem.Text = "AI &Chat\tCtrl+2";
            // 
            // showCommandMenuItem
            // 
            showCommandMenuItem.Checked = true;
            showCommandMenuItem.CheckOnClick = true;
            showCommandMenuItem.CheckState = CheckState.Checked;
            showCommandMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            showCommandMenuItem.Name = "showCommandMenuItem";
            showCommandMenuItem.Size = new Size(189, 22);
            showCommandMenuItem.Text = "Co&mmand Line\tCtrl+3";
            // 
            // viewSeparator1
            // 
            viewSeparator1.Name = "viewSeparator1";
            viewSeparator1.Size = new Size(186, 6);
            // 
            // refreshMenuItem
            // 
            refreshMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            refreshMenuItem.Name = "refreshMenuItem";
            refreshMenuItem.ShortcutKeys = Keys.F5;
            refreshMenuItem.Size = new Size(189, 22);
            refreshMenuItem.Text = "&Refresh Explorer";
            // 
            // editorsToolStripMenuItem
            // 
            editorsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { editActorsMenuItem, editLoopsMenuItem, editWrappersMenuItem, editRunbooksMenuItem, editorsSeparator1, editConfigMenuItem, viewLogsMenuItem, viewChatHistoryMenuItem, viewPromptViewerMenuItem, viewWorkspaceViewerMenuItem, editorsSeparator2, closeAllEditorsMenuItem });
            editorsToolStripMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editorsToolStripMenuItem.Name = "editorsToolStripMenuItem";
            editorsToolStripMenuItem.Size = new Size(55, 20);
            editorsToolStripMenuItem.Text = "E&ditors";
            // 
            // editActorsMenuItem
            // 
            editActorsMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editActorsMenuItem.Name = "editActorsMenuItem";
            editActorsMenuItem.Size = new Size(197, 22);
            editActorsMenuItem.Text = "🎭  &Actors…";
            // 
            // editLoopsMenuItem
            // 
            editLoopsMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editLoopsMenuItem.Name = "editLoopsMenuItem";
            editLoopsMenuItem.Size = new Size(197, 22);
            editLoopsMenuItem.Text = "♻  &Loops…";
            // 
            // editWrappersMenuItem
            // 
            editWrappersMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editWrappersMenuItem.Name = "editWrappersMenuItem";
            editWrappersMenuItem.Size = new Size(197, 22);
            editWrappersMenuItem.Text = "⚙  &Wrappers…";
            // 
            // editRunbooksMenuItem
            // 
            editRunbooksMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editRunbooksMenuItem.Name = "editRunbooksMenuItem";
            editRunbooksMenuItem.Size = new Size(197, 22);
            editRunbooksMenuItem.Text = "📜  &Runbooks…";
            // 
            // editorsSeparator1
            // 
            editorsSeparator1.Name = "editorsSeparator1";
            editorsSeparator1.Size = new Size(194, 6);
            // 
            // editConfigMenuItem
            // 
            editConfigMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            editConfigMenuItem.Name = "editConfigMenuItem";
            editConfigMenuItem.Size = new Size(197, 22);
            editConfigMenuItem.Text = "⚙  &Configuration";
            // 
            // viewLogsMenuItem
            // 
            viewLogsMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            viewLogsMenuItem.Name = "viewLogsMenuItem";
            viewLogsMenuItem.Size = new Size(197, 22);
            viewLogsMenuItem.Text = "📋  Session &Logs";
            // 
            // viewChatHistoryMenuItem
            // 
            viewChatHistoryMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            viewChatHistoryMenuItem.Name = "viewChatHistoryMenuItem";
            viewChatHistoryMenuItem.Size = new Size(197, 22);
            viewChatHistoryMenuItem.Text = "💬  Chat &History";
            // 
            // viewPromptViewerMenuItem
            // 
            viewPromptViewerMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            viewPromptViewerMenuItem.Name = "viewPromptViewerMenuItem";
            viewPromptViewerMenuItem.Size = new Size(197, 22);
            viewPromptViewerMenuItem.Text = "🔍  &Prompt Viewer";
            // 
            // viewWorkspaceViewerMenuItem
            // 
            viewWorkspaceViewerMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            viewWorkspaceViewerMenuItem.Name = "viewWorkspaceViewerMenuItem";
            viewWorkspaceViewerMenuItem.Size = new Size(197, 22);
            viewWorkspaceViewerMenuItem.Text = "📊  &Workspace Viewer";
            // 
            // editorsSeparator2
            // 
            editorsSeparator2.Name = "editorsSeparator2";
            editorsSeparator2.Size = new Size(194, 6);
            // 
            // closeAllEditorsMenuItem
            // 
            closeAllEditorsMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            closeAllEditorsMenuItem.Name = "closeAllEditorsMenuItem";
            closeAllEditorsMenuItem.Size = new Size(197, 22);
            closeAllEditorsMenuItem.Text = "Close All &Editors\tCtrl+W";
            // 
            // workspaceToolStripMenuItem
            // 
            workspaceToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { reloadActorsMenuItem, listActorsMenuItem, workspaceSeparator1, workspaceInfoMenuItem, verifyWorkspaceMenuItem, repairWorkspaceMenuItem, workspaceSeparator2, cleanupWorkspaceMenuItem, openWorkspaceFolderMenuItem });
            workspaceToolStripMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            workspaceToolStripMenuItem.Name = "workspaceToolStripMenuItem";
            workspaceToolStripMenuItem.Size = new Size(77, 20);
            workspaceToolStripMenuItem.Text = "&Workspace";
            // 
            // reloadActorsMenuItem
            // 
            reloadActorsMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            reloadActorsMenuItem.Name = "reloadActorsMenuItem";
            reloadActorsMenuItem.ShortcutKeys = Keys.Control | Keys.R;
            reloadActorsMenuItem.Size = new Size(188, 22);
            reloadActorsMenuItem.Text = "&Reload Actors";
            // 
            // listActorsMenuItem
            // 
            listActorsMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            listActorsMenuItem.Name = "listActorsMenuItem";
            listActorsMenuItem.Size = new Size(188, 22);
            listActorsMenuItem.Text = "&List Actors";
            // 
            // workspaceSeparator1
            // 
            workspaceSeparator1.Name = "workspaceSeparator1";
            workspaceSeparator1.Size = new Size(185, 6);
            // 
            // workspaceInfoMenuItem
            // 
            workspaceInfoMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            workspaceInfoMenuItem.Name = "workspaceInfoMenuItem";
            workspaceInfoMenuItem.Size = new Size(188, 22);
            workspaceInfoMenuItem.Text = "Workspace &Info";
            // 
            // verifyWorkspaceMenuItem
            // 
            verifyWorkspaceMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            verifyWorkspaceMenuItem.Name = "verifyWorkspaceMenuItem";
            verifyWorkspaceMenuItem.Size = new Size(188, 22);
            verifyWorkspaceMenuItem.Text = "&Verify Structure";
            // 
            // repairWorkspaceMenuItem
            // 
            repairWorkspaceMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            repairWorkspaceMenuItem.Name = "repairWorkspaceMenuItem";
            repairWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.R;
            repairWorkspaceMenuItem.Size = new Size(188, 22);
            repairWorkspaceMenuItem.Text = "&Repair Workspace";
            repairWorkspaceMenuItem.ToolTipText = "Add any missing workspace folders, mailboxes, and actor components";
            // 
            // workspaceSeparator2
            // 
            workspaceSeparator2.Name = "workspaceSeparator2";
            workspaceSeparator2.Size = new Size(185, 6);
            // 
            // cleanupWorkspaceMenuItem
            // 
            cleanupWorkspaceMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            cleanupWorkspaceMenuItem.Name = "cleanupWorkspaceMenuItem";
            cleanupWorkspaceMenuItem.Size = new Size(188, 22);
            cleanupWorkspaceMenuItem.Text = "&Cleanup Workspace…";
            // 
            // openWorkspaceFolderMenuItem
            // 
            openWorkspaceFolderMenuItem.ForeColor = Color.FromArgb(228, 228, 233);
            openWorkspaceFolderMenuItem.Name = "openWorkspaceFolderMenuItem";
            openWorkspaceFolderMenuItem.Size = new Size(188, 22);
            openWorkspaceFolderMenuItem.Text = "Open in &Explorer";
            // 
            // toolbarPanel
            // 
            toolbarPanel.BackColor = Color.FromArgb(39, 39, 44);
            toolbarPanel.Controls.Add(editorsToolStrip);
            toolbarPanel.Controls.Add(workspaceToolStrip);
            toolbarPanel.Controls.Add(fileToolStrip);
            toolbarPanel.Dock = DockStyle.Top;
            toolbarPanel.Location = new Point(0, 24);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Orientation = Orientation.Horizontal;
            toolbarPanel.RowMargin = new Padding(3, 0, 0, 0);
            toolbarPanel.Size = new Size(1280, 25);
            // 
            // fileToolStrip
            // 
            fileToolStrip.BackColor = Color.FromArgb(39, 39, 44);
            fileToolStrip.Dock = DockStyle.None;
            fileToolStrip.ForeColor = Color.FromArgb(228, 228, 233);
            fileToolStrip.Items.AddRange(new ToolStripItem[] { tsbOpen, tsbSetup, tsbSave, tsFileSep1, tsbClose });
            fileToolStrip.Location = new Point(269, 0);
            fileToolStrip.Name = "fileToolStrip";
            fileToolStrip.Padding = new Padding(0, 0, 6, 0);
            fileToolStrip.Size = new Size(234, 25);
            fileToolStrip.TabIndex = 0;
            // 
            // tsbOpen
            // 
            tsbOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbOpen.Font = new Font("Segoe UI", 8.25F);
            tsbOpen.ForeColor = Color.FromArgb(228, 228, 233);
            tsbOpen.Name = "tsbOpen";
            tsbOpen.Size = new Size(55, 22);
            tsbOpen.Text = "📂 Open";
            tsbOpen.ToolTipText = "Open Workspace (Ctrl+O)";
            // 
            // tsbSetup
            // 
            tsbSetup.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSetup.Font = new Font("Segoe UI", 8.25F);
            tsbSetup.ForeColor = Color.FromArgb(228, 228, 233);
            tsbSetup.Name = "tsbSetup";
            tsbSetup.Size = new Size(56, 22);
            tsbSetup.Text = "✨ Setup";
            tsbSetup.ToolTipText = "Setup New Workspace (Ctrl+Shift+N)";
            // 
            // tsbSave
            // 
            tsbSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSave.Font = new Font("Segoe UI", 8.25F);
            tsbSave.ForeColor = Color.FromArgb(228, 228, 233);
            tsbSave.Name = "tsbSave";
            tsbSave.Size = new Size(49, 22);
            tsbSave.Text = "💾 Save";
            tsbSave.ToolTipText = "Save Workspace (Ctrl+S)";
            // 
            // tsFileSep1
            // 
            tsFileSep1.Name = "tsFileSep1";
            tsFileSep1.Size = new Size(6, 25);
            // 
            // tsbClose
            // 
            tsbClose.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbClose.Font = new Font("Segoe UI", 8.25F);
            tsbClose.ForeColor = Color.FromArgb(161, 161, 170);
            tsbClose.Name = "tsbClose";
            tsbClose.Size = new Size(51, 22);
            tsbClose.Text = "✕ Close";
            tsbClose.ToolTipText = "Close Workspace";
            // 
            // workspaceToolStrip
            // 
            workspaceToolStrip.BackColor = Color.FromArgb(39, 39, 44);
            workspaceToolStrip.Dock = DockStyle.None;
            workspaceToolStrip.ForeColor = Color.FromArgb(228, 228, 233);
            workspaceToolStrip.Items.AddRange(new ToolStripItem[] { tsbRefresh, tsbReloadActors, tsWsSep1, tsbInfo, tsbVerify, tsbRepair, tsWsSep2, tsbStop });
            workspaceToolStrip.Location = new Point(503, 0);
            workspaceToolStrip.Name = "workspaceToolStrip";
            workspaceToolStrip.Padding = new Padding(0, 0, 6, 0);
            workspaceToolStrip.Size = new Size(335, 25);
            workspaceToolStrip.TabIndex = 1;
            // 
            // tsbRefresh
            // 
            tsbRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbRefresh.Font = new Font("Segoe UI", 8.25F);
            tsbRefresh.ForeColor = Color.FromArgb(228, 228, 233);
            tsbRefresh.Name = "tsbRefresh";
            tsbRefresh.Size = new Size(62, 22);
            tsbRefresh.Text = "↻ Refresh";
            tsbRefresh.ToolTipText = "Refresh Explorer (F5)";
            // 
            // tsbReloadActors
            // 
            tsbReloadActors.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbReloadActors.Font = new Font("Segoe UI", 8.25F);
            tsbReloadActors.ForeColor = Color.FromArgb(228, 228, 233);
            tsbReloadActors.Name = "tsbReloadActors";
            tsbReloadActors.Size = new Size(96, 22);
            tsbReloadActors.Text = "♻ Reload Actors";
            tsbReloadActors.ToolTipText = "Reload Actors from Disk (Ctrl+R)";
            // 
            // tsWsSep1
            // 
            tsWsSep1.Name = "tsWsSep1";
            tsWsSep1.Size = new Size(6, 25);
            // 
            // tsbInfo
            // 
            tsbInfo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbInfo.Font = new Font("Segoe UI", 8.25F);
            tsbInfo.ForeColor = Color.FromArgb(228, 228, 233);
            tsbInfo.Name = "tsbInfo";
            tsbInfo.Size = new Size(47, 22);
            tsbInfo.Text = "ℹ Info";
            tsbInfo.ToolTipText = "Workspace Info";
            // 
            // tsbVerify
            // 
            tsbVerify.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbVerify.Font = new Font("Segoe UI", 8.25F);
            tsbVerify.ForeColor = Color.FromArgb(228, 228, 233);
            tsbVerify.Name = "tsbVerify";
            tsbVerify.Size = new Size(50, 22);
            tsbVerify.Text = "✓ Verify";
            tsbVerify.ToolTipText = "Verify Workspace Structure";
            // 
            // tsbRepair
            // 
            tsbRepair.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbRepair.Font = new Font("Segoe UI", 8.25F);
            tsbRepair.ForeColor = Color.FromArgb(228, 228, 233);
            tsbRepair.Name = "tsbRepair";
            tsbRepair.Size = new Size(56, 22);
            tsbRepair.Text = "🔧 Repair";
            tsbRepair.ToolTipText = "Repair Workspace — add any missing folders, mailboxes, and actor components (Ctrl+Shift+R)";
            // 
            // tsWsSep2
            // 
            tsWsSep2.Name = "tsWsSep2";
            tsWsSep2.Size = new Size(6, 25);
            // 
            // tsbStop
            // 
            tsbStop.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbStop.Enabled = false;
            tsbStop.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            tsbStop.ForeColor = Color.FromArgb(200, 150, 150);
            tsbStop.Name = "tsbStop";
            tsbStop.Size = new Size(51, 22);
            tsbStop.Text = "⏹ Stop";
            tsbStop.ToolTipText = "Stop the current running AI or terminal command (Esc)";
            // 
            // editorsToolStrip
            // 
            editorsToolStrip.BackColor = Color.FromArgb(39, 39, 44);
            editorsToolStrip.Dock = DockStyle.None;
            editorsToolStrip.ForeColor = Color.FromArgb(228, 228, 233);
            editorsToolStrip.Items.AddRange(new ToolStripItem[] { tsbEditActors, tsbConfig, tsbLogs, tsEdSep1, tsbClearChat });
            editorsToolStrip.Location = new Point(3, 0);
            editorsToolStrip.Name = "editorsToolStrip";
            editorsToolStrip.Padding = new Padding(0, 0, 6, 0);
            editorsToolStrip.Size = new Size(266, 25);
            editorsToolStrip.TabIndex = 2;
            // 
            // tsbEditActors
            // 
            tsbEditActors.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbEditActors.Font = new Font("Segoe UI", 8.25F);
            tsbEditActors.ForeColor = Color.FromArgb(228, 228, 233);
            tsbEditActors.Name = "tsbEditActors";
            tsbEditActors.Size = new Size(58, 22);
            tsbEditActors.Text = "🎭 Actors";
            tsbEditActors.ToolTipText = "Open Actor Editor";
            // 
            // tsbConfig
            // 
            tsbConfig.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbConfig.Font = new Font("Segoe UI", 8.25F);
            tsbConfig.ForeColor = Color.FromArgb(228, 228, 233);
            tsbConfig.Name = "tsbConfig";
            tsbConfig.Size = new Size(61, 22);
            tsbConfig.Text = "⚙ Config";
            tsbConfig.ToolTipText = "Open Workspace Configuration";
            // 
            // tsbLogs
            // 
            tsbLogs.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbLogs.Font = new Font("Segoe UI", 8.25F);
            tsbLogs.ForeColor = Color.FromArgb(161, 161, 170);
            tsbLogs.Name = "tsbLogs";
            tsbLogs.Size = new Size(48, 22);
            tsbLogs.Text = "📋 Logs";
            tsbLogs.ToolTipText = "Open Session Log Viewer";
            // 
            // tsEdSep1
            // 
            tsEdSep1.Name = "tsEdSep1";
            tsEdSep1.Size = new Size(6, 25);
            // 
            // tsbClearChat
            // 
            tsbClearChat.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbClearChat.Font = new Font("Segoe UI", 8.25F);
            tsbClearChat.ForeColor = Color.FromArgb(161, 161, 170);
            tsbClearChat.Name = "tsbClearChat";
            tsbClearChat.Size = new Size(76, 22);
            tsbClearChat.Text = "✕ Clear Chat";
            tsbClearChat.ToolTipText = "Clear Chat Conversation";
            // 
            // contentPanel
            // 
            contentPanel.BackColor = Color.FromArgb(24, 24, 27);
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(0, 49);
            contentPanel.Name = "contentPanel";
            contentPanel.Size = new Size(1280, 701);
            contentPanel.TabIndex = 0;
            // 
            // WallyForms
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(24, 24, 27);
            ClientSize = new Size(1280, 750);
            Controls.Add(contentPanel);
            Controls.Add(toolbarPanel);
            Controls.Add(menuPanel);
            ForeColor = Color.FromArgb(228, 228, 233);
            MainMenuStrip = menuStrip1;
            Name = "WallyForms";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Wally — AI Actor Environment";
            menuPanel.ResumeLayout(false);
            menuPanel.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            toolbarPanel.ResumeLayout(false);
            toolbarPanel.PerformLayout();
            fileToolStrip.ResumeLayout(false);
            fileToolStrip.PerformLayout();
            workspaceToolStrip.ResumeLayout(false);
            workspaceToolStrip.PerformLayout();
            editorsToolStrip.ResumeLayout(false);
            editorsToolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // ── Chrome containers ──
        private Panel           menuPanel;
        private ToolStripPanel  toolbarPanel;
        private Panel           contentPanel;
        private MenuStrip       menuStrip1;

        // ── File menu ──
        private ToolStripMenuItem  fileToolStripMenuItem;
        private ToolStripMenuItem  openWorkspaceMenuItem;
        private ToolStripMenuItem  setupWorkspaceMenuItem;
        private ToolStripSeparator fileSeparator1;
        private ToolStripMenuItem  saveWorkspaceMenuItem;
        private ToolStripMenuItem  closeWorkspaceMenuItem;
        private ToolStripSeparator fileSeparator2;
        private ToolStripMenuItem  exitMenuItem;

        // ── Edit menu ──
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem editCopyMenuItem;
        private ToolStripMenuItem editSelectAllMenuItem;

        // ── Options menu ──
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem wordWrapMenuItem;

        // ── View menu ──
        private ToolStripMenuItem  viewToolStripMenuItem;
        private ToolStripMenuItem  showExplorerMenuItem;
        private ToolStripMenuItem  showChatMenuItem;
        private ToolStripMenuItem  showCommandMenuItem;
        private ToolStripSeparator viewSeparator1;
        private ToolStripMenuItem  refreshMenuItem;

        // ── Editors menu ──
        private ToolStripMenuItem  editorsToolStripMenuItem;
        private ToolStripMenuItem  editActorsMenuItem;
        private ToolStripMenuItem  editLoopsMenuItem;
        private ToolStripMenuItem  editWrappersMenuItem;
        private ToolStripMenuItem  editRunbooksMenuItem;
        private ToolStripSeparator editorsSeparator1;
        private ToolStripMenuItem  editConfigMenuItem;
        private ToolStripMenuItem  viewLogsMenuItem;
        private ToolStripMenuItem  viewChatHistoryMenuItem;
        private ToolStripMenuItem  viewPromptViewerMenuItem;
        private ToolStripMenuItem  viewWorkspaceViewerMenuItem;
        private ToolStripSeparator editorsSeparator2;
        private ToolStripMenuItem  closeAllEditorsMenuItem;

        // ── Workspace menu ──
        private ToolStripMenuItem  workspaceToolStripMenuItem;
        private ToolStripMenuItem  reloadActorsMenuItem;
        private ToolStripMenuItem  listActorsMenuItem;
        private ToolStripSeparator workspaceSeparator1;
        private ToolStripMenuItem  workspaceInfoMenuItem;
        private ToolStripMenuItem  verifyWorkspaceMenuItem;
        private ToolStripMenuItem  repairWorkspaceMenuItem;
        private ToolStripSeparator workspaceSeparator2;
        private ToolStripMenuItem  cleanupWorkspaceMenuItem;
        private ToolStripMenuItem  openWorkspaceFolderMenuItem;

        // ── File ToolStrip ──
        private WallyToolStrip     fileToolStrip;
        private ToolStripButton    tsbOpen;
        private ToolStripButton    tsbSetup;
        private ToolStripButton    tsbSave;
        private ToolStripSeparator tsFileSep1;
        private ToolStripButton    tsbClose;

        // ── Workspace ToolStrip ──
        private WallyToolStrip     workspaceToolStrip;
        private ToolStripButton    tsbRefresh;
        private ToolStripButton    tsbReloadActors;
        private ToolStripSeparator tsWsSep1;
        private ToolStripButton    tsbInfo;
        private ToolStripButton    tsbVerify;
        private ToolStripButton    tsbRepair;
        private ToolStripSeparator tsWsSep2;
        private ToolStripButton    tsbStop;

        // ── Editors ToolStrip ──
        private WallyToolStrip     editorsToolStrip;
        private ToolStripButton    tsbEditActors;
        private ToolStripButton    tsbConfig;
        private ToolStripButton    tsbLogs;
        private ToolStripSeparator tsEdSep1;
        private ToolStripButton    tsbClearChat;
    }
}
