using Logic.Models.MapekModels;
using Logic.Models.OntologicalModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Logic.Mapek {
    public class BangBangPlanner : IBangBangPlanner {
        private readonly ILogger<IBangBangPlanner> _logger;
        private readonly IMapekKnowledge _mapekKnowledge;

        private const string ValueIncreaseUri = "http://www.semanticweb.org/ivans/ontologies/2025/ruleless-digital-twins/ValueIncrease";

        public BangBangPlanner(IServiceProvider serviceProvider) {
            _logger = serviceProvider.GetRequiredService<ILogger<IBangBangPlanner>>();
            _mapekKnowledge = serviceProvider.GetRequiredService<IMapekKnowledge>();
        }

        public Simulation Plan(Cache cache) {
            _logger.LogInformation("Firing the bang-bang controller.");

            // Query actuator → property → direction (ValueIncrease / ValueDecrease).
            var query = _mapekKnowledge.GetParameterizedStringQuery(@"SELECT ?actuator ?property ?direction WHERE {
                ?actuator rdf:type sosa:Actuator .
                ?actuator meta:enacts ?propertyChange .
                ?propertyChange ssn:forProperty ?property .
                ?propertyChange meta:affectsPropertyWith ?direction . }");

            var mappings = new Dictionary<string, (string propertyUri, bool isIncrease)>();
            foreach (var row in _mapekKnowledge.ExecuteQuery(query).Results) {
                var actuatorUri = row["actuator"].ToString();
                var propertyUri = row["property"].ToString();
                var isIncrease  = row["direction"].ToString() == ValueIncreaseUri;
                // If an actuator enacts multiple PropertyChanges, last one wins — acceptable for now.
                mappings[actuatorUri] = (propertyUri, isIncrease);
            }

            var actions = new List<Models.OntologicalModels.Action>();

            foreach (var (actuatorUri, actuator) in cache.Actuators) {
                var newState = 0;

                if (mappings.TryGetValue(actuatorUri, out var mapping) &&
                    cache.PropertyCache.Properties.TryGetValue(mapping.propertyUri, out var property)) {

                    var currentValue = Convert.ToDouble(property.Value);
                    var oc = cache.OptimalConditions.FirstOrDefault(o => o.Property.Name == mapping.propertyUri);

                    if (oc is not null) {
                        var (tooLow, tooHigh) = IsViolated(currentValue, oc.ConditionConstraint);

                        if (tooLow && mapping.isIncrease)   newState = 1;
                        if (tooHigh && !mapping.isIncrease) newState = 1;
                    }
                }

                actuator.State = newState;
                actions.Add(new ActuationAction {
                    Name       = $"{actuatorUri.Split('/').Last()}_{newState}",
                    Actuator   = actuator,
                    NewStateValue = newState
                });
            }

            _logger.LogInformation("Generated decision.");
            return new Simulation(cache.PropertyCache) { Actions = actions };
        }

        private static (bool tooLow, bool tooHigh) IsViolated(double value, ConstraintExpression constraint) =>
            constraint switch {
                NestedConstraintExpression { ConstraintType: ConstraintType.And } nested =>
                    IsViolatedConjunction(value, nested),
                AtomicConstraintExpression atomic =>
                    IsViolatedAtomic(value, atomic),
                _ => (false, false)
            };

        private static (bool tooLow, bool tooHigh) IsViolatedConjunction(double value, NestedConstraintExpression nested) {
            if (nested.Left is AtomicConstraintExpression lower && nested.Right is AtomicConstraintExpression upper) {
                var lowerBound = Convert.ToDouble(lower.Property.Value);
                var upperBound = Convert.ToDouble(upper.Property.Value);
                bool tooLow  = lower.ConstraintType == ConstraintType.GreaterThan ? value <= lowerBound : value < lowerBound;
                bool tooHigh = upper.ConstraintType == ConstraintType.LessThanOrEqualTo ? value > upperBound : value >= upperBound;
                return (tooLow, tooHigh);
            }
            return (false, false);
        }

        private static (bool tooLow, bool tooHigh) IsViolatedAtomic(double value, AtomicConstraintExpression atomic) {
            var bound = Convert.ToDouble(atomic.Property.Value);
            return atomic.ConstraintType switch {
                ConstraintType.GreaterThan           => (value <= bound, false),
                ConstraintType.GreaterThanOrEqualTo  => (value < bound,  false),
                ConstraintType.LessThan              => (false, value >= bound),
                ConstraintType.LessThanOrEqualTo     => (false, value > bound),
                _ => (false, false)
            };
        }
    }
}
