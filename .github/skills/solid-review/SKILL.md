---
name: solid-review
description: 'Review a codebase or change set for evidence-backed SOLID design-principle violations. Use for SOLID reviews, single-responsibility, open-closed, Liskov substitution, interface-segregation, or dependency-inversion assessments.'
---

# SOLID Code Review

Review the requested code for concrete violations of the SOLID principles. Treat this as a read-only review: do not modify files unless the user separately asks for fixes.

## Procedure

1. Establish the review scope: requested files, changed files, or the whole codebase. If the user asks for the whole codebase, identify production code and its tests; avoid generated, vendored, and build-output files.
2. Read applicable repository instructions and inspect the relevant code, callers, implementations, tests, and project configuration. Follow dependencies only as far as needed to verify behavior and ownership.
3. Evaluate each concern against the principle it may violate:
   - **Single Responsibility:** A module has multiple independent reasons to change, supported by distinct responsibilities or change paths—not merely because it is long.
   - **Open/Closed:** A recurring kind of extension requires modifying stable logic in ways that create real coupling or repeated branching—not merely because a type is currently concrete.
   - **Liskov Substitution:** A subtype breaks behavior callers are entitled to rely on, such as strengthening preconditions, weakening postconditions, or violating established invariants.
   - **Interface Segregation:** Consumers are forced to depend on or implement irrelevant operations, creating unnecessary coupling or empty/unsupported implementations.
   - **Dependency Inversion:** High-level policy depends directly on low-level details where that dependency causes rigidity or makes the design difficult to vary or test. Do not require an interface for every dependency.
4. Trace relevant call sites and tests before concluding. Separate confirmed design problems from preferences, hypothetical future needs, and ordinary trade-offs. Do not infer a violation from class size, naming, use of a concrete type, or missing abstractions alone.
5. Report only actionable findings. For each, include severity, principle, file and line, evidence, impact, and a concise direction for improvement. State when the reviewed scope has no substantiated findings and note any limits to the review.

## Finding quality

- Cite the smallest relevant line range and describe the observed behavior or dependency that supports the finding.
- Explain the concrete cost or correctness risk; do not present a principle label as the evidence.
- Prefer the narrowest accurate severity. Reserve high severity for substantial, demonstrated design risk; do not inflate severity for style or hypothetical extensibility.
- Avoid duplicate findings for the same underlying cause. If a concern is uncertain or depends on missing context, explain that uncertainty rather than asserting it as a violation.
- Keep recommendations proportionate. Do not propose broad redesigns when a small boundary or responsibility change addresses the issue.

For this repository, account for the configured .NET target and existing C# / ASP.NET Core patterns in `.github/instructions/csharp.instructions.md` and `.github/instructions/evergreen.instructions.md`. Preserve the distinction between SOLID findings and unrelated correctness, security, or style observations.
