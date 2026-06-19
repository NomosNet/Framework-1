namespace Framework3Service.Configuration;

public static class ConfigValidator
{
    private static readonly HashSet<string> AllowedModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "learning",
        "production"
    };

    public static ValidationResult Validate(AppConfig config)
    {
        var errors = new List<string>();

        if (!AllowedModes.Contains(config.Mode))
        {
            errors.Add("mode must be learning or production");
        }

        if (config.Port is < 1 or > 65535)
        {
            errors.Add("port must be an integer in range 1..65535");
        }

        if (config.TrustedOrigins.Count == 0)
        {
            errors.Add("trustedOrigins must be a non-empty array");
        }
        else
        {
            var badOrigin = config.TrustedOrigins.FirstOrDefault(origin => !IsValidHttpOrigin(origin));
            if (badOrigin is not null)
            {
                errors.Add($"trusted origin is invalid: {badOrigin}");
            }
        }

        if (config.RateLimit is null)
        {
            errors.Add("rateLimit must be an object");
        }
        else
        {
            if (!IsPositiveInt(config.RateLimit.WindowMs))
            {
                errors.Add("rateLimit.windowMs must be a positive integer");
            }

            if (!IsPositiveInt(config.RateLimit.ReadMax))
            {
                errors.Add("rateLimit.readMax must be a positive integer");
            }

            if (!IsPositiveInt(config.RateLimit.CreateMax))
            {
                errors.Add("rateLimit.createMax must be a positive integer");
            }
        }

        return new ValidationResult
        {
            Ok = errors.Count == 0,
            Errors = errors
        };
    }

    private static bool IsPositiveInt(int value)
    {
        return value > 0;
    }

    private static bool IsValidHttpOrigin(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
