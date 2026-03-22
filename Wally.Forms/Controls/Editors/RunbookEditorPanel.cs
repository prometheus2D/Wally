using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ScintillaNET;
using Wally.Core;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Editor panel for viewing and editing a Runbook (.wrb) file.
    /// Uses the Scintilla-based code editor with .wrb syntax highlighting.
    /// </summary>
    public sealed class RunbookEditorPanel : UserControl
    {
        private readonly Label     _lblName;
        private readonly Label     _lblDescription;
        private readonly Scintilla _txtContent;
        private readonly Label     _lblStatus;
        private readonly Button    _btnSave;
        private readonly Button    _btnRevert;

        private WallyRunbook? _runbook;
        private string? _originalContent;
        private bool _isDirty;

        public event EventHandler? DirtyChanged;
        public event EventHandler? Saved;

        public bool IsDirty => _isDirty;
        public WallyRunbook? Runbook => _runbook;

        public RunbookEditorPanel()
        {
            SuspendLayout();
            Dock      = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            var headerPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoSize = true, BackColor = WallyTheme.Surface0,
                Padding = new Padding(20, 20, 20, 0)
            };

            headerPanel.Controls.Add(CreateSectionLabel("\uD83D\uDCDC Runbook Editor", WallyTheme.FontUIBold, WallyTheme.TextPrimary));
            headerPanel.Controls.Add(CreateSpacer(8));

            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight, AutoSize = true,
                WrapContents = false, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };

            _btnSave   = CreateButton("\uD83D\uDCBE Save");
            _btnSave.Click += OnSave;
            _btnRevert = CreateButton("\u21BA Revert");
            _btnRevert.Click += OnRevert;

            _lblStatus = new Label
            {
                Text = "", AutoSize = true, Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted, BackColor = Color.Transparent,
                Padding = new Padding(8, 6, 0, 0)
            };

            actionBar.Controls.Add(_btnSave);
            actionBar.Controls.Add(_btnRevert);
            actionBar.Controls.Add(_lblStatus);
            headerPanel.Controls.Add(actionBar);

            _lblName = new Label
            {
                Text = "", AutoSize = true, Font = WallyTheme.FontUIBold,
                ForeColor = WallyTheme.TextSecondary, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 2)
            };
            headerPanel.Controls.Add(_lblName);

            _lblDescription = new Label
            {
                Text = "", AutoSize = true, Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };
            headerPanel.Controls.Add(_lblDescription);

            // Scintilla code editor with .wrb syntax highlighting.
            _txtContent = ThemedEditorFactory.CreateRunbookEditor(readOnly: false);
            _txtContent.TextChanged += OnContentChanged;

            Controls.Add(_txtContent);
            Controls.Add(headerPanel);
            ResumeLayout(true);
        }

        // ?? Public API ?????????????????????????????????????????????????????

        public void LoadRunbook(WallyRunbook runbook)
        {
            _runbook = runbook;
            _lblName.Text        = runbook.Name;
            _lblDescription.Text = runbook.Description;

            _txtContent.TextChanged -= OnContentChanged;

            if (File.Exists(runbook.FilePath))
            {
                _originalContent = File.ReadAllText(runbook.FilePath);
                _txtContent.Text = _originalContent;
            }
            else
            {
                _originalContent = "";
                _txtContent.Text = "";
            }

            _txtContent.EmptyUndoBuffer();
            _txtContent.TextChanged += OnContentChanged;

            SetDirty(false);
            _lblStatus.Text      = $"Loaded from: {runbook.FilePath}";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        /// <summary>
        /// Saves the current content back to disk. Returns true on success.
        /// </summary>
        public bool Save()
        {
            if (_runbook == null) return false;
            OnSave(this, EventArgs.Empty);
            return !_isDirty; // OnSave sets dirty=false on success
        }

        // ?? Event handlers ?????????????????????????????????????????????????

        private void OnContentChanged(object? sender, EventArgs e)
        {
            if (_runbook == null) return;
            SetDirty(_txtContent.Text != _originalContent);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_runbook == null) return;
            try
            {
                File.WriteAllText(_runbook.FilePath, _txtContent.Text);
                _originalContent = _txtContent.Text;
                _txtContent.EmptyUndoBuffer();
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

        private void OnRevert(object? sender, EventArgs e)
        {
            if (_runbook == null || _originalContent == null) return;
            _txtContent.TextChanged -= OnContentChanged;
            _txtContent.Text = _originalContent;
            _txtContent.EmptyUndoBuffer();
            _txtContent.TextChanged += OnContentChanged;
            SetDirty(false);
            _lblStatus.Text      = "Reverted.";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        private void SetDirty(bool dirty)
        {
            _isDirty           = dirty;
            _btnSave.Enabled   = dirty;
            _btnRevert.Enabled = dirty;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        // ?? Control factories ??????????????????????????????????????????????

        private static Label CreateSectionLabel(string text, Font font, Color color) =>
            new() { Text = text, AutoSize = true, Font = font, ForeColor = color,
                    BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 4) };

        private static Button CreateButton(string text)
        {
            var btn = new Button
            {
                Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3, ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUISmallBold, Cursor = Cursors.Hand,
                Padding = new Padding(8, 2, 8, 2), Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }

        private static Panel CreateSpacer(int height) =>
            new() { Height = height, Width = 500, BackColor = Color.Transparent, Margin = Padding.Empty };
    }
}
