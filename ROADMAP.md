# ROADMAP — Ruleless Digital Twins Demo

> Action plan to transform this repo into an end-to-end live demo.
> **Target**: Volker Stolz internship defense — week of **May 5, 2026**.
> **Components to show**: FMU simulation · Jena inference engine · stored data · simple visualization.
>
> This document complements [readme.md](readme.md) which covers the theoretical architecture. Here we speak only of the **execution** of a reproducible demo.

---

## Table of Contents

- [Current Status](#current-status)
- [Demo Target Architecture](#demo-target-architecture)
- [Phase 0 — Preparation](#phase-0--preparation-j-3-à-j-2)
- [Phase 1 — Environment Stabilization](#phase-1--stabilisation-environnement-j-2)
- [Phase 2 — Activate the 4 Components](#phase-2--activer-les-4-composants-j-1)
- [Phase 3 — Defense Preparation](#phase-3--préparation-soutenance-j)
- [Phase 4 — Optional Post-Defense](#phase-4--optionnel-post-soutenance)
- [Quick-start](#quick-start)
- [Success Criteria](#success-criteria-démo)
- [Appendices](#annexes)

---

## Current Status

### ✅ What is already working

| Component | Status | Location |
|---|---|---|
| Complete MAPE-K loop | ✅ | `SmartNode/Logic/Mapek/` (Manager + Monitor + Analyze + Plan + Execute + Knowledge) |
| SOSA/SSN ontology + RDT extensions | ✅ | `ontology/ruleless-digital-twins.ttl` |
| Inference engine (Apache Jena 5.3.0) | ✅ | `models-and-rules/ruleless-digital-twins-inference-engine.jar` (15 MB) |
| Jena Rules | ✅ | `models-and-rules/inference-rules.rules` (459 l.) + `verification-rules.rules` (549 l.) |
| Instance models | ✅ | `instance-model-1/2.ttl`, `M370.ttl`, `homeassistant-ha-instance.ttl`, `nordpool-simple.ttl` |
| FMU physical simulation | ✅ | `SmartNode/Implementations/FMUs/` (m370, incubator) — loaded via Femyou |
| Home Assistant integration | ✅ | REST + 4 types of actuators (Light, Switch, InputBoolean, InputNumber) |
| SmartNode HTTP API (chatbox) | ✅ | port 8080, 13 endpoints (cf. [appendices](#annexes)) |
| Web Chatbox | ✅ | `SmartNode/SmartNode/index.html` — dashboard, NLU, scheduler UI |
| Dynamic NLU | ✅ | Ollama `qwen2.5-coder:7b` + HA registry injected into prompt + keyword fallback |
| In-memory Scheduler | ✅ | `SmartNode/SmartNode/ScheduleManager.cs` — 24h plans, demo mode 1h=60s |
| Price Optimization | ✅ | `/api/optimize` — picks N cheapest hours before deadline |
| Tests | ✅ | `SmartNode/TestProject/` — 9 xUnit files (Mapek, Inference, HA, FMU, NordPool…) |

### ⚠️ What is missing for a credible demo

| Missing | Demo Impact | Effort |
|---|---|---|
| Documented replicable setup | If HA crashes on the day, panic live | XS |
| Realistic price data (not flat FakepoolSensor) | Displayed savings ≈ 2-4% only, not very impressive | S |
| Visible MAPE-K cycle persistence | No concrete illustration of "digital twin stores its history" | XS |
| Scripted demo scenarios | Risk of stuttering live | S |
| Slides | Task #3 of `Faire.md` still open | M |
| Exhibitable inferred model | The inferred `.ttl` runs in RAM but we never show it | XS |

---

## Demo Target Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                         DEFENSE — LIVE DEMO                          │
└──────────────────────────────────────────────────────────────────────┘

    [1] CHATBOX (browser)                         [2] HOME ASSISTANT
    ┌───────────────────┐                        ┌──────────────────┐
    │ Live Dashboard    │                        │ Lovelace UI      │
    │ + 6 quick chips   │     "turn on living"   │ Switches react   │
    │ + 24h plans       │   ───────────────────> │ in real time     │
    │ + energy bar      │                        │ (port :8123)     │
    └───────────────────┘                        └──────────────────┘
             │                                            ▲
             │                                            │ REST
             ▼ HTTP :8080                                 │
    ┌────────────────────────────────────────────────────┴───────────┐
    │                  SmartNode (.NET 8)                             │
    │                                                                 │
    │   [3] INFERENCE ENGINE              [4] SIMULATION             │
    │   ┌─────────────────────┐           ┌────────────────────┐     │
    │   │ Apache Jena (JAR)   │           │ Femyou + FMU       │     │
    │   │ instance.ttl        │           │ Modelica plant     │     │
    │   │  + rules.rules      │           │ N steps look-ahead │     │
    │   │  → inferred.ttl     │           │ → Energy×€ fitness │     │
    │   └─────────────────────┘           └────────────────────┘     │
    │                                                                 │
    │   [5] STORED DATA                                              │
    │   ┌─────────────────────┐                                      │
    │   │ state-data/cycle-*  │  (SaveMapekCycleData=true)           │
    │   │ + MongoDB cases     │  (UseCaseBasedFunctionality=true)    │
    │   └─────────────────────┘                                      │
    │                                                                 │
    │   [6] NLU                                                       │
    │   Ollama qwen2.5-coder:7b (port 11434)                         │
    └─────────────────────────────────────────────────────────────────┘
```

The 4 components to highlight during the demo:
1. **Simulation** → FMU executed during the Plan phase (logs visible in the SmartNode terminal)
2. **Inference engine** → JAR executed at each Knowledge cycle (show an inferred TTL file before/after)
3. **Stored data** → JSON files in `state-data/` + MongoDB entries (optional)
4. **Visualization** → chatbox + HA lovelace in parallel

---

## Phase 0 — Preparation (D-3 to D-2)

> *Do not touch code. Inventory and preparation of artifacts.*

- [ ] **0.1** Clone the repo into a *clean* folder (`C:\demo\ruleless-digital-twins`) to avoid noise from dev sessions.
- [ ] **0.2** Verify that critical demo files exist and are not corrupted:
  - `models-and-rules/ruleless-digital-twins-inference-engine.jar` (≈ 15 MB)
  - `models-and-rules/instance-model-1.ttl` and `inferred-model-1.ttl`
  - `models-and-rules/inference-rules.rules` and `verification-rules.rules`
  - `ontology/ruleless-digital-twins.ttl`
  - `SmartNode/Implementations/FMUs/*.fmu` (at least 1 compiled FMU)
- [ ] **0.3** Verify installed external services:
  - `docker --version`, `docker ps` (Docker Desktop OK)
  - `dotnet --info` ≥ 8.0
  - `java -version` ≥ 11
  - `ollama list` contains `qwen2.5-coder:7b`
- [ ] **0.4** Backup `C:\ha-showcase-config\` (admin user + token + helpers already created):
  ```powershell
  Compress-Archive -Path C:\ha-showcase-config\* `
    -DestinationPath C:\demo\ha-showcase-config-backup.zip
  ```
- [ ] **0.5** Save the HA long-lived token in a vault (1Password, local encrypted file…) to be able to put it back in `$env:TOKEN_HA` in less than 30s.

---

## Phase 1 — Environment Stabilization (D-2)

> *Ensure that the launch sequence works the first time, from cold.*

- [ ] **1.1** Dry run: complete machine reboot, then execute [quick-start](#quick-start) without editing anything. Everything must work in < 5 min.
- [ ] **1.2** Document in [DEMO_SETUP.md](#annexes) (to be created in Phase 3):
  - Exact order of `docker start`
  - How to recover if `ha-instance` has no eth0 (`docker network connect bridge ha-instance`)
  - How to recreate the admin user if auth is lost (`docker exec ha-instance hass --script auth add admin admin`)
  - How to relaunch if Ollama is not loaded (first call 30-60s)
- [ ] **1.3** Activate MAPE-K persistence for the demo. Edit **only the config** (no code):
  - `SmartNode/SmartNode/Properties/appsettings.json`
  - Set `"SaveMapekCycleData": true`
  - The `state-data/` folder will fill with JSON files per cycle
- [ ] **1.4** *(Optional)* Activate case-based reasoning to show MongoDB:
  - Start a local MongoDB: `docker run -d --name mongo-rdt -p 27017:27017 mongo:latest`
  - Set `"UseCaseBasedFunctionality": true` in `appsettings.json`
  - Otherwise, skip this and settle for `state-data/`
- [ ] **1.5** Test the inference JAR in standalone (showable directly in demo):
  ```powershell
  cd C:\dev\ruleless-digital-twins\models-and-rules
  java -jar ruleless-digital-twins-inference-engine.jar `
       ..\ontology\ruleless-digital-twins.ttl `
       homeassistant-ha-instance.ttl `
       inference-rules.rules `
       homeassistant-ha-inferred-DEMO.ttl
  ```
  Compare `instance` (before) vs `inferred` (after) sizes — this shows the added value of reasoning.

---

## Phase 2 — Activate the 4 Components (D-1)

> *For each component, plan 1 "highlight" moment where it is made visible to the jury.*

### 2.1 — FMU Simulation
- [ ] Confirm `appsettings.json` points to chosen environment (`"Environment": "homeassistant"`).
- [ ] Launch `dotnet run` and **identify in logs** the Plan phase: we should see `Running simulation #1`, `Parameters: ...`, `New values: ...`. This is the FMU projecting future N cycles.
- [ ] Prepare a terminal capture showing 5-6 consecutive simulations → show in slide or live.

### 2.2 — Inference Engine
- [ ] Run the JAR manually (Phase 1.5 command) **before** launching SmartNode → have a fresh inferred `.ttl` to exhibit.
- [ ] Open `homeassistant-ha-inferred-DEMO.ttl` in VS Code → highlight some derived triples (e.g. `meta:isViolated true` produced by verification rules).
- [ ] *Bonus*: create a "Before / After" slide with a portion of the instance `.ttl` (no inference) vs inferred `.ttl` (rules applied).

### 2.3 — Stored Data
- [ ] While SmartNode runs (10-20 cycles), show `state-data/` filling up.
- [ ] *If MongoDB activated*: open MongoDB Compass or `mongosh`:
  ```
  mongosh "mongodb://localhost:27017"
  use CaseBase
  db.Cases.countDocuments()
  db.Cases.findOne()
  ```
- [ ] Prepare a capture of both storage types (JSON files + Mongo collection).

### 2.4 — Visualization
- [ ] Put side-by-side on the screen:
  - **Browser tab 1**: Chatbox `index.html` (dashboard + chips + schedule banner).
  - **Browser tab 2**: `localhost:8123` HA Lovelace (Direct Controls + Sensors + Environment Inputs).
- [ ] Prepare the live "scenario":
  1. *« What is the living room temperature? »* → chatbox response + HA dashboard match.
  2. *« Turn on the kitchen light. »* → toggle visible in HA in real time.
  3. *« Set the temperature to 23 degrees. »* → HA slider moves instantly (via `InputNumber` actuator).
  4. *« Charge the car to 100% by 7am. »* → 24h bar appears, `Savings: N%`, "▶ Execute" button → live banner and `input_boolean.showcase_car_charger` toggle in HA.
  5. *« Activate the night scene. »* → test of dynamic NLU via `/api/call_service` (chatbox knows all HA entities).

---

## Phase 3 — Defense Preparation (D-day)

- [ ] **3.1** Slides (5 slides, 7-10 min total):
  1. **Context & Objectives**: CPS, *ruleless* digital twins (vs fixed rules), MAPE-K, FMU, OWL ontology.
  2. **Requirements**: energy control (heating / EV charging) under constraints (comfort + budget + off-peak hours).
  3. **Achievements**: `hacvt_rdt.py` (HA → OWL export), NLU chatbox, optimized scheduler, Energy × Price fitness, HA integration via REST.
  4. **Live Demo** (3-4 min): 5 phrases in chatbox, show HA reacting, show optimized 24h plan.
  5. **Conclusion & Perspectives**: real NordPool prices, multi-home, smart-meter integration, horizontal persistence (Influx).
- [ ] **3.2** Architecture captures (drawio → PNG/SVG) for slides:
  - `Diagrams/framework_architecture.drawio` → export as PNG.
  - `Diagrams/inference_algorithm_phase_1.drawio` and `..._phase_2.drawio` to explain reasoning.
- [ ] **3.3** Create a **`DEMO_SETUP.md`** at the repo root, containing:
  - [Quick-start](#quick-start) commands in copy-paste-ready format
  - Remedies for known failures (missing eth0, lost auth, squatted :8123 port…)
  - `C:\ha-showcase-config\` backup with restoration instructions
- [ ] **3.4** Filmed dry run (smartphone) to identify blurry parts and check timing (target 8-10 min).
- [ ] **3.5** Prepare 3-4 likely jury questions and answers:
  - *« Why not a fixed rules approach? »* → flexibility, adaptation to new contexts without recoding.
  - *« How to scale to 300 entities? »* → dynamic HA registry already wired, test on volume.
  - *« HA token security? »* → env variable, never committed, long-lived scope.
  - *« Why local Ollama instead of a cloud API? »* → privacy + latency + offline-capable + no cost.

---

## Phase 4 — Optional Post-Defense

> *Improvement ideas to mention in perspectives, code only if time available.*

- [ ] Replace `FakepoolSensor` with **real** NordPool price reader (CSV or `nordpool-api.com` API). Generates visible 15-25% savings, much more convincing.
- [ ] Schedule persistence (JSON serialization in `state-data/schedules.json`) to survive SmartNode restart.
- [ ] MAPE-K auto-recovery with exponential retry when HA becomes reachable again.
- [ ] Small graphical dashboard (Grafana + InfluxDB) reading `state-data/` or MongoDB → energy / cost / temperature curve over 24h.
- [ ] "Entities" panel in the chatbox listing everything the NLU can control (discoverability).
- [ ] `entity_id` validation on SmartNode side against `HomeAssistantRegistry` to avoid LLM hallucinations.
- [ ] End-to-end integration tests (HA mock + scheduler) for CI.

---

## Quick-start

> *To be copied and pasted into a clean PowerShell session. Requires no code editing.*

```powershell
# 1. Required containers
docker start ha-instance      # HA showcase, port 8123
docker start jarvis-ollama    # Local LLM, port 11434
# (optional) docker start mongo-rdt   # case-based reasoning

# 2. Verify ha-instance has an IP (sometimes eth0 missing after wsl shutdown)
docker exec ha-instance ip -4 addr show eth0
# If "no eth0" error:
#   docker stop ha-instance
#   docker network connect bridge ha-instance
#   docker start ha-instance

# 3. HA Token (to be exported in each new PS session)
$env:TOKEN_HA = "<long_lived_HA_token>"

# 4. Launch SmartNode
cd C:\dev\ruleless-digital-twins\SmartNode\SmartNode
dotnet run

# 5. Open the chatbox (other window)
start C:\dev\ruleless-digital-twins\SmartNode\SmartNode\index.html
```

Quick verifications:
```powershell
# SmartNode responds
Invoke-RestMethod http://localhost:8080/api/entities

# HA responds with valid auth
Invoke-RestMethod -Headers @{Authorization="Bearer $env:TOKEN_HA"} http://localhost:8123/api/states `
  | Group-Object {$_.entity_id.Split('.')[0]} | Select Name, Count

# Ollama ready
(Invoke-RestMethod http://localhost:11434/api/tags).models | Select-Object name
```

---

## Success Criteria

At the end of the defense, we must have proven each point:

- [ ] **Chatbox dialogues** in English, understands natural phrases, responds pertinently
- [ ] **At least 1 direct command** turns on / off a real HA entity → toggle visible live in Lovelace
- [ ] **At least 1 optimized plan** displays 24h bar + cost + % savings, can be executed → real toggle every 60s in demo mode
- [ ] **Inference engine**: show the JAR executed in standalone, compare instance vs inferred TTL
- [ ] **FMU Simulation**: MAPE-K Plan logs visible, explain the look-ahead window
- [ ] **Persistence**: open `state-data/` and exhibit cycle files
- [ ] **No crashes** during live 5-min demo

---

## Appendices

### SmartNode API Endpoints (port 8080)

| Method | Path | Role |
|---|---|---|
| GET | `/api/price` | 24h of NOK/kWh prices (FakepoolSensor) |
| GET | `/api/entities` | Sensors/actuators wired in Factory |
| GET | `/api/state` | Live readings from all Factory sensors |
| GET | `/api/entities_full` | Full cache of all HA entities (registry) |
| GET | `/api/ha/states` | HA `/api/states` proxy |
| POST | `/api/actuate` | `{uri, state}` → fire actuator |
| POST | `/api/call_service` | `{domain, service, entity_id, data}` → generic HA service |
| POST | `/api/nlu` | Ollama NLU proxy with enriched system prompt |
| POST | `/api/target_temp` | MAPE-K `FTargetPenalty.TargetValue` constraint |
| POST | `/api/optimize` | 24h plan of cheapest hours before deadline |
| POST | `/api/execute_schedule` | Launches actuation plan over N "hours" |
| GET | `/api/schedules` | Active schedules |
| POST | `/api/cancel_schedule` | Cancels a schedule by ID |

### Critical Files

| File | Why |
|---|---|
| [SmartNode/SmartNode/Program.cs](SmartNode/SmartNode/Program.cs) | Entry + endpoints + NLU system prompt |
| [SmartNode/SmartNode/Factory.cs](SmartNode/SmartNode/Factory.cs) | OWL URI wiring → concrete impl. (lights, switches, input_number) |
| [SmartNode/SmartNode/HomeAssistantRegistry.cs](SmartNode/SmartNode/HomeAssistantRegistry.cs) | HA cache for dynamic LLM prompt |
| [SmartNode/SmartNode/ScheduleManager.cs](SmartNode/SmartNode/ScheduleManager.cs) | Configurable in-memory scheduler (demo mode) |
| [SmartNode/SmartNode/index.html](SmartNode/SmartNode/index.html) | Complete chatbox |
| [SmartNode/SmartNode/Properties/appsettings.json](SmartNode/SmartNode/Properties/appsettings.json) | Runtime config — `SaveMapekCycleData`, `Environment`, `FitnessSettings` |
| [SmartNode/Logic/Mapek/MapekManager.cs](SmartNode/Logic/Mapek/MapekManager.cs) | Orchestrator of the 5 MAPE-K phases |
| [models-and-rules/inference-rules.rules](models-and-rules/inference-rules.rules) | 459 lines of Jena rules |
| [models-and-rules/verification-rules.rules](models-and-rules/verification-rules.rules) | 549 lines of verification rules |
| [ontology/ruleless-digital-twins.ttl](ontology/ruleless-digital-twins.ttl) | Main ontology |

### Useful Diagrams for Slides

| File | Usage |
|---|---|
| `Diagrams/framework_architecture.drawio` | Slide 1 — system view |
| `Diagrams/inference_algorithm_phase_1.drawio` | Slide 3 — how it reasons |
| `Diagrams/inference_algorithm_phase_2.drawio` | Slide 3 (continued) |
| `Diagrams/decision_tree_example.drawio` | Slide 3 — action choice |
| `Diagrams/m370_temperature_curve.png` | Slide 1 or 3 — physical example |
| `Diagrams/single_optimal_condition.png`, `double-double_optimal_condition.png` | Slide 3 — OptimalCondition concept |
| `Diagrams/ontology-class-diagram.drawio` | Appendix — domain model |

---

*Last updated: April 2026 (to be updated before each work session).*
