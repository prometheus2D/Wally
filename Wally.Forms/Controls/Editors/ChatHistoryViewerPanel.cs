using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Logging;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Read-only viewer for the conversation history (<c>conversation.jsonl</c>).
    /// <para>
    /// Left pane: scrollable list of conversation turns with actor/timestamp summary.
    /// Right pane: full details of the selected turn (prompt, response, metadata).
    /// Toolbar: actor filter dropdown, refresh, clear history, export.
    /// </para>
    /// </summary>
    public sealed class ChatHistoryViewerPanel : UserControl
    {
        // ?? Controls ????????????????????????????????????????????????????????

        private readonly Panel _headerPanel;
        private readonly Button _btnRefresh;
        private readonly Button _btnClear;
        private readonly Button _btnCopy;
        private readonly ComboBox _cboActorFilter;
        private readonly Label _lblInfo;

        private readonly ThemedListBox _lstTurns;
        private readonly Splitter _splitter;
        private readonly ThemedRichTextBox _txtDetail;

        // ?? State ???????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;
        private List<ConversationTurn> _allTurns = new();
        private List<ConversationTurn> _filteredTurns = new();

        // ?? Constructor ?????????????????????????????????????????????????????

        public ChatHistoryViewerPanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            // ?? Header ??????????????????????????????????????????????????????
            // Uses a TableLayoutPanel for predictable row stacking without
            // dock-order overlap issues.

            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = WallyTheme.Surface0,
                Padding = new Padding(12, 8, 12, 4),
                Margin = Padding.Empty
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Row 0: Title
            var lblTitle = new Label
            {
                Text = "\uD83D\uDCAC Chat History",
                AutoSize = true,
                Font = WallyTheme.FontUIBold,
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
            headerTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerTable.Controls.Add(lblTitle, 0, 0);

            // Row 1: Action bar (buttons + filter)
            var actionBar = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };

            _btnRefresh = CreateButton("\u21BB Refresh");
            _btnRefresh.Click += (_, _) => RefreshHistory();

            _btnClear = CreateButton("\uD83D\uDDD1 Clear All");
            _btnClear.Click += OnClearHistory;

            _btnCopy = CreateButton("\uD83D\uDCCB Copy");
            _btnCopy.Click += OnCopyDetail;

            var lblFilter = new Label
            {
                Text = "Actor:",
                AutoSize = true,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Margin = new Padding(12, 6, 4, 0)
            };

            _cboActorFilter = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = WallyTheme.FontUISmall,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                FlatStyle = FlatStyle.Standard,
                Margin = new Padding(0, 2, 0, 0)
            };
            _cboActorFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

            actionBar.Controls.Add(_btnRefresh);
            actionBar.Controls.Add(_btnClear);
            actionBar.Controls.Add(_btnCopy);
            actionBar.Controls.Add(lblFilter);
            actionBar.Controls.Add(_cboActorFilter);

            headerTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerTable.Controls.Add(actionBar, 0, 1);

            // Row 2: Info label
            _lblInfo = new Label
            {
                AutoSize = true,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 2)
            };
            headerTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerTable.Controls.Add(_lblInfo, 0, 2);

            _headerPanel = headerTable;

            // ?? Turn list (left) ????????????????????????????????????????????

            _lstTurns = ThemedEditorFactory.CreateListViewer(340, WallyTheme.FontUISmall, WallyTheme.Surface1);
            _lstTurns.DrawMode = DrawMode.OwnerDrawVariable;
            _lstTurns.MeasureItem += OnMeasureItem;
            _lstTurns.DrawItem += OnDrawItem;
            _lstTurns.SelectedIndexChanged += OnTurnSelected;

            _splitter = new Splitter
            {
                Dock = DockStyle.Left,
                Width = 3,
                BackColor = WallyTheme.Border,
                MinSize = 200
            };

            // ?? Detail pane (fill) ??????????????????????????????????????????

            _txtDetail = ThemedEditorFactory.CreateDocumentViewer(wordWrap: true, backColor: WallyTheme.Surface0);

            // ?? Assembly ????????????????????????????????????????????????????
            // Order: Fill first, then docked edges.

            Controls.Add(_txtDetail);       // Fill
            Controls.Add(_splitter);        // Left (after listbox)
            Controls.Add(_lstTurns);        // Left
            Controls.Add(_headerPanel);     // Top

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
        }

        /// <summary>
        /// Reloads all turns from the conversation history file and
        /// refreshes the list and filter dropdown.
        /// </summary>
        public void RefreshHistory()
        {
            _lstTurns.Items.Clear();
            _txtDetail.Clear();
            _allTurns.Clear();
            _filteredTurns.Clear();
            _lblInfo.ForeColor = WallyTheme.TextMuted;

            if (_environment?.HasWorkspace != true)
            {
                _lblInfo.Text = "No workspace loaded.";
                return;
            }

            _allTurns = _environment.History.GetAllTurns();

            // Rebuild actor filter dropdown
            var actors = new List<string> { "(all)" };
            actors.Add("(direct / no actor)");
            actors.AddRange(
                _allTurns
                    .Where(t => t.ActorName != null)
                    .Select(t => t.ActorName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));

            string? previousFilter = _cboActorFilter.SelectedItem?.ToString();
            _cboActorFilter.Items.Clear();
            _cboActorFilter.Items.AddRange(actors.ToArray());

            // Restore previous selection if it still exists
            int idx = previousFilter != null
                ? _cboActorFilter.Items.IndexOf(previousFilter)
                : 0;
            _cboActorFilter.SelectedIndex = idx >= 0 ? idx : 0;

            // ApplyFilter is triggered by SelectedIndexChanged, but call
            // explicitly in case the index didn't change.
            ApplyFilter();
        }

        // ?? Filtering ???????????????????????????????????????????????????????

        private void ApplyFilter()
        {
            string? filter = _cboActorFilter.SelectedItem?.ToString();

            if (filter == null || filter == "(all)")
            {
                _filteredTurns = new List<ConversationTurn>(_allTurns);
            }
            else if (filter == "(direct / no actor)")
            {
                _filteredTurns = _allTurns
                    .Where(t => t.ActorName == null)
                    .ToList();
            }
            else
            {
                _filteredTurns = _allTurns
                    .Where(t => string.Equals(t.ActorName, filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            PopulateTurnList();
        }

        private void PopulateTurnList()
        {
            _lstTurns.BeginUpdate();
            _lstTurns.Items.Clear();

            foreach (var turn in _filteredTurns)
                _lstTurns.Items.Add(new TurnListItem(turn));

            _lstTurns.EndUpdate();

            _lblInfo.ForeColor = WallyTheme.TextMuted;
            _lblInfo.Text = _filteredTurns.Count == _allTurns.Count
                ? $"{_allTurns.Count} turn(s)"
                : $"{_filteredTurns.Count} of {_allTurns.Count} turn(s)";

            // Select the last (most recent) turn
            if (_lstTurns.Items.Count > 0)
                _lstTurns.SelectedIndex = _lstTurns.Items.Count - 1;
            else
                _txtDetail.Clear();
        }

        // ?? Owner-draw for turn list ????????????????????????????????????????

        private void OnMeasureItem(object? sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = 56;
        }

        private void OnDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _lstTurns.Items.Count) return;

            var item = (TurnListItem)_lstTurns.Items[e.Index];
            var turn = item.Turn;
            bool selected = (e.State & DrawItemState.Selected) != 0;

            // Background
            Color bg = selected ? WallyTheme.Surface3 : WallyTheme.Surface1;
            using (var bgBrush = new SolidBrush(bg))
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // Error/loop accent bar on the left
            if (turn.IsError)
            {
                using var accent = new SolidBrush(WallyTheme.Red);
                e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }
            else if (turn.Iteration > 0)
            {
                using var accent = new SolidBrush(WallyTheme.Yellow);
                e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }

            int x = e.Bounds.X + 10;
            int y = e.Bounds.Y + 4;
            int textW = e.Bounds.Width - 20;

            // Line 1: Actor + timestamp
            string actor = turn.ActorName ?? "(direct)";
            string time = turn.Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            string line1 = $"{actor}  \u2022  {time}";
            TextRenderer.DrawText(e.Graphics, line1, WallyTheme.FontUISmallBold,
                new Rectangle(x, y, textW, 16),
                selected ? WallyTheme.TextPrimary : WallyTheme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            y += 17;

            // Line 2: Prompt preview (truncated)
            string promptPreview = turn.Prompt.Length > 80
                ? turn.Prompt[..80] + "\u2026"
                : turn.Prompt;
            promptPreview = promptPreview.Replace('\n', ' ').Replace('\r', ' ');
            TextRenderer.DrawText(e.Graphics, promptPreview, WallyTheme.FontUISmall,
                new Rectangle(x, y, textW, 16),
                WallyTheme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            y += 17;

            // Line 3: Metadata chips
            string meta = "";
            if (!string.IsNullOrEmpty(turn.WrapperName)) meta += turn.WrapperName;
            if (!string.IsNullOrEmpty(turn.Model)) meta += $" \u2022 {turn.Model}";
            if (turn.ElapsedMs > 0) meta += $" \u2022 {turn.ElapsedMs}ms";
            if (!string.IsNullOrEmpty(turn.LoopName)) meta += $" \u2022 {turn.LoopName}[{turn.Iteration}]";
            if (turn.IsError) meta += " \u2022 ERROR";

            TextRenderer.DrawText(e.Graphics, meta, WallyTheme.FontUISmall,
                new Rectangle(x, y, textW, 14),
                turn.IsError ? WallyTheme.Red : WallyTheme.TextDisabled,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // Bottom separator
            using (var pen = new Pen(WallyTheme.Border))
                e.Graphics.DrawLine(pen, e.Bounds.X, e.Bounds.Bottom - 1,
                    e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        // ?? Turn selection ??????????????????????????????????????????????????

        private void OnTurnSelected(object? sender, EventArgs e)
        {
            if (_lstTurns.SelectedItem is not TurnListItem item)
            {
                _txtDetail.Clear();
                return;
            }

            var turn = item.Turn;
            _txtDetail.Clear();

            // ?? Metadata header ??
            AppendHeading("Metadata");
            AppendField("Timestamp",    turn.Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            AppendField("Actor",        turn.ActorName ?? "(direct / no actor)");
            AppendField("Wrapper",      turn.WrapperName);
            AppendField("Model",        turn.Model ?? "(default)");
            AppendField("Elapsed",      $"{turn.ElapsedMs} ms");
            AppendField("Session",      turn.SessionId);
            AppendField("Is Error",     turn.IsError ? "Yes" : "No");

            if (!string.IsNullOrEmpty(turn.LoopName))
            {
                AppendField("Loop",     turn.LoopName);
                AppendField("Iteration", turn.Iteration.ToString());
            }

            AppendBlank();

            // ?? Prompt ??
            AppendHeading("Prompt");
            AppendBody(turn.Prompt);
            AppendBlank();

            // ?? Response ??
            AppendHeading(turn.IsError ? "Response (ERROR)" : "Response");
            AppendBody(turn.Response);

            // Scroll to top of the detail view.
            _txtDetail.SelectionStart = 0;
            _txtDetail.ScrollToCaret();
        }

        // ?? Event handlers ??????????????????????????????????????????????????

        private void OnClearHistory(object? sender, EventArgs e)
        {
            if (_environment?.HasWorkspace != true) return;

            var result = MessageBox.Show(
                "This will permanently delete the entire conversation history file.\n\n" +
                "Turns are not recoverable after deletion.\n\nContinue?",
                "Clear Chat History",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            _environment.History.ClearHistory();
            RefreshHistory();
            _lblInfo.Text = "History cleared.";
            _lblInfo.ForeColor = WallyTheme.Green;
        }

        private void OnCopyDetail(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_txtDetail.Text))
            {
                Clipboard.SetText(_txtDetail.Text);
                _lblInfo.Text = "Copied to clipboard.";
                _lblInfo.ForeColor = WallyTheme.Green;
            }
        }

        // ?? RichTextBox append helpers ??????????????????????????????????????

        private void AppendHeading(string text)
        {
            _txtDetail.SelectionStart = _txtDetail.TextLength;
            _txtDetail.SelectionLength = 0;
            _txtDetail.SelectionColor = WallyTheme.TextSecondary;
            _txtDetail.SelectionFont = WallyTheme.FontUIBold;
            _txtDetail.AppendText($"\u2500\u2500 {text} ");
            _txtDetail.SelectionColor = WallyTheme.Border;
            _txtDetail.AppendText(new string('\u2500', Math.Max(0, 50 - text.Length)));
            _txtDetail.AppendText(Environment.NewLine);
        }

        private void AppendField(string name, string value)
        {
            _txtDetail.SelectionStart = _txtDetail.TextLength;
            _txtDetail.SelectionLength = 0;
            _txtDetail.SelectionColor = WallyTheme.TextMuted;
            _txtDetail.SelectionFont = WallyTheme.FontUISmall;
            _txtDetail.AppendText($"  {name,-14} ");

            _txtDetail.SelectionStart = _txtDetail.TextLength;
            _txtDetail.SelectionLength = 0;
            _txtDetail.SelectionColor = WallyTheme.TextPrimary;
            _txtDetail.SelectionFont = WallyTheme.FontMono;
            _txtDetail.AppendText(value + Environment.NewLine);
        }

        private void AppendBody(string text)
        {
            _txtDetail.SelectionStart = _txtDetail.TextLength;
            _txtDetail.SelectionLength = 0;
            _txtDetail.SelectionColor = WallyTheme.TextPrimary;
            _txtDetail.SelectionFont = WallyTheme.FontMono;
            _txtDetail.AppendText(text + Environment.NewLine);
        }

        private void AppendBlank()
        {
            _txtDetail.AppendText(Environment.NewLine);
        }

        // ?? Control factories ???????????????????????????????????????????????

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

        // ?? List item wrapper ???????????????????????????????????????????????

        private sealed class TurnListItem
        {
            public ConversationTurn Turn { get; }

            public TurnListItem(ConversationTurn turn) => Turn = turn;

            public override string ToString()
            {
                string actor = Turn.ActorName ?? "(direct)";
                string time = Turn.Timestamp.LocalDateTime.ToString("HH:mm:ss");
                return $"{actor} \u2014 {time}";
            }
        }
    }
}
