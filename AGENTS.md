# AGENTS.md — SpesnetTogglSync

Guidance for AI agents working in this repository.

## What this project is

A **.NET 10 WinForms** personal tool that syncs **Toggl Track** time entries into **Spesnet / EvolveMed Timekeeping** (`gateway_internal.evolvemed.co.za/api-evolveTimekeepingAPI`).

Primary entry: `SpesnetTogglSync/Program.cs` → `TrayApplicationContext` (notification area) → `SyncForm.cs` → `Services/SyncService.cs`.

The tray icon prompts at 12:00 SAST and, unless cancelled within one hour, syncs without opening the form. Sync problems (cannot sync, missing description, unresolved mappings, and similar) raise a Windows balloon. Closing the form hides it; Exit is on the tray menu. `autosync.json` stores the daily schedule. `--tray` starts hidden and is what Windows startup launches.

Solution projects:

| Project | Role |
|---------|------|
| `SpesnetTogglSync` | WinForms UI, `SyncService`, `ConfigService`, `FileLogger` |
| `SpesnetTogglSync.Shared` | Models + `IApiLogger` |
| `SpesnetTogglSync.TogglApi` | Toggl Track HTTP library |
| `SpesnetTogglSync.SpesnetApi` | Spesnet HTTP library (real + mock) |

## Domain mapping (do not invent alternate mappings)

- One **entry mapping** matches on Toggl **client + project** together and sets Spesnet **project id + client id + work task id**, plus a **status** (`Active` / `Ignore` / `New`).
- Mapping list is auto-populated with one row per live Toggl client+project; newly discovered pairs start as `New`.
- Spesnet clients are loaded per project via `GetClientsByProject` (project first).
- Spesnet **comment** = mapping `CommentPrefix` + Toggl **description** (prefix defaults empty; concatenated as-is).
- Sync only for the **current Toggl user**; mappings are stored **per Toggl user id** in `mappings.json`.
- Spesnet hierarchy in APIs differs from Toggl: employee → projects → clients-by-project; work tasks are a separate list.

## Sync invariants (preserve these)

1. **Watermark**: `syncstate.json` / DateTimePicker — only entries with `start > watermark`. After each successful Spesnet save for a Toggl entry, persist the new watermark immediately.
2. **Stop at the first entry that cannot sync yet**: entries before it are saved; that entry and anything later are not. The watermark must stay before it so the next run fetches it again. Reasons include a running timer, missing client/project/description, a missing or `New` mapping, and an invalid Spesnet destination.
3. **Mapping status**: each entry mapping is `Active`, `Ignore`, or `New`. `Ignore` is the only skip that continues (those entries should never sync). `New` stops the run at the first matching entry. `Active` requires a full Spesnet destination. A client-only row (empty project) with `Ignore` skips every project for that client.
4. **Missing Toggl client, project, or description** stops the run at that entry (entries before it are already saved). Include entry id, South African (GMT+2) date/time, and client/project in the message so the user can fix it in Toggl. Whitespace-only description counts as missing.
5. **Duration > 8 hours** → split into multiple Spesnet rows ≤ 8h; always use `normalHours` (overtime = 0).
6. **Minimize Toggl calls**: sync uses `GET /me/time_entries?start_date=&end_date=&meta=true` (end_date = now). Clients/projects fetch only for mapping UI refresh. A running timer stops the run like any other entry that cannot sync yet. Overlaps still sync (watermark = start); log a warning. Do not sync entries that start at the same time as the stopped entry.
7. **Mock by default**: `UseMockSpesnet: true` → `MockSpesnetTimekeepingClient`. Real client uses cookie login. Prefer keeping both behind `ISpesnetTimekeepingClient`.
8. **User-facing entry times** in validation / abort messages use South African (GMT+2 / SAST), not UTC.

## Central API failure breakpoints

Every live HTTP call goes through one transport per integration. Failures (non-success status or transport exception) hit a single gate:

| Integration | Class | Method |
|-------------|-------|--------|
| Toggl | `TogglApi/TogglApiHttp.cs` | `CreateFailure` |
| Spesnet | `SpesnetApi/SpesnetApiHttp.cs` | `CreateFailure` |

When a debugger is attached, `Debugger.Break()` runs there. Inspect locals: `aiPrompt` (copy/paste to an AI as-is), `operation`, `requestUrl`, `requestPayload`, `response`, `rawResponse`, `exception`. The same `aiPrompt` is thrown as `HttpRequestException.Message` so UI error popups show it. Do **not** add per-endpoint breakpoints for API error inspection — extend these gates instead.

## Key files

| Path | Role |
|------|------|
| `SpesnetTogglSync/TrayApplicationContext.cs` | Notification-area icon, daily 12:00 prompt, unattended sync |
| `SpesnetTogglSync/SyncForm.cs` | UI: sync bar, tabs (log, mapping, settings) |
| `SpesnetTogglSync/Services/SyncService.cs` | Orchestration, validation, transform, watermark |
| `SpesnetTogglSync.TogglApi/TogglApiClient.cs` | Toggl Track API v9 (token auth) |
| `SpesnetTogglSync.TogglApi/TogglApiHttp.cs` | Central Toggl send + failure breakpoint |
| `SpesnetTogglSync.SpesnetApi/SpesnetTimekeepingClient.cs` | Real Spesnet HTTP + cookies |
| `SpesnetTogglSync.SpesnetApi/SpesnetApiHttp.cs` | Central Spesnet send + failure breakpoint |
| `SpesnetTogglSync.SpesnetApi/MockSpesnetTimekeepingClient.cs` | Local test double |
| `SpesnetTogglSync/Services/ConfigService.cs` | Bootstrap `config-location.json` + data-dir `appsettings` / `syncstate` / `mappings` |
| `SpesnetTogglSync/Services/FileLogger.cs` | `logs/sync-YYYYMMDD.log` under data dir + UI events (`IApiLogger`) |
| `SpesnetTogglSync.Shared/Models/` | Shared DTOs and settings |
| `SpesnetTogglSync/Data/mock-spesnet-reference.json` | Mock projects/clients/tasks |
| `SpesnetTogglSync/appsettings.example.json` | Template without secrets |
| `SpesnetTogglSync/config-location.example.json` | Bootstrap pointer template for relocatable data dir |

## Spesnet API surface (real)

- `POST api/Account/Login` — cookie `.AspNetCore.Identity.Application`
- `GET api/User/GetUserInfo`
- `GET api/employee/GetEmployeeByDate?workDate=…` — use `currentUser.id`
- `GET api/Project/GetProjectForEmployee?employeeId=…`
- `GET api/Client/GetClientsByProject?projectId=…`
- `GET api/worktask`
- `POST api/worktask/save` — body `{ workDoneList, accessKey }`

`txdatetime` is the entry start converted to **South African (GMT+2)** local time: `yyyy-MM-ddTHH:mm:ss.fff` (no `Z` / offset). Convert Toggl `start` from UTC to Africa/Johannesburg (do not send UTC clock time).

## Naming collision

Model `Models.TogglClient` vs HTTP helper `TogglApi.TogglApiClient`. Do **not** rename the HTTP class back to `TogglClient`.

## Secrets and git

Never commit `appsettings.json`, `syncstate.json`, `autosync.json`, `mappings.json`, `config-location.json`, or `logs/`. Update `appsettings.example.json` / `config-location.example.json` when adding settings keys.

## Data directory (bootstrap)

`config-location.json` next to the exe is a small pointer (`dataDirectory`) so settings/mappings/syncstate/logs can live under a cloud-backed folder. Read it first; if missing, use the install directory. Do not put the data path only inside `appsettings.json` (chicken-and-egg). The Settings tab exposes Data Directory; Save Settings writes the pointer, creates the folder, copies missing files from the previous data directory when needed, and creates any missing config files.

## Coding preferences

- Match existing C# style (file-scoped namespaces, nullable enabled, implicit usings).
- Prefer `HttpClient` + `System.Text.Json` / `System.Net.Http.Json` already in use — avoid new NuGet packages unless necessary.
- Keep UI logic in the form thin; business rules in `SyncService` / clients.
- Do not remove mock mode when adding Spesnet features — extend both implementations of the interface.
- Route all new live Spesnet/Toggl HTTP through `SpesnetApiHttp` / `TogglApiHttp.SendAsync` so the central failure breakpoint still covers them.
