using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Repositories.Abstractions;
using StackExchange.Redis;
using System.Text.Json;

namespace MatchMaking.Service.Persistance;

public class RedisRepository(IConnectionMultiplexer redis) : IRedisRepository
{
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task SaveMatchAsync(MatchCompleteMessage match)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(match);
        foreach (var u in match.UserIds)
            await db.StringSetAsync($"match:user:{u}", json);
        await db.StringSetAsync($"match:{match.MatchId}", json);
    }

    public async Task<MatchCompleteMessage?> GetMatchForUserAsync(string userId)
    {
        var db = _redis.GetDatabase();
        var val = await db.StringGetAsync($"match:user:{userId}");
        return val.HasValue ? JsonSerializer.Deserialize<MatchCompleteMessage>(val!) : null;
    }

    public async Task<bool> CheckRateLimitAsync(string userId, int intervalMs = 100)
    {
        var db = _redis.GetDatabase();
        var key = $"rl:user:{userId}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var last = await db.StringGetAsync(key);
        if (last.HasValue && now - (long)last < intervalMs)
            return false;
        await db.StringSetAsync(key, now, TimeSpan.FromSeconds(5));
        return true;
    }
}
