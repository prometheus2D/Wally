using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Core.Providers;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// Object Explorer — shows loaded runtime objects (Actors, Loops, LLM Wrappers,
    /// Runbooks) in a tree so the user can inspect and interact with the live
    /// workspace state rather than raw files.
    /// </summary>
    public sealed class ObjectExplorerPanel : UserControl
    {
        // ?? Controls ??????????????????????????????????????????????????????????

        private readonly WallyToolStrip _toolbar;
        private readonly ToolStripButton _btnRefresh;
        private readonly ToolStripButton _btnCollapseAll;
        private readonly TreeView _tree;
        private readonly ImageList _imageList;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _ctxOpen;
        private readonly ToolStripMenuItem _ctxCopyName;

        // ?? State ?????????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;

        // ?? Object node tags ??????????????????????????????????????????????????

        private sealed record ObjectNodeTag(object Item, ObjectKind Kind);
        private enum ObjectKind { Actor, Loop, Wrapper, Runbook, LoopStep, ActorProp }

        // ?? Events ????????????????????????????????????????????????????????????

        /// <summary>Raised when the user double-clicks an Actor node — passes the actor name.</summary>
        public event EventHandler<string>? ActorActivated;
        /// <summary>Raised when the user double-clicks a Loop node — passes the loop name.</summary>
        public event EventHandler<string>? LoopActivated;
        /// <summary>Raised when the user double-clicks a Wrapper node — passes the wrapper name.</summary>
        public event EventHandler<string>? WrapperActivated;
        /// <summary>Raised when the user double-clicks a Runbook node — passes the runbook name.</summary>
        public event EventHandler<string>? RunbookActivated;

        // ?? Constructor ???????????????????????????????????????????????????????

        public ObjectExplorerPanel()
        {
            SuspendLayout();

            var renderer = WallyTheme.CreateRenderer();

            // Image list
            _imageList = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
            _imageList.Images.Add("category", DrawCategoryIcon());
            _imageList.Images.Add("actor",    DrawCircleIcon(Color.FromArgb(130, 180, 240)));
            _imageList.Images.Add("loop",     DrawCycleIcon(Color.FromArgb(100, 200, 130)));
            _imageList.Images.Add("wrapper",  DrawGearIcon(Color.FromArgb(210, 170, 80)));
            _imageList.Images.Add("runbook",  DrawPageIcon(Color.FromArgb(200, 140, 220)));
            _imageList.Images.Add("prop",     DrawDotIcon(Color.FromArgb(113, 113, 122)));

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
            _ctxOpen = new ToolStripMenuItem("Open Editor") { ForeColor = WallyTheme.TextPrimary };
            _ctxOpen.Click += (_, _) => ActivateSelected();

            _ctxCopyName = new ToolStripMenuItem("Copy Name") { ForeColor = WallyTheme.TextPrimary };
            _ctxCopyName.Click += (_, _) => CopySelectedName();

            _contextMenu = new ContextMenuStrip { Renderer = renderer };
            _contextMenu.Items.AddRange(new ToolStripItem[] { _ctxOpen, new ToolStripSeparator(), _ctxCopyName });
            _contextMenu.BackColor = WallyTheme.Surface2;
            _contextMenu.ForeColor = WallyTheme.TextPrimary;
            _contextMenu.Opening   += OnContextMenuOpening;

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
                DrawMode         = TreeViewDrawMode.OwnerDrawAll
            };
            _tree.NodeMouseDoubleClick += OnNodeDoubleClick;
            _tree.DrawNode             += OnDrawNode;

            Controls.Add(_tree);
            Controls.Add(_toolbar);

            BackColor = WallyTheme.Surface1;
            ForeColor = WallyTheme.TextPrimary;

            ResumeLayout(true);
        }

        // ?? Public API ????????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env)
        {
            _environment = env;
        }

        public override void Refresh()
        {
            base.Refresh();
            BuildTree();
        }

        public void ClearTree()
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();
            _tree.EndUpdate();
        }

        // ?? Tree building ?????????????????????????????????????????????????????

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

            // ?? Actors ????????????????????????????????????????????????????????
            var actorsRoot = CategoryNode($"\U0001F3AD  Actors  [{_environment.Actors.Count}]");
            foreach (var actor in _environment.Actors)
            {
                var actorNode = new TreeNode(actor.Name)
                {
                    Tag              = new ObjectNodeTag(actor, ObjectKind.Actor),
                    ImageKey         = "actor",
                    SelectedImageKey = "actor",
                    ToolTipText      = $"Actor: {actor.Name}\nFolder: {actor.FolderPath}"
                };

                // Show RBA props as child info nodes
                if (!string.IsNullOrWhiteSpace(actor.RolePrompt))
                    actorNode.Nodes.Add(PropNode("Role",     TruncateLine(actor.RolePrompt,     60)));
                if (!string.IsNullOrWhiteSpace(actor.CriteriaPrompt))
                    actorNode.Nodes.Add(PropNode("Criteria", TruncateLine(actor.CriteriaPrompt, 60)));
                if (!string.IsNullOrWhiteSpace(actor.IntentPrompt))
                    actorNode.Nodes.Add(PropNode("Intent",   TruncateLine(actor.IntentPrompt,   60)));

                actorsRoot.Nodes.Add(actorNode);
            }
            _tree.Nodes.Add(actorsRoot);

            // ?? Loops ?????????????????????????????????????????????????????????
            var loopsRoot = CategoryNode($"\u267B  Loops  [{_environment.Loops.Count}]");
            foreach (var loop in _environment.Loops)
            {
                var loopNode = new TreeNode(loop.Name)
                {
                    Tag              = new ObjectNodeTag(loop, ObjectKind.Loop),
                    ImageKey         = "loop",
                    SelectedImageKey = "loop",
                    ToolTipText      = string.IsNullOrEmpty(loop.Description)
                        ? $"Loop: {loop.Name}"
                        : $"Loop: {loop.Name}\n{loop.Description}"
                };

                if (!string.IsNullOrWhiteSpace(loop.Description))
                    loopNode.Nodes.Add(PropNode("Description", loop.Description));

                if (loop.HasSteps)
                {
                    int i = 1;
                    foreach (var step in loop.Steps)
                    {
                        var stepNode = new TreeNode($"{i++}. {step.Name}  \u2192  {step.ActorName}")
                        {
                            Tag              = new ObjectNodeTag(step, ObjectKind.LoopStep),
                            ImageKey         = "actor",
                            SelectedImageKey = "actor",
                            ForeColor        = WallyTheme.TextSecondary,
                            ToolTipText      = $"Actor: {step.ActorName}"
                        };
                        loopNode.Nodes.Add(stepNode);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(loop.ActorName))
                {
                    loopNode.Nodes.Add(PropNode("Actor", loop.ActorName));
                }

                loopsRoot.Nodes.Add(loopNode);
            }
            _tree.Nodes.Add(loopsRoot);

            // ?? LLM Wrappers ??????????????????????????????????????????????????
            var wrappersRoot = CategoryNode($"\u2699  LLM Wrappers  [{ws.LlmWrappers.Count}]");
            foreach (var wrapper in ws.LlmWrappers)
            {
                bool isDefault = string.Equals(wrapper.Name, ws.Config.DefaultWrapper,
                    StringComparison.OrdinalIgnoreCase);

                string label = isDefault ? $"{wrapper.Name}  \u2605" : wrapper.Name;
                var wNode = new TreeNode(label)
                {
                    Tag              = new ObjectNodeTag(wrapper, ObjectKind.Wrapper),
                    ImageKey         = "wrapper",
                    SelectedImageKey = "wrapper",
                    ToolTipText      = isDefault ? $"{wrapper.Name}  (default)" : wrapper.Name
                };

                if (!string.IsNullOrWhiteSpace(wrapper.Executable))
                    wNode.Nodes.Add(PropNode("Executable", wrapper.Executable));
                if (!string.IsNullOrWhiteSpace(wrapper.Description))
                    wNode.Nodes.Add(PropNode("Description", TruncateLine(wrapper.Description, 70)));
                if (wrapper.CanMakeChanges)
                    wNode.Nodes.Add(PropNode("Mode", "Agentic (can make changes)"));

                wrappersRoot.Nodes.Add(wNode);
            }
            _tree.Nodes.Add(wrappersRoot);

            // ?? Runbooks ??????????????????????????????????????????????????????
            var runbooksRoot = CategoryNode($"\uD83D\uDCDC  Runbooks  [{_environment.Runbooks.Count}]");
            foreach (var rb in _environment.Runbooks)
            {
                var rbNode = new TreeNode(rb.Name)
                {
                    Tag              = new ObjectNodeTag(rb, ObjectKind.Runbook),
                    ImageKey         = "runbook",
                    SelectedImageKey = "runbook",
                    ToolTipText      = $"Runbook: {rb.Name}\nCommands: {rb.Commands?.Count ?? 0}"
                };

                if (!string.IsNullOrEmpty(rb.Description))
                    rbNode.Nodes.Add(PropNode("Description", rb.Description));

                if (rb.Commands != null)
                {
                    int i = 1;
                    foreach (var cmd in rb.Commands)
                    {
                        rbNode.Nodes.Add(new TreeNode($"{i++}. {cmd}")
                        {
                            ImageKey         = "prop",
                            SelectedImageKey = "prop",
                            ForeColor        = WallyTheme.TextMuted
                        });
                    }
                }

                runbooksRoot.Nodes.Add(rbNode);
            }
            _tree.Nodes.Add(runbooksRoot);

            // Expand category roots
            foreach (TreeNode n in _tree.Nodes)
                n.Expand();

            _tree.EndUpdate();
        }

        private static TreeNode CategoryNode(string text) =>
            new TreeNode(text)
            {
                ImageKey         = "category",
                SelectedImageKey = "category",
                ForeColor        = WallyTheme.TextMuted
            };

        private static TreeNode PropNode(string key, string value) =>
            new TreeNode($"{key}: {value}")
            {
                ImageKey         = "prop",
                SelectedImageKey = "prop",
                ForeColor        = WallyTheme.TextMuted
            };

        private static string TruncateLine(string s, int max)
        {
            string first = s.Split('\n')[0].Trim();
            return first.Length > max ? first[..max] + "\u2026" : first;
        }

        // ?? Activation ????????????????????????????????????????????????????????

        private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is not ObjectNodeTag t) return;
            ActivateTag(t);
        }

        private void ActivateSelected()
        {
            if (_tree.SelectedNode?.Tag is ObjectNodeTag t)
                ActivateTag(t);
        }

        private void ActivateTag(ObjectNodeTag t)
        {
            switch (t.Kind)
            {
                case ObjectKind.Actor   when t.Item is Actor a:
                    ActorActivated?.Invoke(this, a.Name); break;
                case ObjectKind.Loop    when t.Item is WallyLoopDefinition l:
                    LoopActivated?.Invoke(this, l.Name); break;
                case ObjectKind.Wrapper when t.Item is LLMWrapper w:
                    WrapperActivated?.Invoke(this, w.Name); break;
                case ObjectKind.Runbook when t.Item is WallyRunbook r:
                    RunbookActivated?.Invoke(this, r.Name); break;
            }
        }

        private void CopySelectedName()
        {
            if (_tree.SelectedNode == null) return;
            string text = _tree.SelectedNode.Text.Split('\u2605')[0].Trim();
            Clipboard.SetText(text);
        }

        // ?? Context menu ??????????????????????????????????????????????????????

        private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_tree.SelectedNode?.Tag is not ObjectNodeTag t ||
                t.Kind is ObjectKind.LoopStep or ObjectKind.ActorProp)
            { e.Cancel = true; return; }
        }

        // ?? Custom draw ???????????????????????????????????????????????????????

        private void OnDrawNode(object? sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null || e.Bounds.IsEmpty) return;

            var g = e.Graphics;
            var tv = e.Node.TreeView!;
            bool selected = (e.State & TreeNodeStates.Selected) != 0;

            // ?? Full-width background ??????????????????????????????????
            var rowRect = new Rectangle(0, e.Bounds.Y, tv.ClientSize.Width, e.Bounds.Height);
            Color bg = selected ? WallyTheme.Surface3 : WallyTheme.Surface1;
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, rowRect);

            // ?? Left accent bar on selected ????????????????????????????
            if (selected)
            {
                using var accent = new SolidBrush(WallyTheme.Accent);
                g.FillRectangle(accent, 0, e.Bounds.Y, 2, e.Bounds.Height);
            }

            // ?? Expand / collapse glyph ????????????????????????????????
            int indent = e.Node.Level * tv.Indent + 4;
            if (e.Node.Nodes.Count > 0)
            {
                int glyphX = indent;
                int glyphY = e.Bounds.Y + (e.Bounds.Height - 8) / 2;
                using var glyphPen = new Pen(WallyTheme.TextMuted, 1.5f);
                if (e.Node.IsExpanded)
                {
                    g.DrawLine(glyphPen, glyphX, glyphY + 2, glyphX + 4, glyphY + 6);
                    g.DrawLine(glyphPen, glyphX + 4, glyphY + 6, glyphX + 8, glyphY + 2);
                }
                else
                {
                    g.DrawLine(glyphPen, glyphX + 2, glyphY, glyphX + 6, glyphY + 4);
                    g.DrawLine(glyphPen, glyphX + 6, glyphY + 4, glyphX + 2, glyphY + 8);
                }
            }

            int iconX = indent + 14;

            // ?? Icon from ImageList ????????????????????????????????????
            if (tv.ImageList != null)
            {
                string key = selected
                    ? (e.Node.SelectedImageKey ?? e.Node.ImageKey ?? "")
                    : (e.Node.ImageKey ?? "");
                int imgIdx = string.IsNullOrEmpty(key) ? -1 : tv.ImageList.Images.IndexOfKey(key);
                if (imgIdx >= 0)
                {
                    int imgY = e.Bounds.Y + (e.Bounds.Height - tv.ImageList.ImageSize.Height) / 2;
                    tv.ImageList.Draw(g, iconX, imgY, imgIdx);
                }
            }

            int textX = iconX + 20;

            // ?? Text ???????????????????????????????????????????????????
            Color fg = e.Node.ForeColor != Color.Empty && e.Node.ForeColor != Color.Black
                ? e.Node.ForeColor
                : (selected ? WallyTheme.TextPrimary : WallyTheme.TextSecondary);
            var textRect = new Rectangle(textX, e.Bounds.Y, tv.ClientSize.Width - textX, e.Bounds.Height);
            TextRenderer.DrawText(g, e.Node.Text, tv.Font,
                textRect, fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        // ?? Icon drawing ??????????????????????????????????????????????????????

        private static Bitmap DrawCategoryIcon()
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(WallyTheme.TextMuted);
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
            using var i = new SolidBrush(WallyTheme.Surface1);
            g.FillEllipse(i, 5, 5, 6, 6);
            return bmp;
        }

        private static Bitmap DrawCycleIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = new System.Drawing.Pen(c, 2f);
            g.DrawArc(p, 2, 2, 12, 12, -30, 270);
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
            using var i = new SolidBrush(WallyTheme.Surface1);
            g.FillEllipse(i, 5, 5, 6, 6);
            for (int t = 0; t < 4; t++)
            {
                float angle = t * 45f * (float)(Math.PI / 180.0);
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
            using var b = new SolidBrush(c);
            g.FillRectangle(b, 3, 1, 10, 14);
            using var line = new SolidBrush(WallyTheme.Surface1);
            for (int y = 4; y <= 10; y += 3)
                g.FillRectangle(line, 5, y, 6, 1);
            return bmp;
        }

        private static Bitmap DrawDotIcon(Color c)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(c);
            g.FillEllipse(b, 5, 5, 6, 6);
            return bmp;
        }
    }
}
