What people mean by a Ralph/Wiggum loop:

A wrapper repeatedly runs the agent until a concrete stop condition is met.

Each run is often a fresh model invocation with clean context, not one giant ever-growing chat.

Persistent state lives outside the model: repo state, task file, progress log, tests, evaluator output, and sometimes a small memory file. The common Ralph implementations explicitly say memory persists via things like git history, progress.txt, and prd.json, while each iteration starts fresh.

So the core idea is not “the LLM remembers.” It is:

state_t + prompt -> action -> environment changes -> new state_(t+1)

The model is just a transient policy over the current serialized state.

The basic loop

Minimal form:

Load durable state

task/spec

current workspace / repo

recent evaluator feedback

compact memory summary

Build a prompt from that state

Call the model once

Execute its proposed action

edit files

call tools

run tests

update DB

mark task status

Evaluate result

tests pass?

acceptance criteria met?

lint/typecheck green?

reviewer/evaluator score acceptable?

Persist outcome

Repeat until stop condition

That is why people loosely say “one-shot prompting”: each iteration is one shot against the current world state, not a forever-chat with megabytes of prior tokens.

How information is stored between calls

Efficient systems use external memory, not giant prompt replay.

There are 4 practical layers:

1. Source of truth state

This is the real memory.

code repo / files

database rows

task queue

issue tracker / PRD / checklist

tool outputs

test results

event log

For Ralph specifically, the public examples use:

git commits/history

prd.json for task status

progress.txt for append-only learnings

AGENTS.md/CLAUDE.md for conventions and gotchas.

2. Working memory

A small structured summary injected every iteration.
Example:

current objective

subtask in progress

constraints

known gotchas

last failure

next best action

Think 0.5–2 KB, not 200 KB.

3. Episodic memory

An append-only event log:

“tried X”

“failed because Y”

“fixed by Z”

“do not touch file A before migration B”

This is useful for compression and debugging.

4. Retrieval memory

Long-term searchable notes/vector store/keyword index.
Only retrieve what is relevant to the current step.

The efficient way

For most real systems, the efficient answer is:

Do not store full transcripts as memory.
Store artifacts and summaries, then reconstruct only the minimum context needed for the next call.

Best practice stack:

A. Keep the model stateless

Treat each call as disposable. This avoids context drift and “rot.” Ralph advocates intentionally fresh instances for exactly this reason.

B. Persist structured state, not prose

Use machine-readable records:

JSON task state

test results

tool outputs

file diffs

checkpoint metadata

evaluator verdicts

Bad:

20 pages of chat history

Good:

{
  "goal": "add OAuth callback handler",
  "status": "blocked",
  "last_attempt": "callback route added but CSRF check failing",
  "acceptance_tests": ["oauth_login_test", "csrf_replay_test"],
  "next_action": "inspect session token issuance"
}
C. Summarize aggressively

After each loop, distill the iteration into:

what changed

what failed

what to preserve

what to do next

This should be tiny.

D. Use retrieval, not replay

Only pull in:

relevant files

relevant prior failures

relevant spec fragments

relevant design constraints

Do not resend everything.

E. Separate planner memory from executor memory

Useful pattern:

planner gets broader summary

executor gets only the local subtask and acceptance criteria

This reduces token waste and reduces dumb branching.

F. Make the environment carry memory

In coding loops, the repo is the memory.
In workflow loops, the DB is the memory.
In browsing/research loops, the notes store is the memory.

The model should read from that environment each time.

Why this works better than giant context carry-forward

Because long conversational context degrades in three ways:

Cost
You keep paying for stale tokens.

Attention dilution
Important facts compete with irrelevant prior chatter.

Error accumulation
Bad assumptions get re-amplified every turn.

The Ralph pattern’s whole selling point is that fresh runs plus external state reduce these problems. Public writeups explicitly describe it as using fresh context each loop and relying on external files/history for persistence.

A clean architecture

Use this mental model:

Control plane

Owns:

loop orchestration

stop conditions

retries

budget

checkpointing

evaluator

Memory plane

Owns:

task graph

event log

summaries

retrieval index

durable artifacts

Execution plane

Owns:

model calls

tool calls

environment mutations

The LLM should not be your database.

Concrete implementation pattern

A strong simple design:

Persistent stores

tasks.json

events.log

memory.md or memory.json

repo/filesystem

optional vector index over docs and events

Per-iteration prompt

Include only:

system rules

current task

acceptance criteria

relevant retrieved context

compact memory

current environment snapshot

After iteration

Persist:

diff or artifact IDs

evaluator result

compact summary

next action hint

Pseudo-code:

while not done:
    task = task_store.next_open_task()
    retrieved = retriever.fetch(task, k=5)
    memory = summarizer.compact(last_n_events=20, budget_tokens=800)

    prompt = build_prompt(
        rules=SYSTEM_RULES,
        task=task,
        retrieved=retrieved,
        memory=memory,
        acceptance_criteria=task.acceptance_criteria,
    )

    result = llm(prompt, tools=TOOLS)
    outcome = executor.apply(result)
    verdict = evaluator.check(outcome, task.acceptance_criteria)

    event_store.append({
        "task_id": task.id,
        "action": result.summary,
        "outcome": outcome.summary,
        "verdict": verdict.status,
        "errors": verdict.errors,
    })

    summary_store.update(compact_summary(verdict, outcome))
    task_store.update(task.id, verdict)
What not to do

Avoid these unless the task is tiny:

sending the entire chat transcript every call

storing only freeform natural-language memories

relying on the model to “remember” unstored facts

keeping one endless session alive for hours

mixing planner notes, execution logs, and durable facts into one blob

Rule of thumb by scale
Small toy agent

Just resend:

system prompt

last 3–5 turns

current task

Medium agent

Use:

rolling summary

task state JSON

retrieval over artifacts

Serious production loop

Use:

durable task graph

event sourcing

evaluator/verifier

retrieval over artifacts and prior failures

tiny per-call summaries

fresh model invocations

For coding specifically

Ralph/Wiggum is unusually effective in coding because the environment is already highly compressible and self-checking:

git stores state

tests/lint/typecheck provide feedback

task files define stop conditions

diffs are compact history

repo structure is searchable memory.

That is why these loops often outperform giant “keep chatting until done” sessions.

Bottom line

The efficient way to store information between calls is:

Persist state outside the LLM, keep prompts small, retrieve only what matters, and re-run the model fresh.

In one line:

Artifacts are memory; prompts are views over memory.