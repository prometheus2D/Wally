using Wally.Forms.Theme;

namespace Wally.Forms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Switch ToolStrip/MenuStrip text rendering to GDI+ so that the
            // system can fall back through Segoe UI Emoji for emoji glyphs.
            // Must be set before ApplicationConfiguration.Initialize().
            WallyTheme.EnableEmojiRendering();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new WallyForms());
        }
    }
}