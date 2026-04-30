# Ruleless Digital Twins Demo

A smart-home digital twin demo built with .NET 8, Home Assistant, semantic reasoning, FMU simulation, and live Nord Pool electricity prices.

The goal is to show how a smart home can observe live Home Assistant state, reason over an ontology, infer valid actions, simulate future outcomes, and optimize schedules without writing one-off automation rules for every device.

## What this project demonstrates

- Live Home Assistant integration: sensors, actuators, service calls, and entity discovery.
- Ruleless reasoning over a SOSA/SSN-based ontology with project-specific RDT extensions.
- Apache Jena inference that generates actuation actions from the model and rule set.
- MAPE-K adaptation loop: Monitor, Analyze, Plan, Execute, Knowledge.
- FMU look-ahead simulation during the planning phase.
- Nord Pool-aware scheduling for a configured EV charger demo workload.
- Runtime schedule persistence in `state-data/schedules.json`.
- Local natural-language interface through Ollama.

## Architecture overview

```text
Home Assistant  ── REST / WebSocket ──>  SmartNode (.NET 8)
     │                                      │
     │ live entities, sensors, prices        ├─ MAPE-K loop
     │                                      ├─ Apache Jena inference engine
     │                                      ├─ FMU look-ahead simulation
     │                                      ├─ Nord Pool optimizer
     ▼                                      │
  actuators <──────── execute actions ──────┘
```

## Important honesty note

The EV charging scenario uses live Home Assistant Nord Pool prices. The charger workload itself is currently a configured demo actuator (`CarCharger`, typically 11 kW for 4 hours) unless you connect the project to a real Home Assistant charger or Tesla entity.

If Home Assistant or the Nord Pool forecast is unavailable, the optimizer refuses to answer instead of silently using fake prices.

## Requirements

- Windows 11 or another .NET-compatible development environment.
- .NET 8 SDK.
- Java 17 or newer.
- Docker Desktop, if you want to run the Home Assistant demo container.
- Home Assistant running on `http://localhost:8123`.
- A Home Assistant long-lived access token.
- Ollama with `qwen2.5-coder:7b` for the natural-language interface.

## Quick start

```powershell
git clone https://github.com/sfrayan/ruleless-digital-twins-demo.git
cd ruleless-digital-twins-demo
git submodule update --init --recursive

dotnet restore SmartNode/SmartNode.csproj
dotnet build SmartNode/SmartNode.csproj -c Release
```

Set your Home Assistant token:

```powershell
$env:TOKEN_HA = "your_home_assistant_long_lived_access_token"
```

Run SmartNode:

```powershell
dotnet run --project SmartNode/SmartNode/SmartNode.csproj
```

Open the chat UI:

```powershell
start SmartNode/SmartNode/index.html
```

## Demo prompts

```text
turn on the living room
turn off the kitchen
set the temperature to 22 degrees
what's the house status
what is the energy price
charge the Tesla to 100% by 7am at the cheapest rate
```

## Expected runtime markers

In the SmartNode terminal, look for:

```text
Internal API listening on http://localhost:8080/
Starting the MAPE-K loop
NordPool: discovered config_entry ...
NordPool: ... → 96 slots added
Generated ActuationAction
Generated the inferred model
Running simulation #1
[OPTIMIZE] priceSource=homeassistant_nordpool
```

## Repository hygiene

Generated runtime files are intentionally ignored:

- `state-data/`
- inferred TTL outputs such as `models-and-rules/*-inferred.ttl`
- local SQLite caches
- local `.env` files
- local slide exports

Use `.env.example` as a template for local secrets. Never commit real Home Assistant tokens.

## More documentation

- [`DEMO_SETUP.md`](DEMO_SETUP.md): operational setup and troubleshooting guide.
- [`ROADMAP.md`](ROADMAP.md): project status, demo milestones, and future work.
