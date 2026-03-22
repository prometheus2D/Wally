using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// A dark-themed, fully custom-drawn tab control that hosts document editor
    /// panels in the center content area. Supports close buttons on each tab,
    /// dirty indicators, and themed scrollbar-blending content area.
    /// <para>
    /// This replaces the standard <see cref="TabControl"/> to eliminate the
    /// native border and opaque background that cannot be overridden.
    /// </para>
    /// </summary>
    public sealed class DocumentTabHost : UserControl
    {
        private readonly TabStrip _tabStrip;
        private readonly Panel _contentPanel;
        private readonly List<TabEntry> _tabs = new();
        private readonly Dictionary<string, TabEntry> _tabsByKey = new(StringComparer.OrdinalIgnoreCase);
        private int _selectedIndex = -1;
        private bool _wordWrap;

        /// <summary>Raised when the user closes a tab.</summary>
        public event EventHandler<TabClosedEventArgs>? TabClosed;

        /// <summary>Raised when the active tab changes.</summary>
        public event EventHandler? ActiveTabChanged;

        public DocumentTabHost()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            _tabStrip = new TabStrip(this)
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = WallyTheme.Surface0
            };

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WallyTheme.Surface0,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            // Content first so it fills, then strip on top
            Controls.Add(_contentPanel);
            Controls.Add(_tabStrip);

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        /// <summary>
        /// Adds a tab with the given key, title, and content control.
        /// If a tab with that key already exists, it is selected instead.
        /// All tabs have a close button.
        /// </summary>
        public void OpenTab(string key, string title, string? icon, Control content, bool closeable = true)
        {
            if (_tabsByKey.TryGetValue(key, out var existing))
            {
                SelectEntry(existing);
                return;
            }

            var entry = new TabEntry(key, title, icon, closeable, content);
            content.Dock = DockStyle.Fill;
            content.Visible = false;
            _contentPanel.Controls.Add(content);

            _tabs.Add(entry);
            _tabsByKey[key] = entry;

            SelectEntry(entry);
            _tabStrip.Invalidate();
        }

        /// <summary>
        /// Selects the tab with the given key if it exists.
        /// Returns true if the tab was found and selected.
        /// </summary>
        public bool SelectTab(string key)
        {
            if (_tabsByKey.TryGetValue(key, out var entry))
            {
                SelectEntry(entry);
                return true;
            }
            return false;
        }

        /// <summary>Closes the tab with the given key.</summary>
        public void CloseTab(string key)
        {
            if (!_tabsByKey.TryGetValue(key, out var entry)) return;
            if (!entry.Closeable) return;

            int idx = _tabs.IndexOf(entry);
            _tabs.Remove(entry);
            _tabsByKey.Remove(key);

            _contentPanel.Controls.Remove(entry.Content);
            entry.Content.Dispose();

            // Select a neighbour
            if (_tabs.Count > 0)
            {
                int newIdx = Math.Min(idx, _tabs.Count - 1);
                SelectEntry(_tabs[newIdx]);
            }
            else
            {
                _selectedIndex = -1;
            }

            _tabStrip.Invalidate();
            TabClosed?.Invoke(this, new TabClosedEventArgs(key));
        }

        /// <summary>Returns all currently open tab keys.</summary>
        public IEnumerable<string> OpenTabKeys => _tabsByKey.Keys;

        /// <summary>Returns the key of the currently active tab, or null.</summary>
        public string? ActiveTabKey =>
            _selectedIndex >= 0 && _selectedIndex < _tabs.Count
                ? _tabs[_selectedIndex].Key
                : null;

        /// <summary>Returns the number of open tabs.</summary>
        public int TabCount => _tabs.Count;

        /// <summary>
        /// Marks a tab as dirty (unsaved changes) by prepending a bullet.
        /// </summary>
        public void SetTabDirty(string key, bool dirty)
        {
            if (!_tabsByKey.TryGetValue(key, out var entry)) return;
            entry.IsDirty = dirty;
            _tabStrip.Invalidate();
        }

        /// <summary>Closes all closeable tabs.</summary>
        public void CloseAllTabs()
        {
            var keys = _tabsByKey.Keys.ToList();
            foreach (var key in keys)
                CloseTab(key);
        }

        /// <summary>
        /// Gets or sets the global word-wrap preference. When changed, applies
        /// word-wrap to all <see cref="RichTextBox"/> controls found in every
        /// open tab's content hierarchy.
        /// </summary>
        public bool WordWrap
        {
            get => _wordWrap;
            set
            {
                if (_wordWrap == value) return;
                _wordWrap = value;
                foreach (var entry in _tabs)
                    ApplyWordWrap(entry.Content, value);
            }
        }

        /// <summary>Returns the content control of the currently active tab, or null.</summary>
        public Control? GetActivePanel() =>
            _selectedIndex >= 0 && _selectedIndex < _tabs.Count
                ? _tabs[_selectedIndex].Content
                : null;

        // ?? Selection ???????????????????????????????????????????????????????

        private void SelectEntry(TabEntry entry)
        {
            int idx = _tabs.IndexOf(entry);
            if (idx < 0) return;

            // Hide current
            if (_selectedIndex >= 0 && _selectedIndex < _tabs.Count)
                _tabs[_selectedIndex].Content.Visible = false;

            _selectedIndex = idx;
            entry.Content.Visible = true;
            entry.Content.BringToFront();
            _tabStrip.Invalidate();
            ActiveTabChanged?.Invoke(this, EventArgs.Empty);
        }

        // ?? Word-wrap propagation ???????????????????????????????????????????

        private static void ApplyWordWrap(Control root, bool wrap)
        {
            if (root is RichTextBox rtb)
            {
                rtb.WordWrap = wrap;
                return;
            }

            foreach (Control child in root.Controls)
                ApplyWordWrap(child, wrap);
        }

        // ?? Tab strip (header bar) ?????????????????????????????????????????

        /// <summary>
        /// Fully custom-painted panel that renders tab headers.
        /// </summary>
        private sealed class TabStrip : Panel
        {
            private readonly DocumentTabHost _host;
            private int _hoverIndex = -1;
            private int _hoverCloseIndex = -1;
            private int _scrollOffset;

            private const int TabPadding = 12;
            private const int CloseButtonSize = 16;
            private const int CloseButtonMargin = 6;
            private const int TabGap = 1;
            private const int MinTabWidth = 80;
            private const int MaxTabWidth = 200;
            private const int AccentHeight = 2;

            public TabStrip(DocumentTabHost host)
            {
                _host = host;
                DoubleBuffered = true;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                WallyTheme.ConfigureGraphics(g);

                // Background — seamless with parent
                using (var bgBrush = new SolidBrush(WallyTheme.Surface0))
                    g.FillRectangle(bgBrush, ClientRectangle);

                int x = -_scrollOffset;
                for (int i = 0; i < _host._tabs.Count; i++)
                {
                    var entry = _host._tabs[i];
                    int tabW = MeasureTabWidth(g, entry);
                    var tabRect = new Rectangle(x, 0, tabW, Height);

                    bool selected = i == _host._selectedIndex;
                    bool hovered = i == _hoverIndex;

                    // Tab background
                    Color bg = selected ? WallyTheme.Surface1
                             : hovered  ? WallyTheme.Surface2
                                        : WallyTheme.Surface0;
                    using (var brush = new SolidBrush(bg))
                        g.FillRectangle(brush, tabRect);

                    // Bottom accent on selected
                    if (selected)
                    {
                        using var accent = new SolidBrush(WallyTheme.Accent);
                        g.FillRectangle(accent, x, Height - AccentHeight, tabW, AccentHeight);
                    }

                    // Build display text
                    string text = entry.Icon != null
                        ? $"{entry.Icon} {entry.Title}"
                        : entry.Title;
                    if (entry.IsDirty)
                        text = "\u25CF " + text;

                    // Text — all tabs show close button area
                    int textRight = entry.Closeable
                        ? tabRect.Right - CloseButtonSize - CloseButtonMargin - 4
                        : tabRect.Right - TabPadding;
                    var textRect = new Rectangle(
                        tabRect.X + TabPadding,
                        tabRect.Y,
                        textRight - (tabRect.X + TabPadding),
                        tabRect.Height);

                    Color fg = selected ? WallyTheme.TextPrimary : WallyTheme.TextMuted;
                    TextRenderer.DrawText(g, text, WallyTheme.FontUISmall, textRect, fg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                    // Close button — drawn on ALL tabs
                    if (entry.Closeable)
                    {
                        var closeRect = GetCloseRect(tabRect);
                        bool closeHover = i == _hoverCloseIndex;
                        Color closeFg = closeHover
                            ? WallyTheme.TextPrimary
                            : selected
                                ? WallyTheme.TextSecondary
                                : WallyTheme.TextDisabled;

                        if (closeHover)
                        {
                            using var hb = new SolidBrush(WallyTheme.Surface3);
                            g.FillRectangle(hb, closeRect);
                        }

                        TextRenderer.DrawText(g, "\u00D7", WallyTheme.FontUISmall, closeRect, closeFg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    x += tabW + TabGap;
                }

                // Bottom border line across full width
                using (var pen = new Pen(WallyTheme.Border))
                    g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int oldHover = _hoverIndex;
                int oldClose = _hoverCloseIndex;

                _hoverIndex = -1;
                _hoverCloseIndex = -1;

                int hit = HitTest(e.Location, out bool onClose);
                _hoverIndex = hit;
                if (onClose) _hoverCloseIndex = hit;

                if (oldHover != _hoverIndex || oldClose != _hoverCloseIndex)
                    Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_hoverIndex >= 0 || _hoverCloseIndex >= 0)
                {
                    _hoverIndex = -1;
                    _hoverCloseIndex = -1;
                    Invalidate();
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);

                int hit = HitTest(e.Location, out bool onClose);
                if (hit < 0) return;

                var entry = _host._tabs[hit];

                // Middle-click to close
                if (e.Button == MouseButtons.Middle && entry.Closeable)
                {
                    _host.CloseTab(entry.Key);
                    return;
                }

                if (e.Button == MouseButtons.Left)
                {
                    // Close button click
                    if (onClose && entry.Closeable)
                    {
                        _host.CloseTab(entry.Key);
                        return;
                    }

                    // Select tab
                    _host.SelectEntry(entry);
                }
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                int totalWidth = GetTotalTabWidth();
                if (totalWidth <= Width) { _scrollOffset = 0; Invalidate(); return; }

                _scrollOffset -= e.Delta / 4;
                _scrollOffset = Math.Clamp(_scrollOffset, 0, totalWidth - Width);
                Invalidate();
            }

            private int GetTotalTabWidth()
            {
                using var g = CreateGraphics();
                int total = 0;
                for (int i = 0; i < _host._tabs.Count; i++)
                    total += MeasureTabWidth(g, _host._tabs[i]) + TabGap;
                return total;
            }

            // ?? Measurement ????????????????????????????????????????????????

            private int MeasureTabWidth(Graphics g, TabEntry entry)
            {
                string text = entry.Icon != null
                    ? $"{entry.Icon} {entry.Title}"
                    : entry.Title;
                if (entry.IsDirty)
                    text = "\u25CF " + text;

                var size = TextRenderer.MeasureText(g, text, WallyTheme.FontUISmall,
                    new Size(int.MaxValue, Height),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                // All tabs include space for a close button
                int w = size.Width + TabPadding * 2 + CloseButtonSize + CloseButtonMargin;

                return Math.Clamp(w, MinTabWidth, MaxTabWidth);
            }

            // ?? Hit testing ????????????????????????????????????????????????

            private int HitTest(Point pt, out bool onClose)
            {
                onClose = false;
                using var g = CreateGraphics();
                int x = -_scrollOffset;
                for (int i = 0; i < _host._tabs.Count; i++)
                {
                    var entry = _host._tabs[i];
                    int tabW = MeasureTabWidth(g, entry);
                    var tabRect = new Rectangle(x, 0, tabW, Height);

                    if (tabRect.Contains(pt))
                    {
                        if (entry.Closeable)
                        {
                            var closeRect = GetCloseRect(tabRect);
                            onClose = closeRect.Contains(pt);
                        }
                        return i;
                    }

                    x += tabW + TabGap;
                }
                return -1;
            }

            private static Rectangle GetCloseRect(Rectangle tabRect)
            {
                return new Rectangle(
                    tabRect.Right - CloseButtonSize - CloseButtonMargin,
                    tabRect.Y + (tabRect.Height - CloseButtonSize) / 2,
                    CloseButtonSize,
                    CloseButtonSize);
            }
        }

        // ?? Tab entry ???????????????????????????????????????????????????????

        private sealed class TabEntry
        {
            public string Key { get; }
            public string Title { get; }
            public string? Icon { get; }
            public bool Closeable { get; }
            public Control Content { get; }
            public bool IsDirty { get; set; }

            public TabEntry(string key, string title, string? icon, bool closeable, Control content)
            {
                Key = key;
                Title = title;
                Icon = icon;
                Closeable = closeable;
                Content = content;
            }
        }
    }

    /// <summary>Event args for tab close events.</summary>
    public sealed class TabClosedEventArgs : EventArgs
    {
        public string Key { get; }
        public TabClosedEventArgs(string key) => Key = key;
    }
}
