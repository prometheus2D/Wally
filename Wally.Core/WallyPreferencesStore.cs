using System;
using System.IO;
using System.Text.Json;

namespace Wally.Core
{
    /// <summary>
    /// Reads and writes <see cref="WallyPreferences"/> to the OS user-profile
    /// folder.  All methods are static and thread-safe for sequential access
    /// (no locking — callers must not call concurrently from multiple threads).
    /// </summary>
    public static class WallyPreferencesStore
    {
        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions { WriteIndented = true };

        // ?? Path resolution ??????????????????????????????????????????????????

        /// <summary>
        /// Returns the full path to <c>wally-prefs.json</c> in the OS
        /// user-profile application-data directory:
        /// <list type="bullet">
        ///   <item>Windows: <c>%APPDATA%\Wally\wally-prefs.json</c></item>
        ///   <item>Linux/macOS: <c>~/.config/Wally/wally-prefs.json</c></item>
        /// </list>
        /// Falls back to <see cref="Path.GetTempPath"/> when
        /// <c>ApplicationData</c> returns an empty string (e.g. some CI
        /// environments).
        /// </summary>
        public static string GetPrefsFilePath()
        {
            string appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

            if (string.IsNullOrWhiteSpace(appData))
                appData = Path.GetTempPath();

            return Path.Combine(appData, "Wally", "wally-prefs.json");
        }

        // ?? Load ?????????????????????????????????????????????????????????????

        /// <summary>
        /// Loads preferences from disk.  Returns a fresh
        /// <see cref="WallyPreferences"/> with default values when the file is
        /// absent, empty, or corrupt — never throws.
        /// </summary>
        public static WallyPreferences Load()
        {
            string path = GetPrefsFilePath();
            if (!File.Exists(path))
                return new WallyPreferences();

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return new WallyPreferences();

                return JsonSerializer.Deserialize<WallyPreferences>(json)
                       ?? new WallyPreferences();
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                return new WallyPreferences();
            }
        }

        // ?? Save ?????????????????????????????????????????????????????????????

        /// <summary>
        /// Persists <paramref name="prefs"/> to disk atomically
        /// (write to a sibling temp file, then rename over the target) so that
        /// a crash mid-write never produces a corrupt prefs file.
        /// </summary>
        public static void Save(WallyPreferences prefs)
        {
            string path = GetPrefsFilePath();
            string dir  = Path.GetDirectoryName(path)!;

            Directory.CreateDirectory(dir);

            // Write to a temp file in the same directory so the rename is
            // an atomic metadata-only operation on most file systems.
            string tmp = Path.Combine(dir, $"wally-prefs.{Guid.NewGuid():N}.tmp");
            try
            {
                string json = JsonSerializer.Serialize(prefs, _jsonOptions);
                File.WriteAllText(tmp, json);
                File.Move(tmp, path, overwrite: true);
            }
            catch
            {
                // Best-effort cleanup of the temp file on failure.
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* ignore */ }
                throw;
            }
        }

        // ?? RecordWorkspaceLoaded ????????????????????????????????????????????

        /// <summary>
        /// Updates <see cref="WallyPreferences.LastWorkspacePath"/> to
        /// <paramref name="workspaceFolderPath"/> (normalised to an absolute
        /// path), prepends a new <see cref="RecentWorkspaceEntry"/> to
        /// <see cref="WallyPreferences.RecentWorkspaces"/> (deduplicating on
        /// the normalised path), trims the list to
        /// <see cref="WallyPreferences.MaxRecentCount"/>, and saves.
        /// </summary>
        /// <param name="workspaceFolderPath">
        /// Absolute or relative path to the <c>.wally</c> workspace folder.
        /// </param>
        public static void RecordWorkspaceLoaded(string workspaceFolderPath)
        {
            if (string.IsNullOrWhiteSpace(workspaceFolderPath))
                return;

            string fullPath = Path.GetFullPath(workspaceFolderPath);

            // Derive the display name from the WorkSource (parent of .wally).
            string workSource   = Path.GetDirectoryName(fullPath) ?? fullPath;
            string displayName  = Path.GetFileName(workSource);
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = fullPath;

            var prefs = Load();

            prefs.LastWorkspacePath = fullPath;

            // Remove any existing entry for the same path (case-insensitive on
            // Windows, case-sensitive elsewhere — normalise via GetFullPath).
            prefs.RecentWorkspaces.RemoveAll(e =>
                string.Equals(
                    Path.GetFullPath(e.Path), fullPath,
                    StringComparison.OrdinalIgnoreCase));

            // Prepend the freshest entry.
            prefs.RecentWorkspaces.Insert(0, new RecentWorkspaceEntry
            {
                Path        = fullPath,
                DisplayName = displayName,
                LastUsed    = DateTimeOffset.UtcNow
            });

            // Enforce the cap.
            int max = prefs.MaxRecentCount > 0 ? prefs.MaxRecentCount : 10;
            if (prefs.RecentWorkspaces.Count > max)
                prefs.RecentWorkspaces.RemoveRange(max,
                    prefs.RecentWorkspaces.Count - max);

            Save(prefs);
        }

        // ?? RemoveFromRecent ?????????????????????????????????????????????????

        /// <summary>
        /// Removes the entry matching <paramref name="workspaceFolderPath"/>
        /// from <see cref="WallyPreferences.RecentWorkspaces"/> and clears
        /// <see cref="WallyPreferences.LastWorkspacePath"/> when it matches
        /// the same path.  Saves the updated prefs.  Safe to call when the
        /// path is not in the list.
        /// </summary>
        public static void RemoveFromRecent(string workspaceFolderPath)
        {
            if (string.IsNullOrWhiteSpace(workspaceFolderPath))
                return;

            string fullPath = Path.GetFullPath(workspaceFolderPath);
            var prefs = Load();

            prefs.RecentWorkspaces.RemoveAll(e =>
                string.Equals(
                    Path.GetFullPath(e.Path), fullPath,
                    StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(prefs.LastWorkspacePath) &&
                string.Equals(
                    Path.GetFullPath(prefs.LastWorkspacePath), fullPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                // Promote the next most-recent entry, or clear entirely.
                prefs.LastWorkspacePath = prefs.RecentWorkspaces.Count > 0
                    ? prefs.RecentWorkspaces[0].Path
                    : null;
            }

            Save(prefs);
        }
    }
}
