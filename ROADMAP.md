# ROADMAP — Démo Ruleless Digital Twins

> Plan d'action pour transformer ce repo en démo end-to-end exploitable en live.
> **Cible** : soutenance stage Volker Stolz — semaine du **5 mai 2026**.
> **Composants à montrer** : simulation FMU · moteur d'inférence Jena · données stockées · visualisation simple.
>
> Ce document complète [readme.md](readme.md) qui couvre l'architecture théorique. Ici on parle uniquement de l'**exécution** d'une démo reproductible.

---

## Sommaire

- [État actuel](#état-actuel)
- [Architecture cible de la démo](#architecture-cible-de-la-démo)
- [Phase 0 — Préparation](#phase-0--préparation-j-3-à-j-2)
- [Phase 1 — Stabilisation environnement](#phase-1--stabilisation-environnement-j-2)
- [Phase 2 — Activer les 4 composants](#phase-2--activer-les-4-composants-j-1)
- [Phase 3 — Préparation soutenance](#phase-3--préparation-soutenance-j)
- [Phase 4 — Optionnel post-soutenance](#phase-4--optionnel-post-soutenance)
- [Quick-start](#quick-start)
- [Critères de succès](#critères-de-succès-démo)
- [Annexes](#annexes)

---

## État actuel

### ✅ Ce qui fonctionne déjà

| Composant | Statut | Localisation |
|---|---|---|
| Boucle MAPE-K complète | ✅ | `SmartNode/Logic/Mapek/` (Manager + Monitor + Analyze + Plan + Execute + Knowledge) |
| Ontologie SOSA/SSN + extensions RDT | ✅ | `ontology/ruleless-digital-twins.ttl` |
| Inference engine (Apache Jena 5.3.0) | ✅ | `models-and-rules/ruleless-digital-twins-inference-engine.jar` (15 MB) |
| Règles Jena | ✅ | `models-and-rules/inference-rules.rules` (459 l.) + `verification-rules.rules` (549 l.) |
| Modèles d'instance | ✅ | `instance-model-1/2.ttl`, `M370.ttl`, `homeassistant-ha-instance.ttl`, `nordpool-simple.ttl` |
| Simulation physique FMU | ✅ | `SmartNode/Implementations/FMUs/` (m370, incubator) — chargés via Femyou |
| Intégration Home Assistant | ✅ | REST + 4 kinds d'actuateurs (Light, Switch, InputBoolean, InputNumber) |
| API HTTP SmartNode (chatbox) | ✅ | port 8080, 13 endpoints (cf. [annexes](#annexes)) |
| Chatbox web | ✅ | `SmartNode/SmartNode/index.html` — dashboard, NLU, scheduler UI |
| NLU dynamique | ✅ | Ollama `qwen2.5-coder:7b` + registry HA injecté dans le prompt + fallback keyword |
| Scheduler in-memory | ✅ | `SmartNode/SmartNode/ScheduleManager.cs` — plans 24h, mode démo 1h=60s |
| Optimisation prix | ✅ | `/api/optimize` — picks N cheapest hours avant deadline |
| Tests | ✅ | `SmartNode/TestProject/` — 9 fichiers xUnit (Mapek, Inference, HA, FMU, NordPool…) |

### ⚠️ Ce qui manque pour une démo crédible

| Manque | Impact démo | Effort |
|---|---|---|
| Setup réplicable documenté | Si HA crash le jour J, panique en live | XS |
| Données prix réalistes (pas de FakepoolSensor plat) | Économies affichées ≈ 2-4 % seulement, peu impressionnant | S |
| Persistence visible des cycles MAPE-K | Pas d'illustration concrète du « digital twin stocke son histoire » | XS |
| Scénarios de démo scriptés | Risque de bafouiller en live | S |
| Slides | Tâche #3 de `Faire.md` toujours ouverte | M |
| Inferred model exhibable | Le `.ttl` inféré tourne en RAM mais on ne le montre jamais | XS |

---

## Architecture cible de la démo

```
┌──────────────────────────────────────────────────────────────────────┐
│                         SOUTENANCE — DÉMO LIVE                        │
└──────────────────────────────────────────────────────────────────────┘

   [1] CHATBOX (browser)                         [2] HOME ASSISTANT
   ┌───────────────────┐                        ┌──────────────────┐
   │ Dashboard live    │                        │ Lovelace UI      │
   │ + 6 quick chips   │     "allume salon"     │ Switches react   │
   │ + plans 24h       │   ───────────────────> │ en direct        │
   │ + barre énergie   │                        │ (port :8123)     │
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
   │   │  → inferred.ttl     │           │ → fitness Energy×€ │     │
   │   └─────────────────────┘           └────────────────────┘     │
   │                                                                 │
   │   [5] DONNÉES STOCKÉES                                         │
   │   ┌─────────────────────┐                                      │
   │   │ state-data/cycle-*  │  (SaveMapekCycleData=true)           │
   │   │ + MongoDB cases     │  (UseCaseBasedFunctionality=true)    │
   │   └─────────────────────┘                                      │
   │                                                                 │
   │   [6] NLU                                                       │
   │   Ollama qwen2.5-coder:7b (port 11434)                         │
   └─────────────────────────────────────────────────────────────────┘
```

Les 4 composants à mettre en avant pendant la démo :
1. **Simulation** → FMU exécutée pendant la phase Plan (logs visibles dans le terminal SmartNode)
2. **Inference engine** → JAR exécuté à chaque cycle Knowledge (montrer un fichier inferred TTL avant/après)
3. **Données stockées** → fichiers JSON dans `state-data/` + entrées MongoDB (optionnel)
4. **Visualisation** → chatbox + lovelace HA en parallèle

---

## Phase 0 — Préparation (J-3 à J-2)

> *Ne touche à rien dans le code. Inventaire et préparation des artefacts.*

- [ ] **0.1** Cloner le repo dans un dossier *propre* (`C:\demo\ruleless-digital-twins`) pour éviter le bruit des sessions de dev.
- [ ] **0.2** Vérifier que les fichiers démo critiques existent et ne sont pas corrompus :
  - `models-and-rules/ruleless-digital-twins-inference-engine.jar` (≈ 15 MB)
  - `models-and-rules/instance-model-1.ttl` et `inferred-model-1.ttl`
  - `models-and-rules/inference-rules.rules` et `verification-rules.rules`
  - `ontology/ruleless-digital-twins.ttl`
  - `SmartNode/Implementations/FMUs/*.fmu` (au moins 1 FMU compilé)
- [ ] **0.3** Vérifier les services externes installés :
  - `docker --version`, `docker ps` (Docker Desktop OK)
  - `dotnet --info` ≥ 8.0
  - `java -version` ≥ 11
  - `ollama list` contient `qwen2.5-coder:7b`
- [ ] **0.4** Backup `C:\ha-showcase-config\` (admin user + token + helpers déjà créés) :
  ```powershell
  Compress-Archive -Path C:\ha-showcase-config\* `
    -DestinationPath C:\demo\ha-showcase-config-backup.zip
  ```
- [ ] **0.5** Sauvegarder le token long-lived HA dans un coffre-fort (1Password, fichier chiffré local…) pour pouvoir le remettre en `$env:TOKEN_HA` en moins de 30s.

---

## Phase 1 — Stabilisation environnement (J-2)

> *Garantir que la séquence de lancement marche du premier coup, à froid.*

- [ ] **1.1** Test à blanc : reboot complet de la machine, puis exécuter le [quick-start](#quick-start) sans rien éditer. Tout doit fonctionner en < 5 min.
- [ ] **1.2** Documenter dans [DEMO_SETUP.md](#annexes) (à créer en Phase 3) :
  - L'ordre exact des `docker start`
  - Comment récupérer si `ha-instance` n'a pas d'eth0 (`docker network connect bridge ha-instance`)
  - Comment recréer le user admin si auth perdu (`docker exec ha-instance hass --script auth add admin admin`)
  - Comment relancer si Ollama n'est pas chargé (premier appel de 30-60s)
- [ ] **1.3** Activer la persistance MAPE-K pour la démo. Éditer **uniquement la config** (pas de code) :
  - `SmartNode/SmartNode/Properties/appsettings.json`
  - Mettre `"SaveMapekCycleData": true`
  - Le dossier `state-data/` se remplira de fichiers JSON par cycle
- [ ] **1.4** *(Optionnel)* Activer le case-based reasoning pour montrer MongoDB :
  - Démarrer un MongoDB local : `docker run -d --name mongo-rdt -p 27017:27017 mongo:latest`
  - Mettre `"UseCaseBasedFunctionality": true` dans `appsettings.json`
  - Sinon, sauter cette case et se contenter de `state-data/`
- [ ] **1.5** Tester le JAR d'inférence en standalone (montrable directement en démo) :
  ```powershell
  cd C:\dev\ruleless-digital-twins\models-and-rules
  java -jar ruleless-digital-twins-inference-engine.jar `
       ..\ontology\ruleless-digital-twins.ttl `
       homeassistant-ha-instance.ttl `
       inference-rules.rules `
       homeassistant-ha-inferred-DEMO.ttl
  ```
  Comparer les tailles `instance` (avant) vs `inferred` (après) — ça montre la valeur ajoutée du raisonnement.

---

## Phase 2 — Activer les 4 composants (J-1)

> *Pour chaque composant, prévoir 1 « moment fort » où il est rendu visible au jury.*

### 2.1 — Simulation FMU
- [ ] Confirmer que `appsettings.json` pointe sur l'environnement choisi (`"Environment": "homeassistant"`).
- [ ] Lancer `dotnet run` et **identifier dans les logs** la phase Plan : on doit voir `Running simulation #1`, `Parameters: ...`, `New values: ...`. C'est le FMU qui projette les futurs N cycles.
- [ ] Préparer une capture de terminal montrant 5-6 simulations consécutives → à montrer en slide ou en live.

### 2.2 — Inference engine
- [ ] Faire tourner le JAR manuellement (commande de la phase 1.5) **avant** de lancer SmartNode → on a un `.ttl` inféré frais à exhiber.
- [ ] Ouvrir `homeassistant-ha-inferred-DEMO.ttl` dans VS Code → mettre en évidence quelques triplets dérivés (par exemple les `meta:isViolated true` produits par les règles de vérification).
- [ ] *Bonus* : créer une slide « Avant / Après » avec une portion du `.ttl` instance (sans inférence) vs `.ttl` inferred (avec règles appliquées).

### 2.3 — Données stockées
- [ ] Pendant que SmartNode tourne (10-20 cycles), montrer le contenu de `state-data/` qui se remplit.
- [ ] *Si MongoDB activé* : ouvrir MongoDB Compass ou `mongosh` :
  ```
  mongosh "mongodb://localhost:27017"
  use CaseBase
  db.Cases.countDocuments()
  db.Cases.findOne()
  ```
- [ ] Préparer une capture des deux types de stockage (fichiers JSON + collection Mongo).

### 2.4 — Visualisation
- [ ] Mettre côte à côte sur l'écran :
  - **Browser tab 1** : `index.html` du chatbox (dashboard + chips + bandeau plannings).
  - **Browser tab 2** : `localhost:8123` HA Lovelace (Direct Controls + Sensors + Environment Inputs).
- [ ] Préparer le « scénario » live :
  1. *« Quelle est la température du salon ? »* → réponse chatbox + match HA dashboard.
  2. *« Allume la lumière de la cuisine. »* → toggle visible dans HA en temps réel.
  3. *« Mets la température à 23 degrés. »* → curseur HA bouge instantanément (via `InputNumber` actuator).
  4. *« Charge la voiture à 100 % pour 7 h du matin. »* → barre 24 h apparaît, `Économie : N %`, bouton « ▶ Exécuter » → bandeau live et `input_boolean.showcase_car_charger` toggle dans HA.
  5. *« Active la scène nuit. »* → test du NLU dynamique via `/api/call_service` (la chatbox connaît toutes les entités HA).

---

## Phase 3 — Préparation soutenance (J)

- [ ] **3.1** Slides (5 slides, 7-10 min total) :
  1. **Contexte & objectifs** : CPS, jumeaux numériques *ruleless* (vs règles fixes), MAPE-K, FMU, ontologie OWL.
  2. **Cahier des charges** : pilotage énergétique (chauffage / charge VE) sous contraintes (confort + budget + heures creuses).
  3. **Réalisations** : `hacvt_rdt.py` (export HA → OWL), chatbox NLU, scheduler optimisé, fitness Energy × Price, intégration HA via REST.
  4. **Démo live** (3-4 min) : 5 phrases au chatbox, montrer HA réagir, montrer un plan 24 h optimisé.
  5. **Conclusion & perspectives** : prix NordPool réels, multi-foyers, intégration smart-meter, persistance horizontale (Influx).
- [ ] **3.2** Captures de l'architecture (drawio → PNG/SVG) à inclure dans les slides :
  - `Diagrams/framework_architecture.drawio` → exporter en PNG.
  - `Diagrams/inference_algorithm_phase_1.drawio` et `..._phase_2.drawio` pour expliquer le raisonnement.
- [ ] **3.3** Créer un fichier **`DEMO_SETUP.md`** à la racine du repo, contenant :
  - Les commandes du [Quick-start](#quick-start) sous forme copy-paste-ready
  - Les remèdes aux pannes connues (eth0 manquant, auth perdu, port :8123 squatté…)
  - Le backup `C:\ha-showcase-config\` avec instructions de restauration
- [ ] **3.4** Répétition à blanc filmée (smartphone) pour identifier les passages flous et chronométrer (cible 8-10 min).
- [ ] **3.5** Préparer 3-4 questions probables du jury et les réponses :
  - *« Pourquoi pas une approche par règles fixes ? »* → flexibilité, adaptation à de nouveaux contextes sans recoder.
  - *« Comment scale à 300 entités ? »* → registry dynamique HA déjà câblé, à tester sur volume.
  - *« Sécurité du token HA ? »* → variable d'env, jamais commit, scope long-lived.
  - *« Pourquoi Ollama local plutôt qu'une API cloud ? »* → privacy + latence + offline-capable + pas de coût.

---

## Phase 4 — Optionnel post-soutenance

> *Idées d'amélioration à mentionner en perspectives, à coder seulement si temps disponible.*

- [ ] Remplacer `FakepoolSensor` par un lecteur de prix NordPool **réels** (CSV ou API `nordpool-api.com`). Génère des économies de 15-25 % visibles, beaucoup plus convaincant.
- [ ] Persistance des schedules (sérialisation JSON dans `state-data/schedules.json`) pour survivre au restart SmartNode.
- [ ] Auto-recovery MAPE-K avec retry exponentiel quand HA redevient joignable.
- [ ] Petit dashboard graphique (Grafana + InfluxDB) lisant `state-data/` ou MongoDB → courbe énergie / coût / température sur 24 h.
- [ ] Panneau « Entités » dans le chatbox listant tout ce que le NLU peut piloter (discoverability).
- [ ] Validation `entity_id` côté SmartNode contre `HomeAssistantRegistry` pour éviter les hallucinations LLM.
- [ ] Tests d'intégration end-to-end (HA mock + scheduler) pour CI.

---

## Quick-start

> *À copier-coller dans une session PowerShell propre. Ne nécessite aucune édition de code.*

```powershell
# 1. Containers requis
docker start ha-instance      # HA showcase, port 8123
docker start jarvis-ollama    # LLM local, port 11434
# (optionnel) docker start mongo-rdt   # case-based reasoning

# 2. Vérifier que ha-instance a bien une IP (parfois eth0 manquant après wsl shutdown)
docker exec ha-instance ip -4 addr show eth0
# Si erreur "no eth0" :
#   docker stop ha-instance
#   docker network connect bridge ha-instance
#   docker start ha-instance

# 3. Token HA (à exporter à chaque nouvelle session PS)
$env:TOKEN_HA = "<token_long_lived_HA>"

# 4. Lancer SmartNode
cd C:\dev\ruleless-digital-twins\SmartNode\SmartNode
dotnet run

# 5. Ouvrir le chatbox (autre fenêtre)
start C:\dev\ruleless-digital-twins\SmartNode\SmartNode\index.html
```

Vérifications rapides :
```powershell
# SmartNode répond
Invoke-RestMethod http://localhost:8080/api/entities

# HA répond avec auth valide
Invoke-RestMethod -Headers @{Authorization="Bearer $env:TOKEN_HA"} http://localhost:8123/api/states `
  | Group-Object {$_.entity_id.Split('.')[0]} | Select Name, Count

# Ollama prêt
(Invoke-RestMethod http://localhost:11434/api/tags).models | Select-Object name
```

---

## Critères de succès démo

À la fin de la soutenance, on doit avoir prouvé chaque point :

- [ ] **Chatbox dialogue** en français, comprend des phrases naturelles, répond pertinent
- [ ] **Au moins 1 ordre direct** allume / éteint une vraie entité HA → toggle visible en live dans Lovelace
- [ ] **Au moins 1 plan optimisé** affiche barre 24 h + coût + % d'économies, peut être exécuté → toggle réel toutes les 60 s en mode démo
- [ ] **Inference engine** : montrer le JAR exécuté en standalone, comparer instance vs inferred TTL
- [ ] **Simulation FMU** : logs MAPE-K Plan visibles, expliquer la look-ahead window
- [ ] **Persistance** : ouvrir `state-data/` et exhiber les fichiers de cycles
- [ ] **Aucun crash** pendant la démo live (5 min ininterrompus)

---

## Annexes

### Endpoints API SmartNode (port 8080)

| Méthode | Path | Rôle |
|---|---|---|
| GET | `/api/price` | 24 h de prix NOK/kWh (FakepoolSensor) |
| GET | `/api/entities` | Capteurs/actuateurs câblés dans Factory |
| GET | `/api/state` | Lectures live de tous les capteurs Factory |
| GET | `/api/entities_full` | Cache complet de toutes les entités HA (registry) |
| GET | `/api/ha/states` | Proxy `/api/states` HA |
| POST | `/api/actuate` | `{uri, state}` → fire actuateur |
| POST | `/api/call_service` | `{domain, service, entity_id, data}` → service HA générique |
| POST | `/api/nlu` | Proxy NLU Ollama avec system prompt enrichi |
| POST | `/api/target_temp` | Contrainte MAPE-K `FTargetPenalty.TargetValue` |
| POST | `/api/optimize` | Plan 24 h des heures les moins chères avant deadline |
| POST | `/api/execute_schedule` | Lance un plan d'actuation sur N « heures » |
| GET | `/api/schedules` | Plannings actifs |
| POST | `/api/cancel_schedule` | Annule un planning par ID |

### Fichiers critiques

| Fichier | Pourquoi |
|---|---|
| [SmartNode/SmartNode/Program.cs](SmartNode/SmartNode/Program.cs) | Entrée + endpoints + system prompt NLU |
| [SmartNode/SmartNode/Factory.cs](SmartNode/SmartNode/Factory.cs) | Câblage URI OWL → impl. concrète (lights, switches, input_number) |
| [SmartNode/SmartNode/HomeAssistantRegistry.cs](SmartNode/SmartNode/HomeAssistantRegistry.cs) | Cache HA pour le prompt LLM dynamique |
| [SmartNode/SmartNode/ScheduleManager.cs](SmartNode/SmartNode/ScheduleManager.cs) | Scheduler in-memory paramétrable (mode démo) |
| [SmartNode/SmartNode/index.html](SmartNode/SmartNode/index.html) | Chatbox complète |
| [SmartNode/SmartNode/Properties/appsettings.json](SmartNode/SmartNode/Properties/appsettings.json) | Config runtime — `SaveMapekCycleData`, `Environment`, `FitnessSettings` |
| [SmartNode/Logic/Mapek/MapekManager.cs](SmartNode/Logic/Mapek/MapekManager.cs) | Orchestrateur des 5 phases MAPE-K |
| [models-and-rules/inference-rules.rules](models-and-rules/inference-rules.rules) | 459 lignes de règles Jena |
| [models-and-rules/verification-rules.rules](models-and-rules/verification-rules.rules) | 549 lignes de règles de vérification |
| [ontology/ruleless-digital-twins.ttl](ontology/ruleless-digital-twins.ttl) | Ontologie principale |

### Diagrammes utiles pour les slides

| Fichier | Usage |
|---|---|
| `Diagrams/framework_architecture.drawio` | Slide 1 — vue système |
| `Diagrams/inference_algorithm_phase_1.drawio` | Slide 3 — comment ça raisonne |
| `Diagrams/inference_algorithm_phase_2.drawio` | Slide 3 (suite) |
| `Diagrams/decision_tree_example.drawio` | Slide 3 — choix d'action |
| `Diagrams/m370_temperature_curve.png` | Slide 1 ou 3 — exemple physique |
| `Diagrams/single_optimal_condition.png`, `double-double_optimal_condition.png` | Slide 3 — concept d'OptimalCondition |
| `Diagrams/ontology-class-diagram.drawio` | Annexe — modèle de domaine |

---

*Dernière mise à jour : avril 2026 (à actualiser avant chaque session de travail).*
