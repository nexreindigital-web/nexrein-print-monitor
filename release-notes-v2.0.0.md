## Nexrein Printer Monitor v2.0.0 Release Notes

### 🎨 Complete UI Overhaul (Desktop Control Panel & Web Portal)
- **Unified Modern Aesthetics:** Redesigned both the Windows Desktop Manager (WPF) and Laravel Cloud Web Portal with matching high-contrast design tokens:
  - **Dark Mode:** Deep midnight background (`#0A0F1D`), dark navy header (`#0C1322`), card surfaces (`#111C35`), and sky-blue "N" badge (`#38BDF8`).
  - **Light Mode:** Clean light background (`#EDF2F7`), crisp white cards (`#FFFFFF`), with the dark navy header preserved for visual hierarchy.
  - **Instant Theme Toggle:** Switch seamlessly between dark and light modes with instant UI response and saved user preferences.
- **6 KPI Metric Cards:** Top metrics overview featuring color-coded indicator stripes:
  - Total Pages (`#2563EB`), Color Pages (`#06B6D4`), B&W Pages (`#64748B`), Print Jobs (`#A855F7`), Success Rate (`#10B981`), Avg Pages/Job (`#F97316`).
- **Live Visual Analytics:**
  - **Color vs. B&W Breakdown:** Dynamic donut chart showing ratio of color to monochrome prints.
  - **Hourly Activity:** Bar chart detailing hourly printing volume across all counters.
  - **Printers Fleet Card:** Real-time count of total, active, and network-connected printers.
- **Live Print Activity Table:** Clean tabular layout with cyan color badges, mono badges, emerald completed pills, date range filtering, and CSV export.

### ⚡ 10-Second High-Frequency Cloud Synchronization
- Spooler agent syncs print jobs and heartbeat telemetry every **10 seconds** to the cloud backend.
- Cloud dashboard updates in near-real-time so shop owners and administrators can monitor live counter activity from anywhere.

### 👥 Workstation Account Linking & Dynamic Provisioning
- **Automatic User Provisioning:** The email specified during desktop installation automatically creates/links an account on `https://printmonitor.nexreindigital.co.ke` with default password `admin`.
- **Workstation Fleet Management:** Workstations and connected local/network printers automatically register under the client's email account.
- **Clean Slate Database:** No mock or dummy records in the initial database — all stats reflect real print jobs.

### 🔐 Two-Way Centralized Security & Password Push
- Changing the admin password on the cloud web portal immediately flags connected workstations.
- Desktop agents retrieve the password update during the 10-second heartbeat cycle and securely update local software credentials automatically.

### 📦 Setup Wizard & Binaries
- **Installer:** Self-contained `PrintMonitor-Setup.exe` (45.54 MB) installs both the background Windows Service (`PrintMonitor`) and the Desktop Control Panel (`PrintMonitor.Manager`).
- **Zero Config Spooler Hook:** Installs silently, preserves existing SQLite audit history and settings during upgrades.
