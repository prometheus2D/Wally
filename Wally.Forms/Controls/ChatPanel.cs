using System;
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
    /// AI chat panel — right-side copilot conversation window.
    /// Sends prompts to Wally actors and renders the conversation as styled,
    /// rounded message bubbles with timestamps, sender badges, and proper
    /// word-wrapped body text. Supports actor/model selection, cancellation,
    /// and asynchronous execution.
    /// </summary>
    public sealed class ChatPanel : UserControl
    {
        // ?? Header ??????????????????????????????????????????????????????????

        private readonly Panel _header;
        private readonly Label _lblTitle;

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

        // ?? Events ??????????????????????????????????????????????????????????

        public event EventHandler<string>? CommandIssued;

        // ?? Constructor ?????????????????????????????????????????????????????

        public ChatPanel()
        {
            SuspendLayout();

            var renderer = new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false };

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
            _cboModel.ComboBox.Width = 130;

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

            // Apply dark styling to combo boxes after they're created.
            WallyTheme.StyleComboBox(_cboActor);
            WallyTheme.StyleComboBox(_cboModel);

            // ?? Empty state placeholder ??
            _lblEmptyState = new Label
            {
                Text = "\U0001F4AC\n\nSelect an actor and type a message to start a conversation.\n\n" +
                       "Your messages are sent to the actor\u2019s AI pipeline\nand responses appear here as chat bubbles.",
                Dock = DockStyle.Fill,
                ForeColor = WallyTheme.TextDisabled,
                BackColor = WallyTheme.Surface0,
                Font = WallyTheme.FontUI,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            // ?? Message area ??
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

            _messagesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = WallyTheme.Surface0
            };
            _messagesContainer.Controls.Add(_lblEmptyState);
            _messagesContainer.Controls.Add(_messagesFlow);
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

            // ?? Input area ??
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

            _btnSend = CreateActionButton("Send  \u23CE", WallyTheme.Accent);
            _btnSend.Click += OnSendClick;

            _btnCancel = CreateActionButton("Stop  \u25A0", WallyTheme.Red);
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

            // ?? Assembly (order: fill first, then bottom-up, then top-down) ??
            Controls.Add(_messagesContainer);   // Fill
            Controls.Add(_lblStatus);            // Bottom
            Controls.Add(_inputArea);            // Bottom
            Controls.Add(_toolbar);              // Top
            Controls.Add(_header);               // Top (above toolbar)

            BackColor = WallyTheme.Surface0;
            ForeColor = WallyTheme.TextPrimary;

            ResumeLayout(true);
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
                ForeColor = Color.White,
                Font = WallyTheme.FontUIBold,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(Math.Min(255, backColor.R + 25),
                               Math.Min(255, backColor.G + 25),
                               Math.Min(255, backColor.B + 25));
            return btn;
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment environment)
        {
            _environment = environment;
            RefreshActorList();
            RefreshModelList();
        }

        public void RefreshActorList()
        {
            _cboActor.Items.Clear();
            if (_environment?.HasWorkspace != true) return;
            foreach (var actor in _environment.Actors)
                _cboActor.Items.Add(actor.Name);
            if (_cboActor.Items.Count > 0)
                _cboActor.SelectedIndex = 0;
        }

        public void RefreshModelList()
        {
            _cboModel.Items.Clear();
            if (_environment?.HasWorkspace != true) return;
            var cfg = _environment.Workspace!.Config;
            _cboModel.Items.Add("");
            foreach (var model in cfg.Models)
                _cboModel.Items.Add(model);
            if (!string.IsNullOrEmpty(cfg.DefaultModel))
                _cboModel.Text = cfg.DefaultModel;
        }

        public void ClearMessages()
        {
            _messagesFlow.SuspendLayout();
            _messagesFlow.Controls.Clear();
            _messagesFlow.ResumeLayout(true);
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

            if (_environment?.HasWorkspace != true)
            {
                AddMessage("System", "No workspace loaded. Use the command line to run  setup <path>  first.", MessageKind.Error);
                return;
            }

            string actorName = _cboActor.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(actorName))
            {
                AddMessage("System", "No actor selected.", MessageKind.Error);
                return;
            }

            string? modelOverride = string.IsNullOrWhiteSpace(_cboModel.Text) ? null : _cboModel.Text.Trim();

            AddMessage("You", prompt, MessageKind.User);
            _txtInput.Clear();

            string cmdText = $"run {actorName} \"{prompt}\"" + (modelOverride != null ? $" -m {modelOverride}" : "");
            CommandIssued?.Invoke(this, cmdText);

            SetRunning(true, actorName);
            _cts = new CancellationTokenSource();

            try
            {
                var token = _cts.Token;
                var responses = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    if (!string.IsNullOrWhiteSpace(modelOverride))
                    {
                        var actor = _environment.GetActor(actorName);
                        if (actor != null) actor.ModelOverride = modelOverride;
                    }
                    return _environment.RunActor(prompt, actorName);
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
            finally
            {
                SetRunning(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        // ?? Message rendering ???????????????????????????????????????????????

        private void AddMessage(string sender, string text, MessageKind kind)
        {
            if (InvokeRequired) { Invoke(() => AddMessage(sender, text, kind)); return; }

            int bubbleWidth = Math.Max(200, _messagesFlow.ClientSize.Width - 32);
            var bubble = new ChatBubble(sender, text, kind, bubbleWidth);

            _messagesFlow.SuspendLayout();
            _messagesFlow.Controls.Add(bubble);
            _messagesFlow.ResumeLayout(true);

            _messagesContainer.ScrollControlIntoView(bubble);
            UpdateEmptyState();
        }

        private void OnMessagesResize(object? sender, EventArgs e)
        {
            int w = Math.Max(200, _messagesFlow.ClientSize.Width - 32);
            _messagesFlow.SuspendLayout();
            foreach (Control c in _messagesFlow.Controls)
                if (c is ChatBubble b) b.SetBubbleWidth(w);
            _messagesFlow.ResumeLayout(true);
        }

        private void UpdateEmptyState()
        {
            _lblEmptyState.Visible = _messagesFlow.Controls.Count == 0;
        }

        // ?? UI state ????????????????????????????????????????????????????????

        private void SetRunning(bool running, string? actorName = null)
        {
            if (InvokeRequired) { Invoke(() => SetRunning(running, actorName)); return; }
            _isRunning = running;
            _btnSend.Visible = !running;
            _btnCancel.Visible = running;
            _txtInput.ReadOnly = running;
            _cboActor.Enabled = !running;
            _cboModel.Enabled = !running;
            _lblStatus.Text = running ? $"  \u26A1 Running {actorName}\u2026" : "  Ready";
            _lblStatus.ForeColor = running ? WallyTheme.Accent : WallyTheme.TextMuted;
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

            // ?? Background bubble ??
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

            // ?? Accent bar on left edge ??
            Color accentColor = _kind switch
            {
                MessageKind.User => WallyTheme.Accent,
                MessageKind.Error => WallyTheme.Red,
                _ => WallyTheme.Green
            };
            using (var accentBrush = new SolidBrush(accentColor))
            {
                g.FillRectangle(accentBrush, 0, BubbleRadius, 3, Height - BubbleRadius * 2);
            }

            int y = PadY;

            // ?? Sender name ??
            Color senderColor = _kind switch
            {
                MessageKind.User => WallyTheme.SenderUser,
                MessageKind.Error => WallyTheme.SenderSystem,
                _ => WallyTheme.SenderActor
            };
            TextRenderer.DrawText(g, _sender, WallyTheme.FontUISmallBold,
                new Rectangle(PadX, y, Width - PadX * 2 - TimestampWidth, SenderHeight),
                senderColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // ?? Timestamp ??
            TextRenderer.DrawText(g, _timestamp, WallyTheme.FontUISmall,
                new Rectangle(Width - PadX - TimestampWidth, y, TimestampWidth, SenderHeight),
                WallyTheme.TextDisabled, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

            y += SenderHeight + GapAfterSender;

            // ?? Body text ??
            int textWidth = Width - PadX * 2;
            TextRenderer.DrawText(g, _body, WallyTheme.FontMono,
                new Rectangle(PadX, y, textWidth, _bodyHeight),
                WallyTheme.TextPrimary,
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        }
    }
}
