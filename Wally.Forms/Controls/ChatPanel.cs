using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// Describes the kind of chat message for styling purposes.
    /// </summary>
    internal enum MessageKind { User, Actor, Error }

    /// <summary>
    /// The execution mode for the chat panel.
    /// </summary>
    internal enum ChatMode
    {
        /// <summary>Single-shot: one prompt ? one actor response.</summary>
        Ask,
        /// <summary>Iterative loop: runs the actor repeatedly until completion or max iterations.</summary>
        Agent,
        /// <summary>Runs ALL actors sequentially on the same prompt.</summary>
        Autopilot
    }

    /// <summary>
    /// AI chat panel — right-side copilot conversation window.
    /// Supports three execution modes (Ask, Agent, Autopilot),
    /// actor/model selection, cancellation, workspace-gated input,
    /// and asynchronous execution.
    /// </summary>
    public sealed class ChatPanel : UserControl
    {
        // ?? Header ??????????????????????????????????????????????????????????

        private readonly Panel _header;
        private readonly Label _lblTitle;

        // ?? Mode selector (segmented buttons) ???????????????????????????????

        private readonly Panel _modeBar;
        private readonly Button _btnModeAsk;
        private readonly Button _btnModeAgent;
        private readonly Button _btnModeAutopilot;
        private readonly Label _lblModeHint;

        // ?? Toolbar ?????????????????????????????????????????????????????????

        private readonly ToolStrip _toolbar;
        private readonly ToolStripComboBox _cboActor;
        private readonly ToolStripComboBox _cboModel;
        private readonly ToolStripButton _btnClear;

        // ?? Conversation area ???????????????????????????????????????????????

        private readonly Panel _messagesContainer;
        private readonly FlowLayoutPanel _messagesFlow;
        private readonly Label _lblEmptyState;

        // ?? Input area ??????????????????????????????????????????????????????

        private readonly Panel _inputArea;
        private readonly Panel _inputBorder;
        private readonly RichTextBox _txtInput;
        private readonly Button _btnSend;
        private readonly Button _btnCancel;
        private readonly Label _lblStatus;
        private readonly Label _lblHint;

        // ?? State ???????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private bool _workspaceLoaded;
        private ChatMode _currentMode = ChatMode.Ask;

        // ?? Mode colors ?????????????????????????????????????????????????????

        private static readonly Color ModeAskColor = WallyTheme.TextPrimary;
        private static readonly Color ModeAgentColor = WallyTheme.TextPrimary;
        private static readonly Color ModeAutopilotColor = WallyTheme.TextPrimary;

        // ?? Events ??????????????????????????????????????????????????????????

        public event EventHandler<string>? CommandIssued;

        // ?? Constructor ?????????????????????????????????????????????????????

        public ChatPanel()
        {
            SuspendLayout();

            var renderer = WallyTheme.CreateRenderer();

            // ?? Header ??
            _lblTitle = new Label
            {
                Text = "AI CHAT",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = WallyTheme.Surface2
            };
            _header.Controls.Add(_lblTitle);

            // ?? Mode selector bar ??
            _btnModeAsk = CreateModeButton("\uD83D\uDCAC Ask", ModeAskColor);
            _btnModeAgent = CreateModeButton("\uD83E\uDD16 Agent", ModeAgentColor);
            _btnModeAutopilot = CreateModeButton("\u26A1 Autopilot", ModeAutopilotColor);

            _btnModeAsk.Click += (_, _) => SetMode(ChatMode.Ask);
            _btnModeAgent.Click += (_, _) => SetMode(ChatMode.Agent);
            _btnModeAutopilot.Click += (_, _) => SetMode(ChatMode.Autopilot);

            _lblModeHint = new Label
            {
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            var modeButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(6, 4, 0, 4),
                Margin = Padding.Empty
            };
            modeButtonPanel.Controls.Add(_btnModeAsk);
            modeButtonPanel.Controls.Add(_btnModeAgent);
            modeButtonPanel.Controls.Add(_btnModeAutopilot);

            _modeBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = WallyTheme.Surface1,
                Padding = Padding.Empty
            };
            _modeBar.Controls.Add(_lblModeHint);
            _modeBar.Controls.Add(modeButtonPanel);

            // ?? Toolbar ??
            _cboActor = new ToolStripComboBox("cboActor")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                ToolTipText = "Select actor"
            };
            _cboActor.ComboBox.Width = 130;

            _cboModel = new ToolStripComboBox("cboModel")
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                ToolTipText = "Model override (blank = default)"
            };
            _cboModel.ComboBox.Width = 150;

            _btnClear = new ToolStripButton("\u2715 Clear")
            {
                ToolTipText = "Clear conversation",
                ForeColor = WallyTheme.TextSecondary
            };
            _btnClear.Click += (_, _) => ClearMessages();

            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = renderer,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                Padding = new Padding(4, 0, 4, 0)
            };
            _toolbar.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripLabel("Actor") { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _cboActor,
                new ToolStripSeparator(),
                new ToolStripLabel("Model") { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _cboModel,
                new ToolStripSeparator(),
                _btnClear
            });

            // Apply dark styling to combo boxes.
            WallyTheme.StyleComboBox(_cboActor);
            WallyTheme.StyleComboBox(_cboModel);

            // ?? Empty state placeholder ??
            _lblEmptyState = new Label
            {
                Text = "\U0001F4AC\n\nSelect an actor and type a message\nto start a conversation.\n\n" +
                       "Your messages are sent to the actor\u2019s AI pipeline\nand responses appear here as chat bubbles.",
                Dock = DockStyle.Fill,
                ForeColor = WallyTheme.TextDisabled,
                BackColor = WallyTheme.Surface0,
                Font = WallyTheme.FontUI,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            // ?? Message flow (auto-sized, lives inside scrollable container) ??
            _messagesFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12, 12, 12, 12),
                BackColor = WallyTheme.Surface0
            };

            // ?? Scrollable message container ??
            _messagesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = WallyTheme.Surface0
            };
            _messagesContainer.Controls.Add(_messagesFlow);
            _messagesContainer.Controls.Add(_lblEmptyState);
            _messagesContainer.Controls.SetChildIndex(_lblEmptyState, 1);
            _messagesContainer.Controls.SetChildIndex(_messagesFlow, 0);
            _messagesContainer.Resize += OnMessagesResize;

            // ?? Status label ??
            _lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                Text = "  Ready",
                ForeColor = WallyTheme.TextMuted,
                BackColor = WallyTheme.Surface2,
                Font = WallyTheme.FontUISmall,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ?? Input text box ??
            _txtInput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontUI,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                AcceptsTab = false,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };
            _txtInput.KeyDown += OnInputKeyDown;
            _txtInput.GotFocus += (_, _) => _inputBorder.BackColor = WallyTheme.BorderFocused;
            _txtInput.LostFocus += (_, _) => _inputBorder.BackColor = WallyTheme.Border;

            _btnSend = CreateActionButton("Send  \u23CE", WallyTheme.Surface3);
            _btnSend.Click += OnSendClick;

            _btnCancel = CreateActionButton("Stop  \u25A0", WallyTheme.Surface3);
            _btnCancel.Visible = false;
            _btnCancel.Click += (_, _) => _cts?.Cancel();

            _lblHint = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 16,
                Text = "Enter to send \u00B7 Shift+Enter for new line \u00B7 Esc to cancel",
                ForeColor = WallyTheme.TextDisabled,
                BackColor = WallyTheme.Surface1,
                Font = new Font("Segoe UI", 7.5f),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _inputBorder = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(1),
                BackColor = WallyTheme.Border
            };
            _inputBorder.Controls.Add(_txtInput);

            var inputContent = new Panel { Dock = DockStyle.Fill };
            inputContent.Controls.Add(_inputBorder);
            inputContent.Controls.Add(_btnCancel);
            inputContent.Controls.Add(_btnSend);

            _inputArea = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                Padding = new Padding(12, 8, 12, 4),
                BackColor = WallyTheme.Surface1
            };
            _inputArea.Controls.Add(inputContent);
            _inputArea.Controls.Add(_lblHint);

            // ?? Assembly (order: fill first, then docked edges) ??
            Controls.Add(_messagesContainer);   // Fill
            Controls.Add(_lblStatus);            // Bottom
            Controls.Add(_inputArea);            // Bottom
            Controls.Add(_toolbar);              // Top
            Controls.Add(_modeBar);              // Top (below header, above toolbar)
            Controls.Add(_header);               // Top (topmost)

            BackColor = WallyTheme.Surface0;
            ForeColor = WallyTheme.TextPrimary;

            ResumeLayout(true);

            // ?? Initial state ??
            SetMode(ChatMode.Ask);
            SetWorkspaceLoaded(false);
        }

        // ?? Mode button factory ?????????????????????????????????????????????

        private static Button CreateModeButton(string text, Color accentColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 90,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextSecondary,
                Font = WallyTheme.FontUISmallBold,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface3;
            btn.Tag = accentColor;  // Store the accent color for SetMode styling
            return btn;
        }

        private static Button CreateActionButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Right,
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUIBold,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }

        // ?? Mode management ?????????????????????????????????????????????????

        private void SetMode(ChatMode mode)
        {
            _currentMode = mode;

            // Reset all mode buttons to inactive.
            foreach (var btn in new[] { _btnModeAsk, _btnModeAgent, _btnModeAutopilot })
            {
                btn.BackColor = WallyTheme.Surface2;
                btn.ForeColor = WallyTheme.TextSecondary;
                btn.FlatAppearance.BorderColor = WallyTheme.Border;
            }

            // Activate the selected button.
            Button active = mode switch
            {
                ChatMode.Ask => _btnModeAsk,
                ChatMode.Agent => _btnModeAgent,
                ChatMode.Autopilot => _btnModeAutopilot,
                _ => _btnModeAsk
            };
            active.BackColor = WallyTheme.Surface4;
            active.ForeColor = WallyTheme.TextPrimary;
            active.FlatAppearance.BorderColor = WallyTheme.TextMuted;

            // Update hint text.
            _lblModeHint.Text = mode switch
            {
                ChatMode.Ask => "Single response",
                ChatMode.Agent => "Iterative loop",
                ChatMode.Autopilot => "All actors",
                _ => ""
            };
            _lblModeHint.ForeColor = WallyTheme.TextMuted;

            // Update send button color to match mode.
            if (!_isRunning && _workspaceLoaded)
                _btnSend.BackColor = WallyTheme.Surface3;

            // Actor selector is disabled in Autopilot (all actors run).
            _cboActor.Enabled = mode != ChatMode.Autopilot && _workspaceLoaded && !_isRunning;

            // Update header.
            _lblTitle.Text = mode switch
            {
                ChatMode.Ask => "AI CHAT \u2014 Ask",
                ChatMode.Agent => "AI CHAT \u2014 Agent",
                ChatMode.Autopilot => "AI CHAT \u2014 Autopilot",
                _ => "AI CHAT"
            };
        }

        private Color GetModeAccentColor() => WallyTheme.TextSecondary;

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment environment)
        {
            _environment = environment;
            RefreshActorList();
            RefreshModelList();
        }

        /// <summary>
        /// Called by the main form to enable or disable workspace-dependent controls.
        /// </summary>
        public void SetWorkspaceLoaded(bool loaded)
        {
            if (InvokeRequired) { Invoke(() => SetWorkspaceLoaded(loaded)); return; }

            _workspaceLoaded = loaded;

            _cboActor.Enabled = loaded && !_isRunning && _currentMode != ChatMode.Autopilot;
            _cboModel.Enabled = loaded && !_isRunning;
            _btnClear.Enabled = loaded;
            _btnSend.Enabled = loaded && !_isRunning;
            _txtInput.ReadOnly = !loaded || _isRunning;

            _btnModeAsk.Enabled = loaded;
            _btnModeAgent.Enabled = loaded;
            _btnModeAutopilot.Enabled = loaded;

            // Visually dim the input area when workspace is not loaded.
            _txtInput.BackColor = loaded ? WallyTheme.Surface2 : WallyTheme.Surface0;
            _txtInput.ForeColor = loaded ? WallyTheme.TextPrimary : WallyTheme.TextDisabled;
            _inputBorder.BackColor = loaded ? WallyTheme.Border : WallyTheme.BorderSubtle;
            _inputArea.BackColor = loaded ? WallyTheme.Surface1 : WallyTheme.Surface0;
            _btnSend.BackColor = loaded ? WallyTheme.Surface3 : WallyTheme.Surface2;

            if (!loaded)
            {
                _lblEmptyState.Text =
                    "\U0001F4AC\n\nNo workspace loaded.\n\n" +
                    "Use File \u2192 Open Workspace or File \u2192 Setup New Workspace\n" +
                    "to get started.";
                _lblStatus.Text = "  No workspace";
                _lblStatus.ForeColor = WallyTheme.TextDisabled;
            }
            else
            {
                _lblEmptyState.Text =
                    "\U0001F4AC\n\nSelect a mode and type a message\nto start a conversation.\n\n" +
                    "\uD83D\uDCAC Ask \u2014 single response from one actor\n" +
                    "\uD83E\uDD16 Agent \u2014 iterative loop until completion\n" +
                    "\u26A1 Autopilot \u2014 run all actors on your prompt";
                if (!_isRunning)
                {
                    _lblStatus.Text = "  Ready";
                    _lblStatus.ForeColor = WallyTheme.TextMuted;
                }
            }
        }

        public void RefreshActorList()
        {
            if (InvokeRequired) { Invoke(RefreshActorList); return; }

            if (!_cboActor.ComboBox.IsHandleCreated)
                _cboActor.ComboBox.CreateControl();

            _cboActor.Items.Clear();
            if (_environment?.HasWorkspace != true) return;
            foreach (var actor in _environment.Actors)
                _cboActor.Items.Add(actor.Name);
            if (_cboActor.Items.Count > 0)
                _cboActor.SelectedIndex = 0;
        }

        public void RefreshModelList()
        {
            if (InvokeRequired) { Invoke(RefreshModelList); return; }

            if (!_cboModel.ComboBox.IsHandleCreated)
                _cboModel.ComboBox.CreateControl();

            _cboModel.Items.Clear();
            if (_environment?.HasWorkspace != true) return;
            var cfg = _environment.Workspace!.Config;
            _cboModel.Items.Add("");
            foreach (var model in cfg.DefaultModels)
                _cboModel.Items.Add(model);
            if (!string.IsNullOrEmpty(cfg.DefaultModel))
                _cboModel.Text = cfg.DefaultModel;
        }

        /// <summary>
        /// Clears all chat bubbles, properly disposing each one,
        /// and resets the empty-state placeholder.
        /// </summary>
        public void ClearMessages()
        {
            if (InvokeRequired) { Invoke(ClearMessages); return; }

            _messagesFlow.SuspendLayout();
            while (_messagesFlow.Controls.Count > 0)
            {
                var ctrl = _messagesFlow.Controls[0];
                _messagesFlow.Controls.RemoveAt(0);
                ctrl.Dispose();
            }
            _messagesFlow.ResumeLayout(true);

            _txtInput.Clear();
            _txtInput.SelectionColor = WallyTheme.TextPrimary;
            _txtInput.SelectionFont = WallyTheme.FontUI;

            UpdateEmptyState();
        }

        // ?? Send logic ??????????????????????????????????????????????????????

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                _ = SendMessageAsync();
            }
            else if (e.KeyCode == Keys.Escape && _isRunning)
            {
                e.SuppressKeyPress = true;
                _cts?.Cancel();
            }
        }

        private void OnSendClick(object? sender, EventArgs e) => _ = SendMessageAsync();

        private async Task SendMessageAsync()
        {
            string prompt = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(prompt) || _isRunning) return;

            if (_environment?.HasWorkspace != true || !_workspaceLoaded)
            {
                AddMessage("System",
                    "No workspace loaded. Use File \u2192 Open Workspace first.",
                    MessageKind.Error);
                return;
            }

            string? modelOverride = string.IsNullOrWhiteSpace(_cboModel.Text)
                ? null
                : _cboModel.Text.Trim();

            AddMessage("You", prompt, MessageKind.User);

            _txtInput.Clear();
            _txtInput.SelectionColor = WallyTheme.TextPrimary;
            _txtInput.SelectionFont = WallyTheme.FontUI;

            _cts = new CancellationTokenSource();

            switch (_currentMode)
            {
                case ChatMode.Ask:
                    await RunAskModeAsync(prompt, modelOverride);
                    break;
                case ChatMode.Agent:
                    await RunAgentModeAsync(prompt, modelOverride);
                    break;
                case ChatMode.Autopilot:
                    await RunAutopilotModeAsync(prompt, modelOverride);
                    break;
            }

            _cts?.Dispose();
            _cts = null;
        }

        // ?? Ask mode (single-shot) ??????????????????????????????????????????

        private async Task RunAskModeAsync(string prompt, string? modelOverride)
        {
            string actorName = _cboActor.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(actorName))
            {
                AddMessage("System", "No actor selected.", MessageKind.Error);
                return;
            }

            string cmdText = $"run {actorName} \"{prompt}\"" +
                             (modelOverride != null ? $" -m {modelOverride}" : "");
            CommandIssued?.Invoke(this, cmdText);

            SetRunning(true, actorName);
            try
            {
                var token = _cts!.Token;
                var responses = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    return _environment!.RunActor(prompt, actorName, modelOverride);
                }, token);

                foreach (string response in responses)
                    AddMessage(actorName, response, MessageKind.Actor);
                if (responses.Count == 0)
                    AddMessage("System", "No response from actor.", MessageKind.Error);
            }
            catch (OperationCanceledException)
            {
                AddMessage("System", "Cancelled.", MessageKind.Error);
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Error: {ex.Message}", MessageKind.Error);
            }
            finally { SetRunning(false); }
        }

        // ?? Agent mode (iterative loop) ?????????????????????????????????????

        private async Task RunAgentModeAsync(string prompt, string? modelOverride)
        {
            string actorName = _cboActor.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(actorName))
            {
                AddMessage("System", "No actor selected.", MessageKind.Error);
                return;
            }

            int maxIter = _environment!.Workspace!.Config.MaxIterations;
            string cmdText = $"run {actorName} \"{prompt}\" --loop -n {maxIter}" +
                             (modelOverride != null ? $" -m {modelOverride}" : "");
            CommandIssued?.Invoke(this, cmdText);

            SetRunning(true, $"{actorName} (agent loop)");
            try
            {
                var token = _cts!.Token;
                await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var actor = _environment.GetActor(actorName);
                    if (actor == null)
                    {
                        AddMessage("System", $"Actor '{actorName}' not found.", MessageKind.Error);
                        return;
                    }

                    string currentPrompt = prompt;
                    for (int i = 1; i <= maxIter; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        string result = _environment.ExecuteActor(actor, currentPrompt, modelOverride);

                        AddMessage($"{actorName} [{i}/{maxIter}]", result ?? "(no response)", MessageKind.Actor);

                        if (result == null) break;

                        if (result.Contains(WallyLoop.CompletedKeyword, StringComparison.OrdinalIgnoreCase))
                        {
                            AddMessage("System", $"Agent completed after {i} iteration(s).", MessageKind.Actor);
                            break;
                        }
                        if (result.Contains(WallyLoop.ErrorKeyword, StringComparison.OrdinalIgnoreCase))
                        {
                            AddMessage("System", $"Agent stopped with error after {i} iteration(s).", MessageKind.Error);
                            break;
                        }

                        currentPrompt =
                            $"You are continuing a task. Here is your previous response:\n\n" +
                            $"---\n{result}\n---\n\n" +
                            $"Continue where you left off. " +
                            $"If you are finished, respond with: {WallyLoop.CompletedKeyword}\n" +
                            $"If something went wrong, respond with: {WallyLoop.ErrorKeyword}";
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                AddMessage("System", "Agent loop cancelled.", MessageKind.Error);
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Error: {ex.Message}", MessageKind.Error);
            }
            finally { SetRunning(false); }
        }

        // ?? Autopilot mode (all actors) ?????????????????????????????????????

        private async Task RunAutopilotModeAsync(string prompt, string? modelOverride)
        {
            var actors = _environment!.Actors;
            if (actors.Count == 0)
            {
                AddMessage("System", "No actors loaded.", MessageKind.Error);
                return;
            }

            string actorNames = string.Join(", ", actors.ConvertAll(a => a.Name));
            string cmdText = $"autopilot \"{prompt}\" [{actorNames}]" +
                             (modelOverride != null ? $" -m {modelOverride}" : "");
            CommandIssued?.Invoke(this, cmdText);

            SetRunning(true, $"Autopilot ({actors.Count} actors)");
            try
            {
                var token = _cts!.Token;
                await Task.Run(() =>
                {
                    foreach (var actor in actors)
                    {
                        token.ThrowIfCancellationRequested();

                        AddMessage("System", $"Running {actor.Name}\u2026", MessageKind.Actor);

                        string result = _environment.ExecuteActor(actor, prompt, modelOverride);

                        AddMessage(actor.Name,
                            result != null ? $"[Role: {actor.Role.Name}]\n{result}" : "(no response)",
                            result != null ? MessageKind.Actor : MessageKind.Error);
                    }
                }, token);

                AddMessage("System",
                    $"Autopilot complete \u2014 {actors.Count} actor(s) responded.",
                    MessageKind.Actor);
            }
            catch (OperationCanceledException)
            {
                AddMessage("System", "Autopilot cancelled.", MessageKind.Error);
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Error: {ex.Message}", MessageKind.Error);
            }
            finally { SetRunning(false); }
        }

        // ?? Helpers ?????????????????????????????????????????????????????????

        // ApplyModelOverride removed — model is now passed to env.ExecuteActor/RunActor directly.

        // ?? Message rendering ???????????????????????????????????????????????

        private void AddMessage(string sender, string text, MessageKind kind)
        {
            if (InvokeRequired) { Invoke(() => AddMessage(sender, text, kind)); return; }

            int bubbleWidth = Math.Max(200, _messagesContainer.ClientSize.Width - 48);
            var bubble = new ChatBubble(sender, text, kind, bubbleWidth);

            _messagesFlow.SuspendLayout();
            _messagesFlow.Controls.Add(bubble);
            _messagesFlow.ResumeLayout(true);

            _messagesContainer.ScrollControlIntoView(bubble);
            UpdateEmptyState();
        }

        private void OnMessagesResize(object? sender, EventArgs e)
        {
            int w = Math.Max(200, _messagesContainer.ClientSize.Width - 48);
            _messagesFlow.SuspendLayout();
            foreach (Control c in _messagesFlow.Controls)
                if (c is ChatBubble b) b.SetBubbleWidth(w);
            _messagesFlow.ResumeLayout(true);
        }

        private void UpdateEmptyState()
        {
            bool empty = _messagesFlow.Controls.Count == 0;
            _lblEmptyState.Visible = empty;
            if (empty) _lblEmptyState.BringToFront();
        }

        // ?? UI state ????????????????????????????????????????????????????????

        private void SetRunning(bool running, string? context = null)
        {
            if (InvokeRequired) { Invoke(() => SetRunning(running, context)); return; }

            _isRunning = running;
            _btnSend.Visible = !running;
            _btnSend.Enabled = !running && _workspaceLoaded;
            _btnCancel.Visible = running;
            _txtInput.ReadOnly = running || !_workspaceLoaded;
            _cboActor.Enabled = !running && _workspaceLoaded && _currentMode != ChatMode.Autopilot;
            _cboModel.Enabled = !running && _workspaceLoaded;
            _btnModeAsk.Enabled = !running && _workspaceLoaded;
            _btnModeAgent.Enabled = !running && _workspaceLoaded;
            _btnModeAutopilot.Enabled = !running && _workspaceLoaded;

            _lblStatus.Text = running
                ? $"  \u26A1 {context}\u2026"
                : "  Ready";
            _lblStatus.ForeColor = running ? WallyTheme.TextSecondary : WallyTheme.TextMuted;

            if (!running && _workspaceLoaded)
                _btnSend.BackColor = WallyTheme.Surface3;
        }
    }

    // ????????????????????????????????????????????????????????????????????????
    //  ChatBubble — custom owner-drawn message with rounded corners
    // ????????????????????????????????????????????????????????????????????????

    internal sealed class ChatBubble : Control
    {
        private readonly string _sender;
        private readonly string _body;
        private readonly MessageKind _kind;
        private readonly string _timestamp;

        private const int BubbleRadius = 8;
        private const int PadX = 14;
        private const int PadY = 10;
        private const int SenderHeight = 18;
        private const int GapAfterSender = 4;
        private const int TimestampWidth = 50;

        private int _bodyHeight;

        public ChatBubble(string sender, string body, MessageKind kind, int width)
        {
            _sender = sender;
            _body = body;
            _kind = kind;
            _timestamp = DateTime.Now.ToString("HH:mm");

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            Margin = new Padding(0, 0, 0, 8);
            Width = width;
            RecalcHeight();
        }

        public void SetBubbleWidth(int w)
        {
            if (Width == w) return;
            Width = w;
            RecalcHeight();
        }

        private void RecalcHeight()
        {
            int textWidth = Width - PadX * 2;
            if (textWidth <= 0) { Height = 50; return; }

            using var g = CreateGraphics();
            var sz = TextRenderer.MeasureText(g, _body, WallyTheme.FontMono,
                new Size(textWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            _bodyHeight = sz.Height;
            Height = PadY + SenderHeight + GapAfterSender + _bodyHeight + PadY;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            WallyTheme.ConfigureGraphics(g);

            Color bubbleBg = _kind switch
            {
                MessageKind.User => WallyTheme.BubbleUser,
                MessageKind.Error => WallyTheme.BubbleError,
                _ => WallyTheme.BubbleActor
            };

            var bubbleRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = WallyTheme.RoundedRect(bubbleRect, BubbleRadius))
            using (var brush = new SolidBrush(bubbleBg))
            {
                g.FillPath(brush, path);
            }

            // Accent bar — subtle gray, slightly brighter for user messages,
            // slightly warmer for errors.
            Color accentColor = _kind switch
            {
                MessageKind.User => WallyTheme.Surface4,
                MessageKind.Error => WallyTheme.Red,
                _ => WallyTheme.Border
            };
            using (var accentBrush = new SolidBrush(accentColor))
            {
                g.FillRectangle(accentBrush, 0, BubbleRadius, 3, Height - BubbleRadius * 2);
            }

            int y = PadY;

            Color senderColor = _kind switch
            {
                MessageKind.User => WallyTheme.SenderUser,
                MessageKind.Error => WallyTheme.SenderSystem,
                _ => WallyTheme.SenderActor
            };
            TextRenderer.DrawText(g, _sender, WallyTheme.FontUISmallBold,
                new Rectangle(PadX, y, Width - PadX * 2 - TimestampWidth, SenderHeight),
                senderColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            TextRenderer.DrawText(g, _timestamp, WallyTheme.FontUISmall,
                new Rectangle(Width - PadX - TimestampWidth, y, TimestampWidth, SenderHeight),
                WallyTheme.TextDisabled, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

            y += SenderHeight + GapAfterSender;

            int textWidth = Width - PadX * 2;
            TextRenderer.DrawText(g, _body, WallyTheme.FontMono,
                new Rectangle(PadX, y, textWidth, _bodyHeight),
                WallyTheme.TextPrimary,
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        }
    }
}
