namespace InovaNotas.FluidHtmx.Exceptions;

public class TemplateParseException : FluidHtmxException
{
    public string TemplateName { get; }

    public TemplateParseException(string templateName, string message)
        : base($"Failed to parse template '{templateName}': {message}")
    {
        TemplateName = templateName;
    }

    public TemplateParseException(string templateName, string message, Exception innerException)
        : base($"Failed to parse template '{templateName}': {message}", innerException)
    {
        TemplateName = templateName;
    }
}
