using System;
using System.IO;
using System.Linq;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        public static bool HandleDiagram(
            WallyEnvironment env,
            string targetType,
            string? primaryName = null,
            string? secondaryName = null,
            string outputFormat = "png",
            string? outputPath = null)
        {
            if (RequireWorkspace(env, "diagram") == null)
                return false;

            try
            {
                MermaidDiagramDefinition definition = ResolveDefinition(env, targetType, primaryName, secondaryName);
                MermaidDiagramRenderResult result = MermaidDiagramService.Render(env, definition, outputFormat, outputPath);

                Console.WriteLine($"Generated {definition.TargetKind} diagram: {result.OutputPath}");
                Console.WriteLine($"Mermaid source: {result.MermaidFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                env.Logger.LogError($"Diagram generation failed: {ex.Message}", "diagram");
                Console.WriteLine($"Diagram generation failed: {ex.Message}");
                return false;
            }
        }

        private static MermaidDiagramDefinition ResolveDefinition(
            WallyEnvironment env,
            string targetType,
            string? primaryName,
            string? secondaryName)
        {
            string normalizedTarget = (targetType ?? string.Empty).Trim().ToLowerInvariant();
            return normalizedTarget switch
            {
                "workspace" => MermaidDiagramService.BuildWorkspaceDefinition(env.Workspace!),
                "loop" => MermaidDiagramService.BuildLoopDefinition(ResolveLoop(env, primaryName)),
                "runbook" => MermaidDiagramService.BuildRunbookDefinition(ResolveRunbook(env, primaryName)),
                "step" => ResolveStepDefinition(env, primaryName, secondaryName),
                _ => throw new InvalidOperationException(
                    "Diagram target must be workspace, loop, step, or runbook.")
            };
        }

        private static WallyLoopDefinition ResolveLoop(WallyEnvironment env, string? loopName)
        {
            if (string.IsNullOrWhiteSpace(loopName))
                throw new InvalidOperationException("A loop name is required for loop diagrams.");

            return env.GetLoop(loopName)
                ?? throw new InvalidOperationException($"Loop '{loopName}' was not found.");
        }

        private static WallyRunbook ResolveRunbook(WallyEnvironment env, string? runbookName)
        {
            if (string.IsNullOrWhiteSpace(runbookName))
                throw new InvalidOperationException("A runbook name is required for runbook diagrams.");

            return env.GetRunbook(runbookName)
                ?? throw new InvalidOperationException($"Runbook '{runbookName}' was not found.");
        }

        private static MermaidDiagramDefinition ResolveStepDefinition(
            WallyEnvironment env,
            string? loopName,
            string? stepIdentifier)
        {
            WallyLoopDefinition loop = ResolveLoop(env, loopName);
            if (!loop.HasSteps)
                throw new InvalidOperationException($"Loop '{loop.Name}' has no explicit steps.");
            if (string.IsNullOrWhiteSpace(stepIdentifier))
                throw new InvalidOperationException("A step name or 1-based index is required for step diagrams.");

            int stepIndex = ResolveStepIndex(loop, stepIdentifier);
            return MermaidDiagramService.BuildLoopStepDefinition(loop, stepIndex);
        }

        private static int ResolveStepIndex(WallyLoopDefinition loop, string stepIdentifier)
        {
            if (int.TryParse(stepIdentifier, out int oneBasedIndex))
            {
                int zeroBasedIndex = oneBasedIndex - 1;
                if (zeroBasedIndex >= 0 && zeroBasedIndex < loop.Steps.Count)
                    return zeroBasedIndex;
            }

            int byName = loop.Steps.FindIndex(step =>
                string.Equals(step.Name, stepIdentifier, StringComparison.OrdinalIgnoreCase));
            if (byName >= 0)
                return byName;

            string available = string.Join(", ", loop.Steps.Select((step, index) =>
                $"{index + 1}:{(string.IsNullOrWhiteSpace(step.Name) ? $"step-{index + 1}" : step.Name)}"));
            throw new InvalidOperationException(
                $"Step '{stepIdentifier}' was not found in loop '{loop.Name}'. Available: {available}");
        }
    }
}