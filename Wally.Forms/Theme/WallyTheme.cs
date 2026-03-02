using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Wally.Forms.Theme
{
    /// <summary>
    /// Centralized design tokens for the Wally dark theme.
    /// All panels and controls reference these constants for a unified visual language.
    /// </summary>
    internal static class WallyTheme
    {
        // ?? Surface hierarchy (darkest ? lightest) ??????????????????????????

        /// <summary>Primary background — deepest panels (chat messages, output).</summary>
        public static readonly Color Surface0 = Color.FromArgb(24, 24, 27);

        /// <summary>Secondary background — panel bodies (file tree, input areas).</summary>
        public static readonly Color Surface1 = Color.FromArgb(30, 30, 34);

        /// <summary>Tertiary background — toolbars, input rows, raised surfaces.</summary>
        public static readonly Color Surface2 = Color.FromArgb(39, 39, 44);

        /// <summary>Quaternary — elevated elements (hover states, selected items).</summary>
        public static readonly Color Surface3 = Color.FromArgb(50, 50, 56);

        /// <summary>Highest elevation — active highlights, focused borders.</summary>
        public static readonly Color Surface4 = Color.FromArgb(63, 63, 70);

        // ?? Borders ?????????????????????????????????????????????????????????

        public static readonly Color Border = Color.FromArgb(46, 46, 51);
        public static readonly Color BorderSubtle = Color.FromArgb(38, 38, 43);
        public static readonly Color BorderFocused = Color.FromArgb(0, 122, 204);

        // ?? Text hierarchy ??????????????????????????????????????????????????

        public static readonly Color TextPrimary = Color.FromArgb(228, 228, 233);
        public static readonly Color TextSecondary = Color.FromArgb(161, 161, 170);
        public static readonly Color TextMuted = Color.FromArgb(113, 113, 122);
        public static readonly Color TextDisabled = Color.FromArgb(82, 82, 91);

        // ?? Accent palette ??????????????????????????????????????????????????

        public static readonly Color Accent = Color.FromArgb(59, 130, 246);        // Blue
        public static readonly Color AccentHover = Color.FromArgb(96, 165, 250);
        public static readonly Color AccentMuted = Color.FromArgb(30, 64, 120);
        public static readonly Color AccentSubtle = Color.FromArgb(23, 37, 63);

        public static readonly Color Green = Color.FromArgb(74, 222, 128);
        public static readonly Color GreenMuted = Color.FromArgb(22, 78, 44);

        public static readonly Color Red = Color.FromArgb(248, 113, 113);
        public static readonly Color RedMuted = Color.FromArgb(80, 30, 30);

        public static readonly Color Yellow = Color.FromArgb(250, 204, 21);
        public static readonly Color YellowMuted = Color.FromArgb(80, 65, 15);

        public static readonly Color Purple = Color.FromArgb(168, 85, 247);

        // ?? Status bar ??????????????????????????????????????????????????????

        public static readonly Color StatusBarActive = Color.FromArgb(0, 122, 204);
        public static readonly Color StatusBarInactive = Color.FromArgb(68, 28, 96);

        // ?? Splitter ????????????????????????????????????????????????????????

        public static readonly Color Splitter = Color.FromArgb(46, 46, 51);
        public static readonly Color SplitterHover = Color.FromArgb(0, 122, 204);

        // ?? Fonts ???????????????????????????????????????????????????????????

        public static readonly Font FontUI = new("Segoe UI", 9f);
        public static readonly Font FontUISmall = new("Segoe UI", 8.25f);
        public static readonly Font FontUIBold = new("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font FontUISmallBold = new("Segoe UI", 8.25f, FontStyle.Bold);
        public static readonly Font FontMono = new("Cascadia Mono", 9.5f);
        public static readonly Font FontMonoSmall = new("Cascadia Mono", 9f);
        public static readonly Font FontMonoLarge = new("Cascadia Mono", 10f);
        public static readonly Font FontMonoBold = new("Cascadia Mono", 10f, FontStyle.Bold);

        // ?? Chat bubble sender colors ???????????????????????????????????????

        public static readonly Color SenderUser = Accent;
        public static readonly Color SenderActor = Green;
        public static readonly Color SenderSystem = Red;

        public static readonly Color BubbleUser = Color.FromArgb(23, 37, 63);
        public static readonly Color BubbleActor = Color.FromArgb(39, 39, 44);
        public static readonly Color BubbleError = Color.FromArgb(60, 25, 25);

        // ?? Helpers ?????????????????????????????????????????????????????????

        /// <summary>
        /// Creates a rounded rectangle path for custom-painted controls.
        /// </summary>
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Applies dark theme defaults to any control tree.</summary>
        public static void ApplyTo(Control control)
        {
            control.BackColor = Surface1;
            control.ForeColor = TextPrimary;
            control.Font = FontUI;
        }

        /// <summary>
        /// Configures high-quality rendering on a Graphics instance.
        /// </summary>
        public static void ConfigureGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        /// <summary>
        /// Applies dark theme colors to a ComboBox embedded in a ToolStripComboBox.
        /// WinForms ToolStripComboBox doesn't inherit theme colors automatically.
        /// </summary>
        public static void StyleComboBox(ToolStripComboBox tsCombo)
        {
            var cb = tsCombo.ComboBox;
            cb.BackColor = Surface2;
            cb.ForeColor = TextPrimary;
            cb.FlatStyle = FlatStyle.Flat;
        }
    }

    // ????????????????????????????????????????????????????????????????????????
    //  Dark color table — covers ALL ProfessionalColorTable overrides
    // ????????????????????????????????????????????????????????????????????????

    internal sealed class DarkColorTable : ProfessionalColorTable
    {
        // ?? ToolStrip / MenuStrip backgrounds ???????????????????????????????

        public override Color ToolStripGradientBegin => WallyTheme.Surface2;
        public override Color ToolStripGradientMiddle => WallyTheme.Surface2;
        public override Color ToolStripGradientEnd => WallyTheme.Surface2;
        public override Color MenuStripGradientBegin => WallyTheme.Surface2;
        public override Color MenuStripGradientEnd => WallyTheme.Surface2;
        public override Color ToolStripDropDownBackground => WallyTheme.Surface2;
        public override Color ToolStripContentPanelGradientBegin => WallyTheme.Surface0;
        public override Color ToolStripContentPanelGradientEnd => WallyTheme.Surface0;
        public override Color ToolStripPanelGradientBegin => WallyTheme.Surface2;
        public override Color ToolStripPanelGradientEnd => WallyTheme.Surface2;

        // ?? Menu item states ????????????????????????????????????????????????

        public override Color MenuItemSelected => WallyTheme.Surface3;
        public override Color MenuItemSelectedGradientBegin => WallyTheme.Surface3;
        public override Color MenuItemSelectedGradientEnd => WallyTheme.Surface3;
        public override Color MenuItemPressedGradientBegin => WallyTheme.Surface4;
        public override Color MenuItemPressedGradientMiddle => WallyTheme.Surface4;
        public override Color MenuItemPressedGradientEnd => WallyTheme.Surface4;
        public override Color MenuItemBorder => WallyTheme.Border;
        public override Color MenuBorder => WallyTheme.Border;

        // ?? Image margin ????????????????????????????????????????????????????

        public override Color ImageMarginGradientBegin => WallyTheme.Surface2;
        public override Color ImageMarginGradientMiddle => WallyTheme.Surface2;
        public override Color ImageMarginGradientEnd => WallyTheme.Surface2;
        public override Color ImageMarginRevealedGradientBegin => WallyTheme.Surface2;
        public override Color ImageMarginRevealedGradientMiddle => WallyTheme.Surface2;
        public override Color ImageMarginRevealedGradientEnd => WallyTheme.Surface2;

        // ?? Separators ??????????????????????????????????????????????????????

        public override Color SeparatorDark => WallyTheme.Border;
        public override Color SeparatorLight => WallyTheme.Border;

        // ?? Borders ?????????????????????????????????????????????????????????

        public override Color ToolStripBorder => WallyTheme.Border;

        // ?? Button states (toolbar buttons, checked items) ??????????????????

        public override Color ButtonSelectedBorder => WallyTheme.BorderFocused;
        public override Color ButtonSelectedHighlight => WallyTheme.Surface3;
        public override Color ButtonSelectedHighlightBorder => WallyTheme.BorderFocused;
        public override Color ButtonSelectedGradientBegin => WallyTheme.Surface3;
        public override Color ButtonSelectedGradientMiddle => WallyTheme.Surface3;
        public override Color ButtonSelectedGradientEnd => WallyTheme.Surface3;
        public override Color ButtonPressedBorder => WallyTheme.BorderFocused;
        public override Color ButtonPressedHighlight => WallyTheme.Surface4;
        public override Color ButtonPressedHighlightBorder => WallyTheme.BorderFocused;
        public override Color ButtonPressedGradientBegin => WallyTheme.Surface4;
        public override Color ButtonPressedGradientMiddle => WallyTheme.Surface4;
        public override Color ButtonPressedGradientEnd => WallyTheme.Surface4;
        public override Color ButtonCheckedHighlight => WallyTheme.Surface3;
        public override Color ButtonCheckedHighlightBorder => WallyTheme.BorderFocused;
        public override Color ButtonCheckedGradientBegin => WallyTheme.Surface3;
        public override Color ButtonCheckedGradientMiddle => WallyTheme.Surface3;
        public override Color ButtonCheckedGradientEnd => WallyTheme.Surface3;

        // ?? Check states ????????????????????????????????????????????????????

        public override Color CheckBackground => WallyTheme.AccentMuted;
        public override Color CheckPressedBackground => WallyTheme.Accent;
        public override Color CheckSelectedBackground => WallyTheme.AccentMuted;

        // ?? Overflow / grip ?????????????????????????????????????????????????

        public override Color OverflowButtonGradientBegin => WallyTheme.Surface3;
        public override Color OverflowButtonGradientMiddle => WallyTheme.Surface3;
        public override Color OverflowButtonGradientEnd => WallyTheme.Surface3;
        public override Color GripDark => WallyTheme.Border;
        public override Color GripLight => WallyTheme.Surface4;

        // ?? Status strip ????????????????????????????????????????????????????

        public override Color StatusStripGradientBegin => WallyTheme.StatusBarActive;
        public override Color StatusStripGradientEnd => WallyTheme.StatusBarActive;

        // ?? Raft / float ????????????????????????????????????????????????????

        public override Color RaftingContainerGradientBegin => WallyTheme.Surface1;
        public override Color RaftingContainerGradientEnd => WallyTheme.Surface1;
    }

    // ????????????????????????????????????????????????????????????????????????
    //  Themed splitter with hover highlight
    // ????????????????????????????????????????????????????????????????????????

    internal sealed class ThemedSplitter : Splitter
    {
        protected override void OnMouseEnter(EventArgs e)
        {
            BackColor = WallyTheme.SplitterHover;
            Cursor = Cursors.SizeAll;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            BackColor = WallyTheme.Splitter;
            base.OnMouseLeave(e);
        }
    }
}
