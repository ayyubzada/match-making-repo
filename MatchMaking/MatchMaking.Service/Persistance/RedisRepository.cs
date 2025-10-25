using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Repositories.Abstractions;
using StackExchange.Redis;
using System.Text.Json;

namespace MatchMaking.Service.Persistance;

public class RedisRepository(
    ILogger<RedisRepository> logger,
    IConnectionMultiplexer redis) : IRedisRepository
{
    private readonly ILogger<RedisRepository> _logger = logger;
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task SaveMatchAsync(MatchCompleteMessage match)
    {
        _logger.LogInformation("Saving match {MatchId} for users: {UserIds}", match.MatchId, string.Join(", ", match.UserIds));

        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(match);
        foreach (var u in match.UserIds)
            await db.StringSetAsync($"match:user:{u}", json);
        await db.StringSetAsync($"match:{match.MatchId}", json);
    }

    public async Task<MatchCompleteMessage?> GetMatchForUserAsync(string userId)
    {
        _logger.LogInformation("Retrieving match for userId: {UserId}", userId);

        var db = _redis.GetDatabase();
        var val = await db.StringGetAsync($"match:user:{userId}");
        return val.HasValue ? JsonSerializer.Deserialize<MatchCompleteMessage>(val!) : null;
    }

    public async Task<bool> CheckRateLimitAsync(string userId, int intervalMs = 100)
    {
        _logger.LogInformation("Checking rate limit for userId: {UserId}", userId);

        var db = _redis.GetDatabase();
        var key = $"rl:user:{userId}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var last = await db.StringGetAsync(key);

        _logger.LogInformation("Last request time for userId {UserId}: {LastRequestTime}", userId, last.HasValue ? last.ToString() : "none");
        if (last.HasValue && now - (long)last < intervalMs)
            return false;
        await db.StringSetAsync(key, now, TimeSpan.FromSeconds(5));
        return true;
    }
}
