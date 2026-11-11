# Todo Manager

Status: Complete; locally verified

## Confirmed requirements
- New todo list manager application.
- .NET backend with server-side persistence shared across devices.
- Responsive interface with task creation, editing, deletion, completion,
  priority, due dates, search, and status filters.

## Proposed implementation
- ASP.NET Core on .NET 10, serving a same-origin browser interface.
- SQLite persistence on the server.
- Automated tests and local run instructions.

## Approved scope
- One shared list without login; local/trusted-network use only.
- Local build and verification only. No Azure setup or deployment.
- Same-origin static HTML, CSS, and JavaScript frontend.
- SQLite database stored outside the public web root.
- Integration tests covering CRUD, validation, and durable persistence.

## Deployment
No cloud resources will be provisioned without explicit approval.

## Verification
- 12 integration tests pass, including persistence across host restarts.
- Browser verification passes: CRUD, completion/reopening, search, filters,
  overdue counters, shared-browser data, reload persistence, mobile layout,
  literal task text, and visible network errors.
- Build passes with warnings treated as errors.
- Dependency audit reports no known vulnerable packages.
- Due dates use DateOnly because they represent calendar dates; creation
  timestamps use DateTimeOffset.
- Azure validation and deployment are out of scope.
