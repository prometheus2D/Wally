using System;
using System.Collections.Generic;
using System.Linq;

namespace Wally.Core
{
    /// <summary>
    /// Shared argument parsing utilities used by both CLI and Forms components.
    /// Provides consistent tokenization and argument extraction without external dependencies.
    /// </summary>
    public static class WallyArgParser
    {
        /// <summary>
        /// Splits a command line string into arguments, handling quoted strings.
        /// Supports double-quote escaping only (consistent with existing behavior).
        /// </summary>
        /// <param name="input">Command line string to tokenize</param>
        /// <returns>Array of argument tokens</returns>
        public static string[] Tokenise(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<string>();

            var args = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
                args.Add(current.ToString());

            return args.ToArray();
        }

        /// <summary>
        /// Extracts the value for a named option from arguments.
        /// Supports multiple flag variations (e.g., "-a", "--actor").
        /// </summary>
        /// <param name="args">Argument array</param>
        /// <param name="flags">Flag names to search for (e.g., "-a", "--actor")</param>
        /// <returns>Value following the flag, or null if not found</returns>
        public static string? GetOption(string[] args, params string[] flags)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (flags.Any(flag => args[i].Equals(flag, StringComparison.OrdinalIgnoreCase)))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a boolean flag is present in the arguments.
        /// </summary>
        /// <param name="args">Argument array</param>
        /// <param name="flag">Flag to search for (e.g., "--no-history")</param>
        /// <returns>True if flag is present, false otherwise</returns>
        public static bool HasFlag(string[] args, string flag)
        {
            return args.Any(arg => arg.Equals(flag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a positional argument by index, skipping flags and their values.
        /// </summary>
        /// <param name="args">Argument array</param>
        /// <param name="index">Zero-based positional index</param>
        /// <returns>Positional argument, or null if not found</returns>
        public static string? GetPositional(string[] args, int index)
        {
            int currentIndex = 0;
            for (int i = 0; i < args.Length; i++)
            {
                // Skip flags and their values
                if (args[i].StartsWith('-'))
                {
                    // If this looks like a flag that takes a value, skip the next argument too
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                    {
                        i++; // Skip the value
                    }
                    continue;
                }

                // This is a positional argument
                if (currentIndex == index)
                {
                    return args[i];
                }
                currentIndex++;
            }

            return null;
        }

        /// <summary>
        /// Gets the first positional argument at or after the specified start index.
        /// Useful for commands where the first positional might be after some flags.
        /// </summary>
        /// <param name="args">Argument array</param>
        /// <param name="startIndex">Array index to start searching from</param>
        /// <returns>First non-flag argument found, or null if none</returns>
        public static string? GetFirstPositional(string[] args, int startIndex)
        {
            for (int i = startIndex; i < args.Length; i++)
            {
                if (!args[i].StartsWith('-'))
                {
                    return args[i];
                }
            }
            return null;
        }
    }
}