using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    internal class ThemedRichTextBox : RichTextBox
    {
        // ?? Win32 constants ???????????????????????????????????????????????
        private const int SB_VERT          = 1;
        private const int SB_HORZ          = 0;
        private const int SIF_ALL          = 0x0017;   // RANGE | PAGE | POS | TRACKPOS

        private const int WM_VSCROLL       = 0x0115;
        private const int WM_MOUSEWHEEL    = 0x020A;
        private const int EM_SETRECT       = 0x00B3;
        private const int EM_SETSCROLLPOS  = 0x04DD;

        private const int BarWidth         = 12;
        private const int ScrollGutterWidth = BarWidth + 6;
        private const int MinThumbHeight   = 24;
        private const int ThumbRadius      = 4;

        // ?? State ?????????????????????????????????????????????????????????
        private bool _thumbHovered;
        private bool _thumbDragging;
        private int  _dragStartMouseY;
        private int  _dragStartScrollY;

        /// <summary>Always render the thumb (not just on hover/focus).</summary>
        public bool AlwaysShowScrollbar { get; set; }

        // ?? P/Invoke ??????????????????????????????????????????????????????
        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int  nMin;
            public int  nMax;
            public uint nPage;
            public int  nPos;
            public int  nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("user32.dll")] private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        [DllImport("user32.dll")] private static extern int  GetScrollInfo(IntPtr hwnd, int nBar, ref SCROLLINFO lpsi);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref POINT lParam);

        // ?? Constructor ???????????????????????????????????????????????????
        public ThemedRichTextBox()
        {
            // Vertical must be set so the RTB tracks scroll info internally.
            // We hide it visually in OnHandleCreated. Horizontal kept for
            // callers that explicitly want it (word-wrap off mode).
            ScrollBars  = RichTextBoxScrollBars.Vertical;
            BorderStyle = BorderStyle.None;

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        // ?? Handle lifecycle ??????????????????????????????????????????????
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            HideNativeVerticalBar();
            UpdateFormattingRect();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            HideNativeVerticalBar();
            UpdateFormattingRect();
            Invalidate();
        }

        // ?? Paint ?????????????????????????????????????????????????????????
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawScrollThumb(e.Graphics);
        }

        // WM_VSCROLL and WM_MOUSEWHEEL change the scroll position but don't
        // always trigger a full repaint — nudge Invalidate so the thumb moves.
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
                Invalidate();
        }

        // ?? Draw the custom thumb ?????????????????????????????????????????
        private void DrawScrollThumb(Graphics g)
        {
            if (!ShouldRenderScrollbar) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Track background
            var track = GetTrackRect();
            using (var b = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                g.FillRectangle(b, track);

            var thumb = GetThumbRect();
            if (thumb.IsEmpty) return;

            Color thumbColor = _thumbDragging  ? WallyTheme.TextMuted
                             : _thumbHovered   ? WallyTheme.Surface4
                             : AlwaysShowScrollbar ? WallyTheme.Surface3
                                                : WallyTheme.Surface2;

            using var path  = WallyTheme.RoundedRect(thumb, ThumbRadius);
            using var brush = new SolidBrush(thumbColor);
            g.FillPath(brush, path);
        }

        // ?? Scroll metrics ????????????????????????????????????????????????
        private readonly struct ScrollMetrics
        {
            public readonly int Position;
            public readonly int MaxScroll;
            public readonly int Page;
            public ScrollMetrics(int pos, int max, int page)
            { Position = pos; MaxScroll = max; Page = page; }
        }

        private bool TryGetScrollMetrics(out ScrollMetrics m)
        {
            m = default;
            if (!IsHandleCreated) return false;

            var info = new SCROLLINFO
            {
                cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(),
                fMask  = SIF_ALL
            };
            if (GetScrollInfo(Handle, SB_VERT, ref info) == 0) return false;

            int page      = (int)info.nPage;
            int maxScroll = Math.Max(0, info.nMax - Math.Max(1, page) + 1 - info.nMin);
            int position  = Math.Clamp(info.nPos - info.nMin, 0, maxScroll);
            m = new ScrollMetrics(position, maxScroll, page);
            return true;
        }

        // ?? Thumb geometry ????????????????????????????????????????????????
        private bool NeedsScrollbar =>
            TryGetScrollMetrics(out var m) && m.MaxScroll > 0 && m.Page > 0;

        private bool ShouldRenderScrollbar
        {
            get
            {
                if (!NeedsScrollbar) return false;
                if (AlwaysShowScrollbar || _thumbDragging || Focused) return true;
                return ClientRectangle.Contains(PointToClient(Cursor.Position));
            }
        }

        private Rectangle GetTrackRect() =>
            new(ClientSize.Width - BarWidth - 2, 0, BarWidth, ClientSize.Height);

        private Rectangle GetThumbRect()
        {
            if (!TryGetScrollMetrics(out var m)) return Rectangle.Empty;
            return GetThumbRect(m);
        }

        private Rectangle GetThumbRect(ScrollMetrics m)
        {
            if (m.Page <= 0 || m.MaxScroll <= 0) return Rectangle.Empty;

            int  total       = m.MaxScroll + m.Page;
            float ratio      = (float)m.Page / total;
            int  thumbH      = Math.Max(MinThumbHeight, (int)(ClientSize.Height * ratio));
            int  maxThumbY   = Math.Max(0, ClientSize.Height - thumbH);
            int  thumbY      = (int)(maxThumbY * ((float)m.Position / m.MaxScroll));

            return new Rectangle(
                ClientSize.Width - BarWidth - 1,
                thumbY + 1,
                BarWidth - 2,
                Math.Max(2, thumbH - 2));
        }

        // ?? Mouse handling ????????????????????????????????????????????????
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (TryHandleScrollbarMouseDown(e)) return;
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_thumbDragging)
            {
                if (TryGetScrollMetrics(out var m))
                {
                    int thumbH     = GetThumbRect(m).Height;
                    int trackRange = ClientSize.Height - thumbH;
                    if (trackRange > 0)
                    {
                        int deltaY     = e.Y - _dragStartMouseY;
                        float ratio    = (float)deltaY / trackRange;
                        int newScroll  = _dragStartScrollY + (int)(ratio * m.MaxScroll);
                        SetScrollPosition(newScroll);
                    }
                }
                Invalidate();
                return;
            }

            base.OnMouseMove(e);

            bool wasHovered = _thumbHovered;
            _thumbHovered   = ShouldRenderScrollbar && GetThumbRect().Contains(e.Location);
            if (wasHovered != _thumbHovered) Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_thumbDragging)
            {
                _thumbDragging = false;
                Capture        = false;
                Invalidate();
                return;
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_thumbHovered) { _thumbHovered = false; Invalidate(); }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnEnter(EventArgs e) { base.OnEnter(e); Invalidate(); }
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            _thumbHovered = false;
            Invalidate();
        }

        private bool TryHandleScrollbarMouseDown(MouseEventArgs e)
        {
            if (!ShouldRenderScrollbar || e.Button != MouseButtons.Left) return false;

            var thumb = GetThumbRect();
            var track = GetTrackRect();
            if (!track.Contains(e.Location)) return false;

            Focus();

            if (thumb.Contains(e.Location))
            {
                _thumbDragging    = true;
                _dragStartMouseY  = e.Y;
                _dragStartScrollY = TryGetScrollMetrics(out var m) ? m.Position : 0;
                Capture           = true;
            }
            else if (e.Y < thumb.Y)
            {
                // Page up — use the keyboard equivalent so RTB handles it
                SendMessage(Handle, WM_VSCROLL, (IntPtr)2 /* SB_PAGEUP */,   IntPtr.Zero);
            }
            else
            {
                SendMessage(Handle, WM_VSCROLL, (IntPtr)3 /* SB_PAGEDOWN */, IntPtr.Zero);
            }

            Invalidate();
            return true;
        }

        // ?? Programmatic scroll position ??????????????????????????????????
        private void SetScrollPosition(int position)
        {
            if (!TryGetScrollMetrics(out var m)) return;

            position = Math.Clamp(position, 0, m.MaxScroll);

            // EM_SETSCROLLPOS is the correct way to move a RichTextBox scroll
            // position — SB_THUMBPOSITION does NOT reliably reposition the RTB.
            var pt = new POINT { X = 0, Y = position };
            SendMessage(Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref pt);
            Invalidate();
        }

        // ?? Native bar management ?????????????????????????????????????????
        /// <summary>
        /// Hides the native vertical scrollbar. Called only on handle creation
        /// and resize — NOT on every paint — to avoid the re-layout storm that
        /// causes flicker.
        /// </summary>
        private void HideNativeVerticalBar()
        {
            if (IsHandleCreated)
                ShowScrollBar(Handle, SB_VERT, false);
        }

        // ?? Formatting rect ???????????????????????????????????????????????
        private void UpdateFormattingRect()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            int rightEdge = WordWrap
                ? Math.Max(2, ClientSize.Width - ScrollGutterWidth)
                : Math.Max(2, ClientSize.Width - 2);

            var rect = new RECT
            {
                Left   = 2,
                Top    = 2,
                Right  = rightEdge,
                Bottom = Math.Max(2, ClientSize.Height - 2)
            };
            SendMessage(Handle, EM_SETRECT, IntPtr.Zero, ref rect);
        }
    }
}
