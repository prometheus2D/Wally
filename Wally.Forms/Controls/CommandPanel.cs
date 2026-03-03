using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wally.Core;
using Wally.Forms.Theme;

namespace Wally.Forms.Controls
{
    /// <summary>
    /// Command-line interface panel — bottom terminal window.
    /// Accepts Wally commands, displays coloured output, supports command history,
    /// tab-completion, and captures <see cref="Console"/> output from the Core layer.
    /// </summary>
    public sealed class CommandPanel : UserControl
    {
        // ?? Controls ????????????????????????????????????????????????????????

        private readonly Panel _header;
        private readonly Label _lblTitle;
        private readonly RichTextBox _output;
        private readonly Panel _inputRow;
        private readonly Panel _separator;
        private readonly Panel _inputBorder;
        private readonly Label _lblPrompt;
        private readonly TextBox _txtInput;

        // ?? State ???????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;
        private readonly List<string> _history = new();
        private int _historyIndex = -1;
        private bool _isRunning;

        private static readonly string[] KnownCommands =
        {
            "setup", "load", "save", "run", "list", "list-loops", "info",
            "reload-actors", "cleanup", "commands", "help", "clear", "cls"
        };

        // ?? Events ??????????????????????????????????????????????????????????

        public event EventHandler? WorkspaceChanged;

        // ?? Constructor ?????????????????????????????????????????????????????

        public CommandPanel()
        {
            SuspendLayout();

            // ?? Header ??
            _lblTitle = new Label
            {
                Text = "TERMINAL",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = WallyTheme.TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = WallyTheme.Surface2
            };
            _header.Controls.Add(_lblTitle);

            // ?? Output area ??
            _output = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = WallyTheme.FontMonoLarge,
                BackColor = WallyTheme.Surface0,
                ForeColor = WallyTheme.TextPrimary,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                DetectUrls = false
            };

            // ?? Thin separator line ??
            _separator = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = WallyTheme.Border
            };

            // ?? Prompt label ??
            _lblPrompt = new Label
            {
                Text = "wally\u203A",
                Dock = DockStyle.Left,
                Width = 56,
                Font = WallyTheme.FontMonoBold,
                ForeColor = WallyTheme.TextSecondary,
                BackColor = WallyTheme.Surface2,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            // ?? Input text box ??
            _txtInput = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = WallyTheme.FontMonoLarge,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextPrimary,
                BorderStyle = BorderStyle.None
            };
            _txtInput.KeyDown += OnInputKeyDown;
            _txtInput.GotFocus += (_, _) => _inputBorder.BackColor = WallyTheme.BorderFocused;
            _txtInput.LostFocus += (_, _) => _inputBorder.BackColor = WallyTheme.Border;

            // ?? Input row with focus border ??
            var inputInner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WallyTheme.Surface2,
                Padding = new Padding(0, 5, 8, 5)
            };
            inputInner.Controls.Add(_txtInput);
            inputInner.Controls.Add(_lblPrompt);

            _inputBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                Padding = new Padding(0, 1, 0, 0),
                BackColor = WallyTheme.Border
            };
            _inputBorder.Controls.Add(inputInner);

            _inputRow = _inputBorder; // alias for clarity in assembly

            // ?? Assembly ??
            Controls.Add(_output);
            Controls.Add(_separator);
            Controls.Add(_inputRow);
            Controls.Add(_header);

            BackColor = WallyTheme.Surface0;

            ResumeLayout(true);

            // Welcome message.
            AppendStyledLine("Wally \u2014 AI Actor Environment Manager", WallyTheme.TextPrimary);
            AppendStyledLine("Type 'commands' for help. Use File \u2192 Open or File \u2192 Setup to load a workspace.", WallyTheme.TextMuted);
            AppendStyledLine("", WallyTheme.TextPrimary);
        }

        // ?? Public API ??????????????????????????????????????????????????????

        public void BindEnvironment(WallyEnvironment environment) => _environment = environment;

        /// <summary>Writes a coloured line to the output (thread-safe).</summary>
        public void AppendLine(string text, Color? color = null) =>
            AppendStyledLine(text, color ?? WallyTheme.TextPrimary);

        /// <summary>Programmatically execute a command as if typed.</summary>
        public void ExecuteCommand(string command)
        {
            AppendStyledLine($"wally\u203A {command}", WallyTheme.TextSecondary);
            _ = RunCommandAsync(command);
        }

        /// <summary>Focuses the input text box.</summary>
        public void FocusInput() => _txtInput.Focus();

        // ?? Private output helper ???????????????????????????????????????????

        private void AppendStyledLine(string text, Color color)
        {
            if (InvokeRequired) { Invoke(() => AppendStyledLine(text, color)); return; }

            _output.SelectionStart = _output.TextLength;
            _output.SelectionLength = 0;
            _output.SelectionColor = color;
            _output.AppendText(text + Environment.NewLine);
            _output.ScrollToCaret();
        }

        // ?? Input handling ??????????????????????????????????????????????????

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    string cmd = _txtInput.Text.Trim();
                    if (!string.IsNullOrEmpty(cmd) && !_isRunning)
                    {
                        _txtInput.Clear();
                        _history.Add(cmd);
                        _historyIndex = _history.Count;
                        AppendStyledLine($"wally\u203A {cmd}", WallyTheme.TextSecondary);
                        _ = RunCommandAsync(cmd);
                    }
                    break;

                case Keys.Up:
                    e.SuppressKeyPress = true;
                    if (_history.Count > 0 && _historyIndex > 0)
                    {
                        _historyIndex--;
                        _txtInput.Text = _history[_historyIndex];
                        _txtInput.SelectionStart = _txtInput.Text.Length;
                    }
                    break;

                case Keys.Down:
                    e.SuppressKeyPress = true;
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        _txtInput.Text = _history[_historyIndex];
                        _txtInput.SelectionStart = _txtInput.Text.Length;
                    }
                    else
                    {
                        _historyIndex = _history.Count;
                        _txtInput.Clear();
                    }
                    break;

                case Keys.Tab:
                    e.SuppressKeyPress = true;
                    HandleTabCompletion();
                    break;

                case Keys.Escape:
                    e.SuppressKeyPress = true;
                    _txtInput.Clear();
                    break;
            }
        }

        private void HandleTabCompletion()
        {
            string text = _txtInput.Text;
            if (string.IsNullOrEmpty(text)) return;

            if (!text.Contains(' '))
            {
                var matches = KnownCommands
                    .Where(c => c.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length == 1)
                {
                    _txtInput.Text = matches[0] + " ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (matches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", matches)}", WallyTheme.TextMuted);
                }
                return;
            }

            string[] parts = text.Split(' ', 2);
            string verb = parts[0].ToLowerInvariant();
            string partial = parts.Length > 1 ? parts[1] : "";

            if (verb is "run" && !partial.Contains(' ') && _environment?.HasWorkspace == true)
            {
                var actorMatches = _environment.Actors
                    .Select(a => a.Name)
                    .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (actorMatches.Length == 1)
                {
                    _txtInput.Text = $"{verb} {actorMatches[0]} ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (actorMatches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", actorMatches)}", WallyTheme.TextMuted);
                }
            }
        }

        // ?? Command execution ???????????????????????????????????????????????

        private async Task RunCommandAsync(string input)
        {
            if (_environment == null)
            {
                AppendStyledLine("No environment initialized.", WallyTheme.Red);
                return;
            }

            _isRunning = true;
            _txtInput.Enabled = false;
            _lblPrompt.ForeColor = WallyTheme.TextMuted;

            try
            {
                using var capture = new ConsoleCapture(this);

                await Task.Run(() =>
                {
                    string[] args = SplitArgs(input);
                    if (args.Length == 0) return;

                    string verb = args[0].ToLowerInvariant();

                    switch (verb)
                    {
                        case "setup":
                        {
                            bool verify = HasFlag(args, "--verify");
                            string? setupPath = GetFirstPositional(args, 1);
                            WallyCommands.HandleSetup(_environment, setupPath, verify);
                            break;
                        }

                        case "load":
                            if (args.Length < 2) { Console.WriteLine("Usage: load <path>"); break; }
                            WallyCommands.HandleLoad(_environment, args[1]);
                            break;

                        case "save":
                            if (args.Length < 2) { Console.WriteLine("Usage: save <path>"); break; }
                            WallyCommands.HandleSave(_environment, args[1]);
                            break;

                        case "run":
                        {
                            if (args.Length < 3) { Console.WriteLine("Usage: run <actor> \"<prompt>\" [-m model] [-w wrapper] [--loop] [-l name] [-n max]"); break; }
                            string? runModel = GetOption(args, "-m") ?? GetOption(args, "--model");
                            string? runWrapper = GetOption(args, "-w") ?? GetOption(args, "--wrapper");
                            string? loopName = GetOption(args, "-l") ?? GetOption(args, "--loop-name");
                            string? maxStr = GetOption(args, "-n") ?? GetOption(args, "--max-iterations");
                            bool looped = HasFlag(args, "--loop");
                            int maxIter = int.TryParse(maxStr, out int n) ? n : 0;
                            WallyCommands.HandleRun(_environment, args[2], args[1], runModel, looped, loopName, maxIter, runWrapper);
                            break;
                        }

                        case "list":
                            WallyCommands.HandleList(_environment);
                            break;

                        case "list-loops":
                            WallyCommands.HandleListLoops(_environment);
                            break;

                        case "info":
                            WallyCommands.HandleInfo(_environment);
                            break;

                        case "reload-actors":
                            WallyCommands.HandleReloadActors(_environment);
                            break;

                        case "cleanup":
                        {
                            string? cleanupPath = GetFirstPositional(args, 1);
                            WallyCommands.HandleCleanup(_environment, cleanupPath);
                            break;
                        }

                        case "commands" or "help":
                            WallyCommands.HandleHelp();
                            break;

                        case "clear" or "cls":
                            Invoke(_output.Clear);
                            break;

                        default:
                            Console.WriteLine($"Unknown command: {verb}. Type 'commands' for help.");
                            break;
                    }
                });

                bool stateChanging = input.StartsWith("setup", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("load", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("reload", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("cleanup", StringComparison.OrdinalIgnoreCase);

                if (stateChanging)
                    WorkspaceChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                AppendStyledLine($"Error: {ex.Message}", WallyTheme.Red);
            }
            finally
            {
                _isRunning = false;
                _txtInput.Enabled = true;
                _txtInput.Focus();
                _lblPrompt.ForeColor = WallyTheme.TextSecondary;
            }
        }

        // ?? Argument parsing ????????????????????????????????????????????????

        private static string[] SplitArgs(string input)
        {
            var args = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in input)
            {
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0) { args.Add(current.ToString()); current.Clear(); }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0) args.Add(current.ToString());
            return args.ToArray();
        }

        private static string? GetOption(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        private static bool HasFlag(string[] args, string flag) =>
            Array.Exists(args, a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

        private static string? GetFirstPositional(string[] args, int startIndex)
        {
            for (int i = startIndex; i < args.Length; i++)
                if (!args[i].StartsWith('-'))
                    return args[i];
            return null;
        }

        // ?? Console capture ?????????????????????????????????????????????????

        private sealed class ConsoleCapture : IDisposable
        {
            private readonly TextWriter _originalOut;
            private readonly TextWriter _originalErr;

            public ConsoleCapture(CommandPanel panel)
            {
                _originalOut = Console.Out;
                _originalErr = Console.Error;
                Console.SetOut(new PanelWriter(panel, WallyTheme.TextPrimary));
                Console.SetError(new PanelWriter(panel, WallyTheme.Red));
            }

            public void Dispose()
            {
                Console.SetOut(_originalOut);
                Console.SetError(_originalErr);
            }
        }

        private sealed class PanelWriter : TextWriter
        {
            private readonly CommandPanel _panel;
            private readonly Color _color;
            private readonly System.Text.StringBuilder _buf = new();

            public PanelWriter(CommandPanel panel, Color color) { _panel = panel; _color = color; }

            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            public override void Write(char value)
            {
                if (value == '\n')
                {
                    _panel.AppendStyledLine(_buf.ToString().TrimEnd('\r'), _color);
                    _buf.Clear();
                }
                else _buf.Append(value);
            }

            public override void Write(string? value)
            {
                if (value == null) return;
                foreach (char c in value) Write(c);
            }

            public override void WriteLine(string? value) { Write(value); Write('\n'); }

            protected override void Dispose(bool disposing)
            {
                if (_buf.Length > 0) { _panel.AppendStyledLine(_buf.ToString(), _color); _buf.Clear(); }
                base.Dispose(disposing);
            }
        }
    }
}
