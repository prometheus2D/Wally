using System;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Editor panel for user preferences (wally-prefs.json).
    /// </summary>
    public sealed class UserPreferencesEditorPanel : UserControl
    {
        private readonly PropertyGrid _propertyGrid;
        private WallyPreferences? _prefs;

        public UserPreferencesEditorPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;
            _propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                HelpVisible = true,
                ToolbarVisible = false,
                BackColor = WallyTheme.Surface0,
                ForeColor = WallyTheme.TextPrimary
            };
            Controls.Add(_propertyGrid);
        }

        public void LoadPreferences()
        {
            _prefs = WallyPreferencesStore.Load();
            _propertyGrid.SelectedObject = _prefs;
        }

        public void SavePreferences()
        {
            if (_prefs != null)
                WallyPreferencesStore.Save(_prefs);
        }
    }
}
