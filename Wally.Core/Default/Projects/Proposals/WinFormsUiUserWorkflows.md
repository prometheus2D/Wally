# WinForms UI User Workflows

**Status**: Draft  
**Audience**: Business analysts, product owners, UX planners  
**Purpose**: Describe how a user experiences the current desktop UI in practical workflow terms, without focusing on implementation details.

---

## Overview

The desktop application supports several overlapping user workflows rather than one fixed path. Users can move between:
- workspace setup and opening
- browsing and inspection
- editing and review
- AI chat interaction
- terminal-driven execution
- logs/history/diagram review
- workspace maintenance and recovery
- preferences and personalization

The workflows below describe how the UI is used in practice.

---

## Workflow 1: First-Time User Starts the Application

### Goal
Understand what the application is and how to begin.

### User experience
1. The application opens to a welcome screen.
2. The center of the screen explains how to get started.
3. The user sees clear options to:
   - open an existing workspace
   - set up a new workspace
4. The terminal is also visible as an alternative entry point.

### UI surfaces involved
- welcome screen
- File menu
- setup/open actions
- terminal panel

### Outcome
The user understands that the application is workspace-based and can either open or create a workspace.

---

## Workflow 2: User Creates a New Workspace

### Goal
Create a new working environment from the UI.

### User experience
1. The user chooses **Setup New Workspace**.
2. A setup dialog appears.
3. The dialog offers:
   - use current directory
   - choose another folder
4. The user confirms the location.
5. The application transitions into a loaded workspace state.
6. The welcome screen changes from onboarding to workspace summary.
7. The explorer and chat panels become available.

### UI surfaces involved
- File menu or toolbar
- setup dialog
- welcome screen
- explorer panel
- chat panel
- status bar

### Outcome
The user now has a ready workspace and can begin browsing, chatting, editing, or using the terminal.

---

## Workflow 3: User Opens an Existing Workspace

### Goal
Resume work in an existing environment.

### User experience
1. The user chooses **Open Workspace** or selects a recent workspace.
2. The workspace loads.
3. The left explorer becomes populated.
4. The chat panel becomes active.
5. The status bar updates to show workspace information.
6. The welcome screen changes to a workspace summary.

### UI surfaces involved
- File menu
- recent workspaces
- toolbar open actions
- explorer region
- chat panel
- status bar

### Outcome
The user is returned to an operational workspace and can continue work immediately.

---

## Workflow 4: User Reopens a Recent Workspace

### Goal
Return to a frequently used workspace with minimal effort.

### User experience
1. The user opens the recent workspaces list from the File menu or toolbar.
2. They review the available recent entries.
3. They select a workspace from the list.
4. If an entry is no longer valid, it appears unavailable rather than behaving like a normal option.
5. The workspace loads and the main workbench becomes active.

### UI surfaces involved
- File menu
- recent workspaces dropdown
- toolbar recent-workspace dropdown

### Outcome
The user can resume prior work quickly without browsing for the folder again.

---

## Workflow 5: User Browses the Workspace as Files

### Goal
Inspect the workspace in a familiar file/folder way.

### User experience
1. The user opens the **Files** explorer tab.
2. They expand folders and inspect files.
3. They can jump to the workspace folder area quickly.
4. They can double-click a file to open it.
5. They can right-click for actions such as:
   - open
   - edit as text
   - open with system
   - open containing folder
   - copy path

### UI surfaces involved
- left explorer region
- Files tab
- context menus
- center tabbed area

### Outcome
The user can navigate the workspace like a file browser and open content for reading or editing.

---

## Workflow 6: User Browses the Workspace as Active Work

### Goal
Understand what work is in progress rather than just where files live.

### User experience
1. The user opens the **Project** explorer tab.
2. They browse actors and their work areas.
3. They inspect mailbox-style folders such as inbox, active, pending, and outbox.
4. They browse project hierarchy and workspace docs.
5. They open relevant files from this workflow-oriented view.

### UI surfaces involved
- left explorer region
- Project tab
- center tabbed area

### Outcome
The user sees the workspace as an operating environment, not just a directory tree.

---

## Workflow 7: User Browses the Workspace as Definitions and Capabilities

### Goal
Understand the configured objects that define behavior.

### User experience
1. The user opens the **Objects** explorer tab.
2. They inspect categories such as:
   - actors
   - loops
   - wrappers
   - runbooks
3. They expand items to see summaries and child details.
4. They double-click an object to open its editor.
5. They may request a diagram for a loop, runbook, or loop step.

### UI surfaces involved
- left explorer region
- Objects tab
- center tabbed area
- diagram viewer

### Outcome
The user understands the workspace in conceptual terms and can move directly into editing or visual review.

---

## Workflow 8: User Opens and Reviews Multiple Tabs

### Goal
Work across several documents or editors at once.

### User experience
1. The user opens files, editors, and viewers from menus, toolbars, or explorers.
2. Each item opens in the center tabbed area.
3. The user switches between tabs as needed.
4. Some tabs are editable; others are read-only viewers.
5. Dirty tabs show unsaved state.
6. The user can close one tab or close all tabs.

### UI surfaces involved
- center tabbed document area
- explorers
- menus/toolbars

### Outcome
The user can compare and manage multiple work surfaces without losing context.

---

## Workflow 9: User Edits Structured Workspace Definitions

### Goal
Modify workspace entities through dedicated editors.

### User experience
1. The user opens an actor, loop, wrapper, runbook, or config item.
2. A dedicated editor tab opens.
3. The user reviews and changes the content.
4. The tab reflects unsaved state when modified.
5. The user saves the changes.

### UI surfaces involved
- Workspace menu
- toolbar shortcuts
- Objects explorer
- center tabbed area

### Outcome
The user edits workspace definitions in a structured way rather than only editing raw files.

---

## Workflow 10: User Edits an Actor Definition

### Goal
Refine an actor’s role and guidance.

### User experience
1. The user opens an actor from the Workspace menu, toolbar, or Objects explorer.
2. The actor editor tab shows the actor name and editable sections.
3. The user updates role, acceptance criteria, or intent.
4. The editor shows unsaved state.
5. The user saves or reverts.

### UI surfaces involved
- actor editor
- Objects explorer
- Workspace menu
- center tabbed area

### Outcome
The user can maintain actor behavior through a focused editor rather than editing raw JSON manually.

---

## Workflow 11: User Edits a Loop Definition

### Goal
Review or refine a loop’s identity and visible structure.

### User experience
1. The user opens a loop from the Workspace menu or Objects explorer.
2. The loop editor tab shows loop name, description, actor, start prompt, and visible steps.
3. The user reviews the loop structure.
4. They may request a loop diagram or a selected step diagram.
5. They save or revert changes.

### UI surfaces involved
- loop editor
- Objects explorer
- diagram viewer
- center tabbed area

### Outcome
The user can inspect and maintain loop definitions in a structured editing experience.

---

## Workflow 12: User Edits a Wrapper Definition

### Goal
Review or refine how an AI wrapper is represented in the workspace.

### User experience
1. The user opens a wrapper from the Workspace menu or Objects explorer.
2. The wrapper editor shows identity, description, executable, argument template, and behavior flags.
3. The user updates the wrapper definition.
4. The editor shows unsaved state.
5. The user saves or reverts.

### UI surfaces involved
- wrapper editor
- Objects explorer
- center tabbed area

### Outcome
The user can maintain wrapper definitions through a focused form-based editor.

---

## Workflow 13: User Edits a Runbook

### Goal
Review or update a predefined workflow script.

### User experience
1. The user opens a runbook from the Workspace menu or Objects explorer.
2. The runbook editor opens in a dedicated tab.
3. The user edits the runbook content directly.
4. They may request a diagram for the runbook.
5. They save or revert changes.

### UI surfaces involved
- runbook editor
- Objects explorer
- diagram viewer
- center tabbed area

### Outcome
The user can maintain repeatable workflows in a dedicated editing experience.

---

## Workflow 14: User Edits Workspace Configuration

### Goal
Adjust workspace-level defaults and settings.

### User experience
1. The user opens the configuration editor from the Workspace menu or toolbar.
2. The editor shows folder names, available defaults, selected defaults, resolved defaults, and runtime settings.
3. The user updates configuration values.
4. The editor shows unsaved state.
5. The user saves or reverts.

### UI surfaces involved
- config editor
- Workspace menu
- toolbar shortcut
- center tabbed area

### Outcome
The user can manage workspace-wide settings in a structured form rather than editing the config file directly.

---

## Workflow 15: User Edits a Raw File Directly

### Goal
Make direct text changes to a file.

### User experience
1. The user selects a file from an explorer.
2. They choose **Edit as Text** or open a text-editable file directly.
3. The file opens in a text editor tab.
4. The user edits the content.
5. The tab shows unsaved state.
6. The user saves the file.

### UI surfaces involved
- Files explorer or Project explorer
- center tabbed area
- save commands/shortcuts

### Outcome
The user can bypass structured editors when direct file editing is preferred.

---

## Workflow 16: User Uses the AI Chat in Ask Mode

### Goal
Get read-only AI assistance while keeping the workspace visible.

### User experience
1. The user goes to the chat panel.
2. They select **Ask** mode.
3. They optionally choose actor, loop, and model.
4. They type a message.
5. They send the request.
6. The conversation appears as chat bubbles.
7. The user can continue the conversation or inspect prompt details.

### UI surfaces involved
- chat panel
- actor/loop/model selectors
- message area
- prompt preview features

### Outcome
The user gets conversational assistance without leaving the main workbench.

---

## Workflow 17: User Uses the AI Chat in Agent Mode

### Goal
Use the AI in a more action-oriented mode.

### User experience
1. The user switches the chat panel to **Agent** mode.
2. They choose actor, loop, and model if needed.
3. They send a request.
4. The panel shows progress and responses.
5. The user can stop the run if needed.
6. The user can inspect prompt previews or diagrams.

### UI surfaces involved
- chat panel
- mode toggle
- selectors
- stop control
- prompt/diagram actions

### Outcome
The user uses the chat panel as an execution-oriented assistant rather than only a conversational helper.

---

## Workflow 18: User Runs Chat Automatically

### Goal
Let the selected chat flow run through without manual stepping.

### User experience
1. The user selects **Auto** pacing.
2. They send a request.
3. The chat panel runs the flow automatically.
4. Results appear in the conversation area.
5. Completion or system messages summarize the outcome.

### UI surfaces involved
- chat panel
- auto/manual controls
- message area
- status line

### Outcome
The user gets a streamlined conversational execution experience.

---

## Workflow 19: User Steps Through Chat Manually

### Goal
Inspect and control each step of a guided chat flow.

### User experience
1. The user selects **Manual** pacing.
2. They send a request.
3. The session starts in manual mode.
4. The user uses **Next** to advance one step at a time.
5. They can inspect:
   - current prompt preview
   - next prompt preview
   - execution diagram
6. They continue until the session completes.

### UI surfaces involved
- chat panel
- manual pacing controls
- next-step button
- prompt preview buttons
- diagram button

### Outcome
The user gets a controlled, inspectable step-by-step experience.

---

## Workflow 20: User Clears or Reviews Chat History

### Goal
Manage live and stored conversation history.

### User experience
1. In the chat panel, the user can clear visible conversation bubbles.
2. They can also clear persisted history.
3. If they want deeper review, they open the **Chat History Viewer**.
4. In that viewer they can:
   - browse turns
   - filter by actor
   - inspect prompt/response details
   - copy details
   - clear all stored history

### UI surfaces involved
- chat panel
- toolbar clear chat action
- chat history viewer

### Outcome
The user can manage both immediate conversation clutter and long-term stored history.

---

## Workflow 21: User Reviews Prompt Details

### Goal
Understand what the system is sending or planning to send.

### User experience
1. The user opens the prompt viewer or a prompt preview tab.
2. They inspect:
   - resolved selections
   - execution state
   - equivalent command form
   - conversation history contribution
   - final prompt content
3. In manual chat sessions, they may inspect the next prompt before continuing.

### UI surfaces involved
- chat panel preview actions
- prompt viewer tabs

### Outcome
The user gains transparency into the conversational workflow.

---

## Workflow 22: User Uses the Prompt Viewer as a Standalone Inspection Tool

### Goal
Inspect prompt construction outside an active chat run.

### User experience
1. The user opens the Prompt Viewer from the View menu.
2. They choose mode, actor, loop, model, and wrapper context.
3. They enter a sample prompt.
4. They build the preview.
5. They review prompt construction details and the exact prompt.
6. They may copy the details or exact prompt.

### UI surfaces involved
- View menu
- prompt viewer tab
- center tabbed area

### Outcome
The user can inspect prompt construction as a standalone analysis activity.

---

## Workflow 23: User Uses the Terminal for Direct Commands

### Goal
Run direct commands without leaving the application.

### User experience
1. The user types a command into the terminal input.
2. Output appears in the terminal output area.
3. The user can use command history and tab completion.
4. They can stop a running command if needed.
5. They can save terminal output for later reference.

### UI surfaces involved
- terminal panel
- terminal header actions
- terminal context menu

### Outcome
The user can perform direct operational work inside the same application shell.

---

## Workflow 24: User Uses Terminal Productivity Features

### Goal
Work efficiently in the integrated terminal.

### User experience
1. The user enters commands repeatedly.
2. They use up/down history to revisit prior commands.
3. They use tab completion to reduce typing.
4. They use the terminal context menu to copy, select all, save output, or clear output.
5. They use the help button for quick command discovery.

### UI surfaces involved
- terminal panel
- terminal input
- terminal output
- terminal context menu
- terminal header buttons

### Outcome
The user can treat the terminal as a practical daily-use command console.

---

## Workflow 25: User Runs a Predefined Runbook

### Goal
Execute a predefined workflow quickly from the UI.

### User experience
1. The user selects a runbook from the runbook toolbar dropdown.
2. They click start.
3. Output appears in the terminal.
4. They can stop the run if needed.

### UI surfaces involved
- runbook toolbar
- terminal panel

### Outcome
The user can launch repeatable workflows without typing the command manually.

---

## Workflow 26: User Reviews Logs

### Goal
Inspect operational history and prior activity.

### User experience
1. The user opens the **Logs** viewer from the menu or toolbar.
2. A logs tab opens in the center area.
3. The user reviews available log information.
4. They refresh as needed.

### UI surfaces involved
- View menu
- toolbar shortcut
- logs viewer tab

### Outcome
The user can review prior activity in a dedicated inspection surface.

---

## Workflow 27: User Reviews Workspace Summary

### Goal
See the workspace as a whole in one place.

### User experience
1. The user opens the **Workspace Viewer**.
2. A summary tab opens.
3. The user reviews sections such as:
   - workspace identity
   - folder structure
   - defaults
   - actors
   - loops
   - wrappers
   - runbooks
   - docs/templates
   - session info
   - disk usage
4. They may copy the summary or open a workspace diagram.

### UI surfaces involved
- View menu
- workspace viewer tab
- diagram action

### Outcome
The user gets a single-pane overview of the entire workspace.

---

## Workflow 28: User Reviews Diagrams

### Goal
Understand structures visually.

### User experience
1. The user opens a diagram from chat, object explorer, loop editor, runbook editor, or workspace viewer.
2. A diagram tab opens.
3. The user can:
   - zoom in/out
   - fit to window
   - reset to actual size
   - refresh
   - save the diagram
   - open the output folder
4. Supporting details are visible below the diagram.

### UI surfaces involved
- diagram viewer
- object explorer
- chat panel
- workspace viewer
- editors

### Outcome
The user can review workflows and structures visually rather than only as text.

---

## Workflow 29: User Verifies or Repairs a Workspace

### Goal
Check or restore workspace readiness.

### User experience
1. The user chooses **Verify Workspace** or **Repair Workspace**.
2. Repair includes a confirmation step.
3. The action runs through the terminal-oriented workflow.
4. The explorer and related panels can then be refreshed.

### UI surfaces involved
- Workspace menu
- workspace operations toolbar
- terminal panel
- explorer region

### Outcome
The user can maintain workspace health from the UI.

---

## Workflow 30: User Cleans Up a Workspace

### Goal
Remove the current workspace intentionally.

### User experience
1. The user chooses **Cleanup Workspace**.
2. A warning dialog explains the impact.
3. If confirmed, the workspace is removed.
4. The UI returns to a no-workspace state.
5. Workspace-specific tabs and panels are cleared or hidden.

### UI surfaces involved
- Workspace menu
- confirmation dialog
- explorer/chat/tab areas
- welcome screen

### Outcome
The user can reset the environment and start fresh if needed.

---

## Workflow 31: User Opens the Workspace Folder Externally

### Goal
Move from the application into the operating system’s file explorer.

### User experience
1. The user chooses **Open in Explorer** from the Workspace menu or a file/folder context menu.
2. The operating system file explorer opens to the relevant location.
3. The user continues work outside the application if needed.

### UI surfaces involved
- Workspace menu
- file explorer context menu
- project explorer context menu

### Outcome
The user can bridge between the application and the operating system easily.

---

## Workflow 32: User Customizes the Visible Workbench

### Goal
Adjust the UI to fit the current task.

### User experience
1. The user uses the **View** menu to show or hide:
   - explorer
   - chat
   - command panel
2. They resize panels using splitters.
3. They keep only the surfaces needed for the current task.

### UI surfaces involved
- View menu
- splitters
- main layout panels

### Outcome
The user can simplify or expand the workbench depending on whether they are browsing, editing, chatting, or running commands.

---

## Workflow 33: User Manages User Preferences

### Goal
Adjust personal application behavior without changing workspace definitions.

### User experience
1. The user opens **User Preferences** from the File menu.
2. A preferences tab opens.
3. The user reviews settings such as:
   - last workspace path
   - auto-load last workspace
   - maximum recent workspaces
4. They save changes.

### UI surfaces involved
- File menu
- user preferences tab
- center tabbed area

### Outcome
The user can personalize application behavior separately from workspace configuration.

---

## Workflow 34: User Saves Work Across Tabs

### Goal
Persist changes safely while working across multiple tabs.

### User experience
1. The user edits one or more tabs.
2. Dirty tabs indicate unsaved changes.
3. The user saves the active tab or saves all dirty tabs using commands or shortcuts.
4. The dirty indicators clear after successful save.

### UI surfaces involved
- tabbed document area
- save commands
- keyboard shortcuts
- terminal/status feedback

### Outcome
The user can manage edits confidently across multiple open work items.

---

## Workflow 35: User Closes the Application Safely

### Goal
Exit without losing work accidentally.

### User experience
1. The user closes the application.
2. If there are unsaved tabs, the application warns them.
3. The user can:
   - save all changes
   - discard changes
   - cancel closing

### UI surfaces involved
- tabbed document area
- close confirmation dialog

### Outcome
The user is protected from accidental loss of edits.

---

## Cross-Cutting UX Patterns

Several patterns appear across many workflows.

### Pattern 1: Multiple entry points
Most actions can be reached from more than one place:
- menu
- toolbar
- explorer
- keyboard shortcut

### Pattern 2: Inspect before act
The UI often lets users inspect prompts, diagrams, history, logs, or summaries before or after taking action.

### Pattern 3: Side-by-side work
Users can browse, edit, chat, and run commands in parallel because the UI is multi-pane.

### Pattern 4: Workspace-first context
Almost every workflow assumes the workspace is the central unit of work.

### Pattern 5: Guided + direct interaction
The application supports both:
- guided interaction through menus, dialogs, and chat selectors
- direct interaction through terminal commands and raw file editing

### Pattern 6: Safe editing and recovery
The UI consistently supports:
- save/revert
- dirty-state visibility
- confirmation before destructive actions
- stop/cancel for running work
