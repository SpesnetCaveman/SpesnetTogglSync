# SpesnetTogglSync

Windows Forms (.NET 10) desktop app that syncs **Toggl Track** time entries into **Spesnet / EvolveMed Timekeeping**.

## Purpose

You track time in Toggl. This app pulls entries newer than a persisted watermark, maps Toggl clients/projects onto Spesnet project/client/work-task IDs, and posts work done to Spesnet in bulk. A DateTimePicker shows “synced up to” so the next run continues where the last one left off.

## Quick start

```powershell
dotnet run --project SpesnetTogglSync\SpesnetTogglSync.csproj
```

1. **Settings** — paste your Toggl API token (profile → API token). Set the hourly rate and the billing-cycle start day. Keep **Use mock Spesnet** checked for local testing.
2. **Mapping** — Refresh from Toggl / Spesnet as needed. Set each row’s **Status** to Active (fill Spesnet fields) or Ignore. Click **Save Mappings** (unsaved edits show an amber label and a `*` in the title).
3. Set **Sync from**, click **Start Sync**. Watch **Sync Log**.

Production: uncheck mock mode, enter Spesnet credentials, click **Refresh Spesnet Reference Data**, then sync.

## Mapping model

One **entry mapping** row defines status plus the destination for a Toggl client + project pair:

| Field | Role |
|-------|------|
| Status | `Active` (sync), `Ignore` (skip), or `New` (blocks sync until resolved) |
| Toggl Client + Project | Match key (empty project + Ignore = ignore whole client) |
| Spesnet Project + Client + Work task | Destination when Active |
| Description → Comment | Copied onto Spesnet work done |

- **Ignore** is the only skip that sync continues past. Any other entry that should sync but cannot (still running, missing client/project/description, `New` or missing mapping, invalid Spesnet destination) stops the run. Entries before it are saved; later ones wait so the watermark cannot skip the blocked entry.
- Each entry’s hours are rounded to the nearest 0.05 before they are sent (6.66 becomes 6.65, 6.68 becomes 6.70). Entries longer than 8 hours are then split into ≤8h chunks (all `normalHours`).

## Billing

Settings store an hourly rate (ZAR) and the day of the month a cycle starts. A start day of 20 means the 20th through the 19th of the next month.

When a sync finishes, the notification shows hours just synced × rate, and hours and earnings for the cycle so far (stopped Toggl entries whose start falls in that cycle, South African dates).

The daily sync runs at the time in Settings (default 07:00 South African time), or as soon as the PC is on after that. Cancel today's sync on the tray menu skips that day.

The first sync on or after a new cycle starts saves the previous cycle from the Toggl summary report, and an invoice PDF filled from the app's Word template in the data folder (`invoice-template.docx`), into `{billing report folder}/yyyy-MM/`. The timesheet file is named `toggle timesheet report {start} to {end}.pdf`. The report folder is a setting; when it is empty the files go in the data directory. The `yyyy-MM` folder is the month the new cycle starts. The report waits if that finished cycle still has time entries that have not synced (Ignore entries do not count). If the PC was off, the next sync still saves it. **Create report**, next to Start Sync, saves both files without waiting. Each period is given one invoice number; running the report again for that period keeps the same number. Example: start day 20, sync on 22 Sep 2026 → folder `2026-09`, period 20 Aug 2026–19 Sep 2026.

## Runtime files (gitignored)

Optional bootstrap next to the exe:

| File | Role |
|------|------|
| `config-location.json` | Points `dataDirectory` at where the files below live (e.g. OneDrive). If missing, they stay next to the exe. |

Data files (in `dataDirectory`, or next to the exe when no pointer):

| File | Role |
|------|------|
| `appsettings.json` | Credentials, domain, `UseMockSpesnet`, hourly rate, billing-cycle start day, cached Spesnet reference |
| `{yyyy-MM}/` report files | Toggl summary PDF and invoice PDF for a finished billing cycle, under the billing report folder (or the data directory when that setting is empty). The `yyyy-MM` folder is the month the new cycle starts. **Create report** next to Start Sync saves them without waiting for the daily sync |
| `invoice-numbers.json` | Invoice number already used for each billing period. Regenerating a period reuses its number |
| `syncstate.json` | `lastSyncedStartTime` watermark |
| `mappings.json` | Per–Toggl-user selections and mappings |
| `logs/sync-YYYYMMDD.log` | Audit / debug trail |

Templates: `appsettings.example.json`, `config-location.example.json`.

## Architecture (high level)

```
SyncForm → SyncService → SpesnetTogglSync.TogglApi   (TogglApiClient)
                      → SpesnetTogglSync.SpesnetApi  (mock or real)
ConfigService persists settings / mappings / watermark
FileLogger (IApiLogger) writes logs/ and UI log tab
```

- **Toggl sync call**: typically one `GET /me/time_entries?start_date=…&end_date=…&meta=true` per run (`end_date` is now).
- **Spesnet mock**: `MockSpesnetTimekeepingClient` + `Data/mock-spesnet-reference.json` — logs save payloads, no production API.
- **Watermark**: advanced and saved after **each** successful Toggl entry save.
- **Debug API failures**: set one breakpoint in `TogglApiHttp.CreateFailure` and/or `SpesnetApiHttp.CreateFailure` (auto-breaks when a debugger is attached; exception message is the AI prompt for error popups).

## Project layout

```
SpesnetTogglSync.slnx
├── SpesnetTogglSync/                 # WinForms UI, SyncService, Config, FileLogger
│   ├── SyncForm*.cs
│   ├── Services/
│   ├── Data/                         # Mock Spesnet reference data
│   ├── appsettings.example.json
│   └── config-location.example.json  # Bootstrap pointer to data directory
├── SpesnetTogglSync.Shared/          # Models + IApiLogger
├── SpesnetTogglSync.TogglApi/        # Toggl Track library (central TogglApiHttp)
└── SpesnetTogglSync.SpesnetApi/      # Spesnet library (central SpesnetApiHttp + mock)
```
## Auth notes

- **Toggl**: API token as Basic Auth username, password literal `api_token`.
- **Spesnet**: `POST api/Account/Login` → ASP.NET Identity cookie on subsequent calls.

## Docs for Cursor AI

See `AGENTS.md` and `.cursor/rules/` for agent-oriented project context and conventions.
