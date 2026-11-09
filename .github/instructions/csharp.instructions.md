---
applyTo: "**/*.cs"
---

# C# Instructions

- Keep changes compatible with the project's .NET 10 target, nullable reference
  types, and implicit usings. Do not disable nullable analysis to work around
  warnings.
- Follow the surrounding code's formatting and naming conventions. Prefer clear,
  descriptive names and straightforward control flow.
- Use nullable annotations to represent optional values, and validate external
  input at the boundary before using it. Preserve established API behavior and
  response shapes when changing endpoints.
- Keep ASP.NET Core endpoints consistent with the existing minimal API patterns.
  Return appropriate HTTP results for invalid input and missing resources; do
  not hide unexpected failures with broad exception handling.
- Add or update focused xUnit tests for behavior changes, including relevant
  edge cases. Keep test data isolated and clean up temporary resources.
- Run `dotnet test TodoApp.sln` after C# changes when practical, and report any
  failures or validation that could not be completed.
