using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// A <see cref="RichTextBox"/> with a custom-painted vertical scrollbar thumb
    /// and optional native horizontal scrollbar.
    ///
    /// Design constraints that drove every decision here:
    ///
    /// 1.  <see cref="RichTextBox"/> is a Win32 RICHEDIT control. WinForms does NOT
    ///     call <see cref="Control.OnPaint"/> for it (no <c>ControlStyles.UserPaint</c>).
    ///     Drawing must happen via <c>Graphics.FromHwnd</c> after the native paint.
    ///
    /// 2.  Setting <c>ScrollBars = Vertical</c> (or <c>Both</c>) tells the RTB it
    ///     OWNS the scrollbar. Every <c>WM_SIZE</c>, content change, or
    ///     <see cref="ShowScrollBar"/> call causes the RTB to re-show the native bar
    ///     and post another <c>WM_PAINT</c>/<c>WM_SIZE</c> — an infinite loop.
    ///     The only way to stop this is <c>ScrollBars = None</c>.
    ///
    /// 3.  With <c>ScrollBars = None</c>, <c>GetScrollInfo</c> returns nothing because
    ///     no scrollbar window exists. We need scroll metrics for the custom thumb.
    ///     Solution: add <c>WS_VSCROLL</c> to <see cref="CreateParams"/> directly.
    ///     This gives Windows a scroll bar object (and thus valid <c>SCROLLINFO</c>)
    ///     without the RTB's managed <c>ScrollBars</c> property knowing about it.
    ///     The native bar is hidden by default via <c>WS_VSCROLL</c> without
    ///     <c>ES_DISABLENOSCROLL</c> — it only appears when there is scrollable
    ///     content and we never explicitly show it, so it stays hidden.
    ///
    /// 4.  The native horizontal bar (<c>WS_HSCROLL</c>) is left as-is when
    ///     <see cref="ShowHorizontalScrollbar"/> is true — we don't touch it at all.
    ///
    /// 5.  <c>ShowScrollBar</c> is NEVER called. Calling it triggers layout
    ///     recalculations that feed back into WM_SIZE ? more repaints.
    ///
    /// 6.  <c>DrawThumb</c> is called only from <c>WM_PAINT</c> after
    ///     <c>base.WndProc</c> finishes. <c>Invalidate()</c> is called only from
    ///     scroll/mouse events. These two never call each other — no loop.
    /// </summary>
    internal class ThemedRichTextBox : RichTextBox
    {
        // ?? Win32 constants ???????????????????????????????????????????????
        private const int WS_VSCROLL      = 0x00200000;
        private const int WS_HSCROLL      = 0x00100000;
        private const int SB_VERT         = 1;
        private const int SIF_ALL         = 0x0017;

        private const int WM_PAINT        = 0x000F;
        private const int WM_VSCROLL      = 0x0115;
        private const int WM_HSCROLL      = 0x0114;
        private const int WM_MOUSEWHEEL   = 0x020A;
        private const int EM_SETRECT      = 0x00B3;
        private const int EM_SETSCROLLPOS = 0x04DD;

        private const int BarWidth          = 12;
        private const int ScrollGutterWidth = BarWidth + 6;
        private const int MinThumbHeight    = 24;
        private const int ThumbRadius       = 4;

        // ?? State ?????????????????????????????????????????????????????????
        private bool _thumbHovered;
        private bool _thumbDragging;
        private int  _dragStartMouseY;
        private int  _dragStartScrollY;
        private bool _inPaint;              // re-entrancy guard for DrawThumb
        private bool _formattingRectSet;    // track whether EM_SETRECT has been applied

        /// <summary>
        /// Always show the vertical thumb regardless of focus or mouse position.
        /// Useful for read-only log/terminal viewers.
        /// </summary>
        public bool AlwaysShowScrollbar { get; set; }

        /// <summary>
        /// When true, the native horizontal scrollbar is visible for non-word-wrap
        /// viewers. Has no effect when <see cref="RichTextBox.WordWrap"/> is true.
        /// </summary>
        public bool ShowHorizontalScrollbar { get; set; }

        // ?? P/Invoke ??????????????????????????????????????????????????????
        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize, fMask;
            public int  nMin, nMax;
            public uint nPage;
            public int  nPos, nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] private static extern int    GetScrollInfo(IntPtr hwnd, int nBar, ref SCROLLINFO lpsi);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref POINT lParam);

        // ?? Constructor ???????????????????????????????????????????????????
        public ThemedRichTextBox()
        {
            // CRITICAL: None means the RTB will never try to manage or re-show
            // a native vertical bar. We inject WS_VSCROLL below in CreateParams
            // so Windows still maintains SCROLLINFO for us.
            ScrollBars  = RichTextBoxScrollBars.None;
            BorderStyle = BorderStyle.None;
        }

        // ?? CreateParams — inject WS_VSCROLL without using ScrollBars ?????
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Add WS_VSCROLL so Windows maintains a SCROLLINFO structure
                // for this window (enabling GetScrollInfo). The bar itself is
                // not shown because we never call ShowScrollBar(true) and the
                // RTB's ScrollBars property is None — so the bar has no visual
                // representation unless content actually overflows.
                cp.Style |= WS_VSCROLL;
                if (ShowHorizontalScrollbar)
                    cp.Style |= WS_HSCROLL;
                return cp;
            }
        }

        // ?? Handle lifecycle ??????????????????????????????????????????????
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _formattingRectSet = false;
            UpdateFormattingRect();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateFormattingRect();
            // Don't Invalidate here — WM_SIZE fires from base and we handle
            // it in WndProc below, which calls Invalidate once.
        }

        // ?? WndProc ???????????????????????????????????????????????????????
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (!IsHandleCreated) return;

            switch (m.Msg)
            {
                case WM_PAINT:
                    // base.WndProc has fully completed the native paint at this
                    // point — safe to overdraw. _inPaint guards against the
                    // extremely unlikely case of re-entrant WM_PAINT.
                    if (!_inPaint)
                    {
                        _inPaint = true;
                        try { DrawThumb(); }
                        finally { _inPaint = false; }
                    }
                    break;

                case WM_VSCROLL:
                case WM_HSCROLL:
                case WM_MOUSEWHEEL:
                    // Scroll position changed — repaint so thumb moves.
                    // Do NOT call Invalidate from WM_PAINT (that's the loop).
                    Invalidate();
                    break;

                case 0x0005: // WM_SIZE
                    // Content or size changed — re-apply formatting rect and
                    // repaint. UpdateFormattingRect is safe here because it
                    // only calls SendMessage(EM_SETRECT), not ShowScrollBar.
                    UpdateFormattingRect();
                    Invalidate();
                    break;
            }
        }

        // ?? Draw thumb ????????????????????????????????????????????????????
        private void DrawThumb()
        {
            if (!ShouldRenderScrollbar) return;
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            // Graphics.FromHwnd after WM_PAINT is complete — safe, not erased.
            using var g = Graphics.FromHwnd(Handle);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Track
            var track = GetTrackRect();
            using (var tb = new SolidBrush(Color.FromArgb(45, 255, 255, 255)))
                g.FillRectangle(tb, track);

            var thumb = GetThumbRect();
            if (thumb.IsEmpty) return;

            Color thumbColor = _thumbDragging    ? WallyTheme.TextPrimary
                             : _thumbHovered     ? WallyTheme.TextSecondary
                             : AlwaysShowScrollbar ? WallyTheme.TextMuted
                                                  : WallyTheme.TextDisabled;

            using var path  = WallyTheme.RoundedRect(thumb, ThumbRadius);
            using var brush = new SolidBrush(thumbColor);
            g.FillPath(brush, path);
        }

        // ?? Scroll metrics ????????????????????????????????????????????????
        private readonly struct ScrollMetrics
        {
            public readonly int Position, MaxScroll, Page;
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

        // ?? Visibility decision ???????????????????????????????????????????
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

        // ?? Geometry ??????????????????????????????????????????????????????
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

            int   total  = m.MaxScroll + m.Page;
            float ratio  = (float)m.Page / total;
            int   thumbH = Math.Max(MinThumbHeight, (int)(ClientSize.Height * ratio));
            int   maxY   = Math.Max(0, ClientSize.Height - thumbH);
            int   thumbY = (int)(maxY * ((float)m.Position / m.MaxScroll));

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
                        float ratio   = (float)(e.Y - _dragStartMouseY) / trackRange;
                        int newScroll = _dragStartScrollY + (int)(ratio * m.MaxScroll);
                        SetScrollPosition(newScroll);
                    }
                }
                Invalidate();
                return;
            }

            base.OnMouseMove(e);

            bool wasHovered = _thumbHovered;
            _thumbHovered = ShouldRenderScrollbar && GetThumbRect().Contains(e.Location);
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

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnEnter(EventArgs e)       { base.OnEnter(e);       Invalidate(); }
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            _thumbHovered = false;
            Invalidate();
        }

        private bool TryHandleScrollbarMouseDown(MouseEventArgs e)
        {
            if (!ShouldRenderScrollbar || e.Button != MouseButtons.Left) return false;
            var track = GetTrackRect();
            if (!track.Contains(e.Location)) return false;

            Focus();
            var thumb = GetThumbRect();
            if (thumb.Contains(e.Location))
            {
                _thumbDragging    = true;
                _dragStartMouseY  = e.Y;
                _dragStartScrollY = TryGetScrollMetrics(out var m) ? m.Position : 0;
                Capture           = true;
            }
            else
            {
                // Page up / down
                SendMessage(Handle, WM_VSCROLL, (IntPtr)(e.Y < thumb.Y ? 2 : 3), IntPtr.Zero);
            }
            Invalidate();
            return true;
        }

        private void SetScrollPosition(int position)
        {
            if (!TryGetScrollMetrics(out var m)) return;
            position = Math.Clamp(position, 0, m.MaxScroll);
            var pt   = new POINT { X = 0, Y = position };
            SendMessage(Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref pt);
            Invalidate();
        }

        // ?? Formatting rect ???????????????????????????????????????????????
        /// <summary>
        /// Reserves the right-hand gutter for the custom scrollbar so text
        /// does not flow underneath the thumb. Only applied when word-wrap is
        /// on; non-word-wrap viewers use the full width for horizontal scroll.
        /// </summary>
        private void UpdateFormattingRect()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

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
            _formattingRectSet = true;
        }
    }
}
