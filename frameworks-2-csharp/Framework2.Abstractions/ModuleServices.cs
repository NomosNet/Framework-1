namespace Framework2.Abstractions;

public interface IAuditTrail
{
    void Write(string message);

    IReadOnlyList<string> Entries { get; }
}

public interface IValidationService
{
    IReadOnlyList<int> ValidateScores(IEnumerable<int> scores);
}

public interface IReportExporter
{
    void Export(IReadOnlyList<int> validatedScores);
}
