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
    /// Project Explorer — shows the live state of the Wally workspace:
    /// actors with their mailbox folders (Inbox / Outbox / Pending / Active)
    /// and private docs, the Projects hierarchy (Epochs ? Sprints ? Tasks),
    /// and workspace-level documentation.
    ///
    /// This is a "what's in flight" view — use the Object Explorer to inspect
    /// the workspace configuration objects (Loops, Wrappers, Runbooks, etc.).
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
        private enum NodeKind { Category, Actor, Mailbox, Doc, File, Project, Epoch, Sprint, Task }

        // ?? Constructor ?????????????????????????????????????????????????????

        public ProjectExplorerPanel()
        {
            SuspendLayout();

            var renderer = WallyTheme.CreateRenderer();

            // Image list
            _imageList = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
            _imageList.Images.Add("category", DrawCategoryIcon(Color.FromArgb(113, 113, 122)));
            _imageList.Images.Add("actor",    DrawCircleIcon(Color.FromArgb(130, 180, 240)));
            _imageList.Images.Add("inbox",    DrawMailboxIcon(Color.FromArgb(100, 200, 130)));
            _imageList.Images.Add("outbox",   DrawMailboxIcon(Color.FromArgb(200, 160, 80)));
            _imageList.Images.Add("pending",  DrawMailboxIcon(Color.FromArgb(200, 120, 120)));
            _imageList.Images.Add("active",   DrawMailboxIcon(Color.FromArgb(120, 160, 220)));
            _imageList.Images.Add("mailbox",  DrawMailboxIcon(Color.FromArgb(150, 150, 160)));
            _imageList.Images.Add("doc",      DrawDocIcon(Color.FromArgb(100, 180, 100)));
            _imageList.Images.Add("file",     DrawFileIcon(Color.FromArgb(160, 160, 170)));
            _imageList.Images.Add("project",  DrawFolderIcon(Color.FromArgb(210, 170, 80)));
            _imageList.Images.Add("epoch",    DrawFolderIcon(Color.FromArgb(180, 140, 200)));
            _imageList.Images.Add("sprint",   DrawFolderIcon(Color.FromArgb(100, 200, 160)));
            _imageList.Images.Add("task",     DrawFileIcon(Color.FromArgb(200, 200, 120)));

            // Toolbar
            _btnRefresh = new ToolStripButton("\u21BB")
            {
                ToolTipText  = "Refresh",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font         = new Font("Segoe UI", 10f),
                ForeColor    = WallyTheme.TextSecondary
            };
            _btnRefresh.Click += (_, _) => Refresh();

            _btnCollapseAll = new ToolStripButton("\u229F")
            {
                ToolTipText  = "Collapse All",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font         = new Font("Segoe UI", 10f),
                ForeColor    = WallyTheme.TextSecondary
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
            _contextMenu.Opening  += OnContextMenuOpening;

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
            _tree.AfterSelect          += OnAfterSelect;
            _tree.NodeMouseDoubleClick += OnNodeDoubleClick;
            _tree.DrawNode             += OnDrawNode;

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

            // ?? Actors (with mailboxes and docs) ?????????????????????????????
            var actorsNode = MakeCategoryNode("Actors", "\U0001F3AD", _environment.Actors.Count);
            foreach (var actor in _environment.Actors)
            {
                string actorFolder = actor.FolderPath;
                var actorNode = new TreeNode(actor.Name)
                {
                    Tag              = new NodeTag(actorFolder, NodeKind.Actor),
                    ImageKey         = "actor",
                    SelectedImageKey = "actor",
                    ToolTipText      = actorFolder
                };

                // Mailbox folders — shown with live item counts
                AddMailboxNode(actorNode, actorFolder, WallyHelper.MailboxInboxFolderName,   "?? Inbox",   "inbox");
                AddMailboxNode(actorNode, actorFolder, WallyHelper.MailboxActiveFolderName,  "? Active",  "active");
                AddMailboxNode(actorNode, actorFolder, WallyHelper.MailboxPendingFolderName, "? Pending", "pending");
                AddMailboxNode(actorNode, actorFolder, WallyHelper.MailboxOutboxFolderName,  "?? Outbox",  "outbox");

                // Actor Docs
                string docsDir = Path.Combine(actorFolder, actor.DocsFolderName);
                if (Directory.Exists(docsDir))
                {
                    string[] docFiles = Directory.GetFiles(docsDir);
                    if (docFiles.Length > 0)
                    {
                        var docsNode = MakeFolderNode($"?? Docs  [{docFiles.Length}]", docsDir, NodeKind.Category, "category");
                        foreach (string f in docFiles)
                            docsNode.Nodes.Add(MakeFileNode(Path.GetFileName(f), f, NodeKind.Doc, "doc"));
                        actorNode.Nodes.Add(docsNode);
                    }
                }

                actorsNode.Nodes.Add(actorNode);
            }
            _tree.Nodes.Add(actorsNode);

            // ?? Projects ?????????????????????????????????????????????????????
            string projectsDir = Path.Combine(ws.WorkspaceFolder, ws.Config.ProjectsFolderName);
            var projectsRoot = MakeCategoryNode("Projects", "\uD83D\uDDC2", -1);
            projectsRoot.Tag = new NodeTag(projectsDir, NodeKind.Category);

            if (Directory.Exists(projectsDir))
            {
                string[] projectDirs = Directory.GetDirectories(projectsDir);
                foreach (string projectDir in projectDirs)
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

                projectsRoot.Text = projectDirs.Length > 0
                    ? $"\uD83D\uDDC2 Projects  [{projectDirs.Length}]"
                    : "\uD83D\uDDC2 Projects";
            }
            else
            {
                projectsRoot.Text      = "\uD83D\uDDC2 Projects  (not initialised)";
                projectsRoot.ForeColor = WallyTheme.TextDisabled;
            }
            _tree.Nodes.Add(projectsRoot);

            // ?? Workspace Docs ???????????????????????????????????????????????
            string wsDocsDir = Path.Combine(ws.WorkspaceFolder, ws.Config.DocsFolderName);
            if (Directory.Exists(wsDocsDir))
            {
                string[] wsDocFiles = Directory.GetFiles(wsDocsDir, "*", SearchOption.AllDirectories);
                if (wsDocFiles.Length > 0)
                {
                    var wsDocs = MakeCategoryNode("Workspace Docs", "\U0001F4D6", wsDocFiles.Length);
                    foreach (string f in wsDocFiles)
                        wsDocs.Nodes.Add(MakeFileNode(Path.GetFileName(f), f, NodeKind.Doc, "doc"));
                    _tree.Nodes.Add(wsDocs);
                }
            }

            // Expand first level
            foreach (TreeNode n in _tree.Nodes)
                n.Expand();

            _tree.EndUpdate();
        }

        // ?? Helpers ?????????????????????????????????????????????????????????

        /// <summary>
        /// Adds a mailbox folder node under <paramref name="actorNode"/>.
        /// The node shows the item count and lists all files inside the folder.
        /// If the folder does not exist or is empty, still shows the folder
        /// so the user can see the mailbox at a glance (empty = clear).
        /// </summary>
        private static void AddMailboxNode(TreeNode actorNode, string actorFolder,
            string folderName, string label, string imageKey)
        {
            string dir = Path.Combine(actorFolder, folderName);
            string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir) : Array.Empty<string>();

            string nodeText = files.Length > 0 ? $"{label}  [{files.Length}]" : label;
            var mailboxNode = new TreeNode(nodeText)
            {
                Tag              = new NodeTag(dir, NodeKind.Mailbox),
                ImageKey         = imageKey,
                SelectedImageKey = imageKey,
                ToolTipText      = dir,
                // Dim empty mailboxes so attention naturally falls on busy ones
                ForeColor        = files.Length > 0 ? WallyTheme.TextSecondary : WallyTheme.TextDisabled
            };

            foreach (string f in files)
                mailboxNode.Nodes.Add(MakeFileNode(Path.GetFileName(f), f, NodeKind.File, "file"));

            actorNode.Nodes.Add(mailboxNode);
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

        private static TreeNode MakeFolderNode(string label, string path, NodeKind kind, string imageKey) =>
            new TreeNode(label)
            {
                Tag              = new NodeTag(path, kind),
                ImageKey         = imageKey,
                SelectedImageKey = imageKey,
                ToolTipText      = path,
                ForeColor        = WallyTheme.TextMuted
            };

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
                parent.Nodes.Add(MakeFileNode(Path.GetFileName(f), f, NodeKind.Task, "task"));
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

            _ctxOpen.Visible       = File.Exists(t.Path);
            _ctxOpenFolder.Text    = File.Exists(t.Path) ? "Open Containing Folder" : "Open in Explorer";
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

        private static Bitmap DrawMailboxIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(c);
            // Envelope body
            g.FillRectangle(b, 2, 5, 12, 8);
            // Envelope flap (triangle)
            using var dark = new SolidBrush(Color.FromArgb(
                Math.Max(0, c.R - 40), Math.Max(0, c.G - 40), Math.Max(0, c.B - 40)));
            g.FillPolygon(dark, new[] { new Point(2, 5), new Point(8, 10), new Point(14, 5) });
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
            g.FillRectangle(b, 1, 4, 6, 2);
            g.FillRectangle(b, 1, 6, 14, 8);
            return bmp;
        }
    }
}
