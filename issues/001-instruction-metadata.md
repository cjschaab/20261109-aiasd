---
ai_generated: true
model: "OpenAI/unknown@unknown"
operator: "jMM-at-CODETraining"
chat_id: "instruction-compliance-issues-20261112"
prompt: |
  create issues for the findings and put them in the issues folder as markdown files
started: "2026-11-12T11:10:00.322-08:00"
ended: "2026-11-12T11:15:11.457-08:00"
task_durations:
  - task: "Create Markdown issues from instruction compliance audit findings"
    duration: "00:05:11"
total_duration: "00:05:11"
ai_log: "../ai-logs/2026/11/12/instruction-compliance-issues-20261112/conversation.md"
source: "user"
revisions:
  - chat_id: "prioritize-local-issues-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Add impact, effort, risk, and recommended priority assessment."
    ai_log: "../ai-logs/2026/11/12/prioritize-local-issues-20261112/conversation.md"
  - chat_id: "issues-conformance-20261112"
    model: "OpenAI/unknown@unknown"
    date: "2026-11-12"
    change: "Restructure the local issue draft to follow GitHub issue authoring guidance and add verified evidence."
    ai_log: "../ai-logs/2026/11/12/issues-conformance-20261112/conversation.md"
---

# Make instruction-file provenance and governance metadata consistent

**Audit rule:** RULE-001

## Summary

Bring active instruction files into conformance with the repository's provenance
and governance metadata requirements.

## Context

The instruction-authoring guide defines provenance and governance metadata, but
the current instruction files do not apply those requirements consistently.
The scope includes confirming whether the empty C# instruction file is
intentional and correcting unknown provenance only when its value can be
verified.

## Assessment

- **Impact:** Medium - inconsistent metadata makes instruction files harder to
  trace and maintain.
- **Effort:** Medium - audit active files and resolve empty or duplicate files
  without inventing provenance.
- **Risk:** Medium - broad metadata edits can introduce inaccurate history or
  broken links if not verified.
- **Recommended priority:** P2 - resolve the metadata-field ambiguity in
  [008](008-metadata-field-count.md) first; then coordinate provenance checks
  with [002](002-missing-provenance-logs.md).

## Expected outcome

Every active instruction file has accurate, consistent provenance and
governance metadata, with empty or duplicate files given a verified disposition.

## Acceptance criteria

- [ ] Active instruction files meet the applicable provenance and governance
  requirements.
- [ ] The empty C# instruction file has a documented purpose or is removed after
  confirming it is no longer needed.
- [ ] Unknown provenance remains explicitly unknown unless its actual value can
  be established.
- [ ] Metadata and referenced links are checked for consistency.

## Evidence

- The [instruction-authoring guide](../.github/instructions/instruction-files.instructions.md#L181-L228)
  distinguishes provenance and governance metadata requirements.
- The [AI-output instructions](../.github/instructions/ai-assisted-output.instructions.md#L67-L100)
  define the required artifact provenance fields.
- The [C# instruction file](../.github/instructions/csharp.instructions.md)
  exists as an empty file in the current workspace.

## Dependencies and scope

- [008](008-metadata-field-count.md) should be resolved first or alongside this
  work so the required provenance-field count is unambiguous.
- Coordinate validation of log paths with [002](002-missing-provenance-logs.md).
- Do not infer or fabricate historical model or prompt information.
