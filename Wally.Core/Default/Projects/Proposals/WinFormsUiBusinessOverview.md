# WinForms UI Business Overview

**Status**: Draft  
**Audience**: Product owners, business analysts, workflow designers, UX planners  
**Purpose**: Describe the current desktop application as a user-facing product so its workflow, screens, navigation, and feature set can be replicated in a new application.

---

## Executive Summary

The application is a desktop workspace manager built around three simultaneous ways of working:

1. **browse and inspect workspace content**
2. **chat with the AI in a guided side panel**
3. **run commands in an integrated terminal**

The UI is designed as a multi-pane workbench rather than a wizard. Users can keep navigation, editing, chat, and terminal activity visible at the same time. The experience is intended for iterative work: open a workspace, inspect files and objects, run commands, review outputs, open editors, and continue without leaving the main window.

The application behaves like a hybrid of:
- an IDE-style desktop shell
- a workspace browser
- an AI chat console
- a command terminal
- a tabbed document editor

### Experience summary
From a business-user perspective, the application gives one place where users can open a workspace, understand it, act on it, and review results without switching tools.

---

## Primary User Experience Model

The application presents one main window with five major user-facing regions:

1. **top menu bar**
2. **top toolbars**
3. **left explorer area**
4. **center tabbed document area**
5. **right AI chat area**
6. **bottom terminal area**
7. **bottom status bar**

Not every region is always visible. The workspace-oriented panels can be shown or hidden, but the overall experience is built around these regions working together.

The user does not move through a fixed sequence. Instead, they:
- load or create a workspace
- inspect the workspace from the left side
- open documents and editors in the center
- use chat on the right for guided interaction
- use the terminal below for direct commands
- monitor state in the status bar

This creates a highly inspectable workflow where the user can switch between conversational work, structured editing, and command-driven work without changing applications.

### User expectation created by this model
Users expect the application to behave like a persistent workbench, not a sequence of isolated screens.

---

## Main Window Layout

### 1. Menu Bar

The menu bar provides the formal top-level navigation model. It exposes the application in familiar desktop terms rather than requiring users to discover everything through buttons.

The menu structure includes:
- **File**
- **Edit**
- **View**
- **Workspace**

These menus define the application’s business-facing capability areas.

### 2. Toolbars

Below the menu bar are multiple toolbars that expose common actions as one-click controls. These toolbars reduce friction for frequent tasks and make the application feel operational rather than document-only.

Toolbar groups include:
- file/workspace actions
- workspace maintenance actions
- runbook execution actions
- editor and viewer shortcuts

### 3. Left Explorer Area

The left side is a tabbed explorer region. It gives users multiple ways to understand the workspace:
- raw files
- project/workflow structure
- loaded objects and definitions

This area is intended for discovery, navigation, and context.

### 4. Center Document Area

The center is a tabbed workspace where editors and viewers open. It acts as the main reading and editing surface.

Users can have multiple tabs open at once and move between them without losing context.

### 5. Right Chat Area

The right side is an AI chat panel. It supports conversational work in either read-only or action-oriented modes and is designed to feel like an embedded assistant workspace.

### 6. Bottom Terminal Area

The bottom panel is a terminal-like command area. It supports direct command entry, command history, output review, and operational control.

### 7. Status Bar

The bottom status bar provides passive awareness:
- workspace state
- actor count
- session identity
- running/progress indication

### Layout expectation
Users expect these regions to work together as one coordinated environment and to remain available while they move between tasks.

---

## Startup Experience

When no workspace is loaded, the center area shows a **welcome screen**.

The welcome screen is not just decorative. It acts as a lightweight onboarding surface that tells the user:
- what the application is
- how to open a workspace
- how to create a new workspace
- that terminal commands can also be used

When a workspace is loaded, the welcome area changes to a workspace summary view showing:
- workspace name/path
- actor count summary
- default model summary
- guidance to use chat or terminal next

This means the landing experience changes from **getting started** to **workspace ready**.

### User expectation
Users expect the application to explain itself clearly before they know the product vocabulary.

---

## File Menu

The **File** menu is the user’s entry point for workspace lifecycle and application lifecycle tasks.

### File menu capabilities

- **Open Workspace**
  - lets the user choose an existing workspace folder
- **Setup New Workspace**
  - launches a guided setup dialog for creating a new workspace in a selected folder
- **Save Workspace**
  - saves current workspace state
- **Close Workspace**
  - unloads the current workspace and returns the UI to a no-workspace state
- **Recent Workspaces**
  - shows a list of recently used workspaces for quick reopening
- **User Settings / Preferences**
  - opens user-level preferences in a tab
- **Exit**
  - closes the application

### Business meaning of the File menu

The File menu treats the workspace as the primary business object. The user is not opening a single file first; they are opening a managed working environment.

### User expectation
Users expect File to answer the question: “How do I start, reopen, save, close, or leave this environment?”

---

## Setup Workspace Dialog

The setup dialog is a small guided modal used when creating a new workspace.

### What the dialog communicates

- a workspace will be created inside the selected directory
- the user can use the current directory or browse for another folder
- the action is framed as creating a ready-to-use environment

### User choices

- **Use Current Directory**
- **Choose Different Folder**
- **Cancel**

This is a simple, business-friendly entry point for first-time setup.

### User expectation
Users expect setup to feel safe, guided, and easy to understand.

---

## Edit Menu

The **Edit** menu is lightweight and focused on editing behavior rather than workspace operations.

### Edit menu capabilities

- copy
- select all
- word wrap toggle for document-style views

### Business meaning of the Edit menu

The Edit menu supports reading and editing comfort. It is not a full authoring ribbon; it provides universal editing conveniences that apply across viewers and text editors.

### User expectation
Users expect Edit to contain universal text and reading actions rather than workspace-specific operations.

---

## View Menu

The **View** menu controls visibility and access to major UI surfaces.

### View menu capabilities

- refresh explorer content
- show/hide explorer panel
- show/hide chat panel
- show/hide command panel
- open logs viewer
- open chat history viewer
- open prompt viewer
- open workspace viewer
- close all editor tabs

### Business meaning of the View menu

The View menu lets users shape the workbench around their current task. For example:
- a user can focus on terminal + editor work
- a user can focus on chat + workspace browsing
- a user can hide side panels for reading

This makes the application adaptable without changing its core workflow.

### User expectation
Users expect View to answer the question: “What parts of the workbench do I want visible right now?”

---

## Workspace Menu

The **Workspace** menu is the operational menu for managing workspace-defined entities and maintenance tasks.

### Workspace menu capabilities

- edit actors
- edit loops
- edit wrappers
- edit runbooks
- edit config
- reload actors
- list actors
- workspace info
- verify workspace
- repair workspace
- cleanup workspace
- open workspace folder in the system file explorer

### Business meaning of the Workspace menu

This menu groups together all actions that affect the structure, definitions, and health of the workspace. It is the administrative and maintenance hub.

### User expectation
Users expect Workspace to answer the question: “How do I inspect, maintain, and manage the environment I am working in?”

---

## Top Toolbars

The application duplicates important menu actions in toolbars for speed and visibility.

### File/workspace toolbar

Provides quick access to:
- open workspace
- setup workspace
- save workspace
- close workspace
- recent workspaces

### Workspace operations toolbar

Provides quick access to:
- refresh
- reload actors
- workspace info
- verify
- repair
- stop current operation

### Runbook toolbar

Provides a compact execution strip for:
- selecting a runbook
- starting a runbook
- stopping a runbook

### Editors/viewers toolbar

Provides shortcuts to:
- actor editing
- config editing
- logs
- clear chat

### Business meaning of the toolbars

The toolbars make the application feel like an active operations console. They expose the most common actions without requiring menu navigation.

### User expectation
Users expect the toolbar to surface the actions they perform repeatedly and to reduce friction for daily use.

---

## Left Explorer Area

The left side is a tabbed explorer with three distinct views.

## Explorer Tab 1: Files

The **Files** tab is a raw file-system-oriented view of the workspace root.

### What users can do

- browse folders and files in a tree
- expand and collapse directories
- jump quickly to the workspace folder area
- double-click files to open them
- use context menus on files and folders
- see item counts and tooltips

### File context menu features

Depending on selection, users can:
- open
- edit as text
- open with system
- open containing folder
- copy path
- refresh selected node

### Business meaning

This tab supports users who think in terms of files and folders. It is the most literal representation of the workspace.

### User expectation
Users expect this tab to behave like a practical desktop file browser inside the application.

---

## Explorer Tab 2: Project

The **Project** tab is a workflow-oriented view rather than a raw file tree.

### What it emphasizes

- actors and their working folders
- mailbox-style folders such as inbox/outbox/pending/active
- actor documentation
- project hierarchy
- workspace-level documentation

### What users can do

- inspect what is in flight
- browse actor-specific work areas
- open documents and task-related files
- use context menus similar to the file explorer

### Business meaning

This tab is designed for users who want to understand the current state of work rather than the raw disk layout. It presents the workspace as an operating environment.

### User expectation
Users expect this tab to answer questions about active work, waiting work, and work structure.

---

## Explorer Tab 3: Objects

The **Objects** tab is a definition-oriented view of loaded workspace objects.

### What it shows

- actors
- loops
- wrappers
- runbooks
- selected properties and summaries for each

### What users can do

- inspect loaded definitions
- open editors for actors, loops, wrappers, and runbooks
- request diagrams for loops, runbooks, and loop steps
- copy names

### Business meaning

This tab is for users who think in terms of configured capabilities rather than files. It is the conceptual map of the workspace.

### User expectation
Users expect this tab to let them navigate by concept rather than by storage location.

---

## Center Document Area

The center area is a **tabbed document host**.

### Core behaviors

- multiple tabs can be open at once
- tabs can be selected, closed, and revisited
- tabs show dirty/unsaved state
- tabs support close-all behavior
- the active tab changes the user’s working context

### Types of content opened in tabs

- welcome screen
- user preferences
- actor editor
- loop editor
- wrapper editor
- runbook editor
- config editor
- raw text file editor
- logs viewer
- chat history viewer
- prompt viewer
- workspace viewer
- diagram viewer

### Business meaning

The center area is the main work surface. It allows users to compare, edit, inspect, and review multiple artifacts without losing their place.

### User expectation
Users expect the center area to be where focused work remains available until they intentionally close it.

---

## Editing and Viewing Experiences in Tabs

The application supports both **structured editors** and **read-only viewers**.

### Structured editors

These are specialized screens for workspace entities such as:
- actors
- loops
- wrappers
- runbooks
- config

These editors are intended to present the entity in a business-friendly form rather than forcing the user to edit raw files.

### Raw text editing

Users can also open many file types directly as text for manual editing.

### Read-only viewers

The application includes dedicated viewers for:
- logs
- chat history
- prompt previews
- workspace summaries
- diagrams

This creates a mixed environment where some tabs are for editing and others are for inspection.

### User expectation
Users expect the application to support both guided editing and direct inspection without forcing one style.

---

## AI Chat Panel

The right-side chat panel is a major part of the application’s workflow.

### Overall purpose

It gives the user a conversational interface to the workspace while keeping the rest of the application visible.

### Main chat concepts visible to the user

- **Ask mode**
  - read-only conversational interaction
- **Agent mode**
  - action-oriented interaction that may change files
- **Auto mode**
  - run through the selected flow automatically
- **Manual mode**
  - step through the flow one step at a time

### Chat panel controls

The panel includes:
- mode toggle buttons
- clear conversation button
- clear persisted history button
- auto/manual pacing controls
- next-step button for manual progression
- prompt preview button
- next prompt preview button
- diagram button
- actor selector
- loop selector
- model selector
- message history area
- status line
- input box
- send button
- stop button

### Message area behavior

The chat area displays conversation as message bubbles with different visual styles for:
- user messages
- AI responses
- system messages
- errors

### Empty-state guidance

When no conversation exists, the panel explains:
- Ask vs Agent
- Auto vs Manual
- how actor/loop/model selection affects the request

### Business meaning

The chat panel is a guided work surface for conversational execution. It is not just a chatbot window; it is a configurable execution console.

### User expectation
Users expect the AI area to be configurable, inspectable, interruptible, and integrated with the rest of the workspace.

---

## Chat Features from a User Perspective

### Selection controls

Users can choose:
- actor
- loop
- model

These selectors make the chat experience configurable without requiring command syntax.

### Prompt preview features

Users can inspect:
- the current prompt that would be sent
- the next prompt in a manual session

### Diagram feature

Users can open a visual representation of the current chat execution setup.

### Manual stepping

In manual mode, users can:
- start a session
- execute one step at a time
- inspect what comes next
- continue until completion

### History handling

Users can:
- clear visible chat bubbles
- clear persisted conversation history
- open a dedicated chat history viewer elsewhere in the application

### User expectation
Users expect to understand and adjust the context of an AI request before sending it.

---

## Terminal Panel

The bottom terminal panel is a command-driven workspace console.

### What it provides

- command input line
- scrolling output area
- command history navigation
- tab completion support
- stop/cancel support
- save terminal output
- help shortcut
- context menu for copy/select all/save/clear

### Terminal workflow

Users can:
- type commands directly
- review command output inline
- rerun commands from history
- save terminal logs
- stop long-running operations

### Relationship to the rest of the UI

The terminal is not separate from the application. It is integrated with workspace state and can trigger updates in the rest of the UI.

### Business meaning

The terminal serves advanced and repeatable workflows. It complements the visual UI rather than replacing it.

### User expectation
Users expect the terminal to be a serious operational surface, not a toy console.

---

## Terminal Features from a User Perspective

### Input conveniences

- enter to run
- up/down history navigation
- tab completion
- escape to cancel or clear

### Output conveniences

- colored output
- auto-scroll behavior
- copy and select-all support
- save output to file

### Busy-state behavior

The terminal can be temporarily locked while another panel is running work, preventing conflicting actions.

### User expectation
Frequent users expect the terminal to reward repetition with speed and control.

---

## Logs Viewer

The logs viewer is a read-only inspection screen.

### User-facing purpose

- review session logs
- inspect operational history
- understand what happened during prior activity

### Business meaning

This is an audit and troubleshooting surface for users who need visibility into prior runs.

### User expectation
Users expect a dedicated place to review what happened during prior activity.

---

## Chat History Viewer

The chat history viewer is a dedicated screen for reviewing stored conversation turns.

### What users can do

- browse prior turns in a list
- filter by actor
- inspect prompt and response details
- copy details
- clear all stored history

### Business meaning

This viewer separates long-term conversation review from the live chat panel. It supports traceability and retrospective review.

### User expectation
Users expect conversation history to be reviewable independently from the live chat panel.

---

## Prompt Viewer and Prompt Preview Tabs

The application includes prompt-focused inspection screens.

### What users can inspect

- resolved selections
- execution state
- equivalent command form
- conversation history included in the prompt
- final prompt content
- wrapper summary
- loop step summaries

### Business meaning

These screens help users understand what the system is about to do or just did, without requiring technical debugging.

### User expectation
Users expect to inspect request context rather than simply trust that the system interpreted their choices correctly.

---

## Workspace Viewer

The workspace viewer is a comprehensive read-only summary screen.

### What it summarizes

- workspace identity
- folder structure
- resolved defaults
- available lists and selections
- actors
- loops
- wrappers
- runbooks
- documentation and templates
- session information
- disk usage

### Business meaning

This is the “single pane of glass” for workspace understanding.

### User expectation
Users expect a single place where they can orient themselves quickly without opening many separate tabs.

---

## Diagram Viewer

The diagram viewer is a visual inspection surface.

### What users can do

- open diagrams for workspace structures
- open diagrams for loops
- open diagrams for runbooks
- open diagrams for loop steps
- zoom in/out
- fit to window
- reset to actual size
- refresh
- save diagram output
- open the output folder

### Business meaning

The diagram viewer turns abstract workflow structures into visual artifacts that are easier to review and discuss.

### User expectation
Users expect diagrams to be review tools, not just decorative outputs.

---

## Runbook Execution Experience

The application includes a dedicated runbook selection and execution experience in the toolbar.

### User-facing flow

- choose a runbook from a dropdown
- start execution
- stop execution if needed
- observe output in the terminal

### Business meaning

This gives users a lightweight “run predefined workflow” capability without requiring them to type the command manually.

### User expectation
Users expect predefined workflows to feel easy to launch and easy to monitor.

---

## Workspace Lifecycle Experience in Practice

From a business user perspective, the application supports the full workspace lifecycle:

1. create a workspace
2. open a workspace
3. inspect workspace contents
4. edit workspace definitions
5. run chat or terminal workflows
6. verify or repair the workspace
7. save workspace state
8. close the workspace
9. clean up the workspace if needed

This makes the application feel like a managed desktop environment rather than a loose collection of tools.

### User expectation
Users expect the application to remain coherent across the entire lifecycle of a workspace, not just during active editing.

---

## Visibility and Panel Management

The application allows users to show or hide major panels.

### Panels that can be toggled

- explorer
- chat
- command panel

### Business meaning

Users can simplify the screen for focused work or expand it for multi-surface workflows.

### User expectation
Users expect the workbench to adapt to the task without forcing a completely different navigation model.

---

## Status Bar Experience

The status bar provides passive operational awareness.

### Information shown

- current workspace status/path
- actor count
- session identifier
- progress indicator when work is running

### Business meaning

The status bar reassures the user that the application is connected to the expected workspace and indicates when work is in progress.

### User expectation
Users expect the application to quietly confirm where they are and whether something is happening.

---

## Keyboard and Productivity Features

The application includes desktop productivity behaviors expected by frequent users.

### Examples

- save active tab
- save all dirty tabs
- focus terminal quickly
- refresh explorer
- focus explorer/chat/terminal regions
- close active tab
- escape to stop running work

### Business meaning

These features support power users and repeated daily use without changing the visible workflow model.

### User expectation
Frequent users expect the application to reward repetition with speed.

---

## Unsaved Changes Experience

The application tracks dirty tabs and warns on close.

### User-facing behavior

- tabs can show unsaved state
- closing the application prompts the user if unsaved changes exist
- the user can save all, discard, or cancel closing

### Business meaning

This protects user edits and reinforces the application’s role as a serious editing environment.

### User expectation
Users expect the application to protect them from accidental loss of work.

---

## Overall Design Character

From a business and workflow perspective, the UI is characterized by:

- **workspace-first design**
- **multi-pane visibility**
- **simultaneous chat + terminal + editor workflows**
- **strong inspectability**
- **desktop-style menus and toolbars**
- **tabbed editing and viewing**
- **support for both guided and direct interaction**

It is not a wizard, not a single-chat app, and not just a file editor. It is a desktop operations workbench.

### Experience quality to preserve
The future application should preserve the feeling that users can browse, inspect, act, and review without leaving one coordinated environment.

---

## Functional Capability Inventory

The current UI supports these broad feature categories:

### Workspace management
- create
- open
- save
- close
- reopen recent
- verify
- repair
- cleanup

### Navigation
- file browsing
- project/workflow browsing
- object browsing
- tabbed document navigation

### Editing
- structured entity editing
- raw text editing
- dirty tracking
- save/save all

### AI interaction
- ask mode
- action-oriented mode
- auto execution
- manual stepping
- prompt inspection
- diagram inspection
- actor/loop/model selection

### Command interaction
- integrated terminal
- command history
- completion
- stop/cancel
- output saving

### Inspection and reporting
- logs viewer
- chat history viewer
- prompt viewer
- workspace viewer
- diagram viewer

### Productivity and control
- panel visibility toggles
- keyboard shortcuts
- recent workspaces
- status bar awareness

---

## Reproduction Guidance for a Future Application

If this UI is rebuilt in a different engine or environment, the goal should not be pixel-perfect imitation. The goal should be preservation of user experience.

### Preserve these experience outcomes
- users can open a workspace and immediately understand where to begin
- users can browse the same workspace in multiple ways
- users can keep several work surfaces visible at once
- users can edit, inspect, chat, and run commands without leaving the main environment
- users can review history, logs, prompts, and diagrams without losing context
- users can stop, save, revert, and recover safely

### Preserve these interaction patterns
- menus for discoverability
- toolbars for speed
- explorers for navigation
- tabs for focused work
- side panels for persistent context
- status surfaces for passive awareness
- confirmations for destructive actions

### Preserve these user expectations
- the application remembers recent work
- the application distinguishes workspace settings from user preferences
- the application supports both guided and direct workflows
- the application makes AI interaction inspectable rather than opaque
- the application behaves like a serious desktop workbench

---

## Recommended Documentation Follow-On

To replicate this UI in a new application, the next documentation set should break this overview into:

1. **screen-by-screen UX specification**
2. **menu and toolbar feature inventory**
3. **panel-by-panel workflow descriptions**
4. **editor/viewer catalog**
5. **user journeys for common tasks**
6. **feature parity checklist for the new application**

---

## Summary

The current WinForms application is a desktop workspace workbench that combines:
- workspace lifecycle management
- multi-view navigation
- tabbed editing and inspection
- embedded AI chat
- embedded terminal execution
- logs/history/prompt/diagram review

Its defining strength is that all of these capabilities are available in one coordinated UI, allowing users to move fluidly between browsing, editing, chatting, and command execution without leaving the main window.
