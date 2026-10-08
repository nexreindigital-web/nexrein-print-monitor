# PRINTMONITOR — Windows Print Monitoring and Accounting Agent

A production-ready Windows Service agent designed for real-time print job monitoring, accounting, and synchronization across Windows workstations and print servers.

Built with modern **.NET 10**, native **Win32 Print Spooler APIs (`winspool.drv`)**, high-performance **SQLite local storage (`Microsoft.Data.Sqlite`)**, resilient **offline batch synchronization**, and packaged with an **Inno Setup** enterprise installer (`PrintMonitor-Setup.exe`).

---

## Architecture Overview

```
                                      ┌─────────────────────────────────┐
                                      │      Windows Print Spooler      │
                                      │    (winspool.drv native APIs)   │
                                      └────────────────┬────────────────┘
                                                       │
                                  Native Job / Printer Notifications
                                                       │
                                                       ▼
                                      ┌─────────────────────────────────┐
                                      │       PrintMonitor Agent        │
                                      │       (Windows Service)         │
                                      │                                 │
                                      │  • Win32PrintSpooler            │
                                      │  • PrintJobMonitorService       │
                                      │  • PrinterDiscoveryService      │
                                      │  • DeviceRegistrationService    │
                                      │  • SyncService                  │
                                      │  • HealthCheckService           │
                                      └───────┬─────────────────┬───────┘
                                              │                 │
                         Local SQLite Storage │                 │ HTTPS REST API
                                 (WAL Mode)   │                 │ (Offline Resilient)
                                              ▼                 ▼
             ┌──────────────────────────────────┐     ┌──────────────────────────────────┐
             │       Local SQLite Database      │     │      Remote Laravel Backend      │
             │   C:\ProgramData\PrintMonitor\   │     │        (Printers, Users,         │
             │         printmonitor.db          │     │     Accounting, Dashboards)      │
             └──────────────────────────────────┘     └──────────────────────────────────┘
```

---

## Key Features

1. **Native Print Spooler Integration (No System.Printing)**
   - Uses low-level Win32 APIs: `OpenPrinter`, `ClosePrinter`, `EnumPrinters`, `EnumJobs`, `GetJob`, `FindFirstPrinterChangeNotification`, and `FindNextPrinterChangeNotification`.
   - Captures detailed DevMode parameters: Copies, Color mode (Monochrome vs Color), Duplex (Simplex, Duplex Long Edge, Duplex Short Edge), and Paper Size (Letter, A4, A3, Legal, etc.).
   - Discovers all printer topologies: Local, USB, Network (TCP/IP), Shared, and Virtual printers.

2. **Accurate Job Lifecycle & Disappearance Tracking**
   - Tracks jobs from `Submitted` ➔ `Printing` ➔ `Completed` / `Error` / `Cancelled`.
   - Windows Spooler removes jobs after printing finishes; PrintMonitor detects spooler removal and preserves records locally with final page counts and completion timestamps.

3. **Guaranteed Offline Operation**
   - High-concurrency SQLite storage located at `C:\ProgramData\PrintMonitor\printmonitor.db`.
   - Operates in WAL mode (`PRAGMA journal_mode=WAL`).
   - If internet or remote API is unreachable, print records and heartbeats are safely preserved locally.
   - Batch synchronization retries automatically with exponential backoff when connection returns. Never deletes unsynchronized records.

4. **Zero-Duplicate Fingerprinting**
   - Windows Spooler resets Job IDs when old jobs clear or the spooler restarts.
   - PrintMonitor generates a composite unique fingerprint:
     `JobUid = {DeviceId}_{PrinterName}_{JobId}_{SubmittedAtUtcTicks}`
   - Reused Job IDs at different timestamps are stored as separate records.
   - Server-side idempotent upsert prevents duplicate entries on re-transmissions.

5. **Self-Contained Deployment**
   - Shipped as a self-contained 64-bit Windows application (`win-x64`).
   - Does **not** require target computers to pre-install the .NET Runtime.

6. **Enterprise Windows Service & Recovery**
   - Auto-starts with Windows (`start= auto`).
   - Configured with automatic recovery on failure (restarts on 1st, 2nd, and subsequent crashes).

---

## Project Structure

```
PrintMonitor/
├── PrintMonitor.sln
│
├── src/
│   └── PrintMonitor/
│       ├── Program.cs                  # CLI diagnostic tool & Windows Service entrypoint
│       ├── Worker.cs                   # BackgroundService lifecycle coordinator
│       ├── appsettings.json            # Base configuration
│       ├── PrintMonitor.csproj         # Self-contained .NET 10 project definition
│       │
│       ├── Models/
│       │   ├── PrintJob.cs             # Core print job data model
│       │   ├── PrinterInfo.cs          # Printer metadata and status
│       │   ├── ComputerInfo.cs         # Workstation / device hardware information
│       │   ├── AgentSettings.cs        # Application settings model
│       │   └── ApiModels.cs            # Laravel REST DTOs (Register, Heartbeat, Batch Sync)
│       │
│       ├── Data/
│       │   ├── DatabaseInitializer.cs  # SQLite schema creation, WAL pragma & indexes
│       │   └── PrintMonitorDbContext.cs # High-performance data access layer
│       │
│       ├── Native/
│       │   ├── Structures.cs           # Win32 spooler structs (JOB_INFO_2, DEVMODE, PRINTER_INFO_2)
│       │   ├── NativeMethods.cs        # P/Invoke signatures for winspool.drv and kernel32.dll
│       │   └── Win32PrintSpooler.cs    # Managed wrappers for native spooler operations
│       │
│       ├── Services/
│       │   ├── PrintSpoolerService.cs     # Spooler notification and query service
│       │   ├── PrintJobMonitorService.cs  # Real-time print job monitor loop
│       │   ├── PrinterDiscoveryService.cs # Dynamic printer enumeration
│       │   ├── ApiClient.cs               # Strongly-typed HTTP REST client
│       │   ├── DeviceRegistrationService.cs # Initial machine registration
│       │   ├── SyncService.cs             # Offline batch queue sync with backoff
│       │   └── HealthCheckService.cs      # Heartbeat beacon
│       │
│       ├── Configuration/
│       │   └── SettingsManager.cs      # Multi-tier configuration (appsettings + override JSON)
│       │
│       └── Utilities/
│           ├── MachineIdentifier.cs    # Persistent GUID generator and hardware inspection
│           ├── NetworkHelper.cs        # IP, MAC, and network status helper
│           └── Logger.cs               # Rolling file logger with automatic secret redaction
│
├── tests/
│   └── PrintMonitor.Tests/
│       ├── DatabaseTests.cs            # Schema creation and CRUD tests
│       ├── DuplicateDetectionTests.cs  # JobId reuse & duplicate detection tests
│       ├── SyncQueueTests.cs           # Offline queue retention and retry tests
│       ├── ApiClientTests.cs           # Mock HTTP client tests (Register, Heartbeat, Sync)
│       └── ConfigurationAndDeviceTests.cs # Config loading, persistent GUID & secret redaction
│
├── installer/
│   ├── PrintMonitor.iss                # Inno Setup 6 installer script
│   └── LICENSE.txt                     # Software license text
│
├── scripts/
│   ├── publish.ps1                     # Self-contained win-x64 publishing script
│   └── build-installer.ps1             # Compiles PrintMonitor-Setup.exe
│
├── backend-reference/                  # Complete Laravel 11/12 backend reference
│   ├── migrations/                     # Database migrations (devices, printers, print_jobs, sync_logs)
│   ├── models/                         # Eloquent models (Device, Printer, PrintJob)
│   ├── controllers/                    # DeviceController and PrintJobController
│   └── routes/api.php                  # REST API routes
│
└── dist/
    └── PrintMonitor-Setup.exe          # Production Windows installer executable
```

---

## Build Instructions

### Prerequisites
- .NET 10 SDK (or modern .NET SDK)
- Windows 10 / 11 / Server 2019+
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (for building the installer)

### Step 1: Restore and Build
```powershell
dotnet restore
dotnet build
```

### Step 2: Run Automated Tests
```powershell
dotnet test
```

### Automated One-Step Full Build
Run the automated build and packaging pipeline:
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```
This single command automatically restores NuGet packages, builds the solution, executes all automated unit & integration tests, publishes self-contained win-x64 binaries, and compiles the enterprise installer with Inno Setup.

The output executable deliverable is placed at:
```
release\PrintMonitor-Setup.exe
```

---

## Graphical Control Panel & Manager (GUI)

The agent suite includes a native Windows desktop GUI application: **`PrintMonitor.Manager.exe`**.

You can launch it via:
- Desktop Shortcut: **`PrintMonitor Manager`**
- Start Menu: **`PrintMonitor Control Panel`**
- Command Line: `PrintMonitor.exe --gui` (or directly `PrintMonitor.Manager.exe`)

### Key GUI Features
1. **Live Windows Service Control**:
   - Real-time service state indicator (Running / Stopped / Paused).
   - Instant **Start**, **Stop**, and **Restart Service** buttons.
   - Shows automatic restart recovery configuration (`reset= 86400 actions= restart/60000...`).
2. **Printed Pages & Job History Monitoring**:
   - Live counters: Total Pages Printed, Monochrome vs. Color breakdown, and Total Jobs.
   - Interactive searchable and filterable print job history table.
   - Includes document title, user name, client PC, printer, pages, copies, duplex, paper size, and cloud sync status.
   - **Export to CSV** and **Simulate Test Job** buttons.
3. **Installed Printers Inspector**:
   - Enumerates all Windows Print Spooler printers live via `winspool.drv`.
   - Displays printer status, port (USB/IP), driver, network/shared topology, and active queue counts.
   - Direct button to open Windows Printers and Scanners system settings.
4. **Website / Remote API Endpoint Manager**:
   - Edit the remote API Base URL (e.g. `https://your-domain.com/api`) and authentication token.
   - Configure sync intervals (30s default), heartbeat intervals (60s default), and spooler polling rate.
   - **Test Endpoint Connection**: Sends immediate health probe to verify server reachability and latency.
   - **Save Configuration**: Writes directly to `C:\ProgramData\PrintMonitor\config.json`.
   - **Trigger Sync Now**: Manually syncs pending local SQLite records to the remote server.
5. **Live Diagnostic Logs**:
   - Stream rolling service logs directly from `C:\ProgramData\PrintMonitor\Logs`.

## Command-Line Diagnostics & Administration

`PrintMonitor.exe` includes built-in administrative commands for verification, troubleshooting, and manual operations:

### 1. Show Comprehensive Status
```powershell
PrintMonitor.exe --status
```
Example Output:
```text
==================================================
  PrintMonitor Agent Diagnostic Status (v1.0.0)
==================================================
Service:      Running
Device ID:    0444e10b-eef5-4695-857c-88cd07dd8818
Database:     OK (72 KB)
Printers:     3
Total Jobs:   14
Pending Sync: 0
API:          Connected (https://your-domain.com/api)
==================================================
```

### 2. List Discovered Printers
```powershell
PrintMonitor.exe --printers
```
Example Output:
```text
Scanning printers from Windows Print Spooler and local database...
Found 3 printer(s):
----------------------------------------------------------------------------------------------------
Name                           | Status       | Port            | Driver                    | Net  | Jobs
----------------------------------------------------------------------------------------------------
Canon G2010 series             | Ready        | USB001          | Canon G2010 series        | No   | 0   
HP LaserJet Pro M404           | Ready        | 192.168.1.150   | HP LaserJet Pro M404      | Yes  | 0   
Microsoft Print to PDF         | Ready        | PORTPROMPT:     | Microsoft Print To PDF    | Yes  | 0   
----------------------------------------------------------------------------------------------------
```

### 3. Test Laravel API Connectivity
```powershell
PrintMonitor.exe --test-api
```

### 4. Trigger Immediate Batch Synchronization
```powershell
PrintMonitor.exe --sync
```

### 5. Install / Uninstall Windows Service Directly
```powershell
# Install Windows Service (Run as Administrator)
PrintMonitor.exe --install

# Uninstall Windows Service (Run as Administrator)
PrintMonitor.exe --uninstall
```

### 6. Display Version
```powershell
PrintMonitor.exe --version
```

---

## Windows Service Management

### Service Details
- **Service Name:** `PrintMonitor`
- **Display Name:** `PrintMonitor - Print Monitoring Agent`
- **Startup Type:** `Automatic`

### Managing via PowerShell / Command Prompt
```powershell
# Query status
sc.exe query PrintMonitor

# Start service
sc.exe start PrintMonitor

# Stop service
sc.exe stop PrintMonitor

# View recovery configuration
sc.exe qfailure PrintMonitor
```

---

## Configuration

Configuration values are resolved in the following priority order:
1. `C:\ProgramData\PrintMonitor\config.json` (created during installation or modified by administrators)
2. Environment variables (`PrintMonitor__ApiBaseUrl`, etc.)
3. `appsettings.json` (application directory defaults)

### Sample `C:\ProgramData\PrintMonitor\config.json`
```json
{
  "ApiBaseUrl": "https://print.company.com/api",
  "ApiKey": "pm_sec_99a8b7c6d5e4f3a2b1c0",
  "DeviceId": "0444e10b-eef5-4695-857c-88cd07dd8818",
  "SyncIntervalSeconds": 30,
  "HeartbeatIntervalSeconds": 60,
  "PollingIntervalSeconds": 5,
  "MaxBatchSize": 50,
  "MaxRetryAttempts": 5,
  "DatabasePath": "C:\\ProgramData\\PrintMonitor\\printmonitor.db",
  "LogDirectory": "C:\\ProgramData\\PrintMonitor\\Logs",
  "LogLevel": "Information"
}
```

---

## Logging & Security

- **Log Location:** `C:\ProgramData\PrintMonitor\Logs\printmonitor-YYYY-MM-DD.log`
- **Log Retention:** Automatically purges log files older than 14 days.
- **Secret Redaction:** `PrintMonitorFileLogger` automatically intercepts and redacts any API keys, tokens, or passwords before writing them to disk.
- **Windows Event Log:** Critical service errors and startup alerts are written to the Windows `Application` log with Source `PrintMonitor`.
- **Privacy:** Document contents are **never** inspected, captured, or transmitted. Only print accounting metadata (document title, pages, printer, user) is monitored.

---

## Laravel Backend Specification

The repository includes a ready-to-deploy reference implementation in `backend-reference/`:

### Endpoints
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/health` | Health check endpoint |
| `POST` | `/api/devices/register` | Registers computer, generates API token |
| `POST` | `/api/devices/heartbeat` | Updates last seen timestamp and printer count |
| `POST` | `/api/print-jobs/sync` | Batch synchronization of print jobs |
| `GET` | `/api/dashboard/summary` | Dashboard statistics (prints, pages, color vs mono) |

### Installing into Laravel
Copy files from `backend-reference/` into your Laravel project:
```bash
cp backend-reference/migrations/* database/migrations/
cp backend-reference/models/* app/Models/
cp backend-reference/controllers/* app/Http/Controllers/
cat backend-reference/routes/api.php >> routes/api.php
php artisan migrate
```

---

## Manual Verification & Testing Checklist

| # | Step | Expected Result | Verified |
|---|---|---|:---:|
| 1 | Run `PrintMonitor-Setup.exe` | Wizard guides installation, enters API URL, registers service | [x] |
| 2 | Check `sc query PrintMonitor` | Service status is `RUNNING` with automatic startup | [x] |
| 3 | Run `PrintMonitor.exe --status` | Shows service `Running`, DB `OK`, and detected printers | [x] |
| 4 | Print a 1-page document | Job detected, captured in `print_jobs` table | [x] |
| 5 | Disconnect internet / disable network | Agent continues running silently | [x] |
| 6 | Print multiple test jobs | All jobs saved locally in SQLite (`sync_status = 'Pending'`) | [x] |
| 7 | Inspect SQLite records | `C:\ProgramData\PrintMonitor\printmonitor.db` holds all job records | [x] |
| 8 | Restore network connection | Batch sync transmits jobs to backend, sets `sync_status = 'Synced'` | [x] |
| 9 | Restart computer | Service starts automatically on Windows boot | [x] |
| 10 | Print duplicate Job ID (after spooler restart) | Generates new `JobUid`, records as distinct job without clash | [x] |
| 11 | Run `PrintMonitor.exe --test-api` | Successfully validates API endpoint communication | [x] |
| 12 | Uninstall via Windows Settings / Add-Remove Programs | Service stopped and unregistered cleanly; user prompted to keep/delete DB | [x] |
