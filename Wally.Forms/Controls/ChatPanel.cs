using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Logging;
using Wally.Core.Providers;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// Describes the kind of chat message for styling purposes.
    /// </summary>
    internal enum MessageKind { User, Actor, Error, System }

    /// <summary>
    /// The action mode determines how prompts are executed and what the AI is
    /// allowed to do.
    /// </summary>
    internal enum ActionMode
    {
        /// <summary>Read-only: AI responds with text only. Uses the default (non-agentic) wrapper.</summary>
        Ask,
        /// <summary>Agentic: AI can make file changes on disk. Uses a wrapper with CanMakeChanges=true.</summary>
        Agent
    }

    /// <summary>
    /// AI chat panel — right-side copilot conversation window.
    /// <para>
    /// The panel exposes two axes of control:
    /// <list type="bullet">
    ///   <item><b>Action mode</b> (Ask / Agent) — determines the wrapper
    ///         and whether the AI can make file changes.</item>
    ///   <item><b>Loop selection</b> — optionally runs the prompt through an iterative
    ///         loop definition from the workspace's Loops/ folder.</item>
    /// </list>
    /// Dropdowns reflect exactly what is loaded from disk — no hardcoded items.
    /// </para>
    /// </summary>
    public sealed class ChatPanel : UserControl
    {
        // -- Header ----------------------------------------------------------

        private readonly Panel _header;
        private readonly Label _lblTitle;

        // -- Action mode selector --------------------------------------------

        private readonly Panel _modeBar;
        private readonly Button _btnModeAsk;
        private readonly Button _btnModeAgent;
        private readonly Label _lblModeHint;

        // -- Toolbar (Actor, Loop, Runbook, Model) ---------------------------

        private readonly ToolStrip _toolbar;
        private readonly ToolStripDropDownButton _ddActor;
        private readonly ToolStripDropDownButton _ddLoop;
        private readonly ToolStripDropDownButton _ddRunbook;
        private readonly ToolStripDropDownButton _ddModel;
        private readonly ToolStripButton _btnClear;
        private readonly ToolStripButton _btnClearHistory;

        // -- Selected values (tracked explicitly) ----------------------------

        private string? _selectedActor;    // null = "None" (direct prompt)
        private string? _selectedLoop;     // null = no loop (single run)
        private string? _selectedRunbook;  // null = no runbook
        private string? _selectedModel;    // null = workspace default

        // -- Conversation area -----------------------------------------------

        private readonly Panel _messagesContainer;
        private readonly FlowLayoutPanel _messagesFlow;
        private readonly Label _lblEmptyState;

        // -- Input area ------------------------------------------------------

        private readonly Panel _inputArea;
        private readonly Panel _inputBorder;
        private readonly RichTextBox _txtInput;
        private readonly Button _btnSend;
        private readonly Button _btnCancel;
        private readonly Label _lblStatus;
        private readonly Label _lblHint;

        // -- State -----------------------------------------------------------

        private WallyEnvironment? _environment;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private bool _workspaceLoaded;
        private ActionMode _currentMode = ActionMode.Ask;

        // -- Events ----------------------------------------------------------

        public event EventHandler<string>? CommandIssued;

        /// <summary>
        /// Raised on the UI thread whenever the running state changes.
        /// </summary>
        public event EventHandler? RunningChanged;

        // -- Constructor -----------------------------------------------------

        public ChatPanel()
        {
            SuspendLayout();

            var renderer = WallyTheme.CreateRenderer();

            // -- Header --
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

            // -- Action mode selector bar --
            _btnModeAsk   = CreateModeButton("\uD83D\uDCAC Ask");
            _btnModeAgent = CreateModeButton("\uD83E\uDD16 Agent");

            _btnModeAsk.Click   += (_, _) => SetMode(ActionMode.Ask);
            _btnModeAgent.Click += (_, _) => SetMode(ActionMode.Agent);

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

            _modeBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = WallyTheme.Surface1,
                Padding = Padding.Empty
            };
            _modeBar.Controls.Add(_lblModeHint);
            _modeBar.Controls.Add(modeButtonPanel);

            // -- Toolbar dropdown buttons --
            _ddActor   = CreateDropDown("None",      "Select actor (None = direct prompt)");
            _ddLoop    = CreateDropDown("(none)",    "Select loop (none = single run)");
            _ddRunbook = CreateDropDown("(none)",    "Select a runbook to execute in Agent mode");
            _ddModel   = CreateDropDown("(default)", "Select model (default = workspace default)");

            _btnClear = new ToolStripButton("\u2715 Clear")
            {
                ToolTipText = "Clear conversation bubbles (does not delete history file)",
                ForeColor = WallyTheme.TextSecondary
            };
            _btnClear.Click += (_, _) => ClearMessages();

            _btnClearHistory = new ToolStripButton("\uD83D\uDDD1 Clear History")
            {
                ToolTipText = "Clear persisted conversation history and chat bubbles",
                ForeColor = WallyTheme.TextSecondary
            };
            _btnClearHistory.Click += (_, _) =>
            {
                _environment?.History.ClearHistory();
                ClearMessages();
            };

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
                new ToolStripLabel("Actor")   { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _ddActor,
                new ToolStripSeparator(),
                new ToolStripLabel("Loop")    { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _ddLoop,
                new ToolStripSeparator(),
                new ToolStripLabel("Runbook") { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _ddRunbook,
                new ToolStripSeparator(),
                new ToolStripLabel("Model")   { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _ddModel,
                new ToolStripSeparator(),
                _btnClear,
                _btnClearHistory
            });

            // -- Empty state placeholder --
            _lblEmptyState = new Label
            {
                Text = "\U0001F4AC\n\nType a message to start a conversation.\n\n" +
                       "\uD83D\uDCAC Ask \u2014 read-only, text response only\n" +
                       "\uD83E\uDD16 Agent \u2014 can make file changes; select a Runbook for multi-step pipelines\n\n" +
                       "Use the Actor, Loop, and Model dropdowns\nto customise each request.",
                Dock = DockStyle.Fill,
                ForeColor = WallyTheme.TextDisabled,
                BackColor = WallyTheme.Surface0,
                Font = WallyTheme.FontUI,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            // -- Message flow --
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

            // -- Scrollable message container --
            _messagesContainer = ThemedEditorFactory.CreateScrollableSurface();
            _messagesContainer.Controls.Add(_messagesFlow);
            _messagesContainer.Controls.Add(_lblEmptyState);
            _messagesContainer.Controls.SetChildIndex(_lblEmptyState, 1);
            _messagesContainer.Controls.SetChildIndex(_messagesFlow, 0);
            _messagesContainer.Resize += OnMessagesResize;

            // -- Status label --
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

            // -- Input text box --
            _txtInput = ThemedEditorFactory.CreateInputTextArea(wordWrap: true, acceptsTab: false, backColor: WallyTheme.Surface2);
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

            // -- Assembly (order: fill first, then docked edges) --
            Controls.Add(_messagesContainer);   // Fill
            Controls.Add(_lblStatus);            // Bottom
            Controls.Add(_inputArea);            // Bottom
            Controls.Add(_toolbar);              // Top
            Controls.Add(_modeBar);              // Top (below header, above toolbar)
            Controls.Add(_header);               // Top (topmost)

            BackColor = WallyTheme.Surface0;
            ForeColor = WallyTheme.TextPrimary;

            ResumeLayout(true);

            // -- Initial state --
            SetMode(ActionMode.Ask);
            SetWorkspaceLoaded(false);
        }

        // -- Dropdown factory ------------------------------------------------

        /// <summary>
        /// Creates a themed <see cref="ToolStripDropDownButton"/> that acts as a
        /// simple single-level dropdown menu. Items are populated dynamically via
        /// the Refresh* methods. No inner ComboBox — just ToolStripMenuItems.
        /// </summary>
        private static ToolStripDropDownButton CreateDropDown(string defaultText, string tooltip)
        {
            var dd = new ToolStripDropDownButton(defaultText)
            {
                AutoSize = true,
                ShowDropDownArrow = true,
                ToolTipText = tooltip,
                ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUISmall
            };
            dd.DropDown.BackColor = WallyTheme.Surface2;
            dd.DropDown.ForeColor = WallyTheme.TextPrimary;
            return dd;
        }

        /// <summary>
        /// Replaces all items in a <see cref="ToolStripDropDownButton"/> with the
        /// given list. When the user clicks an item, <paramref name="onSelected"/>
        /// is called with the item text and a check mark is placed on the active item.
        /// </summary>
        private static void PopulateDropDown(
            ToolStripDropDownButton dd,
            IEnumerable<string> items,
            string? selectedValue,
            Action<string?> onSelected)
        {
            dd.DropDownItems.Clear();
            foreach (string item in items)
            {
                var mi = new ToolStripMenuItem(item)
                {
                    ForeColor = WallyTheme.TextPrimary,
                    BackColor = WallyTheme.Surface2,
                    Font = WallyTheme.FontUISmall,
                    Checked = string.Equals(item, selectedValue, StringComparison.Ordinal)
                };
                mi.Click += (s, _) =>
                {
                    string text = ((ToolStripMenuItem)s!).Text;
                    dd.Text = text;
                    // Update check marks.
                    foreach (ToolStripMenuItem other in dd.DropDownItems)
                        other.Checked = string.Equals(other.Text, text, StringComparison.Ordinal);
                    onSelected(text);
                };
                dd.DropDownItems.Add(mi);
            }
        }

        // -- Button factories ------------------------------------------------

        private static Button CreateModeButton(string text)
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

        // -- Action mode management ------------------------------------------

        private void SetMode(ActionMode mode)
        {
            _currentMode = mode;

            // Reset both buttons to inactive style.
            foreach (var btn in new[] { _btnModeAsk, _btnModeAgent })
            {
                btn.BackColor = WallyTheme.Surface2;
                btn.ForeColor = WallyTheme.TextSecondary;
                btn.FlatAppearance.BorderColor = WallyTheme.Border;
            }

            // Highlight the active button.
            Button active = mode == ActionMode.Agent ? _btnModeAgent : _btnModeAsk;
            active.BackColor = WallyTheme.Surface4;
            active.ForeColor = WallyTheme.TextPrimary;
            active.FlatAppearance.BorderColor = WallyTheme.TextMuted;

            // Update hint and header.
            _lblModeHint.Text = mode switch
            {
                ActionMode.Ask   => "Read-only \u2014 text response only",
                ActionMode.Agent => "Agentic \u2014 can make file changes \u00B7 select a Runbook for multi-step pipelines",
                _                => ""
            };
            _lblModeHint.ForeColor = WallyTheme.TextMuted;

            _lblTitle.Text = mode switch
            {
                ActionMode.Ask   => "AI CHAT \u2014 Ask",
                ActionMode.Agent => "AI CHAT \u2014 Agent",
                _                => "AI CHAT"
            };

            // Runbook is only meaningful in Agent mode — dim it otherwise.
            _ddRunbook.ForeColor = mode == ActionMode.Agent
                ? WallyTheme.TextPrimary
                : WallyTheme.TextDisabled;
            _ddRunbook.Enabled = mode == ActionMode.Agent && _workspaceLoaded && !_isRunning;

            bool inputEnabled = _workspaceLoaded && !_isRunning;
            _ddActor.Enabled = inputEnabled;
            _ddLoop.Enabled  = inputEnabled;
            _ddRunbook.Enabled   = inputEnabled && _currentMode == ActionMode.Agent;
            _ddModel.Enabled = inputEnabled;

            if (!_isRunning && _workspaceLoaded)
            {
                _btnSend.BackColor = WallyTheme.Surface3;
                _btnSend.Enabled   = true;
                _txtInput.ReadOnly = false;
            }
        }

        // -- Wrapper resolution ----------------------------------------------

        /// <summary>
        /// Resolves the wrapper name for the current action mode.
        /// Ask uses the first read-only wrapper (CanMakeChanges=false).
        /// Agent uses the first agentic wrapper (CanMakeChanges=true).
        /// Falls back to the workspace default if no capability-match is found.
        /// </summary>
        private string? ResolveWrapperForMode()
        {
            if (_environment?.HasWorkspace != true) return null;

            var wrappers = _environment.Workspace!.LlmWrappers;
            bool wantAgentic = _currentMode == ActionMode.Agent;
            var match = wrappers.FirstOrDefault(w => w.CanMakeChanges == wantAgentic);
            return match?.Name ?? (wrappers.Count > 0 ? wrappers[0].Name : null);
        }

        // -- Public API ------------------------------------------------------

        public void BindEnvironment(WallyEnvironment environment)
        {
            _environment = environment;
            RefreshActorList();
            RefreshLoopList();
            RefreshRunbookList();
            RefreshModelList();
        }

        /// <summary>Returns <see langword="true"/> while an AI request is in flight.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Requests cancellation of the currently running AI request, if any.</summary>
        public void Cancel() => _cts?.Cancel();

        /// <summary>
        /// Called by the main form to enable or disable workspace-dependent controls.
        /// </summary>
        public void SetWorkspaceLoaded(bool loaded)
        {
            if (InvokeRequired) { Invoke(() => SetWorkspaceLoaded(loaded)); return; }

            _workspaceLoaded = loaded;

            bool inputEnabled = loaded && !_isRunning;
            _ddActor.Enabled = inputEnabled;
            _ddLoop.Enabled = inputEnabled;
            _ddModel.Enabled = inputEnabled;
            _btnClear.Enabled = loaded;
            _btnClearHistory.Enabled = loaded;
            _btnSend.Enabled = inputEnabled;
            _txtInput.ReadOnly = !inputEnabled;

            _btnModeAsk.Enabled   = loaded;
            _btnModeAgent.Enabled = loaded;

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
                    "\U0001F4AC\n\nType a message to start a conversation.\n\n" +
                    "\uD83D\uDCAC Ask \u2014 read-only, text response only\n" +
                    "\uD83E\uDD16 Agent \u2014 can make file changes; select a Runbook for multi-step pipelines\n\n" +
                    "Use the Actor, Loop, and Model dropdowns\nto customise each request.";
                if (!_isRunning)
                {
                    _lblStatus.Text = "  Ready";
                    _lblStatus.ForeColor = WallyTheme.TextMuted;
                }

                // Populate chat bubbles from persisted conversation history.
                LoadHistory();
            }
        }

        public void RefreshActorList()
        {
            if (InvokeRequired) { Invoke(RefreshActorList); return; }

            var items = new List<string> { "None" };
            if (_environment?.HasWorkspace == true)
                items.AddRange(_environment.Actors.Select(a => a.Name));

            _selectedActor = null; // default to None
            _ddActor.Text = "None";
            PopulateDropDown(_ddActor, items, "None", value =>
            {
                _selectedActor = string.Equals(value, "None", StringComparison.OrdinalIgnoreCase) ? null : value;
            });
        }

        public void RefreshLoopList()
        {
            if (InvokeRequired) { Invoke(RefreshLoopList); return; }

            var items = new List<string> { "(none)" };
            if (_environment?.HasWorkspace == true)
                items.AddRange(_environment.Loops.Select(l => l.Name));

            string selected = "(none)";
            _selectedLoop = null;
            if (_environment?.HasWorkspace == true)
            {
                var cfg = _environment.Workspace!.Config;
                if (!string.IsNullOrEmpty(cfg.ResolvedDefaultLoop) &&
                    _environment.Loops.Any(l => l.Name == cfg.ResolvedDefaultLoop))
                {
                    selected = cfg.ResolvedDefaultLoop;
                    _selectedLoop = selected;
                }
            }

            _ddLoop.Text = selected;
            PopulateDropDown(_ddLoop, items, selected, value =>
            {
                _selectedLoop = string.Equals(value, "(none)", StringComparison.OrdinalIgnoreCase)
                    ? null : value;
                // Selecting a loop clears the runbook (mutually exclusive execution paths).
                if (_selectedLoop != null)
                {
                    _selectedRunbook = null;
                    _ddRunbook.Text = "(none)";
                    foreach (ToolStripMenuItem mi in _ddRunbook.DropDownItems)
                        mi.Checked = string.Equals(mi.Text, "(none)", StringComparison.Ordinal);
                }
            });
        }

        public void RefreshRunbookList()
        {
            if (InvokeRequired) { Invoke(RefreshRunbookList); return; }

            var items = new List<string> { "(none)" };
            if (_environment?.HasWorkspace == true)
                items.AddRange(_environment.Runbooks.Select(r => r.Name));

            _selectedRunbook = null;
            _ddRunbook.Text = "(none)";
            PopulateDropDown(_ddRunbook, items, "(none)", value =>
            {
                _selectedRunbook = string.Equals(value, "(none)", StringComparison.OrdinalIgnoreCase)
                    ? null : value;
                // Selecting a runbook clears the loop (they are mutually exclusive execution paths).
                if (_selectedRunbook != null)
                {
                    _selectedLoop = null;
                    _ddLoop.Text = "(none)";
                    foreach (ToolStripMenuItem mi in _ddLoop.DropDownItems)
                        mi.Checked = string.Equals(mi.Text, "(none)", StringComparison.Ordinal);
                }
            });
        }

        public void RefreshModelList()
        {
            if (InvokeRequired) { Invoke(RefreshModelList); return; }

            var items = new List<string>();
            if (_environment?.HasWorkspace == true)
            {
                var cfg = _environment.Workspace!.Config;
                items.AddRange(cfg.DefaultModels);
            }

            // Resolve default model selection.
            string? selected = null;
            if (_environment?.HasWorkspace == true)
            {
                var cfg = _environment.Workspace!.Config;
                if (!string.IsNullOrEmpty(cfg.DefaultModel) && cfg.DefaultModels.Contains(cfg.DefaultModel))
                    selected = cfg.DefaultModel;
                else if (cfg.DefaultModels.Count > 0)
                    selected = cfg.DefaultModels[0];
            }

            _selectedModel = selected;
            _ddModel.Text = selected ?? "(default)";
            PopulateDropDown(_ddModel, items, selected, value =>
            {
                _selectedModel = value;
            });
        }

        /// <summary>Clears all chat bubbles, properly disposing each one, and resets the empty-state placeholder.</summary>
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

        /// <summary>
        /// Populates the chat panel with bubbles from the persisted conversation
        /// history. Called once on workspace load to provide display continuity
        /// across app restarts.
        /// </summary>
        private void LoadHistory()
        {
            if (_environment?.HasWorkspace != true) return;

            var turns = _environment.History.GetAllTurns();
            if (turns.Count == 0) return;

            _messagesFlow.SuspendLayout();

            // Cap at the most recent 50 turns to avoid sluggish UI.
            int start = Math.Max(0, turns.Count - 50);
            for (int i = start; i < turns.Count; i++)
            {
                var turn = turns[i];
                int bubbleWidth = Math.Max(200, _messagesContainer.ClientSize.Width - 48);

                // Prompt bubble (User).
                string userLabel = turn.ActorName != null ? $"You \u2192 {turn.ActorName}" : "You";
                var userBubble = new ChatBubble(userLabel, turn.Prompt, MessageKind.User, bubbleWidth);
                _messagesFlow.Controls.Add(userBubble);

                // Response bubble — Wally-mode turns had a loop name recorded on them.
                string responseLabel = turn.ActorName ?? "AI";
                var responseKind = turn.IsError ? MessageKind.Error : MessageKind.Actor;
                var responseBubble = new ChatBubble(responseLabel, turn.Response, responseKind, bubbleWidth);
                _messagesFlow.Controls.Add(responseBubble);
            }

            _messagesFlow.ResumeLayout(true);

            // Scroll to the end and update empty state.
            if (_messagesFlow.Controls.Count > 0)
                _messagesContainer.ScrollControlIntoView(
                    _messagesFlow.Controls[_messagesFlow.Controls.Count - 1]);
            UpdateEmptyState();
        }

        // -- Clear history logic ----------------------------------------------

        private void ClearHistory()
        {
            if (InvokeRequired) { Invoke(ClearHistory); return; }

            var result = MessageBox.Show("Clear conversation history?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            // TODO: Implement history clearing logic
            MessageBox.Show("History cleared (not really, implement me).", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // -- Send logic ------------------------------------------------------

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                if (_isRunning)
                {
                    // Swallow the Enter key while a response is in flight —
                    // let the user cancel via the Stop button or Escape instead.
                    return;
                }
                _ = SendMessageAsync();
            }
            else if (e.KeyCode == Keys.Escape && _isRunning)
            {
                e.SuppressKeyPress = true;
                _cts?.Cancel();
            }
        }

        private void OnSendClick(object? sender, EventArgs e)
        {
            if (_isRunning) return;  // button should already be hidden, but guard defensively
            _ = SendMessageAsync();
        }

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

            // -- Resolve selections --
            string? actorName     = _selectedActor;
            string? loopName      = _selectedLoop;
            string? runbookName   = _currentMode == ActionMode.Agent ? _selectedRunbook : null;
            string? modelOverride = string.IsNullOrWhiteSpace(_selectedModel) ? null : _selectedModel;
            string? wrapperName   = ResolveWrapperForMode();

            bool hasRunbook = !string.IsNullOrEmpty(runbookName);
            bool directMode = string.IsNullOrEmpty(actorName);
            bool isLooped   = !string.IsNullOrEmpty(loopName);
            string label    = directMode ? "AI" : actorName!;

            // Build the equivalent CLI command for the terminal echo.
            string cmdText = hasRunbook
                ? $"runbook {runbookName} \"{prompt}\""
                : string.Join(" ",
                    new List<string>(new[] { "run", $"\"{prompt}\"" })
                    .Concat(!directMode         ? new[] { $"-a {actorName}" } : Array.Empty<string>())
                    .Concat(isLooped            ? new[] { $"-l {loopName}" }  : Array.Empty<string>())
                    .Concat(modelOverride != null ? new[] { $"-m {modelOverride}" } : Array.Empty<string>())
                    .Concat(wrapperName   != null ? new[] { $"-w {wrapperName}" }   : Array.Empty<string>()));
            CommandIssued?.Invoke(this, cmdText);

            AddMessage("You", prompt, MessageKind.User);
            _txtInput.Clear();
            _txtInput.SelectionColor = WallyTheme.TextPrimary;
            _txtInput.SelectionFont  = WallyTheme.FontUI;

            _cts = new CancellationTokenSource();

            string modeLabel = _currentMode == ActionMode.Agent ? "Agent" : "Ask";
            string runLabel  = hasRunbook
                ? $"{modeLabel}: runbook [{runbookName}]"
                : isLooped
                    ? $"{modeLabel}: {label} [{loopName}]"
                    : $"{modeLabel}: {label}";
            SetRunning(true, runLabel);

            try
            {
                var token = _cts.Token;

                if (hasRunbook)
                {
                    // -- Runbook execution path --
                    // Runs an entire .wrb command sequence with the prompt substituted in.
                    await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        WallyCommands.HandleRunbook(_environment!, runbookName!, prompt);
                    }, token);

                    AddMessage("System",
                        $"\uD83D\uDCDC Runbook '{runbookName}' complete.",
                        MessageKind.System);
                }
                else
                {
                    // -- Standard run path (Ask / Agent with optional loop) --
                    var results = await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        return WallyCommands.HandleRunTyped(
                            _environment!,
                            prompt,
                            actorName,
                            modelOverride,
                            loopName:          loopName,
                            wrapper:           wrapperName,
                            noHistory:         false,
                            cancellationToken: token);
                    }, token);

                    if (results.Count == 0)
                        AddMessage("System", "No response from AI.", MessageKind.Error);
                    else if (results.Count == 1)
                        AddMessage(results[0].DisplayLabel(), results[0].Response, MessageKind.Actor);
                    else
                    {
                        foreach (var r in results)
                            AddMessage(r.DisplayLabel(), r.Response, MessageKind.Actor);
                        AddMessage("System",
                            $"Pipeline complete \u2014 {results.Count} step(s).",
                            MessageKind.System);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                AddMessage("System", "Cancelled.", MessageKind.Error);
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Error: {ex.Message}", MessageKind.Error);
            }
            finally
            {
                SetRunning(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        // -- Message rendering -----------------------------------------------

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

        // -- UI state --------------------------------------------------------

        private void SetRunning(bool running, string? context = null)
        {
            if (InvokeRequired) { Invoke(() => SetRunning(running, context)); return; }

            _isRunning = running;
            _btnSend.Visible   = !running;
            _btnSend.Enabled   = !running && _workspaceLoaded;
            _btnCancel.Visible = running;

            bool inputEnabled = !running && _workspaceLoaded;
            _txtInput.ReadOnly   = !inputEnabled;
            _ddActor.Enabled     = inputEnabled;
            _ddLoop.Enabled      = inputEnabled;
            _ddRunbook.Enabled   = inputEnabled && _currentMode == ActionMode.Agent;
            _ddModel.Enabled     = inputEnabled;
            _btnModeAsk.Enabled  = inputEnabled;
            _btnModeAgent.Enabled = inputEnabled;

            _lblStatus.Text = running
                ? $"  \u26A1 {context}\u2026"
                : "  Ready";
            _lblStatus.ForeColor = running ? WallyTheme.TextSecondary : WallyTheme.TextMuted;

            if (!running && _workspaceLoaded)
                _btnSend.BackColor = WallyTheme.Surface3;

            RunningChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // ------------------------------------------------------------------------
    //  ChatBubble — custom owner-drawn message with rounded corners
    // ------------------------------------------------------------------------

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
                MessageKind.User   => WallyTheme.BubbleUser,
                MessageKind.Error  => WallyTheme.BubbleError,
                MessageKind.System => WallyTheme.Surface2,
                _                  => WallyTheme.BubbleActor
            };

            var bubbleRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = WallyTheme.RoundedRect(bubbleRect, BubbleRadius))
            using (var brush = new SolidBrush(bubbleBg))
            {
                g.FillPath(brush, path);
            }

            // Accent bar
            Color accentColor = _kind switch
            {
                MessageKind.User => WallyTheme.Surface4,
                MessageKind.Error => WallyTheme.Red,
                MessageKind.System => WallyTheme.TextMuted,
                _ => WallyTheme.Border
            };
            using (var accentBrush = new SolidBrush(accentColor))
            {
                g.FillRectangle(accentBrush, 0, BubbleRadius, 3, Height - BubbleRadius * 2);
            }

            int y = PadY;

            Color senderColor = _kind switch
            {
                MessageKind.User   => WallyTheme.SenderUser,
                MessageKind.Error  => WallyTheme.SenderSystem,
                MessageKind.System => WallyTheme.TextMuted,
                _                  => WallyTheme.SenderActor
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

