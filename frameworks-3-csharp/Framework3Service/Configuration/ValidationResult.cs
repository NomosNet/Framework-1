namespace Framework3Service.Configuration;

public sealed class ValidationResult
{
    public bool Ok { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
