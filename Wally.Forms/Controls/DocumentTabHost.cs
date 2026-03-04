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
    /// A dark-themed, owner-drawn TabControl that hosts document editor panels
    /// in the center content area. Supports close buttons on each tab, dirty
    /// indicators, and a persistent Welcome tab.
    /// </summary>
    public sealed class DocumentTabHost : UserControl
    {
        private readonly TabControl _tabs;
        private readonly Dictionary<string, TabPage> _openTabs = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raised when the user closes a tab.</summary>
        public event EventHandler<TabClosedEventArgs>? TabClosed;

        /// <summary>Raised when the active tab changes.</summary>
        public event EventHandler? ActiveTabChanged;

        public DocumentTabHost()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(180, 28),
                Padding = new Point(12, 4),
                Font = WallyTheme.FontUISmall,
                Appearance = TabAppearance.Normal,
                Multiline = true
            };
            _tabs.DrawItem += OnDrawTab;
            _tabs.MouseDown += OnTabMouseDown;
            _tabs.SelectedIndexChanged += (_, _) => ActiveTabChanged?.Invoke(this, EventArgs.Empty);

            // Remove the white border around the tab control.
            _tabs.BackColor = WallyTheme.Surface0;

            Controls.Add(_tabs);

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        /// <summary>
        /// Adds a tab with the given key, title, and content control.
        /// If a tab with that key already exists, it is selected instead.
        /// </summary>
        /// <param name="key">Unique identifier for this tab.</param>
        /// <param name="title">Display title on the tab header.</param>
        /// <param name="icon">Optional icon character prepended to the title.</param>
        /// <param name="content">The control to display in the tab body.</param>
        /// <param name="closeable">Whether the tab has a close button.</param>
        public void OpenTab(string key, string title, string? icon, Control content, bool closeable = true)
        {
            if (_openTabs.TryGetValue(key, out var existing))
            {
                _tabs.SelectedTab = existing;
                return;
            }

            var page = new TabPage
            {
                Text = (icon != null ? $"{icon} {title}" : title) + (closeable ? "   ×" : ""),
                Tag = new TabInfo(key, title, icon, closeable),
                BackColor = WallyTheme.Surface0,
                ForeColor = WallyTheme.TextPrimary,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);

            _openTabs[key] = page;
            _tabs.TabPages.Add(page);
            _tabs.SelectedTab = page;
        }

        /// <summary>
        /// Selects the tab with the given key if it exists.
        /// Returns true if the tab was found and selected.
        /// </summary>
        public bool SelectTab(string key)
        {
            if (_openTabs.TryGetValue(key, out var page))
            {
                _tabs.SelectedTab = page;
                return true;
            }
            return false;
        }

        /// <summary>Closes the tab with the given key.</summary>
        public void CloseTab(string key)
        {
            if (!_openTabs.TryGetValue(key, out var page)) return;

            var info = (TabInfo)page.Tag;
            if (!info.Closeable) return;

            _openTabs.Remove(key);
            _tabs.TabPages.Remove(page);

            // Dispose the content control.
            foreach (Control c in page.Controls)
                c.Dispose();
            page.Dispose();

            TabClosed?.Invoke(this, new TabClosedEventArgs(key));
        }

        /// <summary>Returns all currently open tab keys.</summary>
        public IEnumerable<string> OpenTabKeys => _openTabs.Keys;

        /// <summary>Returns the key of the currently active tab, or null.</summary>
        public string? ActiveTabKey
        {
            get
            {
                if (_tabs.SelectedTab?.Tag is TabInfo info)
                    return info.Key;
                return null;
            }
        }

        /// <summary>Returns the number of open tabs.</summary>
        public int TabCount => _tabs.TabPages.Count;

        /// <summary>
        /// Marks a tab as dirty (unsaved changes) by prepending a bullet.
        /// </summary>
        public void SetTabDirty(string key, bool dirty)
        {
            if (!_openTabs.TryGetValue(key, out var page)) return;
            var info = (TabInfo)page.Tag;
            info.IsDirty = dirty;
            _tabs.Invalidate(); // Force redraw of tab headers
        }

        /// <summary>Closes all closeable tabs.</summary>
        public void CloseAllTabs()
        {
            var keys = _openTabs.Keys.ToList();
            foreach (var key in keys)
                CloseTab(key);
        }

        // ?? Owner-draw ??????????????????????????????????????????????????????

        private void OnDrawTab(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _tabs.TabPages.Count) return;

            var page = _tabs.TabPages[e.Index];
            var info = page.Tag as TabInfo;
            bool selected = _tabs.SelectedIndex == e.Index;
            var g = e.Graphics;
            WallyTheme.ConfigureGraphics(g);

            // Background
            Color bg = selected ? WallyTheme.Surface1 : WallyTheme.Surface2;
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, e.Bounds);

            // Bottom accent on selected tab
            if (selected)
            {
                using var accent = new SolidBrush(WallyTheme.Accent);
                g.FillRectangle(accent, e.Bounds.X, e.Bounds.Bottom - 2, e.Bounds.Width, 2);
            }

            // Build display text
            string displayText = info?.Icon != null ? $"{info.Icon} {info.Title}" : (info?.Title ?? page.Text);
            if (info?.IsDirty == true)
                displayText = "\u25CF " + displayText;

            // Text
            Color fg = selected ? WallyTheme.TextPrimary : WallyTheme.TextMuted;
            var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 28, e.Bounds.Height);
            TextRenderer.DrawText(g, displayText, WallyTheme.FontUISmall, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // Close button (×)
            if (info?.Closeable == true)
            {
                var closeRect = GetCloseButtonRect(e.Bounds);
                Color closeFg = selected ? WallyTheme.TextSecondary : WallyTheme.TextDisabled;
                TextRenderer.DrawText(g, "×", WallyTheme.FontUISmall, closeRect, closeFg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void OnTabMouseDown(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                var bounds = _tabs.GetTabRect(i);
                if (!bounds.Contains(e.Location)) continue;

                var info = _tabs.TabPages[i].Tag as TabInfo;

                // Middle-click to close
                if (e.Button == MouseButtons.Middle && info?.Closeable == true)
                {
                    CloseTab(info.Key);
                    return;
                }

                // Close button click
                if (e.Button == MouseButtons.Left && info?.Closeable == true)
                {
                    var closeRect = GetCloseButtonRect(bounds);
                    if (closeRect.Contains(e.Location))
                    {
                        CloseTab(info.Key);
                        return;
                    }
                }

                break;
            }
        }

        private static Rectangle GetCloseButtonRect(Rectangle tabBounds)
        {
            return new Rectangle(
                tabBounds.Right - 22,
                tabBounds.Y + (tabBounds.Height - 16) / 2,
                16, 16);
        }

        // ?? Tab info ????????????????????????????????????????????????????????

        private sealed class TabInfo
        {
            public string Key { get; }
            public string Title { get; }
            public string? Icon { get; }
            public bool Closeable { get; }
            public bool IsDirty { get; set; }

            public TabInfo(string key, string title, string? icon, bool closeable)
            {
                Key = key;
                Title = title;
                Icon = icon;
                Closeable = closeable;
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
