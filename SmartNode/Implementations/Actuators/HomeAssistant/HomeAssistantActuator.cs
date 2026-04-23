using Logic.TTComponentInterfaces;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Implementations.Actuators.HomeAssistant
{
    public class HomeAssistantActuator : IActuator
    {
        private readonly HttpClient _httpClient;
        private readonly string _entityId;
        private readonly ActuatorKind _kind;
        private readonly string? _onOption;
        private int _state = 0;

        public enum ActuatorKind { InputBoolean, InputSelect }

        public HomeAssistantActuator(string actuatorName, string entityId, ActuatorKind kind, HttpClient httpClient, string? onOption = null)
        {
            ActuatorName = actuatorName;
            _entityId = entityId;
            _kind = kind;
            _httpClient = httpClient;
            _onOption = onOption;
        }

        public string ActuatorName { get; }

        public object ActuatorState => _state;

        public async Task Actuate(object state)
        {
            if (state is not int intState)
                intState = int.Parse(state.ToString()!, CultureInfo.InvariantCulture);
            _state = intState;

            string requestUri;
            string body;

            if (_kind == ActuatorKind.InputBoolean)
            {
                var service = intState == 1 ? "input_boolean/turn_on" : "input_boolean/turn_off";
                requestUri = $"api/services/{service}";
                body = JsonSerializer.Serialize(new { entity_id = _entityId });
            }
            else
            {
                var option = intState == 1 ? (_onOption ?? "on") : "off";
                requestUri = "api/services/input_select/select_option";
                body = JsonSerializer.Serialize(new { entity_id = _entityId, option });
            }

            await _httpClient.PostAsync(requestUri, new StringContent(body, Encoding.UTF8, "application/json"));
        }

        public void RunDummyEnvironment(double mapekExecutionDurationSeconds) { }
    }
}
