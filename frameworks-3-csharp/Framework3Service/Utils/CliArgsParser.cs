namespace Framework3Service.Utils;

public static class CliArgsParser
{
    public static Dictionary<string, string> Parse(IReadOnlyList<string> argv)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < argv.Count; i++)
        {
            var token = argv[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var body = token[2..];
            if (body.Length == 0)
            {
                continue;
            }

            var eqIndex = body.IndexOf('=');
            if (eqIndex >= 0)
            {
                var key = body[..eqIndex];
                var value = body[(eqIndex + 1)..];
                result[key] = value;
                continue;
            }

            if (i + 1 < argv.Count && !argv[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                result[body] = argv[i + 1];
                i++;
            }
            else
            {
                result[body] = "true";
            }
        }

        return result;
    }
}
