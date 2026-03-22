using System;
using System.Drawing;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Editor panel for user preferences (wally-prefs.json).
    /// Rendered as a hand-built themed form to match the rest of the UI —
    /// no PropertyGrid / OS chrome.
    /// </summary>
    public sealed class UserPreferencesEditorPanel : UserControl
    {
        // ?? Fields ???????????????????????????????????????????????????????????

        private readonly TextBox    _txtLastWorkspace;
        private readonly CheckBox   _chkAutoLoad;
        private readonly NumericUpDown _nudMaxRecent;

        private readonly Button _btnSave;
        private readonly Label  _lblStatus;

        private WallyPreferences? _prefs;
        private bool _loading;
        private bool _isDirty;

        public bool IsDirty => _isDirty;
        public event EventHandler? DirtyChanged;
        public event EventHandler? Saved;

        // ?? Constructor ??????????????????????????????????????????????????????

        public UserPreferencesEditorPanel()
        {
            SuspendLayout();

            Dock      = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            var scroll = ThemedEditorFactory.CreateScrollableSurface();

            var table = ThemedEditorFactory.CreateScrollableFormTable(1);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            int row = 0;

            // ?? Header ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("??  User Preferences", WallyTheme.FontUIBold, WallyTheme.TextPrimary), 0, row++);

            // ?? Action bar ??
            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize      = true,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0, 4, 0, 8)
            };
            _btnSave = MakeButton("??  Save");
            _btnSave.Click += OnSave;
            _lblStatus = new Label
            {
                Text      = "",
                AutoSize  = true,
                Font      = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Padding   = new Padding(8, 6, 0, 0)
            };
            actionBar.Controls.Add(_btnSave);
            actionBar.Controls.Add(_lblStatus);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(actionBar, 0, row++);

            // ?? Session section ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("??  Session", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            // Last workspace (read-only display)
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Last Workspace Path"), 0, row++);
            _txtLastWorkspace = MakeTextBox(readOnly: true);
            _txtLastWorkspace.ForeColor = WallyTheme.TextMuted;
            _txtLastWorkspace.ToolTipText("Path of the most recently loaded workspace");
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtLastWorkspace, 0, row++);

            // Auto-load
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _chkAutoLoad = MakeCheckBox("Auto-load last workspace on startup");
            _chkAutoLoad.CheckedChanged += OnFieldChanged;
            table.Controls.Add(_chkAutoLoad, 0, row++);

            // ?? History section ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("??  History", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Maximum Recent Workspaces"), 0, row++);
            _nudMaxRecent = MakeNumeric(1, 50);
            _nudMaxRecent.ValueChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_nudMaxRecent, 0, row++);

            scroll.Controls.Add(table);
            Controls.Add(scroll);
            ResumeLayout(true);
        }

        // ?? Public API ???????????????????????????????????????????????????????

        public void LoadPreferences()
        {
            _loading = true;
            try
            {
                _prefs = WallyPreferencesStore.Load();

                _txtLastWorkspace.Text = _prefs.LastWorkspacePath ?? "(none)";
                _chkAutoLoad.Checked   = _prefs.AutoLoadLast;
                _nudMaxRecent.Value    = Math.Clamp(_prefs.MaxRecentCount, 1, 50);

                SetDirty(false);
                _lblStatus.Text      = "Loaded from user profile.";
                _lblStatus.ForeColor = WallyTheme.TextMuted;
            }
            finally { _loading = false; }
        }

        public void SavePreferences()
        {
            if (_prefs == null) return;
            ApplyFieldsToPrefs();
            WallyPreferencesStore.Save(_prefs);
            SetDirty(false);
        }

        // ?? Apply / save ?????????????????????????????????????????????????????

        private void ApplyFieldsToPrefs()
        {
            if (_prefs == null) return;
            _prefs.AutoLoadLast    = _chkAutoLoad.Checked;
            _prefs.MaxRecentCount  = (int)_nudMaxRecent.Value;
            // LastWorkspacePath is read-only in this editor
        }

        private void OnSave(object? sender, EventArgs e)
        {
            try
            {
                ApplyFieldsToPrefs();
                WallyPreferencesStore.Save(_prefs!);
                SetDirty(false);
                _lblStatus.Text      = $"Saved at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
                Saved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _lblStatus.Text      = $"Save failed: {ex.Message}";
                _lblStatus.ForeColor = WallyTheme.Red;
            }
        }

        private void OnFieldChanged(object? sender, EventArgs e)
        {
            if (_loading) return;
            SetDirty(true);
        }

        private void SetDirty(bool dirty)
        {
            _isDirty         = dirty;
            _btnSave.Enabled = dirty;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        // ?? Control factories ????????????????????????????????????????????????

        private static Label MakeSection(string text, Font font, Color color) =>
            new()
            {
                Text      = text,
                AutoSize  = true,
                Font      = font,
                ForeColor = color,
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 8, 0, 4),
                Dock      = DockStyle.Top
            };

        private static Label MakeLabel(string text) =>
            new()
            {
                Text      = text,
                AutoSize  = true,
                Font      = WallyTheme.FontUISmallBold,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 0, 0, 2),
                Dock      = DockStyle.Top
            };

        private static TextBox MakeTextBox(bool readOnly = false) =>
            new()
            {
                Dock        = DockStyle.Top,
                Font        = WallyTheme.FontUI,
                BackColor   = WallyTheme.Surface2,
                ForeColor   = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly    = readOnly,
                Margin      = new Padding(0, 0, 0, 4)
            };

        private static CheckBox MakeCheckBox(string text) =>
            new()
            {
                Text      = text,
                AutoSize  = true,
                Font      = WallyTheme.FontUI,
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 4, 0, 8)
            };

        private static NumericUpDown MakeNumeric(int min, int max) =>
            new()
            {
                Width       = 120,
                Minimum     = min,
                Maximum     = max,
                Font        = WallyTheme.FontUI,
                BackColor   = WallyTheme.Surface2,
                ForeColor   = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin      = new Padding(0, 0, 0, 4)
            };

        private static Button MakeButton(string text)
        {
            var btn = new Button
            {
                Text      = text,
                AutoSize  = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3,
                ForeColor = WallyTheme.TextPrimary,
                Font      = WallyTheme.FontUISmallBold,
                Cursor    = Cursors.Hand,
                Padding   = new Padding(8, 2, 8, 2),
                Margin    = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }
    }

    // ?? Extension to attach a ToolTip to a TextBox inline ????????????????????

    internal static class ControlExtensions
    {
        public static void ToolTipText(this Control control, string text)
        {
            var tip = new ToolTip();
            tip.SetToolTip(control, text);
        }
    }
}
