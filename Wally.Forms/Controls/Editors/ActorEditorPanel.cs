using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Wally.Core;
using Wally.Core.Actors;
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
        private readonly TextBox _txtRoleName;
        private readonly RichTextBox _txtRolePrompt;
        private readonly TextBox _txtCriteriaName;
        private readonly RichTextBox _txtCriteriaPrompt;
        private readonly TextBox _txtIntentName;
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
            AutoScroll = true;
            Padding = new Padding(20);

            var mainPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                AutoScroll = true,
                BackColor = WallyTheme.Surface0,
                Padding = new Padding(0)
            };

            // Header
            mainPanel.Controls.Add(CreateSectionLabel("\U0001F3AD Actor Editor", WallyTheme.FontUIBold, WallyTheme.TextPrimary));
            mainPanel.Controls.Add(CreateSpacer(8));

            // Status bar
            var actionBar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
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
            mainPanel.Controls.Add(actionBar);
            mainPanel.Controls.Add(CreateSpacer(4));

            // Actor name
            mainPanel.Controls.Add(CreateFieldLabel("Actor Name"));
            _txtName = CreateTextBox();
            _txtName.ReadOnly = true;
            _txtName.BackColor = WallyTheme.Surface1;
            mainPanel.Controls.Add(_txtName);
            mainPanel.Controls.Add(CreateSpacer(12));

            // Role
            mainPanel.Controls.Add(CreateSectionLabel("\u2694 Role", WallyTheme.FontUIBold, WallyTheme.TextSecondary));
            mainPanel.Controls.Add(CreateFieldLabel("Name"));
            _txtRoleName = CreateTextBox();
            _txtRoleName.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(_txtRoleName);

            mainPanel.Controls.Add(CreateFieldLabel("Prompt"));
            _txtRolePrompt = CreateRichTextBox(80);
            _txtRolePrompt.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(_txtRolePrompt);
            mainPanel.Controls.Add(CreateSpacer(12));

            // Acceptance Criteria
            mainPanel.Controls.Add(CreateSectionLabel("\u2705 Acceptance Criteria", WallyTheme.FontUIBold, WallyTheme.TextSecondary));
            mainPanel.Controls.Add(CreateFieldLabel("Name"));
            _txtCriteriaName = CreateTextBox();
            _txtCriteriaName.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(_txtCriteriaName);

            mainPanel.Controls.Add(CreateFieldLabel("Prompt"));
            _txtCriteriaPrompt = CreateRichTextBox(80);
            _txtCriteriaPrompt.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(_txtCriteriaPrompt);
            mainPanel.Controls.Add(CreateSpacer(12));

            // Intent
            mainPanel.Controls.Add(CreateSectionLabel("\uD83C\uDFAF Intent", WallyTheme.FontUIBold, WallyTheme.TextSecondary));
            mainPanel.Controls.Add(CreateFieldLabel("Name"));
            _txtIntentName = CreateTextBox();
            _txtIntentName.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(_txtIntentName);

            mainPanel.Controls.Add(CreateFieldLabel("Prompt"));
            _txtIntentPrompt = CreateRichTextBox(80);
            _txtIntentPrompt.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(_txtIntentPrompt);

            Controls.Add(mainPanel);
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

            _txtName.Text = _actor.Name;
            _txtRoleName.Text = _actor.Role.Name;
            _txtRolePrompt.Text = _actor.Role.Prompt;
            _txtCriteriaName.Text = _actor.AcceptanceCriteria.Name;
            _txtCriteriaPrompt.Text = _actor.AcceptanceCriteria.Prompt;
            _txtIntentName.Text = _actor.Intent.Name;
            _txtIntentPrompt.Text = _actor.Intent.Prompt;
        }

        private void ApplyFieldsToActor()
        {
            if (_actor == null) return;

            _actor.Role.Name = _txtRoleName.Text.Trim();
            _actor.Role.Prompt = _txtRolePrompt.Text.Trim();
            _actor.AcceptanceCriteria.Name = _txtCriteriaName.Text.Trim();
            _actor.AcceptanceCriteria.Prompt = _txtCriteriaPrompt.Text.Trim();
            _actor.Intent.Name = _txtIntentName.Text.Trim();
            _actor.Intent.Prompt = _txtIntentPrompt.Text.Trim();
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
                Margin = new Padding(0, 0, 0, 4)
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
                Margin = new Padding(0, 0, 0, 2)
            };
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Width = 500,
                Font = WallyTheme.FontUI,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 4)
            };
        }

        private static RichTextBox CreateRichTextBox(int height)
        {
            return new RichTextBox
            {
                Width = 500,
                Height = height,
                Font = WallyTheme.FontMono,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Margin = new Padding(0, 0, 0, 4)
            };
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

        private static Panel CreateSpacer(int height)
        {
            return new Panel
            {
                Height = height,
                Width = 500,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
        }
    }
}
