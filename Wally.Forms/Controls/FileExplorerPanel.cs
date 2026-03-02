using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// File explorer panel — left-side workspace tree with type-specific icons,
    /// lazy-loading, context menus, tooltips with size/date, and a styled header.
    /// </summary>
    public sealed class FileExplorerPanel : UserControl
    {
        // ?? Controls ????????????????????????????????????????????????????????

        private readonly Panel _header;
        private readonly Label _lblTitle;
        private readonly Label _lblSubtitle;
        private readonly ToolStrip _toolbar;
        private readonly ToolStripButton _btnRefresh;
        private readonly ToolStripButton _btnCollapseAll;
        private readonly ToolStripButton _btnExpandWally;
        private readonly TreeView _tree;
        private readonly ImageList _imageList;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _ctxOpen;
        private readonly ToolStripMenuItem _ctxOpenFolder;
        private readonly ToolStripMenuItem _ctxCopyPath;
        private readonly ToolStripMenuItem _ctxRefresh;

        // ?? State ???????????????????????????????????????????????????????????

        private string? _rootPath;

        // ?? Events ??????????????????????????????????????????????????????????

        public event EventHandler<FileSelectedEventArgs>? FileDoubleClicked;
        public event EventHandler<FileSelectedEventArgs>? FileSelected;

        // ?? Constructor ?????????????????????????????????????????????????????

        public FileExplorerPanel()
        {
            SuspendLayout();

            var renderer = new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false };

            // ?? Image list ??
            _imageList = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
            _imageList.Images.Add("folder", DrawIcon(IconKind.Folder));
            _imageList.Images.Add("folder-open", DrawIcon(IconKind.FolderOpen));
            _imageList.Images.Add("file", DrawIcon(IconKind.File));
            _imageList.Images.Add("file-code", DrawIcon(IconKind.Code));
            _imageList.Images.Add("file-doc", DrawIcon(IconKind.Doc));
            _imageList.Images.Add("file-json", DrawIcon(IconKind.Config));

            // ?? Header (title + subtitle) ??
            _lblTitle = new Label
            {
                Text = "EXPLORER",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _lblSubtitle = new Label
            {
                Text = "",
                Dock = DockStyle.Right,
                Width = 120,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextDisabled,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0)
            };
            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = WallyTheme.Surface2
            };
            _header.Controls.Add(_lblTitle);
            _header.Controls.Add(_lblSubtitle);

            // ?? Toolbar ??
            _btnRefresh = new ToolStripButton("\u21BB")
            {
                ToolTipText = "Refresh (F5)",
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

            _btnExpandWally = new ToolStripButton(".wally")
            {
                ToolTipText = "Jump to .wally folder",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = WallyTheme.Accent
            };
            _btnExpandWally.Click += OnExpandWally;

            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = renderer,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                Padding = new Padding(4, 0, 4, 0)
            };
            _toolbar.Items.AddRange(new ToolStripItem[]
            {
                _btnRefresh, _btnCollapseAll,
                new ToolStripSeparator(),
                _btnExpandWally
            });

            // ?? Context menu ??
            _ctxOpen = new ToolStripMenuItem("Open");
            _ctxOpen.Click += (_, _) => OpenSelectedFile();

            _ctxOpenFolder = new ToolStripMenuItem("Open in Explorer");
            _ctxOpenFolder.Click += (_, _) => OpenSelectedInExplorer();

            _ctxCopyPath = new ToolStripMenuItem("Copy Path");
            _ctxCopyPath.Click += (_, _) => CopySelectedPath();

            _ctxRefresh = new ToolStripMenuItem("Refresh");
            _ctxRefresh.Click += (_, _) => RefreshSelectedNode();

            _contextMenu = new ContextMenuStrip { Renderer = renderer };
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                _ctxOpen, _ctxOpenFolder,
                new ToolStripSeparator(),
                _ctxCopyPath,
                new ToolStripSeparator(),
                _ctxRefresh
            });
            _contextMenu.BackColor = WallyTheme.Surface2;
            _contextMenu.ForeColor = WallyTheme.TextPrimary;
            _contextMenu.Opening += OnContextMenuOpening;

            // ?? Tree view ??
            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                ImageList = _imageList,
                ShowLines = false,
                ShowRootLines = true,
                HideSelection = false,
                FullRowSelect = true,
                ShowNodeToolTips = true,
                Font = WallyTheme.FontUI,
                BorderStyle = BorderStyle.None,
                ItemHeight = 24,
                Indent = 18,
                ContextMenuStrip = _contextMenu,
                BackColor = WallyTheme.Surface1,
                ForeColor = WallyTheme.TextPrimary,
                DrawMode = TreeViewDrawMode.OwnerDrawText
            };
            _tree.BeforeExpand += OnBeforeExpand;
            _tree.AfterSelect += OnAfterSelect;
            _tree.NodeMouseDoubleClick += OnNodeDoubleClick;
            _tree.DrawNode += OnDrawNode;

            // ?? Bottom border ??
            var bottomBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = WallyTheme.Border
            };

            // ?? Assembly ??
            Controls.Add(_tree);
            Controls.Add(bottomBorder);
            Controls.Add(_toolbar);
            Controls.Add(_header);

            BackColor = WallyTheme.Surface1;
            ForeColor = WallyTheme.TextPrimary;

            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void SetRootPath(string rootPath)
        {
            _rootPath = Path.GetFullPath(rootPath);
            _lblTitle.Text = $"EXPLORER \u2014 {Path.GetFileName(_rootPath)}";
            PopulateTree();
        }

        public override void Refresh()
        {
            base.Refresh();
            if (_rootPath != null) PopulateTree();
        }

        // ?? Tree population ?????????????????????????????????????????????????

        private void PopulateTree()
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();

            if (_rootPath == null || !Directory.Exists(_rootPath))
            {
                _tree.Nodes.Add("(no workspace loaded)");
                _lblSubtitle.Text = "";
                _tree.EndUpdate();
                return;
            }

            var rootNode = CreateDirNode(_rootPath);
            rootNode.Text = Path.GetFileName(_rootPath);
            rootNode.Expand();
            _tree.Nodes.Add(rootNode);
            _tree.EndUpdate();

            UpdateSubtitleCount();
        }

        private static TreeNode CreateDirNode(string path)
        {
            var node = new TreeNode(Path.GetFileName(path))
            {
                Tag = path,
                ImageKey = "folder",
                SelectedImageKey = "folder-open",
                ToolTipText = path
            };

            try
            {
                bool hasChildren = Directory.EnumerateFileSystemEntries(path).GetEnumerator().MoveNext();
                if (hasChildren)
                    node.Nodes.Add(new TreeNode("\u2026") { Tag = "__placeholder__" });
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            return node;
        }

        private void LoadChildren(TreeNode parentNode)
        {
            if (parentNode.Tag is not string path || !Directory.Exists(path)) return;
            parentNode.Nodes.Clear();

            try
            {
                var dirs = Directory.GetDirectories(path);
                Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
                foreach (string dir in dirs)
                {
                    string name = Path.GetFileName(dir);
                    if (ShouldSkipDir(name)) continue;
                    parentNode.Nodes.Add(CreateDirNode(dir));
                }

                var files = Directory.GetFiles(path);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string imageKey = FileImageKey(fileName);
                    string tooltip;
                    try
                    {
                        var fi = new FileInfo(file);
                        tooltip = $"{file}\n{FormatSize(fi.Length)}  \u2502  {fi.LastWriteTime:yyyy-MM-dd HH:mm}";
                    }
                    catch { tooltip = file; }

                    parentNode.Nodes.Add(new TreeNode(fileName)
                    {
                        Tag = file,
                        ImageKey = imageKey,
                        SelectedImageKey = imageKey,
                        ToolTipText = tooltip
                    });
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            UpdateSubtitleCount();
        }

        private static bool ShouldSkipDir(string name)
        {
            if (name.StartsWith('.') &&
                !name.Equals(".wally", StringComparison.OrdinalIgnoreCase) &&
                !name.Equals(".git", StringComparison.OrdinalIgnoreCase))
                return true;

            return name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("__pycache__", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateSubtitleCount()
        {
            int count = CountVisibleNodes(_tree.Nodes);
            _lblSubtitle.Text = $"{count} items";
        }

        private static int CountVisibleNodes(TreeNodeCollection nodes)
        {
            int count = 0;
            foreach (TreeNode n in nodes)
            {
                if (n.Tag as string == "__placeholder__") continue;
                count++;
                if (n.IsExpanded) count += CountVisibleNodes(n.Nodes);
            }
            return count;
        }

        // ?? Events ??????????????????????????????????????????????????????????

        private void OnBeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Nodes.Count == 1 && e.Node.Nodes[0].Tag as string == "__placeholder__")
                LoadChildren(e.Node);
        }

        private void OnAfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is string fp && File.Exists(fp))
                FileSelected?.Invoke(this, new FileSelectedEventArgs(fp));
        }

        private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is string fp && File.Exists(fp))
                FileDoubleClicked?.Invoke(this, new FileSelectedEventArgs(fp));
        }

        private void OnDrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            bool selected = (e.State & TreeNodeStates.Selected) != 0;
            bool focused = (e.State & TreeNodeStates.Focused) != 0;

            // ?? Background ??
            Color bg = selected ? WallyTheme.Surface3 : WallyTheme.Surface1;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            // ?? Left accent bar on selected ??
            if (selected)
            {
                using var accent = new SolidBrush(WallyTheme.Accent);
                e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y, 2, e.Bounds.Height);
            }

            // ?? Text ??
            Color fg = selected ? WallyTheme.TextPrimary : WallyTheme.TextSecondary;
            TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.TreeView!.Font,
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        private void OnExpandWally(object? sender, EventArgs e)
        {
            if (_tree.Nodes.Count == 0) return;
            foreach (TreeNode child in _tree.Nodes[0].Nodes)
            {
                if (child.Text.Equals(".wally", StringComparison.OrdinalIgnoreCase))
                {
                    child.Expand();
                    _tree.SelectedNode = child;
                    child.EnsureVisible();
                    return;
                }
            }
        }

        // ?? Context menu ????????????????????????????????????????????????????

        private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_tree.SelectedNode?.Tag is not string path || path == "__placeholder__")
            { e.Cancel = true; return; }

            bool isFile = File.Exists(path);
            _ctxOpen.Visible = isFile;
            _ctxOpenFolder.Text = isFile ? "Open Containing Folder" : "Open in Explorer";
        }

        private void OpenSelectedFile()
        {
            if (_tree.SelectedNode?.Tag is string fp && File.Exists(fp))
                FileDoubleClicked?.Invoke(this, new FileSelectedEventArgs(fp));
        }

        private void OpenSelectedInExplorer()
        {
            if (_tree.SelectedNode?.Tag is not string path) return;
            try
            {
                if (File.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                else if (Directory.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
            }
            catch { }
        }

        private void CopySelectedPath()
        {
            if (_tree.SelectedNode?.Tag is string path)
                Clipboard.SetText(path);
        }

        private void RefreshSelectedNode()
        {
            if (_tree.SelectedNode?.Tag is string path && Directory.Exists(path))
                LoadChildren(_tree.SelectedNode);
        }

        // ?? File classification ?????????????????????????????????????????????

        private static string FileImageKey(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".cs" or ".js" or ".ts" or ".tsx" or ".jsx" or ".py" or ".java" or
                ".cpp" or ".c" or ".h" or ".hpp" or ".xaml" or ".razor" or ".go" or
                ".rs" or ".rb" or ".php" or ".swift" or ".kt" or ".lua" or ".sh" or
                ".ps1" or ".bat" or ".cmd" => "file-code",

                ".md" or ".txt" or ".rst" or ".adoc" or ".rtf" or ".log" => "file-doc",

                ".json" or ".xml" or ".yaml" or ".yml" or ".toml" or ".ini" or
                ".config" or ".csproj" or ".sln" or ".props" or ".targets" or
                ".editorconfig" or ".gitignore" or ".env" => "file-json",

                _ => "file"
            };
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };

        // ?? Icon generation ?????????????????????????????????????????????????

        private enum IconKind { Folder, FolderOpen, File, Code, Doc, Config }

        private static Bitmap DrawIcon(IconKind kind)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            switch (kind)
            {
                case IconKind.Folder:
                    using (var b = new SolidBrush(Color.FromArgb(220, 180, 80)))
                    {
                        g.FillRectangle(b, 1, 3, 6, 2);
                        g.FillRectangle(b, 1, 5, 14, 9);
                    }
                    break;

                case IconKind.FolderOpen:
                    using (var b = new SolidBrush(Color.FromArgb(245, 210, 100)))
                    {
                        g.FillRectangle(b, 1, 3, 6, 2);
                        g.FillRectangle(b, 0, 5, 15, 9);
                    }
                    break;

                case IconKind.File:
                    DrawFileBody(g, Color.FromArgb(160, 160, 170), Color.FromArgb(120, 120, 130));
                    break;

                case IconKind.Code:
                    DrawFileBody(g, Color.FromArgb(86, 156, 214), Color.FromArgb(55, 110, 175));
                    break;

                case IconKind.Doc:
                    DrawFileBody(g, Color.FromArgb(74, 180, 110), Color.FromArgb(50, 130, 80));
                    break;

                case IconKind.Config:
                    DrawFileBody(g, Color.FromArgb(210, 170, 80), Color.FromArgb(170, 130, 55));
                    break;
            }
            return bmp;
        }

        private static void DrawFileBody(Graphics g, Color body, Color corner)
        {
            using var brush = new SolidBrush(body);
            g.FillRectangle(brush, 3, 1, 10, 14);
            using var cBrush = new SolidBrush(corner);
            g.FillPolygon(cBrush, new[] { new Point(9, 1), new Point(13, 5), new Point(9, 5) });
        }
    }

    // ?? Event args ??????????????????????????????????????????????????????

    public sealed class FileSelectedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public FileSelectedEventArgs(string filePath) => FilePath = filePath;
    }
}
