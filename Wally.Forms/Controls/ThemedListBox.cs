using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    internal class ThemedListBox : ListBox
    {
        private const int SB_VERT = 1;
        private const int SIF_RANGE = 0x0001;
        private const int SIF_PAGE = 0x0002;
        private const int SIF_POS = 0x0004;
        private const int SIF_TRACKPOS = 0x0010;
        private const int SIF_ALL = SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS;

        private const int WM_PAINT = 0x000F;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_SIZE = 0x0005;

        private const int SB_PAGEUP = 2;
        private const int SB_PAGEDOWN = 3;
        private const int SB_THUMBPOSITION = 4;

        private const int BarWidth = 12;
        private const int MinThumbHeight = 24;
        private const int ThumbRadius = 4;

        private bool _thumbHovered;
        private bool _thumbDragging;
        private int _dragStartMouseY;
        private int _dragStartScrollY;

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        private readonly struct ScrollMetrics
        {
            public ScrollMetrics(int position, int maxScroll, int page)
            {
                Position = position;
                MaxScroll = maxScroll;
                Page = page;
            }

            public int Position { get; }
            public int MaxScroll { get; }
            public int Page { get; }
        }

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        [DllImport("user32.dll")]
        private static extern int GetScrollInfo(IntPtr hwnd, int nBar, ref SCROLLINFO lpsi);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            HideNativeScrollbar();
            Invalidate();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            _thumbHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (TryHandleScrollbarMouseDown(e))
                return;

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_thumbDragging)
            {
                if (TryGetScrollMetrics(out var metrics))
                {
                    int thumbHeight = GetThumbRect(metrics).Height;
                    int trackRange = ClientSize.Height - thumbHeight;
                    if (trackRange > 0)
                    {
                        int deltaY = e.Y - _dragStartMouseY;
                        float scrollRatio = (float)deltaY / trackRange;
                        int newScroll = _dragStartScrollY + (int)(scrollRatio * metrics.MaxScroll);
                        SetScrollPosition(newScroll);
                    }
                }

                Invalidate();
                return;
            }

            base.OnMouseMove(e);

            bool wasHovered = _thumbHovered;
            _thumbHovered = ShouldRenderScrollbar && GetThumbRect().Contains(e.Location);
            if (wasHovered != _thumbHovered)
                Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_thumbDragging)
            {
                _thumbDragging = false;
                Capture = false;
                Invalidate();
                return;
            }

            base.OnMouseUp(e);
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

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (!IsHandleCreated)
                return;

            switch (m.Msg)
            {
                case WM_PAINT:
                case WM_MOUSEWHEEL:
                case WM_VSCROLL:
                case WM_NCCALCSIZE:
                case WM_SIZE:
                    HideNativeScrollbar();
                    DrawOverlay();
                    break;
            }
        }

        private bool NeedsScrollbar => TryGetScrollMetrics(out var metrics) && metrics.MaxScroll > 0 && metrics.Page > 0;

        private bool ShouldRenderScrollbar
        {
            get
            {
                if (!NeedsScrollbar)
                    return false;

                if (_thumbDragging || Focused)
                    return true;

                var mouse = PointToClient(Cursor.Position);
                return ClientRectangle.Contains(mouse);
            }
        }

        private Rectangle GetTrackRect() =>
            new(ClientSize.Width - BarWidth - 2, 0, BarWidth, ClientSize.Height);

        private Rectangle GetThumbRect()
        {
            if (!TryGetScrollMetrics(out var metrics))
                return Rectangle.Empty;

            return GetThumbRect(metrics);
        }

        private Rectangle GetThumbRect(ScrollMetrics metrics)
        {
            if (metrics.Page <= 0 || metrics.MaxScroll <= 0)
                return Rectangle.Empty;

            int contentHeight = metrics.MaxScroll + metrics.Page;
            if (contentHeight <= 0)
                return Rectangle.Empty;

            float ratio = (float)metrics.Page / contentHeight;
            int thumbHeight = Math.Max(MinThumbHeight, (int)(ClientSize.Height * ratio));
            int maxThumbY = Math.Max(0, ClientSize.Height - thumbHeight);
            int thumbY = metrics.MaxScroll > 0
                ? (int)(maxThumbY * ((float)metrics.Position / metrics.MaxScroll))
                : 0;

            return new Rectangle(
                ClientSize.Width - BarWidth - 1,
                thumbY + 1,
                BarWidth - 2,
                Math.Max(2, thumbHeight - 2));
        }

        private void DrawOverlay()
        {
            if (!Visible || ClientSize.Width <= 0 || ClientSize.Height <= 0 || !ShouldRenderScrollbar)
                return;

            using var g = Graphics.FromHwnd(Handle);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var track = GetTrackRect();
            using (var trackBrush = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
                g.FillRectangle(trackBrush, track);

            var thumb = GetThumbRect();
            if (thumb.IsEmpty)
                return;

            Color thumbColor = _thumbDragging
                ? WallyTheme.TextMuted
                : _thumbHovered
                    ? WallyTheme.Surface4
                    : WallyTheme.Surface3;

            using var path = WallyTheme.RoundedRect(thumb, ThumbRadius);
            using var brush = new SolidBrush(thumbColor);
            g.FillPath(brush, path);
        }

        private bool TryHandleScrollbarMouseDown(MouseEventArgs e)
        {
            if (!ShouldRenderScrollbar || e.Button != MouseButtons.Left)
                return false;

            var thumb = GetThumbRect();
            var track = GetTrackRect();
            if (!thumb.Contains(e.Location) && !track.Contains(e.Location))
                return false;

            Focus();

            if (thumb.Contains(e.Location))
            {
                _thumbDragging = true;
                _dragStartMouseY = e.Y;
                _dragStartScrollY = TryGetScrollMetrics(out var metrics) ? metrics.Position : 0;
                Capture = true;
            }
            else if (e.Y < thumb.Y)
            {
                PageScroll(SB_PAGEUP);
            }
            else
            {
                PageScroll(SB_PAGEDOWN);
            }

            Invalidate();
            return true;
        }

        private void PageScroll(int direction)
        {
            SendMessage(Handle, WM_VSCROLL, MakeWParam(direction, 0), IntPtr.Zero);
            Invalidate();
        }

        private void SetScrollPosition(int position)
        {
            if (!TryGetScrollMetrics(out var metrics))
                return;

            position = Math.Clamp(position, 0, metrics.MaxScroll);
            SendMessage(Handle, WM_VSCROLL, MakeWParam(SB_THUMBPOSITION, position), IntPtr.Zero);
            Invalidate();
        }

        private bool TryGetScrollMetrics(out ScrollMetrics metrics)
        {
            metrics = default;
            if (!IsHandleCreated)
                return false;

            var info = new SCROLLINFO
            {
                cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(),
                fMask = SIF_ALL
            };

            if (GetScrollInfo(Handle, SB_VERT, ref info) == 0)
                return false;

            int page = (int)info.nPage;
            int maxScroll = Math.Max(0, info.nMax - Math.Max(1, page) + 1 - info.nMin);
            int position = Math.Clamp(info.nPos - info.nMin, 0, maxScroll);
            metrics = new ScrollMetrics(position, maxScroll, page);
            return true;
        }

        private void HideNativeScrollbar()
        {
            if (IsHandleCreated)
                ShowScrollBar(Handle, SB_VERT, false);
        }

        private static IntPtr MakeWParam(int low, int high) =>
            (IntPtr)(((high & 0xFFFF) << 16) | (low & 0xFFFF));
    }
}
