using System;
using System.Drawing;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Editor panel for viewing and editing a WallyLoopDefinition.
    /// </summary>
    public sealed class LoopEditorPanel : UserControl
    {
        private readonly TextBox _txtName;
        private readonly TextBox _txtDescription;
        private readonly TextBox _txtActorName;
        private readonly RichTextBox _txtStartPrompt;
        private readonly RichTextBox _txtContinueTemplate;
        private readonly TextBox _txtCompletedKeyword;
        private readonly TextBox _txtErrorKeyword;
        private readonly NumericUpDown _nudMaxIter;
        private readonly Label _lblStatus;
        private readonly Button _btnSave;
        private readonly Button _btnRevert;

        private WallyLoopDefinition? _loop;
        private WallyEnvironment? _environment;
        private bool _isDirty;

        public event EventHandler? DirtyChanged;
        public event EventHandler? Saved;

        public bool IsDirty => _isDirty;
        public WallyLoopDefinition? Loop => _loop;

        public LoopEditorPanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            var scroll = new ThemedScrollPanel
            {
                Dock = DockStyle.Fill,
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
            table.Controls.Add(CreateSectionLabel("\u267B Loop Editor", WallyTheme.FontUIBold, WallyTheme.TextPrimary), 0, row++);

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
            table.Controls.Add(CreateFieldLabel("Actor Name"), 0, row++);
            _txtActorName = CreateTextBox();
            _txtActorName.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtActorName, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("Start Prompt", WallyTheme.FontUISmallBold, WallyTheme.TextMuted), 0, row++);
            _txtStartPrompt = CreateRichTextBox(140);
            _txtStartPrompt.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtStartPrompt, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("Continue Prompt Template", WallyTheme.FontUISmallBold, WallyTheme.TextMuted), 0, row++);
            _txtContinueTemplate = CreateRichTextBox(140);
            _txtContinueTemplate.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtContinueTemplate, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Completed Keyword"), 0, row++);
            _txtCompletedKeyword = CreateTextBox();
            _txtCompletedKeyword.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtCompletedKeyword, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Error Keyword"), 0, row++);
            _txtErrorKeyword = CreateTextBox();
            _txtErrorKeyword.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtErrorKeyword, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Max Iterations (0 = use workspace default)"), 0, row++);
            _nudMaxIter = new NumericUpDown
            {
                Width = 120,
                Minimum = 0,
                Maximum = 1000,
                Font = WallyTheme.FontUI,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 4)
            };
            _nudMaxIter.ValueChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_nudMaxIter, 0, row++);

            scroll.Controls.Add(table);
            Controls.Add(scroll);
            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env) => _environment = env;

        public void LoadLoop(WallyLoopDefinition loop)
        {
            _loop = loop;
            PopulateFields();
            SetDirty(false);
            _lblStatus.Text = $"Loaded: {loop.Name}";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        // ?? Field population ????????????????????????????????????????????????

        private void PopulateFields()
        {
            if (_loop == null) return;

            _txtName.Text = _loop.Name;
            _txtDescription.Text = _loop.Description;
            _txtActorName.Text = _loop.ActorName;
            _txtStartPrompt.Text = _loop.StartPrompt;
            _txtContinueTemplate.Text = _loop.ContinuePromptTemplate ?? "";
            _txtCompletedKeyword.Text = _loop.CompletedKeyword ?? "";
            _txtErrorKeyword.Text = _loop.ErrorKeyword ?? "";
            _nudMaxIter.Value = Math.Max(0, Math.Min(1000, _loop.MaxIterations));
        }

        private void ApplyFieldsToLoop()
        {
            if (_loop == null) return;

            _loop.Name = _txtName.Text.Trim();
            _loop.Description = _txtDescription.Text.Trim();
            _loop.ActorName = _txtActorName.Text.Trim();
            _loop.StartPrompt = _txtStartPrompt.Text.Trim();
            _loop.ContinuePromptTemplate = string.IsNullOrWhiteSpace(_txtContinueTemplate.Text)
                ? null : _txtContinueTemplate.Text.Trim();
            _loop.CompletedKeyword = string.IsNullOrWhiteSpace(_txtCompletedKeyword.Text)
                ? null : _txtCompletedKeyword.Text.Trim();
            _loop.ErrorKeyword = string.IsNullOrWhiteSpace(_txtErrorKeyword.Text)
                ? null : _txtErrorKeyword.Text.Trim();
            _loop.MaxIterations = (int)_nudMaxIter.Value;
        }

        // ?? Event handlers ??????????????????????????????????????????????????

        private void OnFieldChanged(object? sender, EventArgs e)
        {
            if (_loop == null) return;
            SetDirty(true);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_loop == null || _environment?.HasWorkspace != true) return;

            try
            {
                ApplyFieldsToLoop();

                string loopsFolder = System.IO.Path.Combine(
                    _environment.WorkspaceFolder!,
                    _environment.Workspace!.Config.LoopsFolderName);
                System.IO.Directory.CreateDirectory(loopsFolder);

                string filePath = System.IO.Path.Combine(loopsFolder, $"{_loop.Name}.json");
                _loop.SaveToFile(filePath);

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
            if (_loop == null) return;
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

        // ?? Control factories ???????????????????????????????????????????????

        private static Label CreateSectionLabel(string text, Font font, Color color) =>
            new() { Text = text, AutoSize = true, Font = font, ForeColor = color, BackColor = Color.Transparent, Margin = new Padding(0, 8, 0, 4), Dock = DockStyle.Top };

        private static Label CreateFieldLabel(string text) =>
            new() { Text = text, AutoSize = true, Font = WallyTheme.FontUISmallBold, ForeColor = WallyTheme.TextMuted, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 2), Dock = DockStyle.Top };

        private static TextBox CreateTextBox() =>
            new() { Dock = DockStyle.Top, Font = WallyTheme.FontUI, BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 4) };

        private static RichTextBox CreateRichTextBox(int height) =>
            new() { Dock = DockStyle.Top, Height = height, MinimumSize = new Size(0, height), Font = WallyTheme.FontMono, BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, WordWrap = true, ScrollBars = RichTextBoxScrollBars.Vertical, Margin = new Padding(0, 0, 0, 4) };

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
