# AGENTS.md — SpesnetTogglSync

Guidance for AI agents working in this repository.

## What this project is

A **.NET 10 WinForms** personal tool that syncs **Toggl Track** time entries into **Spesnet / EvolveMed Timekeeping** (`gateway_internal.evolvemed.co.za/api-evolveTimekeepingAPI`).

Primary entry: `SpesnetTogglSync/SyncForm.cs` → `Services/SyncService.cs`.

## Domain mapping (do not invent alternate mappings)

- Toggl **client** → Spesnet **project id + client id** (clients are loaded per project via `GetClientsByProject`).
- Toggl **project** → Spesnet **work task id**.
- Toggl **description** → Spesnet **comment**.
- Sync only for the **current Toggl user**; mappings are stored **per Toggl user id** in `mappings.json`.
- Spesnet hierarchy in APIs differs from Toggl: employee → projects → clients-by-project; work tasks are a separate list.

## Sync invariants (preserve these)

1. **Watermark**: `syncstate.json` / DateTimePicker — only entries with `start > watermark`. After each successful Spesnet save for a Toggl entry, persist the new watermark immediately.
2. **Validate all, then write**: mapping/client/project validation runs on the full candidate set before any Spesnet save. Fail with a user-facing message naming the missing map.
3. **Selected clients only**: unchecked Toggl clients are skipped (not an error). Require ≥1 selected client.
4. **Missing Toggl client or project** on an entry → abort; include entry date/time in the message.
5. **Duration > 8 hours** → split into multiple Spesnet rows ≤ 8h; always use `normalHours` (overtime = 0).
6. **Minimize Toggl calls**: sync uses `GET /me/time_entries?start_date=&meta=true`. Clients/projects fetch only for mapping UI refresh.
7. **Mock by default**: `UseMockSpesnet: true` → `MockSpesnetTimekeepingClient`. Real client uses cookie login. Prefer keeping both behind `ISpesnetTimekeepingClient`.

## Key files

| Path | Role |
|------|------|
| `SyncForm.cs` / `SyncForm.Designer.cs` | UI: sync bar, tabs (log, clients, mappings, settings) |
| `Services/SyncService.cs` | Orchestration, validation, transform, watermark |
| `Services/TogglApiClient.cs` | Toggl Track API v9 (token auth) |
| `Services/SpesnetTimekeepingClient.cs` | Real Spesnet HTTP + cookies |
| `Services/MockSpesnetTimekeepingClient.cs` | Local test double |
| `Services/ConfigService.cs` | `appsettings.json`, `syncstate.json`, `mappings.json` |
| `Services/FileLogger.cs` | `logs/sync-YYYYMMDD.log` + UI events |
| `Data/mock-spesnet-reference.json` | Mock projects/clients/tasks |
| `appsettings.example.json` | Template without secrets |

## Spesnet API surface (real)

- `POST api/Account/Login` — cookie `.AspNetCore.Identity.Application`
- `GET api/User/GetUserInfo`
- `GET api/employee/GetEmployeeByDate?workDate=…` — use `currentUser.id`
- `GET api/Project/GetProjectForEmployee?employeeId=…`
- `GET api/Client/GetClientsByProject?projectId=…`
- `GET api/worktask`
- `POST api/worktask/save` — body `{ workDoneList, accessKey }`

`txdatetime` is midnight UTC for the entry’s start date: `yyyy-MM-ddT00:00:00.000Z`.

## Naming collision

Model `Models.TogglClient` vs HTTP helper `Services.TogglApiClient`. Do **not** rename the HTTP class back to `TogglClient`.

## Secrets and git

Never commit `appsettings.json`, `syncstate.json`, `mappings.json`, or `logs/`. Update `appsettings.example.json` when adding settings keys.

## Coding preferences

- Match existing C# style (file-scoped namespaces, nullable enabled, implicit usings).
- Prefer `HttpClient` + `System.Text.Json` / `System.Net.Http.Json` already in use — avoid new NuGet packages unless necessary.
- Keep UI logic in the form thin; business rules in `SyncService` / clients.
- Do not remove mock mode when adding Spesnet features — extend both implementations of the interface.
