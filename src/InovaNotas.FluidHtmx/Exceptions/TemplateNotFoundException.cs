namespace InovaNotas.FluidHtmx.Exceptions;

public class TemplateNotFoundException : FluidHtmxException
{
    public string TemplateName { get; }

    public TemplateNotFoundException(string templateName)
        : base($"Template '{templateName}' was not found in local or embedded resources.")
    {
        TemplateName = templateName;
    }
}
