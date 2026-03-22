using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ScintillaNET;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Generic text file editor panel backed by Scintilla.
    /// Supports JSON, Markdown, and plain-text files with syntax highlighting,
    /// save, revert, and dirty-state tracking.
    /// </summary>
    public sealed class TextFileEditorPanel : UserControl
    {
        private readonly Label     _lblFileName;
        private readonly Label     _lblStatus;
        private readonly Scintilla _editor;
        private readonly Button    _btnSave;
        private readonly Button    _btnRevert;

        private string? _filePath;
        private string? _originalContent;
        private bool    _isDirty;

        /// <summary>Raised when the dirty state changes.</summary>
        public event EventHandler? DirtyChanged;

        /// <summary>Raised after a successful save.</summary>
        public event EventHandler? Saved;

        /// <summary>Whether the content has unsaved changes.</summary>
        public bool IsDirty => _isDirty;

        /// <summary>The absolute path of the file being edited.</summary>
        public string? FilePath => _filePath;

        public TextFileEditorPanel(string languageId)
        {
            SuspendLayout();

            Dock      = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            // ?? Header ?????????????????????????????????????????????????????
            var headerPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = WallyTheme.Surface0,
                Padding       = new Padding(20, 12, 20, 0)
            };

            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize      = true,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0, 0, 0, 6)
            };

            _btnSave = CreateButton("\uD83D\uDCBE Save");
            _btnSave.Click += OnSave;
            _btnSave.Enabled = false;

            _btnRevert = CreateButton("\u21BA Revert");
            _btnRevert.Click += OnRevert;
            _btnRevert.Enabled = false;

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
            actionBar.Controls.Add(_btnRevert);
            actionBar.Controls.Add(_lblStatus);
            headerPanel.Controls.Add(actionBar);

            _lblFileName = new Label
            {
                Text      = "",
                AutoSize  = true,
                Font      = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 0, 0, 6)
            };
            headerPanel.Controls.Add(_lblFileName);

            // ?? Scintilla editor ???????????????????????????????????????????
            _editor = ThemedEditorFactory.CreateCodeEditor(languageId, readOnly: false);
            _editor.TextChanged += OnContentChanged;

            Controls.Add(_editor);
            Controls.Add(headerPanel);
            ResumeLayout(true);
        }

        // ?? Public API ?????????????????????????????????????????????????????

        /// <summary>
        /// Loads a file into the editor. The file's content becomes the
        /// baseline for dirty tracking.
        /// </summary>
        public void LoadFile(string filePath)
        {
            _filePath = filePath;
            _lblFileName.Text = filePath;

            _editor.TextChanged -= OnContentChanged;

            if (File.Exists(filePath))
            {
                _originalContent = File.ReadAllText(filePath);
                _editor.Text     = _originalContent;
            }
            else
            {
                _originalContent = "";
                _editor.Text     = "";
            }

            _editor.EmptyUndoBuffer();
            _editor.TextChanged += OnContentChanged;

            SetDirty(false);
            _lblStatus.Text      = $"Loaded: {Path.GetFileName(filePath)}";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        /// <summary>
        /// Saves the current content back to disk. Returns true on success.
        /// </summary>
        public bool Save()
        {
            if (_filePath == null) return false;
            try
            {
                File.WriteAllText(_filePath, _editor.Text);
                _originalContent = _editor.Text;
                _editor.EmptyUndoBuffer();
                SetDirty(false);
                _lblStatus.Text      = $"Saved at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
                Saved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                _lblStatus.Text      = $"Save failed: {ex.Message}";
                _lblStatus.ForeColor = WallyTheme.Red;
                return false;
            }
        }

        // ?? Event handlers ?????????????????????????????????????????????????

        private void OnContentChanged(object? sender, EventArgs e)
        {
            if (_filePath == null) return;
            SetDirty(_editor.Text != _originalContent);
        }

        private void OnSave(object? sender, EventArgs e) => Save();

        private void OnRevert(object? sender, EventArgs e)
        {
            if (_filePath == null || _originalContent == null) return;
            _editor.TextChanged -= OnContentChanged;
            _editor.Text = _originalContent;
            _editor.EmptyUndoBuffer();
            _editor.TextChanged += OnContentChanged;
            SetDirty(false);
            _lblStatus.Text      = "Reverted.";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        private void SetDirty(bool dirty)
        {
            if (_isDirty == dirty) return;
            _isDirty           = dirty;
            _btnSave.Enabled   = dirty;
            _btnRevert.Enabled = dirty;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        // ?? Helpers ????????????????????????????????????????????????????????

        /// <summary>
        /// Determines the Scintilla language ID from a file extension.
        /// </summary>
        public static string LanguageIdForFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".json"                          => "json",
                ".md" or ".markdown"             => "markdown",
                ".wrb"                           => "wally-runbook",
                _                                => "text"
            };
        }

        private static Button CreateButton(string text)
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
}
