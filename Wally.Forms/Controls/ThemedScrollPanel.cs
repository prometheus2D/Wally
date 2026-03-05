using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// A themed scrollable panel that hides the native Windows scrollbar and
    /// paints a slim, dark overlay scrollbar that blends with the Wally UI.
    /// <para>
    /// Drop-in replacement for <c>new Panel { AutoScroll = true }</c>.
    /// Just add child controls to this panel exactly as you would a normal
    /// auto-scroll panel — the themed scrollbar appears automatically when
    /// content exceeds the visible area.
    /// </para>
    /// </summary>
    internal class ThemedScrollPanel : Panel
    {
        // ?? Win32 interop — hide native scrollbar ???????????????????????????

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        private const int SB_VERT = 1;

        // ?? Constants ???????????????????????????????????????????????????????

        private const int BarWidth = 8;
        private const int MinThumbHeight = 24;
        private const int ThumbRadius = 4;

        // ?? State ???????????????????????????????????????????????????????????

        private bool _thumbHovered;
        private bool _thumbDragging;
        private int _dragStartMouseY;
        private int _dragStartScrollY;

        // ?? Constructor ?????????????????????????????????????????????????????

        public ThemedScrollPanel()
        {
            AutoScroll = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);
        }

        // ?? Suppress native scrollbar on every layout pass ??????????????????

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (IsHandleCreated && VerticalScroll.Visible)
                ShowScrollBar(Handle, SB_VERT, false);
        }

        /// <summary>Override WndProc to keep the native scrollbar hidden.</summary>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // WM_NCCALCSIZE — after base processes it, hide scrollbar again.
            if (m.Msg == 0x0083 && IsHandleCreated)
                ShowScrollBar(Handle, SB_VERT, false);
        }

        // ?? Geometry helpers ????????????????????????????????????????????????

        private bool NeedsScrollbar => VerticalScroll.Visible ||
                                       DisplayRectangle.Height > ClientSize.Height;

        private int ContentHeight => DisplayRectangle.Height;
        private int ViewHeight => ClientSize.Height;
        private int ScrollY => VerticalScroll.Value;
        private int MaxScrollY => Math.Max(0, ContentHeight - ViewHeight);

        private Rectangle GetTrackRect() =>
            new(ClientSize.Width - BarWidth, 0, BarWidth, ViewHeight);

        private Rectangle GetThumbRect()
        {
            if (ContentHeight <= 0 || ViewHeight >= ContentHeight)
                return Rectangle.Empty;

            float ratio = (float)ViewHeight / ContentHeight;
            int thumbH = Math.Max(MinThumbHeight, (int)(ViewHeight * ratio));
            int maxY = MaxScrollY;
            int thumbY = maxY > 0
                ? (int)((ViewHeight - thumbH) * ((float)ScrollY / maxY))
                : 0;

            return new Rectangle(
                ClientSize.Width - BarWidth + 1,
                thumbY + 1,
                BarWidth - 2,
                thumbH - 2);
        }

        // ?? Paint the overlay scrollbar ?????????????????????????????????????

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!NeedsScrollbar) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Track — very subtle
            var track = GetTrackRect();
            using (var trackBrush = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
                g.FillRectangle(trackBrush, track);

            // Thumb
            var thumb = GetThumbRect();
            if (thumb.IsEmpty) return;

            Color thumbColor = _thumbDragging
                ? WallyTheme.TextMuted
                : _thumbHovered
                    ? WallyTheme.Surface4
                    : WallyTheme.Surface3;

            using var path = WallyTheme.RoundedRect(thumb, ThumbRadius);
            using var brush = new SolidBrush(thumbColor);
            g.FillPath(brush, path);
        }

        // ?? Mouse interaction ???????????????????????????????????????????????

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_thumbDragging)
            {
                int thumbH = GetThumbRect().Height;
                int trackRange = ViewHeight - thumbH;
                if (trackRange > 0)
                {
                    int deltaY = e.Y - _dragStartMouseY;
                    float scrollRatio = (float)deltaY / trackRange;
                    int newScroll = _dragStartScrollY + (int)(scrollRatio * MaxScrollY);
                    newScroll = Math.Clamp(newScroll, 0, MaxScrollY);
                    AutoScrollPosition = new Point(0, newScroll);
                }
                Invalidate();
                return;
            }

            bool wasHovered = _thumbHovered;
            _thumbHovered = NeedsScrollbar && GetThumbRect().Contains(e.Location);
            if (wasHovered != _thumbHovered)
                Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!NeedsScrollbar || e.Button != MouseButtons.Left) return;

            var thumb = GetThumbRect();

            if (thumb.Contains(e.Location))
            {
                _thumbDragging = true;
                _dragStartMouseY = e.Y;
                _dragStartScrollY = ScrollY;
                Capture = true;
                Invalidate();
            }
            else if (GetTrackRect().Contains(e.Location))
            {
                // Page-scroll on track click
                int page = ViewHeight;
                int newScroll = e.Y < thumb.Y
                    ? Math.Max(0, ScrollY - page)
                    : Math.Min(MaxScrollY, ScrollY + page);
                AutoScrollPosition = new Point(0, newScroll);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_thumbDragging)
            {
                _thumbDragging = false;
                Capture = false;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_thumbHovered)
            {
                _thumbHovered = false;
                Invalidate();
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Invalidate();
        }
    }
}
