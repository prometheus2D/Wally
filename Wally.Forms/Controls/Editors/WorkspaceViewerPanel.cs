using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Core.Providers;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Read-only viewer that displays a comprehensive summary of the loaded
    /// workspace — paths, config, actors, loops, wrappers, runbooks, resolved
    /// defaults, session info, and disk structure. Designed for expert users
    /// who want a single-pane view of everything that matters.
    /// </summary>
    public sealed class WorkspaceViewerPanel : UserControl
    {
        // ?? Controls ????????????????????????????????????????????????????????

        private readonly RichTextBox _txtOutput;
        private readonly Button _btnRefresh;
        private readonly Button _btnCopy;
        private readonly Label _lblStatus;

        // ?? State ???????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;

        // ?? Constructor ?????????????????????????????????????????????????????

        public WorkspaceViewerPanel()
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
                Text = "\uD83D\uDCCA Workspace Viewer",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = WallyTheme.FontUIBold,
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent
            };

            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _btnRefresh = CreateButton("\u21BB Refresh");
            _btnRefresh.Click += (_, _) => BuildView();

            _btnCopy = CreateButton("\uD83D\uDCCB Copy");
            _btnCopy.Click += (_, _) =>
            {
                if (!string.IsNullOrEmpty(_txtOutput.Text))
                {
                    Clipboard.SetText(_txtOutput.Text);
                    _lblStatus.Text = "Copied to clipboard.";
                    _lblStatus.ForeColor = WallyTheme.Green;
                }
            };

            actionBar.Controls.Add(_btnRefresh);
            actionBar.Controls.Add(_btnCopy);

            _lblStatus = new Label
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
            headerPanel.Controls.Add(actionBar);
            headerPanel.Controls.Add(_lblStatus);

            // ?? Output ??
            _txtOutput = new RichTextBox
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

            Controls.Add(_txtOutput);
            Controls.Add(headerPanel);

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
        }

        /// <summary>Rebuilds the viewer output from the current environment state.</summary>
        public void BuildView()
        {
            _txtOutput.Clear();

            if (_environment?.HasWorkspace != true)
            {
                AppendSection("Status", "No workspace loaded.", WallyTheme.Red);
                return;
            }

            try
            {
                var ws = _environment.Workspace!;
                var cfg = ws.Config;

                // ?? 1. Workspace identity ??
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"WorkSource:        {ws.WorkSource}");
                    sb.AppendLine($"WorkspaceFolder:   {ws.WorkspaceFolder}");
                    sb.AppendLine($"SourcePath:        {ws.SourcePath}");
                    sb.AppendLine($"IsLoaded:          {ws.IsLoaded}");
                    AppendSection("Workspace", sb.ToString().TrimEnd(), WallyTheme.TextPrimary);
                }

                // ?? 2. Folder structure ??
                {
                    var sb = new StringBuilder();
                    AppendFolderLine(sb, "Actors",    cfg.ActorsFolderName,    ws.WorkspaceFolder);
                    AppendFolderLine(sb, "Docs",      cfg.DocsFolderName,      ws.WorkspaceFolder);
                    AppendFolderLine(sb, "Templates", cfg.TemplatesFolderName, ws.WorkspaceFolder);
                    AppendFolderLine(sb, "Loops",     cfg.LoopsFolderName,     ws.WorkspaceFolder);
                    AppendFolderLine(sb, "Wrappers",  cfg.WrappersFolderName,  ws.WorkspaceFolder);
                    AppendFolderLine(sb, "Runbooks",  cfg.RunbooksFolderName,  ws.WorkspaceFolder);
                    AppendFolderLine(sb, "Logs",      cfg.LogsFolderName,      ws.WorkspaceFolder);
                    AppendSection("Folder Structure", sb.ToString().TrimEnd(), WallyTheme.TextSecondary);
                }

                // ?? 3. Resolved defaults ??
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"DefaultModel:      {cfg.DefaultModel ?? "(none)"}");
                    sb.AppendLine($"DefaultWrapper:    {cfg.DefaultWrapper ?? "(none)"}");
                    sb.AppendLine($"DefaultLoop:       {cfg.ResolvedDefaultLoop ?? "(none)"}");
                    sb.AppendLine($"DefaultRunbook:    {cfg.ResolvedDefaultRunbook ?? "(none)"}");
                    sb.AppendLine($"MaxIterations:     {cfg.MaxIterations}");
                    sb.AppendLine($"LogRotationMin:    {(cfg.LogRotationMinutes > 0 ? $"{cfg.LogRotationMinutes} min" : "disabled")}");
                    AppendSection("Resolved Defaults", sb.ToString().TrimEnd(), WallyTheme.TextSecondary);
                }

                // ?? 4. Available / Selected lists ??
                {
                    var sb = new StringBuilder();
                    AppendList(sb, "DefaultModels",     cfg.DefaultModels);
                    AppendList(sb, "SelectedModels",    cfg.SelectedModels);
                    sb.AppendLine();
                    AppendList(sb, "DefaultWrappers",   cfg.DefaultWrappers);
                    AppendList(sb, "SelectedWrappers",  cfg.SelectedWrappers);
                    sb.AppendLine();
                    AppendList(sb, "DefaultLoops",      cfg.DefaultLoops);
                    AppendList(sb, "SelectedLoops",     cfg.SelectedLoops);
                    sb.AppendLine();
                    AppendList(sb, "DefaultRunbooks",   cfg.DefaultRunbooks);
                    AppendList(sb, "SelectedRunbooks",  cfg.SelectedRunbooks);
                    AppendSection("Config Lists (Available / Priority)", sb.ToString().TrimEnd(), WallyTheme.TextMuted);
                }

                // ?? 5. Actors ??
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Loaded: {ws.Actors.Count}");
                    sb.AppendLine();
                    foreach (var actor in ws.Actors)
                    {
                        sb.AppendLine($"  [{actor.Name}]");
                        sb.AppendLine($"    Folder:   {actor.FolderPath}");
                        sb.AppendLine($"    Role:     {Truncate(actor.Role.Prompt, 100)}");
                        sb.AppendLine($"    Criteria: {Truncate(actor.AcceptanceCriteria.Prompt, 100)}");
                        sb.AppendLine($"    Intent:   {Truncate(actor.Intent.Prompt, 100)}");
                        sb.AppendLine($"    Docs:     {actor.DocsFolderName}");

                        // Check for actor-level docs
                        string actorDocsPath = Path.Combine(actor.FolderPath, actor.DocsFolderName);
                        if (Directory.Exists(actorDocsPath))
                        {
                            var docFiles = Directory.GetFiles(actorDocsPath, "*", SearchOption.AllDirectories);
                            sb.AppendLine($"    DocFiles: {docFiles.Length} file(s)");
                        }
                        sb.AppendLine();
                    }
                    AppendSection($"Actors ({ws.Actors.Count})", sb.ToString().TrimEnd(), WallyTheme.TextPrimary);
                }

                // ?? 6. Loops ??
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Loaded: {ws.Loops.Count}");
                    sb.AppendLine();
                    foreach (var loop in ws.Loops)
                    {
                        sb.AppendLine($"  [{loop.Name}]");
                        if (!string.IsNullOrWhiteSpace(loop.Description))
                            sb.AppendLine($"    Description:    {loop.Description}");
                        sb.AppendLine($"    Actor:          {(string.IsNullOrWhiteSpace(loop.ActorName) ? "(caller specifies)" : loop.ActorName)}");
                        sb.AppendLine($"    MaxIterations:  {(loop.MaxIterations > 0 ? loop.MaxIterations.ToString() : "(workspace default)")}");
                        sb.AppendLine($"    Completed:      {loop.ResolvedCompletedKeyword}");
                        sb.AppendLine($"    Error:          {loop.ResolvedErrorKeyword}");
                        sb.AppendLine($"    StartPrompt:    {Truncate(loop.StartPrompt, 120)}");
                        if (!string.IsNullOrWhiteSpace(loop.ContinuePromptTemplate))
                            sb.AppendLine($"    ContinueTmpl:   {Truncate(loop.ContinuePromptTemplate, 120)}");
                        sb.AppendLine();
                    }
                    AppendSection($"Loops ({ws.Loops.Count})", sb.ToString().TrimEnd(), WallyTheme.TextPrimary);
                }

                // ?? 7. Wrappers ??
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Loaded: {ws.LlmWrappers.Count}");
                    sb.AppendLine();
                    foreach (var w in ws.LlmWrappers)
                    {
                        sb.AppendLine($"  [{w.Name}]");
                        if (!string.IsNullOrWhiteSpace(w.Description))
                            sb.AppendLine($"    Description:      {w.Description}");
                        sb.AppendLine($"    Executable:       {w.Executable}");
                        sb.AppendLine($"    ArgTemplate:      {Truncate(w.ArgumentTemplate, 120)}");
                        sb.AppendLine($"    ModelArgFmt:      {w.ModelArgFormat}");
                        sb.AppendLine($"    SourcePathArgFmt: {w.SourcePathArgFormat}");
                        sb.AppendLine($"    UseSourceAsWD:    {w.UseSourcePathAsWorkingDirectory}");
                        sb.AppendLine($"    CanMakeChanges:   {w.CanMakeChanges}");
                        sb.AppendLine();
                    }
                    AppendSection($"Wrappers ({ws.LlmWrappers.Count})", sb.ToString().TrimEnd(), WallyTheme.TextPrimary);
                }

                // ?? 8. Runbooks ??
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Loaded: {ws.Runbooks.Count}");
                    sb.AppendLine();
                    foreach (var rb in ws.Runbooks)
                    {
                        sb.AppendLine($"  [{rb.Name}]");
                        if (!string.IsNullOrWhiteSpace(rb.Description))
                            sb.AppendLine($"    Description: {rb.Description}");
                        sb.AppendLine($"    Commands:    {rb.Commands.Count}");
                        sb.AppendLine($"    File:        {rb.FilePath}");
                        if (rb.Commands.Count > 0)
                        {
                            int previewCount = Math.Min(rb.Commands.Count, 5);
                            for (int i = 0; i < previewCount; i++)
                                sb.AppendLine($"      {i + 1}. {Truncate(rb.Commands[i], 100)}");
                            if (rb.Commands.Count > previewCount)
                                sb.AppendLine($"      ... ({rb.Commands.Count - previewCount} more)");
                        }
                        sb.AppendLine();
                    }
                    AppendSection($"Runbooks ({ws.Runbooks.Count})", sb.ToString().TrimEnd(), WallyTheme.TextPrimary);
                }

                // ?? 9. Workspace docs ??
                {
                    var sb = new StringBuilder();
                    string wsDocsDir = Path.Combine(ws.WorkspaceFolder, cfg.DocsFolderName);
                    if (Directory.Exists(wsDocsDir))
                    {
                        var docFiles = Directory.GetFiles(wsDocsDir, "*", SearchOption.AllDirectories);
                        sb.AppendLine($"Folder:  {wsDocsDir}");
                        sb.AppendLine($"Files:   {docFiles.Length}");
                        foreach (var f in docFiles.Take(20))
                        {
                            string rel = Path.GetRelativePath(ws.WorkSource, f);
                            long size = new FileInfo(f).Length;
                            sb.AppendLine($"  {rel}  ({FormatSize(size)})");
                        }
                        if (docFiles.Length > 20)
                            sb.AppendLine($"  ... ({docFiles.Length - 20} more)");
                    }
                    else
                    {
                        sb.AppendLine($"Folder:  {wsDocsDir}");
                        sb.AppendLine($"(not found)");
                    }

                    // Templates
                    sb.AppendLine();
                    string templatesDir = Path.Combine(ws.WorkspaceFolder, cfg.TemplatesFolderName);
                    if (Directory.Exists(templatesDir))
                    {
                        var tplFiles = Directory.GetFiles(templatesDir, "*", SearchOption.AllDirectories);
                        sb.AppendLine($"Templates Folder: {templatesDir}");
                        sb.AppendLine($"Templates Files:  {tplFiles.Length}");
                        foreach (var f in tplFiles.Take(20))
                        {
                            string rel = Path.GetRelativePath(ws.WorkSource, f);
                            sb.AppendLine($"  {rel}");
                        }
                        if (tplFiles.Length > 20)
                            sb.AppendLine($"  ... ({tplFiles.Length - 20} more)");
                    }
                    else
                    {
                        sb.AppendLine($"Templates Folder: {templatesDir}");
                        sb.AppendLine($"(not found)");
                    }
                    AppendSection("Documentation & Templates", sb.ToString().TrimEnd(), WallyTheme.TextSecondary);
                }

                // ?? 10. Session info ??
                {
                    var logger = _environment.Logger;
                    var sb = new StringBuilder();
                    sb.AppendLine($"SessionId:     {logger.SessionId:N}");
                    sb.AppendLine($"StartedAt:     {logger.StartedAt:u}");
                    sb.AppendLine($"LogFolder:     {logger.LogFolder ?? "(not bound)"}");
                    sb.AppendLine($"CurrentLogFile:{logger.CurrentLogFile ?? "(none)"}");
                    sb.AppendLine($"LogRotation:   {(cfg.LogRotationMinutes > 0 ? $"every {cfg.LogRotationMinutes} min" : "disabled")}");

                    // Count log sessions
                    string logsDir = Path.Combine(ws.WorkspaceFolder, cfg.LogsFolderName);
                    if (Directory.Exists(logsDir))
                    {
                        var sessionDirs = Directory.GetDirectories(logsDir);
                        var looseFiles = Directory.GetFiles(logsDir, "*.jsonl");
                        sb.AppendLine($"LogSessions:   {sessionDirs.Length} dir(s), {looseFiles.Length} loose file(s)");
                    }
                    AppendSection("Session", sb.ToString().TrimEnd(), WallyTheme.TextMuted);
                }

                // ?? 11. Disk size summary ??
                {
                    var sb = new StringBuilder();
                    try
                    {
                        long totalSize = GetDirectorySize(ws.WorkspaceFolder);
                        sb.AppendLine($"Total .wally/ size: {FormatSize(totalSize)}");
                        sb.AppendLine();

                        // Size per subfolder
                        string[] subfolders = new[]
                        {
                            cfg.ActorsFolderName, cfg.DocsFolderName, cfg.TemplatesFolderName,
                            cfg.LoopsFolderName, cfg.WrappersFolderName, cfg.RunbooksFolderName,
                            cfg.LogsFolderName
                        };
                        foreach (string sub in subfolders)
                        {
                            string subPath = Path.Combine(ws.WorkspaceFolder, sub);
                            if (Directory.Exists(subPath))
                            {
                                long subSize = GetDirectorySize(subPath);
                                int fileCount = Directory.GetFiles(subPath, "*", SearchOption.AllDirectories).Length;
                                sb.AppendLine($"  {sub + "/",-18} {FormatSize(subSize),10}  ({fileCount} file{(fileCount == 1 ? "" : "s")})");
                            }
                            else
                            {
                                sb.AppendLine($"  {sub + "/",-18} (missing)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"Error calculating size: {ex.Message}");
                    }
                    AppendSection("Disk Usage", sb.ToString().TrimEnd(), WallyTheme.TextMuted);
                }

                _lblStatus.Text = $"Refreshed at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
            }
            catch (Exception ex)
            {
                AppendSection("Error", ex.ToString(), WallyTheme.Red);
                _lblStatus.Text = "Failed to build view.";
                _lblStatus.ForeColor = WallyTheme.Red;
            }
        }

        // ?? Output helpers ??????????????????????????????????????????????????

        private void AppendSection(string heading, string body, Color headingColor)
        {
            _txtOutput.SelectionStart = _txtOutput.TextLength;
            _txtOutput.SelectionLength = 0;
            _txtOutput.SelectionColor = headingColor;
            _txtOutput.SelectionFont = WallyTheme.FontUISmallBold;
            _txtOutput.AppendText($"??? {heading} ");
            _txtOutput.SelectionColor = WallyTheme.Border;
            _txtOutput.AppendText(new string('?', Math.Max(0, 64 - heading.Length)));
            _txtOutput.AppendText(Environment.NewLine);

            _txtOutput.SelectionStart = _txtOutput.TextLength;
            _txtOutput.SelectionLength = 0;
            _txtOutput.SelectionColor = WallyTheme.TextPrimary;
            _txtOutput.SelectionFont = WallyTheme.FontMono;
            _txtOutput.AppendText(body);
            _txtOutput.AppendText(Environment.NewLine + Environment.NewLine);
        }

        // ?? Private helpers ?????????????????????????????????????????????????

        private static void AppendFolderLine(StringBuilder sb, string label, string folderName, string wsFolder)
        {
            string path = Path.Combine(wsFolder, folderName);
            bool exists = Directory.Exists(path);
            string status = exists ? "\u2713" : "\u2717 (missing)";
            sb.AppendLine($"  {label,-14} {folderName + "/",-18} {status}");
        }

        private static void AppendList(StringBuilder sb, string label, List<string> items)
        {
            if (items.Count == 0)
            {
                sb.AppendLine($"  {label}: (empty)");
            }
            else
            {
                sb.AppendLine($"  {label}: [{string.Join(", ", items)}]");
            }
        }

        private static string Truncate(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "(empty)";
            string singleLine = text.Replace("\r", "").Replace("\n", " ");
            return singleLine.Length > maxLength
                ? singleLine[..maxLength] + "\u2026"
                : singleLine;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        private static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }

        // ?? Control factories ???????????????????????????????????????????????

        private static Button CreateButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3,
                ForeColor = WallyTheme.TextPrimary,
                Font = WallyTheme.FontUISmallBold,
                Cursor = Cursors.Hand,
                Padding = new Padding(8, 2, 8, 2),
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = WallyTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = WallyTheme.Surface4;
            return btn;
        }
    }
}
