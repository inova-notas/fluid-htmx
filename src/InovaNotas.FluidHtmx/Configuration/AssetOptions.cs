namespace InovaNotas.FluidHtmx.Configuration;

public class AssetOptions
{
    public bool TailwindEnabled { get; set; }
    public string TailwindVersion { get; set; } = "4.2";
    public string InputCss { get; set; } = "Styles/app.css";
    public string OutputCss { get; set; } = "wwwroot/css/app.css";
    public bool DaisyUIEnabled { get; set; }
    public List<string> DaisyUIThemes { get; set; } = [];

    public AssetOptions EnableTailwind(string? version = null)
    {
        TailwindEnabled = true;
        if (version is not null)
            TailwindVersion = version;
        return this;
    }
}
