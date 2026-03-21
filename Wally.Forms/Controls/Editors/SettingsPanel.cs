using System;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Controls.Editors;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Tabbed settings panel for editing workspace and user preferences.
    /// </summary>
    public sealed class SettingsPanel : UserControl
    {
        private readonly TabControl _tabControl;
        private readonly ConfigEditorPanel _workspacePanel;
        private readonly UserPreferencesEditorPanel _userPanel;

        /// <summary>Tab index for Workspace settings.</summary>
        public const int TabIndexWorkspace = 0;

        /// <summary>Tab index for User Preferences.</summary>
        public const int TabIndexUser = 1;

        public SettingsPanel(WallyEnvironment environment)
        {
            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontUI,
                BackColor = WallyTheme.Surface0,
                ForeColor = WallyTheme.TextPrimary
            };

            // Workspace settings tab
            _workspacePanel = new ConfigEditorPanel();
            _workspacePanel.BindEnvironment(environment);
            _workspacePanel.LoadConfig();
            var workspaceTab = new TabPage("?  Workspace") { Controls = { _workspacePanel } };

            // User preferences tab
            _userPanel = new UserPreferencesEditorPanel();
            _userPanel.LoadPreferences();
            var userTab = new TabPage("??  User") { Controls = { _userPanel } };

            _tabControl.TabPages.Add(workspaceTab);
            _tabControl.TabPages.Add(userTab);

            Controls.Add(_tabControl);
        }

        /// <summary>
        /// Activates the tab at the given index (use the TabIndex* constants).
        /// </summary>
        public void SelectTab(int index)
        {
            if (index >= 0 && index < _tabControl.TabPages.Count)
                _tabControl.SelectedIndex = index;
        }
    }
}
