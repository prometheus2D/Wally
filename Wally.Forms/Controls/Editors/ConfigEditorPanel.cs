using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Editor panel for viewing and editing wally-config.json settings.
    /// Provides structured access to all workspace configuration fields.
    /// </summary>
    public sealed class ConfigEditorPanel : UserControl
    {
        // Folder names
        private readonly TextBox _txtActorsFolder;
        private readonly TextBox _txtLogsFolder;
        private readonly TextBox _txtDocsFolder;
        private readonly TextBox _txtTemplatesFolder;
        private readonly TextBox _txtLoopsFolder;
        private readonly TextBox _txtWrappersFolder;
        private readonly TextBox _txtRunbooksFolder;

        // Default options (available)
        private readonly RichTextBox _txtDefaultModels;
        private readonly RichTextBox _txtDefaultWrappers;
        private readonly RichTextBox _txtDefaultLoops;
        private readonly RichTextBox _txtDefaultRunbooks;

        // Default selected (priority order)
        private readonly RichTextBox _txtSelectedModels;
        private readonly RichTextBox _txtSelectedWrappers;
        private readonly RichTextBox _txtSelectedLoops;
        private readonly RichTextBox _txtSelectedRunbooks;

        // Resolved defaults (read-only display)
        private readonly Label _lblResolvedModel;
        private readonly Label _lblResolvedWrapper;
        private readonly Label _lblResolvedLoop;
        private readonly Label _lblResolvedRunbook;

        // Runtime
        private readonly NumericUpDown _nudMaxIterations;
        private readonly NumericUpDown _nudLogRotation;

        private readonly Label _lblStatus;
        private readonly Button _btnSave;
        private readonly Button _btnRevert;

        private WallyEnvironment? _environment;
        private bool _isDirty;
        private bool _loading;

        public event EventHandler? DirtyChanged;
        public event EventHandler? Saved;

        public bool IsDirty => _isDirty;

        public ConfigEditorPanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            var scroll = ThemedEditorFactory.CreateScrollableSurface();

            var table = ThemedEditorFactory.CreateScrollableFormTable(1);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            int row = 0;

            // Header
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("\u2699 Workspace Configuration", WallyTheme.FontUIBold, WallyTheme.TextPrimary), 0, row++);

            // Action bar
            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 8)
            };
            _btnSave = MakeButton("\uD83D\uDCBE Save");
            _btnSave.Click += OnSave;
            _btnRevert = MakeButton("\u21BA Revert");
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
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(actionBar, 0, row++);

            // ?? Resolved defaults section (read-only) ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("\u2B50 Active Defaults (resolved from selected \u00D7 loaded)", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            void AddResolvedField(string label, out Label lbl)
            {
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(MakeLabel(label), 0, row++);
                lbl = new Label
                {
                    AutoSize = true, Font = WallyTheme.FontUI, ForeColor = WallyTheme.TextPrimary,
                    BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 4), Dock = DockStyle.Top,
                    Text = "(not resolved)"
                };
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(lbl, 0, row++);
            }

            AddResolvedField("Model", out _lblResolvedModel);
            AddResolvedField("Wrapper", out _lblResolvedWrapper);
            AddResolvedField("Loop", out _lblResolvedLoop);
            AddResolvedField("Runbook", out _lblResolvedRunbook);

            // ?? Folder names section ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("\uD83D\uDCC1 Folder Names", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            void AddTextField(string label, out TextBox txt)
            {
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(MakeLabel(label), 0, row++);
                txt = MakeTextBox(); txt.TextChanged += OnFieldChanged;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(txt, 0, row++);
            }

            void AddRichField(string label, int height, out RichTextBox rtb)
            {
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(MakeLabel(label), 0, row++);
                rtb = ThemedEditorFactory.CreateFormTextArea(height, wordWrap: false, backColor: WallyTheme.Surface2); rtb.TextChanged += OnFieldChanged;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.Controls.Add(rtb, 0, row++);
            }

            AddTextField("Actors Folder", out _txtActorsFolder);
            AddTextField("Logs Folder", out _txtLogsFolder);
            AddTextField("Docs Folder", out _txtDocsFolder);
            AddTextField("Templates Folder", out _txtTemplatesFolder);
            AddTextField("Loops Folder", out _txtLoopsFolder);
            AddTextField("Wrappers Folder", out _txtWrappersFolder);
            AddTextField("Runbooks Folder", out _txtRunbooksFolder);

            // ?? Default options section ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("\u2699 Default Options (all available choices)", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            AddRichField("Available Models (one per line)", 80, out _txtDefaultModels);
            AddRichField("Available Wrappers (one per line)", 80, out _txtDefaultWrappers);
            AddRichField("Available Loops (one per line)", 80, out _txtDefaultLoops);
            AddRichField("Available Runbooks (one per line)", 80, out _txtDefaultRunbooks);

            // ?? Default selected section (priority order) ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("\u2B50 Default Selected (priority order \u2014 first loaded wins)", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            AddRichField("Selected Models (one per line, first available = default)", 60, out _txtSelectedModels);
            AddRichField("Selected Wrappers (one per line, first loaded = default)", 60, out _txtSelectedWrappers);
            AddRichField("Selected Loops (one per line, first loaded = default)", 60, out _txtSelectedLoops);
            AddRichField("Selected Runbooks (one per line, first loaded = default)", 60, out _txtSelectedRunbooks);

            // ?? Runtime section ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeSection("\u23F1 Runtime", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Max Iterations"), 0, row++);
            _nudMaxIterations = MakeNumeric(1, 1000);
            _nudMaxIterations.ValueChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_nudMaxIterations, 0, row++);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Log Rotation (minutes, 0 = disabled)"), 0, row++);
            _nudLogRotation = MakeNumeric(0, 120);
            _nudLogRotation.ValueChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_nudLogRotation, 0, row++);

            scroll.Controls.Add(table);
            Controls.Add(scroll);
            ResumeLayout(true);
        }

        // ?? Public API ????????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
        }

        public void LoadConfig()
        {
            if (_environment?.HasWorkspace != true) return;
            _loading = true;
            try
            {
                var cfg = _environment.Workspace!.Config;

                _txtActorsFolder.Text = cfg.ActorsFolderName;
                _txtLogsFolder.Text = cfg.LogsFolderName;
                _txtDocsFolder.Text = cfg.DocsFolderName;
                _txtTemplatesFolder.Text = cfg.TemplatesFolderName;
                _txtLoopsFolder.Text = cfg.LoopsFolderName;
                _txtWrappersFolder.Text = cfg.WrappersFolderName;
                _txtRunbooksFolder.Text = cfg.RunbooksFolderName;

                _txtDefaultModels.Text = string.Join(Environment.NewLine, cfg.DefaultModels);
                _txtDefaultWrappers.Text = string.Join(Environment.NewLine, cfg.DefaultWrappers);
                _txtDefaultLoops.Text = string.Join(Environment.NewLine, cfg.DefaultLoops);
                _txtDefaultRunbooks.Text = string.Join(Environment.NewLine, cfg.DefaultRunbooks);

                _txtSelectedModels.Text = string.Join(Environment.NewLine, cfg.SelectedModels);
                _txtSelectedWrappers.Text = string.Join(Environment.NewLine, cfg.SelectedWrappers);
                _txtSelectedLoops.Text = string.Join(Environment.NewLine, cfg.SelectedLoops);
                _txtSelectedRunbooks.Text = string.Join(Environment.NewLine, cfg.SelectedRunbooks);

                _nudMaxIterations.Value = Math.Clamp(cfg.MaxIterations, 1, 1000);
                _nudLogRotation.Value = Math.Clamp(cfg.LogRotationMinutes, 0, 120);

                // Show resolved defaults (read-only)
                _lblResolvedModel.Text = cfg.DefaultModel ?? "(none)";
                _lblResolvedWrapper.Text = cfg.DefaultWrapper ?? "(none)";
                _lblResolvedLoop.Text = cfg.ResolvedDefaultLoop ?? "(none)";
                _lblResolvedRunbook.Text = cfg.ResolvedDefaultRunbook ?? "(none)";

                SetDirty(false);
                _lblStatus.Text = $"Loaded from: {_environment.WorkspaceFolder}";
                _lblStatus.ForeColor = WallyTheme.TextMuted;
            }
            finally { _loading = false; }
        }

        // ?? Apply to config ???????????????????????????????????????????????????

        private void ApplyFieldsToConfig()
        {
            if (_environment?.HasWorkspace != true) return;
            var cfg = _environment.Workspace!.Config;

            cfg.ActorsFolderName = _txtActorsFolder.Text.Trim();
            cfg.LogsFolderName = _txtLogsFolder.Text.Trim();
            cfg.DocsFolderName = _txtDocsFolder.Text.Trim();
            cfg.TemplatesFolderName = _txtTemplatesFolder.Text.Trim();
            cfg.LoopsFolderName = _txtLoopsFolder.Text.Trim();
            cfg.WrappersFolderName = _txtWrappersFolder.Text.Trim();
            cfg.RunbooksFolderName = _txtRunbooksFolder.Text.Trim();

            cfg.DefaultModels = ParseLines(_txtDefaultModels.Text);
            cfg.DefaultWrappers = ParseLines(_txtDefaultWrappers.Text);
            cfg.DefaultLoops = ParseLines(_txtDefaultLoops.Text);
            cfg.DefaultRunbooks = ParseLines(_txtDefaultRunbooks.Text);

            cfg.SelectedModels = ParseLines(_txtSelectedModels.Text);
            cfg.SelectedWrappers = ParseLines(_txtSelectedWrappers.Text);
            cfg.SelectedLoops = ParseLines(_txtSelectedLoops.Text);
            cfg.SelectedRunbooks = ParseLines(_txtSelectedRunbooks.Text);

            cfg.MaxIterations = (int)_nudMaxIterations.Value;
            cfg.LogRotationMinutes = (int)_nudLogRotation.Value;
        }

        private static List<string> ParseLines(string text)
        {
            var list = new List<string>();
            foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim('\r', ' ', '\t');
                if (!string.IsNullOrEmpty(trimmed))
                    list.Add(trimmed);
            }
            return list;
        }

        // ?? Event handlers ????????????????????????????????????????????????????

        private void OnFieldChanged(object? sender, EventArgs e)
        {
            if (_loading) return;
            SetDirty(true);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_environment?.HasWorkspace != true) return;

            try
            {
                ApplyFieldsToConfig();
                string configPath = System.IO.Path.Combine(_environment.WorkspaceFolder!, WallyHelper.ConfigFileName);
                _environment.Workspace!.Config.SaveToFile(configPath);

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
            LoadConfig();
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

        // ?? Control factories ?????????????????????????????????????????????????

        private static Label MakeSection(string text, Font font, Color color) =>
            new() { Text = text, AutoSize = true, Font = font, ForeColor = color, BackColor = Color.Transparent, Margin = new Padding(0, 8, 0, 4), Dock = DockStyle.Top };

        private static Label MakeLabel(string text) =>
            new() { Text = text, AutoSize = true, Font = WallyTheme.FontUISmallBold, ForeColor = WallyTheme.TextMuted, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 2), Dock = DockStyle.Top };

        private static TextBox MakeTextBox() =>
            new() { Dock = DockStyle.Top, Font = WallyTheme.FontUI, BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 4) };

        private static RichTextBox MakeRichBox(int height) =>
            ThemedEditorFactory.CreateFormTextArea(height, wordWrap: false, backColor: WallyTheme.Surface2);

        private static NumericUpDown MakeNumeric(int min, int max) =>
            new() { Width = 120, Minimum = min, Maximum = max, Font = WallyTheme.FontUI, BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 4) };

        private static Button MakeButton(String text)
        {
            var btn = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = WallyTheme.Surface3, ForeColor = WallyTheme.TextPrimary, Font = WallyTheme.FontUISmallBold, Cursor = Cursors.Hand, Padding = new Padding(8, 2, 8, 2), Margin = new Padding(0, 0, 6, 0) };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }
    }
}
