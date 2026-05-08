# MTC.Log

A thin, stable wrapper around [NLog](https://nlog-project.org/) designed for large solutions where you want a **single place** to manage your logging dependency.

Instead of referencing NLog directly in every project, all your projects reference `MTC.Log`. When NLog releases a new version, you update one `.csproj` — not 120.

---

## Why MTC.Log?

In a solution with many projects, having each one depend on NLog directly creates a maintenance problem: every NLog update touches every project. `MTC.Log` solves this by acting as the single point of contact with NLog. Your projects depend on the abstraction (`ILoggerSink`), not on NLog itself.

- Update NLog in **one place**
- All your projects stay clean of direct NLog references
- Swap or extend the logging backend without touching consumers
- Optionally forward log entries to a second sink (e.g. a UI component or remote logger)

---

## Installation