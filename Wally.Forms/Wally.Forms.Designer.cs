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
            fileToolStripMenuItem = new ToolStripMenuItem();
            openWorkspaceMenuItem = new ToolStripMenuItem();
            setupWorkspaceMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            editCopyMenuItem = new ToolStripMenuItem();
            editSelectAllMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            showExplorerMenuItem = new ToolStripMenuItem();
            showChatMenuItem = new ToolStripMenuItem();
            showCommandMenuItem = new ToolStripMenuItem();
            viewSeparator1 = new ToolStripSeparator();
            refreshMenuItem = new ToolStripMenuItem();
            toolStripContainer1.TopToolStripPanel.SuspendLayout();
            toolStripContainer1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();

            // toolStripContainer1
            toolStripContainer1.ContentPanel.Size = new Size(1280, 700);
            toolStripContainer1.Dock = DockStyle.Fill;
            toolStripContainer1.Location = new Point(0, 0);
            toolStripContainer1.Name = "toolStripContainer1";
            toolStripContainer1.Size = new Size(1280, 750);
            toolStripContainer1.TabIndex = 0;
            toolStripContainer1.TopToolStripPanel.Controls.Add(menuStrip1);
            toolStripContainer1.BottomToolStripPanelVisible = false;
            toolStripContainer1.LeftToolStripPanelVisible = false;
            toolStripContainer1.RightToolStripPanelVisible = false;

            // menuStrip1
            menuStrip1.Dock = DockStyle.None;
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1280, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.BackColor = WallyTheme.Surface2;
            menuStrip1.ForeColor = WallyTheme.TextPrimary;
            menuStrip1.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false };

            // fileToolStripMenuItem
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openWorkspaceMenuItem, setupWorkspaceMenuItem, toolStripSeparator1, exitMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";

            // openWorkspaceMenuItem
            openWorkspaceMenuItem.Name = "openWorkspaceMenuItem";
            openWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openWorkspaceMenuItem.Size = new Size(250, 22);
            openWorkspaceMenuItem.Text = "&Open Workspace\u2026";

            // setupWorkspaceMenuItem
            setupWorkspaceMenuItem.Name = "setupWorkspaceMenuItem";
            setupWorkspaceMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            setupWorkspaceMenuItem.Size = new Size(250, 22);
            setupWorkspaceMenuItem.Text = "&Setup Workspace\u2026";

            // toolStripSeparator1
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(247, 6);

            // exitMenuItem
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitMenuItem.Size = new Size(250, 22);
            exitMenuItem.Text = "E&xit";

            // editToolStripMenuItem
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { editCopyMenuItem, editSelectAllMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "&Edit";

            // editCopyMenuItem
            editCopyMenuItem.Name = "editCopyMenuItem";
            editCopyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            editCopyMenuItem.Size = new Size(200, 22);
            editCopyMenuItem.Text = "&Copy";
            editCopyMenuItem.Click += (s, e) => { if (ActiveControl is RichTextBox rtb) rtb.Copy(); };

            // editSelectAllMenuItem
            editSelectAllMenuItem.Name = "editSelectAllMenuItem";
            editSelectAllMenuItem.ShortcutKeys = Keys.Control | Keys.A;
            editSelectAllMenuItem.Size = new Size(200, 22);
            editSelectAllMenuItem.Text = "Select &All";
            editSelectAllMenuItem.Click += (s, e) => { if (ActiveControl is RichTextBox rtb) rtb.SelectAll(); };

            // viewToolStripMenuItem
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showExplorerMenuItem, showChatMenuItem, showCommandMenuItem, viewSeparator1, refreshMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";

            // showExplorerMenuItem
            showExplorerMenuItem.Name = "showExplorerMenuItem";
            showExplorerMenuItem.Size = new Size(240, 22);
            showExplorerMenuItem.Text = "File &Explorer\tCtrl+1";
            showExplorerMenuItem.Checked = true;
            showExplorerMenuItem.CheckOnClick = true;

            // showChatMenuItem
            showChatMenuItem.Name = "showChatMenuItem";
            showChatMenuItem.Size = new Size(240, 22);
            showChatMenuItem.Text = "AI &Chat\tCtrl+2";
            showChatMenuItem.Checked = true;
            showChatMenuItem.CheckOnClick = true;

            // showCommandMenuItem
            showCommandMenuItem.Name = "showCommandMenuItem";
            showCommandMenuItem.Size = new Size(240, 22);
            showCommandMenuItem.Text = "Co&mmand Line\tCtrl+3";
            showCommandMenuItem.Checked = true;
            showCommandMenuItem.CheckOnClick = true;

            // viewSeparator1
            viewSeparator1.Name = "viewSeparator1";
            viewSeparator1.Size = new Size(237, 6);

            // refreshMenuItem
            refreshMenuItem.Name = "refreshMenuItem";
            refreshMenuItem.ShortcutKeys = Keys.F5;
            refreshMenuItem.Size = new Size(240, 22);
            refreshMenuItem.Text = "&Refresh Explorer";

            // WallyForms
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

            toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            toolStripContainer1.TopToolStripPanel.PerformLayout();
            toolStripContainer1.ResumeLayout(false);
            toolStripContainer1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ToolStripContainer toolStripContainer1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openWorkspaceMenuItem;
        private ToolStripMenuItem setupWorkspaceMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem editCopyMenuItem;
        private ToolStripMenuItem editSelectAllMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem showExplorerMenuItem;
        private ToolStripMenuItem showChatMenuItem;
        private ToolStripMenuItem showCommandMenuItem;
        private ToolStripSeparator viewSeparator1;
        private ToolStripMenuItem refreshMenuItem;
    }
}
