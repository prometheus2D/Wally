using System;
using System.Drawing;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Providers;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Editor panel for viewing and editing an LLM Wrapper definition.
    /// </summary>
    public sealed class WrapperEditorPanel : UserControl
    {
        private readonly TextBox _txtName;
        private readonly TextBox _txtDescription;
        private readonly TextBox _txtExecutable;
        private readonly RichTextBox _txtArgTemplate;
        private readonly TextBox _txtModelArgFormat;
        private readonly TextBox _txtSourcePathArgFormat;
        private readonly CheckBox _chkUseSourceAsWD;
        private readonly CheckBox _chkCanMakeChanges;
        private readonly Label _lblStatus;
        private readonly Button _btnSave;
        private readonly Button _btnRevert;

        private LLMWrapper? _wrapper;
        private WallyEnvironment? _environment;
        private bool _isDirty;

        public event EventHandler? DirtyChanged;
        public event EventHandler? Saved;

        public bool IsDirty => _isDirty;
        public LLMWrapper? Wrapper => _wrapper;

        public WrapperEditorPanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = WallyTheme.Surface0
            };

            var table = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                ColumnCount = 1,
                BackColor = WallyTheme.Surface0,
                Padding = new Padding(20)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            int row = 0;

            // Header
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("\u2699 Wrapper Editor", WallyTheme.FontUIBold, WallyTheme.TextPrimary), 0, row++);

            // Action bar
            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 8)
            };

            _btnSave = CreateButton("\uD83D\uDCBE Save");
            _btnSave.Click += OnSave;
            _btnRevert = CreateButton("\u21BA Revert");
            _btnRevert.Click += OnRevert;

            _lblStatus = new Label
            {
                Text = "",
                AutoSize = true,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 6, 0, 0)
            };

            actionBar.Controls.Add(_btnSave);
            actionBar.Controls.Add(_btnRevert);
            actionBar.Controls.Add(_lblStatus);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(actionBar, 0, row++);

            // Fields
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Name"), 0, row++);
            _txtName = CreateTextBox();
            _txtName.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtName, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Description"), 0, row++);
            _txtDescription = CreateTextBox();
            _txtDescription.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtDescription, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("CLI Recipe", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Executable"), 0, row++);
            _txtExecutable = CreateTextBox();
            _txtExecutable.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtExecutable, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Argument Template"), 0, row++);
            _txtArgTemplate = CreateRichTextBox(120);
            _txtArgTemplate.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtArgTemplate, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Model Arg Format (e.g. --model {model})"), 0, row++);
            _txtModelArgFormat = CreateTextBox();
            _txtModelArgFormat.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtModelArgFormat, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Source Path Arg Format (e.g. --add-dir {sourcePath})"), 0, row++);
            _txtSourcePathArgFormat = CreateTextBox();
            _txtSourcePathArgFormat.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtSourcePathArgFormat, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("Behaviour", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            _chkUseSourceAsWD = CreateCheckBox("Use source path as working directory");
            _chkUseSourceAsWD.CheckedChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_chkUseSourceAsWD, 0, row++);

            _chkCanMakeChanges = CreateCheckBox("Can make changes to files (agentic mode)");
            _chkCanMakeChanges.CheckedChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_chkCanMakeChanges, 0, row++);

            scroll.Controls.Add(table);
            Controls.Add(scroll);
            ResumeLayout(true);
        }

        // ── Public API ──────────────────────────────────────────────────────

        public void BindEnvironment(WallyEnvironment env) => _environment = env;

        public void LoadWrapper(LLMWrapper wrapper)
        {
            _wrapper = wrapper;
            PopulateFields();
            SetDirty(false);
            _lblStatus.Text = $"Loaded: {wrapper.Name}";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        // ── Field population ────────────────────────────────────────────────

        private void PopulateFields()
        {
            if (_wrapper == null) return;

            _txtName.Text = _wrapper.Name;
            _txtDescription.Text = _wrapper.Description;
            _txtExecutable.Text = _wrapper.Executable;
            _txtArgTemplate.Text = _wrapper.ArgumentTemplate;
            _txtModelArgFormat.Text = _wrapper.ModelArgFormat;
            _txtSourcePathArgFormat.Text = _wrapper.SourcePathArgFormat;
            _chkUseSourceAsWD.Checked = _wrapper.UseSourcePathAsWorkingDirectory;
            _chkCanMakeChanges.Checked = _wrapper.CanMakeChanges;
        }

        private void ApplyFieldsToWrapper()
        {
            if (_wrapper == null) return;

            _wrapper.Name = _txtName.Text.Trim();
            _wrapper.Description = _txtDescription.Text.Trim();
            _wrapper.Executable = _txtExecutable.Text.Trim();
            _wrapper.ArgumentTemplate = _txtArgTemplate.Text.Trim();
            _wrapper.ModelArgFormat = _txtModelArgFormat.Text.Trim();
            _wrapper.SourcePathArgFormat = _txtSourcePathArgFormat.Text.Trim();
            _wrapper.UseSourcePathAsWorkingDirectory = _chkUseSourceAsWD.Checked;
            _wrapper.CanMakeChanges = _chkCanMakeChanges.Checked;
        }

        // ── Event handlers ──────────────────────────────────────────────────

        private void OnFieldChanged(object? sender, EventArgs e)
        {
            if (_wrapper == null) return;
            SetDirty(true);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_wrapper == null || _environment?.HasWorkspace != true) return;

            try
            {
                ApplyFieldsToWrapper();

                string wrappersFolder = System.IO.Path.Combine(
                    _environment.WorkspaceFolder!,
                    _environment.Workspace!.Config.WrappersFolderName);
                System.IO.Directory.CreateDirectory(wrappersFolder);

                string filePath = System.IO.Path.Combine(wrappersFolder, $"{_wrapper.Name}.json");
                _wrapper.SaveToFile(filePath);

                SetDirty(false);
                _lblStatus.Text = $"Saved at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
                Saved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Save failed: {ex.Message}";
                _lblStatus.ForeColor = WallyTheme.Red;
            }
        }

        private void OnRevert(object? sender, EventArgs e)
        {
            if (_wrapper == null) return;
            PopulateFields();
            SetDirty(false);
            _lblStatus.Text = "Reverted.";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        private void SetDirty(bool dirty)
        {
            _isDirty = dirty;
            _btnSave.Enabled = dirty;
            _btnRevert.Enabled = dirty;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── Control factories ───────────────────────────────────────────────

        private static Label CreateSectionLabel(string text, Font font, Color color) =>
            new() { Text = text, AutoSize = true, Font = font, ForeColor = color, BackColor = Color.Transparent, Margin = new Padding(0, 8, 0, 4), Dock = DockStyle.Top };

        private static Label CreateFieldLabel(string text) =>
            new() { Text = text, AutoSize = true, Font = WallyTheme.FontUISmallBold, ForeColor = WallyTheme.TextMuted, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 2), Dock = DockStyle.Top };

        private static TextBox CreateTextBox() =>
            new() { Dock = DockStyle.Top, Font = WallyTheme.FontUI, BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 4) };

        private static RichTextBox CreateRichTextBox(int height) =>
            new() { Dock = DockStyle.Top, Height = height, MinimumSize = new Size(0, height), Font = WallyTheme.FontMono, BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, WordWrap = true, ScrollBars = RichTextBoxScrollBars.Vertical, Margin = new Padding(0, 0, 0, 4) };

        private static CheckBox CreateCheckBox(string text) =>
            new() { Text = text, AutoSize = true, Font = WallyTheme.FontUI, ForeColor = WallyTheme.TextPrimary, BackColor = Color.Transparent, Margin = new Padding(0, 4, 0, 4) };

        private static Button CreateButton(string text)
        {
            var btn = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = WallyTheme.Surface3, ForeColor = WallyTheme.TextPrimary, Font = WallyTheme.FontUISmallBold, Cursor = Cursors.Hand, Padding = new Padding(8, 2, 8, 2), Margin = new Padding(0, 0, 6, 0) };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }
    }
}
