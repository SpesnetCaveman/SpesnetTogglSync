# SpesnetTogglSync

Windows Forms (.NET 10) desktop app that syncs **Toggl Track** time entries into **Spesnet / EvolveMed Timekeeping**.

## Purpose

You track time in Toggl. This app pulls entries newer than a persisted watermark, maps Toggl clients/projects onto Spesnet project/client/work-task IDs, and posts work done to Spesnet in bulk. A DateTimePicker shows “synced up to” so the next run continues where the last one left off.

## Quick start

```powershell
dotnet run --project SpesnetTogglSync\SpesnetTogglSync.csproj
```

1. **Settings** — paste your Toggl API token (profile → API token). Keep **Use mock Spesnet** checked for local testing.
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

- Rows left on **New** block sync until changed to Active or Ignore.
- Entries missing client/project, or with no matching Active mapping (and no client-level Ignore), **abort the sync** with a clear message (no partial writes after validation fails).
- Entries longer than 8 hours are split into ≤8h chunks (all `normalHours`).

## Runtime files (gitignored)

Optional bootstrap next to the exe:

| File | Role |
|------|------|
| `config-location.json` | Points `dataDirectory` at where the files below live (e.g. OneDrive). If missing, they stay next to the exe. |

Data files (in `dataDirectory`, or next to the exe when no pointer):

| File | Role |
|------|------|
| `appsettings.json` | Credentials, domain, `UseMockSpesnet`, cached Spesnet reference |
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
