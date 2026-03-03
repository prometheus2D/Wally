using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wally.Core.Logging;

namespace Wally.Core.Providers
{
    /// <summary>
    /// A data-driven LLM CLI wrapper loaded entirely from a JSON definition.
    /// <para>
    /// Each <c>.json</c> file in the workspace's <c>Wrappers/</c> folder
    /// defines one wrapper: the executable to run, the argument template,
    /// display metadata, and behavioural flags. No C# subclass is needed —
    /// the JSON <em>is</em> the wrapper.
    /// </para>
    /// <para>
    /// At execution time the wrapper builds a CLI command line from its
    /// <see cref="ArgumentTemplate"/>, substituting placeholders for the
    /// prompt, model, and source path, then spawns the process and captures
    /// the output.
    /// </para>
    /// <h3>Supported placeholders in <see cref="ArgumentTemplate"/>:</h3>
    /// <list type="bullet">
    ///   <item><c>{prompt}</c> — the fully enriched prompt text.</item>
    ///   <item><c>{model}</c> — the resolved model identifier (omitted when null).</item>
    ///   <item><c>{sourcePath}</c> — the user's codebase root (omitted when null).</item>
    /// </list>
    /// <h3>Example JSON:</h3>
    /// <code>
    /// {
    ///   "Name": "Copilot",
    ///   "Description": "Read-only — runs gh copilot -p",
    ///   "Executable": "gh",
    ///   "ArgumentTemplate": "copilot {model} {sourcePath} --yolo -s -p {prompt}",
    ///   "ModelArgFormat": "--model {model}",
    ///   "SourcePathArgFormat": "--add-dir {sourcePath}",
    ///   "UseSourcePathAsWorkingDirectory": true,
    ///   "CanMakeChanges": false
    /// }
    /// </code>
    /// </summary>
    public sealed class LlmWrapper
    {
        // — Identity (from JSON) ——————————————————————————————————————————

        /// <summary>
        /// The logical name used to reference this wrapper in config and commands
        /// (e.g. <c>"Copilot"</c>, <c>"AutoCopilot"</c>).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of what this wrapper does.
        /// Displayed in <c>info</c> output and the UI.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        // — CLI recipe (from JSON) ————————————————————————————————————————

        /// <summary>
        /// The executable to run (e.g. <c>"gh"</c>).
        /// </summary>
        public string Executable { get; set; } = "gh";

        /// <summary>
        /// The argument template with placeholders. Each token is added as a
        /// separate argument. Placeholder tokens (<c>{prompt}</c>,
        /// <c>{model}</c>, <c>{sourcePath}</c>) are expanded or omitted.
        /// </summary>
        public string ArgumentTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Format string for the model argument(s). The placeholder
        /// <c>{model}</c> is replaced with the actual model identifier.
        /// When the model is null/empty, the entire segment is omitted
        /// from the command line.
        /// Example: <c>"--model {model}"</c>
        /// </summary>
        public string ModelArgFormat { get; set; } = "--model {model}";

        /// <summary>
        /// Format string for the source path argument(s). The placeholder
        /// <c>{sourcePath}</c> is replaced with the actual path.
        /// When the source path is null/empty or doesn't exist, the entire
        /// segment is omitted from the command line.
        /// Example: <c>"--add-dir {sourcePath}"</c>
        /// </summary>
        public string SourcePathArgFormat { get; set; } = "--add-dir {sourcePath}";

        /// <summary>
        /// When <see langword="true"/>, the source path is also set as the
        /// process's working directory.
        /// </summary>
        public bool UseSourcePathAsWorkingDirectory { get; set; } = true;

        // — Behavioural flags (from JSON) —————————————————————————————————

        /// <summary>
        /// When <see langword="true"/>, this wrapper can make changes to files
        /// on disk (e.g. agentic mode). Read-only wrappers set this to
        /// <see langword="false"/>.
        /// </summary>
        public bool CanMakeChanges { get; set; }

        // — Execution —————————————————————————————————————————————————————

        /// <summary>
        /// Builds the CLI command from <see cref="ArgumentTemplate"/>, spawns
        /// the process, and returns the captured output.
        /// </summary>
        /// <param name="processedPrompt">
        /// The fully enriched prompt (RBA context + user prompt + doc context).
        /// </param>
        /// <param name="sourcePath">
        /// The root of the user's codebase. May be <see langword="null"/>.
        /// </param>
        /// <param name="model">
        /// The resolved model identifier. May be <see langword="null"/>.
        /// </param>
        /// <param name="logger">
        /// Session logger for CLI errors. May be <see langword="null"/>.
        /// </param>
        /// <returns>The LLM response text, or an error message.</returns>
        public string Execute(
            string processedPrompt,
            string? sourcePath,
            string? model,
            SessionLogger? logger)
        {
            try
            {
                bool hasSourcePath = !string.IsNullOrWhiteSpace(sourcePath)
                                     && Directory.Exists(sourcePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName               = Executable,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    RedirectStandardInput  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding  = Encoding.UTF8
                };

                // Build the argument list from the template.
                BuildArguments(startInfo, processedPrompt, sourcePath, model, hasSourcePath);

                if (UseSourcePathAsWorkingDirectory && hasSourcePath)
                    startInfo.WorkingDirectory = sourcePath!;

                return RunProcess(startInfo, logger);
            }
            catch (Exception ex)
            {
                logger?.LogCliError(Name, -1, ex.Message);
                return $"Failed to call {Name}: {ex.Message}";
            }
        }

        // — Argument building —————————————————————————————————————————————

        /// <summary>
        /// Parses <see cref="ArgumentTemplate"/> into tokens and expands
        /// placeholders. Placeholder tokens that resolve to nothing are
        /// omitted entirely.
        /// </summary>
        private void BuildArguments(
            ProcessStartInfo startInfo,
            string prompt,
            string? sourcePath,
            string? model,
            bool hasSourcePath)
        {
            // Build the expanded model and sourcePath segments.
            string? modelSegment = !string.IsNullOrWhiteSpace(model)
                ? ModelArgFormat.Replace("{model}", model)
                : null;

            string? sourceSegment = hasSourcePath
                ? SourcePathArgFormat.Replace("{sourcePath}", sourcePath!)
                : null;

            // Split the template into whitespace-delimited tokens.
            string[] tokens = ArgumentTemplate.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                if (string.Equals(token, "{prompt}", StringComparison.OrdinalIgnoreCase))
                {
                    // The prompt is always a single argument (not split).
                    startInfo.ArgumentList.Add(prompt);
                }
                else if (string.Equals(token, "{model}", StringComparison.OrdinalIgnoreCase))
                {
                    // Expand model segment into individual args.
                    if (modelSegment != null)
                    {
                        foreach (string part in modelSegment.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            startInfo.ArgumentList.Add(part);
                    }
                    // else: omit entirely
                }
                else if (string.Equals(token, "{sourcePath}", StringComparison.OrdinalIgnoreCase))
                {
                    // Expand source path segment into individual args.
                    if (sourceSegment != null)
                    {
                        foreach (string part in sourceSegment.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            startInfo.ArgumentList.Add(part);
                    }
                    // else: omit entirely
                }
                else
                {
                    // Literal token — add as-is.
                    startInfo.ArgumentList.Add(token);
                }
            }
        }

        // — Process execution —————————————————————————————————————————————

        private string RunProcess(ProcessStartInfo startInfo, SessionLogger? logger)
        {
            using var process = new Process { StartInfo = startInfo };

            process.Start();
            process.StandardInput.Close();

            string output = process.StandardOutput.ReadToEnd();
            string error  = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                logger?.LogCliError(Name, process.ExitCode, error);
                return string.IsNullOrWhiteSpace(error)
                    ? $"{Name} exited with code {process.ExitCode}."
                    : $"Error from {Name} (exit {process.ExitCode}):\n{error}";
            }

            return string.IsNullOrWhiteSpace(output)
                ? $"({Name} returned an empty response)"
                : output.Trim();
        }

        // — Serialization ————————————————————————————————————————————————

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Deserializes a wrapper from a JSON file.</summary>
        public static LlmWrapper? LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<LlmWrapper>(json, _jsonOptions);
        }

        /// <summary>Serializes this wrapper to a JSON file.</summary>
        public void SaveToFile(string filePath)
        {
            string json = JsonSerializer.Serialize(this, _jsonOptions);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads all <c>*.json</c> files from <paramref name="folder"/>.
        /// Skips files that fail to parse (logs a warning to stderr).
        /// </summary>
        public static List<LlmWrapper> LoadFromFolder(string folder)
        {
            var wrappers = new List<LlmWrapper>();

            if (!Directory.Exists(folder))
                return wrappers;

            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var wrapper = LoadFromFile(file);
                    if (wrapper != null && !string.IsNullOrWhiteSpace(wrapper.Name))
                        wrappers.Add(wrapper);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"Warning: Failed to parse LLM wrapper '{file}': {ex.Message}");
                }
            }

            return wrappers;
        }
    }
}
