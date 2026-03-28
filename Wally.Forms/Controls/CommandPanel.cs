using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
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
        // ?? Controls ??????????????????????????????????????????????????????????

        private readonly Panel _header;
        private readonly Label _lblTitle;
        private readonly Button _btnHelp;      // header help button
        private readonly Button _btnStop;      // header stop button
        private readonly Button _btnSave;      // header save button
        private readonly ThemedRichTextBox _output;
        private readonly Panel _inputRow;
        private readonly Panel _separator;
        private readonly Panel _inputBorder;
        private readonly Label _lblPrompt;
        private readonly TextBox _txtInput;

        // ?? State ?????????????????????????????????????????????????????????????

        private WallyEnvironment? _environment;
        private readonly List<string> _history = new();
        private int _historyIndex = -1;
        private bool _isRunning;
        private bool _isExternallyBusy;   // true while the chat panel is running
        private CancellationTokenSource? _cts;

        /// <summary>Default subfolder name for terminal log saves inside the workspace Logs folder.</summary>
        private const string TerminalLogsFolderName = "TerminalLogs";

        // ?? Events ????????????????????????????????????????????????????????????

        public event EventHandler? WorkspaceChanged;

        /// <summary>
        /// Raised on the UI thread whenever the running state changes.
        /// <c>EventArgs</c> is always <see cref="EventArgs.Empty"/>.
        /// </summary>
        public event EventHandler? RunningChanged;

        // ?? Constructor ???????????????????????????????????????????????????????

        public CommandPanel()
        {
            SuspendLayout();

            // ?? Header ????????????????????????????????????????????????????????
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

            _btnStop = new Button
            {
                Text = "\u23F9 Stop",
                Dock = DockStyle.Right,
                Width = 58,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextDisabled,
                Cursor = Cursors.Hand,
                Enabled = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.FlatAppearance.MouseOverBackColor = WallyTheme.Surface3;
            _btnStop.Click += (_, _) => _cts?.Cancel();

            _btnSave = new Button
            {
                Text = "\uD83D\uDCBE Save",
                Dock = DockStyle.Right,
                Width = 58,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextSecondary,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.FlatAppearance.MouseOverBackColor = WallyTheme.Surface3;
            _btnSave.Click += (_, _) => SaveTerminalOutput();

            _btnHelp = new Button
            {
                Text = "? Help",
                Dock = DockStyle.Right,
                Width = 52,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = WallyTheme.Surface2,
                ForeColor = WallyTheme.TextSecondary,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty
            };
            _btnHelp.FlatAppearance.BorderSize = 0;
            _btnHelp.FlatAppearance.MouseOverBackColor = WallyTheme.Surface3;
            _btnHelp.Click += (_, _) =>
            {
                if (!_isRunning && !_isExternallyBusy)
                    ExecuteCommand("commands");
            };

            var headerTooltip = new ToolTip();
            headerTooltip.SetToolTip(_btnHelp, "Show available console commands");
            headerTooltip.SetToolTip(_btnSave, "Save terminal output to the TerminalLogs folder");

            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = WallyTheme.Surface2
            };
            _header.Controls.Add(_lblTitle);
            _header.Controls.Add(_btnStop);
            _header.Controls.Add(_btnSave);
            _header.Controls.Add(_btnHelp);

            // ?? Output area ???????????????????????????????????????????????????
            _output = ThemedEditorFactory.CreateDocumentViewer(wordWrap: true, readOnly: true, backColor: WallyTheme.Surface0);
            _output.Font = WallyTheme.FontMonoLarge;
            _output.ShortcutsEnabled = true;            // ensures Ctrl+C works on read-only RTB
            _output.AlwaysShowScrollbar    = true;      // vertical thumb always visible

            // Right-click context menu (standard WinForms terminal pattern).
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Renderer = WallyTheme.CreateRenderer();
            ctxMenu.BackColor = WallyTheme.Surface2;
            ctxMenu.ForeColor = WallyTheme.TextPrimary;

            var ctxCopy = new ToolStripMenuItem("Copy", null, (_, _) =>
            {
                if (_output.SelectionLength > 0)
                    Clipboard.SetText(_output.SelectedText);
            })
            { ShortcutKeyDisplayString = "Ctrl+C" };

            var ctxSelectAll = new ToolStripMenuItem("Select All", null, (_, _) =>
            {
                _output.SelectAll();
            })
            { ShortcutKeyDisplayString = "Ctrl+A" };

            var ctxClear = new ToolStripMenuItem("Clear", null, (_, _) =>
            {
                _output.Clear();
            });

            var ctxSaveOutput = new ToolStripMenuItem("Save Output\u2026", null, (_, _) =>
            {
                SaveTerminalOutput();
            });

            ctxMenu.Items.Add(ctxCopy);
            ctxMenu.Items.Add(ctxSelectAll);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(ctxSaveOutput);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(ctxClear);
            ctxMenu.Opening += (_, _) =>
            {
                ctxCopy.Enabled = _output.SelectionLength > 0;
                ctxSaveOutput.Enabled = _output.TextLength > 0;
            };
            _output.ContextMenuStrip = ctxMenu;

            // ?? Thin separator line ???????????????????????????????????????????
            _separator = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = WallyTheme.Border
            };

            // ?? Prompt label ??????????????????????????????????????????????????
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

            // ?? Input text box ????????????????????????????????????????????????
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

            // ?? Input row with focus border ???????????????????????????????????
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

            // ?? Assembly ??????????????????????????????????????????????????????
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

        // ?? Public API ????????????????????????????????????????????????????????

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

        /// <summary>
        /// Returns <see langword="true"/> while a command is being executed.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Requests cancellation of the currently running command, if any.
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Called by the host form when an external panel (e.g. the chat panel)
        /// starts or stops a long-running operation. While busy, the terminal
        /// input is locked so the user cannot queue a conflicting command.
        /// </summary>
        public void SetExternallyBusy(bool busy)
        {
            if (InvokeRequired) { Invoke(() => SetExternallyBusy(busy)); return; }

            _isExternallyBusy = busy;

            // Don't override the terminal's own running state.
            if (_isRunning) return;

            _txtInput.ReadOnly = busy;
            _lblPrompt.ForeColor = busy ? WallyTheme.TextDisabled : WallyTheme.TextSecondary;
            _inputBorder.BackColor = busy ? WallyTheme.BorderSubtle : WallyTheme.Border;
        }

        // ?? Save terminal output ??????????????????????????????????????????????

        /// <summary>
        /// Saves the current terminal output text to a timestamped file in the
        /// workspace's <c>Logs/TerminalLogs/</c> folder. If no workspace is loaded
        /// the user is prompted with a SaveFileDialog.
        /// </summary>
        private void SaveTerminalOutput()
        {
            if (_output.TextLength == 0)
            {
                AppendStyledLine("Nothing to save — terminal is empty.", WallyTheme.TextMuted);
                return;
            }

            string text = _output.Text;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string fileName = $"terminal_{timestamp}.log";

            // Try saving to the default TerminalLogs folder in the workspace
            if (_environment?.HasWorkspace == true && _environment.WorkspaceFolder != null)
            {
                string logsFolder = Path.Combine(
                    _environment.WorkspaceFolder,
                    _environment.Workspace!.Config.LogsFolderName,
                    TerminalLogsFolderName);

                try
                {
                    Directory.CreateDirectory(logsFolder);
                    string filePath = Path.Combine(logsFolder, fileName);
                    File.WriteAllText(filePath, text);
                    AppendStyledLine($"Terminal output saved: {filePath}", WallyTheme.TextSecondary);
                    return;
                }
                catch (Exception ex)
                {
                    AppendStyledLine($"Failed to save to workspace logs: {ex.Message}", WallyTheme.Red);
                    // Fall through to SaveFileDialog
                }
            }

            // Fallback: prompt the user for a save location
            using var dlg = new SaveFileDialog
            {
                Title = "Save Terminal Output",
                FileName = fileName,
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "log"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, text);
                    AppendStyledLine($"Terminal output saved: {dlg.FileName}", WallyTheme.TextSecondary);
                }
                catch (Exception ex)
                {
                    AppendStyledLine($"Failed to save: {ex.Message}", WallyTheme.Red);
                }
            }
        }

        // ?? Private output helper ?????????????????????????????????????????????

        private void AppendStyledLine(string text, Color color)
        {
            if (InvokeRequired) { Invoke(() => AppendStyledLine(text, color)); return; }

            // Only auto-scroll to the new line if the user is already at (or near)
            // the bottom of the output — so manually scrolling up to read history
            // is not interrupted by incoming text.
            bool nearBottom = IsOutputNearBottom();

            _output.SelectionStart = _output.TextLength;
            _output.SelectionLength = 0;
            _output.SelectionColor = color;
            _output.AppendText(text + Environment.NewLine);

            if (nearBottom)
                _output.ScrollToCaret();
        }

        /// <summary>
        /// Returns <see langword="true"/> when the terminal output's scroll position
        /// is within one page of the bottom, meaning new lines should auto-scroll.
        /// </summary>
        private bool IsOutputNearBottom()
        {
            if (!_output.IsHandleCreated)
                return true;

            // Use the first visible character index to approximate scroll position.
            int firstVisible = _output.GetCharIndexFromPosition(new Point(1, 1));
            int lastChar     = _output.TextLength;

            // If less than ~one screen's worth of characters remain after the
            // first visible char, we consider ourselves "near the bottom".
            // A rough estimate: visible lines * average chars/line.
            int visibleLines = Math.Max(1, _output.ClientSize.Height / (_output.Font.Height + 2));
            int charsPerLine = Math.Max(1, _output.ClientSize.Width  / Math.Max(1, _output.Font.Height));
            int pageChars    = visibleLines * charsPerLine;

            return (lastChar - firstVisible) <= pageChars * 2;
        }

        // ?? Input handling ??????????????????????????????????????????????????

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    if (_isRunning || _isExternallyBusy)
                    {
                        FlashBusyPrompt();
                        return;
                    }
                    string cmd = _txtInput.Text.Trim();
                    if (!string.IsNullOrEmpty(cmd))
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
                    if (!_isRunning && !_isExternallyBusy && _history.Count > 0 && _historyIndex > 0)
                    {
                        _historyIndex--;
                        _txtInput.Text = _history[_historyIndex];
                        _txtInput.SelectionStart = _txtInput.Text.Length;
                    }
                    break;

                case Keys.Down:
                    e.SuppressKeyPress = true;
                    if (!_isRunning && !_isExternallyBusy)
                    {
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
                    }
                    break;

                case Keys.Tab:
                    e.SuppressKeyPress = true;
                    if (!_isRunning && !_isExternallyBusy)
                        HandleTabCompletion();
                    break;

                case Keys.Escape:
                    e.SuppressKeyPress = true;
                    if (_isRunning)
                        _cts?.Cancel();
                    else if (!_isExternallyBusy)
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
                // Complete command verbs
                var knownCommands = WallyCommands.GetVerbs();
                var matches = knownCommands
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

            // Parse arguments using shared parser
            string[] args = WallyArgParser.Tokenise(text);
            if (args.Length == 0) return;

            string verb = args[0].ToLowerInvariant();
            string partial = args.Length > 1 ? args[^1] : "";

            // Handle specific command completions
            if (verb is "run" && !partial.Contains(' ') && _environment?.HasWorkspace == true)
            {
                // Complete actor names for 'run' command
                var actorMatches = _environment.Actors
                    .Select(a => a.Name)
                    .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (actorMatches.Length == 1)
                {
                    // Replace the partial with the complete match
                    var completedArgs = args.Take(args.Length - 1).Append(actorMatches[0]).ToArray();
                    _txtInput.Text = string.Join(" ", completedArgs) + " ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (actorMatches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", actorMatches)}", WallyTheme.TextMuted);
                }
            }
            else if (verb is "runbook" && !partial.Contains(' ') && _environment?.HasWorkspace == true)
            {
                // Complete runbook names
                var rbMatches = _environment.Runbooks
                    .Select(r => r.Name)
                    .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (rbMatches.Length == 1)
                {
                    var completedArgs = args.Take(args.Length - 1).Append(rbMatches[0]).ToArray();
                    _txtInput.Text = string.Join(" ", completedArgs) + " ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (rbMatches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", rbMatches)}", WallyTheme.TextMuted);
                }
            }
            else if (verb is "edit-actor" or "delete-actor" && !partial.Contains(' ') && _environment?.HasWorkspace == true)
            {
                // Complete actor names for edit/delete commands
                var matches = _environment.Actors
                    .Select(a => a.Name)
                    .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length == 1)
                {
                    var completedArgs = args.Take(args.Length - 1).Append(matches[0]).ToArray();
                    _txtInput.Text = string.Join(" ", completedArgs) + " ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (matches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", matches)}", WallyTheme.TextMuted);
                }
            }
            else if (verb is "edit-loop" or "delete-loop" && !partial.Contains(' ') && _environment?.HasWorkspace == true)
            {
                // Complete loop names
                var matches = _environment.Loops
                    .Select(l => l.Name)
                    .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length == 1)
                {
                    var completedArgs = args.Take(args.Length - 1).Append(matches[0]).ToArray();
                    _txtInput.Text = string.Join(" ", completedArgs) + " ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (matches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", matches)}", WallyTheme.TextMuted);
                }
            }
            else if (verb is "edit-wrapper" or "delete-wrapper" && !partial.Contains(' ') && _environment?.HasWorkspace == true)
            {
                // Complete wrapper names
                var matches = _environment.Workspace!.LlmWrappers
                    .Select(w => w.Name)
                    .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length == 1)
                {
                    var completedArgs = args.Take(args.Length - 1).Append(matches[0]).ToArray();
                    _txtInput.Text = string.Join(" ", completedArgs) + " ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (matches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", matches)}", WallyTheme.TextMuted);
                }
            }
            else if (verb is "edit-runbook" or "delete-runbook" && !partial.Contains(' ') && _environment?.HasWorkspace == true)
            {
                // Complete runbook names
                var matches = _environment.Runbooks
                    .Select(r => r.Name)
                    .Where(n => n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length == 1)
                {
                    var completedArgs = args.Take(args.Length - 1).Append(matches[0]).ToArray();
                    _txtInput.Text = string.Join(" ", completedArgs) + " ";
                    _txtInput.SelectionStart = _txtInput.Text.Length;
                }
                else if (matches.Length > 1)
                {
                    AppendStyledLine($"  {string.Join("  ", matches)}", WallyTheme.TextMuted);
                }
            }
        }

        /// <summary>
        /// Briefly flashes the prompt label red to signal that the terminal is
        /// busy processing a command. Resets after 400 ms without blocking the UI thread.
        /// </summary>
        private async void FlashBusyPrompt()
        {
            _lblPrompt.ForeColor = WallyTheme.Red;
            await Task.Delay(400);
            // Only reset if still running — RunCommandAsync restores the
            // correct colour in its finally block when the operation finishes.
            if (_isRunning)
                _lblPrompt.ForeColor = WallyTheme.TextMuted;
        }

        // ?? Command execution ?????????????????????????????????????????????????

        private async Task RunCommandAsync(string input)
        {
            if (_environment == null)
            {
                AppendStyledLine("No environment initialized.", WallyTheme.Red);
                return;
            }

            _isRunning = true;
            _txtInput.ReadOnly = true;
            _lblPrompt.ForeColor = WallyTheme.TextMuted;
            _btnStop.Enabled = true;
            _btnStop.ForeColor = WallyTheme.Red;
            _cts = new CancellationTokenSource();
            RunningChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                using var capture = new ConsoleCapture(this);
                var token = _cts.Token;

                // Do NOT pass the token as Task.Run's second argument — that would
                // cancel the task scheduling itself. The token is checked and
                // threaded through execution inside the lambda instead.
                await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    string[] args = WallyCommands.SplitArgs(input);
                    if (args.Length == 0) return;

                    string verb = args[0].ToLowerInvariant();

                    if (verb is "clear" or "cls")
                    {
                        Invoke(_output.Clear);
                        return;
                    }

                    // Thread the cancellation token into 'run' and 'runbook' commands
                    // so the child process is actually killed when Stop is pressed.
                    if (verb == "run" && args.Length >= 2)
                    {
                        string? actorName = GetOption(args, "-a") ?? GetOption(args, "--actor");
                        string? model     = GetOption(args, "-m") ?? GetOption(args, "--model");
                        string? wrapper   = GetOption(args, "-w") ?? GetOption(args, "--wrapper");
                        string? loopName  = GetOption(args, "-l") ?? GetOption(args, "--loop-name");
                        bool noHistory    = HasFlag(args, "--no-history");
                        WallyCommands.HandleRun(_environment, args[1], actorName, model,
                            loopName, wrapper, noHistory, token);
                        return;
                    }

                    WallyCommands.DispatchCommand(_environment, args);
                });

                bool stateChanging = input.StartsWith("setup", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("load", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("reload", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("cleanup", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("runbook", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("add-", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("edit-", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("delete-", StringComparison.OrdinalIgnoreCase);

                if (stateChanging)
                    WorkspaceChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                AppendStyledLine("Cancelled.", WallyTheme.TextMuted);
            }
            catch (Exception ex)
            {
                AppendStyledLine($"Error: {ex.Message}", WallyTheme.Red);
            }
            finally
            {
                _isRunning = false;
                _cts?.Dispose();
                _cts = null;
                _btnStop.Enabled = false;
                _btnStop.ForeColor = WallyTheme.TextDisabled;
                _txtInput.ReadOnly = _isExternallyBusy;  // stay locked if chat is still running
                _txtInput.Focus();
                _lblPrompt.ForeColor = _isExternallyBusy ? WallyTheme.TextDisabled : WallyTheme.TextSecondary;
                _inputBorder.BackColor = _isExternallyBusy ? WallyTheme.BorderSubtle : WallyTheme.Border;
                RunningChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ?? Argument parsing (now uses shared WallyArgParser) ?????????????????

        private static string[] SplitArgs(string input) => WallyArgParser.Tokenise(input);

        private static string? GetOption(string[] args, string flag) => WallyArgParser.GetOption(args, flag);

        private static bool HasFlag(string[] args, string flag) => WallyArgParser.HasFlag(args, flag);

        private static string? GetFirstPositional(string[] args, int startIndex) => WallyArgParser.GetFirstPositional(args, startIndex);

        // ?? Console capture ???????????????????????????????????????????????????

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
