using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wally.Core.Agents;
using Wally.Core.Agents.RBA;

namespace Wally.Core
{
    /// <summary>
    /// Represents the Wally environment that manages a collection of agents.
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
        /// The list of agents in the environment.
        /// </summary>
        [JsonIgnore]
        public List<Agent> Agents { get; set; } = new List<Agent>();

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
        /// Adds an agent to the environment.
        /// </summary>
        /// <param name="agent">The agent to add.</param>
        public void AddAgent(Agent agent)
        {
            Agents.Add(agent);
        }

        /// <summary>
        /// Runs all agents on the given prompt and collects responses.
        /// </summary>
        /// <param name="prompt">The input prompt.</param>
        /// <returns>A list of responses from agents that returned text.</returns>
        public List<string> RunAgents(string prompt)
        {
            var responses = new List<string>();
            foreach (var agent in Agents)
            {
                string response = agent.Act(prompt);
                if (response != null)
                {
                    responses.Add($"{agent.GetType().Name}: {response}");
                }
            }
            return responses;
        }

        /// <summary>
        /// Gets an agent by type.
        /// </summary>
        /// <typeparam name="T">The type of agent.</typeparam>
        /// <returns>The first agent of the specified type, or null.</returns>
        public T GetAgent<T>() where T : Agent
        {
            return Agents.Find(a => a is T) as T;
        }

        /// <summary>
        /// Loads default agents from a JSON file and adds them to the environment.
        /// Creates WallyAgent instances for each combination of Role, AcceptanceCriteria, and Intent.
        /// </summary>
        /// <param name="jsonPath">The path to the JSON file.</param>
        public void LoadDefaultAgents(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<DefaultAgentsData>(json);
            if (data != null)
            {
                foreach (var role in data.Roles)
                {
                    foreach (var criteria in data.AcceptanceCriterias)
                    {
                        foreach (var intent in data.Intents)
                        {
                            var agent = new WallyAgent(role, criteria, intent);
                            AddAgent(agent);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads the entire Wally environment from a workspace folder.
        /// Sets the top file path, loads default configuration and agents from Wally.Default, and scans for relevant files.
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

            // Load default agents if exists
            string agentsPath = Path.Combine(workspacePath, "Wally.Default", "default-agents.json");
            if (File.Exists(agentsPath))
            {
                LoadDefaultAgents(agentsPath);
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

            // Note: Agents are typically loaded from defaults, so not saved here. Custom agents could be added later.
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

            // Create default agents JSON
            string agentsPath = Path.Combine(defaultDir, "default-agents.json");
            string defaultAgents = @"{
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
            File.WriteAllText(agentsPath, defaultAgents);

            // Create basic project structure
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Wally.Console"));
            Directory.CreateDirectory(Path.Combine(newWorkspacePath, "Wally.Core"));

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
    }
}