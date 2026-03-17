using System.Drawing;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// A scrollable surface for editor panels.
    /// Wraps a standard auto-scroll <see cref="Panel"/> — the native
    /// WinForms scrollbar is reliable, accessible, and zero-maintenance.
    /// </summary>
    internal class ThemedScrollPanel : Panel
    {
        public ThemedScrollPanel()
        {
            AutoScroll = true;
            BackColor = WallyTheme.Surface0;
        }
    }
}
