using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Logic.TTComponentInterfaces;

namespace Implementations.Sensors.HomeAssistant {
    
    public class HomeAssistantSensor : ISensor {
        private readonly HttpClient _httpClient;
        private readonly string? _attribute;

        public HomeAssistantSensor(string sensorName, string procedureName, string? attribute, HttpClient httpClient) {
            Debug.Assert(httpClient.BaseAddress != null, "HttpClient BaseAddress is not set.");
            SensorName = sensorName;
            ProcedureName = procedureName; // Currently used for sensor_id.
            _attribute = attribute; // Do we need to peek into the JSON structure beyond the `state`?
            _httpClient = httpClient;
        }
        
        public string SensorName { get; private set; }

        public string ProcedureName { get; private set; }

        record class HAAttributes(string? unit_of_measurement);
        record class SensorValue(double State, HAAttributes Attributes); // For deserializing the JSON response

        public async Task<object> ObservePropertyValue(params object[] inputProperties) {
            var requestUri = $"api/states/{ProcedureName}";
            // If HA is unreachable or returns malformed data, return a neutral 0.0 so the MAPE-K loop
            // can continue with a degraded reading instead of bubbling an exception that kills it.
            try {
                if (_attribute == null) {
                    var response = await _httpClient.GetFromJsonAsync<SensorValue>(requestUri);
                    if (response == null) {
                        Trace.WriteLine($"HA sensor {ProcedureName} returned null payload — falling back to 0.0");
                        return 0.0;
                    }
                    return response.State;
                } else {
                    var response = await _httpClient.GetStringAsync(requestUri);
                    if (string.IsNullOrEmpty(response)) {
                        Trace.WriteLine($"HA sensor {ProcedureName} returned empty payload — falling back to 0.0");
                        return 0.0;
                    }
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(response);
                    jsonDoc.RootElement.GetProperty("attributes").TryGetProperty(_attribute, out var value);
                    jsonDoc.RootElement.GetProperty("attributes").TryGetProperty(_attribute + "_unit", out var unit);
                    return value.ValueKind == JsonValueKind.Undefined ? 0.0 : value.GetDouble();
                }
            } catch (Exception ex) {
                Trace.WriteLine($"HA sensor {ProcedureName} unreachable: {ex.Message} — falling back to 0.0");
                return 0.0;
            }
        }
    }
}