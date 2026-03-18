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
            // ── Chrome containers ──
            headerSplit  = new SplitContainer();
            toolbarFlow  = new FlowLayoutPanel();
            contentPanel = new Panel();

            menuStrip1 = new MenuStrip();

            // ── File menu ──
            fileToolStripMenuItem    = new ToolStripMenuItem();
            openWorkspaceMenuItem    = new ToolStripMenuItem();
            setupWorkspaceMenuItem   = new ToolStripMenuItem();
            fileSeparator1           = new ToolStripSeparator();
            saveWorkspaceMenuItem    = new ToolStripMenuItem();
            closeWorkspaceMenuItem   = new ToolStripMenuItem();
            fileSeparator2           = new ToolStripSeparator();
            exitMenuItem             = new ToolStripMenuItem();

            // ── Edit menu ──
            editToolStripMenuItem    = new ToolStripMenuItem();
            editCopyMenuItem         = new ToolStripMenuItem();
            editSelectAllMenuItem    = new ToolStripMenuItem();

            // ── Options menu ──
            optionsToolStripMenuItem = new ToolStripMenuItem();
            wordWrapMenuItem         = new ToolStripMenuItem();

            // ── View menu ──
            viewToolStripMenuItem    = new ToolStripMenuItem();
            showExplorerMenuItem     = new ToolStripMenuItem();
            showChatMenuItem         = new ToolStripMenuItem();
            showCommandMenuItem      = new ToolStripMenuItem();
            viewSeparator1           = new ToolStripSeparator();
            refreshMenuItem          = new ToolStripMenuItem();

            // ── Editors menu ──
            editorsToolStripMenuItem    = new ToolStripMenuItem();
            editActorsMenuItem          = new ToolStripMenuItem();
            editLoopsMenuItem           = new ToolStripMenuItem();
            editWrappersMenuItem        = new ToolStripMenuItem();
            editRunbooksMenuItem        = new ToolStripMenuItem();
            editorsSeparator1           = new ToolStripSeparator();
            editConfigMenuItem          = new ToolStripMenuItem();
            viewLogsMenuItem            = new ToolStripMenuItem();
            viewChatHistoryMenuItem     = new ToolStripMenuItem();
            viewPromptViewerMenuItem    = new ToolStripMenuItem();
            viewWorkspaceViewerMenuItem = new ToolStripMenuItem();
            editorsSeparator2           = new ToolStripSeparator();
            closeAllEditorsMenuItem     = new ToolStripMenuItem();

            // ── Workspace menu ──
            workspaceToolStripMenuItem  = new ToolStripMenuItem();
            reloadActorsMenuItem        = new ToolStripMenuItem();
            listActorsMenuItem          = new ToolStripMenuItem();
            workspaceSeparator1         = new ToolStripSeparator();
            workspaceInfoMenuItem       = new ToolStripMenuItem();
            verifyWorkspaceMenuItem     = new ToolStripMenuItem();
            workspaceSeparator2         = new ToolStripSeparator();
            cleanupWorkspaceMenuItem    = new ToolStripMenuItem();
            openWorkspaceFolderMenuItem = new ToolStripMenuItem();

            // ── File ToolStrip ──
            fileToolStrip    = new ToolStrip();
            tsbOpen          = new ToolStripButton();
            tsbSetup         = new ToolStripButton();
            tsbSave          = new ToolStripButton();
            tsFileSep1       = new ToolStripSeparator();
            tsbClose         = new ToolStripButton();

            // ── Workspace ToolStrip ──
            workspaceToolStrip = new ToolStrip();
            tsbRefresh         = new ToolStripButton();
            tsbReloadActors    = new ToolStripButton();
            tsWsSep1           = new ToolStripSeparator();
            tsbInfo            = new ToolStripButton();
            tsbVerify          = new ToolStripButton();
            tsWsSep2           = new ToolStripSeparator();
            tsbStop            = new ToolStripButton();

            // ── Editors ToolStrip ──
            editorsToolStrip = new ToolStrip();
            tsbEditActors    = new ToolStripButton();
            tsbConfig        = new ToolStripButton();
            tsbLogs          = new ToolStripButton();
            tsEdSep1         = new ToolStripSeparator();
            tsbClearChat     = new ToolStripButton();

            ((System.ComponentModel.ISupportInitialize)headerSplit).BeginInit();
            headerSplit.Panel1.SuspendLayout();
            headerSplit.Panel2.SuspendLayout();
            headerSplit.SuspendLayout();
            menuStrip1.SuspendLayout();
            toolbarFlow.SuspendLayout();
            fileToolStrip.SuspendLayout();
            workspaceToolStrip.SuspendLayout();
            editorsToolStrip.SuspendLayout();
            SuspendLayout();

            // Shared renderer for all menus and toolstrips.
            var renderer = WallyTheme.CreateRenderer();

            // ═══════════════════════════════════════════════════════════════
            //  headerSplit  — top-docked, fixed splitter dividing menu / toolbars
            // ═══════════════════════════════════════════════════════════════

            headerSplit.Dock             = DockStyle.Top;
            headerSplit.Orientation      = Orientation.Horizontal;
            headerSplit.IsSplitterFixed  = true;
            headerSplit.SplitterWidth    = 1;
            headerSplit.SplitterDistance = 24;          // height of menu row
            headerSplit.Panel1MinSize    = 24;
            headerSplit.Panel2MinSize    = 26;
            headerSplit.BackColor        = WallyTheme.Surface2;
            headerSplit.Name             = "headerSplit";
            headerSplit.TabStop          = false;
            // Panel1 — menu strip only
            headerSplit.Panel1.BackColor = WallyTheme.Surface2;
            headerSplit.Panel1.Controls.Add(menuStrip1);
            // Panel2 — toolbar flow only
            headerSplit.Panel2.BackColor = WallyTheme.Surface2;
            headerSplit.Panel2.Controls.Add(toolbarFlow);

            // ═══════════════════════════════════════════════════════════════
            //  menuStrip1  (lives exclusively in headerSplit.Panel1)
            // ═══════════════════════════════════════════════════════════════

            menuStrip1.Dock      = DockStyle.Fill;
            menuStrip1.Items.AddRange(new ToolStripItem[]
            {
                fileToolStripMenuItem, editToolStripMenuItem,
                optionsToolStripMenuItem,
                viewToolStripMenuItem, editorsToolStripMenuItem,
                workspaceToolStripMenuItem
            });
            menuStrip1.Name      = "menuStrip1";
            menuStrip1.TabIndex  = 0;
            menuStrip1.BackColor = WallyTheme.Surface2;
            menuStrip1.ForeColor = WallyTheme.TextPrimary;
            menuStrip1.Renderer  = renderer;

            // ═══════════════════════════════════════════════════════════════
            //  toolbarFlow  (lives exclusively in headerSplit.Panel2)
            // ═══════════════════════════════════════════════════════════════

            toolbarFlow.Dock          = DockStyle.Fill;
            toolbarFlow.FlowDirection = FlowDirection.TopDown;
            toolbarFlow.WrapContents  = false;
            toolbarFlow.AutoSize      = true;
            toolbarFlow.BackColor     = WallyTheme.Surface2;
            toolbarFlow.Name          = "toolbarFlow";
            toolbarFlow.Padding       = new Padding(0);
            toolbarFlow.Margin        = new Padding(0);
            toolbarFlow.Controls.Add(fileToolStrip);
            toolbarFlow.Controls.Add(workspaceToolStrip);
            toolbarFlow.Controls.Add(editorsToolStrip);

            // ═══════════════════════════════════════════════════════════════
            //  contentPanel  — fills the rest of the form (replaces ToolStripContainer)
            // ═══════════════════════════════════════════════════════════════

            contentPanel.Dock      = DockStyle.Fill;
            contentPanel.BackColor = WallyTheme.Surface0;
            contentPanel.Name      = "contentPanel";

            // ═══════════════════════════════════════════════════════════════
            //  File menu items
            // ═══════════════════════════════════════════════════════════════

            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                openWorkspaceMenuItem, setupWorkspaceMenuItem,
                fileSeparator1,
                saveWorkspaceMenuItem, closeWorkspaceMenuItem,
                fileSeparator2,
                exitMenuItem
            });
            fileToolStripMenuItem.Name     = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size     = new Size(37, 20);
            fileToolStripMenuItem.Text     = "&File";
            fileToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            openWorkspaceMenuItem.Name        = "openWorkspaceMenuItem";
            openWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openWorkspaceMenuItem.Size        = new Size(280, 22);
            openWorkspaceMenuItem.Text        = "&Open Workspace\u2026";
            openWorkspaceMenuItem.ForeColor   = WallyTheme.TextPrimary;

            setupWorkspaceMenuItem.Name        = "setupWorkspaceMenuItem";
            setupWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.N;
            setupWorkspaceMenuItem.Size        = new Size(280, 22);
            setupWorkspaceMenuItem.Text        = "&Setup New Workspace\u2026";
            setupWorkspaceMenuItem.ForeColor   = WallyTheme.TextPrimary;

            fileSeparator1.Name = "fileSeparator1";
            fileSeparator1.Size = new Size(277, 6);

            saveWorkspaceMenuItem.Name        = "saveWorkspaceMenuItem";
            saveWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveWorkspaceMenuItem.Size        = new Size(280, 22);
            saveWorkspaceMenuItem.Text        = "&Save Workspace";
            saveWorkspaceMenuItem.ForeColor   = WallyTheme.TextPrimary;

            closeWorkspaceMenuItem.Name     = "closeWorkspaceMenuItem";
            closeWorkspaceMenuItem.Size     = new Size(280, 22);
            closeWorkspaceMenuItem.Text     = "&Close Workspace";
            closeWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            fileSeparator2.Name = "fileSeparator2";
            fileSeparator2.Size = new Size(277, 6);

            exitMenuItem.Name        = "exitMenuItem";
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitMenuItem.Size        = new Size(280, 22);
            exitMenuItem.Text        = "E&xit";
            exitMenuItem.ForeColor   = WallyTheme.TextPrimary;

            // ═══════════════════════════════════════════════════════════════
            //  Edit menu items
            // ═══════════════════════════════════════════════════════════════

            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                editCopyMenuItem, editSelectAllMenuItem
            });
            editToolStripMenuItem.Name     = "editToolStripMenuItem";
            editToolStripMenuItem.Size     = new Size(39, 20);
            editToolStripMenuItem.Text     = "&Edit";
            editToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            editCopyMenuItem.Name        = "editCopyMenuItem";
            editCopyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            editCopyMenuItem.Size        = new Size(200, 22);
            editCopyMenuItem.Text        = "&Copy";
            editCopyMenuItem.ForeColor   = WallyTheme.TextPrimary;
            editCopyMenuItem.Click       += OnEditCopy;

            editSelectAllMenuItem.Name        = "editSelectAllMenuItem";
            editSelectAllMenuItem.ShortcutKeys = Keys.Control | Keys.A;
            editSelectAllMenuItem.Size        = new Size(200, 22);
            editSelectAllMenuItem.Text        = "Select &All";
            editSelectAllMenuItem.ForeColor   = WallyTheme.TextPrimary;
            editSelectAllMenuItem.Click       += OnEditSelectAll;

            // ═══════════════════════════════════════════════════════════════
            //  Options menu items
            // ═══════════════════════════════════════════════════════════════

            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { wordWrapMenuItem });
            optionsToolStripMenuItem.Name     = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size     = new Size(61, 20);
            optionsToolStripMenuItem.Text     = "&Options";
            optionsToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            wordWrapMenuItem.Name        = "wordWrapMenuItem";
            wordWrapMenuItem.Size        = new Size(200, 22);
            wordWrapMenuItem.Text        = "&Word Wrap";
            wordWrapMenuItem.Checked     = false;
            wordWrapMenuItem.CheckOnClick = true;
            wordWrapMenuItem.ForeColor   = WallyTheme.TextPrimary;
            wordWrapMenuItem.ShortcutKeys = Keys.Alt | Keys.Z;
            wordWrapMenuItem.ToolTipText = "Toggle word wrap in editor tabs to avoid horizontal scrollbars";

            // ═══════════════════════════════════════════════════════════════
            //  View menu items
            // ═══════════════════════════════════════════════════════════════

            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                showExplorerMenuItem, showChatMenuItem, showCommandMenuItem,
                viewSeparator1, refreshMenuItem
            });
            viewToolStripMenuItem.Name     = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size     = new Size(44, 20);
            viewToolStripMenuItem.Text     = "&View";
            viewToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            showExplorerMenuItem.Name        = "showExplorerMenuItem";
            showExplorerMenuItem.Size        = new Size(240, 22);
            showExplorerMenuItem.Text        = "File &Explorer\tCtrl+1";
            showExplorerMenuItem.Checked     = true;
            showExplorerMenuItem.CheckOnClick = true;
            showExplorerMenuItem.ForeColor   = WallyTheme.TextPrimary;

            showChatMenuItem.Name        = "showChatMenuItem";
            showChatMenuItem.Size        = new Size(240, 22);
            showChatMenuItem.Text        = "AI &Chat\tCtrl+2";
            showChatMenuItem.Checked     = true;
            showChatMenuItem.CheckOnClick = true;
            showChatMenuItem.ForeColor   = WallyTheme.TextPrimary;

            showCommandMenuItem.Name        = "showCommandMenuItem";
            showCommandMenuItem.Size        = new Size(240, 22);
            showCommandMenuItem.Text        = "Co&mmand Line\tCtrl+3";
            showCommandMenuItem.Checked     = true;
            showCommandMenuItem.CheckOnClick = true;
            showCommandMenuItem.ForeColor   = WallyTheme.TextPrimary;

            viewSeparator1.Name = "viewSeparator1";
            viewSeparator1.Size = new Size(237, 6);

            refreshMenuItem.Name        = "refreshMenuItem";
            refreshMenuItem.ShortcutKeys = Keys.F5;
            refreshMenuItem.Size        = new Size(240, 22);
            refreshMenuItem.Text        = "&Refresh Explorer";
            refreshMenuItem.ForeColor   = WallyTheme.TextPrimary;

            // ═══════════════════════════════════════════════════════════════
            //  Editors menu items
            // ═══════════════════════════════════════════════════════════════

            editorsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                editActorsMenuItem, editLoopsMenuItem,
                editWrappersMenuItem, editRunbooksMenuItem,
                editorsSeparator1,
                editConfigMenuItem, viewLogsMenuItem,
                viewChatHistoryMenuItem,
                viewPromptViewerMenuItem, viewWorkspaceViewerMenuItem,
                editorsSeparator2,
                closeAllEditorsMenuItem
            });
            editorsToolStripMenuItem.Name     = "editorsToolStripMenuItem";
            editorsToolStripMenuItem.Size     = new Size(58, 20);
            editorsToolStripMenuItem.Text     = "E&ditors";
            editorsToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            editActorsMenuItem.Name     = "editActorsMenuItem";
            editActorsMenuItem.Size     = new Size(260, 22);
            editActorsMenuItem.Text     = "\U0001F3AD  &Actors\u2026";
            editActorsMenuItem.ForeColor = WallyTheme.TextPrimary;

            editLoopsMenuItem.Name     = "editLoopsMenuItem";
            editLoopsMenuItem.Size     = new Size(260, 22);
            editLoopsMenuItem.Text     = "\u267B  &Loops\u2026";
            editLoopsMenuItem.ForeColor = WallyTheme.TextPrimary;

            editWrappersMenuItem.Name     = "editWrappersMenuItem";
            editWrappersMenuItem.Size     = new Size(260, 22);
            editWrappersMenuItem.Text     = "\u2699  &Wrappers\u2026";
            editWrappersMenuItem.ForeColor = WallyTheme.TextPrimary;

            editRunbooksMenuItem.Name     = "editRunbooksMenuItem";
            editRunbooksMenuItem.Size     = new Size(260, 22);
            editRunbooksMenuItem.Text     = "\uD83D\uDCDC  &Runbooks\u2026";
            editRunbooksMenuItem.ForeColor = WallyTheme.TextPrimary;

            editorsSeparator1.Name = "editorsSeparator1";
            editorsSeparator1.Size = new Size(257, 6);

            editConfigMenuItem.Name     = "editConfigMenuItem";
            editConfigMenuItem.Size     = new Size(260, 22);
            editConfigMenuItem.Text     = "\u2699  &Configuration";
            editConfigMenuItem.ForeColor = WallyTheme.TextPrimary;

            viewLogsMenuItem.Name     = "viewLogsMenuItem";
            viewLogsMenuItem.Size     = new Size(260, 22);
            viewLogsMenuItem.Text     = "\uD83D\uDCCB  Session &Logs";
            viewLogsMenuItem.ForeColor = WallyTheme.TextPrimary;

            viewChatHistoryMenuItem.Name     = "viewChatHistoryMenuItem";
            viewChatHistoryMenuItem.Size     = new Size(260, 22);
            viewChatHistoryMenuItem.Text     = "\uD83D\uDCAC  Chat &History";
            viewChatHistoryMenuItem.ForeColor = WallyTheme.TextPrimary;

            viewPromptViewerMenuItem.Name     = "viewPromptViewerMenuItem";
            viewPromptViewerMenuItem.Size     = new Size(260, 22);
            viewPromptViewerMenuItem.Text     = "\uD83D\uDD0D  &Prompt Viewer";
            viewPromptViewerMenuItem.ForeColor = WallyTheme.TextPrimary;

            viewWorkspaceViewerMenuItem.Name     = "viewWorkspaceViewerMenuItem";
            viewWorkspaceViewerMenuItem.Size     = new Size(260, 22);
            viewWorkspaceViewerMenuItem.Text     = "\uD83D\uDCCA  &Workspace Viewer";
            viewWorkspaceViewerMenuItem.ForeColor = WallyTheme.TextPrimary;

            editorsSeparator2.Name = "editorsSeparator2";
            editorsSeparator2.Size = new Size(257, 6);

            closeAllEditorsMenuItem.Name     = "closeAllEditorsMenuItem";
            closeAllEditorsMenuItem.Size     = new Size(260, 22);
            closeAllEditorsMenuItem.Text     = "Close All &Editors\tCtrl+W";
            closeAllEditorsMenuItem.ForeColor = WallyTheme.TextPrimary;

            // ═══════════════════════════════════════════════════════════════
            //  Workspace menu items
            // ═══════════════════════════════════════════════════════════════

            workspaceToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                reloadActorsMenuItem, listActorsMenuItem,
                workspaceSeparator1,
                workspaceInfoMenuItem, verifyWorkspaceMenuItem,
                workspaceSeparator2,
                cleanupWorkspaceMenuItem,
                openWorkspaceFolderMenuItem
            });
            workspaceToolStripMenuItem.Name     = "workspaceToolStripMenuItem";
            workspaceToolStripMenuItem.Size     = new Size(77, 20);
            workspaceToolStripMenuItem.Text     = "&Workspace";
            workspaceToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            reloadActorsMenuItem.Name        = "reloadActorsMenuItem";
            reloadActorsMenuItem.ShortcutKeys = Keys.Control | Keys.R;
            reloadActorsMenuItem.Size        = new Size(260, 22);
            reloadActorsMenuItem.Text        = "&Reload Actors";
            reloadActorsMenuItem.ForeColor   = WallyTheme.TextPrimary;

            listActorsMenuItem.Name     = "listActorsMenuItem";
            listActorsMenuItem.Size     = new Size(260, 22);
            listActorsMenuItem.Text     = "&List Actors";
            listActorsMenuItem.ForeColor = WallyTheme.TextPrimary;

            workspaceSeparator1.Name = "workspaceSeparator1";
            workspaceSeparator1.Size = new Size(257, 6);

            workspaceInfoMenuItem.Name     = "workspaceInfoMenuItem";
            workspaceInfoMenuItem.Size     = new Size(260, 22);
            workspaceInfoMenuItem.Text     = "Workspace &Info";
            workspaceInfoMenuItem.ForeColor = WallyTheme.TextPrimary;

            verifyWorkspaceMenuItem.Name     = "verifyWorkspaceMenuItem";
            verifyWorkspaceMenuItem.Size     = new Size(260, 22);
            verifyWorkspaceMenuItem.Text     = "&Verify Structure";
            verifyWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            workspaceSeparator2.Name = "workspaceSeparator2";
            workspaceSeparator2.Size = new Size(257, 6);

            openWorkspaceFolderMenuItem.Name     = "openWorkspaceFolderMenuItem";
            openWorkspaceFolderMenuItem.Size     = new Size(260, 22);
            openWorkspaceFolderMenuItem.Text     = "Open in &Explorer";
            openWorkspaceFolderMenuItem.ForeColor = WallyTheme.TextPrimary;

            cleanupWorkspaceMenuItem.Name     = "cleanupWorkspaceMenuItem";
            cleanupWorkspaceMenuItem.Size     = new Size(260, 22);
            cleanupWorkspaceMenuItem.Text     = "&Cleanup Workspace\u2026";
            cleanupWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            // ═══════════════════════════════════════════════════════════════
            //  File ToolStrip  — Open · Setup · Save | Close
            // ═══════════════════════════════════════════════════════════════

            fileToolStrip.Dock      = DockStyle.None;
            fileToolStrip.Stretch   = false;
            fileToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            fileToolStrip.Name      = "fileToolStrip";
            fileToolStrip.TabIndex  = 1;
            fileToolStrip.Renderer  = renderer;
            fileToolStrip.BackColor = WallyTheme.Surface2;
            fileToolStrip.ForeColor = WallyTheme.TextPrimary;
            fileToolStrip.Padding   = new Padding(6, 0, 6, 0);
            fileToolStrip.Margin    = new Padding(0);
            fileToolStrip.Items.AddRange(new ToolStripItem[]
            {
                tsbOpen, tsbSetup, tsbSave, tsFileSep1, tsbClose
            });

            tsbOpen.Name         = "tsbOpen";
            tsbOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbOpen.Text         = "\uD83D\uDCC2 Open";
            tsbOpen.ToolTipText  = "Open Workspace (Ctrl+O)";
            tsbOpen.ForeColor    = WallyTheme.TextPrimary;
            tsbOpen.Font         = WallyTheme.FontUISmall;

            tsbSetup.Name         = "tsbSetup";
            tsbSetup.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSetup.Text         = "\u2728 Setup";
            tsbSetup.ToolTipText  = "Setup New Workspace (Ctrl+Shift+N)";
            tsbSetup.ForeColor    = WallyTheme.TextPrimary;
            tsbSetup.Font         = WallyTheme.FontUISmall;

            tsbSave.Name         = "tsbSave";
            tsbSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSave.Text         = "\uD83D\uDCBE Save";
            tsbSave.ToolTipText  = "Save Workspace (Ctrl+S)";
            tsbSave.ForeColor    = WallyTheme.TextPrimary;
            tsbSave.Font         = WallyTheme.FontUISmall;

            tsFileSep1.Name = "tsFileSep1";

            tsbClose.Name         = "tsbClose";
            tsbClose.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbClose.Text         = "\u2715 Close";
            tsbClose.ToolTipText  = "Close Workspace";
            tsbClose.ForeColor    = WallyTheme.TextSecondary;
            tsbClose.Font         = WallyTheme.FontUISmall;

            // ═══════════════════════════════════════════════════════════════
            //  Workspace ToolStrip  — Refresh · Reload Actors | Info · Verify | Stop
            // ═══════════════════════════════════════════════════════════════

            workspaceToolStrip.Dock      = DockStyle.None;
            workspaceToolStrip.Stretch   = false;
            workspaceToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            workspaceToolStrip.Name      = "workspaceToolStrip";
            workspaceToolStrip.TabIndex  = 2;
            workspaceToolStrip.Renderer  = renderer;
            workspaceToolStrip.BackColor = WallyTheme.Surface2;
            workspaceToolStrip.ForeColor = WallyTheme.TextPrimary;
            workspaceToolStrip.Padding   = new Padding(6, 0, 6, 0);
            workspaceToolStrip.Margin    = new Padding(0);
            workspaceToolStrip.Items.AddRange(new ToolStripItem[]
            {
                tsbRefresh, tsbReloadActors, tsWsSep1, tsbInfo, tsbVerify, tsWsSep2, tsbStop
            });

            tsbRefresh.Name         = "tsbRefresh";
            tsbRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbRefresh.Text         = "\u21BB Refresh";
            tsbRefresh.ToolTipText  = "Refresh Explorer (F5)";
            tsbRefresh.ForeColor    = WallyTheme.TextPrimary;
            tsbRefresh.Font         = WallyTheme.FontUISmall;

            tsbReloadActors.Name         = "tsbReloadActors";
            tsbReloadActors.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbReloadActors.Text         = "\u267B Reload Actors";
            tsbReloadActors.ToolTipText  = "Reload Actors from Disk (Ctrl+R)";
            tsbReloadActors.ForeColor    = WallyTheme.TextPrimary;
            tsbReloadActors.Font         = WallyTheme.FontUISmall;

            tsWsSep1.Name = "tsWsSep1";

            tsbInfo.Name         = "tsbInfo";
            tsbInfo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbInfo.Text         = "\u2139 Info";
            tsbInfo.ToolTipText  = "Workspace Info";
            tsbInfo.ForeColor    = WallyTheme.TextPrimary;
            tsbInfo.Font         = WallyTheme.FontUISmall;

            tsbVerify.Name         = "tsbVerify";
            tsbVerify.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbVerify.Text         = "\u2713 Verify";
            tsbVerify.ToolTipText  = "Verify Workspace Structure";
            tsbVerify.ForeColor    = WallyTheme.TextPrimary;
            tsbVerify.Font         = WallyTheme.FontUISmall;

            tsWsSep2.Name = "tsWsSep2";

            tsbStop.Name         = "tsbStop";
            tsbStop.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbStop.Text         = "\u23F9 Stop";
            tsbStop.ToolTipText  = "Stop the current running AI or terminal command (Esc)";
            tsbStop.ForeColor    = WallyTheme.Red;
            tsbStop.Font         = WallyTheme.FontUISmallBold;
            tsbStop.Enabled      = false;

            // ═══════════════════════════════════════════════════════════════
            //  Editors ToolStrip  — Actors · Config · Logs | Clear Chat
            // ═══════════════════════════════════════════════════════════════

            editorsToolStrip.Dock      = DockStyle.None;
            editorsToolStrip.Stretch   = false;
            editorsToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            editorsToolStrip.Name      = "editorsToolStrip";
            editorsToolStrip.TabIndex  = 3;
            editorsToolStrip.Renderer  = renderer;
            editorsToolStrip.BackColor = WallyTheme.Surface2;
            editorsToolStrip.ForeColor = WallyTheme.TextPrimary;
            editorsToolStrip.Padding   = new Padding(6, 0, 6, 0);
            editorsToolStrip.Margin    = new Padding(0);
            editorsToolStrip.Items.AddRange(new ToolStripItem[]
            {
                tsbEditActors, tsbConfig, tsbLogs, tsEdSep1, tsbClearChat
            });

            tsbEditActors.Name         = "tsbEditActors";
            tsbEditActors.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbEditActors.Text         = "\U0001F3AD Actors";
            tsbEditActors.ToolTipText  = "Open Actor Editor";
            tsbEditActors.ForeColor    = WallyTheme.TextPrimary;
            tsbEditActors.Font         = WallyTheme.FontUISmall;

            tsbConfig.Name         = "tsbConfig";
            tsbConfig.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbConfig.Text         = "\u2699 Config";
            tsbConfig.ToolTipText  = "Open Workspace Configuration";
            tsbConfig.ForeColor    = WallyTheme.TextPrimary;
            tsbConfig.Font         = WallyTheme.FontUISmall;

            tsbLogs.Name         = "tsbLogs";
            tsbLogs.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbLogs.Text         = "\uD83D\uDCCB Logs";
            tsbLogs.ToolTipText  = "Open Session Log Viewer";
            tsbLogs.ForeColor    = WallyTheme.TextSecondary;
            tsbLogs.Font         = WallyTheme.FontUISmall;

            tsEdSep1.Name = "tsEdSep1";

            tsbClearChat.Name         = "tsbClearChat";
            tsbClearChat.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbClearChat.Text         = "\u2715 Clear Chat";
            tsbClearChat.ToolTipText  = "Clear Chat Conversation";
            tsbClearChat.ForeColor    = WallyTheme.TextSecondary;
            tsbClearChat.Font         = WallyTheme.FontUISmall;

            // ═══════════════════════════════════════════════════════════════
            //  WallyForms
            // ═══════════════════════════════════════════════════════════════

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size(1280, 750);
            // Order matters: Fill first so it sits beneath the Top-docked header.
            Controls.Add(contentPanel);
            Controls.Add(headerSplit);
            MainMenuStrip       = menuStrip1;
            Name                = "WallyForms";
            Text                = "Wally \u2014 AI Actor Environment";
            StartPosition       = FormStartPosition.CenterScreen;
            BackColor           = WallyTheme.Surface0;
            ForeColor           = WallyTheme.TextPrimary;

            editorsToolStrip.ResumeLayout(false);
            editorsToolStrip.PerformLayout();
            workspaceToolStrip.ResumeLayout(false);
            workspaceToolStrip.PerformLayout();
            fileToolStrip.ResumeLayout(false);
            fileToolStrip.PerformLayout();
            toolbarFlow.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            headerSplit.Panel1.ResumeLayout(false);
            headerSplit.Panel1.PerformLayout();
            headerSplit.Panel2.ResumeLayout(false);
            headerSplit.Panel2.PerformLayout();
            headerSplit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)headerSplit).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // ── Chrome containers ──
        private SplitContainer  headerSplit;
        private FlowLayoutPanel toolbarFlow;
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
        private ToolStripSeparator workspaceSeparator2;
        private ToolStripMenuItem  cleanupWorkspaceMenuItem;
        private ToolStripMenuItem  openWorkspaceFolderMenuItem;

        // ── File ToolStrip ──
        private ToolStrip          fileToolStrip;
        private ToolStripButton    tsbOpen;
        private ToolStripButton    tsbSetup;
        private ToolStripButton    tsbSave;
        private ToolStripSeparator tsFileSep1;
        private ToolStripButton    tsbClose;

        // ── Workspace ToolStrip ──
        private ToolStrip          workspaceToolStrip;
        private ToolStripButton    tsbRefresh;
        private ToolStripButton    tsbReloadActors;
        private ToolStripSeparator tsWsSep1;
        private ToolStripButton    tsbInfo;
        private ToolStripButton    tsbVerify;
        private ToolStripSeparator tsWsSep2;
        private ToolStripButton    tsbStop;

        // ── Editors ToolStrip ──
        private ToolStrip          editorsToolStrip;
        private ToolStripButton    tsbEditActors;
        private ToolStripButton    tsbConfig;
        private ToolStripButton    tsbLogs;
        private ToolStripSeparator tsEdSep1;
        private ToolStripButton    tsbClearChat;
    }
}
