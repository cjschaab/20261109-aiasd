# Chat Summary

- Chat ID: feature-flags-instructions-20261111
- Date: 2026-11-11
- Operator: cjschaab
- Model: unknown/unknown@unknown
- Duration: 00:09:28

## Objective

Compare feature-flag approaches for the Daybook repository and create a reusable
instruction file with a repository-specific recommendation and usage safeguards.

## Completed

- `.github/instructions/feature-flags.instructions.md` - Scored configuration,
  database, and external-provider approaches; selected typed environment-
  overridable configuration; documented naming, implementation, rollout,
  rollback, testing, retirement, and an example.
- `README.md` - Added a discoverable link to the new instruction and this summary.
- `conversation.md` - Recorded the request, context, and outcome.

## Key decisions

- Prefer typed configuration with environment-variable overrides for the current
  single-server SQLite application; it needs no new dependency or service.
- Treat configuration flags as startup values requiring a controlled restart.
  Do not claim live updates, per-user targeting, or gradual rollout.
- Reconsider database-backed or external flags only when documented targeting,
  operations, audit, or rollout requirements justify their added cost and
  failure modes.
- Limit `applyTo` to Daybook application code and its .NET/browser tests, not the
  separate sample applications.

## Next steps

- Review the instruction with application maintainers.
- Apply it when a runtime feature flag is introduced; no application flag was
  implemented in this task.
