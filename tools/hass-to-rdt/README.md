# Home Assistant → RDT exporter

CLI tool that connects to a [Home Assistant](https://www.home-assistant.io)
instance and exports its static structure (devices, entities, locations) as a
Turtle/OWL file compatible with the **ruleless-digital-twins** SmartNode
ontology (SOSA/SSN + `rdt:` namespace).

This is a slimmed-down, RDT-only fork of the upstream
[HASS-to-OWL-exporter](https://github.com/Edkamb/HASS-to-OWL-exporter)
maintained by HVL (Volker Stolz, Eduard Kamburjan, Fernando Macías,
Adam Cheng). The Flask/Celery web frontend has been left out — only the
command-line backend is shipped here, since that is what the demo uses.

## Files

- `hacvt_rdt.py` — RDT/SOSA backend (this is the entry point).
- `hacvt.py` — Base class shared with the upstream SAREF backend; imported
  by `hacvt_rdt`.
- `ConfigSource.py` — CLI/config helper used by both modules.
- `requirements-cli.txt` — Minimal PyPI deps for the CLI path.

## Setup

```bash
cd tools/hass-to-rdt
python -m venv .venv
.venv\Scripts\activate          # Windows
# source .venv/bin/activate     # Linux/macOS
pip install -r requirements-cli.txt
```

Note: `homeassistant` itself is installed as a regular PyPI package and
pulls in most transitive deps (`voluptuous`, `aiohttp`, `awesomeversion`,
…). Install can take a couple of minutes the first time.

## Usage

You need a Home Assistant URL and a long-lived access token
([how to create one](https://developers.home-assistant.io/docs/auth_api/#long-lived-access-token)).
Store the token in an environment variable — **do not pass the literal
token on the command line**.

```powershell
$env:HA_TOKEN = "eyJhbGc..."
$env:HA_URL   = "http://localhost:8123/api/"
$env:RDT_NS   = "http://www.semanticweb.org/rayan/ontologies/2025/ha/"

python hacvt_rdt.py $env:HA_URL HA_TOKEN `
    --namespace $env:RDT_NS `
    --out ../../models-and-rules/homeassistant-instance.ttl
```

The second positional argument is the **name** of the env var that
holds the token, not the token itself.

## Options

```
usage: hacvt_rdt.py [-h] [-d [DEBUG]] [-n NAMESPACE] [-o OUT]
                    [-p [platform* ...]] [-m IP] [-c ca.crt]
                    url TOKENVAR

positional arguments:
  url               HA API root, e.g. http://localhost:8123/api/.
  TOKENVAR          Name of the env var holding the long-lived token.

options:
  -n, --namespace   RDF namespace for individuals (default RDT_NS).
  -o, --out         Output filename (default ha.ttl).
  -p, --privacy     Enable privacy filter; with no list, sensible default.
  -m, --mount       ForcedIPHTTPSAdapter override IP for internal HA URLs.
  -c, --certificate Path to CA cert; "None" disables validation.
```

## Output

The generated `.ttl` plugs straight into the SmartNode pipeline — copy or
symlink it into `models-and-rules/homeassistant-instance.ttl`. The
inference engine (`inference-engine/ruleless-digital-twins-inference-engine.jar`)
will pick it up on the next MAPE-K cycle.

## Differences vs. upstream `hacvt.py`

- Uses **SOSA** (`http://www.w3.org/ns/sosa/`) and **SSN** namespaces
  instead of SAREF / S4BLDG.
- Maps HA domains → `sosa:Sensor` / `sosa:Actuator` / `sosa:Platform`.
- Attaches `rdt:hasIdentifier` carrying the raw HA `entity_id` (so
  SmartNode can call back to HA without a separate mapping table).
- Queries `/api/services` to enumerate possible actuator states and
  emits `rdt:hasActuatorState` triples.
- Uses `sosa:ObservableProperty` for sensor measurement targets.
- No SAREF / S4BLDG / `homeassistantcore.rdf` side-effects.

## License

Inherits the upstream license from
[HASS-to-OWL-exporter](https://github.com/Edkamb/HASS-to-OWL-exporter).
See the repo root `LICENSE.md` for the RDT project license. If you
redistribute, keep the credit lines above.
