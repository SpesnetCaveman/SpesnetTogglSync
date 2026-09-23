# AGENTS.md — SpesnetTogglSync

Guidance for AI agents working in this repository.

## What this project is

A **.NET 10 WinForms** personal tool that syncs **Toggl Track** time entries into **Spesnet / EvolveMed Timekeeping** (`gateway_internal.evolvemed.co.za/api-evolveTimekeepingAPI`).

Primary entry: `SpesnetTogglSync/Program.cs` → `TrayApplicationContext` (notification area) → `SyncForm.cs` → `Services/SyncService.cs`.

The tray icon shows whether today's sync is still waiting (amber clock), finished (green check), or has a problem (red exclamation, kept until the next successful sync). It syncs at the Settings daily sync time (default 07:00 SAST) as soon as the PC is on. If the PC was off then, the next launch that day syncs immediately. Cancel today's sync on the tray menu skips that run. Sync problems (cannot sync, missing description, unresolved mappings, and similar) raise a Windows balloon. A finished sync balloon shows hours just synced times the hourly rate, plus hours and earnings for the open billing cycle. Closing the form hides it; Exit is on the tray menu. `autosync.json` stores the daily schedule and the last saved billing-report cycle. `--tray` starts hidden and is what Windows startup launches.

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
5. **Duration > 8 hours** → split into multiple Spesnet rows ≤ 8h; always use `normalHours` (overtime = 0). Round each entry’s hours to the nearest 0.05 before that split (6.66 → 6.65, 6.68 → 6.70; half away from zero; 0.05h = 180 seconds). An entry that rounds to 0 is not posted, but the watermark still advances. This rounding is only for Spesnet `normalHours`. The earnings balloon and the invoice use the original Toggl seconds. The real client still rounds `normalHours` to 2 decimals on save so the JSON stays at 2 places.
6. **Minimize Toggl calls**: sync uses `GET /me/time_entries?start_date=&end_date=&meta=true` (end_date = now). Clients/projects fetch only for mapping UI refresh. After that, one more time-entry call covers the open billing cycle for the earnings notice. On the first sync of a new cycle (or the next time a sync runs, if the PC was off), save the previous cycle with `POST /reports/api/v3/workspace/{id}/summary/time_entries.pdf` into `{billing report folder}/{yyyy-MM}/toggle timesheet report {start} to {end}.pdf`, and a PDF invoice from the Word template beside it. **Create report** on the main form (next to Start Sync) saves both without waiting for the daily sync. Each billing period keeps one invoice number in `invoice-numbers.json`; regenerating that period reuses it, and only the next period increments. `InvoiceNumber` in Settings is the latest issued number. A new period uses the next number and updates that setting. The same period keeps the number already stored in `invoice-numbers.json`. When both the setting and the ledger are empty, the template number is the last issued invoice and this period gets the next one, unless the template date is already this period’s end date — then that template number is kept. Skip that save while any non-Ignore entry in the finished cycle still has a start after the watermark. A running timer stops the run like any other entry that cannot sync yet. Overlaps still sync (watermark = start); log a warning. Do not sync entries that start at the same time as the stopped entry.
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
| `SpesnetTogglSync/TrayApplicationContext.cs` | Notification-area icon, daily sync after the configured time, unattended sync |
| `SpesnetTogglSync/SyncForm.cs` | UI: sync bar, tabs (log, mapping, settings) |
| `SpesnetTogglSync/Services/SyncService.cs` | Orchestration, validation, transform, watermark, 0.05h rounding |
| `SpesnetTogglSync/Services/BillingService.cs` | Earnings notice, timesheet PDF, invoice PDF |
| `SpesnetTogglSync/Services/InvoiceDocument.cs` | Word template copy → invoice PDF (template file is not modified) |
| `SpesnetTogglSync/Services/InvoiceNumbers.cs` | One invoice number per period in `invoice-numbers.json` |
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

Never commit `appsettings.json`, `syncstate.json`, `autosync.json`, `invoice-numbers.json`, `invoice-template.docx`, `mappings.json`, `config-location.json`, `logs/`, or billing PDFs. The invoice template contains personal details and lives only in the data directory. Update `appsettings.example.json` / `config-location.example.json` when adding settings keys. Billing reports live at `{billing report folder}/{yyyy-MM}/`. The timesheet name is exactly `toggle timesheet report {yyyy-MM-dd} to {yyyy-MM-dd}.pdf` (the word is `toggle`). The invoice PDF is `Gerrie Pretorius  Invoice - {yyyy-MM} {Month}.pdf` (two spaces before Invoice; month is the period end).

## Invoice PDF

The invoice template used at runtime is `{data directory}/invoice-template.docx`. On first use it is copied from `InvoiceTemplatePath` when that file exists and the data-directory copy does not. After the copy exists, edits to the original file are ignored. Settings shows that path and **Import** replaces the app copy. Do not commit the docx. Word fills a temporary copy of the app template (labels `INVOICE:`, `Date:`, the consultation row, hours, rate, amount, and total); the app template is not overwritten. Late-bound Word COM is enough; do not add an Office NuGet package. The report waits while the hourly rate is 0 or the app template is missing.

Fields written into the copy: invoice number (`000`; when `invoice-numbers.json` is empty, the template number plus one, or the template number itself when its date is already this period’s end date), date = period end `yyyy-MM-dd`, description `Consultation services {d MMMM yyyy} – {d MMMM yyyy}` (en-dash), hours `0.00` from Toggl seconds, rate without decimals when it is a whole number, line amount and total `R ` plus a no-break space as the thousands separator. The ledger key is `{period start}_{period end}` (`yyyy-MM-dd_yyyy-MM-dd`). The number is stored before the PDFs are written, so a failed export retries the same number.

## Data directory (bootstrap)

`config-location.json` next to the exe is a small pointer (`dataDirectory`) so settings/mappings/syncstate/logs/`invoice-template.docx` can live under a cloud-backed folder. Read it first; if missing, use the install directory. Do not put the data path only inside `appsettings.json` (chicken-and-egg). The Settings tab exposes Data Directory; Save Settings writes the pointer, creates the folder, copies missing files from the previous data directory when needed, and creates any missing config files.

## Coding preferences

- Match existing C# style (file-scoped namespaces, nullable enabled, implicit usings).
- Prefer `HttpClient` + `System.Text.Json` / `System.Net.Http.Json` already in use — avoid new NuGet packages unless necessary.
- Keep UI logic in the form thin; business rules in `SyncService` / clients.
- Do not remove mock mode when adding Spesnet features — extend both implementations of the interface.
- Route all new live Spesnet/Toggl HTTP through `SpesnetApiHttp` / `TogglApiHttp.SendAsync` so the central failure breakpoint still covers them.
