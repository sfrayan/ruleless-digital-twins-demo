# DEMO_SETUP.md — Lancement de la démo (Windows 11)

> Procédure copy-paste-ready pour lancer la démo `ruleless-digital-twins` de zéro sur Windows 11. Toutes les commandes sont en **PowerShell**.
>
> **Cible** : démo en moins de 5 minutes, à froid, sans toucher au code.
> **Voisins du repo** : `[ROADMAP.md](ROADMAP.md)` (vision plus large) · `[README.md](README.md)` (concepts).

---

## Sommaire
- [1. Prérequis](#1-prérequis)
- [1bis. Bootstrap (clone propre)](#1bis-bootstrap-clone-propre)
- [2. Lancement séquentiel](#2-lancement-séquentiel)
- [3. Vérifications lecture seule](#3-vérifications-lecture-seule)
- [4. Récupération en cas de panne](#4-récupération-en-cas-de-panne)
- [5. Arrêt propre](#5-arrêt-propre)
- [6. Export Home Assistant → TTL (tools/hass-to-rdt)](#6-export-home-assistant--ttl-toolshass-to-rdt)
- [7. Moteur d'inférence en standalone (moment fort démo)](#7-moteur-dinférence-en-standalone-moment-fort-démo)
- [8. Scénario démo live (script 5 phrases)](#8-scénario-démo-live-script-5-phrases)

---

## 1. Prérequis

### Logiciels installés
| Outil | Vérification | Source |
|---|---|---|
| Windows 11 | `winver` | — |
| Docker Desktop (WSL2 backend) | `docker --version` | https://docs.docker.com/desktop/install/windows-install/ |
| .NET 8 SDK | `dotnet --info` (cherche `Microsoft.NETCore.App 8.0.x`) | https://dotnet.microsoft.com/en-us/download/dotnet/8.0 |
| Java JRE 11+ | `java -version` | OpenJDK ou Adoptium |
| Ollama | `ollama --version` | https://ollama.com/download |

### Modèle Ollama requis
```powershell
ollama list
# Doit afficher : qwen2.5-coder:7b
# Sinon : ollama pull qwen2.5-coder:7b
```

### Conteneurs Docker requis
| Nom | Image | Port | Rôle |
|---|---|---|---|
| `ha-instance` | `kristofferwagen/ha-instance:latest` | 8123 | Home Assistant showcase |
| `jarvis-ollama` | `ollama/ollama:latest` | 11434 | LLM local pour le NLU |
| `mongo-rdt` *(optionnel)* | `mongo:latest` | 27017 | Case-based reasoning |

### Bind-mount HA
Le conteneur `ha-instance` doit être lié à `C:\ha-showcase-config\` pour persister le user admin et le token entre redémarrages :
```powershell
Test-Path C:\ha-showcase-config\configuration.yaml
# Doit retourner : True
```

Si `False`, voir [4.5 — recréer le conteneur ha-instance](#45-recréer-le-conteneur-ha-instance-from-scratch).

---

## 1bis. Bootstrap (clone propre)

> À exécuter **une seule fois** sur une machine neuve, ou après un `git clone` frais. Skip si tu as déjà un workspace fonctionnel.

### 1bis.1 — Submodules

Trois sous-modules sont déclarés dans `.gitmodules` (Femyou, Nordpool-FMU, PythonFMU) et **doivent** être tirés sinon SmartNode ne compilera pas :

```powershell
cd C:\dev\ruleless-digital-twins
git submodule update --init --recursive
```

### 1bis.2 — Démarrer la stack démo via Docker Compose

Le compose dédié `services/docker-compose.demo.yml` lance Home Assistant + MongoDB. RabbitMQ et Ollama y sont **commentés** (cf. notes dans le fichier — décommente seulement si tu démontres l'env `incubator` ou veux Ollama containerisé) :

```powershell
docker compose -f services/docker-compose.demo.yml up -d
docker compose -f services/docker-compose.demo.yml ps
# ha-instance et rdt-mongodb doivent être Up
```

Variables d'environnement utiles (à placer dans le shell ou un `.env` local) :

```powershell
$env:HA_CONFIG_DIR = "C:\ha-showcase-config"      # bind-mount HA (default)
$env:TZ            = "Europe/Oslo"                 # timezone HA
$env:TOKEN_HA      = "<long_lived_token>"          # requis pour SmartNode
$env:HA_URL        = "http://localhost:8123/api/"  # utilisé par hacvt_rdt
$env:RDT_NAMESPACE = "http://www.semanticweb.org/rayan/ontologies/2025/ha/"
```

> Ollama : si tu utilises l'instance Ollama du host (par défaut), ne décommente PAS le service `ollama` dans le compose — conflit sur le port 11434.

---

## 2. Lancement séquentiel

> Variante manuelle (équivalente au bootstrap §1bis.2 si tu préfères piloter chaque conteneur). Si tu as déjà fait `docker compose up -d`, saute à 2.2.

### 2.1 — Démarrer les conteneurs
```powershell
docker start ha-instance
docker start jarvis-ollama

# Optionnel — uniquement si UseCaseBasedFunctionality=true dans appsettings.json
# docker start mongo-rdt
```

Attends ~20 s puis :
```powershell
docker ps --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"
# ha-instance et jarvis-ollama doivent afficher "Up X seconds"
```

### 2.2 — Vérifier le réseau de `ha-instance`
HA doit être attaché au réseau `bridge` et avoir une IPv4 (`172.17.0.x`). Sinon zeroconf crashe en boucle (cf. [4.2](#42-ha-instance-na-pas-de-réseau-eth0-manquant)).

```powershell
docker exec ha-instance ip -4 addr show eth0
# Sortie attendue :
# 2: eth0@if10: <BROADCAST,MULTICAST,UP,LOWER_UP> ...
#     inet 172.17.0.x/16 brd 172.17.255.255 scope global eth0
```

Si tu vois `RTNETLINK answers: Cannot find device "eth0"` ou rien → fix [4.2](#42-ha-instance-na-pas-de-réseau-eth0-manquant).

Test HTTP :
```powershell
Invoke-WebRequest -Uri http://localhost:8123/ -UseBasicParsing -TimeoutSec 5 `
  | Select-Object StatusCode
# Attendu : StatusCode 200
```

### 2.3 — Définir `TOKEN_HA`
Le token long-lived HA est requis dans **chaque session PowerShell** où tu lances `dotnet run`. Il est stocké dans `C:\ha-showcase-config\.storage\auth_provider.homeassistant` côté HA, mais SmartNode le lit dans la variable d'environnement :

```powershell
$env:TOKEN_HA = "<colle_ici_ton_token_long_lived>"
```

Si tu n'as pas/plus de token : voir [4.4 — recréer un token](#44-token-perdu-ou-401-après-recréation-du-conteneur).

### 2.4 — Lancer SmartNode
```powershell
cd C:\dev\ruleless-digital-twins\SmartNode\SmartNode
dotnet run
```

Attendu dans les premières secondes :
```
HH:MM:SS info: SmartNode.Program[0]
      Internal API listening on http://localhost:8080/
HH:MM:SS info: Logic.Mapek.IMapekManager[0]
      Starting the MAPE-K loop. (maxRounds= -1)
```

Si `MAPE-K loop failed — HTTP API continues` apparaît : SmartNode tourne quand même (chatbox utilisable), mais HA est probablement injoignable → vérifie [2.2](#22-vérifier-le-réseau-de-ha-instance).

**Laisse cette fenêtre PowerShell ouverte pendant toute la démo.**

### 2.5 — Ouvrir le chatbox
Dans une autre fenêtre PowerShell :
```powershell
start C:\dev\ruleless-digital-twins\SmartNode\SmartNode\index.html
```

Vérifications visuelles dans le navigateur (interface 100% anglaise depuis avril 2026) :
- En-tête : pastille **verte** + texte `SmartNode + HA connected`
- Dashboard : Temperature / Power / Air quality affichent des valeurs (pas `—`)
- Section `QUICK ACTIONS` avec 6 chips : *Turn on living room*, *Turn off kitchen*, *Set 22°C*, *House status*, *Energy price*, *Charge Tesla overnight*

Si la pastille reste rouge → vérifie le terminal `dotnet run` (logs d'erreur) et [3](#3-vérifications-lecture-seule).

---

## 3. Vérifications lecture seule

> Aucune commande ci-dessous ne modifie rien — utilisable en plein run pour diagnostiquer.

### 3.1 — Conteneurs et réseau
```powershell
# État des 3 conteneurs critiques
docker ps -a --filter "name=ha-instance" --filter "name=jarvis-ollama" --filter "name=mongo-rdt" `
  --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"

# Restart policy de ha-instance et homeassistant (si présent)
docker inspect ha-instance --format "ha-instance: restart={{.HostConfig.RestartPolicy.Name}} status={{.State.Status}}"
docker inspect homeassistant --format "homeassistant: restart={{.HostConfig.RestartPolicy.Name}} status={{.State.Status}}" 2>$null

# IP eth0 de ha-instance
docker exec ha-instance ip -4 addr show eth0 2>$null

# Liste des conteneurs sur le réseau bridge
docker network inspect bridge --format '{{range .Containers}}{{.Name}} {{end}}'

# Qui écoute sur le port 8123 du host ?
Get-NetTCPConnection -LocalPort 8123 -State Listen -ErrorAction SilentlyContinue `
  | ForEach-Object { Get-Process -Id $_.OwningProcess } | Select-Object Id, ProcessName
```

### 3.2 — SmartNode et HA répondent
```powershell
# SmartNode interne
Invoke-RestMethod http://localhost:8080/api/entities | Format-List

# Lectures live capteurs
Invoke-RestMethod http://localhost:8080/api/state

# HA répond (sans token)
Invoke-WebRequest http://localhost:8123/ -UseBasicParsing | Select-Object StatusCode

# HA avec token (vérifier qu'il est valide)
Invoke-RestMethod -Headers @{Authorization="Bearer $env:TOKEN_HA"} http://localhost:8123/api/states `
  | Group-Object {$_.entity_id.Split('.')[0]} | Select-Object Name, Count
```

### 3.3 — Ollama prêt et NLU pipeline
```powershell
# Modèles disponibles
(Invoke-RestMethod http://localhost:11434/api/tags).models | Select-Object name, size

# Test NLU end-to-end (peut prendre 30-60s au premier appel — chargement modèle 4.7 GB)
Invoke-RestMethod -Uri http://localhost:8080/api/nlu -Method Post `
  -ContentType 'application/json' `
  -Body '{"message":"allume la lumière de la cuisine"}'
# Retour attendu : objet JSON avec intent="actuate" ou "call_service", entity_id ou target défini
```

### 3.4 — Registry HA dynamique chargé
```powershell
# Doit lister toutes les entités HA cachées par SmartNode (refresh 30s)
(Invoke-RestMethod http://localhost:8080/api/entities_full) | Measure-Object | Select-Object Count

# Détail des 3 premières
(Invoke-RestMethod http://localhost:8080/api/entities_full) | Select-Object -First 3
```

### 3.5 — Données stockées (si `SaveMapekCycleData=true`)
```powershell
# Cycles persistés (CSV par propriété et par actuateur)
Get-ChildItem state-data\ -Recurse `
  | Sort-Object LastWriteTime -Descending | Select-Object -First 10 Name, Length, LastWriteTime

# Historique des schedules (persiste entre les redémarrages SmartNode)
Get-Content state-data\schedules.json | ConvertFrom-Json | Select-Object id, targetName, status, startedAt

# Cases MongoDB (si UseCaseBasedFunctionality=true)
docker exec mongo-rdt mongosh --quiet --eval "use CaseBase; db.Cases.countDocuments()" 2>$null
```

### 3.6 — Vérifier les logs FMU (simulation visible)

Pendant que SmartNode tourne, chercher dans le terminal `dotnet run` :

```
Generating simulations.
Running simulation #1
Running simulation #2
...
Generated a total of N simulation paths.
```

> Ces lignes confirment que le FMU est exécuté pendant la phase **Plan** de MAPE-K.
> Si elles n'apparaissent pas, vérifier que `Environment` = `homeassistant` dans `appsettings.json`
> et que les fichiers `.fmu` sont présents dans `SmartNode/Implementations/FMUs/`.

### 3.7 — Advisory proactif Nord Pool

```powershell
# 204 = pas encore de cycle MAPE-K ; JSON = advisory calculé
Invoke-RestMethod http://localhost:8080/api/proactive/status
# Champs clés : shouldPreheat, shouldDeferLoad, reason, currentPrice, q1, q3
```

Dans l'UI, le bandeau proactif (🔥 preheat / ⏸️ defer) s'affiche automatiquement si l'advisory est actif.
Il se rafraîchit toutes les 30 s. Le bouton **Preheat now (+1°C)** appelle `/api/actuate` directement.

---

## 4. Récupération en cas de panne

### 4.1 — Le port 8123 est occupé par un autre conteneur

**Symptôme** : `docker start ha-instance` échoue avec `Bind for 0.0.0.0:8123 failed: port is already allocated`. Ou bien `ha-instance` démarre mais répond bizarrement (autre HA sur le port).

**Cause typique** : un second conteneur HA (`homeassistant`, lié à `C:\homeassistant\`) avec `RestartPolicy: always` qui squatte le port à chaque restart de Docker Desktop.

**Diagnostic** :
```powershell
docker ps -a --filter "publish=8123" --format "table {{.Names}}`t{{.Status}}`t{{.HostConfig.RestartPolicy.Name}}"
```

**Fix** :
```powershell
# 1. Désactiver l'auto-restart du conteneur fautif (sans le supprimer ; ses données restent)
docker update --restart=no homeassistant

# 2. L'arrêter
docker stop homeassistant

# 3. Vérifier qu'il ne revient pas
docker ps --filter "name=homeassistant" --format "{{.Names}} {{.Status}}"
# Doit afficher "Exited" ou rien

# 4. Relancer ha-instance
docker start ha-instance
```

Pour le rallumer plus tard (en stoppant ha-instance d'abord) :
```powershell
docker stop ha-instance
docker start homeassistant
```

### 4.2 — `ha-instance` n'a pas de réseau (eth0 manquant)

**Symptôme** : `docker exec ha-instance ip -4 addr show eth0` → `Cannot find device "eth0"`. Logs HA pleins de `OSError: [Errno 19] No such device` et `system does not have any enabled IPv4 addresses`.

**Cause typique** : Docker Desktop / WSL2 redémarré pendant que le conteneur tournait → la NetworkSettings devient vide.

**Diagnostic** :
```powershell
docker inspect ha-instance --format '{{range $k,$v := .NetworkSettings.Networks}}{{$k}}: IP={{$v.IPAddress}}{{end}}'
# Si vide → confirmé
```

**Fix** :
```powershell
# 1. Stopper
docker stop ha-instance

# 2. Reconnecter au bridge
docker network connect bridge ha-instance

# 3. Redémarrer
docker start ha-instance

# 4. Attendre 20-30s puis vérifier
Start-Sleep -Seconds 25
docker exec ha-instance ip -4 addr show eth0
# Doit afficher "inet 172.17.0.x/16"

Invoke-WebRequest http://localhost:8123/ -UseBasicParsing -TimeoutSec 5 | Select StatusCode
# Doit afficher 200
```

### 4.3 — HA crashe encore avec `OSError: [Errno 19]` malgré eth0 OK

**Symptôme** : eth0 présent, mais `docker logs ha-instance` montre toujours zeroconf crash sur multicast.

**Cause** : bug WSL2 connu sur l'opération `IP_ADD_MEMBERSHIP`.

**Fix** (par ordre d'invasivité, à essayer si le 4.2 ne suffit pas) :
```powershell
# Option A — relancer WSL2 et Docker Desktop
wsl --shutdown
# Puis relancer Docker Desktop manuellement (ou : Stop-Process -Name "Docker Desktop")
# Attendre 1 min que Docker reboote, puis reprendre la séquence 2.1

# Option B — mettre WSL2 à jour (PowerShell admin)
wsl --update
# Ferme tout, redémarre, recommence section 2

# Option C — reboot Windows complet
shutdown /r /t 0
```

**Solution durable** : la config `C:\ha-showcase-config\configuration.yaml` remplace déjà `default_config:` par une liste explicite (sans `zeroconf`/`ssdp`/`cloud`/`go2rtc`). Si tu re-pulls une nouvelle image et perds ce patch → re-extraire avec :
```powershell
# Voir 4.5 ci-dessous, étapes 1-3
```

### 4.4 — Token perdu ou 401 après recréation du conteneur

**Symptôme** : `Invoke-RestMethod -Headers @{Authorization="Bearer $env:TOKEN_HA"} http://localhost:8123/api/states` → HTTP 401.

**Cause** : recréation du conteneur a wipé `.storage/auth_provider.homeassistant` (ou tu as collé un token périmé).

**Fix — créer un user admin et générer un nouveau token** :
```powershell
# 1. Vérifier que le user admin existe (sinon "Total users: 0")
docker exec ha-instance hass --script auth list

# 2a. Si user existe mais pas de credentials → recréer le password
docker exec ha-instance hass --script auth add admin admin
# Attendu : "Auth created"

# 2b. Si user existe et tu connais le password → reset
# docker exec ha-instance hass --script auth change_password admin <nouveau_pwd>

# 3. Aller sur http://localhost:8123 → login admin / admin
# 4. Profil (icône en bas à gauche) → Security → Create Long-Lived Access Token
# 5. Donner un nom (ex: "smartnode") → COPIER le token (visible 1 seule fois !)

# 6. Réinjecter dans la session PowerShell de SmartNode
$env:TOKEN_HA = "<token_collé>"

# 7. Relancer SmartNode (Ctrl+C dans son terminal puis dotnet run)
```

### 4.5 — Recréer le conteneur `ha-instance` from scratch

À utiliser uniquement si `C:\ha-showcase-config\` est corrompu ou si tu pars d'une machine vierge.

```powershell
# 1. Extraire la config par défaut depuis l'image
docker create --name ha-extract-tmp kristofferwagen/ha-instance:latest
docker cp ha-extract-tmp:/config C:\ha-showcase-config
docker rm ha-extract-tmp

# 2. Patcher configuration.yaml pour désactiver les modules WSL2-incompatibles
#    (à faire à la main : remplacer la ligne `default_config:` par la liste
#     explicite documentée dans configuration.yaml — voir le commit qui a appliqué
#     ce patch ou regarder les sessions précédentes)

# 3. Supprimer l'ancien conteneur s'il existe
docker rm -f ha-instance

# 4. Recréer avec bind-mount + restart=no + bridge
docker run -d --name ha-instance -p 8123:8123 --network bridge `
  -v C:\ha-showcase-config:/config `
  --restart no `
  kristofferwagen/ha-instance:latest

# 5. Attendre 30-60s puis créer le user (cf. 4.4)
docker exec ha-instance hass --script auth add admin admin
```

### 4.6 — `dotnet run` crashe ou le port 8080 est occupé

```powershell
# Qui occupe le port 8080 ?
Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue `
  | ForEach-Object { Get-Process -Id $_.OwningProcess }

# Tuer un SmartNode fantôme
Get-Process -Name SmartNode -ErrorAction SilentlyContinue | Stop-Process

# Build vérouillé par un dotnet run précédent
# → Ctrl+C la fenêtre dotnet run actuelle, puis relance-la
```

### 4.7 — Premier `/api/nlu` lent (30-60s)

**Cause** : Ollama charge le modèle `qwen2.5-coder:7b` (4.7 GB) en RAM au premier appel.

**C'est normal**. Le chatbox a un timeout client de 10 s avec fallback keyword regex, donc la 1re question marche quand même via fallback. À partir de la 2e, c'est instantané (modèle déjà en RAM).

Pour pré-chauffer Ollama avant la démo :
```powershell
ollama run qwen2.5-coder:7b "test" 2>$null
# Quand le prompt revient, le modèle est en RAM
```

### 4.8 — Le chatbox affiche `SmartNode injoignable`

```powershell
# 1. Vérifier que SmartNode tourne
Invoke-RestMethod http://localhost:8080/api/entities

# 2. Si NOK → relancer dotnet run (cf. 2.4)

# 3. Si OK mais le chatbox dit injoignable → CORS ?
#    Le chatbox lit la fetch via file:// → vérifie qu'il est ouvert avec start
#    et pas glissé dans un onglet existant.
start C:\dev\ruleless-digital-twins\SmartNode\SmartNode\index.html
```

---

## 5. Arrêt propre

```powershell
# Ctrl+C dans la fenêtre dotnet run

# Arrêter les conteneurs (sans les supprimer — données préservées)
docker stop ha-instance
docker stop jarvis-ollama
# docker stop mongo-rdt   # si activé
```

Pour relancer plus tard, reprendre directement à [section 2](#2-lancement-séquentiel).

Si tu as démarré la stack via `docker compose up -d`, l'arrêt symétrique est :
```powershell
docker compose -f services/docker-compose.demo.yml down
```

---

## 6. Export Home Assistant → TTL (`tools/hass-to-rdt`)

> Génère/régénère `models-and-rules/homeassistant-instance.ttl` à partir de l'état courant de HA. À faire à chaque fois que tu ajoutes/renommes des entités HA. **Sans cet export à jour, le moteur d'inférence travaille sur un modèle obsolète.**

### 6.1 — Setup unique du venv

```powershell
cd C:\dev\ruleless-digital-twins\tools\hass-to-rdt
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements-cli.txt
```

L'installation pull `homeassistant` (~10 min la première fois — c'est normal, il pull tout l'écosystème HA).

### 6.2 — Lancement de l'export

Variables nécessaires (déjà couvertes en §1bis.2) :

```powershell
$env:TOKEN_HA      # token long-lived HA
$env:HA_URL        = "http://localhost:8123/api/"
$env:RDT_NAMESPACE = "http://www.semanticweb.org/rayan/ontologies/2025/ha/"
```

> ⚠️ Le second argument `TOKEN_HA` est le **nom** de la variable, pas la valeur du token. C'est `hacvt_rdt.py` qui ira lire l'env var.

```powershell
cd C:\dev\ruleless-digital-twins\tools\hass-to-rdt
.venv\Scripts\Activate.ps1
python hacvt_rdt.py $env:HA_URL TOKEN_HA `
    --namespace $env:RDT_NAMESPACE `
    --out ..\..\models-and-rules\homeassistant-instance.ttl
```

Sortie attendue : un `.ttl` à jour avec toutes les entités HA mappées sur SOSA/SSN + `rdt:hasIdentifier`. Le prochain cycle MAPE-K (le SmartNode reload le modèle à chaque cycle) prendra automatiquement la nouvelle version.

### 6.3 — Vérification rapide

```powershell
Get-Content ..\..\models-and-rules\homeassistant-instance.ttl | Select-String "rdt:hasIdentifier" | Measure-Object
# nombre attendu = nombre d'entités HA exposées
```

---

## Récap visuel — séquence minimale qui marche

Variante compose (clone propre) :
```
git submodule update --init --recursive
docker compose -f services/docker-compose.demo.yml up -d
$env:TOKEN_HA = "<token>"
cd SmartNode\SmartNode && dotnet run
start .\index.html
```

Variante manuelle (containers déjà créés) :
```
docker start ha-instance
docker start jarvis-ollama
docker exec ha-instance ip -4 addr show eth0       # check eth0 OK
$env:TOKEN_HA = "<token>"
cd C:\dev\ruleless-digital-twins\SmartNode\SmartNode
dotnet run
start .\index.html
```

Si une étape échoue → consulter la section 4 correspondante.

---

---

## 7. Moteur d'inférence en standalone (moment fort démo)

> Exécuter **avant** de lancer SmartNode pour avoir un `.ttl` inféré frais à montrer.

```powershell
cd models-and-rules
java -jar ruleless-digital-twins-inference-engine.jar `
     ..\ontology\ruleless-digital-twins.ttl `
     homeassistant-ha-instance.ttl `
     inference-rules.rules `
     homeassistant-ha-inferred-DEMO.ttl
```

Comparer les tailles (la valeur ajoutée du raisonnement est visible en bytes) :

```powershell
"{0} bytes — instance (entrée)" -f (Get-Item homeassistant-ha-instance.ttl).Length
"{0} bytes — inféré (sortie)"   -f (Get-Item homeassistant-ha-inferred-DEMO.ttl).Length
```

Pointer dans VS Code :
- `homeassistant-ha-instance.ttl` : données brutes, pas de violations calculées
- `homeassistant-ha-inferred-DEMO.ttl` : triplets inférés, chercher `meta:isViolated`

```powershell
Select-String -Path homeassistant-ha-inferred-DEMO.ttl -Pattern "isViolated"
```

---

## 8. Scénario démo live (script 5 phrases)

> **Conseil économies** : lancer le scénario le **matin** (avant 10h) avec la deadline 7h du lendemain.
> Cela donne ~21h de fenêtre → les prix Nord Pool varient davantage → **10-20 % d'économies** affichées
> au lieu de 2-4 % si on lance l'après-midi avec 4h restantes.

| # | Phrase à taper dans le chatbox | Ce que ça montre |
|---|-------------------------------|-----------------|
| 1 | `quelle est la température du salon ?` | NLU + query HA en direct |
| 2 | `allume la lumière de la cuisine` | Actuateur réel → toggle visible dans HA Lovelace |
| 3 | `mets la température à 23 degrés` | InputNumber actuator → curseur HA bouge |
| 4 | `quel est le statut de la maison ?` | Dashboard complet (temp + puissance + AQI + lumières) |
| 5 | `charge la Tesla à 100 % pour 7h du matin` | Optimisation Nord Pool + barre 24h + % économies |

Après la phrase 5 :
1. Cliquer **▶ Exécuter** → le bandeau `⏱ SCHEDULES` apparaît
2. `input_boolean.showcase_car_charger` toggle dans HA toutes les 60 s (mode démo : 1h = 1 min)
3. Cliquer ✕ sur le schedule pour le annuler → statut `cancelled` visible dans le bandeau

Si le bandeau proactif 🔥 est visible : expliquer l'advisory proactif en bonus.

### Known demo gotchas

| Symptôme | Cause | Remède |
|----------|-------|--------|
| `FileNotFoundException: Properties\appsettings.json` | Working directory ≠ projet SmartNode | Patch appliqué dans `Program.cs` : `appsettings.json` est résolu via le dossier de l'assembly. `dotnet run --project SmartNode/SmartNode/SmartNode.csproj` fonctionne maintenant depuis n'importe quel CWD. |
| Économies < 5% sur la charge Tesla | Fenêtre d'optimisation trop courte (n'inclut pas les heures creuses de nuit) | Lancer la charge **le matin** (avant 10h) avec deadline `7h le lendemain` → fenêtre 21h qui inclut les prix nocturnes (~0.99 NOK/kWh vs ~1.20 en pointe). |
| `[OPTIMIZE] windowBuckets=6` ou moins | Nord Pool retourne des slots 15-min, ancien code prenait `Take(24)` = 6h | Patch appliqué dans `NordPoolForecastProvider.cs` : `Take(horizon)` remplacé par filtre temporel `s.Start < nowLocal.AddHours(horizon)`. Vérifier dans le terminal SmartNode : `[OPTIMIZE] rawSlots=192 hourlyBuckets=48 windowBuckets=15...`. |
| `NordPool: ... area=NO5 not found in response` | Home Assistant retourne `"no5"` (lowercase) au lieu de `"NO5"` | Patch appliqué dans `NordPoolForecastProvider.cs` : lookup case-insensitive. Si l'erreur revient, l'aire HA réelle est loggée dans le body de la réponse. |
| Schedule disparaît après restart | OK avant le patch — était volatile | Patch appliqué : `state-data/schedules.json` est sauvegardé après chaque création/completion/cancel. Au restart, les `running` deviennent `interrupted`. |
| `MAPE-K loop failed — HTTP API continues` | HA injoignable mais SmartNode tourne | Vérifier `docker ps` (conteneur `ha-instance` up + port 8123) puis attendre le prochain cycle (60 s). |
| Chatbox dit *SmartNode unreachable* | Port 8080 occupé ou SmartNode crashé | `netstat -ano | findstr :8080` puis Ctrl+C dans la fenêtre `dotnet run` et relancer. |

### Logs à surveiller pendant la démo

| Log | Phase | Fichier source |
|-----|-------|----------------|
| `Generated ActuationAction` | Plan (inférence Java) | sortie JAR |
| `Running simulation #N` | Plan (FMU look-ahead) | `Logic/Mapek/MapekPlan.cs` |
| `NordPool: 2026-XX-XX → 96 slots added` | Forecast | `NordPoolForecastProvider.cs` |
| `[OPTIMIZE] rawSlots=192 hourlyBuckets=48 windowBuckets=15 ... cheapest3=04:00@0.9954, ...` | Optimize endpoint | `Program.cs` |
| `[SCHEDULE <id>] h=0 → ON (CarCharger)` | Execute schedule | `ScheduleManager.cs` |

### Questions probables du jury

| Question | Réponse courte |
|----------|----------------|
| *Pourquoi pas des règles fixes ?* | Flexibilité : nouvelles entités HA = 0 ligne de code ; le raisonnement s'adapte |
| *Comment scale à 300 entités ?* | Registry dynamique HA déjà câblé ; l'ontologie est générique SOSA/SSN |
| *Sécurité du token HA ?* | Variable d'env, jamais committé, scope long-lived |
| *Pourquoi Ollama local ?* | Privacy + latence + offline + 0 coût par token |
| *Que se passe-t-il si HA tombe ?* | MAPE-K cycle fail → log + retry automatique au cycle suivant (implémenté dans MapekManager) |

---

*Dernière mise à jour : 2026-04-28.*
