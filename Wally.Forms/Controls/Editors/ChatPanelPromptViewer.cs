using System;
using System.Drawing;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Controls;
using Wally.Forms.ChatPanelSupport;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Read-only viewer that previews the fully constructed prompt that will
    /// be sent to the LLM provider from the Chat Panel.
    /// <para>
    /// The user selects an actor, loop, model, wrapper, and action mode,
    /// types a sample prompt, and clicks "Build Prompt" to see exactly what
    /// the LLM will receive � including RBA enrichment, loop start prompt
    /// expansion, documentation context, and the resolved CLI command.
    /// </para>
    /// </summary>
    public sealed class ChatPanelPromptViewer : UserControl
    {
        // ?? Controls ?????????????????????????????????????????????????????????

        private readonly ComboBox _cboActor;
        private readonly ComboBox _cboLoop;
        private readonly ComboBox _cboModel;
        private readonly ComboBox _cboWrapper;
        private readonly ComboBox _cboMode;
        private readonly RichTextBox _txtUserPrompt;
        private readonly RichTextBox _txtOutput;
        private readonly RichTextBox _txtExactPrompt;
        private readonly Button _btnBuild;
        private readonly Button _btnCopy;
        private readonly Button _btnCopyExact;
        private readonly Label _lblStatus;

        // ?? State ?????????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;

        // ?? Constructor ???????????????????????????????????????????????????????

        public ChatPanelPromptViewer()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            // Scrollable wrapper for the top form area.
            var scroll = ThemedEditorFactory.CreateScrollableSurface();

            var table = ThemedEditorFactory.CreateScrollableFormTable(2);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            int row = 0;

            // ?? Header ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblTitle = CreateSectionLabel(
                "\uD83D\uDD0D Prompt Viewer", WallyTheme.FontUIBold, WallyTheme.TextPrimary);
            table.Controls.Add(lblTitle, 0, row);
            table.SetColumnSpan(lblTitle, 2);
            row++;

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblDesc = CreateSectionLabel(
                "Preview the fully constructed prompt that the Chat Panel would send to the LLM provider.",
                WallyTheme.FontUISmall, WallyTheme.TextMuted);
            table.Controls.Add(lblDesc, 0, row);
            table.SetColumnSpan(lblDesc, 2);
            row++;

            // ?? Action bar ??
            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 8)
            };

            _btnBuild = CreateButton("\u25B6 Build Prompt");
            _btnBuild.Click += (_, _) => BuildPromptPreview();

            _btnCopy = CreateButton("\uD83D\uDCCB Copy Details");
            _btnCopy.Click += OnCopyDetailsClick;

            _btnCopyExact = CreateButton("\uD83E\uDDFE Copy Exact Prompt");
            _btnCopyExact.Click += OnCopyExactPromptClick;

            _lblStatus = new Label
            {
                Text = "",
                AutoSize = true,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 6, 0, 0)
            };

            actionBar.Controls.Add(_btnBuild);
            actionBar.Controls.Add(_btnCopy);
            actionBar.Controls.Add(_btnCopyExact);
            actionBar.Controls.Add(_lblStatus);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(actionBar, 0, row);
            table.SetColumnSpan(actionBar, 2);
            row++;

            // ?? Mode selector ??
            _cboMode = CreateComboBox();
            _cboMode.Items.AddRange(new object[] { "Ask", "Agent" });
            _cboMode.SelectedIndex = 0;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Mode"), 0, row);
            table.Controls.Add(_cboMode, 1, row);
            row++;

            // ?? Actor selector ??
            _cboActor = CreateComboBox();
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Actor"), 0, row);
            table.Controls.Add(_cboActor, 1, row);
            row++;

            // ?? Loop selector ??
            _cboLoop = CreateComboBox();
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Loop"), 0, row);
            table.Controls.Add(_cboLoop, 1, row);
            row++;

            // ?? Model selector ??
            _cboModel = CreateComboBox();
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Model"), 0, row);
            table.Controls.Add(_cboModel, 1, row);
            row++;

            // ?? Wrapper selector ??
            _cboWrapper = CreateComboBox();
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Wrapper"), 0, row);
            table.Controls.Add(_cboWrapper, 1, row);
            row++;

            // ?? User prompt input ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblPrompt = CreateFieldLabel("User Prompt");
            table.Controls.Add(lblPrompt, 0, row);
            table.SetColumnSpan(lblPrompt, 2);
            row++;

            _txtUserPrompt = ThemedEditorFactory.CreateFormTextArea(
                80,
                wordWrap: true,
                backColor: WallyTheme.Surface2,
                margin: new Padding(0, 0, 0, 8));
            _txtUserPrompt.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BuildPromptPreview();
                }
            };

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtUserPrompt, 0, row);
            table.SetColumnSpan(_txtUserPrompt, 2);
            row++;

            // ?? Output section header ??
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblOutput = CreateSectionLabel(
                "\uD83D\uDD2C Prompt Construction Details", WallyTheme.FontUIBold, WallyTheme.TextSecondary);
            table.Controls.Add(lblOutput, 0, row);
            table.SetColumnSpan(lblOutput, 2);
            row++;

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblOutputHelp = CreateSectionLabel(
                "Review each stage of prompt assembly below. The exact first prompt sent to the model is shown in its own section at the bottom.",
                WallyTheme.FontUISmall, WallyTheme.TextMuted);
            table.Controls.Add(lblOutputHelp, 0, row);
            table.SetColumnSpan(lblOutputHelp, 2);
            row++;

            // ?? Output display ??
            _txtOutput = ThemedEditorFactory.CreateFormTextArea(
                200,
                wordWrap: true,
                readOnly: true,
                backColor: WallyTheme.Surface1,
                margin: new Padding(0, 0, 0, 8));

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtOutput, 0, row);
            table.SetColumnSpan(_txtOutput, 2);
            row++;

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblExactPrompt = CreateSectionLabel(
                "\uD83E\uDDFE Exact Prompt Sent To The LLM", WallyTheme.FontUIBold, WallyTheme.TextPrimary);
            table.Controls.Add(lblExactPrompt, 0, row);
            table.SetColumnSpan(lblExactPrompt, 2);
            row++;

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblExactPromptHelp = CreateSectionLabel(
                "This is the exact text used for the first LLM request. If a loop is selected, later iterations use the continue prompt template shown above.",
                WallyTheme.FontUISmall, WallyTheme.TextMuted);
            table.Controls.Add(lblExactPromptHelp, 0, row);
            table.SetColumnSpan(lblExactPromptHelp, 2);
            row++;

            _txtExactPrompt = ThemedEditorFactory.CreateFormTextArea(
                160,
                wordWrap: true,
                readOnly: true,
                backColor: WallyTheme.Surface2,
                detectUrls: false,
                margin: new Padding(0, 0, 0, 4));

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtExactPrompt, 0, row);
            table.SetColumnSpan(_txtExactPrompt, 2);
            row++;

            scroll.Controls.Add(table);
            Controls.Add(scroll);

            ResumeLayout(true);
        }

        // ?? Public API ????????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
            RefreshSelectors();
        }

        internal void LoadPreview(ChatPanelPromptPreview preview)
        {
            ArgumentNullException.ThrowIfNull(preview);

            ApplyRequest(preview.Request);
            RenderPreview(preview);
            _lblStatus.Text = $"Loaded preview at {DateTime.Now:HH:mm:ss}";
            _lblStatus.ForeColor = WallyTheme.Green;
        }

        public void RefreshSelectors()
        {
            if (_environment?.HasWorkspace != true) return;

            _cboActor.Items.Clear();
            _cboActor.Items.Add("(none � direct prompt)");
            foreach (var actor in _environment.Actors)
                _cboActor.Items.Add(actor.Name);
            _cboActor.SelectedIndex = 0;

            _cboLoop.Items.Clear();
            _cboLoop.Items.Add("(none � single run)");
            foreach (var loop in _environment.Loops)
                _cboLoop.Items.Add(loop.Name);
            _cboLoop.SelectedIndex = 0;

            _cboModel.Items.Clear();
            _cboModel.Items.Add("(workspace default)");
            var cfg = _environment.Workspace!.Config;
            foreach (var model in cfg.DefaultModels)
                _cboModel.Items.Add(model);
            _cboModel.SelectedIndex = 0;

            _cboWrapper.Items.Clear();
            _cboWrapper.Items.Add("(auto � based on mode)");
            foreach (var wrapper in _environment.Workspace.LlmWrappers)
                _cboWrapper.Items.Add(wrapper.Name);
            _cboWrapper.SelectedIndex = 0;
        }

        // ?? Build prompt preview ??????????????????????????????????????????????

        private void BuildPromptPreview()
        {
            if (_environment?.HasWorkspace != true)
            {
                RenderError("No workspace loaded.");
                return;
            }

            try
            {
                ChatPanelRequest request = BuildRequestFromControls();
                if (string.IsNullOrWhiteSpace(request.DisplayPrompt))
                {
                    RenderError("Enter a user prompt above to preview.");
                    return;
                }

                RenderPreview(ChatPanelExecutionService.BuildPromptPreview(_environment, request));
                _lblStatus.Text = $"Built at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
            }
            catch (Exception ex)
            {
                RenderError(ex.Message);
            }
        }

        private ChatPanelRequest BuildRequestFromControls()
        {
            return new ChatPanelRequest
            {
                Prompt = _txtUserPrompt.Text.Trim(),
                ActorName = _cboActor.SelectedIndex > 0 ? _cboActor.SelectedItem?.ToString() : null,
                LoopName = _cboLoop.SelectedIndex > 0 ? _cboLoop.SelectedItem?.ToString() : null,
                ModelOverride = _cboModel.SelectedIndex > 0 ? _cboModel.SelectedItem?.ToString() : null,
                WrapperName = _cboWrapper.SelectedIndex > 0 ? _cboWrapper.SelectedItem?.ToString() : null,
                Mode = string.Equals(_cboMode.SelectedItem?.ToString(), "Agent", StringComparison.OrdinalIgnoreCase)
                    ? ChatPanelExecutionMode.Agent
                    : ChatPanelExecutionMode.Ask
            };
        }

        private void ApplyRequest(ChatPanelRequest request)
        {
            _txtUserPrompt.Text = request.Prompt;
            _cboMode.SelectedItem = request.Mode == ChatPanelExecutionMode.Agent ? "Agent" : "Ask";
            SetComboSelection(_cboActor, request.ActorName, 0);
            SetComboSelection(_cboLoop, request.LoopName, 0);
            SetComboSelection(_cboModel, request.ModelOverride, 0);
            SetComboSelection(_cboWrapper, request.WrapperName, 0);
        }

        private void RenderPreview(ChatPanelPromptPreview preview)
        {
            _txtOutput.Clear();
            _txtExactPrompt.Clear();

            foreach (var section in preview.Sections)
                AppendSection(section.Heading, section.Body, WallyTheme.TextSecondary);

            _txtExactPrompt.Text = preview.ExactPrompt;
        }

        private void RenderError(string message)
        {
            _txtOutput.Clear();
            _txtExactPrompt.Clear();
            AppendSection("Error", message, WallyTheme.Red);
            _lblStatus.Text = "Build failed.";
            _lblStatus.ForeColor = WallyTheme.Red;
        }

        private void OnCopyDetailsClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtOutput.Text))
                return;

            Clipboard.SetText(_txtOutput.Text);
            _lblStatus.Text = "Prompt details copied.";
            _lblStatus.ForeColor = WallyTheme.Green;
        }

        private void OnCopyExactPromptClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtExactPrompt.Text))
                return;

            Clipboard.SetText(_txtExactPrompt.Text);
            _lblStatus.Text = "Exact prompt copied.";
            _lblStatus.ForeColor = WallyTheme.Green;
        }

        private static void SetComboSelection(ComboBox comboBox, string? value, int defaultIndex)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                comboBox.SelectedIndex = defaultIndex;
                return;
            }

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (string.Equals(comboBox.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }

            comboBox.SelectedIndex = defaultIndex;
        }

        // ?? Output helpers ????????????????????????????????????????????????????

        private void AppendSection(string heading, string body, Color headingColor)
        {
            _txtOutput.SelectionStart = _txtOutput.TextLength;
            _txtOutput.SelectionLength = 0;
            _txtOutput.SelectionColor = headingColor;
            _txtOutput.SelectionFont = WallyTheme.FontUISmallBold;
            _txtOutput.AppendText($"??? {heading} ");
            _txtOutput.SelectionColor = WallyTheme.Border;
            _txtOutput.AppendText(new string('?', Math.Max(0, 60 - heading.Length)));
            _txtOutput.AppendText(Environment.NewLine);

            _txtOutput.SelectionStart = _txtOutput.TextLength;
            _txtOutput.SelectionLength = 0;
            _txtOutput.SelectionColor = WallyTheme.TextPrimary;
            _txtOutput.SelectionFont = WallyTheme.FontMono;
            _txtOutput.AppendText(body);
            _txtOutput.AppendText(Environment.NewLine + Environment.NewLine);
        }

        // ?? Control factories ?????????????????????????????????????????????????

        private static Label CreateSectionLabel(string text, Font font, Color color) =>
            new()
            {
                Text = text,
                AutoSize = true,
                Font = font,
                ForeColor = color,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 4),
                Dock = DockStyle.Top
            };

        private static Label CreateFieldLabel(string text) =>
            new()
            {
                Text = text,
                AutoSize = true,
                Font = WallyTheme.FontUISmallBold,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 2),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft
            };

        private static ComboBox CreateComboBox() =>
            new()
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = WallyTheme.FontUI,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                FlatStyle = FlatStyle.Standard,
                Margin = new Padding(0, 0, 0, 4)
            };

        private static Button CreateButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3,
                ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUISmallBold,
                Cursor = Cursors.Hand,
                Padding = new Padding(8, 2, 8, 2),
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }
    }
}
