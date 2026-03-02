using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// Centre panel that fills the main content area.
    /// When no workspace is loaded, shows a branded landing page with
    /// guidance on how to open or set up a workspace.
    /// When a workspace is loaded, shows workspace summary info.
    /// </summary>
    public sealed class WelcomePanel : UserControl
    {
        private readonly Label _lblIcon;
        private readonly Label _lblTitle;
        private readonly Label _lblSubtitle;
        private readonly Label _lblActions;
        private readonly Panel _card;
        private readonly Panel _cardBorder;

        private bool _workspaceLoaded;

        public WelcomePanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            // ?? Central card (rounded appearance via border panel) ??

            _lblIcon = new Label
            {
                Text = "\U0001F9E0",
                Dock = DockStyle.Top,
                Height = 64,
                Font = new Font("Segoe UI Emoji", 32f),
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.BottomCenter
            };

            _lblTitle = new Label
            {
                Text = "Wally",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblSubtitle = new Label
            {
                Text = "AI Actor Environment",
                Dock = DockStyle.Top,
                Height = 24,
                Font = WallyTheme.FontUI,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter
            };

            _lblActions = new Label
            {
                Text =
                    "\n\nGet Started\n\n" +
                    "  \u2022  File \u2192 Open Workspace\u2026          Ctrl+O\n" +
                    "  \u2022  File \u2192 Setup New Workspace\u2026  Ctrl+Shift+N\n\n" +
                    "Or type  setup <path>  or  load <path>  in the terminal below.",
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontUI,
                ForeColor = WallyTheme.TextSecondary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(20, 0, 20, 0)
            };

            _card = new Panel
            {
                Size = new Size(460, 340),
                BackColor = WallyTheme.Surface1,
                Padding = new Padding(24, 20, 24, 20)
            };
            _card.Controls.Add(_lblActions);
            _card.Controls.Add(_lblSubtitle);
            _card.Controls.Add(_lblTitle);
            _card.Controls.Add(_lblIcon);

            _cardBorder = new Panel
            {
                Size = new Size(462, 342),
                BackColor = WallyTheme.Border,
                Padding = new Padding(1)
            };
            _cardBorder.Controls.Add(_card);
            _card.Dock = DockStyle.Fill;

            Controls.Add(_cardBorder);

            // Center the card when the panel resizes.
            Resize += (_, _) => CenterCard();

            ResumeLayout(true);
        }

        private void CenterCard()
        {
            _cardBorder.Location = new Point(
                Math.Max(0, (ClientSize.Width - _cardBorder.Width) / 2),
                Math.Max(0, (ClientSize.Height - _cardBorder.Height) / 2));
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            CenterCard();
        }

        /// <summary>
        /// Updates the welcome panel content based on workspace state.
        /// </summary>
        public void SetWorkspaceInfo(bool loaded, string? workSource = null, int actorCount = 0, string? defaultModel = null)
        {
            _workspaceLoaded = loaded;

            if (!loaded)
            {
                _lblIcon.Text = "\U0001F9E0";
                _lblTitle.Text = "Wally";
                _lblSubtitle.Text = "AI Actor Environment";
                _lblActions.Text =
                    "\n\nGet Started\n\n" +
                    "  \u2022  File \u2192 Open Workspace\u2026          Ctrl+O\n" +
                    "  \u2022  File \u2192 Setup New Workspace\u2026  Ctrl+Shift+N\n\n" +
                    "Or type  setup <path>  or  load <path>  in the terminal below.";
                _lblActions.ForeColor = WallyTheme.TextSecondary;
            }
            else
            {
                _lblIcon.Text = "\u2705";
                _lblTitle.Text = System.IO.Path.GetFileName(workSource) ?? "Workspace";
                _lblSubtitle.Text = workSource ?? "";
                _lblActions.Text =
                    $"\n\nWorkspace Loaded\n\n" +
                    $"  Actors:  {actorCount}\n" +
                    $"  Model:   {(string.IsNullOrEmpty(defaultModel) ? "(default)" : defaultModel)}\n\n" +
                    "Use the AI Chat panel on the right to talk to actors,\n" +
                    "or the terminal below to run commands.";
                _lblActions.ForeColor = WallyTheme.TextSecondary;
            }
        }
    }
}
