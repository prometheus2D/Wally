using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Tabbed settings panel — uses the same custom-painted tab bar as
    /// ExplorerTabPanel so it fits the dark theme seamlessly.
    /// </summary>
    public sealed class SettingsPanel : UserControl
    {
        // ?? Tab definitions ??????????????????????????????????????????????????

        private enum Tab { Workspace, User }

        private static readonly (Tab Id, string Label, string Emoji)[] _tabs =
        {
            (Tab.Workspace, "WORKSPACE", "\u2699"),
            (Tab.User,      "USER",      "\uD83D\uDC64"),
        };

        // ?? Tab index constants (for external callers) ???????????????????????

        /// <summary>Tab index for Workspace settings.</summary>
        public const int TabIndexWorkspace = 0;

        /// <summary>Tab index for User Preferences.</summary>
        public const int TabIndexUser = 1;

        // ?? Tab bar metrics (match ExplorerTabPanel exactly) ?????????????????

        private const int TabBarHeight = 32;
        private const int TabPadH      = 14;
        private const int AccentBarH   = 2;

        // ?? Controls ?????????????????????????????????????????????????????????

        private readonly Panel                      _tabBar;
        private readonly Panel                      _body;
        private readonly ConfigEditorPanel          _workspacePanel;
        private readonly UserPreferencesEditorPanel _userPanel;

        // ?? State ?????????????????????????????????????????????????????????????

        private Tab  _activeTab = Tab.Workspace;
        private Tab? _hoverTab  = null;

        // ?? Constructor ???????????????????????????????????????????????????????

        public SettingsPanel(WallyEnvironment environment)
        {
            SuspendLayout();

            Dock      = DockStyle.Fill;
            BackColor = WallyTheme.Surface1;

            // ?? Tab bar ??
            _tabBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = TabBarHeight,
                BackColor = WallyTheme.Surface2
            };
            _tabBar.Paint      += OnTabBarPaint;
            _tabBar.MouseMove  += OnTabBarMouseMove;
            _tabBar.MouseLeave += OnTabBarMouseLeave;
            _tabBar.MouseClick += OnTabBarMouseClick;

            // 1-px separator under the tab bar
            var tabBorder = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = WallyTheme.Border
            };

            // ?? Body ??
            _body = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = WallyTheme.Surface1,
                Padding   = Padding.Empty
            };

            // ?? Child panels ??
            _workspacePanel = new ConfigEditorPanel { Dock = DockStyle.Fill };
            _workspacePanel.BindEnvironment(environment);
            _workspacePanel.LoadConfig();

            _userPanel = new UserPreferencesEditorPanel { Dock = DockStyle.Fill };
            _userPanel.LoadPreferences();

            _body.Controls.Add(_workspacePanel);
            _body.Controls.Add(_userPanel);

            // Assembly — Controls stacked Top-first, Fill last
            Controls.Add(_body);
            Controls.Add(tabBorder);
            Controls.Add(_tabBar);

            ApplyActiveTab();
            ResumeLayout(true);
        }

        // ?? Public API ????????????????????????????????????????????????????????

        /// <summary>
        /// Activates the tab at the given index (use the <c>TabIndex*</c> constants).
        /// </summary>
        public void SelectTab(int index)
        {
            var tab = index switch
            {
                TabIndexUser => Tab.User,
                _            => Tab.Workspace
            };
            SetActiveTab(tab);
        }

        // ?? Tab switching ?????????????????????????????????????????????????????

        private void SetActiveTab(Tab tab)
        {
            _activeTab = tab;
            ApplyActiveTab();
            _tabBar.Invalidate();
        }

        private void ApplyActiveTab()
        {
            _workspacePanel.Visible = _activeTab == Tab.Workspace;
            _userPanel.Visible      = _activeTab == Tab.User;

            if (_activeTab == Tab.Workspace) _workspacePanel.BringToFront();
            else                             _userPanel.BringToFront();
        }

        // ?? Tab bar painting (mirrors ExplorerTabPanel exactly) ???????????????

        private void OnTabBarPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var bgBrush = new SolidBrush(WallyTheme.Surface2))
                g.FillRectangle(bgBrush, _tabBar.ClientRectangle);

            int x = 0;
            foreach (var (id, label, emoji) in _tabs)
            {
                int w    = MeasureTabWidth(label);
                var rect = new Rectangle(x, 0, w, TabBarHeight);

                bool isActive = id == _activeTab;
                bool isHover  = id == _hoverTab && !isActive;

                if (isActive)
                {
                    using var bg = new SolidBrush(WallyTheme.Surface1);
                    g.FillRectangle(bg, rect);
                }
                else if (isHover)
                {
                    using var bg = new SolidBrush(WallyTheme.Surface3);
                    g.FillRectangle(bg, rect);
                }

                if (isActive)
                {
                    using var accent = new SolidBrush(WallyTheme.Accent);
                    g.FillRectangle(accent,
                        new Rectangle(rect.X, rect.Bottom - AccentBarH, rect.Width, AccentBarH));
                }

                Color  fg   = isActive ? WallyTheme.TextPrimary
                            : isHover  ? WallyTheme.TextSecondary
                                       : WallyTheme.TextMuted;
                Font   font = isActive ? WallyTheme.FontUISmallBold : WallyTheme.FontUISmall;
                string text = $"{emoji}  {label}";

                TextRenderer.DrawText(g, text, font,
                    new Rectangle(rect.X + TabPadH / 2, rect.Y,
                                  rect.Width - TabPadH / 2, rect.Height - AccentBarH),
                    fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                x += w;
            }
        }

        private int MeasureTabWidth(string label)
        {
            using var g = _tabBar.CreateGraphics();
            // Size using the widest representation (bold, with emoji prefix)
            int textW = TextRenderer.MeasureText(g, $"\u2699  {label}", WallyTheme.FontUISmallBold).Width;
            return textW + TabPadH * 2;
        }

        // ?? Tab bar interaction ???????????????????????????????????????????????

        private void OnTabBarMouseMove(object? sender, MouseEventArgs e)
        {
            Tab? hit = HitTest(e.X);
            if (hit != _hoverTab)
            {
                _hoverTab = hit;
                _tabBar.Invalidate();
            }
        }

        private void OnTabBarMouseLeave(object? sender, EventArgs e)
        {
            if (_hoverTab != null)
            {
                _hoverTab = null;
                _tabBar.Invalidate();
            }
        }

        private void OnTabBarMouseClick(object? sender, MouseEventArgs e)
        {
            Tab? hit = HitTest(e.X);
            if (hit.HasValue && hit.Value != _activeTab)
                SetActiveTab(hit.Value);
        }

        private Tab? HitTest(int mouseX)
        {
            int x = 0;
            foreach (var (id, label, _) in _tabs)
            {
                int w = MeasureTabWidth(label);
                if (mouseX >= x && mouseX < x + w)
                    return id;
                x += w;
            }
            return null;
        }
    }
}
