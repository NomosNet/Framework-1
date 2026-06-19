namespace Framework3Service.Configuration;

public sealed class AppConfig
{
    public string Mode { get; set; } = "learning";
    public int Port { get; set; } = 3000;
    public List<string> TrustedOrigins { get; set; } = [];
    public bool VerboseErrors { get; set; }
    public RateLimitConfig RateLimit { get; set; } = new();
}

public sealed class RateLimitConfig
{
    public int WindowMs { get; set; }
    public int ReadMax { get; set; }
    public int CreateMax { get; set; }
}
