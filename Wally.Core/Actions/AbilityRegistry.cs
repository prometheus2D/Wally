using System;
using System.Collections.Generic;

namespace Wally.Core.Actions
{
    /// <summary>
    /// Static registry of built-in abilities that are shared across multiple actors.
    /// <para>
    /// An <b>ability</b> is a named <see cref="ActorAction"/> whose schema (parameters,
    /// path pattern, mutability) is canonical and defined once here.  Actors opt in to
    /// an ability by listing its name in their <c>"abilities"</c> array in
    /// <c>actor.json</c>, rather than re-declaring the full action inline.
    /// </para>
    /// <para>
    /// Actors may override only the <see cref="ActorAction.Description"/> of a registered
    /// ability by also including a matching entry in their <c>"actions"</c> array — all
    /// other fields (parameters, pathPattern, isMutating) are locked by the registry and
    /// cannot be overridden per-actor.
    /// </para>
    /// <h3>Registration rules:</h3>
    /// <list type="bullet">
    ///   <item>Only abilities that are shared across two or more actors belong here.</item>
    ///   <item>Role-exclusive actions (e.g. <c>change_code</c>) stay fully defined in
    ///         <c>actor.json</c> — they are not registered here.</item>
    ///   <item>Adding a new shared ability here automatically makes it available to any
    ///         actor that lists it in its <c>"abilities"</c> array.</item>
    /// </list>
    /// </summary>
    public static class AbilityRegistry
    {
        // ?? Registry ??????????????????????????????????????????????????????????

        private static readonly Dictionary<string, ActorAction> _registry =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // ?? read_context ??????????????????????????????????????????????
                // Universal read ability. Every actor can inspect files before acting.
                // Read-only: no mutations, no path restrictions.
                ["read_context"] = new ActorAction
                {
                    Name        = "read_context",
                    Description = "Read any file in the workspace for context before acting. " +
                                  "Use to inspect existing documents, source files, or templates " +
                                  "before writing, to avoid overwriting work or duplicating effort.",
                    PathPattern = "**",
                    IsMutating  = false,
                    Parameters  =
                    [
                        new ActionParameter
                        {
                            Name        = "path",
                            Type        = "string",
                            Description = "Relative path from WorkSource root",
                            Required    = true
                        }
                    ]
                },

                // ?? browse_workspace ??????????????????????????????????????????
                // Universal directory listing. Every actor can discover workspace structure.
                // Read-only: no mutations, no path parameter (uses directory).
                ["browse_workspace"] = new ActorAction
                {
                    Name        = "browse_workspace",
                    Description = "List all files in a directory to discover the workspace structure, " +
                                  "locate existing documents, or verify what has already been written. " +
                                  "Use before writing to avoid duplication.",
                    PathPattern = null,
                    IsMutating  = false,
                    Parameters  =
                    [
                        new ActionParameter
                        {
                            Name        = "directory",
                            Type        = "string",
                            Description = "Relative path from WorkSource root (use '.' for root)",
                            Required    = false
                        }
                    ]
                },

                // ?? send_message ??????????????????????????????????????????????
                // Universal mailbox ability. Every actor can queue a message for another actor.
                // Mutating: writes a file to the target actor's Inbox/.
                // See Docs/MailboxSystemArchitecture.md for the full protocol.
                ["send_message"] = new ActorAction
                {
                    Name        = "send_message",
                    Description = "Send a message to another actor's Inbox to request a handoff, " +
                                  "review, or collaboration. See Docs/MailboxSystemArchitecture.md " +
                                  "for the full mailbox protocol, routing policy, and message format.",
                    PathPattern = null,
                    IsMutating  = true,
                    Parameters  =
                    [
                        new ActionParameter
                        {
                            Name        = "to",
                            Type        = "string",
                            Description = "Target actor name — must be a loaded actor in this workspace",
                            Required    = true
                        },
                        new ActionParameter
                        {
                            Name        = "subject",
                            Type        = "string",
                            Description = "Short topic identifier, PascalCase or hyphenated, " +
                                          "e.g. FeasibilityCheck or CodeReview-AuthModule",
                            Required    = true
                        },
                        new ActionParameter
                        {
                            Name        = "body",
                            Type        = "string",
                            Description = "Message body as free-text Markdown — " +
                                          "the full prompt the target actor will receive",
                            Required    = true
                        },
                        new ActionParameter
                        {
                            Name        = "replyTo",
                            Type        = "string",
                            Description = "Actor name to receive the response. " +
                                          "Defaults to the sending actor if omitted.",
                            Required    = false
                        }
                    ]
                }
            };

        // ?? Public API ????????????????????????????????????????????????????????

        /// <summary>
        /// Returns the canonical <see cref="ActorAction"/> for the given ability name,
        /// or <see langword="null"/> if the name is not registered.
        /// Lookup is case-insensitive.
        /// </summary>
        public static ActorAction? TryGet(string name) =>
            _registry.TryGetValue(name, out var ability) ? ability : null;

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="name"/> is a registered
        /// ability. Lookup is case-insensitive.
        /// </summary>
        public static bool IsRegistered(string name) =>
            _registry.ContainsKey(name);

        /// <summary>
        /// Returns all registered ability names, in registration order.
        /// </summary>
        public static IEnumerable<string> AllNames => _registry.Keys;

        /// <summary>
        /// Resolves a list of ability names into their canonical <see cref="ActorAction"/>
        /// definitions, optionally applying a description override from
        /// <paramref name="descriptionOverrides"/> when an actor wants custom wording.
        /// <para>
        /// Names that are not registered are silently skipped (logged via
        /// <paramref name="onUnknown"/> when provided).
        /// </para>
        /// </summary>
        /// <param name="abilityNames">The ability names declared in <c>actor.json "abilities"</c>.</param>
        /// <param name="descriptionOverrides">
        /// Optional dictionary of name ? description override strings.
        /// Sourced from matching entries in the actor's <c>"actions"</c> array.
        /// </param>
        /// <param name="onUnknown">Optional callback invoked for each unrecognised name.</param>
        public static List<ActorAction> Resolve(
            IEnumerable<string>              abilityNames,
            IDictionary<string, string>?     descriptionOverrides = null,
            Action<string>?                  onUnknown            = null)
        {
            var result = new List<ActorAction>();

            foreach (string name in abilityNames)
            {
                if (!_registry.TryGetValue(name, out var canonical))
                {
                    onUnknown?.Invoke(name);
                    continue;
                }

                // Clone so per-actor overrides don't mutate the shared registry entry.
                var resolved = Clone(canonical);

                if (descriptionOverrides != null &&
                    descriptionOverrides.TryGetValue(name, out string? overrideDesc) &&
                    !string.IsNullOrWhiteSpace(overrideDesc))
                {
                    resolved.Description = overrideDesc;
                }

                result.Add(resolved);
            }

            return result;
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        /// <summary>
        /// Shallow-clones an <see cref="ActorAction"/> so that per-actor description
        /// overrides do not mutate the shared registry instance.
        /// Parameters are shared by reference — they are never mutated at runtime.
        /// </summary>
        private static ActorAction Clone(ActorAction source) =>
            new()
            {
                Name        = source.Name,
                Description = source.Description,
                PathPattern = source.PathPattern,
                IsMutating  = source.IsMutating,
                Parameters  = source.Parameters   // parameters are read-only — safe to share
            };
    }
}
