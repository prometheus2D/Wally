# WinForms UI Feature Inventory

**Status**: Draft  
**Audience**: Product planning, UX design, business analysis  
**Purpose**: Provide a business-facing inventory of the current desktop UI’s visible features so a new application can decide what to keep, simplify, or redesign.

---

## 1. Application Shell

### Main shell features
- single main desktop window
- menu-driven navigation
- multiple toolbars
- resizable multi-pane layout
- tabbed central work area
- persistent status bar
- welcome/landing experience when no workspace is loaded

### Layout features
- left explorer region
- center document/editor region
- right AI chat region
- bottom terminal region
- bottom status bar
- splitters for resizing panels
- ability to show/hide major panels

### User expectation supported
- the application behaves like a persistent desktop workbench
- multiple work surfaces can remain visible together
- users do not need to leave the main window to switch task types

---

## 2. File Menu Features

### Workspace lifecycle
- open existing workspace
- set up new workspace
- save workspace
- close workspace
- exit application

### Convenience features
- recent workspaces list
- clear recent workspaces
- user preferences/settings access

### User expectation supported
- File is the place for starting, reopening, saving, closing, and leaving the environment

---

## 3. Edit Menu Features

- copy
- select all
- word wrap toggle for document-style views

### User expectation supported
- Edit contains universal text and reading actions rather than workspace-specific operations

---

## 4. View Menu Features

### Visibility controls
- refresh explorer
- show/hide explorer panel
- show/hide chat panel
- show/hide command panel

### Viewer access
- open logs viewer
- open chat history viewer
- open prompt viewer
- open workspace viewer

### Tab management
- close all editor tabs

### User expectation supported
- View controls what is visible and what inspection surfaces are open

---

## 5. Workspace Menu Features

### Entity editing
- edit actors
- edit loops
- edit wrappers
- edit runbooks
- edit config

### Workspace operations
- reload actors
- list actors
- workspace info
- verify workspace
- repair workspace
- cleanup workspace
- open workspace folder in system explorer

### User expectation supported
- Workspace is the place for environment-specific management and maintenance

---

## 6. Toolbar Features

### File/workspace toolbar
- open workspace
- setup workspace
- save workspace
- close workspace
- recent workspaces dropdown

### Workspace operations toolbar
- refresh
- reload actors
- workspace info
- verify
- repair
- stop current operation

### Runbook toolbar
- runbook dropdown selector
- start runbook
- stop runbook

### Editor/viewer toolbar
- open actor editing
- open config editing
- open logs
- clear chat

### User expectation supported
- high-frequency actions are available without opening menus

---

## 7. Welcome Screen Features

### No-workspace state
- branded landing card
- guidance for opening a workspace
- guidance for setting up a workspace
- reminder that terminal commands can also be used

### Workspace-loaded state
- workspace summary
- actor count summary
- default model summary
- guidance to use chat or terminal next

### User expectation supported
- the application explains itself before the user knows the product vocabulary

---

## 8. Setup Dialog Features

- modal setup dialog
- option to use current directory
- option to browse for another folder
- cancel option
- explanatory text about creating a workspace in the selected folder

### User expectation supported
- setup feels guided, safe, and easy to understand

---

## 9. Explorer Region Features

### Explorer host
- tabbed explorer area
- three explorer modes
- custom tab switching

### Explorer tabs
1. Files
2. Project
3. Objects

### User expectation supported
- the same workspace can be navigated in multiple mental models

---

## 10. Files Explorer Features

### Navigation
- browse workspace folders and files
- expand/collapse directories
- refresh tree
- collapse all
- jump to workspace folder area

### File interaction
- double-click to open
- select file to update status context
- tooltips with file details

### Context menu
- open
- edit as text
- open with system
- open containing folder
- copy path
- refresh selected node

### Presentation
- file-type icons
- folder icons
- item count summary

### User expectation supported
- the application includes a practical internal file browser

---

## 11. Project Explorer Features

### Workflow-oriented browsing
- actors section
- mailbox-style folders
- actor docs
- projects hierarchy
- workspace docs

### Mailbox/work-state visibility
- inbox
- active
- pending
- outbox

### Interaction
- open files
- edit as text
- open with system
- open containing folder
- copy path

### Presentation
- category grouping
- item counts
- workflow-oriented structure instead of raw disk layout

### User expectation supported
- the application can answer questions about active work, waiting work, and work structure

---

## 12. Object Explorer Features

### Object categories
- actors
- loops
- wrappers
- runbooks

### Inspection features
- show object names
- show selected summary properties
- show loop steps
- show runbook command previews

### Interaction
- open editor for selected object
- request diagram for loop
- request diagram for runbook
- request diagram for loop step
- copy selected name

### User expectation supported
- users can navigate by concept rather than by file location

---

## 13. Center Tabbed Document Area Features

### Tab behaviors
- open multiple tabs
- switch between tabs
- close tabs individually
- close all tabs
- dirty/unsaved indicator
- active tab tracking
- non-destructive switching between work surfaces

### Content types supported
- welcome tab
- preferences tab
- structured editors
- raw text editors
- logs viewer
- chat history viewer
- prompt viewer
- workspace viewer
- diagram viewer

### User expectation supported
- focused work remains available until the user intentionally closes it

---

## 14. Structured Editor Features

### Entity editors available
- actor editor
- loop editor
- wrapper editor
- runbook editor
- config editor

### Common editor behaviors
- open from menus/toolbars/explorer
- dirty tracking
- save support
- tab-based editing
- revert support in structured editors

### Specialized behaviors visible to users
- loop editor can request diagrams
- runbook editor can request diagrams

### User expectation supported
- important workspace objects can be understood and edited without raw file knowledge

---

## 15. Raw Text Editing Features

- open many common text/config/document file types directly
- edit raw file content in a tab
- save file
- dirty tracking
- syntax-aware presentation by file type

### User expectation supported
- not everything must be mediated through a specialized form

---

## 16. AI Chat Panel Features

### Core modes
- Ask mode
- Agent mode

### Execution pacing
- Auto mode
- Manual mode

### Controls
- clear visible conversation
- clear persisted history
- next-step button
- prompt preview button
- next prompt preview button
- diagram button
- actor selector
- loop selector
- model selector
- send button
- stop button

### Message presentation
- user messages
- AI messages
- system messages
- error messages
- empty-state guidance

### Session behavior
- start new chat request
- continue manual session
- inspect next step in manual mode
- show completion/system messages

### User expectation supported
- the AI area is configurable, inspectable, interruptible, and integrated with the workspace

---

## 17. Chat Configuration Features

### User-selectable context
- actor selection
- loop selection
- model selection

### Workflow support
- direct prompting
- actor-guided prompting
- loop-based prompting
- manual step-by-step progression

### Inspection support
- prompt preview
- next prompt preview
- execution diagram preview

### User expectation supported
- users can understand and adjust request context before sending

---

## 18. Terminal Panel Features

### Core terminal features
- command input line
- scrolling output area
- integrated command execution
- stop/cancel support

### Productivity features
- command history
- tab completion
- keyboard shortcuts
- help shortcut
- save terminal output

### Context menu features
- copy
- select all
- save output
- clear output

### Busy-state behavior
- terminal can be locked while another panel is running work

### User expectation supported
- the terminal is a serious operational surface for repeated daily use

---

## 19. Runbook Execution Features

- runbook dropdown selection
- start selected runbook
- stop selected runbook
- terminal-based output visibility
- toolbar-driven execution shortcut

### User expectation supported
- predefined workflows are easy to launch and easy to monitor

---

## 20. Logs Viewer Features

- open logs in a dedicated tab
- inspect session-oriented log information
- refresh log view

### User expectation supported
- prior activity can be reviewed in a dedicated inspection surface

---

## 21. Chat History Viewer Features

### Browsing
- list conversation turns
- select a turn to inspect details
- actor filter dropdown

### Actions
- refresh history
- clear all history
- copy selected detail

### Detail view
- metadata summary
- prompt content
- response content

### User expectation supported
- conversation history is reviewable independently from the live chat panel

---

## 22. Prompt Viewer Features

- open prompt viewer tab
- inspect prompt-related details
- inspect prompt previews from chat
- inspect next prompt in manual sessions

### User expectation supported
- request context can be inspected rather than treated as a black box

---

## 23. Workspace Viewer Features

### Summary sections
- workspace identity
- folder structure
- resolved defaults
- config lists
- actors summary
- loops summary
- wrappers summary
- runbooks summary
- documentation/templates summary
- session summary
- disk usage summary

### Actions
- refresh
- copy
- open workspace diagram

### User expectation supported
- there is a single place to orient quickly without opening many separate tabs

---

## 24. Diagram Viewer Features

### Diagram sources
- workspace diagrams
- loop diagrams
- loop step diagrams
- runbook diagrams
- chat execution diagrams

### Viewer controls
- refresh
- zoom out
- zoom in
- fit to window
- actual size
- save as
- open output folder

### Supporting information
- status text
- details area

### User expectation supported
- diagrams are review tools that can be inspected, resized, and saved

---

## 25. Status Bar Features

- workspace status/path display
- actor count display
- session identifier display
- progress indicator during running work

### User expectation supported
- the application quietly confirms where the user is and whether work is happening

---

## 26. Recent Workspace Features

- recent workspace list in menu
- recent workspace list in toolbar dropdown
- disabled entries for missing locations
- clear recent list action

### User expectation supported
- the application remembers where the user was working and helps them return quickly

---

## 27. Panel Visibility Features

- show/hide explorer panel
- show/hide chat panel
- show/hide command panel
- retain central tabbed work area while panels are toggled

### User expectation supported
- the workbench adapts to the task without changing the overall navigation model

---

## 28. Save and Unsaved-Changes Features

- dirty tab indicators
- save active tab
- save all dirty tabs
- unsaved changes prompt on application close
- option to save, discard, or cancel close

### User expectation supported
- the application protects users from accidental loss of work

---

## 29. Keyboard/Productivity Features

- save active tab shortcut
- save all shortcut
- focus terminal shortcut
- refresh shortcut
- focus explorer/chat/terminal shortcuts
- close active tab shortcut
- escape to stop running work

### User expectation supported
- frequent users are rewarded with speed and efficiency

---

## 30. Business-Level Capability Summary

The UI currently provides feature coverage in these business categories:

### Workspace lifecycle
- create
- open
- save
- close
- reopen recent
- verify
- repair
- cleanup

### Navigation and discovery
- file browsing
- workflow/project browsing
- object browsing
- tabbed navigation

### Editing and authoring
- structured editing
- raw text editing
- save/save all
- unsaved change protection

### Conversational work
- ask mode
- action-oriented mode
- auto/manual pacing
- prompt inspection
- diagram inspection

### Command-driven work
- integrated terminal
- command history
- completion
- stop/cancel
- output saving

### Inspection and review
- logs
- chat history
- prompt viewer
- workspace viewer
- diagrams

### Productivity and control
- toolbars
- menus
- keyboard shortcuts
- panel visibility toggles
- status awareness

---

## 31. Suggested Use for New Application Planning

This inventory can be used to classify features into:
- must keep
- simplify
- redesign
- defer
- remove

A useful next step would be to convert this inventory into a parity matrix with columns such as:
- current feature
- business value
- frequency of use
- complexity
- keep/change/remove decision
- notes for new application

### Reproduction principle
The goal is not to copy every visual detail. The goal is to preserve the user’s ability to browse, inspect, act, review, and recover inside one coordinated workbench.
