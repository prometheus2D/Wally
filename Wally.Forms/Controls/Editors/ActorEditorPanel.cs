using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    /// <summary>
    /// Editor panel for viewing and editing an Actor's RBA definition
    /// (Role, Acceptance Criteria, Intent) and metadata.
    /// </summary>
    public sealed class ActorEditorPanel : UserControl
    {
        private readonly TextBox _txtName;
        private readonly RichTextBox _txtRolePrompt;
        private readonly RichTextBox _txtCriteriaPrompt;
        private readonly RichTextBox _txtIntentPrompt;
        private readonly Label _lblStatus;
        private readonly Button _btnSave;
        private readonly Button _btnRevert;

        private Actor? _actor;
        private WallyEnvironment? _environment;
        private bool _isDirty;

        /// <summary>Raised when the editor content has been modified.</summary>
        public event EventHandler? DirtyChanged;

        /// <summary>Raised after a successful save.</summary>
        public event EventHandler? Saved;

        public bool IsDirty => _isDirty;
        public Actor? Actor => _actor;

        public ActorEditorPanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            // Scrollable wrapper so the table can exceed the visible area.
            var scroll = ThemedEditorFactory.CreateScrollableSurface();

            var table = ThemedEditorFactory.CreateScrollableFormTable(1);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            int row = 0;

            // Header
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("\U0001F3AD Actor Editor", WallyTheme.FontUIBold, WallyTheme.TextPrimary), 0, row++);

            // Action bar
            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 8)
            };

            _btnSave = CreateButton("\uD83D\uDCBE Save");
            _btnSave.Click += OnSave;
            _btnRevert = CreateButton("\u21BA Revert");
            _btnRevert.Click += OnRevert;

            _lblStatus = new Label
            {
                Text = "",
                AutoSize = true,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 6, 0, 0)
            };

            actionBar.Controls.Add(_btnSave);
            actionBar.Controls.Add(_btnRevert);
            actionBar.Controls.Add(_lblStatus);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(actionBar, 0, row++);

            // Actor name
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateFieldLabel("Actor Name"), 0, row++);
            _txtName = CreateTextBox();
            _txtName.ReadOnly = true;
            _txtName.BackColor = WallyTheme.Surface1;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtName, 0, row++);

            // Role
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("\u2694 Role", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);
            _txtRolePrompt = CreateRichTextBox(160);
            _txtRolePrompt.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtRolePrompt, 0, row++);

            // Acceptance Criteria
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("\u2705 Acceptance Criteria", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);
            _txtCriteriaPrompt = CreateRichTextBox(160);
            _txtCriteriaPrompt.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtCriteriaPrompt, 0, row++);

            // Intent
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(CreateSectionLabel("\uD83C\uDFAF Intent", WallyTheme.FontUIBold, WallyTheme.TextSecondary), 0, row++);
            _txtIntentPrompt = CreateRichTextBox(160);
            _txtIntentPrompt.TextChanged += OnFieldChanged;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(_txtIntentPrompt, 0, row++);

            scroll.Controls.Add(table);
            Controls.Add(scroll);
            ResumeLayout(true);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment env) => _environment = env;

        public void LoadActor(Actor actor)
        {
            _actor = actor;
            PopulateFields();
            SetDirty(false);
            _lblStatus.Text = $"Loaded from: {actor.FolderPath}";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        // ?? Field population ????????????????????????????????????????????????

        private void PopulateFields()
        {
            if (_actor == null) return;

            _txtName.Text          = _actor.Name;
            _txtRolePrompt.Text    = _actor.RolePrompt;
            _txtCriteriaPrompt.Text = _actor.CriteriaPrompt;
            _txtIntentPrompt.Text  = _actor.IntentPrompt;
        }

        private void ApplyFieldsToActor()
        {
            if (_actor == null) return;

            _actor.RolePrompt     = _txtRolePrompt.Text.Trim();
            _actor.CriteriaPrompt = _txtCriteriaPrompt.Text.Trim();
            _actor.IntentPrompt   = _txtIntentPrompt.Text.Trim();
        }

        // ?? Event handlers ??????????????????????????????????????????????????

        private void OnFieldChanged(object? sender, EventArgs e)
        {
            if (_actor == null) return;
            SetDirty(true);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_actor == null || _environment == null) return;

            try
            {
                ApplyFieldsToActor();

                // Save the actor.json to disk
                WallyHelper.SaveActor(_environment.WorkspaceFolder!, _environment.Workspace!.Config, _actor);

                SetDirty(false);
                _lblStatus.Text = $"Saved at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
                Saved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Save failed: {ex.Message}";
                _lblStatus.ForeColor = WallyTheme.Red;
            }
        }

        private void OnRevert(object? sender, EventArgs e)
        {
            if (_actor == null) return;
            PopulateFields();
            SetDirty(false);
            _lblStatus.Text = "Reverted to last saved state.";
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        private void SetDirty(bool dirty)
        {
            _isDirty = dirty;
            _btnSave.Enabled = dirty;
            _btnRevert.Enabled = dirty;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        // ?? Control factories ???????????????????????????????????????????????

        private static Label CreateSectionLabel(string text, Font font, Color color)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = font,
                ForeColor = color,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 4),
                Dock = DockStyle.Top
            };
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = WallyTheme.FontUISmallBold,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 2),
                Dock = DockStyle.Top
            };
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                Font = WallyTheme.FontUI,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 4)
            };
        }

        private static RichTextBox CreateRichTextBox(int height)
        {
            return ThemedEditorFactory.CreateFormTextArea(
                height,
                wordWrap: true,
                backColor: WallyTheme.Surface2);
        }

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
