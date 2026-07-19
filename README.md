# SpesnetTogglSync

Windows Forms (.NET 10) desktop app that syncs **Toggl Track** time entries into **Spesnet / EvolveMed Timekeeping**.

## Purpose

You track time in Toggl. This app pulls entries newer than a persisted watermark, maps Toggl clients/projects onto Spesnet project/client/work-task IDs, and posts work done to Spesnet in bulk. A DateTimePicker shows “synced up to” so the next run continues where the last one left off.

## Quick start

```powershell
dotnet run --project SpesnetTogglSync\SpesnetTogglSync.csproj
```

1. **Settings** — paste your Toggl API token (profile → API token). Keep **Use mock Spesnet** checked for local testing.
2. **Toggl Clients** — Refresh from Toggl, check which clients to sync.
3. **Client Mapping** / **Project Mapping** — map Toggl entities to Spesnet.
4. Set **Sync from**, click **Start Sync**. Watch **Sync Log**.

Production: uncheck mock mode, enter Spesnet credentials, click **Refresh Spesnet Reference Data**, then sync.

## Mapping model

| Toggl | Spesnet |
|-------|---------|
| Client | Project **+** Client |
| Project | Work task |
| Description | Comment |

- Only checked Toggl clients are synced.
- Entries missing client/project, or with unmapped entities, **abort the sync** with a clear message (no partial writes after validation fails).
- Entries longer than 8 hours are split into ≤8h chunks (all `normalHours`).

## Runtime files (gitignored)

| File | Role |
|------|------|
| `appsettings.json` | Credentials, domain, `UseMockSpesnet`, cached Spesnet reference |
| `syncstate.json` | `lastSyncedStartTime` watermark |
| `mappings.json` | Per–Toggl-user selections and mappings |
| `logs/sync-YYYYMMDD.log` | Audit / debug trail |

Template: `appsettings.example.json`.

## Architecture (high level)

```
SyncForm → SyncService → TogglApiClient (real)
                      → ISpesnetTimekeepingClient (mock or real)
ConfigService persists settings / mappings / watermark
FileLogger writes logs/ and UI log tab
```

- **Toggl sync call**: typically one `GET /me/time_entries?start_date=…&meta=true` per run.
- **Spesnet mock**: `MockSpesnetTimekeepingClient` + `Data/mock-spesnet-reference.json` — logs save payloads, no production API.
- **Watermark**: advanced and saved after **each** successful Toggl entry save.

## Project layout

```
SpesnetTogglSync/
├── SyncForm*.cs          # UI
├── Models/               # DTOs, settings, mappings
├── Services/             # Config, Toggl, Spesnet, Sync, logging
├── Data/                 # Mock Spesnet reference data
└── appsettings.example.json
```

## Auth notes

- **Toggl**: API token as Basic Auth username, password literal `api_token`.
- **Spesnet**: `POST api/Account/Login` → ASP.NET Identity cookie on subsequent calls.

## Docs for Cursor AI

See `AGENTS.md` and `.cursor/rules/` for agent-oriented project context and conventions.
