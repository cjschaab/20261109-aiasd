---
ai_generated: true
model: "unknown/unknown@unknown"
operator: "cjschaab"
chat_id: "feature-flags-instructions-20261111"
prompt: |
  give different feature flag options

  Exercise: Evaluate Feature Flag Approaches and Author Instructions

  Objectives
  - Compare implementation approaches for feature flags
  - Select one approach using explicit trade-offs
  - Author a reusable instruction file for consistent usage

  Activities
  1. Evaluate three implementation approaches:
     - In-code config flags (environment variables)
     - Database-driven flags
     - External flag service/SDK
  2. Score each approach from 1 (poor fit) to 5 (strong fit) against:
     - rollout safety
     - operational complexity
     - observability
     - cost and vendor risk
     - For every score, cite evidence or documented constraints. Check propagation/redeploy needs, targeting, telemetry/audit, ownership and failure behavior, cost, and exit effort.
     - Record runtime/deployment assumptions and mark any decision-gate criteria.
  3. Select one approach and justify the decision.
  4. Create .github/instructions/feature-flags.instructions.md with:
     - naming standard
     - rollout/rollback checklist
     - testing requirements
     - retirement criteria
     - implementation guidance
     - example usage snippet
     - narrow applyTo glob; verify intended and unintended matches
     - if AI-assisted: required provenance metadata, chat log, summary, and README entry

  Success Criteria
  - Scores cite evidence or documented constraints; decision gates are explained
  - Selected approach is justified for current architecture
  - Instruction file is specific, actionable, reusable, and has a tested narrow scope
  - AI-assisted instruction work includes repository-required provenance and traceability
started: "2026-11-11T15:10:52.074-05:00"
ended: "2026-11-11T15:20:20.330-05:00"
task_durations:
  - task: "Evaluate approaches and author reusable instructions"
    duration: "00:09:28"
total_duration: "00:09:28"
ai_log: "ai-logs/2026/11/11/feature-flags-instructions-20261111/conversation.md"
source: "user (attached exercise brief)"
name: "feature-flags"
description: "Guidance for choosing, implementing, rolling out, testing, and retiring application feature flags."
applyTo: "src/TodoManager/**/*, tests/TodoManager.Tests/**/*, tests/browser/**/*"
version: "1.0.0"
author: "cjschaab"
tags: ["feature-flags", "configuration", "testing"]
owner: "Application maintainers"
reviewedDate: "2026-11-11"
nextReview: "2027-02-11"
---

# Feature Flag Instructions

## Overview

Use this guidance when adding or changing runtime-selectable application behavior
in Daybook's ASP.NET Core app, browser UI, or related tests. Keep flags small,
server-owned where they protect API behavior, safe by default, observable, and
temporary. This guidance does not apply to the separate `TodoApp.Client` or
`TodoApp.Server` sample projects.

## Table of Contents

- [Approach decision](#approach-decision)
- [Naming and ownership](#naming-and-ownership)
- [Implementation guidance](#implementation-guidance)
- [Rollout and rollback](#rollout-and-rollback)
- [Testing requirements](#testing-requirements)
- [Retirement criteria](#retirement-criteria)
- [Example](#example)
- [Validation Checklist](#validation-checklist)
- [Summary](#summary)

## Approach decision

Use application configuration as the default flag store: define a typed
`FeatureFlags` options model, bind the `Features` section, and override values
with deployment environment variables. The current app is a .NET 10 minimal API
with built-in configuration, one SQLite database, and a documented
single-server deployment model. It already supports environment overrides for
`TaskDatabase:Path`; it has no feature-flag SDK, flag table, authentication, or
flag-administration UI. Keep configuration flags static for a process lifetime;
change them through a controlled deployment and restart. Do not promise
per-user targeting or live updates. See [startup configuration](../../src/TodoManager/Program.cs),
the [Developer Guide](../../src/TodoManager/docs/developers-guide.md), and the
[README](../../README.md) for the application setup and deployment constraints
behind this decision.

Scores below compare fit for the current repository, not universal product
ratings. A score of 5 means the approach fits the criterion well. External
service scores assume a provider with staged rollouts, targeting, and dashboards;
verify those capabilities, failure behavior, pricing, and exit path before
selecting one.

| Approach | Rollout safety | Operational complexity | Observability | Cost and vendor risk |
| --- | --- | --- | --- | --- |
| Typed app configuration + environment overrides | **3** - Environment-level changes affect every app user; safe deploy/restart and rollback are straightforward, but there is no gradual or user targeting. | **5** - Uses the existing ASP.NET Core configuration stack and avoids a new store or service. | **2** - Configuration and deployment history can show the intended value, but there is no flag evaluation telemetry or audit trail today. | **5** - No new vendor or recurring service cost; operational effort is limited to deployment and cleanup. |
| Database-driven flags | **2** - A database value could be changed centrally, but there is no flag schema, admin UI, authorization, audit history, or multi-instance coordination to make changes controlled. | **2** - Requires schema, persistence and caching/refresh rules, admin workflow, access control, and failure semantics beyond the current app. | **2** - SQLite can show the current value, but history, owner attribution, evaluation metrics, and alerts would need to be built. | **5** - Uses the existing SQLite deployment and adds no vendor, but creates ongoing custom maintenance and migration costs. |
| External flag service/SDK | **4*** - A suitable provider can support staged rollout, targeting, and rapid disablement; safety depends on verified access controls and defined behavior during provider/network failure. | **2** - Adds a provider account, SDK, credentials, network dependency, configuration workflow, and operational ownership. | **4*** - A suitable provider can supply evaluation history and dashboards; verify telemetry, retention, and export before adoption. | **1** - Introduces vendor and pricing risk, plus SDK coupling and exit/migration effort. |

`*` Conditional score based on the stated provider capabilities, which have not
been selected or verified. The current README documents no authentication and a
single-server SQLite deployment, so per-user targeting and multi-instance
consistency are not current capabilities. Re-score if deployment architecture,
audience, or operational requirements change.

**Decision gates:** Reconsider an external provider when the product needs
authenticated-user or cohort targeting, staged rollout across instances, a
non-developer live kill switch, or centralized evaluation/audit telemetry.
Consider a database store only if the product already has an authenticated,
audited operator workflow and a defined consistency/refresh model. Before either
change, specify timeout and provider/database failure behavior, flag ownership,
cost limits, data export, and a tested exit path.

## Naming and ownership

- Name flags after the capability they control, not a release date, ticket, or
  person: `TaskRecurrence` or `BulkTaskActions`.
- Use `Features:<FlagName>` in .NET configuration and
  `Features__<FlagName>` for environment-variable overrides.
- Add each setting to the typed `FeatureFlags` options model and document its
  default, owner, purpose, rollout scope, and planned removal date.
- Default new flags to `false` unless the feature is already fully supported and
  enabling it by default is an intentional, reviewed behavior change.
- Do not use a feature flag as authorization, validation, or a security
  boundary. Enforce server-side behavior in the API; hiding a browser control is
  not sufficient.
- Do not create a flag for a permanent product rule or a one-time deployment
  switch. Prefer a normal configuration setting for operational configuration.

## Implementation guidance

- Bind `Features` to a typed options class using ASP.NET Core's built-in
  configuration and options services; do not add a package for local flags.
- Keep each conditional close to the behavior it controls. If a capability
  affects API data or side effects, check the flag on the server as well as in
  the UI.
- Define behavior when disabled, including requests from stale clients and
  existing persisted data. Disabling a flag must not silently delete or corrupt
  user data.
- For the selected configuration approach, treat values as startup configuration.
  Deploy the desired value and restart the app; do not imply that changing an
  environment variable updates a running process.
- Make flag state visible to operators through the deployment configuration and
  a safe startup diagnostic where useful. Do not log secrets or emit a log line
  on every evaluation. Add metrics or audit events only when a real operational
  need exists.
- Keep the false and true paths correct for the full life of the flag. Remove the
  flag, obsolete branch, tests, and stale configuration after retirement.

## Rollout and rollback

### Rollout checklist

- [ ] Record the flag name, owner, default, intended scope, success signal, and
      removal date.
- [ ] Confirm the disabled path is safe for existing data and stale clients.
- [ ] Deploy code with the flag disabled; run focused tests and smoke checks.
- [ ] Enable it only in the intended environment using configuration, then
      restart the app.
- [ ] Verify the effective value and feature behavior; inspect relevant errors
      and user impact before expanding the rollout.
- [ ] For this configuration approach, treat each environment as all-or-none.
      Do not claim percentage or per-user rollout.

### Rollback checklist

- [ ] Set the flag to `false` in deployment configuration and restart, or
      redeploy the last known-good configuration.
- [ ] Verify that the disabled path is active and that existing user data remains
      intact.
- [ ] Check errors and user impact; document the cause and any follow-up.
- [ ] Revert code only if the code itself is faulty; a flag rollback alone must
      not leave incompatible data or schema changes.
- [ ] If rollback requires a database restore or destructive migration, stop and
      follow the application's backup/recovery procedure instead of treating the
      flag as a complete rollback.

## Testing requirements

- Test both enabled and disabled behavior, including the default when the setting
  is absent.
- Test server-side rejection or fallback for direct API requests when a feature
  is disabled; do not test only that a UI element is hidden.
- Test relevant persisted-data compatibility and rollback behavior when a flag
  changes writes, response shapes, or schema usage.
- Keep tests deterministic by supplying configuration explicitly. Do not depend
  on a developer machine's environment variables or production flag service.
- Add a browser test when the flag changes visible UI behavior. Verify both
  enabled and disabled states if the browser owns any part of the gating.
- If an external provider is later adopted, test timeout, unavailable-provider,
  cached-value, and configured fallback behavior without requiring live service
  access in ordinary tests.

## Retirement criteria

Remove a flag when its rollout is complete or the feature is abandoned. Before
removal:

1. Confirm with the owner that no rollback window or environment still needs the
   old path.
2. Record the final enabled/disabled decision and check the rollout's success and
   error signals.
3. Remove the conditional, obsolete implementation, tests for the retired
   branch, and unused configuration.
4. Preserve data and compatibility for supported clients; use a separate
   migration plan if stored data or API contracts need cleanup.
5. Search for the flag name and verify no references, settings, or operational
   instructions remain.

## Example

For a future recurrence UI/API rollout, the configuration could be:

```json
{
  "Features": {
    "TaskRecurrence": false
  }
}
```

Override it in an environment with `Features__TaskRecurrence=true`. Bind the
section to the options type and inject that type into the server component that
owns the behavior. When disabled, the API must enforce the chosen contract even
if a caller bypasses the browser UI. This is illustrative; it does not add a
flag to the application or change its current recurrence behavior.

## Validation Checklist

- [ ] Each flag has a capability-based name, owner, default, scope, and removal
      date.
- [ ] Server-side behavior is gated where APIs or data are affected.
- [ ] Disabled behavior preserves valid existing data and handles stale clients.
- [ ] Deployment steps identify configuration propagation and restart needs.
- [ ] Rollback and failure behavior are documented and testable.
- [ ] Tests cover default, enabled, disabled, direct API access, and relevant
      persistence or UI behavior.
- [ ] Retired flags and their settings, branches, and tests are removed.
- [ ] The chosen approach is re-evaluated when targeting, live changes,
      telemetry, or deployment topology needs exceed its documented limits.

## Summary

Prefer typed, environment-overridable configuration for Daybook's current
single-server architecture. Treat flags as temporary, process-startup settings:
ship disabled, verify, enable per environment, and roll back by disabling and
restarting. Adopt a database or external provider only after its access control,
consistency, failure behavior, observability, cost, and exit plan are justified.

---

**Document Version**: 1.0.0  
**Last Updated**: 2026-11-11  
**Maintainer**: Application maintainers  
**Related Instructions**: [Test-Driven Development](./test-driven-development.instructions.md), [AI-Assisted Output](./ai-assisted-output.instructions.md)
