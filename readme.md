# Ruleless Digital Twins Demo

Smart-home digital twin demo using:

- .NET 8 SmartNode
- Home Assistant integration
- SOSA/SSN + ruleless digital twin ontology
- Apache Jena inference engine
- FMU look-ahead simulation
- Nord Pool electricity price optimization
- Ollama-based NLU for natural language commands

## What this demo shows

The system observes Home Assistant entities, reasons over an ontology, generates valid actuation actions, simulates future states, and optimizes schedules using live Nord Pool prices.

The EV charging scenario uses live Home Assistant Nord Pool prices, but the charger workload is a configured demo actuator unless connected to a real Home Assistant charger/Tesla entity.

If Home Assistant or Nord Pool is unavailable, the optimizer refuses to answer instead of silently using fake prices.

## Requirements

- .NET 8 SDK
- Java 17+
- Home Assistant running on `http://localhost:8123`
- A Home Assistant long-lived access token
- Ollama with `qwen2.5-coder:7b` for natural language interaction
- Optional: Docker for running Home Assistant locally

## Setup

```powershell
git clone https://github.com/sfrayan/ruleless-digital-twins-demo.git
cd ruleless-digital-twins-demo
dotnet restore SmartNode/SmartNode.csproj