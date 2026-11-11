---
ai_generated: true
model: "OpenAI/unknown@unknown"
operator: johnmillerATcodemag-com
chat_id: csharp-development-20261109
prompt: |
  create a new instruction file for csharp code. don't overwrite the existing file
started: "2026-11-09T20:31:53Z"
ended: "2026-11-09T20:33:57Z"
task_durations:
  - task: "Review conventions and write C# guidance"
    duration: "00:02:04"
total_duration: "00:02:04"
ai_log: "ai-logs/2026/11/09/csharp-development-20261109/conversation.md"
source: "user"
name: csharp-development
description: "Use when creating, modifying, reviewing, or testing C# code. Covers .NET compatibility, nullability, async I/O, errors, data handling, and tests."
applyTo: "**/*.cs"
version: "1.0.0"
author: "johnmillerATcodemag-com"
tags: ["csharp", "dotnet", "coding-standards"]
owner: "Development Team"
reviewedDate: "2026-11-09"
nextReview: "2027-02-09"
---

# C# Development Instructions

## Overview

Apply these rules when writing or changing C# source. Preserve existing project architecture and behavior, and use the target framework and established patterns as the source of truth.

## Table of Contents

- [Project conventions](#project-conventions)
- [Correctness and reliability](#correctness-and-reliability)
- [Types and data](#types-and-data)
- [Testing](#testing)
- [Validation checklist](#validation-checklist)

## Project conventions

- Check the project target framework, language version, nullable settings, and nearby code before using framework or language features.
- Do not change target frameworks or dependencies unless the task requires it.
- Follow established naming, formatting, namespace, dependency-injection, and layering conventions. Prefer the simplest implementation that fits.
- Keep nullable analysis and compiler warnings clean; do not suppress warnings merely to make a build pass.

## Correctness and reliability

- Validate untrusted input at application boundaries. Use the existing validation and error-reporting paths; do not silently return success-shaped defaults.
- Use asynchronous APIs for I/O. Pass `CancellationToken` through supported operations and avoid blocking on tasks with `.Result` or `.Wait()`.
- Dispose owned resources with `using` or `await using`, as appropriate.
- Parameterize database commands. Never concatenate untrusted input into SQL or shell commands.
- Handle expected failures specifically. Avoid broad exception catches and null-forgiving operators; let unexpected failures reach standard error handling.
- Preserve public behavior and contracts unless the task explicitly changes them. For HTTP endpoints, follow established HTTP semantics and typed-result patterns.

## Types and data

- Represent optional values with nullable types and check them at use sites.
- Use immutable request/response types where they suit existing patterns; keep domain rules in the appropriate service or domain layer.
- Choose types that express the data's meaning, such as `DateOnly` for calendar dates and `DateTimeOffset` for timestamps.
- Make timezone and culture-sensitive parsing/formatting explicit. Use invariant culture for stable serialized or persisted formats.

## Testing

- Add or update tests for behavior changes and bug fixes. Follow the repository's test framework and assert observable behavior.
- For HTTP behavior, cover relevant status codes, response data, and validation through the existing integration-test setup where practical.
- Run the smallest relevant test command and resolve failures caused by the change.

## Validation checklist

- [ ] Target framework and neighboring conventions were checked.
- [ ] Nullable analysis and compiler warnings remain clean.
- [ ] I/O is asynchronous and cancellation is passed through where supported.
- [ ] Resources are disposed and external inputs are safely handled.
- [ ] Relevant tests cover the changed behavior and pass.

## Summary

Preserve project conventions, use explicit types and safe asynchronous I/O, report failures through established paths, and verify behavior with focused tests.

---

**Document Version**: 1.0.0  
**Last Updated**: 2026-11-09  
**Maintainer**: Development Team  
**Related Instructions**: [AI-Assisted Output Instructions](./ai-assisted-output.instructions.md)
