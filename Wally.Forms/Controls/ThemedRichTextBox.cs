using System.Windows.Forms;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// A thin <see cref="RichTextBox"/> subclass that standardises construction
    /// defaults across the application.  All scrolling is handled by the native
    /// Windows scrollbar — no custom painting is applied.
    /// </summary>
    internal class ThemedRichTextBox : RichTextBox
    {
        /// <summary>
        /// When <see langword="true"/> the native horizontal scrollbar is shown
        /// (sets <see cref="RichTextBox.ScrollBars"/> to <c>Both</c>).
        /// When <see langword="false"/> only the vertical bar is shown.
        /// Must be set before the control handle is created (i.e. before the
        /// control is added to a visible form).
        /// </summary>
        public bool ShowHorizontalScrollbar
        {
            get => ScrollBars == RichTextBoxScrollBars.Both;
            set => ScrollBars = value
                ? RichTextBoxScrollBars.Both
                : RichTextBoxScrollBars.Vertical;
        }

        /// <summary>
        /// No-op property retained for source compatibility with existing call
        /// sites that set it.  The native scrollbar is always visible when
        /// there is scrollable content.
        /// </summary>
        public bool AlwaysShowScrollbar { get; set; }

        public ThemedRichTextBox()
        {
            BorderStyle = BorderStyle.None;
            ScrollBars  = RichTextBoxScrollBars.Vertical;
        }
    }
}
