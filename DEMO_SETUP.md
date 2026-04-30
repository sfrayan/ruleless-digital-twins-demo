# DEMO_SETUP.md — Launching the Demo (Windows 11)

> Copy-paste-ready procedure to launch the `ruleless-digital-twins` demo from scratch on Windows 11. All commands are in **PowerShell**.
>
> **Goal**: demo in less than 5 minutes, from cold, without touching code.
> **Related repo files**: `[ROADMAP.md](ROADMAP.md)` (broader vision) · `[README.md](README.md)` (concepts).

---

## Table of Contents
- [1. Prerequisites](#1-prerequisites)
- [1bis. Bootstrap (clean clone)](#1bis-bootstrap-clean-clone)
- [2. Sequential Launch](#2-sequential-launch)
- [3. Read-only Verifications](#3-read-only-verifications)
- [4. Recovery in Case of Failure](#4-recovery-in-case-of-failure)
- [5. Clean Stop](#5-clean-stop)
- [6. Home Assistant → TTL Export (tools/hass-to-rdt)](#6-export-home-assistant--ttl-toolshass-to-rdt)
- [7. Standalone Inference Engine (demo highlight)](#7-moteur-dinférence-en-standalone-moment-fort-démo)
- [8. Live Demo Scenario (5-phrase script)](#8-scénario-démo-live-script-5-phrases)

---

## 1. Prerequisites

### Installed Software
| Tool | Verification | Source |
|---|---|---|
| Windows 11 | `winver` | — |
| Docker Desktop (WSL2 backend) | `docker --version` | https://docs.docker.com/desktop/install/windows-install/ |
| .NET 8 SDK | `dotnet --info` (look for `Microsoft.NETCore.App 8.0.x`) | https://dotnet.microsoft.com/en-us/download/dotnet/8.0 |
| Java JRE 11+ | `java -version` | OpenJDK or Adoptium |
| Ollama | `ollama --version` | https://ollama.com/download |

### Required Ollama Model
```powershell
ollama list
# Should display: qwen2.5-coder:7b
# Otherwise: ollama pull qwen2.5-coder:7b
```

### Required Docker Containers
| Name | Image | Port | Role |
|---|---|---|---|
| `ha-instance` | `kristofferwagen/ha-instance:latest` | 8123 | Home Assistant showcase |
| `jarvis-ollama` | `ollama/ollama:latest` | 11434 | Local LLM for NLU |
| `mongo-rdt` *(optional)* | `mongo:latest` | 27017 | Case-based reasoning |

### HA Bind-mount
The `ha-instance` container must be linked to `C:\ha-showcase-config\` to persist the admin user and token between restarts:
```powershell
Test-Path C:\ha-showcase-config\configuration.yaml
# Should return: True
```

If `False`, see [4.5 — recreate the ha-instance container from scratch](#45-recréer-le-conteneur-ha-instance-from-scratch).

---

## 1bis. Bootstrap (clean clone)

> To be executed **only once** on a new machine, or after a fresh `git clone`. Skip if you already have a functional workspace.

### 1bis.1 — Submodules

Three submodules are declared in `.gitmodules` (Femyou, Nordpool-FMU, PythonFMU) and **must** be pulled, otherwise SmartNode will not compile:

```powershell
cd C:\dev\ruleless-digital-twins
git submodule update --init --recursive
```

### 1bis.2 — Start the Demo Stack via Docker Compose

The dedicated compose file `services/docker-compose.demo.yml` launches Home Assistant + MongoDB. RabbitMQ and Ollama are **commented out** (see notes in the file — uncomment only if you are demonstrating the `incubator` environment or want containerized Ollama):

```powershell
docker compose -f services/docker-compose.demo.yml up -d
docker compose -f services/docker-compose.demo.yml ps
# ha-instance and rdt-mongodb should be Up
```

Useful environment variables (to be placed in the shell or a local `.env` file):

```powershell
$env:HA_CONFIG_DIR = "C:\ha-showcase-config"      # HA bind-mount (default)
$env:TZ            = "Europe/Oslo"                 # HA timezone
$env:TOKEN_HA      = "<long_lived_token>"          # required for SmartNode
$env:HA_URL        = "http://localhost:8123/api/"  # used by hacvt_rdt
$env:RDT_NAMESPACE = "http://www.semanticweb.org/rayan/ontologies/2025/ha/"
```

> Ollama: if you are using the host's Ollama instance (default), DO NOT uncomment the `ollama` service in the compose file — port conflict on 11434.

---

## 2. Sequential Launch

> Manual variant (equivalent to bootstrap §1bis.2 if you prefer to pilot each container). If you have already done `docker compose up -d`, skip to 2.2.

### 2.1 — Start Containers
```powershell
docker start ha-instance
docker start jarvis-ollama

# Optional — only if UseCaseBasedFunctionality=true in appsettings.json
# docker start mongo-rdt
```

Wait ~20 s then:
```powershell
docker ps --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"
# ha-instance and jarvis-ollama should display "Up X seconds"
```

### 2.2 — Verify `ha-instance` Network
HA must be attached to the `bridge` network and have an IPv4 (`172.17.0.x`). Otherwise, zeroconf crashes in a loop (see [4.2](#42-ha-instance-na-pas-de-réseau-eth0-manquant)).

```powershell
docker exec ha-instance ip -4 addr show eth0
# Expected output:
# 2: eth0@if10: <BROADCAST,MULTICAST,UP,LOWER_UP> ...
#     inet 172.17.0.x/16 brd 172.17.255.255 scope global eth0
```

If you see `RTNETLINK answers: Cannot find device "eth0"` or nothing → fix [4.2](#42-ha-instance-na-pas-de-réseau-eth0-manquant).

HTTP Test:
```powershell
Invoke-WebRequest -Uri http://localhost:8123/ -UseBasicParsing -TimeoutSec 5 `
  | Select-Object StatusCode
# Expected: StatusCode 200
```

### 2.3 — Define `TOKEN_HA`
The HA long-lived token is required in **every PowerShell session** where you launch `dotnet run`. It is stored in `C:\ha-showcase-config\.storage\auth_provider.homeassistant` on the HA side, but SmartNode reads it from the environment variable:

```powershell
$env:TOKEN_HA = "<paste_your_long_lived_token_here>"
```

If you don't have/no longer have a token: see [4.4 — recreate a token](#44-token-perdu-ou-401-après-recréation-du-conteneur).

### 2.4 — Launch SmartNode
```powershell
cd C:\dev\ruleless-digital-twins\SmartNode\SmartNode
dotnet run
```

Expected in the first few seconds:
```
HH:MM:SS info: SmartNode.Program[0]
      Internal API listening on http://localhost:8080/
HH:MM:SS info: Logic.Mapek.IMapekManager[0]
      Starting the MAPE-K loop. (maxRounds= -1)
```

If `MAPE-K loop failed — HTTP API continues` appears: SmartNode is still running (chatbox usable), but HA is likely unreachable → verify [2.2](#22-vérifier-le-réseau-de-ha-instance).

**Keep this PowerShell window open during the entire demo.**

### 2.5 — Open the Chatbox
In another PowerShell window:
```powershell
start C:\dev\ruleless-digital-twins\SmartNode\SmartNode\index.html
```

Visual verifications in the browser (100% English interface since April 2026):
- Header: **green** dot + text `SmartNode + HA connected`
- Dashboard: Temperature / Power / Air quality display values (not `—`)
- `QUICK ACTIONS` section with 6 chips: *Turn on living room*, *Turn off kitchen*, *Set 22°C*, *House status*, *Energy price*, *Charge Tesla overnight*

If the dot remains red → verify the `dotnet run` terminal (error logs) and [3](#3-vérifications-lecture-seule).

---

## 3. Read-only Verifications

> None of the commands below modify anything — usable during a full run to diagnose.

### 3.1 — Containers and Network
```powershell
# State of the 3 critical containers
docker ps -a --filter "name=ha-instance" --filter "name=jarvis-ollama" --filter "name=mongo-rdt" `
  --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"

# Restart policy of ha-instance and homeassistant (if present)
docker inspect ha-instance --format "ha-instance: restart={{.HostConfig.RestartPolicy.Name}} status={{.State.Status}}"
docker inspect homeassistant --format "homeassistant: restart={{.HostConfig.RestartPolicy.Name}} status={{.State.Status}}" 2>$null

# IP eth0 of ha-instance
docker exec ha-instance ip -4 addr show eth0 2>$null

# List of containers on the bridge network
docker network inspect bridge --format '{{range .Containers}}{{.Name}} {{end}}'

# Who is listening on host port 8123?
Get-NetTCPConnection -LocalPort 8123 -State Listen -ErrorAction SilentlyContinue `
  | ForEach-Object { Get-Process -Id $_.OwningProcess } | Select-Object Id, ProcessName
```

### 3.2 — SmartNode and HA are responding
```powershell
# Internal SmartNode
Invoke-RestMethod http://localhost:8080/api/entities | Format-List

# Live sensor readings
Invoke-RestMethod http://localhost:8080/api/state

# HA responds (without token)
Invoke-WebRequest http://localhost:8123/ -UseBasicParsing | Select-Object StatusCode

# HA with token (verify it is valid)
Invoke-RestMethod -Headers @{Authorization="Bearer $env:TOKEN_HA"} http://localhost:8123/api/states `
  | Group-Object {$_.entity_id.Split('.')[0]} | Select-Object Name, Count
```

### 3.3 — Ollama ready and NLU pipeline
```powershell
# Available models
(Invoke-RestMethod http://localhost:11434/api/tags).models | Select-Object name, size

# End-to-end NLU test (may take 30-60s on the first call — loading 4.7 GB model)
Invoke-RestMethod -Uri http://localhost:8080/api/nlu -Method Post `
  -ContentType 'application/json' `
  -Body '{"message":"turn on the kitchen light"}'
# Expected return: JSON object with intent="actuate" or "call_service", entity_id or target defined
```

### 3.4 — Dynamic HA Registry loaded
```powershell
# Should list all HA entities cached by SmartNode (30s refresh)
(Invoke-RestMethod http://localhost:8080/api/entities_full) | Measure-Object | Select-Object Count

# Detail of the first 3
(Invoke-RestMethod http://localhost:8080/api/entities_full) | Select-Object -First 3
```

### 3.5 — Stored Data (if `SaveMapekCycleData=true`)
```powershell
# Persisted cycles (CSV by property and by actuator)
Get-ChildItem state-data\ -Recurse `
  | Sort-Object LastWriteTime -Descending | Select-Object -First 10 Name, Length, LastWriteTime

# Schedule history (persists between SmartNode restarts)
Get-Content state-data\schedules.json | ConvertFrom-Json | Select-Object id, targetName, status, startedAt

# MongoDB cases (if UseCaseBasedFunctionality=true)
docker exec mongo-rdt mongosh --quiet --eval "use CaseBase; db.Cases.countDocuments()" 2>$null
```

### 3.6 — Verify FMU Logs (simulation visible)

While SmartNode is running, look in the `dotnet run` terminal for:

```
Generating simulations.
Running simulation #1
Running simulation #2
...
Generated a total of N simulation paths.
```

> These lines confirm that the FMU is being executed during the MAPE-K **Plan** phase.
> If they do not appear, verify that `Environment` = `homeassistant` in `appsettings.json`
> and that the `.fmu` files are present in `SmartNode/Implementations/FMUs/`.

### 3.7 — Nord Pool Proactive Advisory

```powershell
# 204 = no MAPE-K cycle yet; JSON = advisory calculated
Invoke-RestMethod http://localhost:8080/api/proactive/status
# Key fields: shouldPreheat, shouldDeferLoad, reason, currentPrice, q1, q3
```

In the UI, the proactive banner (🔥 preheat / ⏸️ defer) is automatically displayed if the advisory is active.
It refreshes every 30 s. The **Preheat now (+1°C)** button calls `/api/actuate` directly.

---

## 4. Recovery in Case of Failure

### 4.1 — Port 8123 is occupied by another container

**Symptom**: `docker start ha-instance` fails with `Bind for 0.0.0.0:8123 failed: port is already allocated`. Or `ha-instance` starts but responds strangely (another HA on the port).

**Typical Cause**: a second HA container (`homeassistant`, linked to `C:\homeassistant\`) with `RestartPolicy: always` that squats the port every time Docker Desktop restarts.

**Diagnosis**:
```powershell
docker ps -a --filter "publish=8123" --format "table {{.Names}}`t{{.Status}}`t{{.HostConfig.RestartPolicy.Name}}"
```

**Fix**:
```powershell
# 1. Disable auto-restart of the faulty container (without deleting it; its data remains)
docker update --restart=no homeassistant

# 2. Stop it
docker stop homeassistant

# 3. Verify it does not come back
docker ps --filter "name=homeassistant" --format "{{.Names}} {{.Status}}"
# Should display "Exited" or nothing

# 4. Relaunch ha-instance
docker start ha-instance
```

To turn it back on later (by stopping ha-instance first):
```powershell
docker stop ha-instance
docker start homeassistant
```

### 4.2 — `ha-instance` has no network (missing eth0)

**Symptom**: `docker exec ha-instance ip -4 addr show eth0` → `Cannot find device "eth0"`. HA logs full of `OSError: [Errno 19] No such device` and `system does not have any enabled IPv4 addresses`.

**Typical Cause**: Docker Desktop / WSL2 restarted while the container was running → NetworkSettings becomes empty.

**Diagnosis**:
```powershell
docker inspect ha-instance --format '{{range $k,$v := .NetworkSettings.Networks}}{{$k}}: IP={{$v.IPAddress}}{{end}}'
# If empty → confirmed
```

**Fix**:
```powershell
# 1. Stop
docker stop ha-instance

# 2. Reconnect to bridge
docker network connect bridge ha-instance

# 3. Restart
docker start ha-instance

# 4. Wait 20-30s then verify
Start-Sleep -Seconds 25
docker exec ha-instance ip -4 addr show eth0
# Should display "inet 172.17.0.x/16"

Invoke-WebRequest http://localhost:8123/ -UseBasicParsing -TimeoutSec 5 | Select StatusCode
# Should display 200
```

### 4.3 — HA still crashes with `OSError: [Errno 19]` despite eth0 OK

**Symptom**: eth0 present, but `docker logs ha-instance` still shows zeroconf crash on multicast.

**Cause**: known WSL2 bug on the `IP_ADD_MEMBERSHIP` operation.

**Fix** (in order of invasiveness, to try if 4.2 is not enough):
```powershell
# Option A — restart WSL2 and Docker Desktop
wsl --shutdown
# Then relaunch Docker Desktop manually (or: Stop-Process -Name "Docker Desktop")
# Wait 1 min for Docker to reboot, then resume sequence 2.1

# Option B — update WSL2 (PowerShell admin)
wsl --update
# Close everything, restart, resume Section 2

# Option C — full Windows reboot
shutdown /r /t 0
```

**Durable solution**: the `C:\ha-showcase-config\configuration.yaml` config already replaces `default_config:` with an explicit list (without `zeroconf`/`ssdp`/`cloud`/`go2rtc`). If you re-pull a new image and lose this patch → re-extract with:
```powershell
# See 4.5 below, steps 1-3
```

### 4.4 — Token lost or 401 after container recreation

**Symptom**: `Invoke-RestMethod -Headers @{Authorization="Bearer $env:TOKEN_HA"} http://localhost:8123/api/states` → HTTP 401.

**Cause**: container recreation wiped `.storage/auth_provider.homeassistant` (or you pasted an expired token).

**Fix — create an admin user and generate a new token**:
```powershell
# 1. Verify that the admin user exists (otherwise "Total users: 0")
docker exec ha-instance hass --script auth list

# 2a. If user exists but no credentials → recreate password
docker exec ha-instance hass --script auth add admin admin
# Expected: "Auth created"

# 2b. If user exists and you know the password → reset
# docker exec ha-instance hass --script auth change_password admin <new_pwd>

# 3. Go to http://localhost:8123 → login admin / admin
# 4. Profile (icon in bottom left) → Security → Create Long-Lived Access Token
# 5. Give it a name (e.g.: "smartnode") → COPY the token (visible only once!)

# 6. Re-inject into the SmartNode PowerShell session
$env:TOKEN_HA = "<pasted_token>"

# 7. Relaunch SmartNode (Ctrl+C in its terminal then dotnet run)
```

### 4.5 — Recreate the `ha-instance` container from scratch

To be used only if `C:\ha-showcase-config\` is corrupted or if you are starting from a clean machine.

```powershell
# 1. Extract default config from the image
docker create --name ha-extract-tmp kristofferwagen/ha-instance:latest
docker cp ha-extract-tmp:/config C:\ha-showcase-config
docker rm ha-extract-tmp

# 2. Patch configuration.yaml to disable WSL2-incompatible modules
#    (to be done manually: replace the `default_config:` line with the
#     explicit list documented in configuration.yaml — see the commit that applied
#     this patch or look at previous sessions)

# 3. Delete the old container if it exists
docker rm -f ha-instance

# 4. Recreate with bind-mount + restart=no + bridge
docker run -d --name ha-instance -p 8123:8123 --network bridge `
  -v C:\ha-showcase-config:/config `
  --restart no `
  kristofferwagen/ha-instance:latest

# 5. Wait 30-60s then create user (cf. 4.4)
docker exec ha-instance hass --script auth add admin admin
```

### 4.6 — `dotnet run` crashes or port 8080 is occupied

```powershell
# Who is occupying port 8080?
Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue `
  | ForEach-Object { Get-Process -Id $_.OwningProcess }

# Kill a ghost SmartNode
Get-Process -Name SmartNode -ErrorAction SilentlyContinue | Stop-Process

# Build locked by a previous dotnet run
# → Ctrl+C the current dotnet run window, then relaunch it
```

### 4.7 — First `/api/nlu` slow (30-60s)

**Cause**: Ollama loads the `qwen2.5-coder:7b` model (4.7 GB) into RAM on the first call.

**This is normal**. The chatbox has a 10 s client timeout with fallback keyword regex, so the 1st question works via fallback anyway. From the 2nd one onwards, it's instant (model already in RAM).

To warm up Ollama before the demo:
```powershell
ollama run qwen2.5-coder:7b "test" 2>$null
# When the prompt returns, the model is in RAM
```

### 4.8 — Chatbox displays `SmartNode unreachable`

```powershell
# 1. Verify SmartNode is running
Invoke-RestMethod http://localhost:8080/api/entities

# 2. If NOK → relaunch dotnet run (cf. 2.4)

# 3. If OK but chatbox says unreachable → CORS?
#    The chatbox reads the fetch via file:// → verify it is opened with start
#    and not dragged into an existing tab.
start C:\dev\ruleless-digital-twins\SmartNode\SmartNode\index.html
```

---

## 5. Clean Stop

```powershell
# Ctrl+C in the dotnet run window

# Stop containers (without deleting them — data preserved)
docker stop ha-instance
docker stop jarvis-ollama
# docker stop mongo-rdt   # if enabled
```

To relaunch later, resume directly at [Section 2](#2-lancement-séquentiel).

If you started the stack via `docker compose up -d`, the symmetrical stop is:
```powershell
docker compose -f services/docker-compose.demo.yml down
```

---

## 6. Home Assistant → TTL Export (`tools/hass-to-rdt`)

> Generates/regenerates `models-and-rules/homeassistant-instance.ttl` from the current HA state. To be done every time you add/rename HA entities. **Without this up-to-date export, the inference engine works on an obsolete model.**

### 6.1 — Unique venv setup

```powershell
cd C:\dev\ruleless-digital-twins\tools\hass-to-rdt
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements-cli.txt
```

The installation pulls `homeassistant` (~10 min the first time — this is normal, it pulls the entire HA ecosystem).

### 6.2 — Launching the Export

Necessary variables (already covered in §1bis.2):

```powershell
$env:TOKEN_HA      # HA long-lived token
$env:HA_URL        = "http://localhost:8123/api/"
$env:RDT_NAMESPACE = "http://www.semanticweb.org/rayan/ontologies/2025/ha/"
```

> ⚠️ The second argument `TOKEN_HA` is the **name** of the variable, not the token value. It is `hacvt_rdt.py` that will read the env var.

```powershell
cd C:\dev\ruleless-digital-twins\tools\hass-to-rdt
.venv\Scripts\Activate.ps1
python hacvt_rdt.py $env:HA_URL TOKEN_HA `
    --namespace $env:RDT_NAMESPACE `
    --out ..\..\models-and-rules\homeassistant-instance.ttl
```

Expected output: an up-to-date `.ttl` with all HA entities mapped to SOSA/SSN + `rdt:hasIdentifier`. The next MAPE-K cycle (SmartNode reloads the model every cycle) will automatically pick up the new version.

### 6.3 — Quick Verification

```powershell
Get-Content ..\..\models-and-rules\homeassistant-instance.ttl | Select-String "rdt:hasIdentifier" | Measure-Object
# expected number = number of exposed HA entities
```

---

## Visual Recap — minimal sequence that works

Compose variant (clean clone):
```
git submodule update --init --recursive
docker compose -f services/docker-compose.demo.yml up -d
$env:TOKEN_HA = "<token>"
cd SmartNode\SmartNode && dotnet run
start .\index.html
```

Manual variant (containers already created):
```
docker start ha-instance
docker start jarvis-ollama
docker exec ha-instance ip -4 addr show eth0       # check eth0 OK
$env:TOKEN_HA = "<token>"
cd C:\dev\ruleless-digital-twins\SmartNode\SmartNode
dotnet run
start .\index.html
```

If any step fails → consult the corresponding Section 4.

---

---

## 7. Standalone Inference Engine (demo highlight)

> Execute **before** launching SmartNode to have a fresh inferred `.ttl` to show.

```powershell
cd models-and-rules
java -jar ruleless-digital-twins-inference-engine.jar `
     ..\ontology\ruleless-digital-twins.ttl `
     homeassistant-ha-instance.ttl `
     inference-rules.rules `
     homeassistant-ha-inferred-DEMO.ttl
```

Compare sizes (the value added by reasoning is visible in bytes):

```powershell
"{0} bytes — instance (input)" -f (Get-Item homeassistant-ha-instance.ttl).Length
"{0} bytes — inferred (output)"   -f (Get-Item homeassistant-ha-inferred-DEMO.ttl).Length
```

Point out in VS Code:
- `homeassistant-ha-instance.ttl`: raw data, no calculated violations
- `homeassistant-ha-inferred-DEMO.ttl`: inferred triples, look for `meta:isViolated`

```powershell
Select-String -Path homeassistant-ha-inferred-DEMO.ttl -Pattern "isViolated"
```

---

## 8. Live Demo Scenario (5-phrase script)

> **Savings tip**: launch the scenario in the **morning** (before 10am) with a 7am deadline the next day.
> This gives a ~21h window → Nord Pool prices vary more → **10-20% savings** displayed
> instead of 2-4% if launched in the afternoon with 4h remaining.

| # | Phrase to type in chatbox | What it shows |
|---|-------------------------------|-----------------|
| 1 | `what is the living room temperature?` | NLU + live HA query |
| 2 | `turn on the kitchen light` | Real actuator → toggle visible in HA Lovelace |
| 3 | `set the temperature to 23 degrees` | InputNumber actuator → HA slider moves |
| 4 | `what is the house status?` | Full dashboard (temp + power + AQI + lights) |
| 5 | `charge the Tesla to 100% by 7am` | Nord Pool optimization + 24h bar + % savings |

After phrase 5:
1. Click **▶ Execute** → the `⏱ SCHEDULES` banner appears
2. `input_boolean.showcase_car_charger` toggles in HA every 60 s (demo mode: 1h = 1 min)
3. Click ✕ on the schedule to cancel it → `cancelled` status visible in the banner

If the proactive 🔥 banner is visible: explain the proactive advisory as a bonus.

### Known demo gotchas

| Symptom | Cause | Remedy |
|----------|-------|--------|
| `FileNotFoundException: Properties\appsettings.json` | Working directory ≠ SmartNode project | Patch applied in `Program.cs`: `appsettings.json` is resolved via the assembly folder. `dotnet run --project SmartNode/SmartNode/SmartNode.csproj` now works from any CWD. |
| Savings < 5% on Tesla charge | Optimization window too short (does not include night off-peak hours) | Launch charge **in the morning** (before 10am) with `7am tomorrow` deadline → 21h window including night prices (~0.99 NOK/kWh vs ~1.20 peak). |
| `[OPTIMIZE] windowBuckets=6` or less | Nord Pool returns 15-min slots, old code took `Take(24)` = 6h | Patch applied in `NordPoolForecastProvider.cs`: `Take(horizon)` replaced by time filter `s.Start < nowLocal.AddHours(horizon)`. Verify in SmartNode terminal: `[OPTIMIZE] rawSlots=192 hourlyBuckets=48 windowBuckets=15...`. |
| `NordPool: ... area=NO5 not found in response` | Home Assistant returns `"no5"` (lowercase) instead of `"NO5"` | Patch applied in `NordPoolForecastProvider.cs`: case-insensitive lookup. If error returns, real HA area is logged in response body. |
| Schedule disappears after restart | OK before patch — was volatile | Patch applied: `state-data/schedules.json` is saved after each creation/completion/cancel. At restart, `running` becomes `interrupted`. |
| `MAPE-K loop failed — HTTP API continues` | HA unreachable but SmartNode runs | Verify `docker ps` (`ha-instance` container up + port 8123) then wait for next cycle (60 s). |
| Chatbox says *SmartNode unreachable* | Port 8080 occupied or SmartNode crashed | `netstat -ano | findstr :8080` then Ctrl+C in `dotnet run` window and relaunch. |

> **On Nord Pool savings**: real prices vary every day. The demo does not aim for a fixed 20% number. The important proof is that the optimizer selects cheaper future windows and reports savings both vs *window-average* and vs *peak-hours baseline*.
>
> **Tesla / EV demo honesty**: the EV optimization scenario uses **live Nord Pool prices from Home Assistant** (verifiable via the *Price source* line on the optimizer card + log `[OPTIMIZE] priceSource=homeassistant_nordpool`), but the **charger is a configured demo actuator** (`CarCharger`, 11 kW × 4 h by default). No real Tesla entity (SOC, plugged/unplugged, real power) is read at this stage — the same mechanism plugs into a real HA charger/Tesla entity by replacing the target. If HA/Nord Pool is unavailable, the optimizer **refuses to respond** rather than falling back to simulated prices (the card displays `Live Nord Pool forecast unavailable — optimization not launched`).

### Logs to monitor during demo

| Log | Phase | Source file |
|-----|-------|----------------|
| `Generated ActuationAction` | Plan (Java inference) | JAR output |
| `Running simulation #N` | Plan (FMU look-ahead) | `Logic/Mapek/MapekPlan.cs` |
| `NordPool: 2026-XX-XX → 96 slots added` | Forecast | `NordPoolForecastProvider.cs` |
| `[OPTIMIZE] rawSlots=192 hourlyBuckets=48 windowBuckets=15 ... cheapest3=04:00@0.9954, ...` | Optimize endpoint | `Program.cs` |
| `[SCHEDULE <id>] h=0 → ON (CarCharger)` | Execute schedule | `ScheduleManager.cs` |

### Likely jury questions

| Question | Short answer |
|----------|----------------|
| *Why not fixed rules?* | Flexibility: new HA entities = 0 lines of code; reasoning adapts |
| *How to scale to 300 entities?* | Dynamic HA registry already wired; SOSA/SSN ontology is generic |
| *HA token security?* | Env variable, never committed, long-lived scope |
| *Why local Ollama?* | Privacy + latency + offline + 0 cost per token |
| *What happens if HA goes down?* | MAPE-K cycle fail → log + automatic retry next cycle (implemented in MapekManager) |

---

*Last updated: 2026-04-28.*
