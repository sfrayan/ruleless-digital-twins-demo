namespace Logic.Mapek.Proactive;

public sealed record PriceSlot(DateTimeOffset Start, DateTimeOffset End, double Price);

public sealed class PriceForecast
{
    public bool Available { get; init; }
    public string Source { get; init; } = "";
    public string Area { get; init; } = "";
    public string Currency { get; init; } = "";
    public string Timezone { get; init; } = "";
    public IReadOnlyList<PriceSlot> Slots { get; init; } = Array.Empty<PriceSlot>();
    public string? Warning { get; init; }
}

public interface IPriceForecastProvider
{
    Task<PriceForecast> GetForecastAsync(CancellationToken ct = default);
}
