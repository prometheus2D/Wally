using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wally.Core
{
    /// <summary>
    /// User-profile-level preferences for Wally, persisted in
    /// <c>%APPDATA%\Wally\wally-prefs.json</c> (Windows) or
    /// <c>~/.config/Wally/wally-prefs.json</c> (Linux/macOS).
    /// Completely separate from any workspace — one file per OS user.
    /// </summary>
    public class WallyPreferences
    {
        /// <summary>
        /// Absolute path to the <c>.wally</c> folder of the most recently
        /// successfully loaded workspace. <see langword="null"/> when no
        /// workspace has ever been loaded.
        /// </summary>
        public string? LastWorkspacePath { get; set; }

        /// <summary>
        /// When <see langword="true"/> (default), both <c>Wally.Forms</c> and
        /// <c>Wally.Console</c> attempt to silently load
        /// <see cref="LastWorkspacePath"/> at startup before falling back to
        /// other probes. Set to <see langword="false"/> to opt out and always
        /// start cold.
        /// </summary>
        public bool AutoLoadLast { get; set; } = true;

        /// <summary>
        /// Ordered list of recently used workspaces, newest first.
        /// Capped at <see cref="MaxRecentCount"/> entries.
        /// </summary>
        public List<RecentWorkspaceEntry> RecentWorkspaces { get; set; } = new();

        /// <summary>
        /// Maximum number of entries kept in <see cref="RecentWorkspaces"/>.
        /// Entries beyond this limit are dropped (oldest first).
        /// </summary>
        public int MaxRecentCount { get; set; } = 10;
    }

    /// <summary>
    /// Represents one entry in the <see cref="WallyPreferences.RecentWorkspaces"/> list.
    /// </summary>
    public class RecentWorkspaceEntry
    {
        /// <summary>Absolute path to the <c>.wally</c> workspace folder.</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable label — the name of the WorkSource directory
        /// (the parent of the <c>.wally</c> folder).
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>UTC timestamp of the last successful load.</summary>
        public DateTimeOffset LastUsed { get; set; }
    }
}
