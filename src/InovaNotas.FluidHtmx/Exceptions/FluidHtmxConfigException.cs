namespace InovaNotas.FluidHtmx.Exceptions;

public class FluidHtmxConfigException : FluidHtmxException
{
    public FluidHtmxConfigException(string message) : base(message) { }

    public FluidHtmxConfigException(string message, Exception innerException)
        : base(message, innerException) { }
}
