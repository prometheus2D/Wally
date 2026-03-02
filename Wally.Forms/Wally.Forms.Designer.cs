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
            toolStripContainer1 = new ToolStripContainer();
            menuStrip1 = new MenuStrip();

            // ── File menu ──
            fileToolStripMenuItem = new ToolStripMenuItem();
            openWorkspaceMenuItem = new ToolStripMenuItem();
            setupWorkspaceMenuItem = new ToolStripMenuItem();
            fileSeparator1 = new ToolStripSeparator();
            saveWorkspaceMenuItem = new ToolStripMenuItem();
            closeWorkspaceMenuItem = new ToolStripMenuItem();
            fileSeparator2 = new ToolStripSeparator();
            exitMenuItem = new ToolStripMenuItem();

            // ── Edit menu ──
            editToolStripMenuItem = new ToolStripMenuItem();
            editCopyMenuItem = new ToolStripMenuItem();
            editSelectAllMenuItem = new ToolStripMenuItem();

            // ── View menu ──
            viewToolStripMenuItem = new ToolStripMenuItem();
            showExplorerMenuItem = new ToolStripMenuItem();
            showChatMenuItem = new ToolStripMenuItem();
            showCommandMenuItem = new ToolStripMenuItem();
            viewSeparator1 = new ToolStripSeparator();
            refreshMenuItem = new ToolStripMenuItem();

            // ── Workspace menu ──
            workspaceToolStripMenuItem = new ToolStripMenuItem();
            reloadActorsMenuItem = new ToolStripMenuItem();
            listActorsMenuItem = new ToolStripMenuItem();
            workspaceSeparator1 = new ToolStripSeparator();
            workspaceInfoMenuItem = new ToolStripMenuItem();
            verifyWorkspaceMenuItem = new ToolStripMenuItem();
            workspaceSeparator2 = new ToolStripSeparator();
            openWorkspaceFolderMenuItem = new ToolStripMenuItem();

            // ── Main ToolStrip ──
            mainToolStrip = new ToolStrip();
            tsbOpen = new ToolStripButton();
            tsbSetup = new ToolStripButton();
            tsbSave = new ToolStripButton();
            tsSeparator1 = new ToolStripSeparator();
            tsbRefresh = new ToolStripButton();
            tsbReloadActors = new ToolStripButton();
            tsSeparator2 = new ToolStripSeparator();
            tsbInfo = new ToolStripButton();
            tsbClearChat = new ToolStripButton();

            toolStripContainer1.TopToolStripPanel.SuspendLayout();
            toolStripContainer1.SuspendLayout();
            menuStrip1.SuspendLayout();
            mainToolStrip.SuspendLayout();
            SuspendLayout();

            // Shared renderer for all menus, toolstrips, and context menus.
            var renderer = WallyTheme.CreateRenderer();

            // ═══════════════════════════════════════════════════════════════
            //  toolStripContainer1
            // ═══════════════════════════════════════════════════════════════

            toolStripContainer1.ContentPanel.Size = new Size(1280, 670);
            toolStripContainer1.Dock = DockStyle.Fill;
            toolStripContainer1.Location = new Point(0, 0);
            toolStripContainer1.Name = "toolStripContainer1";
            toolStripContainer1.Size = new Size(1280, 750);
            toolStripContainer1.TabIndex = 0;
            toolStripContainer1.TopToolStripPanel.Controls.Add(menuStrip1);
            toolStripContainer1.TopToolStripPanel.Controls.Add(mainToolStrip);
            toolStripContainer1.BottomToolStripPanelVisible = false;
            toolStripContainer1.LeftToolStripPanelVisible = false;
            toolStripContainer1.RightToolStripPanelVisible = false;

            // ═══════════════════════════════════════════════════════════════
            //  menuStrip1
            // ═══════════════════════════════════════════════════════════════

            menuStrip1.Dock = DockStyle.None;
            menuStrip1.Items.AddRange(new ToolStripItem[]
            {
                fileToolStripMenuItem, editToolStripMenuItem,
                viewToolStripMenuItem, workspaceToolStripMenuItem
            });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1280, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.BackColor = WallyTheme.Surface2;
            menuStrip1.ForeColor = WallyTheme.TextPrimary;
            menuStrip1.Renderer = renderer;

            // ═══════════════════════════════════════════════════════════════
            //  File menu
            // ═══════════════════════════════════════════════════════════════

            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                openWorkspaceMenuItem, setupWorkspaceMenuItem,
                fileSeparator1,
                saveWorkspaceMenuItem, closeWorkspaceMenuItem,
                fileSeparator2,
                exitMenuItem
            });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            fileToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            // Open Workspace…
            openWorkspaceMenuItem.Name = "openWorkspaceMenuItem";
            openWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openWorkspaceMenuItem.Size = new Size(280, 22);
            openWorkspaceMenuItem.Text = "&Open Workspace\u2026";
            openWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            // Setup Workspace…
            setupWorkspaceMenuItem.Name = "setupWorkspaceMenuItem";
            setupWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.N;
            setupWorkspaceMenuItem.Size = new Size(280, 22);
            setupWorkspaceMenuItem.Text = "&Setup New Workspace\u2026";
            setupWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            // fileSeparator1
            fileSeparator1.Name = "fileSeparator1";
            fileSeparator1.Size = new Size(277, 6);

            // Save Workspace
            saveWorkspaceMenuItem.Name = "saveWorkspaceMenuItem";
            saveWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveWorkspaceMenuItem.Size = new Size(280, 22);
            saveWorkspaceMenuItem.Text = "&Save Workspace";
            saveWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            // Close Workspace
            closeWorkspaceMenuItem.Name = "closeWorkspaceMenuItem";
            closeWorkspaceMenuItem.Size = new Size(280, 22);
            closeWorkspaceMenuItem.Text = "&Close Workspace";
            closeWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            // fileSeparator2
            fileSeparator2.Name = "fileSeparator2";
            fileSeparator2.Size = new Size(277, 6);

            // Exit
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitMenuItem.Size = new Size(280, 22);
            exitMenuItem.Text = "E&xit";
            exitMenuItem.ForeColor = WallyTheme.TextPrimary;

            // ═══════════════════════════════════════════════════════════════
            //  Edit menu
            // ═══════════════════════════════════════════════════════════════

            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                editCopyMenuItem, editSelectAllMenuItem
            });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "&Edit";
            editToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            editCopyMenuItem.Name = "editCopyMenuItem";
            editCopyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            editCopyMenuItem.Size = new Size(200, 22);
            editCopyMenuItem.Text = "&Copy";
            editCopyMenuItem.ForeColor = WallyTheme.TextPrimary;
            editCopyMenuItem.Click += (s, e) => { if (ActiveControl is RichTextBox rtb) rtb.Copy(); };

            editSelectAllMenuItem.Name = "editSelectAllMenuItem";
            editSelectAllMenuItem.ShortcutKeys = Keys.Control | Keys.A;
            editSelectAllMenuItem.Size = new Size(200, 22);
            editSelectAllMenuItem.Text = "Select &All";
            editSelectAllMenuItem.ForeColor = WallyTheme.TextPrimary;
            editSelectAllMenuItem.Click += (s, e) => { if (ActiveControl is RichTextBox rtb) rtb.SelectAll(); };

            // ═══════════════════════════════════════════════════════════════
            //  View menu
            // ═══════════════════════════════════════════════════════════════

            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                showExplorerMenuItem, showChatMenuItem, showCommandMenuItem,
                viewSeparator1, refreshMenuItem
            });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            viewToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            showExplorerMenuItem.Name = "showExplorerMenuItem";
            showExplorerMenuItem.Size = new Size(240, 22);
            showExplorerMenuItem.Text = "File &Explorer\tCtrl+1";
            showExplorerMenuItem.Checked = true;
            showExplorerMenuItem.CheckOnClick = true;
            showExplorerMenuItem.ForeColor = WallyTheme.TextPrimary;

            showChatMenuItem.Name = "showChatMenuItem";
            showChatMenuItem.Size = new Size(240, 22);
            showChatMenuItem.Text = "AI &Chat\tCtrl+2";
            showChatMenuItem.Checked = true;
            showChatMenuItem.CheckOnClick = true;
            showChatMenuItem.ForeColor = WallyTheme.TextPrimary;

            showCommandMenuItem.Name = "showCommandMenuItem";
            showCommandMenuItem.Size = new Size(240, 22);
            showCommandMenuItem.Text = "Co&mmand Line\tCtrl+3";
            showCommandMenuItem.Checked = true;
            showCommandMenuItem.CheckOnClick = true;
            showCommandMenuItem.ForeColor = WallyTheme.TextPrimary;

            viewSeparator1.Name = "viewSeparator1";
            viewSeparator1.Size = new Size(237, 6);

            refreshMenuItem.Name = "refreshMenuItem";
            refreshMenuItem.ShortcutKeys = Keys.F5;
            refreshMenuItem.Size = new Size(240, 22);
            refreshMenuItem.Text = "&Refresh Explorer";
            refreshMenuItem.ForeColor = WallyTheme.TextPrimary;

            // ═══════════════════════════════════════════════════════════════
            //  Workspace menu
            // ═══════════════════════════════════════════════════════════════

            workspaceToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                reloadActorsMenuItem, listActorsMenuItem,
                workspaceSeparator1,
                workspaceInfoMenuItem, verifyWorkspaceMenuItem,
                workspaceSeparator2,
                openWorkspaceFolderMenuItem
            });
            workspaceToolStripMenuItem.Name = "workspaceToolStripMenuItem";
            workspaceToolStripMenuItem.Size = new Size(77, 20);
            workspaceToolStripMenuItem.Text = "&Workspace";
            workspaceToolStripMenuItem.ForeColor = WallyTheme.TextPrimary;

            reloadActorsMenuItem.Name = "reloadActorsMenuItem";
            reloadActorsMenuItem.ShortcutKeys = Keys.Control | Keys.R;
            reloadActorsMenuItem.Size = new Size(260, 22);
            reloadActorsMenuItem.Text = "&Reload Actors";
            reloadActorsMenuItem.ForeColor = WallyTheme.TextPrimary;

            listActorsMenuItem.Name = "listActorsMenuItem";
            listActorsMenuItem.Size = new Size(260, 22);
            listActorsMenuItem.Text = "&List Actors";
            listActorsMenuItem.ForeColor = WallyTheme.TextPrimary;

            workspaceSeparator1.Name = "workspaceSeparator1";
            workspaceSeparator1.Size = new Size(257, 6);

            workspaceInfoMenuItem.Name = "workspaceInfoMenuItem";
            workspaceInfoMenuItem.Size = new Size(260, 22);
            workspaceInfoMenuItem.Text = "Workspace &Info";
            workspaceInfoMenuItem.ForeColor = WallyTheme.TextPrimary;

            verifyWorkspaceMenuItem.Name = "verifyWorkspaceMenuItem";
            verifyWorkspaceMenuItem.Size = new Size(260, 22);
            verifyWorkspaceMenuItem.Text = "&Verify Structure";
            verifyWorkspaceMenuItem.ForeColor = WallyTheme.TextPrimary;

            workspaceSeparator2.Name = "workspaceSeparator2";
            workspaceSeparator2.Size = new Size(257, 6);

            openWorkspaceFolderMenuItem.Name = "openWorkspaceFolderMenuItem";
            openWorkspaceFolderMenuItem.Size = new Size(260, 22);
            openWorkspaceFolderMenuItem.Text = "Open in &Explorer";
            openWorkspaceFolderMenuItem.ForeColor = WallyTheme.TextPrimary;

            // ═══════════════════════════════════════════════════════════════
            //  Main ToolStrip (action buttons below menu)
            // ═══════════════════════════════════════════════════════════════

            mainToolStrip.Dock = DockStyle.None;
            mainToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            mainToolStrip.Name = "mainToolStrip";
            mainToolStrip.Size = new Size(1280, 25);
            mainToolStrip.TabIndex = 1;
            mainToolStrip.Renderer = renderer;
            mainToolStrip.BackColor = WallyTheme.Surface2;
            mainToolStrip.ForeColor = WallyTheme.TextPrimary;
            mainToolStrip.Padding = new Padding(6, 0, 6, 0);
            mainToolStrip.Items.AddRange(new ToolStripItem[]
            {
                tsbOpen, tsbSetup, tsbSave,
                tsSeparator1,
                tsbRefresh, tsbReloadActors,
                tsSeparator2,
                tsbInfo, tsbClearChat
            });

            // Open
            tsbOpen.Name = "tsbOpen";
            tsbOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbOpen.Text = "\uD83D\uDCC2 Open";
            tsbOpen.ToolTipText = "Open Workspace (Ctrl+O)";
            tsbOpen.ForeColor = WallyTheme.TextPrimary;
            tsbOpen.Font = WallyTheme.FontUISmall;

            // Setup
            tsbSetup.Name = "tsbSetup";
            tsbSetup.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSetup.Text = "\u2728 Setup";
            tsbSetup.ToolTipText = "Setup New Workspace (Ctrl+Shift+N)";
            tsbSetup.ForeColor = WallyTheme.TextPrimary;
            tsbSetup.Font = WallyTheme.FontUISmall;

            // Save
            tsbSave.Name = "tsbSave";
            tsbSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSave.Text = "\uD83D\uDCBE Save";
            tsbSave.ToolTipText = "Save Workspace (Ctrl+S)";
            tsbSave.ForeColor = WallyTheme.TextPrimary;
            tsbSave.Font = WallyTheme.FontUISmall;

            tsSeparator1.Name = "tsSeparator1";

            // Refresh
            tsbRefresh.Name = "tsbRefresh";
            tsbRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbRefresh.Text = "\u21BB Refresh";
            tsbRefresh.ToolTipText = "Refresh Explorer (F5)";
            tsbRefresh.ForeColor = WallyTheme.TextPrimary;
            tsbRefresh.Font = WallyTheme.FontUISmall;

            // Reload Actors
            tsbReloadActors.Name = "tsbReloadActors";
            tsbReloadActors.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbReloadActors.Text = "\u267B Reload Actors";
            tsbReloadActors.ToolTipText = "Reload Actors from Disk (Ctrl+R)";
            tsbReloadActors.ForeColor = WallyTheme.TextPrimary;
            tsbReloadActors.Font = WallyTheme.FontUISmall;

            tsSeparator2.Name = "tsSeparator2";

            // Info
            tsbInfo.Name = "tsbInfo";
            tsbInfo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbInfo.Text = "\u2139 Info";
            tsbInfo.ToolTipText = "Workspace Info";
            tsbInfo.ForeColor = WallyTheme.TextPrimary;
            tsbInfo.Font = WallyTheme.FontUISmall;

            // Clear Chat
            tsbClearChat.Name = "tsbClearChat";
            tsbClearChat.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbClearChat.Text = "\u2715 Clear Chat";
            tsbClearChat.ToolTipText = "Clear Chat Conversation";
            tsbClearChat.ForeColor = WallyTheme.TextSecondary;
            tsbClearChat.Font = WallyTheme.FontUISmall;

            // ═══════════════════════════════════════════════════════════════
            //  WallyForms
            // ═══════════════════════════════════════════════════════════════

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 750);
            Controls.Add(toolStripContainer1);
            MainMenuStrip = menuStrip1;
            Name = "WallyForms";
            Text = "Wally \u2014 AI Actor Environment";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = WallyTheme.Surface0;
            ForeColor = WallyTheme.TextPrimary;

            mainToolStrip.ResumeLayout(false);
            mainToolStrip.PerformLayout();
            toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            toolStripContainer1.TopToolStripPanel.PerformLayout();
            toolStripContainer1.ResumeLayout(false);
            toolStripContainer1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        // ── Menu bar ──
        private ToolStripContainer toolStripContainer1;
        private MenuStrip menuStrip1;

        // ── File menu ──
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openWorkspaceMenuItem;
        private ToolStripMenuItem setupWorkspaceMenuItem;
        private ToolStripSeparator fileSeparator1;
        private ToolStripMenuItem saveWorkspaceMenuItem;
        private ToolStripMenuItem closeWorkspaceMenuItem;
        private ToolStripSeparator fileSeparator2;
        private ToolStripMenuItem exitMenuItem;

        // ── Edit menu ──
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem editCopyMenuItem;
        private ToolStripMenuItem editSelectAllMenuItem;

        // ── View menu ──
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem showExplorerMenuItem;
        private ToolStripMenuItem showChatMenuItem;
        private ToolStripMenuItem showCommandMenuItem;
        private ToolStripSeparator viewSeparator1;
        private ToolStripMenuItem refreshMenuItem;

        // ── Workspace menu ──
        private ToolStripMenuItem workspaceToolStripMenuItem;
        private ToolStripMenuItem reloadActorsMenuItem;
        private ToolStripMenuItem listActorsMenuItem;
        private ToolStripSeparator workspaceSeparator1;
        private ToolStripMenuItem workspaceInfoMenuItem;
        private ToolStripMenuItem verifyWorkspaceMenuItem;
        private ToolStripSeparator workspaceSeparator2;
        private ToolStripMenuItem openWorkspaceFolderMenuItem;

        // ── Main ToolStrip ──
        private ToolStrip mainToolStrip;
        private ToolStripButton tsbOpen;
        private ToolStripButton tsbSetup;
        private ToolStripButton tsbSave;
        private ToolStripSeparator tsSeparator1;
        private ToolStripButton tsbRefresh;
        private ToolStripButton tsbReloadActors;
        private ToolStripSeparator tsSeparator2;
        private ToolStripButton tsbInfo;
        private ToolStripButton tsbClearChat;
    }
}
