using System;
using System.Drawing;
using System.Windows.Forms;

namespace Wally.Forms.Controls
{
    internal class AutoHeightThemedRichTextBox : ThemedRichTextBox
    {
        private bool _adjustingHeight;
        private bool _adjustHeightQueued;
        private int _minimumAutoHeight = 80;

        public int MinimumAutoHeight
        {
            get => _minimumAutoHeight;
            set
            {
                _minimumAutoHeight = Math.Max(24, value);
                AdjustHeight();
            }
        }

        public AutoHeightThemedRichTextBox()
        {
            ScrollBars = RichTextBoxScrollBars.None;
            BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            QueueAdjustHeight();
        }

        protected override void OnContentsResized(ContentsResizedEventArgs e)
        {
            base.OnContentsResized(e);
            AdjustHeight(e.NewRectangle.Height);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            AdjustHeight();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            AdjustHeight();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (!_adjustingHeight)
                QueueAdjustHeight();
        }

        private void QueueAdjustHeight()
        {
            if (_adjustHeightQueued)
                return;

            if (!IsHandleCreated)
            {
                AdjustHeight();
                return;
            }

            _adjustHeightQueued = true;
            BeginInvoke(new MethodInvoker(() =>
            {
                _adjustHeightQueued = false;
                AdjustHeight();
            }));
        }

        private void AdjustHeight() => AdjustHeight(null);

        private void AdjustHeight(int? contentHeight)
        {
            if (_adjustingHeight)
                return;

            _adjustHeightQueued = false;

            int desiredHeight = Math.Max(MinimumAutoHeight, CalculateDesiredHeight(contentHeight));
            if (Height == desiredHeight)
                return;

            try
            {
                _adjustingHeight = true;
                Height = desiredHeight;
                MinimumSize = new Size(MinimumSize.Width, MinimumAutoHeight);
            }
            finally
            {
                _adjustingHeight = false;
            }
        }

        private int CalculateDesiredHeight(int? contentHeight)
        {
            int measuredHeight = contentHeight ?? EstimateContentHeight();
            return Math.Max(MinimumAutoHeight, measuredHeight + 10);
        }

        private int EstimateContentHeight()
        {
            if (!IsHandleCreated)
            {
                int lineHeight = FontHeight > 0 ? FontHeight : Font.Height;
                int lineCount = Math.Max(1, Lines.Length);
                return Math.Max(lineHeight + 4, lineCount * lineHeight + 4);
            }

            if (TextLength == 0)
                return Math.Max(FontHeight + 4, MinimumAutoHeight);

            int lastCharIndex = Math.Max(0, TextLength - 1);
            Point lastCharPosition = GetPositionFromCharIndex(lastCharIndex);
            int lineHeightWithPadding = Math.Max(FontHeight, Font.Height) + 6;
            return Math.Max(lineHeightWithPadding, lastCharPosition.Y + lineHeightWithPadding);
        }
    }
}
