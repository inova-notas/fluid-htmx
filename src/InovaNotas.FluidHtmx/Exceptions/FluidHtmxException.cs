namespace InovaNotas.FluidHtmx.Exceptions;

public class FluidHtmxException : Exception
{
    public FluidHtmxException(string message) : base(message) { }

    public FluidHtmxException(string message, Exception innerException)
        : base(message, innerException) { }
}
