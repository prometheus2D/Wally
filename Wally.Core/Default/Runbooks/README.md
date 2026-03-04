# Wally Runbooks

Runbooks are `.wrb` files that contain sequences of Wally commands — one per line.
They are for **multi-step workflows** — chaining setup, multiple actors, loops,
and other commands into a repeatable pipeline.

> For single-command tasks, use `run` directly with flags like `-a`, `-l`, `--loop`.
> Runbooks are for when you need *more than one step*.

## Format

```
# Comments start with #
# The first comment line becomes the runbook's description.

setup --verify
run "{userPrompt}" -a Stakeholder
run "{userPrompt}" -a Engineer -l CodeReview
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
wally runbook <name> ["<prompt>"]
wally list-runbooks
```

## Nesting

Runbooks can call other runbooks. A `runbook` line inside a `.wrb` file dispatches
recursively. Nesting is capped at 10 levels to prevent infinite loops.

## Error Handling

Execution stops on the first command that fails. There is no continue-on-error mode.
