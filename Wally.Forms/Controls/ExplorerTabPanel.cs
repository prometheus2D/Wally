using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// Tabbed left-panel host that presents three explorer views:
    /// <list type="bullet">
    ///   <item><b>Files</b>   — raw filesystem tree rooted at the workspace WorkSource.</item>
    ///   <item><b>Project</b> — workspace items grouped by type (Actors, Loops, Wrappers…).</item>
    ///   <item><b>Objects</b> — live runtime objects with inspectable properties.</item>
    /// </list>
    /// The tab strip is custom-drawn to match the flat dark theme and suppresses
    /// all default OS selection highlights.
    /// </summary>
    public sealed class ExplorerTabPanel : UserControl
    {
        // ?? Tab definitions ?????????????????????????????????????????????????

        private enum Tab { Files, Project, Objects }

        private static readonly (Tab Id, string Label, string Emoji)[] _tabs =
        {
            (Tab.Files,   "FILES",   "\uD83D\uDCC1"),
            (Tab.Project, "PROJECT", "\uD83D\uDDC2"),
            (Tab.Objects, "OBJECTS", "\uD83E\uDDE9"),
        };

        // ?? Controls ????????????????????????????????????????????????????????

        private readonly Panel              _tabBar;
        private readonly Panel              _body;
        private readonly FileExplorerPanel  _fileExplorer;
        private readonly ProjectExplorerPanel _projectExplorer;
        private readonly ObjectExplorerPanel  _objectExplorer;

        // ?? Tab bar metrics ?????????????????????????????????????????????????

        private const int TabBarHeight = 32;
        private const int TabPadH      = 14;   // horizontal padding inside each tab
        private const int AccentBarH   = 2;    // active-tab underline thickness

        // ?? State ???????????????????????????????????????????????????????????

        private Tab    _activeTab  = Tab.Files;
        private Tab?   _hoverTab   = null;
        private WallyEnvironment? _environment;

        // ?? Events re-exposed from child panels ?????????????????????????????

        public event EventHandler<FileSelectedEventArgs>? FileDoubleClicked;
        public event EventHandler<FileSelectedEventArgs>? FileSelected;

        /// <summary>Raised when the user double-clicks an Actor in the Object Explorer.</summary>
        public event EventHandler<string>? ActorActivated;
        /// <summary>Raised when the user double-clicks a Loop in the Object Explorer.</summary>
        public event EventHandler<string>? LoopActivated;
        /// <summary>Raised when the user double-clicks a Wrapper in the Object Explorer.</summary>
        public event EventHandler<string>? WrapperActivated;
        /// <summary>Raised when the user double-clicks a Runbook in the Object Explorer.</summary>
        public event EventHandler<string>? RunbookActivated;

        // ?? Constructor ?????????????????????????????????????????????????????

        public ExplorerTabPanel()
        {
            SuspendLayout();

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

            // Bottom border under tab bar
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
                BackColor = WallyTheme.Surface1
            };

            // ?? Child explorers ??
            _fileExplorer = new FileExplorerPanel    { Dock = DockStyle.Fill };
            _projectExplorer = new ProjectExplorerPanel { Dock = DockStyle.Fill };
            _objectExplorer  = new ObjectExplorerPanel  { Dock = DockStyle.Fill };

            _body.Controls.Add(_fileExplorer);
            _body.Controls.Add(_projectExplorer);
            _body.Controls.Add(_objectExplorer);

            // ?? Wire child events ??
            _fileExplorer.FileDoubleClicked += (s, e) => FileDoubleClicked?.Invoke(s, e);
            _fileExplorer.FileSelected      += (s, e) => FileSelected?.Invoke(s, e);
            _projectExplorer.FileDoubleClicked += (s, e) => FileDoubleClicked?.Invoke(s, e);
            _projectExplorer.FileSelected      += (s, e) => FileSelected?.Invoke(s, e);
            _objectExplorer.ActorActivated   += (s, e) => ActorActivated?.Invoke(s, e);
            _objectExplorer.LoopActivated    += (s, e) => LoopActivated?.Invoke(s, e);
            _objectExplorer.WrapperActivated += (s, e) => WrapperActivated?.Invoke(s, e);
            _objectExplorer.RunbookActivated += (s, e) => RunbookActivated?.Invoke(s, e);

            // ?? Assembly ??
            Controls.Add(_body);
            Controls.Add(tabBorder);
            Controls.Add(_tabBar);

            BackColor = WallyTheme.Surface1;

            ApplyActiveTab();

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
            _projectExplorer.BindEnvironment(env);
            _objectExplorer.BindEnvironment(env);
        }

        /// <summary>Called when a workspace is loaded. Sets the root and refreshes all panes.</summary>
        public void SetRootPath(string rootPath)
        {
            _fileExplorer.SetRootPath(rootPath);
            _projectExplorer.SetWorkspace();
            _objectExplorer.Refresh();
        }

        /// <summary>Called when the workspace is closed.</summary>
        public void ClearAll()
        {
            _fileExplorer.ClearTree();
            _projectExplorer.ClearTree();
            _objectExplorer.ClearTree();
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();
            _fileExplorer.Refresh();
            _projectExplorer.Refresh();
            _objectExplorer.Refresh();
        }

        /// <summary>Refreshes only the Object Explorer (cheap — no disk I/O).</summary>
        public void RefreshObjects() => _objectExplorer.Refresh();

        /// <summary>Switches to the Files tab and focuses the tree.</summary>
        public void FocusFiles()
        {
            SetActiveTab(Tab.Files);
            _fileExplorer.Focus();
        }

        // ?? Tab switching ???????????????????????????????????????????????????

        private void SetActiveTab(Tab tab)
        {
            _activeTab = tab;
            ApplyActiveTab();
            _tabBar.Invalidate();
        }

        private void ApplyActiveTab()
        {
            _fileExplorer.Visible    = _activeTab == Tab.Files;
            _projectExplorer.Visible = _activeTab == Tab.Project;
            _objectExplorer.Visible  = _activeTab == Tab.Objects;
        }

        // ?? Tab bar painting ????????????????????????????????????????????????

        private void OnTabBarPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.TextRenderingHint  = TextRenderingHint.ClearTypeGridFit;

            // Fill background
            using (var bgBrush = new SolidBrush(WallyTheme.Surface2))
                g.FillRectangle(bgBrush, _tabBar.ClientRectangle);

            int x = 0;
            foreach (var (id, label, emoji) in _tabs)
            {
                int w = MeasureTabWidth(label);
                var rect = new Rectangle(x, 0, w, TabBarHeight);

                bool isActive = id == _activeTab;
                bool isHover  = id == _hoverTab && !isActive;

                // Hover / active background
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

                // Active underline accent bar
                if (isActive)
                {
                    using var accent = new SolidBrush(WallyTheme.Accent);
                    g.FillRectangle(accent,
                        new Rectangle(rect.X, rect.Bottom - AccentBarH, rect.Width, AccentBarH));
                }

                // Label text
                Color fg = isActive  ? WallyTheme.TextPrimary
                         : isHover   ? WallyTheme.TextSecondary
                                     : WallyTheme.TextMuted;

                string text = $"{emoji}  {label}";
                var font = isActive ? WallyTheme.FontUISmallBold : WallyTheme.FontUISmall;

                TextRenderer.DrawText(g, text, font,
                    new Rectangle(rect.X + TabPadH / 2, rect.Y, rect.Width - TabPadH / 2, rect.Height - AccentBarH),
                    fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                x += w;
            }
        }

        private int MeasureTabWidth(string label)
        {
            // Use a consistent fixed width wide enough for all labels + padding
            using var g = _tabBar.CreateGraphics();
            int textW = TextRenderer.MeasureText(g, $"??  {label}", WallyTheme.FontUISmallBold).Width;
            return textW + TabPadH * 2;
        }

        // ?? Tab bar interaction ?????????????????????????????????????????????

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
            _hoverTab = null;
            _tabBar.Invalidate();
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
