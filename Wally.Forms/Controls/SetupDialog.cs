using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// Small modal dialog shown when the user clicks Setup.
    /// Offers two options:
    /// <list type="bullet">
    ///   <item>Use the current (exe) directory as the WorkSource.</item>
    ///   <item>Browse for a different folder.</item>
    /// </list>
    /// Sets <see cref="SelectedPath"/> to the chosen directory, or
    /// <see langword="null"/> when the user cancels.
    /// </summary>
    internal sealed class SetupDialog : Form
    {
        /// <summary>
        /// The WorkSource path chosen by the user, or <see langword="null"/>
        /// when the dialog was cancelled.
        /// </summary>
        public string? SelectedPath { get; private set; }

        private readonly string _currentDir;
        private readonly Label _lblIcon;
        private readonly Label _lblTitle;
        private readonly Label _lblDescription;
        private readonly Panel _currentDirPanel;
        private readonly Label _lblCurrentPath;
        private readonly Button _btnUseCurrent;
        private readonly Button _btnBrowse;
        private readonly Button _btnCancel;

        public SetupDialog()
        {
            _currentDir = WallyHelper.GetExeDirectory();

            SuspendLayout();

            // ?? Form chrome ??
            Text = "Setup Workspace";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(480, 300);
            BackColor = WallyTheme.Surface1;
            ForeColor = WallyTheme.TextPrimary;
            Font = WallyTheme.FontUI;

            // ?? Icon ??
            _lblIcon = new Label
            {
                Text = "\u2728",
                Location = new Point(24, 20),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI Emoji", 22f),
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ?? Title ??
            _lblTitle = new Label
            {
                Text = "Setup New Workspace",
                Location = new Point(68, 20),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ?? Description ??
            _lblDescription = new Label
            {
                Text = "A .wally/ workspace folder will be created inside the\n" +
                       "selected directory with default actors and configuration.",
                Location = new Point(24, 58),
                Size = new Size(432, 36),
                Font = WallyTheme.FontUI,
                ForeColor = WallyTheme.TextSecondary,
                BackColor = Color.Transparent
            };

            // ?? Current directory panel ??
            _lblCurrentPath = new Label
            {
                Text = TruncatePath(_currentDir, 56),
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontMonoSmall,
                ForeColor = WallyTheme.TextSecondary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0)
            };
            _lblCurrentPath.MouseEnter += (_, _) => { ShowToolTip(_lblCurrentPath, _currentDir); };

            _currentDirPanel = new Panel
            {
                Location = new Point(24, 104),
                Size = new Size(432, 32),
                BackColor = WallyTheme.Surface2
            };
            _currentDirPanel.Controls.Add(_lblCurrentPath);

            // ?? Use Current button ??
            _btnUseCurrent = CreateButton(
                "\uD83D\uDCC1  Use Current Directory",
                new Point(24, 148), new Size(432, 44),
                WallyTheme.Accent);
            _btnUseCurrent.Click += OnUseCurrent;

            // ?? Browse button ??
            _btnBrowse = CreateButton(
                "\uD83D\uDCC2  Choose Different Folder\u2026",
                new Point(24, 200), new Size(432, 44),
                WallyTheme.Surface3);
            _btnBrowse.ForeColor = WallyTheme.TextPrimary;
            _btnBrowse.Click += OnBrowse;

            // ?? Cancel button ??
            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(356, 260),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextSecondary,
                Font = WallyTheme.FontUI,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderColor = WallyTheme.Border;
            _btnCancel.FlatAppearance.BorderSize = 1;
            _btnCancel.FlatAppearance.MouseOverBackColor = WallyTheme.Surface3;

            CancelButton = _btnCancel;

            // ?? Assembly ??
            Controls.Add(_lblIcon);
            Controls.Add(_lblTitle);
            Controls.Add(_lblDescription);
            Controls.Add(_currentDirPanel);
            Controls.Add(_btnUseCurrent);
            Controls.Add(_btnBrowse);
            Controls.Add(_btnCancel);

            ResumeLayout(true);
        }

        // ?? Event handlers ??????????????????????????????????????????????????

        private void OnUseCurrent(object? sender, EventArgs e)
        {
            SelectedPath = _currentDir;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select your codebase root. A .wally/ workspace will be created inside it.",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true,
                InitialDirectory = _currentDir
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                SelectedPath = dlg.SelectedPath;
                DialogResult = DialogResult.OK;
                Close();
            }
            // If folder dialog was cancelled, stay in this dialog.
        }

        // ?? Helpers ?????????????????????????????????????????????????????????

        private static Button CreateButton(string text, Point location, Size size, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = WallyTheme.FontUIBold,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(
                    Math.Min(255, backColor.R + 20),
                    Math.Min(255, backColor.G + 20),
                    Math.Min(255, backColor.B + 20));
            return btn;
        }

        private static string TruncatePath(string path, int maxLen)
        {
            if (path.Length <= maxLen) return path;
            // Show drive + "..." + tail.
            int tail = maxLen - 6;
            return path[..3] + "\u2026" + path[^tail..];
        }

        private readonly ToolTip _toolTip = new();

        private void ShowToolTip(Control control, string text)
        {
            _toolTip.SetToolTip(control, text);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
