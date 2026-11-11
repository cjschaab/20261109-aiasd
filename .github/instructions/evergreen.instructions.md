---
description: "Use when designing, implementing, refactoring, or testing C#, Razor, or CSS in this repository. Apply durable coding practices that remain maintainable as the project evolves."
applyTo: "src/**/*.cs, src/**/*.razor, src/**/*.css, tests/**/*.cs"
---

# Evergreen Coding Practices

Build changes that remain clear, correct, and compatible with the project's current users and supported platform.

## Design and Compatibility

- Check the project files and nearby implementation before choosing APIs, dependencies, or patterns. Keep changes compatible with the configured .NET 10 target; do not upgrade frameworks or dependencies as an incidental part of another change.
- Choose the simplest design that handles the known requirements. Avoid speculative abstractions, clever shortcuts, unnecessary dependencies, and premature optimization.
- Keep each type and method focused. Extract shared logic when it removes meaningful duplication or gives a concept a clear owner.
- Preserve existing behavior and contracts unless a change is intentional. When changing API or persisted JSON shapes, account for existing clients and stored data.

**Example:** For a `PATCH` change, preserve the distinction between an omitted field and an explicitly empty field when that is part of the existing API contract; add tests for both cases.

## Reliability and Tests

- Validate external input at system boundaries and represent optional values explicitly. Handle expected invalid input and missing resources deliberately; let unexpected failures remain visible rather than masking them with broad exception handling.
- Add or update focused tests for behavior changes. Cover relevant boundary cases, and isolate test data so tests do not depend on execution order, shared mutable state, or avoidable external services.
- Prefer correctness and readable control flow over micro-optimizations. Measure before introducing performance complexity.

## User Interface and Maintenance

- Keep Razor and CSS changes responsive and accessible. Preserve semantic markup, keyboard access, and visible focus behavior when changing interactions.
- Update relevant documentation when behavior, setup, or supported usage changes.

## Change Checklist

- [ ] The implementation follows the project's configured target and nearby conventions.
- [ ] Existing API, UI, and persisted-data behavior is preserved or intentionally changed.
- [ ] Meaningful behavior changes have focused tests.
- [ ] Relevant documentation is current.
