using System.Drawing;
using System.Windows.Forms;
using ScintillaNET;
using Wally.Forms.Theme;

// Alias to avoid ambiguity with ScintillaNET.BorderStyle
using WinBorderStyle = System.Windows.Forms.BorderStyle;

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
                BorderStyle = WinBorderStyle.FixedSingle,
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
                BorderStyle = WinBorderStyle.None,
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
                BorderStyle = WinBorderStyle.None,
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
                BorderStyle = WinBorderStyle.None,
                IntegralHeight = false
            };
        }

        // ?? Professional code editor ???????????????????????????????????????

        /// <summary>
        /// Creates a themed, language-aware <see cref="Scintilla"/> editor control.
        /// </summary>
        /// <param name="languageId">
        /// One of <c>"json"</c>, <c>"markdown"</c>, <c>"wally-runbook"</c>, or
        /// <c>"text"</c> (plain-text, no highlighting).
        /// </param>
        /// <param name="readOnly">
        /// When <see langword="true"/> the editor is read-only.
        /// </param>
        public static Scintilla CreateCodeEditor(string languageId, bool readOnly = false)
        {
            var editor = new Scintilla
            {
                Dock        = DockStyle.Fill,
                ReadOnly    = readOnly,
                WrapMode    = WrapMode.None,
                IndentWidth = 4,
                TabWidth    = 4,
                UseTabs     = false,
                EdgeMode    = EdgeMode.None,
                ScrollWidth = 1,
                ScrollWidthTracking = true,
                ViewEol = false
            };

            ApplyWallyTheme(editor);

            switch (languageId.ToLowerInvariant())
            {
                case "json":           ConfigureJson(editor);           break;
                case "markdown":       ConfigureMarkdown(editor);       break;
                case "wally-runbook":  ConfigureWallyRunbook(editor);   break;
                default:               ConfigurePlainText(editor);      break;
            }

            return editor;
        }

        /// <summary>Creates a themed JSON editor.</summary>
        public static Scintilla CreateJsonEditor(bool readOnly = false)
            => CreateCodeEditor("json", readOnly);

        /// <summary>Creates a themed Markdown editor.</summary>
        public static Scintilla CreateMarkdownEditor(bool readOnly = false)
            => CreateCodeEditor("markdown", readOnly);

        /// <summary>Creates a themed WallyScript / runbook editor.</summary>
        public static Scintilla CreateRunbookEditor(bool readOnly = false)
            => CreateCodeEditor("wally-runbook", readOnly);

        /// <summary>Creates a themed plain-text editor with no syntax highlighting.</summary>
        public static Scintilla CreatePlainTextEditor(bool readOnly = false)
            => CreateCodeEditor("text", readOnly);

        // ?? Theme mapping ?????????????????????????????????????????????????

        private static void ApplyWallyTheme(Scintilla editor)
        {
            // Control-level background — prevents WinForms default from leaking
            editor.BackColor = WallyTheme.Surface1;

            editor.StyleResetDefault();
            editor.Styles[Style.Default].Font      = "Cascadia Mono";
            editor.Styles[Style.Default].Size      = 10;
            editor.Styles[Style.Default].BackColor = WallyTheme.Surface1;
            editor.Styles[Style.Default].ForeColor = WallyTheme.TextPrimary;
            editor.StyleClearAll();   // propagates Default's Back/ForeColor to all styles

            editor.SetSelectionBackColor(true, WallyTheme.Surface4);
            editor.SetSelectionForeColor(false, WallyTheme.TextPrimary);
            editor.CaretForeColor     = WallyTheme.TextPrimary;
            editor.CaretLineBackColor = WallyTheme.Surface2;
            editor.CaretLineVisible   = true;

            // Whitespace rendering (visible or not, colours must be correct)
            editor.SetWhitespaceBackColor(true, WallyTheme.Surface1);
            editor.SetWhitespaceForeColor(true, WallyTheme.Border);

            // Margin 0 — line numbers
            editor.Margins[0].Width   = 40;
            editor.Margins[0].Type    = MarginType.Number;
            editor.Styles[Style.LineNumber].BackColor = WallyTheme.Surface1;
            editor.Styles[Style.LineNumber].ForeColor = WallyTheme.TextMuted;

            // Margin 1 — fold markers
            editor.Margins[1].Width     = 16;
            editor.Margins[1].Type      = MarginType.Symbol;
            editor.Margins[1].Mask      = Marker.MaskFolders;
            editor.Margins[1].Sensitive = true;

            // Fold margin background — fills the entire fold-margin gutter area
            editor.SetFoldMarginColor(true, WallyTheme.Surface1);
            editor.SetFoldMarginHighlightColor(true, WallyTheme.Surface1);

            int[] foldMarkers =
            {
                Marker.Folder, Marker.FolderOpen, Marker.FolderEnd,
                Marker.FolderMidTail, Marker.FolderOpenMid,
                Marker.FolderSub, Marker.FolderTail
            };
            foreach (int m in foldMarkers)
            {
                editor.Markers[m].SetForeColor(WallyTheme.Surface1);
                editor.Markers[m].SetBackColor(WallyTheme.TextMuted);
            }

            editor.AutomaticFold    = AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change;
            editor.IndentationGuides = IndentView.LookBoth;
            editor.Styles[Style.IndentGuide].ForeColor = WallyTheme.Border;
            editor.Styles[Style.IndentGuide].BackColor = WallyTheme.Surface1;

            // Brace-match highlight styles
            editor.Styles[Style.BraceLight].BackColor = WallyTheme.Surface3;
            editor.Styles[Style.BraceLight].ForeColor = WallyTheme.TextPrimary;
            editor.Styles[Style.BraceBad].BackColor   = WallyTheme.RedMuted;
            editor.Styles[Style.BraceBad].ForeColor   = WallyTheme.Red;

            // Call-tip styles
            editor.Styles[Style.CallTip].BackColor = WallyTheme.Surface2;
            editor.Styles[Style.CallTip].ForeColor = WallyTheme.TextPrimary;
        }

        // ?? Language configurations ????????????????????????????????????????

        private static void ConfigurePlainText(Scintilla editor)
        {
            editor.LexerName = "null";
        }

        private static void ConfigureJson(Scintilla editor)
        {
            editor.LexerName = "json";

            const int DEFAULT      = 0;
            const int NUMBER       = 1;
            const int STRING       = 2;
            const int UNCLOSED     = 3;
            const int PROPERTY     = 4;
            const int ESCAPE       = 5;
            const int LINECOMMENT  = 6;
            const int BLOCKCOMMENT = 7;
            const int OPERATOR     = 8;
            const int URI          = 9;
            const int COMPACTIRI   = 10;
            const int KEYWORD      = 11;
            const int LDKEYWORD    = 12;
            const int ERROR        = 13;

            Color bg = WallyTheme.Surface1;

            editor.Styles[DEFAULT].ForeColor       = WallyTheme.TextPrimary;
            editor.Styles[DEFAULT].BackColor       = bg;
            editor.Styles[NUMBER].ForeColor        = Color.FromArgb(181, 206, 168);
            editor.Styles[NUMBER].BackColor        = bg;
            editor.Styles[STRING].ForeColor        = Color.FromArgb(206, 145, 120);
            editor.Styles[STRING].BackColor        = bg;
            editor.Styles[UNCLOSED].ForeColor      = WallyTheme.Red;
            editor.Styles[UNCLOSED].BackColor      = bg;
            editor.Styles[PROPERTY].ForeColor      = Color.FromArgb(156, 220, 254);
            editor.Styles[PROPERTY].BackColor      = bg;
            editor.Styles[ESCAPE].ForeColor        = Color.FromArgb(215, 186, 125);
            editor.Styles[ESCAPE].BackColor        = bg;
            editor.Styles[LINECOMMENT].ForeColor   = WallyTheme.TextMuted;
            editor.Styles[LINECOMMENT].BackColor   = bg;
            editor.Styles[LINECOMMENT].Italic      = true;
            editor.Styles[BLOCKCOMMENT].ForeColor  = WallyTheme.TextMuted;
            editor.Styles[BLOCKCOMMENT].BackColor  = bg;
            editor.Styles[BLOCKCOMMENT].Italic     = true;
            editor.Styles[OPERATOR].ForeColor      = WallyTheme.TextSecondary;
            editor.Styles[OPERATOR].BackColor      = bg;
            editor.Styles[URI].ForeColor           = Color.FromArgb(78, 201, 176);
            editor.Styles[URI].BackColor           = bg;
            editor.Styles[COMPACTIRI].ForeColor    = Color.FromArgb(78, 201, 176);
            editor.Styles[COMPACTIRI].BackColor    = bg;
            editor.Styles[KEYWORD].ForeColor       = Color.FromArgb(86, 156, 214);
            editor.Styles[KEYWORD].BackColor       = bg;
            editor.Styles[LDKEYWORD].ForeColor     = Color.FromArgb(86, 156, 214);
            editor.Styles[LDKEYWORD].BackColor     = bg;
            editor.Styles[ERROR].ForeColor         = WallyTheme.Red;
            editor.Styles[ERROR].BackColor         = bg;
            editor.Styles[ERROR].Bold              = true;

            editor.SetProperty("fold", "1");
            editor.SetProperty("fold.compact", "0");
        }

        private static void ConfigureMarkdown(Scintilla editor)
        {
            editor.LexerName = "markdown";

            const int DEFAULT    = 0;
            const int LINE_BEGIN = 1;
            const int STRONG1    = 2;
            const int STRONG2    = 3;
            const int EM1        = 4;
            const int EM2        = 5;
            const int HEADER1    = 6;
            const int HEADER2    = 7;
            const int HEADER3    = 8;
            const int PRECHAR    = 9;
            const int ULIST_ITEM = 10;
            const int OLIST_ITEM = 11;
            const int BLOCKQUOTE = 12;
            const int STRIKEOUT  = 13;
            const int HRULE      = 14;
            const int LINK       = 15;
            const int CODE       = 16;
            const int CODE2      = 17;
            const int CODEBLOCK  = 18;

            Color bg = WallyTheme.Surface1;
            Color codeBg = WallyTheme.Surface2;

            editor.Styles[DEFAULT].ForeColor     = WallyTheme.TextPrimary;
            editor.Styles[DEFAULT].BackColor     = bg;
            editor.Styles[LINE_BEGIN].ForeColor  = WallyTheme.TextMuted;
            editor.Styles[LINE_BEGIN].BackColor  = bg;
            editor.Styles[STRONG1].Bold          = true;
            editor.Styles[STRONG1].ForeColor     = WallyTheme.TextPrimary;
            editor.Styles[STRONG1].BackColor     = bg;
            editor.Styles[STRONG2].Bold          = true;
            editor.Styles[STRONG2].ForeColor     = WallyTheme.TextPrimary;
            editor.Styles[STRONG2].BackColor     = bg;
            editor.Styles[EM1].Italic            = true;
            editor.Styles[EM1].ForeColor         = WallyTheme.TextSecondary;
            editor.Styles[EM1].BackColor         = bg;
            editor.Styles[EM2].Italic            = true;
            editor.Styles[EM2].ForeColor         = WallyTheme.TextSecondary;
            editor.Styles[EM2].BackColor         = bg;

            Color headerColor = Color.FromArgb(86, 156, 214);
            editor.Styles[HEADER1].ForeColor     = headerColor;
            editor.Styles[HEADER1].BackColor     = bg;
            editor.Styles[HEADER1].Bold          = true;
            editor.Styles[HEADER1].Size          = 12;
            editor.Styles[HEADER2].ForeColor     = headerColor;
            editor.Styles[HEADER2].BackColor     = bg;
            editor.Styles[HEADER2].Bold          = true;
            editor.Styles[HEADER2].Size          = 11;
            editor.Styles[HEADER3].ForeColor     = headerColor;
            editor.Styles[HEADER3].BackColor     = bg;
            editor.Styles[HEADER3].Bold          = true;

            editor.Styles[PRECHAR].ForeColor     = WallyTheme.TextMuted;
            editor.Styles[PRECHAR].BackColor     = bg;
            editor.Styles[ULIST_ITEM].ForeColor  = WallyTheme.TextSecondary;
            editor.Styles[ULIST_ITEM].BackColor  = bg;
            editor.Styles[OLIST_ITEM].ForeColor  = WallyTheme.TextSecondary;
            editor.Styles[OLIST_ITEM].BackColor  = bg;
            editor.Styles[BLOCKQUOTE].ForeColor  = WallyTheme.TextMuted;
            editor.Styles[BLOCKQUOTE].BackColor  = bg;
            editor.Styles[BLOCKQUOTE].Italic     = true;
            editor.Styles[STRIKEOUT].ForeColor   = WallyTheme.TextDisabled;
            editor.Styles[STRIKEOUT].BackColor   = bg;
            editor.Styles[HRULE].ForeColor       = WallyTheme.Border;
            editor.Styles[HRULE].BackColor       = bg;
            editor.Styles[LINK].ForeColor        = Color.FromArgb(78, 201, 176);
            editor.Styles[LINK].BackColor        = bg;
            editor.Styles[LINK].Underline        = true;

            Color codeColor = Color.FromArgb(206, 145, 120);
            editor.Styles[CODE].ForeColor        = codeColor;
            editor.Styles[CODE].BackColor        = codeBg;
            editor.Styles[CODE2].ForeColor       = codeColor;
            editor.Styles[CODE2].BackColor       = codeBg;
            editor.Styles[CODEBLOCK].ForeColor   = codeColor;
            editor.Styles[CODEBLOCK].BackColor   = codeBg;
            editor.Styles[CODEBLOCK].FillLine    = true;

            editor.WrapMode = WrapMode.Word;
        }

        private static void ConfigureWallyRunbook(Scintilla editor)
        {
            // Batch lexer — closest built-in to a command-oriented language.
            // Gives us comment colouring (#), keyword highlighting, and $variable
            // support out of the box.
            editor.LexerName = "batch";

            const int DEFAULT     = 0;
            const int COMMENT     = 1;
            const int WORD        = 2;   // keywords from set 0
            const int LABEL       = 3;
            const int HIDE        = 4;
            const int COMMAND     = 5;
            const int IDENTIFIER  = 6;
            const int OPERATOR    = 7;

            Color bg = WallyTheme.Surface1;

            editor.Styles[DEFAULT].ForeColor    = WallyTheme.TextPrimary;
            editor.Styles[DEFAULT].BackColor    = bg;
            editor.Styles[COMMENT].ForeColor    = WallyTheme.TextMuted;
            editor.Styles[COMMENT].BackColor    = bg;
            editor.Styles[COMMENT].Italic       = true;
            editor.Styles[WORD].ForeColor       = Color.FromArgb(86, 156, 214);
            editor.Styles[WORD].BackColor       = bg;
            editor.Styles[WORD].Bold            = true;
            editor.Styles[LABEL].ForeColor      = Color.FromArgb(197, 134, 192);
            editor.Styles[LABEL].BackColor      = bg;
            editor.Styles[HIDE].ForeColor       = WallyTheme.TextMuted;
            editor.Styles[HIDE].BackColor       = bg;
            editor.Styles[COMMAND].ForeColor    = Color.FromArgb(78, 201, 176);
            editor.Styles[COMMAND].BackColor    = bg;
            editor.Styles[IDENTIFIER].ForeColor = WallyTheme.TextPrimary;
            editor.Styles[IDENTIFIER].BackColor = bg;
            editor.Styles[OPERATOR].ForeColor   = WallyTheme.TextSecondary;
            editor.Styles[OPERATOR].BackColor   = bg;

            // Wally command verbs
            editor.SetKeywords(0,
                "run runbook setup repair load save info list list-loops list-wrappers list-runbooks " +
                "reload-actors cleanup clear-history commands help tutorial add-actor edit-actor delete-actor " +
                "add-loop edit-loop delete-loop add-wrapper edit-wrapper delete-wrapper " +
                "add-runbook edit-runbook delete-runbook " +
                "if else while foreach function parallel pipeline stage try catch finally retry " +
                "log send-message wait-for-reply");

            editor.SetProperty("fold", "1");
            editor.SetProperty("fold.compact", "0");
        }
    }
}
