using System.Drawing;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    internal static class ThemedEditorFactory
    {
        public static ThemedScrollPanel CreateScrollableSurface()
        {
            return new ThemedScrollPanel
            {
                Dock = DockStyle.Fill,
                BackColor = WallyTheme.Surface0
            };
        }

        public static TableLayoutPanel CreateScrollableFormTable(int columnCount, Padding? padding = null)
        {
            return new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                ColumnCount = columnCount,
                BackColor = WallyTheme.Surface0,
                Padding = padding ?? new Padding(20)
            };
        }

        public static AutoHeightThemedRichTextBox CreateFormTextArea(
            int minimumHeight,
            bool wordWrap = true,
            bool readOnly = false,
            Color? backColor = null,
            Padding? margin = null,
            bool detectUrls = false)
        {
            return new AutoHeightThemedRichTextBox
            {
                Dock = DockStyle.Top,
                Height = minimumHeight,
                MinimumAutoHeight = minimumHeight,
                Font = WallyTheme.FontMono,
                BackColor = backColor ?? WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap = wordWrap,
                ReadOnly = readOnly,
                ScrollBars = RichTextBoxScrollBars.None,
                DetectUrls = detectUrls,
                Margin = margin ?? new Padding(0, 0, 0, 4)
            };
        }

        public static ThemedRichTextBox CreateDocumentViewer(
            bool wordWrap,
            bool readOnly = true,
            Color? backColor = null,
            DockStyle dock = DockStyle.Fill)
        {
            return new ThemedRichTextBox
            {
                Dock = dock,
                Font = WallyTheme.FontMono,
                BackColor = backColor ?? WallyTheme.Surface0,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                ReadOnly = readOnly,
                WordWrap = wordWrap,
                ShowHorizontalScrollbar = !wordWrap,
                DetectUrls = false
            };
        }

        public static ThemedRichTextBox CreateInputTextArea(
            bool wordWrap = true,
            bool acceptsTab = false,
            Color? backColor = null)
        {
            return new ThemedRichTextBox
            {
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontUI,
                BackColor = backColor ?? WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                AcceptsTab = acceptsTab,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.None,
                WordWrap = wordWrap
            };
        }

        public static ThemedListBox CreateListViewer(int width, Font? font = null, Color? backColor = null)
        {
            return new ThemedListBox
            {
                Dock = DockStyle.Left,
                Width = width,
                Font = font ?? WallyTheme.FontMono,
                BackColor = backColor ?? WallyTheme.Surface1,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };
        }
    }
}
