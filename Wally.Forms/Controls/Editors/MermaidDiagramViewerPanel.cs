using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Controls;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls.Editors
{
    public sealed class MermaidDiagramViewerPanel : UserControl
    {
        private readonly Label _lblTitle;
        private readonly Label _lblStatus;
        private readonly Button _btnRefresh;
        private readonly Button _btnZoomOut;
        private readonly Button _btnZoomIn;
        private readonly Button _btnFit;
        private readonly Button _btnActualSize;
        private readonly Button _btnSaveAs;
        private readonly Button _btnOpenFolder;
        private readonly Panel _viewport;
        private readonly Panel _canvas;
        private readonly PictureBox _pictureBox;
        private readonly RichTextBox _txtDetails;

        private WallyEnvironment? _environment;
        private Func<MermaidDiagramDefinition>? _definitionFactory;
        private MermaidDiagramDefinition? _currentDefinition;
        private MermaidDiagramRenderResult? _lastResult;
        private Image? _currentImage;
        private float _zoomFactor = 1f;
        private bool _fitToWindow;

        public MermaidDiagramViewerPanel()
        {
            SuspendLayout();

            Dock = DockStyle.Fill;
            BackColor = WallyTheme.Surface0;

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                BackColor = WallyTheme.Surface0,
                Padding = new Padding(20, 12, 20, 8)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var titlePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };

            _lblTitle = new Label
            {
                Text = "Mermaid Diagram",
                AutoSize = true,
                Font = WallyTheme.FontUIBold,
                ForeColor = WallyTheme.TextPrimary,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            _lblStatus = new Label
            {
                Text = "Ready",
                AutoSize = true,
                Font = WallyTheme.FontUISmall,
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };
            titlePanel.Controls.Add(_lblTitle);
            titlePanel.Controls.Add(_lblStatus);

            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };

            _btnRefresh = CreateButton("\u21BB Refresh");
            _btnRefresh.Click += (_, _) => GenerateDiagram();
            _btnZoomOut = CreateButton("- Zoom");
            _btnZoomOut.Click += (_, _) => SetZoom(_zoomFactor / 1.2f, fitToWindow: false);
            _btnZoomIn = CreateButton("+ Zoom");
            _btnZoomIn.Click += (_, _) => SetZoom(_zoomFactor * 1.2f, fitToWindow: false);
            _btnFit = CreateButton("Fit");
            _btnFit.Click += (_, _) => ApplyFitZoom();
            _btnActualSize = CreateButton("100%");
            _btnActualSize.Click += (_, _) => SetZoom(1f, fitToWindow: false);
            _btnSaveAs = CreateButton("\uD83D\uDCBE Save As");
            _btnSaveAs.Click += OnSaveAs;
            _btnOpenFolder = CreateButton("\uD83D\uDCC1 Open Folder");
            _btnOpenFolder.Click += OnOpenFolder;

            actionBar.Controls.Add(_btnRefresh);
            actionBar.Controls.Add(_btnZoomOut);
            actionBar.Controls.Add(_btnZoomIn);
            actionBar.Controls.Add(_btnFit);
            actionBar.Controls.Add(_btnActualSize);
            actionBar.Controls.Add(_btnSaveAs);
            actionBar.Controls.Add(_btnOpenFolder);

            header.Controls.Add(titlePanel, 0, 0);
            header.Controls.Add(actionBar, 1, 0);

            _viewport = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = WallyTheme.Surface1,
                Padding = new Padding(16)
            };
            _viewport.Resize += (_, _) =>
            {
                if (_fitToWindow)
                    ApplyFitZoom();
            };

            _canvas = new Panel
            {
                BackColor = Color.White,
                Size = new Size(320, 240)
            };
            _viewport.Controls.Add(_canvas);

            _pictureBox = new PictureBox
            {
                Location = Point.Empty,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.White
            };
            _canvas.Controls.Add(_pictureBox);

            _txtDetails = ThemedEditorFactory.CreateDocumentViewer(wordWrap: false, readOnly: true, backColor: WallyTheme.Surface1, dock: DockStyle.Bottom);
            _txtDetails.Height = 110;
            _txtDetails.BorderStyle = BorderStyle.FixedSingle;

            Controls.Add(_viewport);
            Controls.Add(_txtDetails);
            Controls.Add(header);

            ResumeLayout(true);
        }

        public void Configure(WallyEnvironment environment, Func<MermaidDiagramDefinition> definitionFactory)
        {
            _environment = environment;
            _definitionFactory = definitionFactory;
        }

        public void GenerateDiagram()
        {
            if (_environment?.HasWorkspace != true || _definitionFactory == null)
            {
                ShowError("No workspace is loaded or no diagram target is configured.", null);
                return;
            }

            try
            {
                UseWaitCursor = true;
                _currentDefinition = _definitionFactory();
                _lblTitle.Text = _currentDefinition.Title;
                _lastResult = MermaidDiagramService.Render(_environment, _currentDefinition, "png");
                LoadImage(_lastResult.OutputPath);
                SetZoom(_fitToWindow ? CalculateFitZoom() : _zoomFactor, _fitToWindow);
                _lblStatus.Text = $"Rendered {Path.GetFileName(_lastResult.OutputPath)} at {DateTime.Now:HH:mm:ss}";
                _lblStatus.ForeColor = WallyTheme.Green;
                _txtDetails.Text =
                    $"Output:  {_lastResult.OutputPath}{Environment.NewLine}" +
                    $"Source:  {_lastResult.MermaidFilePath}{Environment.NewLine}" +
                    $"Command: {_lastResult.CommandDisplay}{Environment.NewLine}{Environment.NewLine}" +
                    _currentDefinition.MermaidSource;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, _currentDefinition?.MermaidSource);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentImage?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void LoadImage(string imagePath)
        {
            _currentImage?.Dispose();

            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(stream);
            _currentImage = new Bitmap(image);
            _pictureBox.Image = _currentImage;
        }

        private void SetZoom(float requestedZoom, bool fitToWindow)
        {
            if (_currentImage == null)
                return;

            _fitToWindow = fitToWindow;
            _zoomFactor = Math.Max(0.1f, Math.Min(requestedZoom, 6f));

            int width = Math.Max(1, (int)Math.Round(_currentImage.Width * _zoomFactor));
            int height = Math.Max(1, (int)Math.Round(_currentImage.Height * _zoomFactor));
            _pictureBox.Size = new Size(width, height);
            _canvas.Size = new Size(width, height);
            _lblStatus.Text = $"Zoom: {_zoomFactor:P0}" + (_lastResult != null ? $"   File: {Path.GetFileName(_lastResult.OutputPath)}" : string.Empty);
            _lblStatus.ForeColor = WallyTheme.TextMuted;
        }

        private void ApplyFitZoom()
        {
            if (_currentImage == null)
                return;

            SetZoom(CalculateFitZoom(), fitToWindow: true);
        }

        private float CalculateFitZoom()
        {
            if (_currentImage == null)
                return 1f;

            int availableWidth = Math.Max(1, _viewport.ClientSize.Width - _viewport.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
            int availableHeight = Math.Max(1, _viewport.ClientSize.Height - _viewport.Padding.Vertical - SystemInformation.HorizontalScrollBarHeight);
            float widthRatio = (float)availableWidth / _currentImage.Width;
            float heightRatio = (float)availableHeight / _currentImage.Height;
            return Math.Max(0.1f, Math.Min(widthRatio, heightRatio));
        }

        private void OnSaveAs(object? sender, EventArgs e)
        {
            if (_lastResult == null || !File.Exists(_lastResult.OutputPath))
                return;

            using var dialog = new SaveFileDialog
            {
                Title = "Save Diagram As",
                FileName = Path.GetFileName(_lastResult.OutputPath),
                Filter = "PNG Image (*.png)|*.png|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            File.Copy(_lastResult.OutputPath, dialog.FileName, overwrite: true);
            _lblStatus.Text = $"Saved copy to {dialog.FileName}";
            _lblStatus.ForeColor = WallyTheme.Green;
        }

        private void OnOpenFolder(object? sender, EventArgs e)
        {
            if (_lastResult == null || !File.Exists(_lastResult.OutputPath))
                return;

            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{_lastResult.OutputPath}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void ShowError(string message, string? mermaidSource)
        {
            _pictureBox.Image = null;
            _currentImage?.Dispose();
            _currentImage = null;
            _canvas.Size = new Size(320, 240);
            _pictureBox.Size = Size.Empty;
            _lblStatus.Text = "Diagram render failed";
            _lblStatus.ForeColor = WallyTheme.Red;
            _txtDetails.Text = mermaidSource == null
                ? message
                : message + Environment.NewLine + Environment.NewLine + mermaidSource;
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