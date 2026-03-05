using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wally.Core;
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
    /// allowed to do. Maps directly to the wrapper's <c>CanMakeChanges</c> flag.
    /// </summary>
    internal enum ActionMode
    {
        /// <summary>Read-only: AI responds with text only. Uses the default (non-agentic) wrapper.</summary>
        Ask,
        /// <summary>Agentic: AI can make file changes on disk. Uses a wrapper with CanMakeChanges=true.</summary>
        Agent,
        /// <summary>
        /// Wally Automod: future mode that leverages deeper Wally loop orchestration.
        /// Placeholder — not yet functional.
        /// </summary>
        Automod
    }

    /// <summary>
    /// AI chat panel — right-side copilot conversation window.
    /// <para>
    /// The panel exposes two axes of control:
    /// <list type="bullet">
    ///   <item><b>Action mode</b> (Ask / Agent / Automod) — determines the wrapper
    ///         and whether the AI can make file changes.</item>
    ///   <item><b>Loop selection</b> — optionally runs the prompt through an iterative
    ///         loop definition from the workspace's Loops/ folder.</item>
    /// </list>
    /// Dropdowns reflect exactly what is loaded from disk — no hardcoded items.
    /// </para>
    /// </summary>
    public sealed class ChatPanel : UserControl
    {
        // ── Header ──────────────────────────────────────────────────────────

        private readonly Panel _header;
        private readonly Label _lblTitle;

        // ── Action mode selector ────────────────────────────────────────────

        private readonly Panel _modeBar;
        private readonly Button _btnModeAsk;
        private readonly Button _btnModeAgent;
        private readonly Button _btnModeAutomod;
        private readonly Label _lblModeHint;

        // ── Toolbar (Actor, Loop, Model) ────────────────────────────────────

        private readonly ToolStrip _toolbar;
        private readonly ToolStripDropDownButton _ddActor;
        private readonly ToolStripDropDownButton _ddLoop;
        private readonly ToolStripDropDownButton _ddModel;
        private readonly ToolStripButton _btnClear;

        // ── Selected values (tracked explicitly) ────────────────────────────

        private string? _selectedActor;   // null = "None" (direct prompt)
        private string? _selectedLoop;    // null = no loop (single run)
        private string? _selectedModel;   // null = workspace default

        // ── Conversation area ───────────────────────────────────────────────

        private readonly Panel _messagesContainer;
        private readonly FlowLayoutPanel _messagesFlow;
        private readonly Label _lblEmptyState;

        // ── Input area ──────────────────────────────────────────────────────

        private readonly Panel _inputArea;
        private readonly Panel _inputBorder;
        private readonly RichTextBox _txtInput;
        private readonly Button _btnSend;
        private readonly Button _btnCancel;
        private readonly Label _lblStatus;
        private readonly Label _lblHint;

        // ── State ───────────────────────────────────────────────────────────

        private WallyEnvironment? _environment;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private bool _workspaceLoaded;
        private ActionMode _currentMode = ActionMode.Ask;

        // ── Events ──────────────────────────────────────────────────────────

        public event EventHandler<string>? CommandIssued;

        // ── Constructor ─────────────────────────────────────────────────────

        public ChatPanel()
        {
            SuspendLayout();

            var renderer = WallyTheme.CreateRenderer();

            // ── Header ──
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

            // ── Action mode selector bar ──
            _btnModeAsk = CreateModeButton("\uD83D\uDCAC Ask");
            _btnModeAgent = CreateModeButton("\uD83E\uDD16 Agent");
            _btnModeAutomod = CreateModeButton("\u2699 Automod");

            _btnModeAsk.Click += (_, _) => SetMode(ActionMode.Ask);
            _btnModeAgent.Click += (_, _) => SetMode(ActionMode.Agent);
            _btnModeAutomod.Click += (_, _) => SetMode(ActionMode.Automod);

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
            modeButtonPanel.Controls.Add(_btnModeAutomod);

            _modeBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = WallyTheme.Surface1,
                Padding = Padding.Empty
            };
            _modeBar.Controls.Add(_lblModeHint);
            _modeBar.Controls.Add(modeButtonPanel);

            // ── Toolbar dropdown buttons ──
            _ddActor = CreateDropDown("None", "Select actor (None = direct prompt)");
            _ddLoop = CreateDropDown("(none)", "Select loop (none = single run)");
            _ddModel = CreateDropDown("(default)", "Select model (default = workspace default)");

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
                _ddActor,
                new ToolStripSeparator(),
                new ToolStripLabel("Loop") { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _ddLoop,
                new ToolStripSeparator(),
                new ToolStripLabel("Model") { ForeColor = WallyTheme.TextMuted, Font = WallyTheme.FontUISmallBold },
                _ddModel,
                new ToolStripSeparator(),
                _btnClear
            });

            // ── Empty state placeholder ──
            _lblEmptyState = new Label
            {
                Text = "\U0001F4AC\n\nType a message to start a conversation.\n\n" +
                       "Select an actor to add persona context,\nor leave it unselected for direct AI prompts.\n\n" +
                       "\uD83D\uDCAC Ask \u2014 text response only (read-only)\n" +
                       "\uD83E\uDD16 Agent \u2014 can make file changes\n" +
                       "\u2699 Automod \u2014 coming soon",
                Dock = DockStyle.Fill,
                ForeColor = WallyTheme.TextDisabled,
                BackColor = WallyTheme.Surface0,
                Font = WallyTheme.FontUI,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            // ── Message flow ──
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

            // ── Scrollable message container ──
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

            // ── Status label ──
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

            // ── Input text box ──
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

            // ── Assembly (order: fill first, then docked edges) ──
            Controls.Add(_messagesContainer);   // Fill
            Controls.Add(_lblStatus);            // Bottom
            Controls.Add(_inputArea);            // Bottom
            Controls.Add(_toolbar);              // Top
            Controls.Add(_modeBar);              // Top (below header, above toolbar)
            Controls.Add(_header);               // Top (topmost)

            BackColor = WallyTheme.Surface0;
            ForeColor = WallyTheme.TextPrimary;

            ResumeLayout(true);

            // ── Initial state ──
            SetMode(ActionMode.Ask);
            SetWorkspaceLoaded(false);
        }

        // ── Dropdown factory ────────────────────────────────────────────────

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

        // ── Button factories ────────────────────────────────────────────────

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

        // ── Action mode management ──────────────────────────────────────────

        private void SetMode(ActionMode mode)
        {
            _currentMode = mode;

            // Reset all mode buttons to inactive.
            foreach (var btn in new[] { _btnModeAsk, _btnModeAgent, _btnModeAutomod })
            {
                btn.BackColor = WallyTheme.Surface2;
                btn.ForeColor = WallyTheme.TextSecondary;
                btn.FlatAppearance.BorderColor = WallyTheme.Border;
            }

            // Activate the selected button.
            Button active = mode switch
            {
                ActionMode.Ask => _btnModeAsk,
                ActionMode.Agent => _btnModeAgent,
                ActionMode.Automod => _btnModeAutomod,
                _ => _btnModeAsk
            };
            active.BackColor = WallyTheme.Surface4;
            active.ForeColor = WallyTheme.TextPrimary;
            active.FlatAppearance.BorderColor = WallyTheme.TextMuted;

            // Update hint text.
            _lblModeHint.Text = mode switch
            {
                ActionMode.Ask => "Read-only response",
                ActionMode.Agent => "Can make file changes",
                ActionMode.Automod => "Coming soon",
                _ => ""
            };
            _lblModeHint.ForeColor = WallyTheme.TextMuted;

            // Update send button state.
            if (!_isRunning && _workspaceLoaded)
            {
                _btnSend.BackColor = WallyTheme.Surface3;
                // Automod is not yet functional — disable send.
                _btnSend.Enabled = mode != ActionMode.Automod;
                _txtInput.ReadOnly = mode == ActionMode.Automod;
            }

            // All controls remain enabled in Ask and Agent — actor, loop,
            // and model are orthogonal to the action mode.
            bool inputEnabled = _workspaceLoaded && !_isRunning && mode != ActionMode.Automod;
            _ddActor.Enabled = inputEnabled;
            _ddLoop.Enabled = inputEnabled;
            _ddModel.Enabled = inputEnabled;

            // Update header.
            _lblTitle.Text = mode switch
            {
                ActionMode.Ask => "AI CHAT \u2014 Ask",
                ActionMode.Agent => "AI CHAT \u2014 Agent",
                ActionMode.Automod => "AI CHAT \u2014 Automod (coming soon)",
                _ => "AI CHAT"
            };
        }

        // ── Wrapper resolution ──────────────────────────────────────────────

        /// <summary>
        /// Resolves the wrapper name for the current action mode.
        /// Ask mode uses the first read-only wrapper (CanMakeChanges=false).
        /// Agent mode uses the first agentic wrapper (CanMakeChanges=true).
        /// Falls back to the workspace default if no match is found.
        /// </summary>
        private string? ResolveWrapperForMode()
        {
            if (_environment?.HasWorkspace != true) return null;

            var wrappers = _environment.Workspace!.LlmWrappers;
            bool wantAgentic = _currentMode == ActionMode.Agent;

            // Find the first wrapper matching the desired capability.
            var match = wrappers.FirstOrDefault(w => w.CanMakeChanges == wantAgentic);
            if (match != null) return match.Name;

            // Fallback: any wrapper, or null (let the environment use its default).
            return wrappers.Count > 0 ? wrappers[0].Name : null;
        }

        // ── Public API ──────────────────────────────────────────────────────

        public void BindEnvironment(WallyEnvironment environment)
        {
            _environment = environment;
            RefreshActorList();
            RefreshLoopList();
            RefreshModelList();
        }

        /// <summary>
        /// Called by the main form to enable or disable workspace-dependent controls.
        /// </summary>
        public void SetWorkspaceLoaded(bool loaded)
        {
            if (InvokeRequired) { Invoke(() => SetWorkspaceLoaded(loaded)); return; }

            _workspaceLoaded = loaded;

            bool inputEnabled = loaded && !_isRunning && _currentMode != ActionMode.Automod;
            _ddActor.Enabled = inputEnabled;
            _ddLoop.Enabled = inputEnabled;
            _ddModel.Enabled = inputEnabled;
            _btnClear.Enabled = loaded;
            _btnSend.Enabled = inputEnabled;
            _txtInput.ReadOnly = !inputEnabled;

            _btnModeAsk.Enabled = loaded;
            _btnModeAgent.Enabled = loaded;
            _btnModeAutomod.Enabled = loaded;

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
                    "\uD83D\uDCAC Ask \u2014 text response only (read-only)\n" +
                    "\uD83E\uDD16 Agent \u2014 can make file changes on disk\n" +
                    "\u2699 Automod \u2014 deep Wally loop orchestration (coming soon)\n\n" +
                    "Select an actor and loop from the toolbar,\nor leave them unselected for a direct prompt.";
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

            // Resolve default loop selection.
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
                _selectedLoop = string.Equals(value, "(none)", StringComparison.OrdinalIgnoreCase) ? null : value;
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

        // ── Send logic ──────────────────────────────────────────────────────

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

            if (_currentMode == ActionMode.Automod)
            {
                AddMessage("System",
                    "Automod is not yet available. Use Ask or Agent mode.",
                    MessageKind.Error);
                return;
            }

            if (_environment?.HasWorkspace != true || !_workspaceLoaded)
            {
                AddMessage("System",
                    "No workspace loaded. Use File \u2192 Open Workspace first.",
                    MessageKind.Error);
                return;
            }

            // ── Resolve selections from tracked state ──
            string? actorName = _selectedActor;
            bool directMode = string.IsNullOrEmpty(actorName);

            string? loopName = _selectedLoop;
            bool isLooped = !string.IsNullOrEmpty(loopName);

            string? modelOverride = _selectedModel;
            if (string.IsNullOrWhiteSpace(modelOverride))
                modelOverride = null;

            string? wrapperName = ResolveWrapperForMode();

            // ── Build the equivalent CLI command for logging ──
            string label = directMode ? "AI" : actorName!;
            var cmdParts = new List<string> { "run", $"\"{prompt}\"" };
            if (!directMode) cmdParts.Add($"-a {actorName}");
            if (isLooped) cmdParts.Add($"-l {loopName}");
            if (modelOverride != null) cmdParts.Add($"-m {modelOverride}");
            if (wrapperName != null) cmdParts.Add($"-w {wrapperName}");
            string cmdText = string.Join(" ", cmdParts);
            CommandIssued?.Invoke(this, cmdText);

            AddMessage("You", prompt, MessageKind.User);

            _txtInput.Clear();
            _txtInput.SelectionColor = WallyTheme.TextPrimary;
            _txtInput.SelectionFont = WallyTheme.FontUI;

            _cts = new CancellationTokenSource();

            string modeLabel = _currentMode == ActionMode.Agent ? "Agent" : "Ask";
            string runLabel = isLooped
                ? $"{modeLabel}: {label} [{loopName}]"
                : $"{modeLabel}: {label}";
            SetRunning(true, runLabel);

            try
            {
                var token = _cts.Token;

                var results = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    return WallyCommands.HandleRun(
                        _environment!,
                        prompt,
                        actorName,
                        modelOverride,
                        looped: isLooped,
                        loopName: loopName,
                        maxIterations: 0,   // use loop definition or config default
                        wrapper: wrapperName);
                }, token);

                if (results.Count == 0)
                {
                    AddMessage("System", "No response from AI.", MessageKind.Error);
                }
                else if (results.Count == 1)
                {
                    AddMessage(label, results[0], MessageKind.Actor);
                }
                else
                {
                    for (int i = 0; i < results.Count; i++)
                        AddMessage($"{label} [{i + 1}/{results.Count}]", results[i], MessageKind.Actor);
                    AddMessage("System",
                        $"Loop completed — {results.Count} iteration(s).",
                        MessageKind.System);
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

        // ── Message rendering ───────────────────────────────────────────────

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

        // ── UI state ────────────────────────────────────────────────────────

        private void SetRunning(bool running, string? context = null)
        {
            if (InvokeRequired) { Invoke(() => SetRunning(running, context)); return; }

            _isRunning = running;
            _btnSend.Visible = !running;
            _btnSend.Enabled = !running && _workspaceLoaded && _currentMode != ActionMode.Automod;
            _btnCancel.Visible = running;

            bool inputEnabled = !running && _workspaceLoaded && _currentMode != ActionMode.Automod;
            _txtInput.ReadOnly = !inputEnabled;
            _ddActor.Enabled = inputEnabled;
            _ddLoop.Enabled = inputEnabled;
            _ddModel.Enabled = inputEnabled;
            _btnModeAsk.Enabled = !running && _workspaceLoaded;
            _btnModeAgent.Enabled = !running && _workspaceLoaded;
            _btnModeAutomod.Enabled = !running && _workspaceLoaded;

            _lblStatus.Text = running
                ? $"  \u26A1 {context}\u2026"
                : "  Ready";
            _lblStatus.ForeColor = running ? WallyTheme.TextSecondary : WallyTheme.TextMuted;

            if (!running && _workspaceLoaded)
                _btnSend.BackColor = WallyTheme.Surface3;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  ChatBubble — custom owner-drawn message with rounded corners
    // ────────────────────────────────────────────────────────────────────────

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
                MessageKind.System => WallyTheme.TextMuted,
                _ => WallyTheme.BubbleActor
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
                MessageKind.User => WallyTheme.SenderUser,
                MessageKind.Error => WallyTheme.SenderSystem,
                MessageKind.System => WallyTheme.TextMuted,
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
