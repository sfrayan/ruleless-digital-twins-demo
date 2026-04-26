using Logic.CaseRepository;
using Logic.FactoryInterface;
using Logic.Mapek;
using Logic.Models.DatabaseModels;
using Logic.Models.MapekModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.CommandLine;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fitness;

[assembly: InternalsVisibleTo("TestProject")]

namespace SmartNode
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            RootCommand rootCommand = new();
            Option<string> fileNameArg = new("--appsettings")
            {
                Description = "Which appsettings file to use."
            };
            rootCommand.Add(fileNameArg);
            Option<string> baseDirName = new("--basedir")
            {
                Description = "The base directory for models etc. Used as prefix for all relative paths in `appsettings`."
            };
            rootCommand.Add(baseDirName);

            ParseResult parseResult = rootCommand.Parse(args);
            string? settingsFile = parseResult.GetValue(fileNameArg);
            string? baseDir = parseResult.GetValue(baseDirName);

            var appSettings = settingsFile is not null ? Path.Combine("Properties", settingsFile) : Path.Combine("Properties", $"appsettings.json");

            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile(appSettings);

            var filepathArguments = builder.Configuration.GetSection("FilepathArguments").Get<FilepathArguments>();
            var coordinatorSettings = builder.Configuration.GetSection("CoordinatorSettings").Get<CoordinatorSettings>();
            var databaseSettings = builder.Configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();

            string? rootDirectory;
            try {
                var location = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
                // Dispatch between binary release (e.g. in Docker) and in-IDE/workspace.
                rootDirectory = baseDir ?? location!.Parent!.Parent!.Parent!.Parent!.Parent!.FullName;
            } catch (NullReferenceException) {
                rootDirectory = ""; // Not the most elegant solution, but it'll do.
            }

            // TODO: we can use reflection for this.
            // Fix full paths.
            filepathArguments!.OntologyFilepath = Path.GetFullPath(Path.Combine(rootDirectory, filepathArguments.OntologyFilepath));
            filepathArguments.FmuDirectory = Path.GetFullPath(Path.Combine(rootDirectory, filepathArguments.FmuDirectory));
            filepathArguments.DataDirectory = Path.GetFullPath(Path.Combine(rootDirectory, filepathArguments.DataDirectory));
            filepathArguments.InferenceRulesFilepath = Path.GetFullPath(Path.Combine(rootDirectory, filepathArguments.InferenceRulesFilepath));
            filepathArguments.InstanceModelFilepath = Path.GetFullPath(Path.Combine(rootDirectory, filepathArguments.InstanceModelFilepath));
            filepathArguments.InferredModelFilepath = Path.GetFullPath(Path.Combine(rootDirectory, filepathArguments.InferredModelFilepath));
            filepathArguments.InferenceEngineFilepath = Path.GetFullPath(Path.Combine(rootDirectory, filepathArguments.InferenceEngineFilepath));

            // Register services here.
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole(options => options.TimestampFormat = "HH:mm:ss ");
            });
            builder.Services.AddSingleton(filepathArguments);
            builder.Services.AddSingleton(coordinatorSettings!);
            builder.Services.AddSingleton(databaseSettings!);
            // Register a factory to allow for dynamic constructor argument passing through DI.
            builder.Services.AddSingleton<IMongoClient, MongoClient>(serviceProvider => new MongoClient(databaseSettings!.ConnectionString));
            builder.Services.AddSingleton<ICaseRepository, CaseRepository>(serviceProvider => new CaseRepository(serviceProvider));
            builder.Services.AddSingleton<IFactory, Factory>(serviceProvider => new Factory(coordinatorSettings!.Environment));
            builder.Services.AddSingleton<IMapekMonitor, MapekMonitor>(serviceProvider => new MapekMonitor(serviceProvider));
            builder.Services.AddSingleton<IMapekPlan, MapekPlan>(serviceProvider => {
                MapekPlan plan = coordinatorSettings!.UseEuclid ? new EuclidMapekPlan(serviceProvider) : new MapekPlan(serviceProvider);
                
                var fitnessConfig = builder.Configuration.GetSection("FitnessSettings");
                var calculateFitness = fitnessConfig.GetValue<bool>("CalculateFitness");
                if (calculateFitness) {
                    var energyProp = fitnessConfig.GetValue<string>("EnergyProperty");
                    var priceProp = fitnessConfig.GetValue<string>("PriceProperty");
                    var accProp = fitnessConfig.GetValue<string>("AccumulatedProperty");
                    var tempPropStr = "http://www.semanticweb.org/rayan/ontologies/2025/ha/OfficeTemperature";

                    // 1. Base Cost (Energy * Price)
                    var f_energy = new FProp(energyProp);
                    var f_price = new FProp(priceProp);
                    var f_prod = new FBinOpArith(f_energy, f_price, (x, y) => (double)x * (double)y, name: accProp.Replace("Accumulated", ""));
                    var f_acc_cost = new FAcc<double>(f_prod, name: accProp + "_BaseCost");
                    
                    // 2. Penalty for deviating from target temperature
                    var f_temp = new FProp(tempPropStr);
                    var f_penalty = new FTargetPenalty(f_temp.Prop, name: "TemperaturePenalty");
                    var f_acc_penalty = new FAcc<double>(f_penalty, name: "AccumulatedPenalty");

                    // 3. Total Fitness = Cost + Penalty (named accProp so MapekPlan optimization finds it)
                    var f_total = new FBinOpArith(f_acc_cost, f_acc_penalty, (x, y) => (double)x + (double)y, name: accProp);

                    plan.FitnessOps = [ f_total ];
                }
                
                return plan;
            });
            builder.Services.AddSingleton<IBangBangPlanner, BangBangPlanner>(serviceProvider => new BangBangPlanner(serviceProvider));
            builder.Services.AddSingleton<IMapekExecute, MapekExecute>(serviceProvider => new MapekExecute(serviceProvider));
            builder.Services.AddSingleton<IMapekKnowledge, MapekKnowledge>(serviceProvider => new MapekKnowledge(serviceProvider));
            builder.Services.AddSingleton<IMapekManager, MapekManager>(serviceprovider => new MapekManager(serviceprovider));
            builder.Services.AddSingleton<HomeAssistantRegistry>();

            using var host = builder.Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            // Get an instance of the MAPE-K manager.
            var mapekManager = host.Services.GetRequiredService<IMapekManager>();
            var haRegistry = host.Services.GetRequiredService<HomeAssistantRegistry>();
            haRegistry.Start();

            // Start internal API
            _ = Task.Run(async () => {
                try {
                    var listener = new System.Net.HttpListener();
                    listener.Prefixes.Add("http://localhost:8080/");
                    listener.Start();
                    logger.LogInformation("Internal API listening on http://localhost:8080/");
                    while (true) {
                        var context = await listener.GetContextAsync();
                        // Dispatch each request on a background task so a slow handler
                        // (e.g. Ollama on first load) cannot block subsequent requests.
                        _ = Task.Run(async () => {
                        try {
                        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                        if (context.Request.HttpMethod == "OPTIONS") {
                            context.Response.StatusCode = 200;
                            context.Response.Close();
                            return;
                        }

                        if (context.Request.Url!.AbsolutePath == "/api/price") {
                            try {
                                var factory = host.Services.GetRequiredService<IFactory>();
                                // Price sensor URI differs per environment (roomM370 vs homeassistant).
                                var env = coordinatorSettings!.Environment;
                                string priceSensorUri, priceProcUri;
                                if (env == "homeassistant") {
                                    priceSensorUri = "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceSensor";
                                    priceProcUri = "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceProcedure";
                                } else {
                                    priceSensorUri = "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceSensor";
                                    priceProcUri = "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceProcedure";
                                }
                                var sensor = factory.GetSensorImplementation(priceSensorUri, priceProcUri);

                                var prices = new System.Collections.Generic.List<double>();
                                for(int i=0; i<24; i++) {
                                    var p = await sensor.ObservePropertyValue(i);
                                    prices.Add(Convert.ToDouble(p));
                                }
                                var json = System.Text.Json.JsonSerializer.Serialize(new { prices });
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/entities") {
                            try {
                                var factory = host.Services.GetRequiredService<IFactory>();
                                var sensors = factory.ListSensorKeys()
                                    .Select(k => new { uri = k.SensorName, procedure = k.ProcedureName })
                                    .ToList();
                                var actuators = factory.ListActuatorKeys().ToList();
                                var json = System.Text.Json.JsonSerializer.Serialize(new { sensors, actuators });
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/state") {
                            try {
                                var factory = host.Services.GetRequiredService<IFactory>();
                                var readings = new System.Collections.Generic.List<object>();
                                foreach (var (sensorUri, procUri) in factory.ListSensorKeys()) {
                                    try {
                                        var sensor = factory.GetSensorImplementation(sensorUri, procUri);
                                        // Pass 0 as default timestep (FakepoolSensor needs it, HomeAssistantSensor ignores it).
                                        var value = await sensor.ObservePropertyValue(0);
                                        readings.Add(new { uri = sensorUri, procedure = procUri, value = value?.ToString() ?? "null", ok = true });
                                    } catch (Exception e) {
                                        readings.Add(new { uri = sensorUri, procedure = procUri, value = (string?)null, ok = false, error = e.Message });
                                    }
                                }
                                var json = System.Text.Json.JsonSerializer.Serialize(new { readings });
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/actuate" && context.Request.HttpMethod == "POST") {
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var body = await reader.ReadToEndAsync();
                                using var doc = System.Text.Json.JsonDocument.Parse(body);
                                var uri = doc.RootElement.GetProperty("uri").GetString();
                                // Accept any JSON Number (int or float) — InputNumber needs full precision.
                                var state = doc.RootElement.GetProperty("state").GetDouble();
                                if (uri == null) {
                                    context.Response.StatusCode = 400;
                                } else {
                                    var factory = host.Services.GetRequiredService<IFactory>();
                                    var actuator = factory.GetActuatorImplementation(uri);
                                    await actuator.Actuate(state);
                                    logger.LogInformation($"[CHATBOX] Actuate {uri} => {state}");
                                    var json = System.Text.Json.JsonSerializer.Serialize(new { ok = true, uri, state });
                                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                                }
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/ha/states") {
                            // Proxy to HA /api/states filtered to showcase entities (domain-grouped) for chatbox display.
                            try {
                                var token = Environment.GetEnvironmentVariable("TOKEN_HA") ?? string.Empty;
                                using var http = new HttpClient { BaseAddress = new Uri("http://localhost:8123/"), Timeout = TimeSpan.FromSeconds(5) };
                                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                                var raw = await http.GetStringAsync("api/states");
                                var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/call_service" && context.Request.HttpMethod == "POST") {
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var body = await reader.ReadToEndAsync();
                                using var doc = System.Text.Json.JsonDocument.Parse(body);
                                var domain = doc.RootElement.GetProperty("domain").GetString();
                                var service = doc.RootElement.GetProperty("service").GetString();
                                
                                var token = Environment.GetEnvironmentVariable("TOKEN_HA") ?? string.Empty;
                                using var http = new HttpClient { BaseAddress = new Uri("http://localhost:8123/"), Timeout = TimeSpan.FromSeconds(5) };
                                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                                var reqBody = "{}";
                                if (doc.RootElement.TryGetProperty("data", out var dataProp)) {
                                    reqBody = dataProp.GetRawText();
                                }
                                
                                var res = await http.PostAsync($"api/services/{domain}/{service}", new StringContent(reqBody, System.Text.Encoding.UTF8, "application/json"));
                                var resContent = await res.Content.ReadAsStringAsync();
                                
                                var bytes = System.Text.Encoding.UTF8.GetBytes(resContent);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/entities_full") {
                            try {
                                var registry = host.Services.GetRequiredService<HomeAssistantRegistry>();
                                var list = registry.GetAll();
                                var json = System.Text.Json.JsonSerializer.Serialize(list);
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/nlu" && context.Request.HttpMethod == "POST") {
                            // Proxy user message through Ollama (qwen2.5-coder) to get a structured intent.
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var inBody = await reader.ReadToEndAsync();
                                using var inDoc = System.Text.Json.JsonDocument.Parse(inBody);
                                var userMessage = inDoc.RootElement.GetProperty("message").GetString() ?? "";

                                var registry = host.Services.GetRequiredService<HomeAssistantRegistry>();
                                var entitiesList = registry.SummaryForPrompt();

                                var systemPrompt = $@"You are an NLU module for a smart-home assistant. Given a user message (in French or English), reply ONLY with a JSON object (no prose, no code fences) matching this schema:
{{
  ""intent"": one of [""greeting"", ""capabilities"", ""smalltalk"", ""price_current"", ""price_cheapest"", ""price_expensive"", ""price_average"", ""set_temperature"", ""call_service"", ""query_state"", ""optimize_schedule"", ""out_of_scope"", ""unknown""],
  ""domain"": null | string (for call_service, e.g. ""light"", ""scene"", ""cover"", ""climate""),
  ""service"": null | string (for call_service, e.g. ""turn_on"", ""turn_off"", ""set_temperature""),
  ""entity_id"": null | string (for call_service or query_state, must be exact entity ID),
  ""data"": null | object (for call_service, e.g. {{""temperature"": 22}}),
  ""value"": null | number (for direct orders or legacy set_temperature),
  ""duration_hours"": null | integer (required for optimize_schedule),
  ""deadline_hour"": null | integer 0-24 (required for optimize_schedule),
  ""budget_max"": null | number in NOK (optional),
  ""power_kw"": null | number in kW,
  ""target"": null | string (for optimize_schedule),
  ""answer"": a short French answer for the user (1-2 sentences)
}}

Here are the AVAILABLE entities in the home:
{entitiesList}

Rules:
- To control ANY entity discovered above (lights, scenes, switches, media players, scripts), use intent=""call_service"". Set the correct domain, service, and entity_id. Put any arguments in data.
  * e.g. ""allume la cuisine"" -> intent=""call_service"", domain=""light"", service=""turn_on"", entity_id=""light.showcase_kitchen_light"", data={{""entity_id"": ""light.showcase_kitchen_light""}}
  * e.g. ""desactive le mode cinema"" -> intent=""call_service"", domain=""input_boolean"", service=""turn_off"", entity_id=""input_boolean.showcase_movie_mode"", data={{""entity_id"": ""input_boolean.showcase_movie_mode""}}
- IMPORTANT: To turn ON or OFF a mode (like Movie Mode or Sleep Mode), ALWAYS prefer using the 'input_boolean' entity rather than 'scene'. Scenes cannot be turned off.
- If the user implies a macro-action like leaving the home (""je pars"", ""au revoir"", ""je vais bosser"") or going to sleep, LOOK for a relevant 'script' (e.g. script.showcase_leave_home, script.showcase_good_night) or 'scene' and call it!
- For ""mets la temperature a 21 degres"" (DIRECT ORDER) -> you can still use intent=""set_temperature"", value=21, OR use call_service on the climate entity if available.
- For PLANNED/OPTIMIZED tasks with a deadline and cost concern like ""charge ma voiture a 100% pour 7h en heures creuses"" -> intent=""optimize_schedule"".
- For questions about current states (temperature, humidity, is a door open) -> intent=""query_state"", and set ""entity_id"" to the entity they are asking about.
- For anything outside smart home / energy -> intent=""out_of_scope"".";

                                var payload = new {
                                    model = "qwen2.5-coder:7b",
                                    messages = new[] {
                                        new { role = "system", content = systemPrompt },
                                        new { role = "user", content = userMessage }
                                    },
                                    stream = false,
                                    format = "json"
                                };
                                var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
                                using var ollama = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
                                var resp = await ollama.PostAsync("http://localhost:11434/api/chat",
                                    new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json"));
                                var respBody = await resp.Content.ReadAsStringAsync();
                                using var respDoc = System.Text.Json.JsonDocument.Parse(respBody);
                                var content = respDoc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "{}";

                                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/target_temp" && context.Request.HttpMethod == "POST") {
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var body = await reader.ReadToEndAsync();
                                var data = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, double>>(body);

                                if (data != null && data.ContainsKey("temperature")) {
                                    FTargetPenalty.TargetValue = data["temperature"];
                                    logger.LogInformation($"[CHATBOX] Dynamic constraint received: Target Temperature set to {FTargetPenalty.TargetValue}°C");
                                    context.Response.StatusCode = 200;
                                } else {
                                    context.Response.StatusCode = 400;
                                }
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/execute_schedule" && context.Request.HttpMethod == "POST") {
                            // Register a 24-cell schedule for async actuation.
                            // Body: { target: URI, target_name: str, hours_on: bool[24], time_unit_seconds: number }
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var body = await reader.ReadToEndAsync();
                                using var doc = System.Text.Json.JsonDocument.Parse(body);
                                var targetUri = doc.RootElement.GetProperty("target").GetString()!;
                                var targetName = doc.RootElement.TryGetProperty("target_name", out var tn) && tn.ValueKind == System.Text.Json.JsonValueKind.String
                                    ? tn.GetString()! : targetUri;
                                var timeUnit = doc.RootElement.TryGetProperty("time_unit_seconds", out var tu) && tu.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? tu.GetDouble() : 3600.0;
                                var hoursOnEl = doc.RootElement.GetProperty("hours_on");
                                var hoursOn = new bool[24];
                                for (int i = 0; i < 24 && i < hoursOnEl.GetArrayLength(); i++) {
                                    hoursOn[i] = hoursOnEl[i].GetBoolean();
                                }

                                var factory = host.Services.GetRequiredService<IFactory>();
                                var id = ScheduleManager.Start(factory, logger, targetUri, targetName, hoursOn, timeUnit);

                                var json = System.Text.Json.JsonSerializer.Serialize(new { ok = true, schedule_id = id, time_unit_seconds = timeUnit });
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/schedules" && context.Request.HttpMethod == "GET") {
                            try {
                                var list = ScheduleManager.List().Select(s => new {
                                    id = s.Id,
                                    target = s.TargetName,
                                    target_uri = s.TargetUri,
                                    status = s.Status,
                                    on_hours = s.OnHours,
                                    current_hour = s.CurrentHour,
                                    time_unit_seconds = s.TimeUnitSeconds,
                                    started_at = s.StartedAt.ToString("o"),
                                    last_error = s.LastError
                                }).ToList();
                                var json = System.Text.Json.JsonSerializer.Serialize(new { schedules = list });
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/cancel_schedule" && context.Request.HttpMethod == "POST") {
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var body = await reader.ReadToEndAsync();
                                using var doc = System.Text.Json.JsonDocument.Parse(body);
                                var id = doc.RootElement.GetProperty("id").GetString()!;
                                var ok = ScheduleManager.Cancel(id);
                                var json = System.Text.Json.JsonSerializer.Serialize(new { ok, id });
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/optimize" && context.Request.HttpMethod == "POST") {
                            // Plan: pick the N cheapest hours before deadline for a device of given power.
                            // Returns a 24h schedule + cost + savings vs constant-price baseline.
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var body = await reader.ReadToEndAsync();
                                using var doc = System.Text.Json.JsonDocument.Parse(body);

                                int duration = doc.RootElement.TryGetProperty("duration_hours", out var durEl) && durEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? durEl.GetInt32() : 4;
                                int deadlineHour = doc.RootElement.TryGetProperty("deadline_hour", out var dhEl) && dhEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? dhEl.GetInt32() : 24;
                                double? budget = doc.RootElement.TryGetProperty("budget_max", out var bmEl) && bmEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? bmEl.GetDouble() : (double?)null;
                                double powerKw = doc.RootElement.TryGetProperty("power_kw", out var pkEl) && pkEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? pkEl.GetDouble() : 1.0;
                                string? target = doc.RootElement.TryGetProperty("target", out var tgEl) && tgEl.ValueKind == System.Text.Json.JsonValueKind.String
                                    ? tgEl.GetString() : null;

                                // Fetch 24h price forecast (same wiring as /api/price).
                                var factory = host.Services.GetRequiredService<IFactory>();
                                var env = coordinatorSettings!.Environment;
                                string priceSensorUri, priceProcUri;
                                if (env == "homeassistant") {
                                    priceSensorUri = "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceSensor";
                                    priceProcUri = "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceProcedure";
                                } else {
                                    priceSensorUri = "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceSensor";
                                    priceProcUri = "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceProcedure";
                                }
                                var sensor = factory.GetSensorImplementation(priceSensorUri, priceProcUri);
                                var prices = new double[24];
                                for (int i = 0; i < 24; i++) prices[i] = Convert.ToDouble(await sensor.ObservePropertyValue(i));

                                int effectiveDeadline = Math.Clamp(deadlineHour, 1, 24);
                                int needed = Math.Clamp(duration, 1, effectiveDeadline);

                                var chosen = Enumerable.Range(0, effectiveDeadline)
                                    .OrderBy(h => prices[h])
                                    .Take(needed)
                                    .OrderBy(h => h)
                                    .ToList();

                                double costPerKwhSum = chosen.Sum(h => prices[h]);
                                double totalCost = costPerKwhSum * powerKw;
                                double avgBaseline = prices.Average();
                                double baselineCost = avgBaseline * needed * powerKw;
                                double savingsPct = baselineCost > 0 ? (1 - totalCost / baselineCost) * 100 : 0;
                                double avgChosen = chosen.Count > 0 ? chosen.Average(h => prices[h]) : 0;

                                var schedule = new System.Collections.Generic.List<object>(24);
                                for (int h = 0; h < 24; h++) {
                                    schedule.Add(new {
                                        hour = h,
                                        price = Math.Round(prices[h], 2),
                                        on = chosen.Contains(h),
                                        before_deadline = h < effectiveDeadline
                                    });
                                }

                                var result = new {
                                    target,
                                    chosen_hours = chosen,
                                    duration_hours = needed,
                                    deadline_hour = effectiveDeadline,
                                    power_kw = powerKw,
                                    total_cost_nok = Math.Round(totalCost, 2),
                                    avg_price = Math.Round(avgBaseline, 2),
                                    avg_price_chosen = Math.Round(avgChosen, 2),
                                    savings_percent = (int)Math.Round(savingsPct),
                                    budget_max = budget,
                                    within_budget = budget == null || totalCost <= budget.Value,
                                    schedule
                                };

                                var json = System.Text.Json.JsonSerializer.Serialize(result);
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                                logger.LogInformation($"[CHATBOX] Optimize: target={target}, {needed}h before {effectiveDeadline}h, chosen=[{string.Join(",", chosen)}], cost={totalCost:F2} NOK ({savingsPct:F0}% saved)");
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        }
                        context.Response.Close();
                        } catch (Exception handlerEx) {
                            logger.LogError(handlerEx, "Request handler crashed");
                            try {
                                context.Response.StatusCode = 500;
                                context.Response.Close();
                            } catch { /* response may already be closed */ }
                        }
                        });
                    }
                } catch (Exception ex) {
                    logger.LogError(ex, "Failed to start internal API");
                }
            });

            // Start the loop. MAPE-K needs live HA sensors; if HA is down we log and
            // keep the HTTP API alive so the chatbox remains usable for non-MAPE-K work.
            try
            {
                await mapekManager.StartLoop();
                logger.LogInformation("MAPE-K ended normally.");
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "MAPE-K loop failed — HTTP API continues. Fix HA then restart SmartNode to recover MAPE-K.");
            }

            // Keep the process alive so the background HTTP listener (chatbox API) stays up.
            await Task.Delay(Timeout.Infinite);
            return 0;
        }
    }
}
