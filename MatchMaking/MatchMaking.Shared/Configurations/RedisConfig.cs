namespace MatchMaking.Shared.Configurations;

public record RedisConfig
{
    public string ConnectionString { get; init; } = null!;
    public int RateLimitIntervalMs { get; init; } = 100;
}