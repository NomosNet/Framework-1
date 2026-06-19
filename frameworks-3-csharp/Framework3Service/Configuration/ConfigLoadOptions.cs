namespace Framework3Service.Configuration;

public sealed class ConfigLoadOptions
{
    public string? WorkingDirectory { get; set; }
    public IReadOnlyList<string>? Args { get; set; }
    public IReadOnlyDictionary<string, string?>? Environment { get; set; }
}
