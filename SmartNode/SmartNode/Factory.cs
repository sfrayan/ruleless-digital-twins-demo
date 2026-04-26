using Logic.TTComponentInterfaces;
using Logic.FactoryInterface;
using Logic.ValueHandlerInterfaces;
using Implementations.ValueHandlers;
using Implementations.Sensors.RoomM370;
using Implementations.Sensors.CustomPiece;
using Implementations.SoftwareComponents;
using Implementations.Actuators.RoomM370;
using Implementations.Sensors.Incubator;
using Implementations.Actuators.Incubator;
using Implementations.SimulatedTwinningTargets;
using Implementations.Sensors.Fakepool;
using Implementations.Sensors.HomeAssistant;
using Implementations.Actuators.HomeAssistant;
using System.Net.Http.Headers;

namespace SmartNode
{
    public class Factory : IFactory
    {
        private readonly string _environment;

        // Since sensors and actuators mostly relate to sensor-actuator networks as communciation media for physical TTs (PTs), this factory allows for registering implementations
        // that deliberately do not use the physical implementation as the TT. For testing purposes, one can thus register sensors and actuators for mock environments (dummy
        // environments) with the names of those environments as keys of the maps. Since ConfigurableParameters and value handlers aren't coupled to physical systems, these can just
        // be registered in one map.
        // 
        // New implementations can simply be added to the factory collections.
        public readonly Dictionary<string, SensorActuatorMapWrapper> _sensorActuatorMaps;
        private Dictionary<string, SensorActuatorMapWrapper> MakeSensorMap()
        {
            var map = new Dictionary<string, SensorActuatorMapWrapper>(){
                {
                    "incubator",
                    new SensorActuatorMapWrapper {
                        ActuatorMap = new() {
                            {
                                "http://www.semanticweb.org/vs/ontologies/2025/12/incubator#HeaterActuator",
                                new AmqHeater(_incubatorAdapter)
                            },
                            {
                                "http://www.semanticweb.org/vs/ontologies/2025/12/incubator#FanActuator",
                                new AmqFan(_incubatorAdapter)
                            }
                        },
                        SensorMap = new() {
                            {
                                ("http://www.semanticweb.org/vs/ontologies/2025/12/incubator#TempSensor",
                                "http://www.semanticweb.org/vs/ontologies/2025/12/incubator#TempProcedure"),
                                new AmqSensor(_incubatorAdapter, "http://www.semanticweb.org/vs/ontologies/2025/12/incubator#TempSensor",
                                    "http://www.semanticweb.org/vs/ontologies/2025/12/incubator#TempProcedure",
                                    d => d.average_temperature)
                            }
                        }
                    }
                },
                {
                    "roomM370",
                    new SensorActuatorMapWrapper {
                        ActuatorMap = new() {
                            {
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#Heater",
                                new DummyHeater("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#Heater")
                            },
                            {
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FloorHeating",
                                new DummyFloorHeating("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FloorHeating")
                            },
                            {
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#Dehumidifier",
                                new DummyDehumidifier("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#Dehumidifier")
                            },
                            { // [VS] Abuse -- input for FMU which does not have a TT
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolStepActuator",
                                new DummyDehumidifier("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolStepActuator")

                            }
                        },
                        SensorMap = new() {
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#SoftSensor1",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#SoftSensor1Algorithm"),
                                new DummyTemperatureSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#SoftSensor1Algorithm",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#SoftSensor1")
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor2",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor2Procedure"),
                                new DummyTemperatureSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor2Procedure",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor2")
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor1",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor1Procedure"),
                                new DummyTemperatureSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor1Procedure",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#TemperatureSensor1")
                            },
                            {
                                ("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceSoftSensor",
                                "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CompressionRatioAlgorithm"),
                                new DummySensor("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CompressionRatioAlgorithm",
                                    "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceSoftSensor")
                            },
                            {
                                ("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceSoftSensor",
                                "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPiece"),
                                new DummySensor("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPiece",
                                    "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceSoftSensor")
                            },
                            {
                                ("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/TemperatureSensor1",
                                "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/TemperatureSensor1Procedure"),
                                new DummyTemperatureSensor("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/TemperatureSensor1Procedure",
                                    "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/TemperatureSensor1")
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#EnergyConsumptionMeter",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#EnergyConsumptionMeterProcedure"),
                                new DummyEnergyConsumptionSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#EnergyConsumptionMeterProcedure",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#EnergyConsumptionMeter")
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#HumiditySensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#HumiditySensorProcedure"),
                                new DummyHumiditySensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#HumiditySensorProcedure",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#HumiditySensor")
                            },
                            { // [VS] Abuse:
                                ("http://www.semanticweb.org/vs/ontologies/2025/11/untitled-ontology-97#DummySensor",
                                "http://www.semanticweb.org/vs/ontologies/2025/11/untitled-ontology-97#DummyProcedure"),
                                new ConstantSensor("http://www.semanticweb.org/vs/ontologies/2025/11/untitled-ontology-97#DummyProcedure",
                                    "http://www.semanticweb.org/vs/ontologies/2025/11/untitled-ontology-97#DummySensor", -1)
                            },
                            {
                                ("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CompressionRatioSoftSensor",
                                "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CompressionRatioAlgorithm"),
                                new CompressionRatioSoftSensor("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CompressionRatioSoftSensor",
                                    "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CompressionRatioAlgorithm")
                            },
                            {
                                ("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceSoftSensor",
                                "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceAlgorithm"),
                                new CustomPieceSoftSensor("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceSoftSensor",
                                    "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/CustomPieceAlgorithm")
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MotionSensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MotionSensorProcedure"),
                                new MotionSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MotionSensor",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MotionSensorProcedure")
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceSensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceProcedure"),
                                new FakepoolSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceSensor",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceProcedure")
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceDummySensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceDummyProcedure"),
                                new ConstantSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceDummySensor",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PriceDummyProcedure", 1.58) // XXX In the absence of an FP-not-so-softsensor

                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PricePerEnergySoftSensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PricePerEnergyProcedure"),
                                new PricePerEnergySoftSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PricePerEnergySoftSensor",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#PricePerEnergyProcedure", (x,y) => x*y)
                            },



                            // The following are workarounds due to a bug in how we query Inputs/Outputs and build soft sensor trees!
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolNotFoundSensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolNotFoundProcedure"),
                                new GeneralConstantSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolNotFoundSensor",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolNotFoundProcedure",
                                    false)
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolStepSensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolStepProcedure"),
                                new GeneralConstantSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolStepSensor",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#FakepoolStepProcedure",
                                    0.0)
                            },
                            {
                                ("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MapekCycleSensor",
                                "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MapekCycleProcedure"),
                                new GeneralConstantSensor("http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MapekCycleSensor",
                                    "http://www.semanticweb.org/ivans/ontologies/2025/instance-model-1#MapekCycleProcedure",
                                    0)
                            }
                        }
                    }
                },
                {
                    string.Empty,
                    new SensorActuatorMapWrapper {
                        ActuatorMap = new() {

                        },
                        SensorMap = new() {

                        }
                    }
                }
            };

            if (_haHttpClient != null) {
                map.Add("homeassistant", new SensorActuatorMapWrapper {
                    SensorMap = new() {
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/TempSensor1",
                             "http://www.semanticweb.org/rayan/ontologies/2025/ha/TempSensor1Procedure"),
                            new HomeAssistantSensor(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/TempSensor1",
                                "sensor.showcase_living_room_temperature",
                                null, _haHttpClient)
                        },
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/TempSensor2",
                             "http://www.semanticweb.org/rayan/ontologies/2025/ha/TempSensor2Procedure"),
                            new HomeAssistantSensor(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/TempSensor2",
                                "sensor.showcase_living_room_temperature",
                                null, _haHttpClient)
                        },
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/HumiditySensor",
                             "http://www.semanticweb.org/rayan/ontologies/2025/ha/HumiditySensorProcedure"),
                            new HomeAssistantSensor(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/HumiditySensor",
                                "sensor.showcase_air_quality_index",
                                null, _haHttpClient)
                        },
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceSensor",
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceProcedure"),
                            new FakepoolSensor("http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceSensor",
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceProcedure")
                        },
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceDummySensor",
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceDummyProcedure"),
                            new ConstantSensor("http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceDummySensor",
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/PriceDummyProcedure", 1.58)
                        },
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/EnergyConsumptionMeter",
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/EnergyConsumptionMeterProcedure"),
                            new DummyEnergyConsumptionSensor("http://www.semanticweb.org/rayan/ontologies/2025/ha/EnergyConsumptionMeterProcedure",
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/EnergyConsumptionMeter")
                        },
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/MapekCycleSensor",
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/MapekCycleProcedure"),
                            new GeneralConstantSensor("http://www.semanticweb.org/rayan/ontologies/2025/ha/MapekCycleSensor",
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/MapekCycleProcedure",
                                0)
                        },
                        // Extra HA sensors for chatbox visualisation only (not part of the MAPE-K instance model).
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/PowerDrawSensor",
                             "http://www.semanticweb.org/rayan/ontologies/2025/ha/PowerDrawProcedure"),
                            new HomeAssistantSensor(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/PowerDrawSensor",
                                "sensor.showcase_estimated_power_draw",
                                null, _haHttpClient)
                        },
                        {
                            ("http://www.semanticweb.org/rayan/ontologies/2025/ha/AirQualitySensor",
                             "http://www.semanticweb.org/rayan/ontologies/2025/ha/AirQualityProcedure"),
                            new HomeAssistantSensor(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/AirQualitySensor",
                                "sensor.showcase_air_quality_index",
                                null, _haHttpClient)
                        }
                    },
                    ActuatorMap = new() {
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/HeaterActuator",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/HeaterActuator",
                                "input_boolean.showcase_living_room_light",
                                HomeAssistantActuator.ActuatorKind.InputBoolean,
                                _haHttpClient)
                        },
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/FloorHeatingActuator",
                            new DummyFloorHeating("http://www.semanticweb.org/rayan/ontologies/2025/ha/FloorHeatingActuator")
                        },
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/DehumidifierActuator",
                            new DummyDehumidifier("http://www.semanticweb.org/rayan/ontologies/2025/ha/DehumidifierActuator")
                        },
                        // Real HA actuators wired for the chatbox (lights + air purifier switch).
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/LivingRoomLight",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/LivingRoomLight",
                                "light.showcase_living_room_lamp",
                                HomeAssistantActuator.ActuatorKind.Light,
                                _haHttpClient)
                        },
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/KitchenLight",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/KitchenLight",
                                "light.showcase_kitchen_light",
                                HomeAssistantActuator.ActuatorKind.Light,
                                _haHttpClient)
                        },
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/HallwayLight",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/HallwayLight",
                                "light.showcase_hallway_light",
                                HomeAssistantActuator.ActuatorKind.Light,
                                _haHttpClient)
                        },
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/AirPurifier",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/AirPurifier",
                                "switch.showcase_air_purifier",
                                HomeAssistantActuator.ActuatorKind.Switch,
                                _haHttpClient)
                        },
                        {
                            // Car charger target for the optimize_schedule planner demo.
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/CarCharger",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/CarCharger",
                                "input_boolean.showcase_car_charger",
                                HomeAssistantActuator.ActuatorKind.InputBoolean,
                                _haHttpClient)
                        },
                        // Numeric HA inputs — direct-order actuators (chatbox "mets 21 degrés" → immediate push).
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/LivingRoomTemperatureInput",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/LivingRoomTemperatureInput",
                                "input_number.showcase_temperature",
                                HomeAssistantActuator.ActuatorKind.InputNumber,
                                _haHttpClient)
                        },
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/PowerDrawInput",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/PowerDrawInput",
                                "input_number.showcase_power_draw",
                                HomeAssistantActuator.ActuatorKind.InputNumber,
                                _haHttpClient)
                        },
                        {
                            "http://www.semanticweb.org/rayan/ontologies/2025/ha/AirQualityInput",
                            new HomeAssistantActuator(
                                "http://www.semanticweb.org/rayan/ontologies/2025/ha/AirQualityInput",
                                "input_number.showcase_air_quality_index",
                                HomeAssistantActuator.ActuatorKind.InputNumber,
                                _haHttpClient)
                        }
                    }
                });
            }

            return map;
        }

        private readonly Dictionary<string, IConfigurableParameter> _configurableParameters = new() {
            {
                "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/BucketSize",
                new DummyConfigurableParameter("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/BucketSize")
            },
            {
                "http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/Epsilon",
                new DummyConfigurableParameter("http://www.semanticweb.org/ispa/ontologies/2025/instance-model-2/Epsilon")
            }
        };

        // The keys represent the OWL (RDF/XSD) types supported by Protege, and the values are user implementations.
        private readonly Dictionary<string, IValueHandler> _valueHandlers = new() {
            { "http://www.w3.org/2001/XMLSchema#double", new DoubleValueHandler() },
            { "http://www.w3.org/2001/XMLSchema#string", new StringValueHandler() },
            { "http://www.w3.org/2001/XMLSchema#int", new IntValueHandler() },
            { "http://www.w3.org/2001/XMLSchema#integer", new IntValueHandler() },
            { "http://www.w3.org/2001/XMLSchema#base64Binary", new Base64BinaryValueHandler() },
            { "http://www.w3.org/2001/XMLSchema#boolean", new BooleanValueHandler() }
        };
        private readonly IncubatorAdapter? _incubatorAdapter;
        private HttpClient? _haHttpClient;
        // Changing the environment variable's value requires restarting Visual Studio before it's visible.
        private const string HostNameEnvironmentVariableName = "AU_INCUBATOR_RABBITMQ_HOST_NAME";
        private const string HaTokenEnvVarName = "TOKEN_HA";
        private const string HaBaseUrl = "http://localhost:8123/";

        public Factory(string dummyEnvironment) {
            _environment = dummyEnvironment;
            // XXX: We should really split the factory eventually.
            if ("incubator".Equals(_environment)) {
                // TODO: Might as well directly come from its own section in the ConfigurationSettings.
                var hostName = Environment.GetEnvironmentVariable(HostNameEnvironmentVariableName) ?? "localhost";
                _incubatorAdapter = new IncubatorAdapter(hostName, new CancellationToken());
                Task t = Task.Run(async () => {
                    await _incubatorAdapter.Connect();
                    await _incubatorAdapter.Setup();
                });
                t.Wait();
            }

            if ("homeassistant".Equals(_environment)) {
                var token = Environment.GetEnvironmentVariable(HaTokenEnvVarName) ?? string.Empty;
                _haHttpClient = new HttpClient { BaseAddress = new Uri(HaBaseUrl) };
                _haHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _haHttpClient.DefaultRequestHeaders.Add("User-Agent", "SmartNode/1.0");
            }

            _sensorActuatorMaps = MakeSensorMap();
        }

        public ISensor GetSensorImplementation(string sensorName, string procedureName) {
            if (_sensorActuatorMaps.TryGetValue(_environment, out SensorActuatorMapWrapper? sensorActuatorMapWrapper)) {
                if (sensorActuatorMapWrapper.SensorMap.TryGetValue((sensorName, procedureName), out ISensor? sensor)) {
                    return sensor;
                }

                throw new Exception($"No implementation was found for Sensor {sensorName} with Procedure {procedureName}.");
            }

            throw new Exception($"No sensor-actuator mapping exists for environment {_environment}.");
        }

        public IActuator GetActuatorImplementation(string actuatorName) {
            if (_sensorActuatorMaps.TryGetValue(_environment, out SensorActuatorMapWrapper? sensorActuatorMapWrapper)) {
                if (sensorActuatorMapWrapper.ActuatorMap.TryGetValue(actuatorName, out IActuator? actuator)) {
                    return actuator;
                }

                throw new Exception($"No implementation was found for Actuator {actuatorName}.");
            }

            throw new Exception($"No sensor-actuator mapping exists for environment {_environment}.");
        }

        public IConfigurableParameter GetConfigurableParameterImplementation(string configurableParameterName) {
            if (_configurableParameters.TryGetValue(configurableParameterName, out IConfigurableParameter? configurableParameter)) {
                return configurableParameter;
            }

            throw new Exception($"No implementation was found for software component {configurableParameterName}.");
        }

        public IValueHandler GetValueHandlerImplementation(string owlType) {
            if (_valueHandlers.TryGetValue(owlType, out IValueHandler? sensorValueHandler)) {
                return sensorValueHandler;
            }

            throw new Exception($"No implementation was found for Sensor value handler for OWL type {owlType}.");
        }

        public IEnumerable<(string SensorName, string ProcedureName)> ListSensorKeys() {
            if (_sensorActuatorMaps.TryGetValue(_environment, out SensorActuatorMapWrapper? wrapper)) {
                return wrapper.SensorMap.Keys;
            }
            return [];
        }

        public IEnumerable<string> ListActuatorKeys() {
            if (_sensorActuatorMaps.TryGetValue(_environment, out SensorActuatorMapWrapper? wrapper)) {
                return wrapper.ActuatorMap.Keys;
            }
            return [];
        }

        public class SensorActuatorMapWrapper {
            public required Dictionary<(string, string), ISensor> SensorMap { get; set; }

            public required Dictionary<string, IActuator> ActuatorMap { get; set; }
        }
    }
}
