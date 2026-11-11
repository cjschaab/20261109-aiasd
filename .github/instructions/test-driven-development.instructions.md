---
ai_generated: true
model: "unknown/unknown@unknown"
operator: "jMM-at-CODETraining"
chat_id: "tdd-instructions-20261111"
prompt: |
  create an instruction file for test driven development
started: "2026-11-11T09:18:04.952-08:00"
ended: "2026-11-11T09:20:55.504-08:00"
task_durations:
  - task: "Review repository guidance and write TDD instructions"
    duration: "00:02:50"
total_duration: "00:02:50"
ai_log: "ai-logs/2026/11/11/tdd-instructions-20261111/conversation.md"
source: "user"
name: "test-driven-development"
description: "Use when implementing behavior or fixing bugs with tests first. Covers the red-green-refactor cycle, test selection, deterministic tests, and verification."
applyTo: ["**/*.cs", "**/*.js", "**/*.ts"]
version: "1.0.0"
author: "jMM-at-CODETraining"
tags: ["testing", "tdd", "quality"]
owner: "Development Team"
reviewedDate: "2026-11-11"
nextReview: "2027-02-11"
---

# Test-Driven Development Instructions

## Overview

Use these instructions when implementing behavior or fixing defects in application
code and tests. Drive each change with a failing test, the smallest implementation
that passes, and a cleanup step that preserves the tested behavior.

## Table of Contents

- [Red-Green-Refactor](#red-green-refactor)
- [Test Design](#test-design)
- [Test Selection and Verification](#test-selection-and-verification)
- [When the Cycle Cannot Be Followed](#when-the-cycle-cannot-be-followed)
- [Validation Checklist](#validation-checklist)

## Red-Green-Refactor

1. **Define the behavior.** Identify the observable outcome, relevant boundaries,
   and acceptance criteria before changing production code. For a defect, first
   express the incorrect behavior and the expected behavior.
2. **Red - write one focused test.** Add or update the smallest test that captures
   one behavior. Run it before implementation and confirm it fails for the
   intended assertion, not because of a build, setup, or environment failure.
3. **Green - implement the minimum.** Change production code only enough to pass
   the new test and related tests. Do not weaken assertions, skip tests, or add
   unrelated behavior to obtain a passing result.
4. **Refactor - improve safely.** Simplify the implementation or test structure
   without changing behavior. Re-run the focused tests after each meaningful
   refactoring step.
5. **Repeat in small increments.** Add separate tests for distinct outcomes or
   edge cases instead of combining unrelated behaviors in one large scenario.

For a bug fix, keep the regression test and confirm it fails before the fix when
that can be reproduced reliably. Keep existing public contracts unless a
deliberate behavior change is part of the task.

## Test Design

- Assert externally observable behavior: returned values, persisted state,
  rendered UI, HTTP status and response data, or other relevant contracts.
- Cover relevant normal, boundary, invalid-input, and failure cases. Keep each
  test focused enough that a failure identifies the behavior at issue.
- Prefer deterministic inputs. Control clocks, randomness, environment, and
  external responses through existing seams when the behavior depends on them.
- Isolate test data and clean up resources. Do not rely on execution order,
  shared mutable state, network availability, or real-time delays.
- Use the narrowest suitable test level: unit tests for isolated rules,
  integration tests for component boundaries and persistence, and browser tests
  for rendered interactions. Keep higher-level tests for contracts that lower
  levels cannot verify.
- Use real dependencies when their behavior matters; substitute or mock only
  boundaries that need control. Do not add abstractions solely to make code
  mockable.
- Avoid asserting implementation details that may change without changing the
  behavior.

## Test Selection and Verification

- Inspect the existing test project, test framework, fixtures, and nearby tests
  before choosing a test location or pattern.
- Run the new or changed test first during the red-green cycle, then run the
  smallest relevant test group after implementation and refactoring.
- Before finishing, run the relevant suite(s) affected by the change and report
  any test command that could not be run and why. Do not claim tests passed
  without running them.
- In this repository, run API tests from the root with
  `dotnet test .\tests\TodoManager.Tests`. Run browser tests from
  `.\tests\browser` with `npm test`; follow the configured serial execution
  because browser tests share a test server and database.
- Preserve test isolation and the existing coverage of integration and browser
  behavior. See [Testability Recommendations](../../src/TodoManager/docs/testability-recommendations.md)
  for repository-specific seams and time-control guidance.

## When the Cycle Cannot Be Followed

Do not fabricate a red result when a test cannot be run or reliably made to fail.
If a legacy constraint, unavailable dependency, or environment limitation blocks
the normal cycle, explain the constraint, make the smallest safe change, add or
update the most relevant test, and run whatever focused validation is available.
Ask for clarification when expected behavior is ambiguous rather than encoding
an assumption in the test.

## Validation Checklist

- [ ] Expected behavior and acceptance criteria are clear.
- [ ] A focused test failed for the intended reason before implementation, or
  the reason the red step was blocked is documented.
- [ ] The implementation is the smallest change that makes the test pass.
- [ ] Refactoring preserved behavior and focused tests still pass.
- [ ] Tests cover relevant boundaries and failures with deterministic,
  isolated data.
- [ ] Relevant suites were run and their results are reported accurately.

## Summary

Make behavior changes through a verified red-green-refactor loop. Keep tests
focused, deterministic, isolated, and centered on observable outcomes; disclose
any justified deviation and validate the affected suites.

---

**Document Version**: 1.0.0  
**Last Updated**: 2026-11-11  
**Maintainer**: Development Team  
**Related Instructions**: [C# Development](./csharp-development.instructions.md), [AI-Assisted Output](./ai-assisted-output.instructions.md)
