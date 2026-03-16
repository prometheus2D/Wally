using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Core.Logging;
using Wally.Core.Providers;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Read-only viewer that previews the fully constructed prompt that will
    /// be sent to the LLM provider from the Chat Panel.
    /// <para>
    /// The user selects an actor, loop, model, wrapper, and action mode,
    /// types a sample prompt, and clicks "Build Prompt" to see exactly what
    /// the LLM will receive — including RBA enrichment, loop start prompt
    /// expansion, documentation context, and the resolved CLI command.
    /// </para>
    /// </summary>
    public sealed class ChatPanelPromptViewer : UserControl
    {
        // ?? Controls ????????????????????????????????????????????????????????

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

        // ?? State ???????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;

        // ?? Constructor ?????????????????????????????????????????????????????

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
            _btnCopy.Click += (_, _) =>
            {
                if (!string.IsNullOrEmpty(_txtOutput.Text))
                {
                    Clipboard.SetText(_txtOutput.Text);
                    _lblStatus.Text = "Prompt details copied.";
                    _lblStatus.ForeColor = WallyTheme.Green;
                }
            };

            _btnCopyExact = CreateButton("\uD83E\uDDFE Copy Exact Prompt");
            _btnCopyExact.Click += (_, _) =>
            {
                if (!string.IsNullOrEmpty(_txtExactPrompt.Text))
                {
                    Clipboard.SetText(_txtExactPrompt.Text);
                    _lblStatus.Text = "Exact prompt copied.";
                    _lblStatus.ForeColor = WallyTheme.Green;
                }
            };

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

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
            RefreshSelectors();
        }

        /// <summary>
        /// Reloads actors, loops, models, and wrappers from the current environment.
        /// </summary>
        public void RefreshSelectors()
        {
            if (_environment?.HasWorkspace != true) return;

            // Actor
            _cboActor.Items.Clear();
            _cboActor.Items.Add("(none — direct prompt)");
            foreach (var actor in _environment.Actors)
                _cboActor.Items.Add(actor.Name);
            _cboActor.SelectedIndex = 0;

            // Loop
            _cboLoop.Items.Clear();
            _cboLoop.Items.Add("(none — single run)");
            foreach (var loop in _environment.Loops)
                _cboLoop.Items.Add(loop.Name);
            _cboLoop.SelectedIndex = 0;

            // Model
            _cboModel.Items.Clear();
            _cboModel.Items.Add("(workspace default)");
            var cfg = _environment.Workspace!.Config;
            foreach (var model in cfg.DefaultModels)
                _cboModel.Items.Add(model);
            _cboModel.SelectedIndex = 0;

            // Wrapper
            _cboWrapper.Items.Clear();
            _cboWrapper.Items.Add("(auto — based on mode)");
            foreach (var wrapper in _environment.Workspace.LlmWrappers)
                _cboWrapper.Items.Add(wrapper.Name);
            _cboWrapper.SelectedIndex = 0;
        }

        // ?? Build prompt preview ????????????????????????????????????????????

        private void BuildPromptPreview()
        {
            _txtOutput.Clear();
            _txtExactPrompt.Clear();

            if (_environment?.HasWorkspace != true)
            {
                AppendSection("Error", "No workspace loaded.", WallyTheme.Red);
                return;
            }

            string userPrompt = _txtUserPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userPrompt))
            {
                AppendSection("Error", "Enter a user prompt above to preview.", WallyTheme.Red);
                return;
            }

            try
            {
                // ?? Resolve selections ??
                string? actorName = _cboActor.SelectedIndex > 0
                    ? _cboActor.SelectedItem?.ToString()
                    : null;
                bool directMode = string.IsNullOrEmpty(actorName);

                string? loopName = _cboLoop.SelectedIndex > 0
                    ? _cboLoop.SelectedItem?.ToString()
                    : null;
                bool isLooped = !string.IsNullOrEmpty(loopName);

                string? modelOverride = _cboModel.SelectedIndex > 0
                    ? _cboModel.SelectedItem?.ToString()
                    : null;

                string modeText = _cboMode.SelectedItem?.ToString() ?? "Ask";
                bool isAgent = string.Equals(modeText, "Agent", StringComparison.OrdinalIgnoreCase);
                string? exactPromptToSend = null;

                string? wrapperName = null;
                if (_cboWrapper.SelectedIndex > 0)
                {
                    wrapperName = _cboWrapper.SelectedItem?.ToString();
                }
                else
                {
                    var wrappers = _environment.Workspace!.LlmWrappers;
                    var match = wrappers.FirstOrDefault(w => w.CanMakeChanges == isAgent);
                    wrapperName = match?.Name ?? (wrappers.Count > 0 ? wrappers[0].Name : null);
                }

                // ?? Section 1: Selections summary ??
                var summaryBuilder = new StringBuilder();
                summaryBuilder.AppendLine($"Mode:    {modeText}");
                summaryBuilder.AppendLine($"Actor:   {(directMode ? "(none — direct prompt)" : actorName)}");
                summaryBuilder.AppendLine($"Loop:    {(isLooped ? loopName : "(none — single run)")}");
                summaryBuilder.AppendLine($"Model:   {modelOverride ?? _environment.Workspace!.Config.DefaultModel ?? "(none)"}");
                summaryBuilder.AppendLine($"Wrapper: {wrapperName ?? "(none)"}");
                AppendSection("Resolved Selections", summaryBuilder.ToString().TrimEnd(), WallyTheme.TextSecondary);

                // ?? Section 2: Equivalent CLI command ??
                var cmdParts = new List<string> { "run", $"\"{userPrompt}\"" };
                if (!directMode) cmdParts.Add($"-a {actorName}");
                if (isLooped) cmdParts.Add($"-l {loopName}");
                if (modelOverride != null) cmdParts.Add($"-m {modelOverride}");
                if (wrapperName != null) cmdParts.Add($"-w {wrapperName}");
                string cliCommand = string.Join(" ", cmdParts);
                AppendSection("Equivalent CLI Command", cliCommand, WallyTheme.TextMuted);

                // ?? Section 3: Loop start prompt (if applicable) ??
                string effectivePrompt = userPrompt;
                if (isLooped)
                {
                    var loopDef = _environment.GetLoop(loopName!);
                    if (loopDef != null && !string.IsNullOrWhiteSpace(loopDef.StartPrompt))
                    {
                        effectivePrompt = loopDef.StartPrompt.Replace("{userPrompt}", userPrompt);
                        AppendSection("Loop Start Prompt (expanded)",
                            effectivePrompt, WallyTheme.TextSecondary);

                        // Also show the continue prompt template
                        if (!string.IsNullOrWhiteSpace(loopDef.ContinuePromptTemplate))
                        {
                            string continuePreview = loopDef.ContinuePromptTemplate
                                .Replace("{userPrompt}", userPrompt)
                                .Replace("{previousResult}", "<previous AI response>")
                                .Replace("{completedKeyword}", loopDef.ResolvedCompletedKeyword)
                                .Replace("{errorKeyword}", loopDef.ResolvedErrorKeyword);
                            AppendSection("Loop Continue Prompt Template (preview)",
                                continuePreview, WallyTheme.TextMuted);
                        }

                        AppendSection("Loop Settings",
                            $"MaxIterations:    {(loopDef.MaxIterations > 0 ? loopDef.MaxIterations.ToString() : "(workspace default)")}\n" +
                            $"CompletedKeyword: {loopDef.ResolvedCompletedKeyword}\n" +
                            $"ErrorKeyword:     {loopDef.ResolvedErrorKeyword}",
                            WallyTheme.TextMuted);
                    }
                    else
                    {
                        AppendSection("Loop Warning",
                            $"Loop '{loopName}' not found or has no start prompt.",
                            WallyTheme.Red);
                    }
                }

                // — Section 4: Conversation history injection preview —
                string? historyBlock = null;
                bool wrapperUsesHistory = true;
                if (wrapperName != null)
                {
                    var resolvedWrapper = _environment.Workspace!.LlmWrappers
                        .FirstOrDefault(w => string.Equals(w.Name, wrapperName, StringComparison.OrdinalIgnoreCase));
                    if (resolvedWrapper != null)
                        wrapperUsesHistory = resolvedWrapper.UseConversationHistory;
                }

                if (wrapperUsesHistory)
                {
                    string? actorFilter = directMode ? null : actorName;
                    var recentTurns = _environment.History.GetRecentTurns(
                        ConversationLogger.MaxInjectedTurns, actorFilter);

                    if (recentTurns.Count > 0)
                    {
                        historyBlock = ConversationLogger.FormatHistoryBlock(recentTurns);
                        AppendSection(
                            $"Conversation History ({recentTurns.Count} turn(s) — same-actor filter: {actorFilter ?? "(direct mode)"})",
                            historyBlock ?? "(empty)", WallyTheme.TextSecondary);
                    }
                    else
                    {
                        AppendSection("Conversation History", "(no matching history turns)", WallyTheme.TextMuted);
                    }
                }
                else
                {
                    AppendSection("Conversation History",
                        $"Disabled for wrapper '{wrapperName}' (UseConversationHistory = false).",
                        WallyTheme.TextMuted);
                }

                // — Section 5: Actor-enriched prompt (ProcessPrompt) —
                if (!directMode)
                {
                    var actor = _environment.GetActor(actorName!);
                    if (actor != null)
                    {
                        string processedPrompt = actor.ProcessPrompt(effectivePrompt, historyBlock);
                        exactPromptToSend = processedPrompt;
                        AppendSection(
                            $"Actor-Enriched Prompt (Actor.ProcessPrompt — {actor.Name})",
                            processedPrompt, WallyTheme.TextPrimary);
                    }
                    else
                    {
                        AppendSection("Actor Error",
                            $"Actor '{actorName}' not found.", WallyTheme.Red);
                    }
                }
                else
                {
                    // Direct mode — history block is prepended to the prompt.
                    string finalPrompt = historyBlock != null
                        ? historyBlock + "\n" + effectivePrompt
                        : effectivePrompt;
                    exactPromptToSend = finalPrompt;
                    AppendSection("Final Prompt (direct mode — no actor enrichment)",
                        finalPrompt, WallyTheme.TextPrimary);
                }

                // — Section 6: Wrapper CLI command preview —
                if (wrapperName != null)
                {
                    var wrapper = _environment.Workspace!.LlmWrappers
                        .FirstOrDefault(w => string.Equals(w.Name, wrapperName, StringComparison.OrdinalIgnoreCase));
                    if (wrapper != null)
                    {
                        string resolvedModel = modelOverride
                            ?? _environment.Workspace.Config.DefaultModel
                            ?? "";
                        string sourcePath = _environment.SourcePath ?? "";

                        var cliPreview = new StringBuilder();
                        cliPreview.AppendLine($"Executable:             {wrapper.Executable}");
                        cliPreview.AppendLine($"Template:               {wrapper.ArgumentTemplate}");
                        cliPreview.AppendLine($"CanMakeChanges:         {wrapper.CanMakeChanges}");
                        cliPreview.AppendLine($"UseConversationHistory: {wrapper.UseConversationHistory}");
                        cliPreview.AppendLine($"Model:                  {(string.IsNullOrEmpty(resolvedModel) ? "(none)" : resolvedModel)}");
                        cliPreview.AppendLine($"SourcePath:             {(string.IsNullOrEmpty(sourcePath) ? "(none)" : sourcePath)}");

                        if (!string.IsNullOrWhiteSpace(wrapper.ModelArgFormat) && !string.IsNullOrEmpty(resolvedModel))
                            cliPreview.AppendLine($"ModelArgs:              {wrapper.ModelArgFormat.Replace("{model}", resolvedModel)}");
                        if (!string.IsNullOrWhiteSpace(wrapper.SourcePathArgFormat) && !string.IsNullOrEmpty(sourcePath))
                            cliPreview.AppendLine($"SourcePathArgs:         {wrapper.SourcePathArgFormat.Replace("{sourcePath}", sourcePath)}");

                        AppendSection($"Wrapper: {wrapper.Name}", cliPreview.ToString().TrimEnd(), WallyTheme.TextMuted);
                    }
                }

                if (!string.IsNullOrWhiteSpace(exactPromptToSend))
                    _txtExactPrompt.Text = exactPromptToSend;

                _lblStatus.Text = $"Built at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
            }
            catch (Exception ex)
            {
                AppendSection("Error", ex.Message, WallyTheme.Red);
                _lblStatus.Text = "Build failed.";
                _lblStatus.ForeColor = WallyTheme.Red;
            }
        }

        // ?? Output helpers ??????????????????????????????????????????????????

        private void AppendSection(string heading, string body, Color headingColor)
        {
            // Heading
            _txtOutput.SelectionStart = _txtOutput.TextLength;
            _txtOutput.SelectionLength = 0;
            _txtOutput.SelectionColor = headingColor;
            _txtOutput.SelectionFont = WallyTheme.FontUISmallBold;
            _txtOutput.AppendText($"?? {heading} ");
            _txtOutput.SelectionColor = WallyTheme.Border;
            _txtOutput.AppendText(new string('?', Math.Max(0, 60 - heading.Length)));
            _txtOutput.AppendText(Environment.NewLine);

            // Body
            _txtOutput.SelectionStart = _txtOutput.TextLength;
            _txtOutput.SelectionLength = 0;
            _txtOutput.SelectionColor = WallyTheme.TextPrimary;
            _txtOutput.SelectionFont = WallyTheme.FontMono;
            _txtOutput.AppendText(body);
            _txtOutput.AppendText(Environment.NewLine + Environment.NewLine);
        }

        // ?? Control factories ???????????????????????????????????????????????

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
