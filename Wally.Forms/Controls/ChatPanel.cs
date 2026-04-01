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
using Wally.Forms.ChatPanelSupport;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    internal enum MessageKind { User, Actor, Error, System }

    internal enum ChatPacingMode { Auto, Manual }

    internal enum ActionMode
    {
        /// <summary>Read-only: AI responds with text only.</summary>
        Ask,
        /// <summary>Agentic: AI can make file changes on disk.</summary>
        Agent
    }

    /// <summary>
    /// AI chat panel � right-side copilot conversation window.
    ///
    /// Layout (top ? bottom):
    ///   Header bar        � "AI CHAT � Ask/Agent" title
    ///   Row 1 / Mode bar  � Ask | Agent  �  ? Clear | ?? History  (plain Buttons, no ToolStrip)
    ///   Row 2 / Flow bar  � Auto | Manual � Next | Prompt | Next Prompt | Diagram
    ///   Row 2 / Selectors � Actor ?  Loop ?  Model ?   (wrapping FlowLayoutPanel,
    ///                        never produces a ToolStrip overflow chevron)
    ///   Messages area     � scrollable chat bubbles
    ///   Status bar        � ready / running indicator
    ///   Input area        � text box + Send / Stop buttons
    /// </summary>
    public sealed class ChatPanel : UserControl
    {
        // ?? Header ????????????????????????????????????????????????????????????
        private readonly Panel _header;
        private readonly Label _lblTitle;

        // ?? Row 1: mode bar ???????????????????????????????????????????????????
        private readonly Panel  _modeBar;
        private readonly Button _btnModeAsk;
        private readonly Button _btnModeAgent;
        private readonly Button _btnClear;
        private readonly Button _btnClearHistory;

        // ?? Row 2: execution / preview bar ??????????????????????????????????
        private readonly Panel  _flowBar;
        private readonly Button _btnFlowAuto;
        private readonly Button _btnFlowManual;
        private readonly Button _btnNextStep;
        private readonly Button _btnPreviewPrompt;
        private readonly Button _btnPreviewNextPrompt;
        private readonly Button _btnDiagram;

        // ?? Row 2: selectors bar ??????????????????????????????????????????????
        // Each selector is a plain Button whose Tag holds a ContextMenuStrip.
        // No ToolStrip anywhere � no overflow chevron possible.
        private readonly Panel            _selectorsBar;
        private readonly Button           _btnActorDd;
        private readonly ContextMenuStrip _mnuActor;
        private readonly Button           _btnLoopDd;
        private readonly ContextMenuStrip _mnuLoop;
        private readonly Button           _btnModelDd;
        private readonly ContextMenuStrip _mnuModel;

        // ?? Selected values (raw � never contain the " (default)" suffix) /////
        private string? _selectedActor;
        private string? _selectedLoop;
        private string? _selectedModel;

        // Resolved defaults � used to apply / restore the " (default)" label.
        private string? _defaultActor;
        private string? _defaultLoop;
        private string? _defaultModel;

        // ?? Messages ??????????????????????????????????????????????????????????
        private readonly Panel           _messagesContainer;
        private readonly FlowLayoutPanel _messagesFlow;
        private readonly Label           _lblEmptyState;

        // ?? Input area ????????????????????????????????????????????????????????
        private readonly Panel       _inputArea;
        private readonly Panel       _inputBorder;
        private readonly RichTextBox _txtInput;
        private readonly Button      _btnSend;
        private readonly Button      _btnCancel;
        private readonly Label       _lblStatus;
        private readonly Label       _lblHint;

        // ?? State ?????????????????????????????????????????????????????????????
        private WallyEnvironment?        _environment;
        private CancellationTokenSource? _cts;
        private bool       _isRunning;
        private bool       _workspaceLoaded;
        private ActionMode _currentMode = ActionMode.Ask;
        private ChatPacingMode _pacingMode = ChatPacingMode.Auto;
        private ChatPanelExecutionSession? _manualSession;

        // ?? Events ????????????????????????????????????????????????????????????
        public event EventHandler<string>? CommandIssued;
        public event EventHandler?         RunningChanged;
        internal event EventHandler<ChatPromptPreviewRequestedEventArgs>? PromptPreviewRequested;
        internal event EventHandler<ChatPromptPreviewRequestedEventArgs>? NextPromptPreviewRequested;
        internal event EventHandler<ChatDiagramRequestedEventArgs>? DiagramRequested;

        internal sealed class ChatPromptPreviewRequestedEventArgs : EventArgs
        {
            public ChatPromptPreviewRequestedEventArgs(ChatPanelPromptPreview preview, string tabTitle)
            {
                Preview = preview;
                TabTitle = tabTitle;
            }

            public ChatPanelPromptPreview Preview { get; }
            public string TabTitle { get; }
        }

        internal sealed class ChatDiagramRequestedEventArgs : EventArgs
        {
            public ChatDiagramRequestedEventArgs(MermaidDiagramDefinition definition, string tabTitle)
            {
                Definition = definition;
                TabTitle = tabTitle;
            }

            public MermaidDiagramDefinition Definition { get; }
            public string TabTitle { get; }
        }

        // =====================================================================
        // Constructor
        // =====================================================================

        public ChatPanel()
        {
            SuspendLayout();

            // ?? Header ????????????????????????????????????????????????????????
            _lblTitle = new Label
            {
                Text = "AI CHAT", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = WallyTheme.TextMuted, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            _header = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = WallyTheme.Surface2 };
            _header.Controls.Add(_lblTitle);

            // ?? Row 1: mode + action buttons ??????????????????????????????????
            _btnModeAsk   = CreateModeButton("\uD83D\uDCAC Ask");
            _btnModeAgent = CreateModeButton("\uD83E\uDD16 Agent");
            _btnModeAsk.Click   += (_, _) => SetMode(ActionMode.Ask);
            _btnModeAgent.Click += (_, _) => SetMode(ActionMode.Agent);

            _btnClear = CreateBarButton("\u2715 Clear",
                "Clear conversation bubbles (does not delete history file)");
            _btnClear.Click += (_, _) => ClearMessages();

            _btnClearHistory = CreateBarButton("\uD83D\uDDD1 History",
                "Clear persisted conversation history and chat bubbles");
            _btnClearHistory.Click += (_, _) => { _environment?.History.ClearHistory(); ClearMessages(); };

            _btnFlowAuto = CreateModeButton("\u25B6 Auto");
            _btnFlowManual = CreateModeButton("\u23ED Manual");
            _btnFlowAuto.Click += (_, _) => SetPacingMode(ChatPacingMode.Auto);
            _btnFlowManual.Click += (_, _) => SetPacingMode(ChatPacingMode.Manual);
            _btnNextStep = CreateBarButton("\u23ED Next", "Run exactly one more step or iteration in manual mode.");
            _btnNextStep.Click += OnNextStepClick;
            _btnPreviewPrompt = CreateBarButton("\uD83D\uDD0D Prompt", "Open a center tab showing the current chat prompt preview.");
            _btnPreviewPrompt.Click += OnPreviewPromptClick;
            _btnPreviewNextPrompt = CreateBarButton("\u27A1 Next Prompt", "Open a center tab showing the next pending prompt in the active manual session.");
            _btnPreviewNextPrompt.Click += OnPreviewNextPromptClick;
            _btnDiagram = CreateBarButton("\uD83D\uDDFA Diagram", "Open a center tab showing the execution diagram for the current chat configuration.");
            _btnDiagram.Click += OnDiagramClick;

            // Left cluster � mode toggle buttons
            var modeLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = Color.Transparent, Padding = new Padding(6, 4, 0, 4)
            };
            modeLeft.Controls.Add(_btnModeAsk);
            modeLeft.Controls.Add(_btnModeAgent);

            // Right cluster � clear buttons
            var modeRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = Color.Transparent, Padding = new Padding(0, 4, 6, 4)
            };
            modeRight.Controls.Add(_btnClear);
            modeRight.Controls.Add(_btnClearHistory);

            _modeBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = WallyTheme.Surface1 };
            _modeBar.Controls.Add(modeRight);   // Right must be added before Left
            _modeBar.Controls.Add(modeLeft);

            var flowLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = Color.Transparent, Padding = new Padding(6, 4, 0, 4)
            };
            flowLeft.Controls.Add(_btnFlowAuto);
            flowLeft.Controls.Add(_btnFlowManual);

            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = Color.Transparent, Padding = new Padding(0, 4, 6, 4)
            };
            flowRight.Controls.Add(_btnNextStep);
            flowRight.Controls.Add(_btnPreviewPrompt);
            flowRight.Controls.Add(_btnPreviewNextPrompt);
            flowRight.Controls.Add(_btnDiagram);

            _flowBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = WallyTheme.Surface2 };
            _flowBar.Controls.Add(flowRight);
            _flowBar.Controls.Add(flowLeft);

            // ?? Row 2: selector dropdowns ?????????????????????????????????????
            (_btnActorDd,   _mnuActor)   = CreateSelectorPair();
            (_btnLoopDd,    _mnuLoop)    = CreateSelectorPair();
            (_btnModelDd,   _mnuModel)   = CreateSelectorPair();

            // Wrapping FlowLayoutPanel � items wrap onto a new line when the
            // panel is narrow; there is NO overflow chevron.
            var selectorsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,                        // ? key: wrap not overflow
                BackColor = WallyTheme.Surface2,
                Padding = new Padding(4, 3, 4, 3)
            };

            void AddGroup(string labelText, Button btn)
            {
                selectorsFlow.Controls.Add(new Label
                {
                    Text = labelText, AutoSize = true,
                    Font = WallyTheme.FontUISmallBold, ForeColor = WallyTheme.TextMuted,
                    BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(4, 6, 2, 0)
                });
                selectorsFlow.Controls.Add(btn);
            }
            AddGroup("Actor",   _btnActorDd);
            AddGroup("Loop",    _btnLoopDd);
            AddGroup("Model",   _btnModelDd);

            // The outer panel auto-sizes its height so all wrapped rows are
            // always visible regardless of how narrow the panel gets.
            _selectorsBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = WallyTheme.Surface2 };
            _selectorsBar.Controls.Add(selectorsFlow);

            void SyncSelectorsHeight()
            {
                selectorsFlow.Size = _selectorsBar.ClientSize;
                int needed = selectorsFlow.GetPreferredSize(
                    new Size(_selectorsBar.ClientSize.Width, int.MaxValue)).Height;
                _selectorsBar.Height = Math.Max(30, needed + 4);
            }
            _selectorsBar.Resize      += (_, _) => SyncSelectorsHeight();
            selectorsFlow.SizeChanged += (_, _) => SyncSelectorsHeight();

            // ?? Empty-state placeholder ???????????????????????????????????????
            _lblEmptyState = new Label
            {
                Text =
                    "\U0001F4AC\n\nType a message to start a conversation.\n\n" +
                    "\uD83D\uDCAC Ask \u2014 read-only, text response only\n" +
                    "\uD83E\uDD16 Agent \u2014 can make file changes; select a Runbook for multi-step pipelines\n" +
                    "\u25B6 Auto or \u23ED Manual \u2014 run fully or step-by-step\n\n" +
                    "Use the Actor, Loop, and Model dropdowns\nto customise each request.",
                Dock = DockStyle.Fill, ForeColor = WallyTheme.TextDisabled,
                BackColor = WallyTheme.Surface0, Font = WallyTheme.FontUI,
                TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
            };

            // ?? Message flow ??????????????????????????????????????????????????
            _messagesFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly,
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Padding = new Padding(12, 12, 12, 12), BackColor = WallyTheme.Surface0
            };

            _messagesContainer = ThemedEditorFactory.CreateScrollableSurface();
            _messagesContainer.Controls.Add(_messagesFlow);
            _messagesContainer.Controls.Add(_lblEmptyState);
            _messagesContainer.Controls.SetChildIndex(_lblEmptyState, 1);
            _messagesContainer.Controls.SetChildIndex(_messagesFlow,  0);
            _messagesContainer.Resize += OnMessagesResize;

            // ?? Status bar ????????????????????????????????????????????????????
            _lblStatus = new Label
            {
                Dock = DockStyle.Bottom, Height = 22, Text = "  Ready",
                ForeColor = WallyTheme.TextMuted, BackColor = WallyTheme.Surface2,
                Font = WallyTheme.FontUISmall, TextAlign = ContentAlignment.MiddleLeft
            };

            // ?? Input area ????????????????????????????????????????????????????
            _txtInput = ThemedEditorFactory.CreateInputTextArea(wordWrap: true, acceptsTab: false, backColor: WallyTheme.Surface2);
            _txtInput.KeyDown   += OnInputKeyDown;
            _txtInput.GotFocus  += (_, _) => _inputBorder.BackColor = WallyTheme.BorderFocused;
            _txtInput.LostFocus += (_, _) => _inputBorder.BackColor = WallyTheme.Border;

            _btnSend   = CreateActionButton("Send  \u23CE", WallyTheme.Surface3);
            _btnSend.Click += OnSendClick;
            _btnCancel = CreateActionButton("Stop  \u25A0", WallyTheme.Surface3);
            _btnCancel.Visible = false;
            _btnCancel.Click += (_, _) => _cts?.Cancel();

            _lblHint = new Label
            {
                Dock = DockStyle.Bottom, Height = 16,
                Text = "Enter to send \u00B7 Shift+Enter for new line \u00B7 Esc to cancel",
                ForeColor = WallyTheme.TextDisabled, BackColor = WallyTheme.Surface1,
                Font = new Font("Segoe UI", 7.5f), TextAlign = ContentAlignment.MiddleCenter
            };

            _inputBorder = new Panel
            {
                Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = WallyTheme.Border
            };
            _inputBorder.Controls.Add(_txtInput);

            var inputContent = new Panel { Dock = DockStyle.Fill };
            inputContent.Controls.Add(_inputBorder);
            inputContent.Controls.Add(_btnCancel);
            inputContent.Controls.Add(_btnSend);

            _inputArea = new Panel
            {
                Dock = DockStyle.Bottom, Height = 90,
                Padding = new Padding(12, 8, 12, 4), BackColor = WallyTheme.Surface1
            };
            _inputArea.Controls.Add(inputContent);
            _inputArea.Controls.Add(_lblHint);

            // ?? Assembly (Fill first; Bottom; then Top rows top-to-bottom) ????
            Controls.Add(_messagesContainer);   // Fill
            Controls.Add(_lblStatus);           // Bottom
            Controls.Add(_inputArea);           // Bottom
            Controls.Add(_selectorsBar);        // Top � row 2 (added before row 1)
            Controls.Add(_flowBar);             // Top � row 2
            Controls.Add(_modeBar);             // Top � row 1
            Controls.Add(_header);              // Top � topmost

            BackColor = WallyTheme.Surface0;
            ForeColor = WallyTheme.TextPrimary;
            ResumeLayout(true);

            SetMode(ActionMode.Ask);
            SetPacingMode(ChatPacingMode.Auto);
            SetWorkspaceLoaded(false);
        }

        // =====================================================================
        // Factory helpers
        // =====================================================================

        /// <summary>
        /// Creates a themed Button + ContextMenuStrip dropdown pair.
        /// The button opens the menu on click; items call back with the raw value.
        /// No ToolStrip is involved � no overflow chevron is possible.
        /// </summary>
        private static (Button btn, ContextMenuStrip menu) CreateSelectorPair()
        {
            var menu = new ContextMenuStrip
            {
                BackColor      = WallyTheme.Surface2,
                ForeColor      = WallyTheme.TextPrimary,
                Font           = WallyTheme.FontUISmall,
                ShowImageMargin = false,
                // No custom Renderer � the default system renderer respects
                // BackColor/ForeColor set at the strip level and on each item.
                // A custom ProfessionalRenderer or ToolStripProfessionalRenderer
                // ignores per-item colours and paints items invisible against
                // our dark theme background.
            };

            var btn = new Button
            {
                Text = "", AutoSize = false, Width = 130, Height = 22,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3, ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUISmall, TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand, Margin = new Padding(0, 1, 6, 1),
                Padding = new Padding(4, 0, 16, 0), Tag = menu
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;

            // Small ? arrow drawn at the right edge
            btn.Paint += (s, e) =>
            {
                var b = (Button)s!;
                int ax = b.Width - 13;
                int ay = (b.Height - 5) / 2;
                using var brush = new SolidBrush(b.Enabled ? WallyTheme.TextMuted : WallyTheme.TextDisabled);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPolygon(brush, new[]
                {
                    new Point(ax,     ay),
                    new Point(ax + 7, ay),
                    new Point(ax + 3, ay + 5)
                });
            };

            btn.Click += (s, _) =>
            {
                var b = (Button)s!;
                // Show(control, point) sets SourceControl correctly so the menu
                // is owned by the button's window � point is in control-client coords.
                ((ContextMenuStrip)b.Tag!).Show(b, new Point(0, b.Height));
            };

            return (btn, menu);
        }

        /// <summary>
        /// Rebuilds the items of a selector <see cref="ContextMenuStrip"/>.
        /// A check mark tracks the current selection.  When the user picks the
        /// item that matches <paramref name="defaultValue"/> the button label
        /// shows <c>{value} (default)</c>; any other pick shows the raw name.
        /// The raw value is always passed to <paramref name="onSelected"/> �
        /// the suffix is display-only.
        /// </summary>
        private static void PopulateSelector(
            Button btn, ContextMenuStrip menu,
            IEnumerable<string> items, string? selectedValue,
            Action<string?> onSelected, string? defaultValue = null)
        {
            menu.Items.Clear();
            foreach (string item in items)
            {
                var mi = new ToolStripMenuItem(item)
                {
                    // Colours are inherited from the ContextMenuStrip's BackColor/ForeColor.
                    // Do not set per-item overrides � the system renderer ignores them and
                    // it causes items to render as invisible against the dark background.
                    Font    = WallyTheme.FontUISmall,
                    Checked = string.Equals(item, selectedValue, StringComparison.OrdinalIgnoreCase)
                };
                mi.Click += (s, _) =>
                {
                    string text = ((ToolStripMenuItem)s!).Text;
                    bool isDefault = defaultValue != null &&
                                     string.Equals(text, defaultValue, StringComparison.OrdinalIgnoreCase);
                    btn.Text = isDefault ? $"{text} (default)" : text;
                    foreach (ToolStripMenuItem other in menu.Items.OfType<ToolStripMenuItem>())
                        other.Checked = string.Equals(other.Text, text, StringComparison.Ordinal);
                    onSelected(text);
                };
                menu.Items.Add(mi);
            }
        }

        private static Button CreateModeButton(string text)
        {
            var btn = new Button
            {
                Text = text, Width = 90, Height = 26, FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextSecondary,
                Font = WallyTheme.FontUISmallBold, Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0), TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface3;
            return btn;
        }

        private static Button CreateBarButton(string text, string tooltip)
        {
            var btn = new Button
            {
                Text = text, Width = 82, Height = 26, FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2, ForeColor = WallyTheme.TextSecondary,
                Font = WallyTheme.FontUISmall, Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0), TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface3;
            new ToolTip().SetToolTip(btn, tooltip);
            return btn;
        }

        private static Button CreateActionButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text = text, Dock = DockStyle.Right, Width = 80, FlatStyle = FlatStyle.Flat,
                BackColor = backColor, ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUIBold, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }

        private void SetPacingMode(ChatPacingMode mode)
        {
            _pacingMode = mode;

            foreach (var btn in new[] { _btnFlowAuto, _btnFlowManual })
            {
                btn.BackColor = WallyTheme.Surface2;
                btn.ForeColor = WallyTheme.TextSecondary;
                btn.FlatAppearance.BorderColor = WallyTheme.Border;
            }

            Button active = mode == ChatPacingMode.Manual ? _btnFlowManual : _btnFlowAuto;
            active.BackColor = WallyTheme.Surface4;
            active.ForeColor = WallyTheme.TextPrimary;
            active.FlatAppearance.BorderColor = WallyTheme.TextMuted;
            UpdateFlowButtons();
        }

        // =====================================================================
        // Mode management
        // =====================================================================

        private void SetMode(ActionMode mode)
        {
            _currentMode = mode;

            foreach (var btn in new[] { _btnModeAsk, _btnModeAgent })
            {
                btn.BackColor = WallyTheme.Surface2;
                btn.ForeColor = WallyTheme.TextSecondary;
                btn.FlatAppearance.BorderColor = WallyTheme.Border;
            }
            Button active = mode == ActionMode.Agent ? _btnModeAgent : _btnModeAsk;
            active.BackColor = WallyTheme.Surface4;
            active.ForeColor = WallyTheme.TextPrimary;
            active.FlatAppearance.BorderColor = WallyTheme.TextMuted;

            _lblTitle.Text = mode switch
            {
                ActionMode.Ask   => "AI CHAT \u2014 Ask \u2014 read-only",
                ActionMode.Agent => "AI CHAT \u2014 Agent \u2014 can make file changes",
                _                => "AI CHAT"
            };
            _lblTitle.ForeColor = WallyTheme.TextMuted;

            bool inputEnabled = _workspaceLoaded && !_isRunning;
            bool runbookOn    = inputEnabled && mode == ActionMode.Agent;

            _btnActorDd.Enabled   = inputEnabled;
            _btnLoopDd.Enabled    = inputEnabled;
            _btnModelDd.Enabled   = inputEnabled;

            if (!_isRunning && _workspaceLoaded)
            {
                _btnSend.BackColor = WallyTheme.Surface3;
                _btnSend.Enabled   = true;
                _txtInput.ReadOnly = false;
            }

            UpdateFlowButtons();
        }

        // =====================================================================
        // Wrapper resolution
        // =====================================================================

        /// <summary>
        /// Resolves the wrapper for the current mode, respecting in order:
        /// 1. The selected actor's <c>PreferredWrapper</c> and <c>AllowedWrappers</c>
        ///    constraint (actor-level capability policy).
        /// 2. <c>WallyConfig.DefaultWrapper</c> when it exists and its
        ///    <c>CanMakeChanges</c> matches the active mode.
        /// 3. The first loaded wrapper whose <c>CanMakeChanges</c> matches the mode
        ///    (original behaviour � unchanged when no config override is present).
        /// </summary>
        private string? ResolveWrapperForMode()
        {
            if (_environment?.HasWorkspace != true) return null;

            var wrappers   = _environment.Workspace!.LlmWrappers;
            bool wantAgent = _currentMode == ActionMode.Agent;

            // 1 � Actor-level preference / allow-list
            if (!string.IsNullOrEmpty(_selectedActor))
            {
                var actor = _environment.GetActor(_selectedActor!);
                if (actor != null)
                {
                    // Try actor's preferred wrapper first
                    if (!string.IsNullOrWhiteSpace(actor.PreferredWrapper))
                    {
                        var preferred = wrappers.FirstOrDefault(w =>
                            string.Equals(w.Name, actor.PreferredWrapper, StringComparison.OrdinalIgnoreCase));
                        if (preferred != null && preferred.CanMakeChanges == wantAgent && actor.IsWrapperAllowed(preferred.Name))
                            return preferred.Name;
                    }

                    // Try first entry in AllowedWrappers that matches the mode
                    if (actor.AllowedWrappers.Count > 0)
                    {
                        foreach (string name in actor.AllowedWrappers)
                        {
                            var w = wrappers.FirstOrDefault(x =>
                                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                            if (w != null && w.CanMakeChanges == wantAgent)
                                return w.Name;
                        }
                        // If no mode-matching allowed wrapper, use any allowed wrapper
                        foreach (string name in actor.AllowedWrappers)
                        {
                            if (wrappers.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                                return name;
                        }
                    }
                }
            }

            // 2 � WallyConfig.DefaultWrapper when its CanMakeChanges matches the mode
            string? configDefault = _environment.Workspace!.Config.DefaultWrapper;
            if (!string.IsNullOrWhiteSpace(configDefault))
            {
                var defWrapper = wrappers.FirstOrDefault(w =>
                    string.Equals(w.Name, configDefault, StringComparison.OrdinalIgnoreCase));
                if (defWrapper != null && defWrapper.CanMakeChanges == wantAgent)
                    return defWrapper.Name;
                // DefaultWrapper set but wrong mode � fall through to capability search
            }

            // 3 � Capability search (original behaviour)
            var match = wrappers.FirstOrDefault(w => w.CanMakeChanges == wantAgent);
            return match?.Name ?? (wrappers.Count > 0 ? wrappers[0].Name : null);
        }

        // =====================================================================
        // Public API
        // =====================================================================

        public void BindEnvironment(WallyEnvironment environment)
        {
            _environment = environment;
            RefreshActorList();
            RefreshLoopList();
            RefreshModelList();
        }

        public bool IsRunning => _isRunning;
        public void Cancel()   => _cts?.Cancel();

        public void SetWorkspaceLoaded(bool loaded)
        {
            if (InvokeRequired) { Invoke(() => SetWorkspaceLoaded(loaded)); return; }

            _workspaceLoaded = loaded;
            bool inputEnabled = loaded && !_isRunning;

            _btnActorDd.Enabled      = inputEnabled;
            _btnLoopDd.Enabled       = inputEnabled;
            _btnModelDd.Enabled      = inputEnabled;
            _btnClear.Enabled        = loaded;
            _btnClearHistory.Enabled = loaded;
            _btnSend.Enabled         = inputEnabled;
            _txtInput.ReadOnly       = !inputEnabled;
            _btnModeAsk.Enabled      = loaded;
            _btnModeAgent.Enabled    = loaded;

            _txtInput.BackColor    = loaded ? WallyTheme.Surface2 : WallyTheme.Surface0;
            _txtInput.ForeColor    = loaded ? WallyTheme.TextPrimary : WallyTheme.TextDisabled;
            _inputBorder.BackColor = loaded ? WallyTheme.Border : WallyTheme.BorderSubtle;
            _inputArea.BackColor   = loaded ? WallyTheme.Surface1 : WallyTheme.Surface0;
            _btnSend.BackColor     = loaded ? WallyTheme.Surface3 : WallyTheme.Surface2;

            if (!loaded)
            {
                _lblEmptyState.Text  =
                    "\U0001F4AC\n\nNo workspace loaded.\n\n" +
                    "Use File \u2192 Open Workspace or File \u2192 Setup New Workspace\n" +
                    "to get started.";
                _lblStatus.Text      = "  No workspace";
                _lblStatus.ForeColor = WallyTheme.TextDisabled;
            }
            else
            {
                _lblEmptyState.Text =
                    "\U0001F4AC\n\nType a message to start a conversation.\n\n" +
                    "\uD83D\uDCAC Ask \u2014 read-only, text response only\n" +
                    "\uD83E\uDD16 Agent \u2014 can make file changes; select a Runbook for multi-step pipelines\n" +
                    "\u25B6 Auto or \u23ED Manual \u2014 run fully or step-by-step\n\n" +
                    "Use the Actor, Loop, and Model dropdowns\nto customise each request.";
                if (!_isRunning)
                {
                    _lblStatus.Text      = "  Ready";
                    _lblStatus.ForeColor = WallyTheme.TextMuted;
                }
                LoadHistory();
            }

            if (!loaded)
                _manualSession = null;

            UpdateEmptyState();
            UpdateFlowButtons();
        }

        public void RefreshActorList()
        {
            if (InvokeRequired) { Invoke(RefreshActorList); return; }

            var items = new List<string> { "None" };
            if (_environment?.HasWorkspace == true)
                items.AddRange(_environment.Actors.Select(a => a.Name));

            _selectedActor = null;
            _defaultActor  = null;
            string buttonText = "None";

            if (_environment?.HasWorkspace == true)
            {
                var cfg = _environment.Workspace!.Config;
                if (!string.IsNullOrEmpty(cfg.DefaultActorName) &&
                    _environment.Actors.Any(a => a.Name == cfg.DefaultActorName))
                {
                    _selectedActor = cfg.DefaultActorName;
                    _defaultActor  = cfg.DefaultActorName;
                    buttonText     = $"{_selectedActor} (default)";
                }
            }

            _btnActorDd.Text = buttonText;
            PopulateSelector(_btnActorDd, _mnuActor, items, _selectedActor ?? "None", value =>
            {
                _selectedActor = string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)
                    ? null : value;
                // When actor changes, also update the loop dropdown to reflect
                // the newly selected actor's preferred loop (if any).
                RefreshLoopList();
            }, defaultValue: _defaultActor);
        }

        public void RefreshLoopList()
        {
            if (InvokeRequired) { Invoke(RefreshLoopList); return; }

            var items = new List<string> { "(none)" };
            if (_environment?.HasWorkspace == true)
                items.AddRange(_environment.Loops.Select(l => l.Name));

            _selectedLoop = null;
            _defaultLoop  = null;
            string buttonText = "(none)";

            if (_environment?.HasWorkspace == true)
            {
                // Prefer the selected actor's own preferred loop (if it is loaded
                // and on the actor's allow-list).
                string? actorPreferred = null;
                if (!string.IsNullOrEmpty(_selectedActor))
                {
                    var actor = _environment.GetActor(_selectedActor!);
                    if (actor != null && !string.IsNullOrWhiteSpace(actor.PreferredLoop))
                    {
                        string p = actor.PreferredLoop!;
                        if (actor.IsLoopAllowed(p) &&
                            _environment.Loops.Any(l => l.Name == p))
                            actorPreferred = p;
                    }
                }

                // Fall back to workspace resolved default loop.
                var cfg = _environment.Workspace!.Config;
                string? resolved = actorPreferred
                    ?? (string.IsNullOrEmpty(cfg.ResolvedDefaultLoop) ? null : cfg.ResolvedDefaultLoop);

                if (!string.IsNullOrEmpty(resolved) &&
                    _environment.Loops.Any(l => l.Name == resolved))
                {
                    _selectedLoop = resolved;
                    _defaultLoop  = resolved;
                    buttonText    = $"{_selectedLoop} (default)";
                }
            }

            _btnLoopDd.Text = buttonText;
            PopulateSelector(_btnLoopDd, _mnuLoop, items, _selectedLoop, value =>
            {
                _selectedLoop = string.Equals(value, "(none)", StringComparison.OrdinalIgnoreCase)
                    ? null : value;
            }, defaultValue: _defaultLoop);
        }

        public void RefreshModelList()
        {
            if (InvokeRequired) { Invoke(RefreshModelList); return; }

            var items = new List<string>();
            if (_environment?.HasWorkspace == true)
                items.AddRange(_environment.Workspace!.Config.DefaultModels);

            _selectedModel = null;
            _defaultModel  = null;

            if (_environment?.HasWorkspace == true)
            {
                var cfg = _environment.Workspace!.Config;
                if (!string.IsNullOrEmpty(cfg.DefaultModel) && cfg.DefaultModels.Contains(cfg.DefaultModel))
                    _selectedModel = cfg.DefaultModel;
                else if (cfg.DefaultModels.Count > 0)
                    _selectedModel = cfg.DefaultModels[0];
                if (_selectedModel != null) _defaultModel = _selectedModel;
            }

            _btnModelDd.Text = _defaultModel != null ? $"{_defaultModel} (default)" : "(default)";
            PopulateSelector(_btnModelDd, _mnuModel, items, _selectedModel, value =>
            {
                _selectedModel = value;
            }, defaultValue: _defaultModel);
        }

        public void RefreshRunbookList()
        {
            // Runbook selection is handled by the main form's runbook toolbar.
            // This method exists so callers can refresh all dropdown lists uniformly.
        }

        public void ClearMessages()
        {
            if (InvokeRequired) { Invoke(ClearMessages); return; }

            _messagesFlow.SuspendLayout();
            while (_messagesFlow.Controls.Count > 0)
            {
                var c = _messagesFlow.Controls[0];
                _messagesFlow.Controls.RemoveAt(0);
                c.Dispose();
            }
            _messagesFlow.ResumeLayout(true);
            _txtInput.Clear();
            _txtInput.SelectionColor = WallyTheme.TextPrimary;
            _txtInput.SelectionFont  = WallyTheme.FontUI;
            _manualSession = null;
            UpdateEmptyState();
            UpdateFlowButtons();
        }

        private void LoadHistory()
        {
            if (_environment?.HasWorkspace != true) return;
            var turns = _environment.History.GetAllTurns();
            if (turns.Count == 0) return;

            _messagesFlow.SuspendLayout();
            int start = Math.Max(0, turns.Count - 50);
            for (int i = start; i < turns.Count; i++)
            {
                var turn = turns[i];
                int w    = Math.Max(200, _messagesContainer.ClientSize.Width - 48);
                string userLabel = turn.ActorName != null ? $"You \u2192 {turn.ActorName}" : "You";
                _messagesFlow.Controls.Add(new ChatBubble(userLabel, turn.Prompt, MessageKind.User, w));
                string responseLabel = turn.ActorName ?? "AI";
                var    responseKind  = turn.IsError ? MessageKind.Error : MessageKind.Actor;
                _messagesFlow.Controls.Add(new ChatBubble(responseLabel, turn.Response, responseKind, w));
            }
            _messagesFlow.ResumeLayout(true);

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
                if (!_isRunning) _ = SendMessageAsync();
            }
            else if (e.KeyCode == Keys.Escape && _isRunning)
            {
                e.SuppressKeyPress = true;
                _cts?.Cancel();
            }
        }

        private void OnSendClick(object? sender, EventArgs e)
        {
            if (!_isRunning) _ = SendMessageAsync();
        }

        private void OnNextStepClick(object? sender, EventArgs e)
        {
            if (!_isRunning) _ = ContinueManualSessionAsync();
        }

        private void OnPreviewPromptClick(object? sender, EventArgs e)
        {
            if (_environment?.HasWorkspace != true)
                return;

            try
            {
                ChatPanelRequest request = BuildCurrentChatRequest(preferActiveSessionWhenInputBlank: true);
                if (string.IsNullOrWhiteSpace(request.DisplayPrompt))
                {
                    AddMessage("System", "Enter a prompt before opening a prompt preview tab.", MessageKind.Error);
                    return;
                }

                var preview = ChatPanelExecutionService.BuildPromptPreview(_environment, request);
                PromptPreviewRequested?.Invoke(this,
                    new ChatPromptPreviewRequestedEventArgs(preview, $"Prompt Preview - {request.DisplayLabel}"));
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Prompt preview failed: {ex.Message}", MessageKind.Error);
            }
        }

        private void OnPreviewNextPromptClick(object? sender, EventArgs e)
        {
            if (_manualSession == null)
            {
                AddMessage("System", "No manual chat session is active. Start one in Manual mode first.", MessageKind.Error);
                return;
            }

            try
            {
                var preview = _manualSession.BuildNextPromptPreview();
                NextPromptPreviewRequested?.Invoke(this,
                    new ChatPromptPreviewRequestedEventArgs(preview, $"Next Prompt - {_manualSession.ResolvedRequest.Request.DisplayLabel}"));
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Next prompt preview failed: {ex.Message}", MessageKind.Error);
            }
        }

        private void OnDiagramClick(object? sender, EventArgs e)
        {
            if (_environment?.HasWorkspace != true)
                return;

            try
            {
                ChatPanelRequest request = BuildCurrentChatRequest(preferActiveSessionWhenInputBlank: true, allowEmptyPrompt: true);
                var definition = ChatPanelExecutionService.BuildDiagramDefinition(_environment, request);
                DiagramRequested?.Invoke(this,
                    new ChatDiagramRequestedEventArgs(definition, $"Chat Diagram - {request.DisplayLabel}"));
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Diagram generation failed: {ex.Message}", MessageKind.Error);
            }
        }

        private async Task SendMessageAsync()
        {
            if (_isRunning) return;

            if (_environment?.HasWorkspace != true || !_workspaceLoaded)
            {
                AddMessage("System", "No workspace loaded. Use File \u2192 Open Workspace first.", MessageKind.Error);
                return;
            }

            ChatPanelRequest request = BuildCurrentChatRequest(preferActiveSessionWhenInputBlank: false);
            if (string.IsNullOrWhiteSpace(request.DisplayPrompt)) return;

            if (_pacingMode == ChatPacingMode.Manual)
            {
                await StartManualSessionAsync(request).ConfigureAwait(true);
                return;
            }

            await RunAutomaticSessionAsync(request).ConfigureAwait(true);
        }

        private async Task RunAutomaticSessionAsync(ChatPanelRequest request)
        {
            _manualSession = null;
            ChatPanelRequest executionRequest = PrepareInvestigationExecutionRequest(request);

            ChatPanelResolvedRequest resolved;
            try
            {
                resolved = ChatPanelExecutionService.ResolveRequest(_environment!, executionRequest);
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Error: {ex.Message}", MessageKind.Error);
                return;
            }

            string? actorName = resolved.DirectMode ? null : resolved.ActorLabel;
            string? loopName = resolved.LoopDefinition?.Name;
            string? modelOverride = resolved.ResolvedModel;
            string? wrapperName = resolved.ResolvedWrapperName;
            bool directMode = resolved.DirectMode;
            bool isLooped = resolved.IsLooped;
            string label = directMode ? "AI" : resolved.ActorLabel;

            string cmdText = string.Join(" ",
                new List<string>(new[] { "run", $"\"{executionRequest.DisplayPrompt}\"" })
                .Concat(!directMode           ? new[] { $"-a {actorName}" }      : Array.Empty<string>())
                .Concat(isLooped              ? new[] { $"-l {loopName}" }       : Array.Empty<string>())
                .Concat(modelOverride != null ? new[] { $"-m {modelOverride}" }  : Array.Empty<string>())
                .Concat(wrapperName   != null ? new[] { $"-w {wrapperName}" }    : Array.Empty<string>())
                .Concat(executionRequest.NoHistory ? new[] { "--no-history" }    : Array.Empty<string>()));
            CommandIssued?.Invoke(this, cmdText);

            AddMessage("You", request.DisplayPrompt, MessageKind.User);
            _txtInput.Clear();
            _txtInput.SelectionColor = WallyTheme.TextPrimary;
            _txtInput.SelectionFont  = WallyTheme.FontUI;

            _cts = new CancellationTokenSource();
            string modeLabel = _currentMode == ActionMode.Agent ? "Agent" : "Ask";
            string runLabel  = isLooped ? $"{modeLabel}: {label} [{loopName}]" : $"{modeLabel}: {label}";
            SetRunning(true, runLabel);

            try
            {
                var token = _cts.Token;

                // Run on a background thread so the UI thread (and its message pump)
                // remains free � this keeps the progress bar animating and the Stop
                // button responsive, exactly as CommandPanel.RunCommandAsync does.
                var results = await Task.Run(
                    () => WallyCommands.HandleRunTyped(
                        _environment!, executionRequest.DisplayPrompt, actorName, modelOverride,
                        loopName: loopName, wrapper: wrapperName,
                        noHistory: executionRequest.NoHistory, cancellationToken: token),
                    token).ConfigureAwait(true); // ConfigureAwait(true) to resume on UI thread

                if (results.Count == 0)
                    AddMessage("System", "No response from AI.", MessageKind.Error);
                else if (results.Count == 1)
                    AddMessage(results[0].DisplayLabel(), results[0].Response, MessageKind.Actor);
                else
                {
                    foreach (var r in results)
                        AddMessage(r.DisplayLabel(), r.Response, MessageKind.Actor);
                    AddMessage("System", $"Pipeline complete \u2014 {results.Count} step(s).", MessageKind.System);
                }

                ShowPendingInvestigationInteraction(loopName);
            }
            catch (OperationCanceledException) { AddMessage("System", "Cancelled.", MessageKind.Error); }
            catch (Exception ex)               { AddMessage("System", $"Error: {ex.Message}", MessageKind.Error); }
            finally
            {
                SetRunning(false);
                _cts?.Dispose();
                _cts = null;
                UpdateFlowButtons();
            }
        }

        private async Task StartManualSessionAsync(ChatPanelRequest request)
        {
            ChatPanelRequest executionRequest = PrepareInvestigationExecutionRequest(request);

            try
            {
            _manualSession = ChatPanelExecutionService.CreateSession(_environment!, executionRequest);
            }
            catch (Exception ex)
            {
                AddMessage("System", $"Error: {ex.Message}", MessageKind.Error);
                return;
            }

            string cmdText = string.Join(" ",
                new List<string>(new[] { "run", $"\"{executionRequest.DisplayPrompt}\"" })
                .Concat(!string.IsNullOrWhiteSpace(executionRequest.ActorName) ? new[] { $"-a {executionRequest.ActorName}" } : Array.Empty<string>())
                .Concat(!string.IsNullOrWhiteSpace(executionRequest.LoopName)  ? new[] { $"-l {executionRequest.LoopName}" }  : Array.Empty<string>())
                .Concat(!string.IsNullOrWhiteSpace(executionRequest.ModelOverride) ? new[] { $"-m {executionRequest.ModelOverride}" } : Array.Empty<string>())
                .Concat(executionRequest.NoHistory ? new[] { "--no-history" } : Array.Empty<string>()));
            CommandIssued?.Invoke(this, cmdText);

            AddMessage("You", request.DisplayPrompt, MessageKind.User);
            _txtInput.Clear();
            _txtInput.SelectionColor = WallyTheme.TextPrimary;
            _txtInput.SelectionFont = WallyTheme.FontUI;

            await ExecuteManualStepAsync(isFirstStep: true).ConfigureAwait(true);
        }

        private async Task ContinueManualSessionAsync()
        {
            if (_manualSession == null)
                return;

            await ExecuteManualStepAsync(isFirstStep: false).ConfigureAwait(true);
        }

        private async Task ExecuteManualStepAsync(bool isFirstStep)
        {
            if (_manualSession == null)
                return;

            string modeLabel = _currentMode == ActionMode.Agent ? "Agent" : "Ask";
            string context = _manualSession.CurrentStatus;

            _cts = new CancellationTokenSource();
            SetRunning(true, $"{modeLabel}: {context}");

            try
            {
                WallyRunResult result = await _manualSession.ExecuteNextAsync(_cts.Token).ConfigureAwait(true);
                AddMessage(result.DisplayLabel(), result.Response, MessageKind.Actor);

                if (_manualSession.IsCompleted)
                {
                    AddMessage("System", BuildManualCompletionMessage(_manualSession), MessageKind.System);
                }
                else
                {
                    AddMessage("System", $"Manual step complete. Next: {_manualSession.CurrentStatus}.", MessageKind.System);
                }

                ShowPendingInvestigationInteraction(_manualSession.ResolvedRequest.LoopDefinition?.Name);
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
                UpdateFlowButtons();
            }
        }

        // =====================================================================
        // Message rendering
        // =====================================================================

        private void AddMessage(string sender, string text, MessageKind kind)
        {
            if (InvokeRequired) { Invoke(() => AddMessage(sender, text, kind)); return; }
            int w = Math.Max(200, _messagesContainer.ClientSize.Width - 48);
            var bubble = new ChatBubble(sender, text, kind, w);
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
            {
                if (c is ChatBubble b) b.SetBubbleWidth(w);
            }
            _messagesFlow.ResumeLayout(true);
        }

        private void UpdateEmptyState()
        {
            bool empty = _messagesFlow.Controls.Count == 0;
            _lblEmptyState.Visible = empty;
            if (empty) _lblEmptyState.BringToFront();
        }

        // =====================================================================
        // UI state
        // =====================================================================

        private void SetRunning(bool running, string? context = null)
        {
            if (InvokeRequired) { Invoke(() => SetRunning(running, context)); return; }

            _isRunning = running;
            _btnSend.Visible   = !running;
            _btnSend.Enabled   = !running && _workspaceLoaded;
            _btnCancel.Visible = running;

            bool inputEnabled = !running && _workspaceLoaded;
            _txtInput.ReadOnly    = !inputEnabled;
            _btnActorDd.Enabled   = inputEnabled;
            _btnLoopDd.Enabled    = inputEnabled;
            _btnModelDd.Enabled   = inputEnabled;
            _btnModeAsk.Enabled   = inputEnabled;
            _btnModeAgent.Enabled = inputEnabled;
            _btnFlowAuto.Enabled  = inputEnabled;
            _btnFlowManual.Enabled = inputEnabled;

            _lblStatus.Text      = running ? $"  \u26A1 {context}\u2026" : "  Ready";
            _lblStatus.ForeColor = running ? WallyTheme.TextSecondary : WallyTheme.TextMuted;

            if (!running && _workspaceLoaded) _btnSend.BackColor = WallyTheme.Surface3;
            UpdateFlowButtons();
            RunningChanged?.Invoke(this, EventArgs.Empty);
        }

        private ChatPanelRequest BuildCurrentChatRequest(bool preferActiveSessionWhenInputBlank, bool allowEmptyPrompt = false)
        {
            string prompt = _txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt) && preferActiveSessionWhenInputBlank && _manualSession != null)
                return _manualSession.ResolvedRequest.Request;

            if (string.IsNullOrWhiteSpace(prompt) && !allowEmptyPrompt)
                return new ChatPanelRequest();

            return new ChatPanelRequest
            {
                Prompt = prompt,
                ActorName = _selectedActor,
                LoopName = _selectedLoop,
                ModelOverride = string.IsNullOrWhiteSpace(_selectedModel) ? null : _selectedModel,
                Mode = _currentMode == ActionMode.Agent ? ChatPanelExecutionMode.Agent : ChatPanelExecutionMode.Ask
            };
        }

        private string BuildManualCompletionMessage(ChatPanelExecutionSession session)
        {
            string stopReason = session.StopReason ?? "Completed";
            return session.ResolvedRequest.LoopDefinition?.HasSteps == true
                ? $"Manual pipeline complete - {session.Results.Count} step(s)."
                : session.ResolvedRequest.LoopDefinition?.IsAgentLoop == true
                    ? $"Manual agent loop complete - {session.Results.Count} iteration(s), stop reason: {stopReason}."
                    : "Manual run complete.";
        }

        private ChatPanelRequest PrepareInvestigationExecutionRequest(ChatPanelRequest request)
        {
            if (_environment == null ||
                !InvestigationInteractionStore.IsInvestigationLoop(request.LoopName) ||
                string.IsNullOrWhiteSpace(request.DisplayPrompt))
            {
                return request;
            }

            if (InvestigationInteractionStore.TryRecordResponse(
                _environment,
                request.DisplayPrompt,
                "Forms ChatPanel",
                out InvestigationInteractionState? state))
            {
                AddMessage(
                    "System",
                    $"Recorded answer batch {state!.QuestionBatchId} for investigation {state.InvestigationId}.",
                    MessageKind.System);
                return request.WithPrompt(InvestigationInteractionStore.BuildResumePrompt(state));
            }

            return request;
        }

        private void ShowPendingInvestigationInteraction(string? loopName)
        {
            if (_environment == null || !InvestigationInteractionStore.IsInvestigationLoop(loopName))
                return;

            if (!InvestigationInteractionStore.TryLoadWaiting(_environment, out InvestigationInteractionState? state) ||
                state == null)
            {
                return;
            }

            AddMessage("System", InvestigationInteractionStore.BuildWaitingDisplayText(state), MessageKind.System);
        }

        private void UpdateFlowButtons()
        {
            bool canInspect = _workspaceLoaded && !_isRunning;
            bool hasPrompt = !string.IsNullOrWhiteSpace(_txtInput.Text) || _manualSession != null;
            bool hasManualSession = _manualSession != null;
            bool canContinueManual = hasManualSession && !_manualSession!.IsCompleted && _pacingMode == ChatPacingMode.Manual && !_isRunning;

            _btnPreviewPrompt.Enabled = canInspect && hasPrompt;
            _btnPreviewNextPrompt.Enabled = canInspect && hasManualSession;
            _btnDiagram.Enabled = _workspaceLoaded && !_isRunning;
            _btnNextStep.Enabled = canContinueManual;

            if (!_isRunning && _workspaceLoaded && _pacingMode == ChatPacingMode.Manual && hasManualSession)
            {
                _lblStatus.Text = _manualSession!.IsCompleted
                    ? $"  Manual session complete - {BuildManualCompletionMessage(_manualSession)}"
                    : $"  Manual ready - {_manualSession.CurrentStatus}. Next continues; Send starts a new run.";
                _lblStatus.ForeColor = _manualSession.IsCompleted ? WallyTheme.TextMuted : WallyTheme.TextSecondary;
            }
        }
    }

    // =========================================================================
    //  ChatBubble � custom owner-drawn message bubble with rounded corners
    // =========================================================================

    internal sealed class ChatBubble : Control
    {
        private readonly string    _sender;
        private readonly string    _body;
        private readonly MessageKind _kind;
        private readonly string    _timestamp;

        private const int BubbleRadius   = 8;
        private const int PadX           = 14;
        private const int PadY           = 10;
        private const int SenderHeight   = 18;
        private const int GapAfterSender = 4;
        private const int TimestampWidth = 50;

        private int _bodyHeight;

        public ChatBubble(string sender, string body, MessageKind kind, int width)
        {
            _sender    = sender;
            _body      = body;
            _kind      = kind;
            _timestamp = DateTime.Now.ToString("HH:mm");

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint            |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Margin = new Padding(0, 0, 0, 8);
            Width  = width;
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
            using (var path  = WallyTheme.RoundedRect(bubbleRect, BubbleRadius))
            using (var brush = new SolidBrush(bubbleBg))
                g.FillPath(brush, path);

            Color accentColor = _kind switch
            {
                MessageKind.User   => WallyTheme.Surface4,
                MessageKind.Error  => WallyTheme.Red,
                MessageKind.System => WallyTheme.TextMuted,
                _                  => WallyTheme.Border
            };
            using (var ab = new SolidBrush(accentColor))
                g.FillRectangle(ab, 0, BubbleRadius, 3, Height - BubbleRadius * 2);

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

            TextRenderer.DrawText(g, _body, WallyTheme.FontMono,
                new Rectangle(PadX, y, Width - PadX * 2, _bodyHeight),
                WallyTheme.TextPrimary,
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        }
    }
}
