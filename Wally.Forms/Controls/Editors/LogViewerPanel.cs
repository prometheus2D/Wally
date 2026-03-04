using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Logging;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Read-only viewer for session log files. Lists log sessions and
    /// displays selected log entries in a themed output area.
    /// </summary>
    public sealed class LogViewerPanel : UserControl
    {
        private readonly ListBox _lstSessions;
        private readonly RichTextBox _txtLogContent;
        private readonly Button _btnRefresh;
        private readonly Label _lblInfo;

        private WallyEnvironment? _environment;

        public LogViewerPanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            // ?? Header ??
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = WallyTheme.Surface0,
                Padding = new Padding(20, 12, 20, 4)
            };

            var lblTitle = new Label
            {
                Text = "\uD83D\uDCCB Log Viewer",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = WallyTheme.FontUIBold,
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent
            };

            _btnRefresh = new Button
            {
                Text = "\u21BB Refresh",
                Dock = DockStyle.Right,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3,
                ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUISmallBold,
                Cursor = Cursors.Hand,
                Padding = new Padding(8, 2, 8, 2)
            };
            _btnRefresh.FlatAppearance.BorderSize = 1;
            _btnRefresh.FlatAppearance.BorderColor = WallyTheme.Border;
            _btnRefresh.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            _btnRefresh.Click += (_, _) => RefreshSessions();

            _lblInfo = new Label
            {
                Text = "",
                Dock = DockStyle.Bottom,
                Height = 20,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(_btnRefresh);
            headerPanel.Controls.Add(_lblInfo);

            // ?? Session list (left) ??
            _lstSessions = new ListBox
            {
                Dock = DockStyle.Left,
                Width = 260,
                Font = WallyTheme.FontMono,
                BackColor = WallyTheme.Surface1,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };
            _lstSessions.SelectedIndexChanged += OnSessionSelected;

            var splitter = new Splitter
            {
                Dock = DockStyle.Left,
                Width = 3,
                BackColor = WallyTheme.Border,
                MinSize = 180
            };

            // ?? Log content (fill) ??
            _txtLogContent = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontMono,
                BackColor = WallyTheme.Surface0,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                DetectUrls = false
            };

            Controls.Add(_txtLogContent);
            Controls.Add(splitter);
            Controls.Add(_lstSessions);
            Controls.Add(headerPanel);

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
        }

        public void RefreshSessions()
        {
            _lstSessions.Items.Clear();
            _txtLogContent.Clear();

            if (_environment?.HasWorkspace != true)
            {
                _lblInfo.Text = "No workspace loaded.";
                return;
            }

            string logsFolder = Path.Combine(
                _environment.WorkspaceFolder!,
                _environment.Workspace!.Config.LogsFolderName);

            if (!Directory.Exists(logsFolder))
            {
                _lblInfo.Text = "Logs folder not found.";
                return;
            }

            // Each session is a subdirectory of the Logs folder
            var sessionDirs = Directory.GetDirectories(logsFolder)
                .OrderByDescending(d => d)
                .ToArray();

            if (sessionDirs.Length == 0)
            {
                // Also look for loose .jsonl files
                var looseFiles = Directory.GetFiles(logsFolder, "*.jsonl")
                    .OrderByDescending(f => f)
                    .ToArray();

                foreach (string file in looseFiles)
                    _lstSessions.Items.Add(new LogItem(Path.GetFileName(file), file, isFile: true));

                _lblInfo.Text = $"{looseFiles.Length} log file(s) found.";
            }
            else
            {
                foreach (string dir in sessionDirs)
                    _lstSessions.Items.Add(new LogItem(Path.GetFileName(dir), dir, isFile: false));

                _lblInfo.Text = $"{sessionDirs.Length} session(s) found.";
            }

            if (_lstSessions.Items.Count > 0)
                _lstSessions.SelectedIndex = 0;
        }

        // ?? Event handlers ??????????????????????????????????????????????????

        private void OnSessionSelected(object? sender, EventArgs e)
        {
            if (_lstSessions.SelectedItem is not LogItem item) return;

            _txtLogContent.Clear();

            try
            {
                string[] files;
                if (item.IsFile)
                {
                    files = new[] { item.Path };
                }
                else
                {
                    files = Directory.GetFiles(item.Path, "*.jsonl")
                        .OrderBy(f => f)
                        .ToArray();
                }

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    AppendLine($"?? {fileName} ??", WallyTheme.TextMuted);

                    foreach (string line in File.ReadAllLines(file))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Color-code based on content
                        Color color = WallyTheme.TextPrimary;
                        if (line.Contains("\"Level\":\"Error\"", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("\"level\":\"error\"", StringComparison.OrdinalIgnoreCase))
                            color = WallyTheme.Red;
                        else if (line.Contains("\"Level\":\"Warning\"", StringComparison.OrdinalIgnoreCase) ||
                                 line.Contains("\"level\":\"warning\"", StringComparison.OrdinalIgnoreCase))
                            color = WallyTheme.Yellow;
                        else if (line.Contains("\"Level\":\"Info\"", StringComparison.OrdinalIgnoreCase) ||
                                 line.Contains("\"level\":\"info\"", StringComparison.OrdinalIgnoreCase))
                            color = WallyTheme.TextSecondary;

                        AppendLine(line, color);
                    }

                    AppendLine("", WallyTheme.TextPrimary);
                }

                _lblInfo.Text = $"Showing: {item.Name} ({files.Length} file(s))";
            }
            catch (Exception ex)
            {
                AppendLine($"Error reading logs: {ex.Message}", WallyTheme.Red);
            }
        }

        private void AppendLine(string text, Color color)
        {
            _txtLogContent.SelectionStart = _txtLogContent.TextLength;
            _txtLogContent.SelectionLength = 0;
            _txtLogContent.SelectionColor = color;
            _txtLogContent.AppendText(text + Environment.NewLine);
        }

        // ?? Log item wrapper ????????????????????????????????????????????????

        private sealed class LogItem
        {
            public string Name { get; }
            public string Path { get; }
            public bool IsFile { get; }

            public LogItem(string name, string path, bool isFile)
            {
                Name = name;
                Path = path;
                IsFile = isFile;
            }

            public override string ToString() => Name;
        }
    }
}
