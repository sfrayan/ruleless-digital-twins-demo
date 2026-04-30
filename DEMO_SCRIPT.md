# DEMO_SCRIPT.md — Live Defense Script (5–7 min)

> Complete script for the internship defense at Volker Stolz — week of **May 5, 2026**.
> Format: 8 timed sections, with **what to say** (prepared text) + **what to show** (on-screen actions) + **fallback plan** if the tech fails.
>
> Related documents: [ROADMAP.md](ROADMAP.md) (overview) · [DEMO_SETUP.md](DEMO_SETUP.md) (launch) · [readme.md](readme.md) (concepts).

---

## Table of Contents
- [Before the Defense](#before-the-defense)
- [Timing Overview](#timing-overview)
- [Section 1 — Project Context](#section-1--project-context-0000--0040)
- [Section 2 — Architecture in 60 Seconds](#section-2--architecture-in-60-seconds-0040--0140)
- [Section 3 — SmartNode Launch](#section-3--smartnode-launch-0140--0210)
- [Section 4 — Chatbox + Home Assistant Demo](#section-4--chatbox--home-assistant-demo-0210--0340)
- [Section 5 — Energy Scheduling](#section-5--energy-scheduling-0340--0510)
- [Section 6 — MAPE-K + OWL + FMU + Jena](#section-6--mape-k--owl--fmu--jena-0510--0610)
- [Section 7 — Known Limitations](#section-7--known-limitations-0610--0640)
- [Section 8 — Conclusion & Perspectives](#section-8--conclusion--perspectives-0640--0700)
- [Plan B — If a Demo Crashes](#plan-b--if-a-demo-crashes)
- [Prepared Q&A](#prepared-qa)

---

## Before the Defense

### Day Before (D-1)
- [ ] Follow the entire [DEMO_SETUP.md](DEMO_SETUP.md) procedure from scratch → everything must work in < 5 min from a cold machine
- [ ] Pre-load the Ollama model: `ollama run qwen2.5-coder:7b "ping"` (then Ctrl+C)
- [ ] Verify the full scenario from [Section 4](#section-4--chatbox--home-assistant-demo-0210--0340) works
- [ ] Backup `C:\ha-showcase-config\` zip ready at hand
- [ ] Slides open (LibreOffice / PowerPoint) with screenshots

### 30 Minutes Before
- [ ] Reboot machine → clean, no stray windows
- [ ] Disable notifications (Windows Focus mode)
- [ ] Plug in charger, disable screen sleep
- [ ] Run DEMO_SETUP.md sections 2.1 → 2.5 (containers + dotnet run + chatbox)
- [ ] Warm up Ollama: send an initial question in the chatbox to load the model into RAM
- [ ] Open 3 browser tabs:
  - Tab 1: `index.html` (chatbox) — **full screen**
  - Tab 2: `localhost:8123/lovelace/dev` (HA Lovelace) — logged in as admin
  - Tab 3: VS Code with `homeassistant-ha-inferred-DEMO.ttl` open (if ROADMAP Phase 2.2 done)
- [ ] Open the `dotnet run` terminal and resize it to be visible but not dominant

### Screen Layout (set up before the jury arrives)
```
┌─────────────────────────────────────────────────────────────┐
│   Browser tab 1 — chatbox (always visible)                  │
│   ┌──────────────┐  ┌─────────────────────────────────┐    │
│   │              │  │                                 │    │
│   │   chatbox    │  │   Home Assistant Lovelace      │    │
│   │   (left)     │  │   (right)                       │    │
│   │              │  │                                 │    │
│   └──────────────┘  └─────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```
→ Ideally two screens, otherwise split window tiling (Windows + arrow).

---

## Timing Overview

| Section | Duration | Cumulative |
|---|---|---|
| 1. Context | 00:40 | 00:40 |
| 2. Architecture in 60s | 01:00 | 01:40 |
| 3. SmartNode launch (already running, just show) | 00:30 | 02:10 |
| 4. Chatbox + HA demo | 01:30 | 03:40 |
| 5. Energy scheduling | 01:30 | 05:10 |
| 6. MAPE-K + OWL + FMU + Jena | 01:00 | 06:10 |
| 7. Known limitations | 00:30 | 06:40 |
| 8. Conclusion & perspectives | 00:20 | 07:00 |

> **Target: 7 minutes**. Keep a 30 s mental buffer by skipping Section 6 if running over (skip it and just mention it briefly).

---

## SECTION 1 — Project Context (00:00 → 00:40)

### 🎤 Prepared Text
> "Hello, I'm presenting the work done during my internship at Volker Stolz, at Western Norway University. The project is called **Ruleless Digital Twins**: it's a digital twin system for controlling a connected home, but without hardcoded automation rules like in traditional platforms.
>
> The challenge: a modern apartment today has 50 to 300 entities — lights, sensors, sockets, thermostats. We want the system to **understand natural language requests**, **anticipate** what will happen in the room, and **optimize energy costs** using hourly NordPool prices. No hand-written 'if X then Y' rules: reasoning from an **OWL ontology** + **FMU physical simulations**."

### 🖥️ On Screen
- Slide 1 open (title + CPS context diagram).
- *Optional*: on the slide, a visual of oscillating NordPool prices to introduce "why optimize."

### ⚠️ If Stressed
- If voice trembles: take a breath between "like in traditional platforms" and "The challenge."
- Keywords to mention: *digital twin*, *ruleless*, *natural language*, *NordPool*.

---

## SECTION 2 — Architecture in 60 Seconds (00:40 → 01:40)

### 🎤 Prepared Text
> "The architecture combines 4 building blocks. (1) **Home Assistant**, in a Docker container, communicating with real devices via REST. (2) **SmartNode**, a .NET 8 service that I extended during the internship: it runs a MAPE-K loop — Monitor, Analyze, Plan, Execute, Knowledge — every 60 seconds. (3) A **web chatbox** that I wrote in HTML/JavaScript, which talks to a local LLM — Ollama qwen2.5-coder — to interpret user phrases. (4) And at the heart of the reasoning, an **Apache Jena inference engine**, which applies symbolic rules on a SOSA/SSN ontology describing the home.
>
> Everything runs **locally**, no cloud, no third-party API. The LLM runs on the machine, the data stays local too."

### 🖥️ On Screen
- Slide 2: architecture diagram (export `Diagrams/framework_architecture.drawio` as PNG).
- Point the cursor at the 4 blocks as they are named.

### ⚠️ If Stressed
- Don't get lost in details — this is a 60 s overview, not the detailed MAPE-K slide (reserved for Section 6).
- If speeding up, do NOT skip the word "ruleless" or "MAPE-K."

---

## SECTION 3 — SmartNode Launch (01:40 → 02:10)

> *SmartNode is already running before the defense — we just show it.*

### 🎤 Prepared Text
> "Here is SmartNode running. The MAPE-K loop is active — you can see the cycles passing in real time: Monitor reads Home Assistant sensors, Knowledge applies inference rules, Plan simulates the future via a Modelica FMU… Let's quickly see what this looks like from the user's perspective."

### 🖥️ On Screen
- Bring the **`dotnet run` terminal** window to the foreground for 10 s.
- Point to a line like `Logic.Mapek.IMapekPlan[0] Running simulation #N` or `Executed query: SELECT ?...`.
- Switch back to the browser.

### ⚠️ If There's a Problem
- If the SmartNode terminal is silent (no logs) → switch to the chatbox directly, say "SmartNode is running in the background, let's query it."
- If the terminal shows a red exception → **don't dwell on it**, transition immediately to the chatbox.

---

## SECTION 4 — Chatbox + Home Assistant Demo (02:10 → 03:40)

> *The highlight. 3 prompts that demonstrate: (a) natural language understanding, (b) immediate action, (c) world state reading.*

### 🎤 Prepared Text (transition)
> "Here is the chatbox on the left, Home Assistant on the right. Everything that happens on the left, you see in real time on the right. Three examples."

### Prompt 1 — State Reading
**Type in the chatbox**: `what is the living room temperature?`

> 🎤 "First: we ask for the current state. The LLM identifies a *query_state*, SmartNode reads the Home Assistant sensor. You see the value displayed: 19.6 degrees, and it matches exactly the sensor in Lovelace on the right."

🖥️ Point to the chatbox dashboard + the sensor `Showcase Living Room Temperature` in HA.

### Prompt 2 — Direct Command
**Type**: `turn on the kitchen light`

> 🎤 "Now a command. The LLM chooses a *call_service* — `light.turn_on` — with entity `light.showcase_kitchen_light`. SmartNode executes it, and…"

🖥️ Wait 1-2 s. **Show the toggle changing in HA** (Direct Controls — Kitchen switches to ON, the icon lights up).

> 🎤 "It's instant. And note: I never hardcoded the name *kitchen_light*. SmartNode dynamically discovers HA entities every 30 seconds and injects them into the LLM prompt."

### Prompt 3 — Numeric Adjustment
**Type**: `set the temperature to 23 degrees`

> 🎤 "More subtle case: a direct command on a numeric value. The LLM extracts `23`, SmartNode pushes the value to HA's `input_number.showcase_temperature`. You see the slider move on the right."

🖥️ **Point to the HA slider** moving from 19.6 → 23.

> 🎤 "And I want to clarify: this mode is a *direct command*. If I wanted the system to optimize — for example, maintain 23°C overnight at the cheapest rate — that's a different case we'll see in 30 seconds."

### ⚠️ If There's a Problem
- **If a response is slow (> 5 s)**: "The first call loads the 4.7 GB Ollama model into memory — that's the only slow one, after that it's instant."
- **If HA doesn't react**: show the SmartNode terminal to exhibit the log `[CHATBOX] Actuate ...` proving SmartNode called HA. HA may take 1-2 s to refresh on the Lovelace side.
- **If everything fails**: go to [Plan B](#plan-b--if-a-demo-crashes).

---

## SECTION 5 — Energy Scheduling (03:40 → 05:10)

> *The "impressive" demo — shows optimization and the scheduler.*

### 🎤 Prepared Text (transition)
> "Now, optimized mode. Not an immediate command — an *intention* with a constraint. This is what sets our approach apart from traditional platforms: the system plans based on electricity prices."

### Action — Launch a Plan
**Type**: `charge the car to 100% by 7am`

> 🎤 "The LLM identifies `optimize_schedule`. It extracts the parameters: 7 hours duration, 7am deadline, target = `CarCharger`, estimated power 11 kW. SmartNode queries the NordPool price curve over 24 hours, selects the 7 cheapest hours before the deadline, and returns a visual plan."

🖥️ **Wait for the 24-hour colored bar to appear** (green = selected hours).

> 🎤 "The green bar is when the charger will be ON. Estimated total cost, and you can see the savings percentage compared to constant charging."

### Action — Execute the Plan
**Click the button** `▶ Execute plan (demo mode: 1h = 1min)`

> 🎤 "And now, SmartNode's scheduler takes over. In demo mode, I've compressed 24 hours into 24 minutes — each 'hour' of the plan lasts one real minute. The banner at the top shows progress."

🖥️ **Point to the "⏱ SCHEDULES" banner** that appears: `CarCharger running — h=0/24 (0%)`.

> 🎤 "And on the Home Assistant side, you see: `input_boolean.showcase_car_charger` turns ON **only** during the planned hours. The scheduler calls the HA actuator in real time, without user intervention."

🖥️ **Show the CarCharger toggle changing in HA**.

> 🎤 "In production, you replace the simulation with real NordPool prices and 1 minute with 1 hour. The pattern stays identical."

### ⚠️ If There's a Problem
- **If Ollama returns a bad plan** (deadline badly extracted): use the quick-chip `🚗 Charge Tesla overnight` instead — it has the same intent as a fallback keyword.
- **If the scheduler doesn't fire**: show the request `Invoke-RestMethod http://localhost:8080/api/schedules` in the terminal to exhibit the plan in memory with `current_hour` advancing.

---

## SECTION 6 — MAPE-K + OWL + FMU + Jena (05:10 → 06:10)

> *The technical layer. Adjust based on jury profile — shorter for non-technical jury.*

### 🎤 Prepared Text
> "Under the hood, what you just saw leverages four academic concepts.
>
> **MAPE-K**: Monitor / Analyze / Plan / Execute / Knowledge — the self-adaptation pattern for autonomous systems. Our loop runs every 60 seconds, and at each cycle it checks whether optimal conditions are satisfied. If not, it simulates several action combinations and selects the best one according to a *fitness* function.
>
> **OWL/SOSA-SSN**: the home is described by an ontology. Sensors, actuators, observable properties, optimal conditions — everything is modeled in RDF. This allows adding a new device without modifying the code, just by editing the `.ttl`.
>
> **FMU/Modelica**: for the Plan phase, we physically simulate the room — heat transfers, the effect of heating. We use Functional Mock-up Unit models generated by OpenModelica. This gives the system a **vision of the future** without acting on the real world.
>
> **Apache Jena**: the symbolic inference engine. At each Knowledge cycle, SmartNode forks a Java process that applies 459 lines of inference rules + 549 lines of verification rules on the instance model, and produces an inferred model containing deductions — for example `meta:isViolated true` to flag an unmet optimal condition."

### 🖥️ On Screen (option A — static slide)
- Slide 3 with the 4 concepts in quadrants + arrows showing their interaction.

### 🖥️ On Screen (option B — show Jena rules live)
- If you have time: open VS Code with `models-and-rules/inference-rules.rules`, show 1 rule for 5 s, explain briefly.
- Otherwise: stick with the slide.

### ⚠️ If Running Over
- **Skip this section entirely**. Natural transition: "Under the hood, we combine MAPE-K, OWL ontology, and FMU simulation — I can go into detail during questions if you'd like."

---

## SECTION 7 — Known Limitations (06:10 → 06:40)

> *Honesty > marketing. The jury appreciates lucidity.*

### 🎤 Prepared Text
> "Three important limitations, which I acknowledge:
>
> **First**, the prices used in the demo are simulated — the curve is intentionally flat, so the displayed savings (2 to 4%) are underestimated. With real NordPool prices, we expect 15 to 25% savings on EV charging.
>
> **Second**, the schedules don't survive a SmartNode restart — they're stored in RAM. For production deployment, serialization to a file or database is needed.
>
> **Third**, the LLM can occasionally hallucinate an entity name that doesn't exist. Currently SmartNode lets it through and the error surfaces from the HA call. The next step is to validate the returned `entity_id` against the registry before execution."

### 🖥️ On Screen
- Slide 4: the 3 limitations as bullet points.

---

## SECTION 8 — Conclusion & Perspectives (06:40 → 07:00)

### 🎤 Prepared Text
> "In summary, we have a functional digital twin that combines symbolic reasoning — Jena + OWL — and numerical reasoning — FMU + LLM —, controlled via natural language, without cloud.
>
> Three avenues for future work: integrate a real smart meter, scale to multiple households for coordinated demand response, and empirically measure the gap between planning and observed consumption.
>
> Thank you."

### 🖥️ On Screen
- Slide 5: conclusion + perspectives.
- Leave the slide displayed during questions.

---

## Plan B — If a Demo Crashes

| Failure | Symptom | Immediate Reaction |
|---|---|---|
| Chatbox stops responding | Red dot or no response | "I'll restart the service in the background" → do NOT actually do it live, move to the next point |
| HA doesn't react | Toggle doesn't move in Lovelace | Point to the SmartNode terminal to show the log `[CHATBOX] Actuate ...`, say "the command was sent, the HA render may lag" |
| Ollama timeout | No response > 10 s | The fallback keyword activates automatically → continue as if nothing happened |
| `dotnet run` crash | Red stack trace | Mentally open [DEMO_SETUP.md](DEMO_SETUP.md) Section 4.6 → in practice, say "I'll show the rest via slides", switch to slides only |
| Everything is broken | Catastrophe | Switch immediately to **slides** + screenshot of the demo recorded the day before (always have a backup MP4 video ready) |

> **Golden rule**: NEVER stay stuck on an error for more than 10 seconds. Continue, explain the concept verbally, come back to the tech only if it recovers on its own.

### Backup Video
- Record on D-1 an MP4 video of **the entire demo** (Sections 4 + 5) with OBS Studio or Xbox Game Bar.
- Keep it in an open tab: `file:///C:/dev/demo-backup.mp4`.
- In case of severe failure: *"I'll show you the walkthrough via the recording I prepared."* — switch to the video, continue the script verbally.

---

## Prepared Q&A

> Likely jury questions and calibrated answers (1–2 sentences max each).

### On Architecture
- **"Why not use a cloud LLM like GPT-4?"**
  > "For latency, privacy — home data never leaves the machine — and zero usage cost. Ollama runs locally, qwen2.5-coder is 4.7 GB and responds in 1–2 seconds."
- **"Why MAPE-K and not a simple control loop?"**
  > "MAPE-K cleanly separates perception, reasoning, and action. It allows us to plug different planning methods — FMU simulation today, RL tomorrow — without touching the rest."
- **"Why an OWL ontology and not a simple SQL database?"**
  > "Because we reason about *relationships* — a sensor observes a property, an action affects a property — not just values. With Jena, we can deduce that an OptimalCondition is violated *before* it actually is in reality."

### On Technology
- **"What if I add 200 HA entities?"**
  > "The registry refreshes dynamically and injects the list into the prompt. Today it fits in context. Beyond 500, we'd need to filter by domain or pre-classify the query."
- **"How do you handle conflicts — two opposite commands?"**
  > "For immediate direct commands, last one wins. For optimized plans, the scheduler maintains a single active plan per target — a new plan cancels the old one."
- **"HA token security?"**
  > "Environment variable, never committed, revocable long-lived scope from HA. For production, we'd move to OAuth 2.0."

### On Value
- **"What real benefit for the end user?"**
  > "On EV charging during NordPool off-peak hours, 15 to 25% savings. On heating with day-ahead planning, we projected 8–12% based on the literature, to be validated empirically."
- **"Why *ruleless* rather than an expert system?"**
  > "Because 'if X then Y' rules don't scale to 300 heterogeneous entities. Our system reasons over the model, not over hardcoded recipes."

### On Limitations
- **"What happens if Home Assistant goes down?"**
  > "SmartNode catches the exception, the HTTP API stays alive for the chatbox, the MAPE-K loop stops. When HA comes back, SmartNode needs a restart — or we add retry logic, which is on the roadmap."
- **"Experimental validation?"**
  > "That's the next step — instrument a real home for 2–3 weeks, compare planning vs observed consumption. Requires a smart meter, which was outside the internship scope."

---

## Key Phrases to Drop (Memo)

Mention at least once during the demo:
- ✅ **"ruleless"** (the project keyword)
- ✅ **"local, no cloud"** (strong differentiator)
- ✅ **"MAPE-K"** (the academic anchor)
- ✅ **"SOSA/SSN ontology"** (semantic rigor)
- ✅ **"dynamic registry"** (the mechanism that scales)
- ✅ **"demo mode: 1 hour = 1 minute"** (the trick that makes the demo watchable)

---

*Good luck with the defense! 🚀*

*Last updated: April 2026.*
