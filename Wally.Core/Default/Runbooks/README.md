# Wally Runbooks

Runbooks are `.wrb` files that contain sequences of Wally commands — one per line.

## Format

```
# Comments start with #
# The first comment line becomes the runbook's description.

setup "{workSourcePath}"
run Engineer "{userPrompt}" -l CodeReview
runbook another-runbook
info
```

- **Blank lines** are ignored.
- **Comment lines** start with `#`.
- **Everything else** is a Wally command, identical to what you'd type in the terminal.

## Placeholders

| Placeholder | Resolved To |
|---|---|
| `{workSourcePath}` | The current WorkSource path |
| `{workspaceFolder}` | The current `.wally/` workspace folder path |
| `{userPrompt}` | The optional prompt passed at runtime: `runbook my-book "extra context"` |

## Running

```
wally runbook setup-and-review "Review the auth module"
```

Or from the terminal panel in Wally Forms:

```
runbook full-analysis "Improve error handling"
```

## Nesting

Runbooks can call other runbooks. A `runbook` line inside a `.wrb` file dispatches
recursively. Nesting is capped at 10 levels to prevent infinite loops.

## Error Handling

Execution stops on the first command that fails. There is no continue-on-error mode.
