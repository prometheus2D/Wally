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
    /// Project Explorer — shows the Wally workspace from a project/structural
    /// perspective: Actors, Loops, Wrappers, Runbooks, Config, and Docs grouped
    /// into logical tree categories rather than raw filesystem folders.
    /// </summary>
    public sealed class ProjectExplorerPanel : UserControl
    {
        // ?? Controls ????????????????????????????????????????????????????????

        private readonly WallyToolStrip _toolbar;
        private readonly ToolStripButton _btnRefresh;
        private readonly ToolStripButton _btnCollapseAll;
        private readonly TreeView _tree;
        private readonly ImageList _imageList;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _ctxOpen;
        private readonly ToolStripMenuItem _ctxOpenFolder;
        private readonly ToolStripMenuItem _ctxCopyPath;

        // ?? State ???????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;

        // ?? Events ??????????????????????????????????????????????????????????

        public event EventHandler<FileSelectedEventArgs>? FileDoubleClicked;
        public event EventHandler<FileSelectedEventArgs>? FileSelected;

        // ?? Node tag types ??????????????????????????????????????????????????

        private sealed record NodeTag(string Path, NodeKind Kind);
        private enum NodeKind { Category, Actor, Loop, Wrapper, Runbook, Config, Doc, File, Project, Epoch, Sprint, Task }

        // ?? Constructor ?????????????????????????????????????????????????????

        public ProjectExplorerPanel()
        {
            SuspendLayout();

            var renderer = WallyTheme.CreateRenderer();

            // Image list
            _imageList = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
            _imageList.Images.Add("category",      DrawCategoryIcon(Color.FromArgb(113, 113, 122)));
            _imageList.Images.Add("actor",         DrawCircleIcon(Color.FromArgb(130, 180, 240)));
            _imageList.Images.Add("loop",          DrawCycleIcon(Color.FromArgb(100, 200, 130)));
            _imageList.Images.Add("wrapper",       DrawGearIcon(Color.FromArgb(210, 170, 80)));
            _imageList.Images.Add("runbook",       DrawPageIcon(Color.FromArgb(200, 140, 220)));
            _imageList.Images.Add("config",        DrawGearIcon(Color.FromArgb(161, 161, 170)));
            _imageList.Images.Add("doc",           DrawDocIcon(Color.FromArgb(100, 180, 100)));
            _imageList.Images.Add("file",          DrawFileIcon(Color.FromArgb(160, 160, 170)));
            _imageList.Images.Add("project",       DrawFolderIcon(Color.FromArgb(210, 170, 80)));
            _imageList.Images.Add("epoch",         DrawFolderIcon(Color.FromArgb(180, 140, 200)));
            _imageList.Images.Add("sprint",        DrawFolderIcon(Color.FromArgb(100, 200, 160)));
            _imageList.Images.Add("task",          DrawFileIcon(Color.FromArgb(200, 200, 120)));

            // Toolbar
            _btnRefresh = new ToolStripButton("\u21BB")
            {
                ToolTipText = "Refresh",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 10f),
                ForeColor = WallyTheme.TextSecondary
            };
            _btnRefresh.Click += (_, _) => Refresh();

            _btnCollapseAll = new ToolStripButton("\u229F")
            {
                ToolTipText = "Collapse All",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 10f),
                ForeColor = WallyTheme.TextSecondary
            };
            _btnCollapseAll.Click += (_, _) => _tree.CollapseAll();

            _toolbar = new WallyToolStrip();
            _toolbar.Dock = DockStyle.Top;
            _toolbar.Items.AddRange(new ToolStripItem[] { _btnRefresh, _btnCollapseAll });

            // Context menu
            _ctxOpen = new ToolStripMenuItem("Open") { ForeColor = WallyTheme.TextPrimary };
            _ctxOpen.Click += (_, _) => OpenSelected();

            _ctxOpenFolder = new ToolStripMenuItem("Open in Explorer") { ForeColor = WallyTheme.TextPrimary };
            _ctxOpenFolder.Click += (_, _) => OpenSelectedInExplorer();

            _ctxCopyPath = new ToolStripMenuItem("Copy Path") { ForeColor = WallyTheme.TextPrimary };
            _ctxCopyPath.Click += (_, _) => CopySelectedPath();

            _contextMenu = new ContextMenuStrip { Renderer = renderer };
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                _ctxOpen, _ctxOpenFolder,
                new ToolStripSeparator(),
                _ctxCopyPath
            });
            _contextMenu.BackColor = WallyTheme.Surface2;
            _contextMenu.ForeColor = WallyTheme.TextPrimary;
            _contextMenu.Opening += OnContextMenuOpening;

            // Tree
            _tree = new TreeView
            {
                Dock             = DockStyle.Fill,
                ImageList        = _imageList,
                ShowLines        = false,
                ShowRootLines    = false,
                HideSelection    = false,
                FullRowSelect    = true,
                ShowNodeToolTips = true,
                Font             = WallyTheme.FontUI,
                BorderStyle      = BorderStyle.None,
                ItemHeight       = 24,
                Indent           = 18,
                ContextMenuStrip = _contextMenu,
                BackColor        = WallyTheme.Surface1,
                ForeColor        = WallyTheme.TextPrimary,
                DrawMode         = TreeViewDrawMode.OwnerDrawText
            };
            _tree.AfterSelect           += OnAfterSelect;
            _tree.NodeMouseDoubleClick  += OnNodeDoubleClick;
            _tree.DrawNode              += OnDrawNode;

            Controls.Add(_tree);
            Controls.Add(_toolbar);

            BackColor = WallyTheme.Surface1;
            ForeColor = WallyTheme.TextPrimary;

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
        }

        public void SetWorkspace()
        {
            BuildTree();
        }

        public void ClearTree()
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();
            _tree.EndUpdate();
        }

        public override void Refresh()
        {
            base.Refresh();
            BuildTree();
        }

        // ?? Tree building ???????????????????????????????????????????????????

        private void BuildTree()
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();

            if (_environment?.HasWorkspace != true)
            {
                _tree.Nodes.Add(new TreeNode("(no workspace loaded)")
                {
                    ForeColor = WallyTheme.TextMuted
                });
                _tree.EndUpdate();
                return;
            }

            var ws = _environment.Workspace!;

            // ?? Actors ??
            var actorsNode = MakeCategoryNode("Actors", $"\U0001F3AD", _environment.Actors.Count);
            foreach (var actor in _environment.Actors)
            {
                string actorFolder = actor.FolderPath;
                var n = new TreeNode(actor.Name)
                {
                    Tag              = new NodeTag(actorFolder, NodeKind.Actor),
                    ImageKey         = "actor",
                    SelectedImageKey = "actor",
                    ToolTipText      = actorFolder
                };

                // Actor config file
                string actorJson = Path.Combine(actorFolder, "actor.json");
                if (File.Exists(actorJson))
                    n.Nodes.Add(MakeFileNode("actor.json", actorJson, NodeKind.Config, "config"));

                // Actor Docs folder
                string docsDir = Path.Combine(actorFolder, actor.DocsFolderName);
                if (Directory.Exists(docsDir))
                {
                    var docsNode = MakeCategoryNode("Docs", "\U0001F4C4", 0);
                    docsNode.Tag = new NodeTag(docsDir, NodeKind.Doc);
                    foreach (string f in Directory.GetFiles(docsDir))
                        docsNode.Nodes.Add(MakeFileNode(Path.GetFileName(f), f, NodeKind.Doc, "doc"));
                    if (docsNode.Nodes.Count > 0) docsNode.Text = $"?? Docs  [{docsNode.Nodes.Count}]";
                    n.Nodes.Add(docsNode);
                }

                actorsNode.Nodes.Add(n);
            }
            _tree.Nodes.Add(actorsNode);

            // ?? Loops ??
            var loopsNode = MakeCategoryNode("Loops", "\u267B", _environment.Loops.Count);
            foreach (var loop in _environment.Loops)
            {
                string loopFile = Path.Combine(ws.WorkspaceFolder, ws.Config.LoopsFolderName, loop.Name + ".json");
                var n = new TreeNode(loop.Name)
                {
                    Tag              = new NodeTag(loopFile, NodeKind.Loop),
                    ImageKey         = "loop",
                    SelectedImageKey = "loop",
                    ToolTipText      = string.IsNullOrEmpty(loop.Description) ? loopFile : loop.Description
                };
                if (loop.HasSteps)
                {
                    foreach (var step in loop.Steps)
                    {
                        n.Nodes.Add(new TreeNode($"  {step.Name}  \u2192  {step.ActorName}")
                        {
                            ForeColor        = WallyTheme.TextMuted,
                            ImageKey         = "actor",
                            SelectedImageKey = "actor",
                            Tag              = new NodeTag(loopFile, NodeKind.File)
                        });
                    }
                }
                loopsNode.Nodes.Add(n);
            }
            _tree.Nodes.Add(loopsNode);

            // ?? Wrappers ??
            var wrappersNode = MakeCategoryNode("LLM Wrappers", "\u2699", ws.LlmWrappers.Count);
            foreach (var wrapper in ws.LlmWrappers)
            {
                string wFile = Path.Combine(ws.WorkspaceFolder, ws.Config.WrappersFolderName, wrapper.Name + ".json");
                var n = new TreeNode(wrapper.Name)
                {
                    Tag              = new NodeTag(wFile, NodeKind.Wrapper),
                    ImageKey         = "wrapper",
                    SelectedImageKey = "wrapper",
                    ToolTipText      = wFile
                };
                wrappersNode.Nodes.Add(n);
            }
            _tree.Nodes.Add(wrappersNode);

            // ?? Runbooks ??
            var runbooksNode = MakeCategoryNode("Runbooks", "\uD83D\uDCDC", _environment.Runbooks.Count);
            foreach (var rb in _environment.Runbooks)
            {
                string rbFile = Path.Combine(ws.WorkspaceFolder, ws.Config.RunbooksFolderName, rb.Name + ".wrb");
                var n = new TreeNode(rb.Name)
                {
                    Tag              = new NodeTag(rbFile, NodeKind.Runbook),
                    ImageKey         = "runbook",
                    SelectedImageKey = "runbook",
                    ToolTipText      = rbFile
                };
                runbooksNode.Nodes.Add(n);
            }
            _tree.Nodes.Add(runbooksNode);

            // ?? Projects ??
            string projectsDir = Path.Combine(ws.WorkspaceFolder, ws.Config.ProjectsFolderName);
            var projectsRoot = MakeCategoryNode("Projects", "\uD83D\uDDC2", -1);
            projectsRoot.Tag = new NodeTag(projectsDir, NodeKind.Category);
            if (Directory.Exists(projectsDir))
            {
                foreach (string projectDir in Directory.GetDirectories(projectsDir))
                {
                    string projectName = Path.GetFileName(projectDir);
                    var projectNode = new TreeNode(projectName)
                    {
                        Tag              = new NodeTag(projectDir, NodeKind.Project),
                        ImageKey         = "project",
                        SelectedImageKey = "project",
                        ToolTipText      = projectDir
                    };

                    string epochsDir = Path.Combine(projectDir, WallyHelper.ProjectEpochsFolderName);
                    if (Directory.Exists(epochsDir))
                    {
                        foreach (string epochDir in Directory.GetDirectories(epochsDir))
                        {
                            string epochName = Path.GetFileName(epochDir);
                            var epochNode = new TreeNode(epochName)
                            {
                                Tag              = new NodeTag(epochDir, NodeKind.Epoch),
                                ImageKey         = "epoch",
                                SelectedImageKey = "epoch",
                                ToolTipText      = epochDir
                            };

                            // Tasks directly under the epoch
                            string epochTasksDir = Path.Combine(epochDir, WallyHelper.ProjectTasksFolderName);
                            if (Directory.Exists(epochTasksDir))
                                AddTaskNodes(epochNode, epochTasksDir);

                            // Sprints under the epoch
                            string sprintsDir = Path.Combine(epochDir, WallyHelper.ProjectSprintsFolderName);
                            if (Directory.Exists(sprintsDir))
                            {
                                foreach (string sprintDir in Directory.GetDirectories(sprintsDir))
                                {
                                    string sprintName = Path.GetFileName(sprintDir);
                                    var sprintNode = new TreeNode(sprintName)
                                    {
                                        Tag              = new NodeTag(sprintDir, NodeKind.Sprint),
                                        ImageKey         = "sprint",
                                        SelectedImageKey = "sprint",
                                        ToolTipText      = sprintDir
                                    };
                                    string sprintTasksDir = Path.Combine(sprintDir, WallyHelper.ProjectTasksFolderName);
                                    if (Directory.Exists(sprintTasksDir))
                                        AddTaskNodes(sprintNode, sprintTasksDir);
                                    epochNode.Nodes.Add(sprintNode);
                                }
                            }

                            projectNode.Nodes.Add(epochNode);
                        }
                    }

                    projectsRoot.Nodes.Add(projectNode);
                }

                int projectCount = Directory.GetDirectories(projectsDir).Length;
                projectsRoot.Text = projectCount > 0
                    ? $"\uD83D\uDDC2 Projects  [{projectCount}]"
                    : "\uD83D\uDDC2 Projects";
            }
            else
            {
                projectsRoot.Text      = "\uD83D\uDDC2 Projects  (not initialised)";
                projectsRoot.ForeColor = WallyTheme.TextDisabled;
            }
            _tree.Nodes.Add(projectsRoot);

            // ?? Configuration ??
            string configFile = Path.Combine(ws.WorkspaceFolder, "wally-config.json");
            var configNode = new TreeNode("Configuration")
            {
                Tag              = new NodeTag(ws.WorkspaceFolder, NodeKind.Category),
                ImageKey         = "category",
                SelectedImageKey = "category",
                ForeColor        = WallyTheme.TextMuted
            };
            if (File.Exists(configFile))
                configNode.Nodes.Add(MakeFileNode("wally-config.json", configFile, NodeKind.Config, "config"));
            _tree.Nodes.Add(configNode);

            // ?? Workspace Docs ??
            string wsDocsDir = Path.Combine(ws.WorkspaceFolder, ws.Config.DocsFolderName);
            if (Directory.Exists(wsDocsDir))
            {
                var wsDocs = MakeCategoryNode("Workspace Docs", "\U0001F4D6", 0);
                foreach (string f in Directory.GetFiles(wsDocsDir, "*", SearchOption.AllDirectories))
                    wsDocs.Nodes.Add(MakeFileNode(Path.GetFileName(f), f, NodeKind.Doc, "doc"));
                if (wsDocs.Nodes.Count > 0) wsDocs.Text = $"?? Workspace Docs  [{wsDocs.Nodes.Count}]";
                _tree.Nodes.Add(wsDocs);
            }

            // Expand first level
            foreach (TreeNode n in _tree.Nodes)
                n.Expand();

            _tree.EndUpdate();
        }

        private static TreeNode MakeCategoryNode(string label, string emoji, int count)
        {
            string text = count > 0 ? $"{emoji} {label}  [{count}]" : $"{emoji} {label}";
            return new TreeNode(text)
            {
                ImageKey         = "category",
                SelectedImageKey = "category",
                ForeColor        = WallyTheme.TextMuted
            };
        }

        private static TreeNode MakeFileNode(string label, string path, NodeKind kind, string imageKey) =>
            new TreeNode(label)
            {
                Tag              = new NodeTag(path, kind),
                ImageKey         = imageKey,
                SelectedImageKey = imageKey,
                ToolTipText      = path
            };

        private static void AddTaskNodes(TreeNode parent, string tasksDir)
        {
            foreach (string f in Directory.GetFiles(tasksDir))
            {
                var n = new TreeNode(Path.GetFileName(f))
                {
                    Tag              = new NodeTag(f, NodeKind.Task),
                    ImageKey         = "task",
                    SelectedImageKey = "task",
                    ToolTipText      = f
                };
                parent.Nodes.Add(n);
            }
            foreach (string d in Directory.GetDirectories(tasksDir))
            {
                string name = Path.GetFileName(d);
                var n = new TreeNode(name)
                {
                    Tag              = new NodeTag(d, NodeKind.Task),
                    ImageKey         = "task",
                    SelectedImageKey = "task",
                    ToolTipText      = d
                };
                parent.Nodes.Add(n);
            }
        }

        // ?? Events ??????????????????????????????????????????????????????????

        private void OnAfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is NodeTag t && File.Exists(t.Path))
                FileSelected?.Invoke(this, new FileSelectedEventArgs(t.Path));
        }

        private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is NodeTag t && File.Exists(t.Path))
                FileDoubleClicked?.Invoke(this, new FileSelectedEventArgs(t.Path));
        }

        private void OnDrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            bool selected = (e.State & TreeNodeStates.Selected) != 0;
            Color bg = selected ? WallyTheme.Surface3 : WallyTheme.Surface1;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            if (selected)
            {
                using var accent = new SolidBrush(WallyTheme.Accent);
                e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y, 2, e.Bounds.Height);
            }

            Color fg = e.Node.ForeColor != Color.Empty && e.Node.ForeColor != Color.Black
                ? e.Node.ForeColor
                : (selected ? WallyTheme.TextPrimary : WallyTheme.TextSecondary);

            TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.TreeView!.Font,
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        // ?? Context menu ????????????????????????????????????????????????????

        private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_tree.SelectedNode?.Tag is not NodeTag t || t.Kind == NodeKind.Category)
            { e.Cancel = true; return; }

            _ctxOpen.Visible = File.Exists(t.Path);
            _ctxOpenFolder.Text = File.Exists(t.Path) ? "Open Containing Folder" : "Open in Explorer";
            // Always allow "Open in Explorer" for folder-backed node kinds
            _ctxOpenFolder.Enabled = File.Exists(t.Path) || Directory.Exists(t.Path);
        }

        private void OpenSelected()
        {
            if (_tree.SelectedNode?.Tag is NodeTag t && File.Exists(t.Path))
                FileDoubleClicked?.Invoke(this, new FileSelectedEventArgs(t.Path));
        }

        private void OpenSelectedInExplorer()
        {
            if (_tree.SelectedNode?.Tag is not NodeTag t) return;
            try
            {
                if (File.Exists(t.Path))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{t.Path}\"");
                else if (Directory.Exists(t.Path))
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{t.Path}\"");
            }
            catch { }
        }

        private void CopySelectedPath()
        {
            if (_tree.SelectedNode?.Tag is NodeTag t)
                Clipboard.SetText(t.Path);
        }

        // ?? Icon drawing ????????????????????????????????????????????????????

        private static Bitmap DrawCategoryIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(c);
            g.FillRectangle(b, 1, 5, 14, 2);
            g.FillRectangle(b, 1, 9, 14, 2);
            g.FillRectangle(b, 1, 13, 14, 2);
            return bmp;
        }

        private static Bitmap DrawCircleIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(c);
            g.FillEllipse(b, 2, 2, 12, 12);
            using var inner = new SolidBrush(WallyTheme.Surface1);
            g.FillEllipse(inner, 5, 5, 6, 6);
            return bmp;
        }

        private static Bitmap DrawCycleIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = new System.Drawing.Pen(c, 2f);
            g.DrawArc(p, 2, 2, 12, 12, -30, 270);
            // Arrowhead
            using var b = new SolidBrush(c);
            g.FillPolygon(b, new[] { new Point(13, 4), new Point(9, 2), new Point(11, 7) });
            return bmp;
        }

        private static Bitmap DrawGearIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(c);
            g.FillEllipse(b, 3, 3, 10, 10);
            using var inner = new SolidBrush(WallyTheme.Surface1);
            g.FillEllipse(inner, 5, 5, 6, 6);
            // Teeth
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 45f * (float)(Math.PI / 180.0);
                int cx = 8 + (int)(6.5f * (float)Math.Cos(angle)) - 1;
                int cy = 8 + (int)(6.5f * (float)Math.Sin(angle)) - 1;
                g.FillRectangle(b, cx, cy, 3, 3);
            }
            return bmp;
        }

        private static Bitmap DrawPageIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(c);
            g.FillRectangle(b, 3, 1, 10, 14);
            using var line = new SolidBrush(WallyTheme.Surface1);
            for (int y = 4; y <= 10; y += 3)
                g.FillRectangle(line, 5, y, 6, 1);
            return bmp;
        }

        private static Bitmap DrawDocIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            using var b = new SolidBrush(c);
            g.FillRectangle(b, 3, 1, 10, 14);
            using var corner = new SolidBrush(Color.FromArgb(80, 80, 90));
            g.FillPolygon(corner, new[] { new Point(9, 1), new Point(13, 5), new Point(9, 5) });
            return bmp;
        }

        private static Bitmap DrawFileIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            using var b = new SolidBrush(c);
            g.FillRectangle(b, 3, 1, 10, 14);
            return bmp;
        }

        private static Bitmap DrawFolderIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(c);
            // Tab
            g.FillRectangle(b, 1, 4, 6, 2);
            // Body
            g.FillRectangle(b, 1, 6, 14, 8);
            return bmp;
        }
    }
}
