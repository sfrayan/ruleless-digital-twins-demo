using Logic.CaseRepository;
using Logic.FactoryInterface;
using Logic.Mapek;
using Logic.Mapek.Proactive;
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

            // Resolve appsettings.json relative to the assembly so `dotnet run --project ...`
            // works from any working directory.
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var appSettings = Path.Combine(assemblyDir, "Properties", settingsFile ?? "appsettings.json");

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
            // Proactive arm (V1, consultative): MAPE-K reads the Nord Pool forecast each cycle
            // and exposes a peak/cheap-window advisory. The advisor never mutates planning state.
            builder.Services.AddSingleton<IPriceForecastProvider, NordPoolPriceForecastAdapter>();
            builder.Services.AddSingleton<IProactiveAdvisor, ProactiveAdvisor>();

            using var host = builder.Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            // Get an instance of the MAPE-K manager.
            var mapekManager = host.Services.GetRequiredService<IMapekManager>();
            var haRegistry = host.Services.GetRequiredService<HomeAssistantRegistry>();
            haRegistry.Start();

            // Restore schedule history from previous run.
            ScheduleManager.Configure(filepathArguments.DataDirectory);
            ScheduleManager.Load();

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
                                var env = coordinatorSettings!.Environment;
                                string? haJson = null;

                                // env=homeassistant: try the live Nord Pool aggregate sensors first.
                                // On any failure (HA down, integration absent, parse error), fall through
                                // to the legacy factory-sensor path so the chatbox always gets a payload.
                                if (env == "homeassistant") {
                                    try {
                                        var token = Environment.GetEnvironmentVariable("TOKEN_HA") ?? string.Empty;
                                        var area = NordPoolForecastProvider.GetArea().ToLowerInvariant();
                                        using var http = new HttpClient { BaseAddress = new Uri(NordPoolForecastProvider.GetHaUrl()), Timeout = TimeSpan.FromSeconds(5) };
                                        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                                        string prefix = $"sensor.nord_pool_{area}_";
                                        string[] names = new[] {
                                            "current_price", "next_price", "previous_price",
                                            "lowest_price", "highest_price", "daily_average",
                                            "peak_average", "off_peak_1_average", "off_peak_2_average"
                                        };
                                        var responses = await Task.WhenAll(names.Select(n => http.GetAsync($"api/states/{prefix}{n}")));
                                        if (responses.All(r => r.IsSuccessStatusCode)) {
                                            var bodies = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
                                            var byName = new Dictionary<string, string>();
                                            for (int i = 0; i < names.Length; i++) byName[names[i]] = bodies[i];

                                            double GetState(string n) {
                                                using var d = System.Text.Json.JsonDocument.Parse(byName[n]);
                                                var s = d.RootElement.GetProperty("state").GetString() ?? "0";
                                                return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                                            }
                                            string GetUnit(string n) {
                                                using var d = System.Text.Json.JsonDocument.Parse(byName[n]);
                                                if (d.RootElement.TryGetProperty("attributes", out var a) &&
                                                    a.TryGetProperty("unit_of_measurement", out var u)) return u.GetString() ?? "";
                                                return "";
                                            }
                                            (string from, string until) GetOffPeakTimes(string n) {
                                                using var d = System.Text.Json.JsonDocument.Parse(byName[n]);
                                                if (d.RootElement.TryGetProperty("attributes", out var a)) {
                                                    var f = a.TryGetProperty("time_from", out var tf) ? (tf.GetString() ?? "") : "";
                                                    var u = a.TryGetProperty("time_until", out var tu) ? (tu.GetString() ?? "") : "";
                                                    return (f, u);
                                                }
                                                return ("", "");
                                            }

                                            var current = GetState("current_price");
                                            var op1 = GetOffPeakTimes("off_peak_1_average");
                                            var op2 = GetOffPeakTimes("off_peak_2_average");

                                            // Real future forecast via nordpool.get_prices_for_date (auto-discovered config_entry).
                                            var forecast = await NordPoolForecastProvider.GetForecastAsync(token, logger);
                                            object forecastBlock;
                                            double[] prices;
                                            string? warning = null;

                                            if (forecast.ForecastAvailable) {
                                                prices = forecast.Slots.Select(s => Math.Round(s.Price, 4)).ToArray();
                                                forecastBlock = new {
                                                    forecastAvailable = true,
                                                    forecastSource = forecast.Source,
                                                    configEntryDiscovered = forecast.ConfigEntryDiscovered,
                                                    area = forecast.Area,
                                                    currency = forecast.Currency,
                                                    timezone = forecast.Timezone,
                                                    slots = forecast.Slots.Select(s => new {
                                                        start = s.Start.ToString("o"),
                                                        end = s.End.ToString("o"),
                                                        hourLocal = s.HourLocal,
                                                        price = Math.Round(s.Price, 4)
                                                    }).ToArray()
                                                };
                                            } else {
                                                prices = new[] { current };
                                                warning = forecast.Warning ?? "Nord Pool future price forecast unavailable";
                                                forecastBlock = new {
                                                    forecastAvailable = false,
                                                    forecastSource = forecast.Source,
                                                    configEntryDiscovered = forecast.ConfigEntryDiscovered,
                                                    area = forecast.Area,
                                                    currency = forecast.Currency,
                                                    timezone = forecast.Timezone,
                                                    warning,
                                                    slots = Array.Empty<object>()
                                                };
                                            }

                                            haJson = System.Text.Json.JsonSerializer.Serialize(new {
                                                current,
                                                next = GetState("next_price"),
                                                previous = GetState("previous_price"),
                                                lowest = GetState("lowest_price"),
                                                highest = GetState("highest_price"),
                                                dailyAverage = GetState("daily_average"),
                                                peakAverage = GetState("peak_average"),
                                                offPeak1Average = GetState("off_peak_1_average"),
                                                offPeak2Average = GetState("off_peak_2_average"),
                                                offPeak1 = new { from = op1.from, until = op1.until },
                                                offPeak2 = new { from = op2.from, until = op2.until },
                                                unit = GetUnit("current_price"),
                                                forecast = forecastBlock,
                                                forecastAvailable = forecast.ForecastAvailable,
                                                warning,
                                                prices
                                            });
                                        } else {
                                            logger.LogWarning("/api/price: Nord Pool fetch returned non-success ({codes}); falling back to factory sensor",
                                                string.Join(",", responses.Select(r => (int)r.StatusCode)));
                                        }
                                    } catch (Exception npEx) {
                                        logger.LogWarning("/api/price: Nord Pool fetch failed ({msg}); falling back to factory sensor", npEx.Message);
                                    }
                                }

                                // Legacy/fallback path: read the factory-registered price sensor 24 times.
                                if (haJson == null) {
                                    var factory = host.Services.GetRequiredService<IFactory>();
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
                                    haJson = System.Text.Json.JsonSerializer.Serialize(new { prices });
                                }

                                var bytes = System.Text.Encoding.UTF8.GetBytes(haJson);
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

                                var systemPrompt = $@"You are an NLU module for a smart-home assistant. The user may write in English or French. Reply ONLY with a JSON object (no prose, no code fences) matching this schema:
{{
  ""intent"": one of [""greeting"", ""capabilities"", ""smalltalk"", ""price_current"", ""price_cheapest"", ""price_expensive"", ""price_average"", ""set_temperature"", ""call_service"", ""query_state"", ""optimize_schedule"", ""out_of_scope"", ""unknown""],
  ""domain"": null | string (for call_service, e.g. ""light"", ""scene"", ""cover"", ""climate""),
  ""service"": null | string (for call_service, e.g. ""turn_on"", ""turn_off"", ""set_temperature""),
  ""entity_id"": null | string (for call_service or query_state, must be exact entity ID),
  ""data"": null | object (for call_service, e.g. {{""temperature"": 22}}),
  ""value"": null | number (for direct orders or legacy set_temperature),
  ""duration_hours"": null | integer (required for optimize_schedule),
  ""deadline_hour"": null | integer 0-24 (required for optimize_schedule),
  ""start_hour"": null | integer 0-23 (optional, only when the user gives an explicit time window like ""between 2am and 7am""),
  ""budget_max"": null | number in NOK (optional),
  ""power_kw"": null | number in kW,
  ""target"": null | string (for optimize_schedule, e.g. ""CarCharger"" or ""HeaterActuator""),
  ""answer"": a short English answer for the user (1-2 sentences)
}}

Here are the AVAILABLE entities in the home:
{entitiesList}

Rules:
- To control ANY entity discovered above (lights, scenes, switches, media players, scripts), use intent=""call_service"". Set the correct domain, service, and entity_id. Put any arguments in data.
  * e.g. ""turn on the kitchen"" -> intent=""call_service"", domain=""light"", service=""turn_on"", entity_id=""light.showcase_kitchen_light"", data={{""entity_id"": ""light.showcase_kitchen_light""}}
  * e.g. ""turn off movie mode"" -> intent=""call_service"", domain=""input_boolean"", service=""turn_off"", entity_id=""input_boolean.showcase_movie_mode"", data={{""entity_id"": ""input_boolean.showcase_movie_mode""}}
- IMPORTANT: To turn ON or OFF a mode (like Movie Mode or Sleep Mode), ALWAYS prefer the 'input_boolean' entity rather than 'scene'. Scenes cannot be turned off.
- If the user implies a macro-action like leaving home (""I'm leaving"", ""bye"", ""I'm going to work"") or going to bed (""good night""), LOOK for a relevant 'script' (e.g. script.showcase_leave_home, script.showcase_good_night) or 'scene' and call it.
- For ""set the temperature to 21 degrees"" (DIRECT ORDER) -> use intent=""set_temperature"", value=21, OR call_service on the climate entity if available.
- PRIORITY RULE: any request to charge a car / EV / Tesla / vehicle MUST be intent=""optimize_schedule"", even when the user mentions ""cheapest"" or ""lowest price"" — those are constraints, NOT a price-query intent.
  * e.g. ""charge the Tesla to 100% by 7am at the cheapest rate"" -> intent=""optimize_schedule"", target=""CarCharger"", duration_hours=4, deadline_hour=7, power_kw=11
  * e.g. ""charge the car between 2am and 7am at the lowest price"" -> intent=""optimize_schedule"", target=""CarCharger"", duration_hours=5, start_hour=2, deadline_hour=7, power_kw=11
- For questions about current states (temperature, humidity, is a door open) -> intent=""query_state"", and set ""entity_id"" to the asked entity.
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
                            // This is the first production path toward MAPE-K future-cost optimization:
                            // price forecast comes from Nord Pool (auto-discovered config_entry, real
                            // hourly prices from nordpool.get_prices_for_date), EV consumption forecast
                            // is simulated from candidate schedules. Later, general consumption should
                            // come from the digital twin/FMUs for each simulation path.
                            try {
                                using var reader = new StreamReader(context.Request.InputStream);
                                var body = await reader.ReadToEndAsync();
                                using var doc = System.Text.Json.JsonDocument.Parse(body);

                                // Nord Pool slots can be sub-hourly (typically 15 min). duration_hours is a
                                // *duration* in hours requested by the user, not a slot count.
                                double requestedDurationHours = doc.RootElement.TryGetProperty("duration_hours", out var durEl) && durEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? durEl.GetDouble() : 4.0;
                                int deadlineHour = doc.RootElement.TryGetProperty("deadline_hour", out var dhEl) && dhEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? dhEl.GetInt32() : 24;
                                int? startHour = doc.RootElement.TryGetProperty("start_hour", out var shEl) && shEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? shEl.GetInt32() : (int?)null;
                                double? budget = doc.RootElement.TryGetProperty("budget_max", out var bmEl) && bmEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? bmEl.GetDouble() : (double?)null;
                                double powerKw = doc.RootElement.TryGetProperty("power_kw", out var pkEl) && pkEl.ValueKind == System.Text.Json.JsonValueKind.Number
                                    ? pkEl.GetDouble() : 1.0;
                                string? target = doc.RootElement.TryGetProperty("target", out var tgEl) && tgEl.ValueKind == System.Text.Json.JsonValueKind.String
                                    ? tgEl.GetString() : null;

                                var token = Environment.GetEnvironmentVariable("TOKEN_HA") ?? string.Empty;
                                var forecast = await NordPoolForecastProvider.GetForecastAsync(token, logger);

                                if (!forecast.ForecastAvailable) {
                                    var noFc = System.Text.Json.JsonSerializer.Serialize(new {
                                        optimized = false,
                                        forecastAvailable = false,
                                        reason = forecast.Warning ?? "Future Nord Pool price forecast unavailable",
                                        priceSource = forecast.Source,
                                        configEntryDiscovered = forecast.ConfigEntryDiscovered,
                                        area = forecast.Area,
                                        currency = forecast.Currency
                                    });
                                    var noFcBytes = System.Text.Encoding.UTF8.GetBytes(noFc);
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(noFcBytes, 0, noFcBytes.Length);
                                    logger.LogWarning("[CHATBOX] Optimize aborted: forecast unavailable ({reason})", forecast.Warning);
                                    return;
                                }

                                // Build candidate window aligned to wall-clock local time, using the same
                                // timezone the forecast was projected into.
                                TimeZoneInfo tz;
                                try { tz = TimeZoneInfo.FindSystemTimeZoneById(forecast.Timezone); }
                                catch { try { tz = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); } catch { tz = TimeZoneInfo.Utc; } }
                                var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

                                // Resolve deadline as the next wall-clock occurrence of `deadline_hour` in the future.
                                int dl = Math.Clamp(deadlineHour, 1, 48);
                                var deadlineCandidate = new DateTimeOffset(nowLocal.Year, nowLocal.Month, nowLocal.Day, 0, 0, 0, nowLocal.Offset).AddHours(dl);
                                if (deadlineCandidate <= nowLocal) deadlineCandidate = deadlineCandidate.AddDays(1);

                                DateTimeOffset windowStart = nowLocal;
                                if (startHour is int sh) {
                                    int sHour = Math.Clamp(sh, 0, 23);
                                    var startCandidate = new DateTimeOffset(nowLocal.Year, nowLocal.Month, nowLocal.Day, 0, 0, 0, nowLocal.Offset).AddHours(sHour);
                                    if (startCandidate <= nowLocal) startCandidate = startCandidate.AddDays(1);
                                    if (startCandidate >= deadlineCandidate) startCandidate = startCandidate.AddDays(-1);
                                    windowStart = startCandidate > nowLocal ? startCandidate : nowLocal;
                                }

                                // Aggregate sub-hourly Nord Pool slots (typically 15 min) into full-hour buckets.
                                // /api/optimize works in whole hours so it stays compatible with /api/execute_schedule
                                // (24 hourly cells) and the demo time mode (1h = 1min). /api/price still exposes the
                                // raw 15-min slots for callers that want full resolution.
                                static double SlotHours(NordPoolForecastProvider.Slot s) => Math.Max(0, (s.End - s.Start).TotalHours);

                                var allBuckets = forecast.Slots
                                    .GroupBy(s => new DateTimeOffset(s.Start.Year, s.Start.Month, s.Start.Day, s.Start.Hour, 0, 0, s.Start.Offset))
                                    .Select(g => {
                                        var bucketStart = g.Key;
                                        var bucketEnd = bucketStart.AddHours(1);
                                        var slots = g.OrderBy(s => s.Start).ToList();
                                        double covered = slots.Sum(SlotHours);
                                        double avgPrice = covered > 0
                                            ? slots.Sum(s => s.Price * SlotHours(s)) / covered
                                            : slots.Average(s => s.Price);
                                        return new {
                                            Start = bucketStart,
                                            End = bucketEnd,
                                            AvgPrice = avgPrice,
                                            Complete = covered >= 0.999  // tolerate float jitter; 4×0.25 = 1.0
                                        };
                                    })
                                    .ToList();

                                // Keep only complete future hours fully contained in [windowStart, deadlineCandidate].
                                var hourBuckets = allBuckets
                                    .Where(b => b.Complete && b.Start >= windowStart && b.End <= deadlineCandidate)
                                    .OrderBy(b => b.Start)
                                    .ToList();

                                var cheapest3 = string.Join(", ",
                                    hourBuckets.OrderBy(b => b.AvgPrice).Take(3)
                                        .Select(b => $"{b.Start:HH:mm}@{b.AvgPrice:F4}"));
                                logger.LogInformation(
                                    "[OPTIMIZE] rawSlots={fs} hourlyBuckets={ab} windowBuckets={hb} windowStart={ws:HH:mm} deadline={dc:HH:mm} | cheapest3={c}",
                                    forecast.Slots.Count, allBuckets.Count, hourBuckets.Count,
                                    windowStart, deadlineCandidate, cheapest3);

                                if (hourBuckets.Count == 0) {
                                    var emptyJson = System.Text.Json.JsonSerializer.Serialize(new {
                                        optimized = false,
                                        forecastAvailable = true,
                                        reason = "No complete future hours within the requested window",
                                        priceSource = forecast.Source,
                                        windowStart = windowStart.ToString("o"),
                                        deadline = deadlineCandidate.ToString("o")
                                    });
                                    var eb = System.Text.Encoding.UTF8.GetBytes(emptyJson);
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(eb, 0, eb.Length);
                                    return;
                                }

                                // Each bucket is 1h, so #buckets = #hours. duration_hours is rounded to the nearest
                                // whole hour and capped by what the window allows.
                                int needed = Math.Max(1, (int)Math.Round(requestedDurationHours, MidpointRounding.AwayFromZero));
                                needed = Math.Min(needed, hourBuckets.Count);

                                var chosenBuckets = hourBuckets
                                    .OrderBy(b => b.AvgPrice)
                                    .ThenBy(b => b.Start)
                                    .Take(needed)
                                    .OrderBy(b => b.Start)
                                    .ToList();

                                double actualDurationHours = needed; // 1h per bucket
                                double totalCost   = chosenBuckets.Sum(b => b.AvgPrice * powerKw); // ×1h
                                double avgWindow   = hourBuckets.Average(b => b.AvgPrice);
                                // Window-average baseline: charging the same energy at the window's mean hourly price.
                                // Conservative — represents "you didn't optimize within the deadline window".
                                double baselineCost = avgWindow * powerKw * requestedDurationHours;
                                double savingsPct  = baselineCost > 0 ? (1 - totalCost / baselineCost) * 100 : 0;
                                double avgChosen   = chosenBuckets.Average(b => b.AvgPrice);

                                // Worst-N baseline: same energy charged at the N most expensive hours available
                                // within the deadline window. This is the upper bound of achievable savings —
                                // i.e. "the worst plan you could have picked under the same constraint".
                                var worstBuckets = hourBuckets
                                    .OrderByDescending(b => b.AvgPrice)
                                    .ThenBy(b => b.Start)
                                    .Take(needed)
                                    .ToList();
                                double worstAvgPrice = worstBuckets.Count > 0 ? worstBuckets.Average(b => b.AvgPrice) : avgWindow;
                                double worstCost = worstAvgPrice * powerKw * requestedDurationHours;
                                double worstSavingsPct = worstCost > 0 ? (1 - totalCost / worstCost) * 100 : 0;

                                var chosenStarts = new HashSet<DateTimeOffset>(chosenBuckets.Select(b => b.Start));

                                // 24-cell schedule keyed on local hour-of-day. on=true iff the bucket for that
                                // hour was chosen — guarantees schedule[].on matches chosen_slots exactly so the
                                // Run-plan executor never runs more hours than were optimized.
                                var schedule = new System.Collections.Generic.List<object>(24);
                                var bucketsByHour = hourBuckets
                                    .GroupBy(b => b.Start.Hour)
                                    .ToDictionary(g => g.Key, g => g.OrderBy(b => b.Start).First());
                                for (int h = 0; h < 24; h++) {
                                    if (bucketsByHour.TryGetValue(h, out var b)) {
                                        schedule.Add(new {
                                            hour = h,
                                            price = Math.Round(b.AvgPrice, 4),
                                            on = chosenStarts.Contains(b.Start),
                                            before_deadline = b.End <= deadlineCandidate,
                                            in_window = b.Start >= windowStart && b.End <= deadlineCandidate
                                        });
                                    } else {
                                        schedule.Add(new {
                                            hour = h, price = (double?)null, on = false,
                                            before_deadline = false, in_window = false
                                        });
                                    }
                                }

                                var chosenSlotsOut = chosenBuckets.Select(b => new {
                                    start = b.Start.ToString("o"),
                                    end = b.End.ToString("o"),
                                    hour = b.Start.Hour,
                                    duration_hours = 1.0,
                                    price = Math.Round(b.AvgPrice, 4),
                                    cost = Math.Round(b.AvgPrice * powerKw, 4)
                                }).ToList();

                                var result = new {
                                    optimized = true,
                                    forecastAvailable = true,
                                    priceSource = forecast.Source,
                                    configEntryDiscovered = forecast.ConfigEntryDiscovered,
                                    area = forecast.Area,
                                    currency = forecast.Currency,
                                    timezone = forecast.Timezone,
                                    target,
                                    chosen_slots = chosenSlotsOut,
                                    chosen_hours = chosenBuckets.Select(b => b.Start.Hour).OrderBy(h => h).ToList(),
                                    requested_duration_hours = Math.Round(requestedDurationHours, 4),
                                    duration_hours = (int)Math.Round(actualDurationHours),
                                    actual_duration_hours = Math.Round(actualDurationHours, 4),
                                    slot_count = chosenBuckets.Count,
                                    start_hour = windowStart.Hour,
                                    deadline_hour = deadlineCandidate.Hour == 0 ? 24 : deadlineCandidate.Hour,
                                    power_kw = powerKw,
                                    total_cost_nok = Math.Round(totalCost, 2),
                                    baseline_cost_nok = Math.Round(baselineCost, 2),
                                    baseline_worst_cost_nok = Math.Round(worstCost, 2),
                                    baseline_worst_avg_price = Math.Round(worstAvgPrice, 4),
                                    baseline_worst_savings_percent = (int)Math.Round(worstSavingsPct),
                                    avg_price = Math.Round(avgWindow, 4),
                                    avg_price_chosen = Math.Round(avgChosen, 4),
                                    savings_percent = (int)Math.Round(savingsPct),
                                    budget_max = budget,
                                    within_budget = budget == null || totalCost <= budget.Value,
                                    schedule
                                };

                                var json = System.Text.Json.JsonSerializer.Serialize(result);
                                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                                logger.LogInformation($"[CHATBOX] Optimize: target={target}, requested={requestedDurationHours:F2}h actual={actualDurationHours:F2}h ({chosenBuckets.Count} hourly buckets) in [{windowStart:HH:mm}..{deadlineCandidate:HH:mm}), cost={totalCost:F2} {forecast.Currency} ({savingsPct:F0}% saved)");
                            } catch (Exception ex) {
                                context.Response.StatusCode = 500;
                                var bytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            }
                        } else if (context.Request.Url!.AbsolutePath == "/api/proactive/status" && context.Request.HttpMethod == "GET") {
                            // Read-only view of the proactive arm: returns the latest advisory computed
                            // by MAPE-K (cheap/peak window detection from the Nord Pool forecast).
                            // Returns 204 if MAPE-K hasn't produced an advisory yet.
                            try {
                                var advisor = host.Services.GetRequiredService<IProactiveAdvisor>();
                                var latest = advisor.Latest;
                                if (latest is null) {
                                    context.Response.StatusCode = 204;
                                } else {
                                    var json = System.Text.Json.JsonSerializer.Serialize(new {
                                        forecastAvailable = latest.ForecastAvailable,
                                        warning = latest.Warning,
                                        generatedAt = latest.GeneratedAt.ToString("o"),
                                        currency = latest.Currency,
                                        area = latest.Area,
                                        currentPrice = latest.CurrentPrice,
                                        q1 = latest.Q1,
                                        q3 = latest.Q3,
                                        nextPeakStart = latest.NextPeakStart?.ToString("o"),
                                        nextPeakPrice = latest.NextPeakPrice,
                                        hoursUntilNextPeak = latest.HoursUntilNextPeak,
                                        shouldPreheat = latest.ShouldPreheat,
                                        shouldDeferLoad = latest.ShouldDeferLoad,
                                        reason = latest.Reason
                                    });
                                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                                    context.Response.ContentType = "application/json";
                                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                                }
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
