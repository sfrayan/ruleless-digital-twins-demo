# DEMO_SCRIPT.md — Script de soutenance live (5-7 min)

> Script complet pour la soutenance du stage chez Volker Stolz — semaine du **5 mai 2026**.
> Format : 8 sections chronométrées, avec **ce qu'on dit** (texte préparé) + **ce qu'on montre** (actions à l'écran) + **plan de secours** si la techno bug.
>
> Documents voisins : [ROADMAP.md](ROADMAP.md) (vue d'ensemble) · [DEMO_SETUP.md](DEMO_SETUP.md) (lancement) · [readme.md](readme.md) (concepts).

---

## Sommaire
- [Avant la soutenance](#avant-la-soutenance)
- [Vue d'ensemble du timing](#vue-densemble-du-timing)
- [Section 1 — Contexte du projet](#section-1--contexte-du-projet-0000--0040)
- [Section 2 — Architecture en 60 secondes](#section-2--architecture-en-60-secondes-0040--0140)
- [Section 3 — Lancement SmartNode](#section-3--lancement-smartnode-0140--0210)
- [Section 4 — Démo chatbox + Home Assistant](#section-4--démo-chatbox--home-assistant-0210--0340)
- [Section 5 — Planification énergétique](#section-5--planification-énergétique-0340--0510)
- [Section 6 — MAPE-K + OWL + FMU + Jena](#section-6--mape-k--owl--fmu--jena-0510--0610)
- [Section 7 — Limites connues](#section-7--limites-connues-0610--0640)
- [Section 8 — Conclusion & perspectives](#section-8--conclusion--perspectives-0640--0700)
- [Plan B — si une démo plante](#plan-b--si-une-démo-plante)
- [Q&A préparée](#qa-préparée)

---

## Avant la soutenance

### J-1 (la veille)
- [ ] Suivre toute la procédure de [DEMO_SETUP.md](DEMO_SETUP.md) à blanc → tout doit marcher en < 5 min depuis machine éteinte
- [ ] Pré-charger le modèle Ollama : `ollama run qwen2.5-coder:7b "ping"` (puis Ctrl+C)
- [ ] Vérifier que le scénario complet de la [section 4](#section-4--démo-chatbox--home-assistant-0210--0340) marche
- [ ] Backup `C:\ha-showcase-config\` zip à portée de main
- [ ] Slides ouvertes (LibreOffice / PowerPoint) avec captures

### 30 min avant
- [ ] Reboot machine → propre, sans fenêtres parasites
- [ ] Désactiver notifications (Windows Focus mode)
- [ ] Brancher l'alim, désactiver veille écran
- [ ] Lancer DEMO_SETUP.md sections 2.1 → 2.5 (containers + dotnet run + chatbox)
- [ ] Préchauffer Ollama : envoyer une 1re question dans le chatbox pour que le modèle soit en RAM
- [ ] Ouvrir 3 onglets navigateur :
  - Onglet 1 : `index.html` (chatbox) — **plein écran**
  - Onglet 2 : `localhost:8123/lovelace/dev` (HA Lovelace) — connecté avec admin
  - Onglet 3 : VS Code avec `homeassistant-ha-inferred-DEMO.ttl` ouvert (si Phase 2.2 de ROADMAP faite)
- [ ] Ouvrir le terminal `dotnet run` et le redimensionner pour qu'il soit visible mais pas dominant

### Configuration écran (à régler avant le jury)
```
┌─────────────────────────────────────────────────────────────┐
│   Onglet navigateur 1 — chatbox (visible toujours)         │
│   ┌──────────────┐  ┌─────────────────────────────────┐    │
│   │              │  │                                 │    │
│   │   chatbox    │  │   Home Assistant Lovelace      │    │
│   │   (left)     │  │   (right)                       │    │
│   │              │  │                                 │    │
│   └──────────────┘  └─────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```
→ idéalement deux écrans, sinon split window en mosaïque (Windows + flèche).

---

## Vue d'ensemble du timing

| Section | Durée | Cumulé |
|---|---|---|
| 1. Contexte | 00:40 | 00:40 |
| 2. Architecture en 60s | 01:00 | 01:40 |
| 3. Lancement SmartNode (déjà lancé, juste montrer) | 00:30 | 02:10 |
| 4. Démo chatbox + HA | 01:30 | 03:40 |
| 5. Planification énergétique | 01:30 | 05:10 |
| 6. MAPE-K + OWL + FMU + Jena | 01:00 | 06:10 |
| 7. Limites connues | 00:30 | 06:40 |
| 8. Conclusion & perspectives | 00:20 | 07:00 |

> **Cible 7 minutes**. Garder 30 s de marge mentale en supprimant la section 6 si on dérape (la sauter et juste la mentionner).

---

## SECTION 1 — Contexte du projet (00:00 → 00:40)

### 🎤 Texte préparé
> « Bonjour, je vous présente le travail réalisé pendant mon stage chez Volker Stolz, à Western Norway University. Le projet s'appelle **Ruleless Digital Twins** : c'est un système de jumeau numérique pour piloter une maison connectée, mais sans règles d'automation hardcodées comme dans les plateformes classiques.
>
> Le défi : un appartement moderne, c'est aujourd'hui 50 à 300 entités — lumières, capteurs, prises, thermostats. On veut que le système **comprenne ce qu'on lui demande en langage naturel**, qu'il **anticipe** ce qui va se passer dans la pièce, et qu'il **optimise les coûts** d'énergie avec les prix horaires NordPool. Pas de règles « si X alors Y » écrites à la main : un raisonnement à partir d'une **ontologie OWL** + des **simulations physiques FMU**. »

### 🖥️ À l'écran
- Slide 1 ouverte (titre + diagramme contexte CPS).
- *Optionnel* : sur la slide, un visuel des prix NordPool oscillant pour amorcer le « pourquoi optimiser ».

### ⚠️ Si stress
- Si la voix tremble : prendre une respiration entre « comme dans les plateformes classiques » et « Le défi ».
- Mots-clés à caser : *jumeau numérique*, *ruleless*, *langage naturel*, *NordPool*.

---

## SECTION 2 — Architecture en 60 secondes (00:40 → 01:40)

### 🎤 Texte préparé
> « L'architecture combine 4 briques. (1) **Home Assistant**, dans un conteneur Docker, qui parle aux appareils réels via REST. (2) **SmartNode**, un service .NET 8 que j'ai étendu pendant le stage : il tourne une boucle MAPE-K — Monitor, Analyze, Plan, Execute, Knowledge — toutes les 60 secondes. (3) Une **chatbox web** que j'ai écrite en HTML/JavaScript, qui dialogue avec un LLM local — Ollama qwen2.5-coder — pour interpréter les phrases de l'utilisateur. (4) Et au cœur du raisonnement, un **moteur d'inférence Apache Jena**, qui applique des règles symboliques sur une ontologie SOSA/SSN décrivant la maison.
>
> Le tout fonctionne **en local**, sans cloud, sans API tierce. Le LLM tourne sur la machine, les données aussi. »

### 🖥️ À l'écran
- Slide 2 : schéma architecture (export `Diagrams/framework_architecture.drawio` en PNG).
- Pointer du curseur sur les 4 blocs au moment où ils sont nommés.

### ⚠️ Si stress
- Ne pas se perdre dans les détails — c'est une vue 60 s, pas la slide MAPE-K détaillée (réservée à la section 6).
- Si on accélère, ne sauter NI le mot « ruleless » NI « MAPE-K ».

---

## SECTION 3 — Lancement SmartNode (01:40 → 02:10)

> *SmartNode est déjà lancé avant la soutenance — on ne fait que le montrer.*

### 🎤 Texte préparé
> « Voici SmartNode en cours d'exécution. La boucle MAPE-K tourne — vous voyez les cycles passer en temps réel : Monitor lit les capteurs Home Assistant, Knowledge applique les règles d'inférence, Plan simule le futur via une FMU Modelica… On va vite voir ce que ça donne côté utilisateur. »

### 🖥️ À l'écran
- Glisser la fenêtre **terminal `dotnet run`** au premier plan pendant 10 s.
- Pointer une ligne `Logic.Mapek.IMapekPlan[0] Running simulation #N` ou `Executed query: SELECT ?...`.
- Repasser au navigateur.

### ⚠️ Si problème
- Si le terminal SmartNode est silencieux (pas de logs) → glisser sur la chatbox directement, dire « SmartNode est lancé en arrière-plan, on va l'interroger ».
- Si le terminal montre une exception en rouge → **ne pas s'attarder**, transition immédiate vers la chatbox.

---

## SECTION 4 — Démo chatbox + Home Assistant (02:10 → 03:40)

> *Le moment fort. 3 phrases qui mettent en évidence : (a) la compréhension naturelle, (b) l'action immédiate, (c) la lecture du monde.*

### 🎤 Texte préparé (transition)
> « Voici la chatbox côté gauche, Home Assistant côté droit. Tout ce qui se passe à gauche, on le voit en temps réel à droite. Trois exemples. »

### Phrase 1 — Lecture d'état
**Taper dans la chatbox** : `quelle est la température du salon ?`

> 🎤 « Première chose : on demande l'état actuel. Le LLM identifie un *query_state*, le SmartNode lit le capteur Home Assistant. Vous voyez la valeur s'afficher : 19,6 degrés, et elle correspond exactement au capteur dans Lovelace à droite. »

🖥️ Pointer le dashboard du chatbox + le sensor `Showcase Living Room Temperature` dans HA.

### Phrase 2 — Ordre direct
**Taper** : `allume la lumière de la cuisine`

> 🎤 « Maintenant un ordre. Le LLM choisit un *call_service* — `light.turn_on` — avec l'entité `light.showcase_kitchen_light`. SmartNode l'exécute, et… »

🖥️ Attendre 1-2 s. **Montrer le toggle qui change dans HA** (Direct Controls — Kitchen passe à ON, l'icône s'allume).

> 🎤 « C'est instantané. Et notez : je n'ai jamais codé en dur le nom *kitchen_light*. SmartNode découvre dynamiquement les entités HA toutes les 30 secondes et les injecte dans le prompt du LLM. »

### Phrase 3 — Réglage numérique
**Taper** : `mets la température à 23 degrés`

> 🎤 « Cas plus subtil : un ordre direct sur une valeur numérique. Le LLM extrait `23`, SmartNode pousse la valeur sur le `input_number.showcase_temperature` de HA. Vous voyez le curseur bouger à droite. »

🖥️ **Pointer le slider HA** qui passe de 19,6 → 23.

> 🎤 « Et je tiens à préciser : ce mode-là est un *ordre direct*. Si je voulais que le système optimise — par exemple maintenir 23 °C la nuit au moins cher — c'est un cas différent qu'on va voir dans 30 secondes. »

### ⚠️ Si problème
- **Si une réponse traîne (> 5 s)** : « Le premier appel charge le modèle Ollama de 4,7 Go en mémoire — c'est le seul lent, après c'est instantané. »
- **Si HA ne réagit pas** : montrer le terminal SmartNode pour exhiber le log `[CHATBOX] Actuate ...` qui prouve que SmartNode a appelé HA. Possible que HA mette 1-2 s à se rafraîchir côté Lovelace.
- **Si tout fail** : passer au [Plan B](#plan-b--si-une-démo-plante).

---

## SECTION 5 — Planification énergétique (03:40 → 05:10)

> *La démo qui « impressionne » — montre l'optimisation et le scheduler.*

### 🎤 Texte préparé (transition)
> « Maintenant, le mode optimisé. Pas un ordre immédiat — une *intention* avec une contrainte. C'est ce qui distingue notre approche des plateformes traditionnelles : le système planifie en fonction des prix de l'électricité. »

### Action — Lancer un plan
**Taper** : `charge la voiture à 100% pour 7h du matin`

> 🎤 « Le LLM identifie `optimize_schedule`. Il extrait les paramètres : 7 h de durée, deadline 7 h du matin, target = `CarCharger`, puissance estimée 11 kW. SmartNode interroge la courbe de prix NordPool sur 24 h, sélectionne les 7 heures les moins chères avant la deadline, et retourne un plan visuel. »

🖥️ **Attendre l'apparition de la barre 24 h colorée** (verte = heures choisies).

> 🎤 « La barre verte, c'est quand le chargeur sera ON. Coût total estimé, et on voit le pourcentage d'économies par rapport à une charge constante. »

### Action — Exécuter le plan
**Cliquer sur le bouton** `▶ Exécuter le plan (mode démo : 1h = 1min)`

> 🎤 « Et là, le scheduler du SmartNode prend le relais. En mode démo, j'ai compressé 24 heures en 24 minutes — chaque "heure" du plan dure une minute réelle. Le bandeau en haut montre l'avancement. »

🖥️ **Pointer le bandeau « ⏱ PLANNINGS »** qui apparaît : `CarCharger running — h=0/24 (0%)`.

> 🎤 « Et côté Home Assistant, vous voyez : `input_boolean.showcase_car_charger` qui s'allume **uniquement** pendant les heures planifiées. Le scheduler appelle l'actuateur HA en temps réel, sans intervention de l'utilisateur. »

🖥️ **Montrer dans HA le toggle CarCharger qui change**.

> 🎤 « En production, on remplace la simulation par les prix NordPool réels et 1 minute par 1 heure. Le pattern reste identique. »

### ⚠️ Si problème
- **Si Ollama renvoie un mauvais plan** (deadline mal extraite) : utiliser le quick-chip `🚗 Charge Tesla nuit` à la place — il a le même intent en fallback keyword.
- **Si le scheduler ne fire pas** : montrer la requête `Invoke-RestMethod http://localhost:8080/api/schedules` dans le terminal pour exhiber le plan en mémoire avec `current_hour` qui avance.

---

## SECTION 6 — MAPE-K + OWL + FMU + Jena (05:10 → 06:10)

> *La couche technique. À adapter selon le profil du jury — plus court si jury non-technique.*

### 🎤 Texte préparé
> « Sous le capot, ce que vous venez de voir mobilise quatre concepts académiques.
>
> **MAPE-K** : Monitor / Analyze / Plan / Execute / Knowledge — c'est le pattern d'auto-adaptation des systèmes autonomes. Notre boucle tourne toutes les 60 secondes, et à chaque cycle elle vérifie si les conditions optimales sont satisfaites. Sinon, elle simule plusieurs combinaisons d'actions et choisit la meilleure selon une fonction *fitness*.
>
> **OWL/SOSA-SSN** : la maison est décrite par une ontologie. Capteurs, actuateurs, propriétés observables, conditions optimales — tout est modélisé en RDF. Ça permet d'ajouter un nouvel équipement sans modifier le code, juste en éditant le `.ttl`.
>
> **FMU/Modelica** : pour la phase Plan, on simule physiquement la pièce — les transferts de chaleur, l'effet du chauffage. On utilise des modèles Functional Mock-up Units générés par OpenModelica. Ça donne au système une **vision du futur** sans agir sur le monde réel.
>
> **Apache Jena** : le moteur d'inférence symbolique. À chaque cycle Knowledge, le SmartNode fork un processus Java qui applique les 459 lignes de règles d'inférence + 549 lignes de règles de vérification sur le modèle d'instance, et produit un modèle inféré qui contient les déductions — par exemple `meta:isViolated true` pour signaler une condition optimale non respectée. »

### 🖥️ À l'écran (option A — slide statique)
- Slide 3 avec les 4 concepts en quadrants + flèches montrant leur interaction.

### 🖥️ À l'écran (option B — montrer les règles Jena en live)
- Si tu as le temps : ouvrir VS Code avec `models-and-rules/inference-rules.rules`, montrer 1 règle pendant 5 s, expliquer brièvement.
- Sinon : se contenter de la slide.

### ⚠️ Si on déborde
- **Sauter cette section entièrement**. La transition naturelle : « Sous le capot, on combine MAPE-K, ontologie OWL et simulation FMU — je peux détailler en questions si vous voulez. »

---

## SECTION 7 — Limites connues (06:10 → 06:40)

> *Honnêteté > marketing. Le jury apprécie la lucidité.*

### 🎤 Texte préparé
> « Trois limites importantes, que j'assume :
>
> **D'abord**, les prix utilisés en démo sont simulés — la courbe est volontairement plate, donc les économies affichées (2 à 4 %) sont sous-estimées. En branchant les vrais prix NordPool, on attend 15 à 25 % d'économies sur la charge d'un véhicule électrique.
>
> **Ensuite**, les plannings ne survivent pas à un redémarrage du SmartNode — ils sont stockés en mémoire vive. Pour une mise en production, il faut sérialiser dans un fichier ou une base.
>
> **Enfin**, le LLM peut occasionnellement halluciner un nom d'entité qui n'existe pas. Aujourd'hui SmartNode laisse passer et l'erreur remonte au call HA. La prochaine étape, c'est valider l'`entity_id` retourné contre le registry avant exécution. »

### 🖥️ À l'écran
- Slide 4 : les 3 limites en bullets.

---

## SECTION 8 — Conclusion & perspectives (06:40 → 07:00)

### 🎤 Texte préparé
> « En résumé, on a un jumeau numérique fonctionnel qui combine raisonnement symbolique — Jena + OWL — et numérique — FMU + LLM —, piloté en langage naturel, sans cloud.
>
> Trois pistes pour la suite : intégrer un compteur intelligent réel, scaler à plusieurs foyers pour de l'effacement coordonné, et mesurer empiriquement l'écart entre la planification et la consommation observée.
>
> Merci. »

### 🖥️ À l'écran
- Slide 5 : conclusion + perspectives.
- Laisser la slide affichée pendant les questions.

---

## Plan B — si une démo plante

| Panne | Symptôme | Réaction immédiate |
|---|---|---|
| Chatbox ne répond plus | Pastille rouge ou pas de réponse | « Je vais redémarrer le service en arrière-plan » → ne PAS le faire en live, passer au point suivant |
| HA ne réagit pas | Toggle ne bouge pas dans Lovelace | Pointer le terminal SmartNode pour montrer le log `[CHATBOX] Actuate ...`, dire « la commande est partie, le rendu HA peut traîner » |
| Ollama timeout | Pas de réponse > 10 s | Le fallback keyword s'active automatiquement → continuer comme si de rien n'était |
| `dotnet run` crash | Stack trace rouge | Ouvrir [DEMO_SETUP.md](DEMO_SETUP.md) section 4.6 mentalement → en pratique, dire « je vais montrer la suite via les slides », passer en slides uniquement |
| Tout est cassé | Catastrophe | Passer immédiatement aux **slides** + capture d'écran de la démo enregistrée la veille (toujours avoir une vidéo de backup MP4 prête) |

> **Règle d'or** : ne JAMAIS rester bloqué plus de 10 secondes sur une erreur. Continuer, expliquer le concept à la voix, revenir sur la techno seulement si elle revient seule.

### Vidéo de backup
- Enregistrer J-1 une vidéo MP4 de **toute la démo** (sections 4 + 5) avec OBS Studio ou Xbox Game Bar.
- La garder dans un onglet ouvert : `file:///C:/dev/demo-backup.mp4`.
- En cas de panne sévère : *« Je vais vous montrer le déroulé via la captation que j'ai préparée. »* — bascule sur la vidéo, continue le script à la voix.

---

## Q&A préparée

> Questions probables du jury et réponses calibrées (1-2 phrases max chacune).

### Sur l'architecture
- **« Pourquoi ne pas utiliser un cloud LLM type GPT-4 ? »**
  > « Pour la latence, la confidentialité — les données de la maison ne sortent pas — et le coût zéro à l'usage. Ollama tourne sur la machine, qwen2.5-coder fait 4,7 Go et répond en 1-2 secondes. »
- **« Pourquoi MAPE-K et pas une simple boucle de contrôle ? »**
  > « MAPE-K sépare proprement la perception, le raisonnement et l'action. Ça nous permet de plug différentes méthodes de planification — simulation FMU aujourd'hui, RL demain — sans toucher au reste. »
- **« Pourquoi une ontologie OWL et pas une simple base SQL ? »**
  > « Parce qu'on raisonne sur les *relations* — un capteur observe une propriété, une action affecte une propriété — et pas juste sur des valeurs. Avec Jena, on peut déduire qu'une OptimalCondition est violée *avant même* qu'elle le soit en réel. »

### Sur la techno
- **« Et si je rajoute 200 entités HA ? »**
  > « Le registry refresh dynamiquement et injecte la liste dans le prompt. Aujourd'hui ça tient en contexte. Au-delà de 500, il faudrait filtrer par domaine ou pré-classer la requête. »
- **« Comment vous gérez les conflits — deux ordres opposés ? »**
  > « Pour les ordres directs immédiats, le dernier gagne. Pour les plans optimisés, le scheduler conserve un seul plan actif par target — un nouveau plan annule l'ancien. »
- **« Sécurité du token Home Assistant ? »**
  > « Variable d'environnement, jamais committée, scope long-lived révocable depuis HA. Pour la prod, on passerait à OAuth 2.0. »

### Sur la valeur
- **« Quel gain réel pour l'utilisateur final ? »**
  > « Sur la charge d'un VE en heures creuses NordPool, 15 à 25 % d'économies. Sur le chauffage avec planification J+1, on tablait sur 8-12 % d'après la littérature, à valider empiriquement. »
- **« Pourquoi *ruleless* plutôt qu'un système expert ? »**
  > « Parce que les règles "si X alors Y" ne tiennent pas la route sur 300 entités hétérogènes. Notre système raisonne sur le modèle, pas sur des recettes câblées. »

### Sur les limites
- **« Que se passe-t-il si Home Assistant tombe ? »**
  > « SmartNode catche l'exception, l'API HTTP reste vivante pour le chatbox, le MAPE-K loop s'arrête. Au retour de HA, il faut redémarrer SmartNode — ou ajouter du retry, ce qui est dans la roadmap. »
- **« Validation expérimentale ? »**
  > « C'est la prochaine étape — instrumenter une vraie maison sur 2-3 semaines, comparer planification vs consommation observée. Demande un compteur smart-meter, pas dans le périmètre du stage. »

---

## Phrases-clés à placer (mémo)

À glisser au moins une fois pendant la démo :
- ✅ **« ruleless »** (le mot-clé du projet)
- ✅ **« en local, sans cloud »** (différenciateur fort)
- ✅ **« MAPE-K »** (l'ancrage académique)
- ✅ **« ontologie SOSA/SSN »** (la rigueur sémantique)
- ✅ **« registry dynamique »** (le mécanisme qui scale)
- ✅ **« mode démo : 1 heure = 1 minute »** (l'astuce qui rend la démo regardable)

---

*Bonne soutenance ! 🚀*

*Dernière mise à jour : avril 2026.*
