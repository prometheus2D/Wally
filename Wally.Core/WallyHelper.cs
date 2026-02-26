using System;
using System.Collections.Generic;
using System.IO;
using Wally.Core.RBA;

namespace Wally.Core
{
    /// <summary>
    /// Provides static helper methods for workspace creation, directory copying, and
    /// config resolution. No configuration values are hardcoded here.
    /// </summary>
    public static class WallyHelper
    {
        // ?? Well-known file names ?????????????????????????????????????????????

        /// <summary>File name for the canonical workspace configuration file.</summary>
        public const string ConfigFileName = "wally-config.json";

        /// <summary>File name for the default actor definitions file.</summary>
        public const string DefaultAgentsFileName = "default-agents.json";

        // ?? Default parent folder ?????????????????????????????????????????????

        /// <summary>
        /// Returns the default parent folder: the directory that contains the executing
        /// assembly. Both the project subfolder and workspace subfolder will be created
        /// as siblings inside this folder.
        /// </summary>
        public static string GetDefaultParentFolder() =>
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? Directory.GetCurrentDirectory();

        // ?? Workspace scaffolding ?????????????????????????????????????????????

        /// <summary>
        /// Scaffolds a complete workspace under <paramref name="parentFolder"/>:
        /// <code>
        ///   &lt;parentFolder&gt;/
        ///       Project/            ? codebase root
        ///       .wally/             ? workspace
        ///           wally-config.json
        ///           default-agents.json
        ///           Roles/          ? one .txt per role      (name = filename stem)
        ///           Criteria/       ? one .txt per criterion
        ///           Intents/        ? one .txt per intent
        /// </code>
        /// Default prompt files are copied from the exe directory when present.
        /// Existing files are never overwritten.
        /// </summary>
        public static void CreateDefaultWorkspace(string parentFolder, WallyConfig config = null)
        {
            config ??= ResolveConfig();

            string workspaceFolder = Path.Combine(parentFolder, config.WorkspaceFolderName);
            string projectFolder   = Path.Combine(parentFolder, config.ProjectFolderName);

            Directory.CreateDirectory(workspaceFolder);
            Directory.CreateDirectory(projectFolder);

            // Config
            string destConfig = Path.Combine(workspaceFolder, ConfigFileName);
            if (!File.Exists(destConfig))
                config.SaveToFile(destConfig);

            // default-agents.json
            CopyDefaultFile(DefaultAgentsFileName, workspaceFolder);

            // RBA prompt directories
            CopyDefaultDirectory(config.RolesFolderName,    workspaceFolder);
            CopyDefaultDirectory(config.CriteriaFolderName, workspaceFolder);
            CopyDefaultDirectory(config.IntentsFolderName,  workspaceFolder);
        }

        // ?? RBA loading from prompt files ?????????????????????????????????????

        /// <summary>
        /// Reads RBA prompt files from the workspace folder and populates
        /// <paramref name="config"/>. Supported filename conventions:
        /// <list type="bullet">
        ///   <item><c>&lt;Name&gt;.txt</c> — name from stem, no tier.</item>
        ///   <item><c>&lt;Name&gt;.&lt;Tier&gt;.txt</c> — name from first segment, tier from second.</item>
        /// </list>
        /// Existing lists in <paramref name="config"/> are replaced.
        /// Does nothing when the subdirectories do not exist yet.
        /// </summary>
        public static void LoadRbaFromPromptFiles(string workspaceFolder, WallyConfig config)
        {
            config.Roles               = LoadRbaItems(workspaceFolder, config.RolesFolderName,
                                             (n, p, t) => new Role(n, p, t));
            config.AcceptanceCriterias = LoadRbaItems(workspaceFolder, config.CriteriaFolderName,
                                             (n, p, t) => new AcceptanceCriteria(n, p, t));
            config.Intents             = LoadRbaItems(workspaceFolder, config.IntentsFolderName,
                                             (n, p, t) => new Intent(n, p, t));
        }

        // ?? Config resolution ?????????????????????????????????????????????????

        /// <summary>
        /// Resolves a <see cref="WallyConfig"/> by searching for a <c>wally-config.json</c>
        /// in the workspace subfolder of the default parent folder.
        /// Returns a default <see cref="WallyConfig"/> when no file is found.
        /// </summary>
        public static WallyConfig ResolveConfig()
        {
            string parentFolder              = GetDefaultParentFolder();
            string defaultWorkspaceFolderName = new WallyConfig().WorkspaceFolderName;

            string subFolderConfig = Path.Combine(parentFolder, defaultWorkspaceFolderName, ConfigFileName);
            if (File.Exists(subFolderConfig))
                return WallyConfig.LoadFromFile(subFolderConfig);

            return new WallyConfig();
        }

        /// <summary>
        /// Returns the path to <c>default-agents.json</c> inside
        /// <paramref name="workspaceFolder"/>, or inside the exe directory as fallback.
        /// Returns <see langword="null"/> when the file cannot be found in either location.
        /// </summary>
        public static string? ResolveDefaultAgentsPath(string workspaceFolder)
        {
            string inWorkspace = Path.Combine(workspaceFolder, DefaultAgentsFileName);
            if (File.Exists(inWorkspace)) return inWorkspace;

            string inExeDir = Path.Combine(GetDefaultParentFolder(), DefaultAgentsFileName);
            if (File.Exists(inExeDir)) return inExeDir;

            return null;
        }

        // ?? Default environment loading ???????????????????????????????????????

        /// <summary>
        /// Loads a <see cref="WallyEnvironment"/> using the default parent folder.
        /// Scaffolds a workspace there if one does not exist yet.
        /// </summary>
        public static WallyEnvironment LoadDefault()
        {
            var env = new WallyEnvironment();
            env.SetupLocal();
            return env;
        }

        // ?? Directory utilities ???????????????????????????????????????????????

        /// <summary>Copies a directory and all its contents recursively.</summary>
        public static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            foreach (string subDir in Directory.GetDirectories(sourceDir))
                CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        /// <summary>
        /// Copies a single file from the exe directory into <paramref name="destFolder"/>.
        /// Skipped silently when the source does not exist or the dest already exists.
        /// </summary>
        private static void CopyDefaultFile(string fileName, string destFolder)
        {
            string src  = Path.Combine(GetDefaultParentFolder(), fileName);
            string dest = Path.Combine(destFolder, fileName);
            if (File.Exists(src) && !File.Exists(dest))
                File.Copy(src, dest);
        }

        /// <summary>
        /// Copies an entire subdirectory (e.g. <c>Roles/</c>) from the exe directory
        /// into <paramref name="destFolder"/>. Each file is copied individually;
        /// existing destination files are not overwritten.
        /// </summary>
        private static void CopyDefaultDirectory(string subDirName, string destFolder)
        {
            string srcDir  = Path.Combine(GetDefaultParentFolder(), subDirName);
            string destDir = Path.Combine(destFolder, subDirName);
            if (!Directory.Exists(srcDir)) return;

            Directory.CreateDirectory(destDir);
            foreach (string srcFile in Directory.GetFiles(srcDir, "*.txt"))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(srcFile));
                if (!File.Exists(destFile))
                    File.Copy(srcFile, destFile);
            }
        }

        /// <summary>
        /// Reads all <c>.txt</c> files from <c>&lt;workspaceFolder&gt;/&lt;subDir&gt;/</c>
        /// and converts each into an RBA item.
        /// Filename convention: <c>&lt;Name&gt;.txt</c> or <c>&lt;Name&gt;.&lt;Tier&gt;.txt</c>.
        /// </summary>
        private static List<T> LoadRbaItems<T>(
            string workspaceFolder, string subDir, Func<string, string, string?, T> factory)
        {
            var items = new List<T>();
            string dir = Path.Combine(workspaceFolder, subDir);
            if (!Directory.Exists(dir)) return items;

            foreach (string file in Directory.GetFiles(dir, "*.txt"))
            {
                // Split on '.' to detect optional tier segment: "Name.Tier.txt" ? ["Name","Tier","txt"]
                string fileNameNoExt = Path.GetFileNameWithoutExtension(file); // "Name" or "Name.Tier"
                int dotIdx = fileNameNoExt.IndexOf('.');
                string name = dotIdx >= 0 ? fileNameNoExt[..dotIdx]    : fileNameNoExt;
                string? tier = dotIdx >= 0 ? fileNameNoExt[(dotIdx + 1)..] : null;

                string prompt = File.ReadAllText(file).Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    items.Add(factory(name, prompt, tier));
            }
            return items;
        }
    }
}
