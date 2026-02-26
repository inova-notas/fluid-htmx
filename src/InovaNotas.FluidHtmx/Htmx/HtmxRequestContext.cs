namespace InovaNotas.FluidHtmx.Htmx;

public class HtmxRequestContext
{
    internal const string ItemsKey = "__HtmxRequestContext";

    public bool IsHtmx { get; init; }
    public bool IsBoosted { get; init; }
    public string? CurrentUrl { get; init; }
    public bool IsHistoryRestoreRequest { get; init; }
    public string? Prompt { get; init; }
    public string? Target { get; init; }
    public string? TriggerName { get; init; }
}
