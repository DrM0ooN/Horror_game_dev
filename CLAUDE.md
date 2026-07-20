# Claude Code — read this first

Before doing anything in this Unity project, read two files in full:
1. `docs/PROJECT_BRIEF.md` — the shared source of truth for architecture, gameplay decisions, tech stack, and conventions. Shared with GitHub Copilot and Gemini CLI, which also read the same file.
2. `todo.md` — the current task checklist, so you know what's already done and what's assigned to whom.

If you make or learn about a significant new decision (architecture change, gameplay behavior, tech choice), append it to `docs/PROJECT_BRIEF.md` Section 8 (Decisions & Implementation Log) with today's date before finishing your response, so Copilot and Gemini stay in sync.

Do not duplicate this context elsewhere. Do not rely on memory of past sessions — always re-read `docs/PROJECT_BRIEF.md` at the start of new work, since it may have changed since you last read it.

This project also has a live Unity Editor connection via the free "MCP for Unity" bridge (CoplayDev). If you have Unity MCP tools available, prefer using them to create/wire GameObjects and components directly in the live scene over asking the user to do it manually.
