---
ai_generated: true
model: "OpenAI/unknown@unknown"
operator: johnmillerATcodemag-com
chat_id: ebe5a9d2-17a3-4d4d-b4d9-07a745c3d2df
prompt: |
  create an instruction file for csharp code
started: "2026-11-09T20:10:18Z"
ended: "2026-11-09T20:13:40Z"
task_durations:
  - task: "Review project conventions"
    duration: "00:01:00"
  - task: "Write C# guidance"
    duration: "00:02:22"
total_duration: "00:03:22"
ai_log: "ai-logs/2026/11/09/ebe5a9d2-17a3-4d4d-b4d9-07a745c3d2df/conversation.md"
source: "user"
description: "Use when writing, modifying, reviewing, or testing C# code. Covers modern C# style, .NET APIs, asynchronous code, error handling, and tests."
applyTo: "**/*.cs"
---

# C# Development Instructions

## Project conventions

- Check the project target framework and existing patterns before using newer language or framework features. Do not change target frameworks or dependencies unless requested.
- Preserve nullable reference type annotations and implicit-using conventions. In this repository, nullable analysis is enabled and compiler warnings are errors; keep changes warning-free.
- Follow nearby naming, formatting, namespace, and file-organization conventions. Prefer clear, specific names and straightforward implementations over unnecessary abstractions.
- Use the repository's existing dependency-injection and ASP.NET Core patterns when changing application code.

## Correctness and reliability

- Model nullability explicitly. Validate untrusted input at application boundaries and return or raise errors through the established validation and error-handling paths; do not silently substitute success-shaped defaults.
- Use asynchronous APIs for I/O. Accept and pass through a `CancellationToken` where the surrounding API supports cancellation; avoid blocking on tasks with `.Result` or `.Wait()`.
- Dispose owned `IDisposable` and `IAsyncDisposable` resources with `using` or `await using`, consistent with the API.
- Parameterize database commands. Do not build SQL by concatenating user-controlled values.
- Keep externally visible behavior and API contracts stable unless the task calls for a behavior change. Use typed results and established HTTP semantics for minimal API endpoints.
- Avoid broad exception catches and null-forgiving operators. Handle expected failures specifically and let unexpected failures reach the application's standard error handling.

## Types and data

- Prefer immutable data shapes for request/response contracts when consistent with nearby code; use records where they make the contract clearer.
- Keep domain rules in the appropriate service or domain layer rather than duplicating them across endpoints.
- Use types that reflect the data's meaning, such as `DateOnly` for calendar dates and `DateTimeOffset` for timestamps. Be explicit about timezone and culture-sensitive conversions.

## Tests

- Add or update tests for behavior changes and bug fixes. Follow the existing test framework and patterns, and assert observable behavior rather than implementation details.
- For HTTP endpoint changes, cover relevant status codes, response data, and validation behavior through the existing integration-test setup where practical.
- Run the smallest relevant test command, then address failures introduced by the change.
