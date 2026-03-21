using System.Windows.Forms;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// A thin <see cref="ListBox"/> subclass that standardises construction
    /// defaults across the application.  All scrolling is handled by the
    /// native Windows scrollbar — no custom painting is applied.
    /// </summary>
    internal class ThemedListBox : ListBox
    {
        public ThemedListBox()
        {
            BorderStyle = BorderStyle.None;
        }
    }
}
