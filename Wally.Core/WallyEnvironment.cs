using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wally.Core.Actors;
using Wally.Core.RBA;

namespace Wally.Core
{
    /// <summary>
    /// Represents the Wally environment that manages a collection of Actors.
    /// </summary>
    public class WallyEnvironment
    {
        /// <summary>
        /// The top-level file path for the project or workspace.
        /// </summary>
        public string TopFilePath { get; set; }

        /// <summary>
        /// A list of file paths relevant to the Wally environment.
        /// </summary>
        public List<string> FilePaths { get; set; } = new List<string>();

        /// <summary>
        /// The path to the documentation folder.
        /// </summary>
        public string DocumentationFolder { get; set; }

        /// <summary>
        /// The path to the working folder.
        /// </summary>
        public string WorkingFolder { get; set; }

        /// <summary>
        /// The path to the completed documentation folder.
        /// </summary>
        public string CompletedDocumentationFolder { get; set; }

        /// <summary>
        /// The path to the code directory where code changes occur.
        /// </summary>
        public string CodeDirectory { get; set; }

        /// <summary>
        /// The list of Actors in the environment.
        /// </summary>
        [JsonIgnore]
        public List<Actor> Actors { get; set; } = new List<Actor>();

        /// <summary>
        /// Adds a file path to the configuration.
        /// </summary>
        /// <param name="filePath">The file path to add.</param>
        public void AddFilePath(string filePath)
        {
            FilePaths.Add(filePath);
        }

        /// <summary>
        /// Gets the path to the pending subfolder in the working folder.
        /// </summary>
        public string GetPendingFolder() => Path.Combine(WorkingFolder, "Pending");

        /// <summary>
        /// Gets the path to the active subfolder in the working folder.
        /// </summary>
        public string GetActiveFolder() => Path.Combine(WorkingFolder, "Active");

        /// <summary>
        /// Gets the path to the completed subfolder in the working folder.
        /// </summary>
        public string GetWorkingCompletedFolder() => Path.Combine(WorkingFolder, "Completed");

        /// <summary>
        /// Ensures all required folders exist.
        /// </summary>
        public void EnsureFoldersExist()
        {
            Directory.CreateDirectory(DocumentationFolder);
            Directory.CreateDirectory(WorkingFolder);
            Directory.CreateDirectory(GetPendingFolder());
            Directory.CreateDirectory(GetActiveFolder());
            Directory.CreateDirectory(GetWorkingCompletedFolder());
            Directory.CreateDirectory(CompletedDocumentationFolder);
        }

        /// <summary>
        /// Loads the configuration from a JSON file.
        /// </summary>
        /// <param name="jsonPath">The path to the JSON file.</param>
        public void LoadConfigurationFromJson(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize<WallyEnvironment>(json);
            if (config != null)
            {
                TopFilePath = config.TopFilePath;
                FilePaths = config.FilePaths;
            }
        }

        /// <summary>
        /// Adds an Actor to the environment.
        /// </summary>
        /// <param name="Actor">The Actor to add.</param>
        public void AddActor(Actor Actor)
        {
            Actors.Add(Actor);
        }

        /// <summary>
        /// Runs all Actors on the given prompt and collects responses.
        /// </summary>
        /// <param name="prompt">The input prompt.</param>
        /// <returns>A list of responses from Actors that returned text.</returns>
        public List<string> RunActors(string prompt)
        {
            var responses = new List<string>();
            foreach (var Actor in Actors)
            {
                string response = Actor.Act(prompt);
                if (response != null)
                {
                    responses.Add($"{Actor.GetType().Name}: {response}");
                }
            }
            return responses;
        }

        /// <summary>
        /// Gets an Actor by type.
        /// </summary>
        /// <typeparam name="T">The type of Actor.</typeparam>
        /// <returns>The first Actor of the specified type, or null.</returns>
        public T GetActor<T>() where T : Actor
        {
            return Actors.Find(a => a is T) as T;
        }

        /// <summary>
        /// Loads default Actors from a JSON file and adds them to the environment.
        /// Creates WallyActor instances for each combination of Role, AcceptanceCriteria, and Intent.
        /// </summary>
        /// <param name="jsonPath">The path to the JSON file.</param>
        public void LoadDefaultActors(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<DefaultActorsData>(json);
            if (data != null)
            {
                foreach (var role in data.Roles)
                {
                    foreach (var criteria in data.AcceptanceCriterias)
                    {
                        foreach (var intent in data.Intents)
                        {
                            var Actor = new WallyActor(role, criteria, intent);
                            AddActor(Actor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads the entire Wally environment from a workspace folder.
        /// Sets the top file path, loads default configuration and Actors from Wally.Default, and scans for relevant files.
        /// </summary>
        /// <param name="workspacePath">The path to the workspace folder.</param>
        public void LoadFromWorkspace(string workspacePath)
        {
            // Set top file path
            TopFilePath = workspacePath;

            // Set folder paths
            DocumentationFolder = Path.Combine(workspacePath, "Documentation");
            WorkingFolder = Path.Combine(workspacePath, "Working");
            CompletedDocumentationFolder = Path.Combine(workspacePath, "CompletedDocumentation");

            // Ensure folders exist
            EnsureFoldersExist();

            // Load default configuration if exists
            string configPath = Path.Combine(workspacePath, "Wally.Default", "default-configuration.json");
            if (File.Exists(configPath))
            {
                LoadConfigurationFromJson(configPath);
            }

            // Load default Actors if exists
            string ActorsPath = Path.Combine(workspacePath, "Wally.Default", "default-Actors.json");
            if (File.Exists(ActorsPath))
            {
                LoadDefaultActors(ActorsPath);
            }

            // Scan for .cs files in the workspace and add to file paths
            if (Directory.Exists(workspacePath))
            {
                var csFiles = Directory.GetFiles(workspacePath, "*.cs", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(workspacePath, f))
                    .ToList();
                FilePaths.AddRange(csFiles);
            }
        }

        /// <summary>
        /// Saves the Wally environment to a workspace folder.
        /// Creates the Wally.Default folder and saves the configuration to JSON.
        /// </summary>
        /// <param name="workspacePath">The path to the workspace folder.</param>
        public void SaveToWorkspace(string workspacePath)
        {
            // Ensure Wally.Default directory exists
            string defaultDir = Path.Combine(workspacePath, "Wally.Default");
            Directory.CreateDirectory(defaultDir);

            // Save configuration to JSON
            string configPath = Path.Combine(defaultDir, "default-configuration.json");
            string configJson = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, configJson);

            // Note: Actors are typically loaded from defaults, so not saved here. Custom Actors could be added later.
        }

        /// <summary>
        /// Creates a new Wally workspace by copying the default structure to the specified path.
        /// </summary>
        /// <param name="newWorkspacePath">The path for the new workspace.</param>
        public static void CreateDefaultWorkspace(string newWorkspacePath)
        {
            // Create Wally.Default directory
            string defaultDir = Path.Combine(newWorkspacePath, "Wally.Default");
            Directory.CreateDirectory(defaultDir);

            // Create default configuration JSON
            string configPath = Path.Combine(defaultDir, "default-configuration.json");
            string defaultConfig = @"{
  ""TopFilePath"": """",
  ""FilePaths"": []
}";
            File.WriteAllText(configPath, defaultConfig);

            // Create default Actors JSON
            string ActorsPath = Path.Combine(defaultDir, "default-Actors.json");
            string defaultActors = @"{
  ""Roles"": [
    {
      ""Name"": ""Developer"",
      ""Prompt"": ""Act as an expert software developer, writing clean and efficient code."",
      ""Tier"": ""task""
    },
    {
      ""Name"": ""Tester"",
      ""Prompt"": ""Act as a QA tester, identifying bugs and ensuring functionality."",
      ""Tier"": ""task""
    }
  ],
  ""AcceptanceCriterias"": [
    {
      ""Name"": ""CodeQuality"",
      ""Prompt"": ""Code must compile without errors, follow best practices, and pass unit tests."",
      ""Tier"": ""task""
    },
    {
      ""Name"": ""UserSatisfaction"",
      ""Prompt"": ""The output must meet user requirements and be user-friendly."",
      ""Tier"": ""story""
    }
  ],
  ""Intents"": [
    {
      ""Name"": ""ImplementFeature"",
      ""Prompt"": ""Implement the requested feature with proper error handling."",
      ""Tier"": ""task""
    },
    {
      ""Name"": ""FixBug"",
      ""Prompt"": ""Identify and fix the reported bug."",
      ""Tier"": ""task""
    }
  ]
}";
            File.WriteAllText(ActorsPath, defaultActors);

            // Create basic project structure
            // Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Wally.Console"));
            // Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Wally.Core"));

            // Create Wally environment folders
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Documentation"));
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Working"));
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Working", "Pending"));
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Working", "Active"));
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Working", "Completed"));
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "CompletedDocumentation"));
        }

        /// <summary>
        /// Loads and returns a default WallyEnvironment from the current workspace or default location.
        /// </summary>
        /// <returns>A WallyEnvironment loaded with defaults.</returns>
        public static WallyEnvironment LoadDefault()
        {
            var env = new WallyEnvironment();
            string workspacePath = Directory.GetCurrentDirectory();
            env.LoadFromWorkspace(workspacePath);
            return env;
        }

        /// <summary>
        /// Copies a directory recursively.
        /// </summary>
        /// <param name="sourceDir">The source directory.</param>
        /// <param name="destDir">The destination directory.</param>
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        /// <summary>
        /// Creates a Todo app by copying the default TodoApp to the specified path.
        /// </summary>
        /// <param name="targetPath">The path where to create the Todo app.</param>
        public void CreateTodoApp(string targetPath)
        {
            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "Wally.Default", "TodoApp");
            CopyDirectory(sourcePath, targetPath);
            CodeDirectory = targetPath;
            Console.WriteLine($"Todo app created at {targetPath}.");
        }

        /// <summary>
        /// Creates a Weather app by copying the default WeatherApp to the specified path.
        /// </summary>
        /// <param name="targetPath">The path where to create the Weather app.</param>
        public void CreateWeatherApp(string targetPath)
        {
            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "Wally.Default", "WeatherApp");
            CopyDirectory(sourcePath, targetPath);
            CodeDirectory = targetPath;
            Console.WriteLine($"Weather app created at {targetPath}.");
        }
    }
}