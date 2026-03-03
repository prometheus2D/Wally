using System;
using System.Collections.Generic;
using System.IO;

namespace Wally.Core
{
    /// <summary>
    /// A runbook loaded from a <c>.wrb</c> (Wally Runbook) file.
    /// <para>
    /// Runbooks are plain-text files with one Wally command per line.
    /// Lines starting with <c>#</c> are comments. Blank lines are ignored.
    /// The first comment line becomes the <see cref="Description"/>.
    /// </para>
    /// </summary>
    public class WallyRunbook
    {
        /// <summary>Name derived from filename without extension (e.g. "setup-and-review").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Description extracted from the first comment line, or empty.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Absolute path to the <c>.wrb</c> file on disk.</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Parsed command lines (non-comment, non-blank, trimmed).</summary>
        public List<string> Commands { get; set; } = new();

        // — Loading ——————————————————————————————————————————————————————

        /// <summary>Parses a single <c>.wrb</c> file.</summary>
        public static WallyRunbook LoadFromFile(string filePath)
        {
            var rb = new WallyRunbook
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = Path.GetFullPath(filePath)
            };

            bool descriptionSet = false;

            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith('#'))
                {
                    if (!descriptionSet)
                    {
                        rb.Description = line.TrimStart('#').Trim();
                        descriptionSet = true;
                    }
                    continue;
                }

                rb.Commands.Add(line);
            }

            return rb;
        }

        /// <summary>
        /// Loads all <c>*.wrb</c> files from <paramref name="folder"/>.
        /// Skips files that fail to parse.
        /// </summary>
        public static List<WallyRunbook> LoadFromFolder(string folder)
        {
            var runbooks = new List<WallyRunbook>();
            if (!Directory.Exists(folder)) return runbooks;

            foreach (string file in Directory.GetFiles(folder, "*.wrb"))
            {
                try
                {
                    runbooks.Add(LoadFromFile(file));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"Warning: Failed to load runbook '{file}': {ex.Message}");
                }
            }

            return runbooks;
        }
    }
}
