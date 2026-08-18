# Capability-First Skill and MCP Policy

## Core Principle

Use the capabilities that are actually available in the current environment instead of relying only on general model knowledge.

Skills, plugin skills, MCP servers, tools, resources, and prompts may differ by project, machine, installation, and session. Treat the current capability set as dynamic. Discover what is available from its current names, descriptions, schemas, and instructions rather than depending on a fixed list written in this file.

For any substantive task, actively look for relevant capabilities before planning, answering, editing files, writing code, debugging, reviewing, or declaring completion.

## Skill Discovery and Selection

- Inspect the Skills currently exposed in the session and identify those whose descriptions match the request.
- Do not wait for the user to explicitly name or invoke a Skill when a relevant Skill is already available.
- Do not assume that a Skill is unavailable merely because its name differs from a previously installed version.
- Select Skills by purpose, trigger conditions, supported technology, and version scope—not by remembered names alone.
- Read the selected Skill instructions before acting. A Skill is not considered used merely because its name or summary was visible.
- Follow required prerequisites, ordering rules, checklists, validation steps, and referenced materials contained in the selected Skill.
- Re-check available Skills when the task changes meaningfully, because a later phase may require a different process or domain Skill.
- Use more than one Skill when the task spans multiple concerns, but choose the smallest complete set needed to perform the work correctly.
- Never claim that a Skill was used unless its instructions were actually read and applied.

## Process Skills and Domain Skills

When process-oriented Skills are available, use them to determine how the work should be performed. When domain-oriented Skills are available, use them to determine what is technically correct.

The preferred order is:

1. Select the relevant process guidance for the current type of work.
2. Select the relevant domain and version-specific knowledge.
3. Inspect the real project or external state with available tools.
4. Perform the work according to the selected instructions.
5. Verify the result with current evidence.

Superpowers Skills, when installed, should be treated as the primary source of process discipline. Discover the appropriate Superpowers Skill from its current description and use it at the phase where it applies. Do not hardcode or maintain a permanent list of individual Superpowers Skill names here, because the installed set and its instructions may change.

Use process Skills for activities such as understanding requirements, designing behavior, planning multi-step work, debugging, testing, executing approved work, reviewing changes, validating results, and authoring or maintaining Skills. Use the actual Skill descriptions to determine which one applies.

A process Skill does not replace technical knowledge. Combine it with the relevant Unity, language, framework, platform, build, testing, or project-specific Skill whenever both are applicable.

## Unity Version-Aware Skill Use

Unity knowledge is version-sensitive. Multiple Unity Skill families may be installed at the same time, and similarly named Skills may contain different APIs, package versions, limitations, lifecycle behavior, or recommendations.

Before selecting Unity knowledge Skills, determine the actual project version and package state from project files whenever they are available, including:

- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- Assembly Definition files and relevant project settings when they affect the question

Then apply these rules:

- Prefer Unity Skills that explicitly match the project's exact Editor version or supported version range.
- Use the current Skill descriptions and version policy to decide whether a Skill is compatible with the project.
- Never apply guidance from one Unity version to another as though it were automatically identical.
- Do not infer that an optional package is installed merely because a Unity Skill documents it.
- Verify package-dependent advice against the project's actual package manifest and lock file.
- Load only the Unity references relevant to the current question instead of reading or applying every reference indiscriminately.
- Treat version-specific Unity Skills as knowledge sources for official behavior, APIs, architecture, C# practices, game systems, performance, testing, build, and quality decisions within their stated scope.
- Keep official Unity facts separate from design interpretation. Do not present an architectural inference as an official Unity requirement.
- When an exact-version Skill is not available, use only clearly version-independent concepts and verify every version-sensitive detail against official documentation or direct project evidence before applying it.
- When compatibility cannot be established, state the limitation instead of guessing.

The Unity knowledge Skills are not a substitute for inspecting the current project. Their role is to explain what is valid for the relevant Unity version; project files, logs, tests, and Editor state determine what is actually present in this project. The Unity knowledge Skills created for this workspace are intentionally separated by version and knowledge area, so select them dynamically rather than assuming one fixed Unity prefix or one universal set of contents.

## MCP Discovery and Use

MCP capabilities are also dynamic. Discover the MCP servers, tools, resources, and prompts currently connected to the session and use those relevant to the task.

- Select MCP capabilities from their current descriptions and schemas rather than relying on remembered server or tool names.
- Use MCP when it can provide authoritative current state, project context, connected service data, or an operation that cannot be established reliably from assumptions.
- Prefer direct inspection over speculation when MCP can read the relevant Editor, repository, issue tracker, documentation source, build system, runtime, database, design source, or connected service.
- Read the tool schema before invoking it and supply only supported arguments.
- Start with read-only inspection when the state is unknown.
- Keep reads and writes scoped to the minimum required for the task.
- Before a write, confirm the target and understand the current state well enough to avoid overwriting unrelated work.
- After a write or action, read the resulting state again or run an appropriate verification rather than trusting the action response alone.
- Do not claim an MCP operation occurred if the tool was not successfully invoked.
- If a relevant MCP capability is unavailable, disconnected, or fails, continue with the best available local evidence and clearly identify the limitation.

## Combining Skills and MCP

Skills and MCP serve different but complementary roles:

- Skills provide process rules, technical knowledge, decision criteria, constraints, and proven workflows.
- MCP provides access to current external state and, where supported, performs operations on that state.

Use both when the task benefits from both. A typical pattern is:

1. Use a process Skill to choose the correct working method.
2. Use a version-matched domain Skill to establish the correct technical expectations.
3. Use MCP or local project tools to inspect the actual state.
4. Apply the change or produce the answer.
5. Use MCP, tests, builds, logs, or file inspection to verify the result.

Do not replace available project evidence with generic advice. Do not replace required technical reasoning with blind tool execution.

## Design Planning Before Implementation

Do not immediately implement non-trivial features or systems that require meaningful design decisions.

Treat a task as requiring design review when the change can materially affect one or more of the following:

- architecture
- coupling between classes or systems
- extensibility
- performance
- maintainability
- data flow
- Unity object or scene structure
- project development workflow

For such tasks:

1. Inspect the current project structure and relevant existing code first.
2. When multiple reasonable approaches exist, propose 2-3 practical alternatives before implementation.
3. Briefly compare each alternative in terms of:
   - advantages
   - disadvantages
   - implementation complexity
   - suitability for the current project scope and requirements
4. Recommend one approach and explain why it is preferred.
5. Do not implement the selected approach until the user explicitly approves or chooses one.
6. Prefer the simplest design that satisfies the current requirements.
7. Avoid unnecessary abstraction or over-engineering based only on hypothetical future needs.

Do not propose multiple design alternatives for trivial changes such as:

- adjusting numeric values
- renaming variables or objects
- moving GameObjects
- changing Inspector values
- fixing obvious syntax or compilation errors
- simple cleanup that does not affect system behavior

## Working Behavior

For substantive work:

1. Read the request and relevant project instructions.
2. Inspect enough project context to understand the target and constraints.
3. Discover relevant Skills and MCP capabilities available now.
4. Read and apply the selected Skill instructions.
5. Confirm the actual Unity version, package state, or connected-system state when relevant.
6. Perform the work using the appropriate process, domain knowledge, and tools.
7. Verify the result with fresh evidence appropriate to the claim.
8. Report what was changed, what was verified, and any remaining limitation.

Do not add unnecessary ceremony to trivial requests. However, apparent simplicity is not a reason to ignore a clearly relevant Skill or an authoritative MCP source.

Do not ask the user to choose a Skill or MCP tool when the available descriptions are sufficient to select the correct capability. Ask only when a genuine product, design, safety, or scope decision cannot be resolved from the request and available context.

## Reporting Capability Use

At the beginning of substantive work, briefly identify the actual Skills and MCP capabilities selected for that task when doing so helps the user understand the approach.

- Mention only capabilities that will actually be used.
- Do not print the complete installed capability catalog.
- Do not repeat the same capability announcement on every follow-up turn when the working context has not changed.
- If additional capabilities become necessary later, mention only the newly added capability or changed approach.
- Keep capability reporting short; the work and verification evidence are more important than the announcement.

## Verification and Completion

Do not report that work is complete, fixed, correct, passing, or ready based only on intention, edited files, or a successful tool call.

Use the strongest available verification for the task, such as:

- Relevant automated tests
- Unity Edit Mode or Play Mode tests
- Compilation and build results
- Current Console and log output
- Static validation
- Repository diff inspection
- Editor or runtime state read back through MCP
- Reopening and checking generated artifacts

State exactly what was verified. If full verification is impossible in the current environment, distinguish completed work from unverified assumptions and identify the missing evidence.
