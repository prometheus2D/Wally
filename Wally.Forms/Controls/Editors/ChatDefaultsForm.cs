using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Providers;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Modal form for managing all chat-related workspace defaults in one place.
    /// <para>
    /// Covers all four phases from the Chat Defaults Manager proposal:
    /// <list type="bullet">
    ///   <item><b>Tab 1 — Defaults</b>: Available Models, Selected Models (priority-ordered),
    ///         Default Loop, Default Actor, Ask Wrapper, Agent Wrapper, Max Iterations.</item>
    ///   <item><b>Tab 2 — Advanced</b>: Read-only display of resolved runtime values so the
    ///         user can confirm exactly what each session will use.</item>
    /// </list>
    /// On <b>Save</b>: writes <c>wally-config.json</c>, re-runs
    /// <c>ResolveSelectedDefaults</c>, and calls the supplied
    /// <paramref name="onSaved"/> callback so <c>ChatPanel</c> can refresh
    /// its dropdowns without a full workspace reload.
    /// </para>
    /// </summary>
    internal sealed class ChatDefaultsForm : Form
    {
        // ?? Dependencies ??????????????????????????????????????????????????????
        private readonly WallyEnvironment _env;
        private readonly Action           _onSaved;

        // ?? Tab 1 controls ????????????????????????????????????????????????????
        private readonly ListBox          _lstAvailableModels;
        private readonly ListBox          _lstSelectedModels;
        private readonly Button           _btnAddModel;
        private readonly Button           _btnRemoveAvailable;
        private readonly Button           _btnMoveSelectedUp;
        private readonly Button           _btnMoveSelectedDown;
        private readonly Button           _btnRemoveSelected;
        private readonly Button           _btnModelToSelected;
        private readonly TextBox          _txtNewModel;

        private readonly ComboBox         _cboDefaultLoop;
        private readonly ComboBox         _cboDefaultActor;
        private readonly ComboBox         _cboAskWrapper;
        private readonly ComboBox         _cboAgentWrapper;
        private readonly NumericUpDown    _nudMaxIterations;

        // ?? Tab 2 controls ????????????????????????????????????????????????????
        private readonly Label            _lblResolvedModel;
        private readonly Label            _lblResolvedWrapper;
        private readonly Label            _lblResolvedLoop;
        private readonly Label            _lblResolvedRunbook;
        private readonly Label            _lblResolvedActor;

        // ?? Buttons ???????????????????????????????????????????????????????????
        private readonly Button           _btnSave;
        private readonly Button           _btnCancel;

        // ?? Constructor ???????????????????????????????????????????????????????

        /// <param name="env">The loaded workspace environment.</param>
        /// <param name="onSaved">
        /// Called after a successful save so the caller can refresh ChatPanel
        /// dropdowns. Executed on the UI thread.
        /// </param>
        public ChatDefaultsForm(WallyEnvironment env, Action onSaved)
        {
            _env     = env ?? throw new ArgumentNullException(nameof(env));
            _onSaved = onSaved ?? throw new ArgumentNullException(nameof(onSaved));

            // ?? Form properties ???????????????????????????????????????????????
            Text            = "Chat Defaults";
            Size            = new Size(700, 560);
            MinimumSize     = new Size(600, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = false;
            BackColor       = WallyTheme.Surface1;
            ForeColor       = WallyTheme.TextPrimary;
            Font            = WallyTheme.FontUI;

            // ?? Tab 1 — Available / Selected Models ???????????????????????????
            _lstAvailableModels = CreateListBox();
            _lstSelectedModels  = CreateListBox();

            _txtNewModel = new TextBox
            {
                PlaceholderText = "Model name (e.g. gpt-4o)",
                BackColor       = WallyTheme.Surface2,
                ForeColor       = WallyTheme.TextPrimary,
                BorderStyle     = BorderStyle.FixedSingle,
                Font            = WallyTheme.FontUI,
                Height          = 24
            };

            _btnAddModel         = CreateSmallButton("+ Add");
            _btnRemoveAvailable  = CreateSmallButton("Remove");
            _btnModelToSelected  = CreateSmallButton("? Priority");
            _btnMoveSelectedUp   = CreateSmallButton("? Up");
            _btnMoveSelectedDown = CreateSmallButton("? Down");
            _btnRemoveSelected   = CreateSmallButton("? Remove");

            _btnAddModel.Click        += OnAddModel;
            _btnRemoveAvailable.Click += OnRemoveAvailable;
            _btnModelToSelected.Click += OnAddToSelected;
            _btnMoveSelectedUp.Click   += OnMoveSelectedUp;
            _btnMoveSelectedDown.Click += OnMoveSelectedDown;
            _btnRemoveSelected.Click   += OnRemoveSelected;

            // ?? Tab 1 — Combos / spinners ?????????????????????????????????????
            _cboDefaultLoop    = CreateComboBox();
            _cboDefaultActor   = CreateComboBox();
            _cboAskWrapper     = CreateComboBox();
            _cboAgentWrapper   = CreateComboBox();
            _nudMaxIterations  = new NumericUpDown
            {
                Minimum   = 1, Maximum  = 100, Value = 10,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                Font      = WallyTheme.FontUI,
                Width     = 70
            };

            // ?? Tab 2 — Resolved labels ???????????????????????????????????????
            _lblResolvedModel   = CreateValueLabel();
            _lblResolvedWrapper = CreateValueLabel();
            _lblResolvedLoop    = CreateValueLabel();
            _lblResolvedRunbook = CreateValueLabel();
            _lblResolvedActor   = CreateValueLabel();

            // ?? Save / Cancel ?????????????????????????????????????????????????
            _btnSave = new Button
            {
                Text = "Save", Width = 90, Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface3,
                ForeColor = WallyTheme.TextPrimary,
                Font      = WallyTheme.FontUIBold,
                Anchor    = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.None
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += OnSave;

            _btnCancel = new Button
            {
                Text = "Cancel", Width = 90, Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextSecondary,
                Font      = WallyTheme.FontUI,
                Anchor    = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderSize = 0;

            // ?? Build layout ??????????????????????????????????????????????????
            var tabs = BuildTabs();
            tabs.Dock = DockStyle.Fill;

            var btnRow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                Height        = 46,
                FlowDirection = FlowDirection.RightToLeft,
                Padding       = new Padding(8, 6, 8, 6),
                BackColor     = WallyTheme.Surface2
            };
            btnRow.Controls.Add(_btnCancel);
            btnRow.Controls.Add(_btnSave);

            Controls.Add(tabs);
            Controls.Add(btnRow);

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;

            // ?? Populate ??????????????????????????????????????????????????????
            PopulateAll();
        }

        // ?? Layout helpers ????????????????????????????????????????????????????

        private TabControl BuildTabs()
        {
            var tabs = new TabControl
            {
                Font      = WallyTheme.FontUI,
                BackColor = WallyTheme.Surface1,
                Padding   = new Point(10, 4)
            };

            // Tab 1 — Defaults
            var tab1 = new TabPage("Defaults")
            {
                BackColor = WallyTheme.Surface1,
                ForeColor = WallyTheme.TextPrimary,
                Padding   = new Padding(10)
            };
            tab1.Controls.Add(BuildDefaultsTab());
            tabs.TabPages.Add(tab1);

            // Tab 2 — Advanced
            var tab2 = new TabPage("Advanced (resolved values)")
            {
                BackColor = WallyTheme.Surface1,
                ForeColor = WallyTheme.TextPrimary,
                Padding   = new Padding(10)
            };
            tab2.Controls.Add(BuildAdvancedTab());
            tabs.TabPages.Add(tab2);

            return tabs;
        }

        private Panel BuildDefaultsTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = WallyTheme.Surface1 };

            // ?? Models section ????????????????????????????????????????????????
            int y = 10;

            panel.Controls.Add(SectionLabel("Available Models", 10, y));
            panel.Controls.Add(SectionLabel("Selected Models (top = default)", 290, y));
            y += 20;

            _lstAvailableModels.SetBounds(10, y, 200, 140);
            _lstSelectedModels.SetBounds(290, y, 200, 140);
            panel.Controls.Add(_lstAvailableModels);
            panel.Controls.Add(_lstSelectedModels);

            // Arrow button between the two lists
            _btnModelToSelected.SetBounds(220, y + 60, 60, 26);
            panel.Controls.Add(_btnModelToSelected);

            // Buttons below Available list
            _btnRemoveAvailable.SetBounds(10, y + 146, 70, 24);
            panel.Controls.Add(_btnRemoveAvailable);

            // Buttons beside Selected list
            _btnMoveSelectedUp.SetBounds(500, y, 70, 24);
            _btnMoveSelectedDown.SetBounds(500, y + 30, 70, 24);
            _btnRemoveSelected.SetBounds(500, y + 60, 70, 24);
            panel.Controls.Add(_btnMoveSelectedUp);
            panel.Controls.Add(_btnMoveSelectedDown);
            panel.Controls.Add(_btnRemoveSelected);

            y += 180;

            // New model entry
            panel.Controls.Add(SectionLabel("Add model:", 10, y + 3));
            _txtNewModel.SetBounds(80, y, 210, 24);
            _btnAddModel.SetBounds(300, y, 60, 24);
            panel.Controls.Add(_txtNewModel);
            panel.Controls.Add(_btnAddModel);

            y += 38;

            // ?? Separator ?????????????????????????????????????????????????????
            panel.Controls.Add(HRule(10, y, panel.Width - 20));
            y += 14;

            // ?? Combos row ????????????????????????????????????????????????????
            int col1 = 10, col2 = 200, col3 = 390, col4 = 530;

            panel.Controls.Add(SectionLabel("Default Loop", col1, y));
            panel.Controls.Add(SectionLabel("Default Actor", col2, y));
            panel.Controls.Add(SectionLabel("Ask Wrapper", col3, y));
            panel.Controls.Add(SectionLabel("Agent Wrapper", col4, y));
            y += 18;

            _cboDefaultLoop.SetBounds(col1, y, 175, 24);
            _cboDefaultActor.SetBounds(col2, y, 175, 24);
            _cboAskWrapper.SetBounds(col3, y, 125, 24);
            _cboAgentWrapper.SetBounds(col4, y, 125, 24);
            panel.Controls.Add(_cboDefaultLoop);
            panel.Controls.Add(_cboDefaultActor);
            panel.Controls.Add(_cboAskWrapper);
            panel.Controls.Add(_cboAgentWrapper);

            y += 36;

            // Max iterations
            panel.Controls.Add(SectionLabel("Max Iterations:", col1, y + 4));
            _nudMaxIterations.SetBounds(col1 + 100, y, 70, 24);
            panel.Controls.Add(_nudMaxIterations);

            return panel;
        }

        private Panel BuildAdvancedTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = WallyTheme.Surface1 };

            var note = new Label
            {
                Text      = "These are the runtime values resolved from your Selected* lists. " +
                            "They are computed at workspace load and are never written to disk.",
                AutoSize  = false,
                Dock      = DockStyle.Top,
                Height    = 38,
                ForeColor = WallyTheme.TextMuted,
                Font      = WallyTheme.FontUISmall,
                Padding   = new Padding(8, 6, 8, 0)
            };
            panel.Controls.Add(note);

            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                AutoSize    = true,
                ColumnCount = 2,
                Padding     = new Padding(8, 4, 8, 4)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));

            void Row(string labelText, Label valueLabel)
            {
                grid.Controls.Add(new Label
                {
                    Text      = labelText,
                    AutoSize  = true,
                    ForeColor = WallyTheme.TextSecondary,
                    Font      = WallyTheme.FontUISmallBold,
                    Anchor    = AnchorStyles.Left | AnchorStyles.Top,
                    Margin    = new Padding(0, 8, 0, 0)
                });
                valueLabel.Margin = new Padding(0, 8, 0, 0);
                grid.Controls.Add(valueLabel);
            }

            Row("Resolved Model:",   _lblResolvedModel);
            Row("Resolved Wrapper:", _lblResolvedWrapper);
            Row("Resolved Loop:",    _lblResolvedLoop);
            Row("Resolved Runbook:", _lblResolvedRunbook);
            Row("Resolved Actor:",   _lblResolvedActor);

            panel.Controls.Add(grid);
            return panel;
        }

        // ?? Populate ??????????????????????????????????????????????????????????

        private void PopulateAll()
        {
            var cfg      = _env.Workspace!.Config;
            var ws       = _env.Workspace!;

            // Available / Selected models
            _lstAvailableModels.Items.Clear();
            foreach (var m in cfg.DefaultModels) _lstAvailableModels.Items.Add(m);

            _lstSelectedModels.Items.Clear();
            foreach (var m in cfg.SelectedModels) _lstSelectedModels.Items.Add(m);

            // Max iterations
            _nudMaxIterations.Value = Math.Max(1, Math.Min(100, cfg.MaxIterations));

            // Default Loop
            _cboDefaultLoop.Items.Clear();
            _cboDefaultLoop.Items.Add("(none)");
            foreach (var l in ws.Loops) _cboDefaultLoop.Items.Add(l.Name);
            _cboDefaultLoop.SelectedItem = cfg.SelectedLoops.Count > 0 ? cfg.SelectedLoops[0] : "(none)";
            if (_cboDefaultLoop.SelectedIndex < 0) _cboDefaultLoop.SelectedIndex = 0;

            // Default Actor
            _cboDefaultActor.Items.Clear();
            _cboDefaultActor.Items.Add("(none)");
            foreach (var a in ws.Actors) _cboDefaultActor.Items.Add(a.Name);
            _cboDefaultActor.SelectedItem = cfg.SelectedActors.Count > 0 ? cfg.SelectedActors[0] : "(none)";
            if (_cboDefaultActor.SelectedIndex < 0) _cboDefaultActor.SelectedIndex = 0;

            // Ask / Agent Wrappers (filtered by CanMakeChanges)
            _cboAskWrapper.Items.Clear();
            _cboAskWrapper.Items.Add("(none)");
            _cboAgentWrapper.Items.Clear();
            _cboAgentWrapper.Items.Add("(none)");

            foreach (var w in ws.LlmWrappers)
            {
                if (!w.CanMakeChanges) _cboAskWrapper.Items.Add(w.Name);
                else                   _cboAgentWrapper.Items.Add(w.Name);
            }

            // Resolve which SelectedWrappers entries go to Ask vs Agent
            string? askSelected   = null;
            string? agentSelected = null;
            foreach (var name in cfg.SelectedWrappers)
            {
                var w = ws.LlmWrappers.FirstOrDefault(x =>
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (w == null) continue;
                if (!w.CanMakeChanges && askSelected   == null) askSelected   = w.Name;
                if ( w.CanMakeChanges && agentSelected == null) agentSelected = w.Name;
                if (askSelected != null && agentSelected != null) break;
            }

            _cboAskWrapper.SelectedItem   = askSelected   ?? "(none)";
            _cboAgentWrapper.SelectedItem = agentSelected ?? "(none)";
            if (_cboAskWrapper.SelectedIndex   < 0) _cboAskWrapper.SelectedIndex   = 0;
            if (_cboAgentWrapper.SelectedIndex < 0) _cboAgentWrapper.SelectedIndex = 0;

            // Advanced tab — resolved values
            _lblResolvedModel.Text   = cfg.DefaultModel           ?? "(none)";
            _lblResolvedWrapper.Text = cfg.DefaultWrapper         ?? "(none)";
            _lblResolvedLoop.Text    = cfg.ResolvedDefaultLoop    ?? "(none)";
            _lblResolvedRunbook.Text = cfg.ResolvedDefaultRunbook ?? "(none)";
            _lblResolvedActor.Text   = cfg.DefaultActorName       ?? "(none)";
        }

        // ?? Event handlers ????????????????????????????????????????????????????

        private void OnAddModel(object? sender, EventArgs e)
        {
            string name = _txtNewModel.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!_lstAvailableModels.Items.Contains(name))
                _lstAvailableModels.Items.Add(name);
            _txtNewModel.Clear();
        }

        private void OnRemoveAvailable(object? sender, EventArgs e)
        {
            if (_lstAvailableModels.SelectedItem is string item)
                _lstAvailableModels.Items.Remove(item);
        }

        private void OnAddToSelected(object? sender, EventArgs e)
        {
            if (_lstAvailableModels.SelectedItem is string item &&
                !_lstSelectedModels.Items.Contains(item))
                _lstSelectedModels.Items.Add(item);
        }

        private void OnMoveSelectedUp(object? sender, EventArgs e)   => MoveListItem(_lstSelectedModels, -1);
        private void OnMoveSelectedDown(object? sender, EventArgs e) => MoveListItem(_lstSelectedModels,  1);
        private void OnRemoveSelected(object? sender, EventArgs e)
        {
            if (_lstSelectedModels.SelectedItem is string item)
                _lstSelectedModels.Items.Remove(item);
        }

        private static void MoveListItem(ListBox list, int delta)
        {
            int idx = list.SelectedIndex;
            if (idx < 0) return;
            int target = idx + delta;
            if (target < 0 || target >= list.Items.Count) return;
            object item = list.Items[idx];
            list.Items.RemoveAt(idx);
            list.Items.Insert(target, item);
            list.SelectedIndex = target;
        }

        private void OnSave(object? sender, EventArgs e)
        {
            var cfg = _env.Workspace!.Config;
            var ws  = _env.Workspace!;

            // ?? Collect Available Models ??????????????????????????????????????
            cfg.DefaultModels.Clear();
            foreach (object item in _lstAvailableModels.Items)
                cfg.DefaultModels.Add(item.ToString()!);

            // ?? Collect Selected Models ???????????????????????????????????????
            cfg.SelectedModels.Clear();
            foreach (object item in _lstSelectedModels.Items)
                cfg.SelectedModels.Add(item.ToString()!);

            // ?? Default Loop ??????????????????????????????????????????????????
            cfg.SelectedLoops.Clear();
            string loopSel = _cboDefaultLoop.SelectedItem?.ToString() ?? "(none)";
            if (!string.Equals(loopSel, "(none)", StringComparison.OrdinalIgnoreCase))
                cfg.SelectedLoops.Add(loopSel);

            // ?? Default Actor ?????????????????????????????????????????????????
            cfg.SelectedActors.Clear();
            string actorSel = _cboDefaultActor.SelectedItem?.ToString() ?? "(none)";
            if (!string.Equals(actorSel, "(none)", StringComparison.OrdinalIgnoreCase))
                cfg.SelectedActors.Add(actorSel);

            // ?? Wrappers — rebuild SelectedWrappers preserving non-Ask/Agent entries ??
            var askNames   = ws.LlmWrappers.Where(w => !w.CanMakeChanges).Select(w => w.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var agentNames = ws.LlmWrappers.Where(w =>  w.CanMakeChanges).Select(w => w.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            cfg.SelectedWrappers.RemoveAll(n => askNames.Contains(n) || agentNames.Contains(n));

            string askSel   = _cboAskWrapper.SelectedItem?.ToString()   ?? "(none)";
            string agentSel = _cboAgentWrapper.SelectedItem?.ToString() ?? "(none)";
            if (!string.Equals(askSel,   "(none)", StringComparison.OrdinalIgnoreCase)) cfg.SelectedWrappers.Insert(0, askSel);
            if (!string.Equals(agentSel, "(none)", StringComparison.OrdinalIgnoreCase)) cfg.SelectedWrappers.Add(agentSel);

            // ?? Max iterations ????????????????????????????????????????????????
            cfg.MaxIterations = (int)_nudMaxIterations.Value;

            // ?? Persist to disk ???????????????????????????????????????????????
            string configPath = System.IO.Path.Combine(ws.WorkspaceFolder, WallyHelper.ConfigFileName);
            try
            {
                cfg.SaveToFile(configPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save config:\n{ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ?? Re-resolve runtime defaults ???????????????????????????????????
            cfg.ResolveSelectedDefaults(
                ws.LlmWrappers.Select(w => w.Name),
                ws.Loops.Select(l => l.Name),
                ws.Runbooks.Select(r => r.Name),
                ws.Actors.Select(a => a.Name));

            // ?? Refresh advanced tab labels ???????????????????????????????????
            _lblResolvedModel.Text   = cfg.DefaultModel ?? "(none)";
            _lblResolvedWrapper.Text = cfg.DefaultWrapper         ?? "(none)";
            _lblResolvedLoop.Text    = cfg.ResolvedDefaultLoop    ?? "(none)";
            _lblResolvedRunbook.Text = cfg.ResolvedDefaultRunbook ?? "(none)";
            _lblResolvedActor.Text   = cfg.DefaultActorName       ?? "(none)";

            // ?? Notify caller to refresh ChatPanel dropdowns ??????????????????
            _onSaved();

            DialogResult = DialogResult.OK;
            Close();
        }

        // ?? Factory helpers ???????????????????????????????????????????????????

        private static ListBox CreateListBox() => new ListBox
        {
            BackColor     = WallyTheme.Surface2,
            ForeColor     = WallyTheme.TextPrimary,
            BorderStyle   = BorderStyle.FixedSingle,
            Font          = WallyTheme.FontUISmall,
            IntegralHeight = false
        };

        private static ComboBox CreateComboBox() => new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor     = WallyTheme.Surface2,
            ForeColor     = WallyTheme.TextPrimary,
            FlatStyle     = FlatStyle.Flat,
            Font          = WallyTheme.FontUI
        };

        private static Button CreateSmallButton(string text) => new Button
        {
            Text      = text,
            Height    = 24,
            Width     = 70,
            FlatStyle = FlatStyle.Flat,
            BackColor = WallyTheme.Surface3,
            ForeColor = WallyTheme.TextPrimary,
            Font      = WallyTheme.FontUISmall
        };

        private static Label SectionLabel(string text, int x, int y) => new Label
        {
            Text      = text,
            AutoSize  = true,
            Location  = new Point(x, y),
            ForeColor = WallyTheme.TextMuted,
            Font      = WallyTheme.FontUISmallBold
        };

        private static Label CreateValueLabel() => new Label
        {
            AutoSize  = true,
            ForeColor = WallyTheme.TextPrimary,
            Font      = WallyTheme.FontUI
        };

        private static Panel HRule(int x, int y, int width)
        {
            return new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(width, 1),
                BackColor = WallyTheme.Border
            };
        }
    }
}
