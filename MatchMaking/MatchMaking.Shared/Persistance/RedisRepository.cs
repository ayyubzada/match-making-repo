using MatchMaking.Shared.Configurations;
using MatchMaking.Shared.Constants;
using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Persistance.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace MatchMaking.Service.Persistance;

public class RedisRepository(
    ILogger<RedisRepository> logger,
    IConnectionMultiplexer redis,
    IOptions<RedisConfig> redisConfig) : IRedisRepository
{
    private readonly ILogger<RedisRepository> _logger = logger;
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly RedisConfig _redisConfig = redisConfig.Value;

    public async Task SaveMatchAsync(MatchCompleteMessage match)
    {
        _logger.LogInformation("Saving match {MatchId} for users: {UserIds}", match.MatchId, string.Join(", ", match.UserIds));

        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(match);

        foreach (var u in match.UserIds)
            await db.StringSetAsync(string.Format(RedisKeys.MatchUser, u), json);
        await db.StringSetAsync(string.Format(RedisKeys.Match, match.MatchId), json);

        _logger.LogInformation("Match {MatchId} saved successfully.", match.MatchId);
    }

    public async Task<MatchCompleteMessage?> GetMatchForUserAsync(string userId)
    {
        _logger.LogInformation("Retrieving match for userId: {UserId}", userId);

        var db = _redis.GetDatabase();
        var val = await db.StringGetAsync(string.Format(RedisKeys.MatchUser, userId));

        if (!val.HasValue)
        {
            _logger.LogInformation("No match found for userId: {userId}", userId);
            return null;
        }

        return JsonSerializer.Deserialize<MatchCompleteMessage>(val!);
    }

    public async Task<bool> CheckRateLimitAsync(string userId)
    {
        _logger.LogInformation("Checking rate limit for userId: {UserId}", userId);

        int intervalMs = _redisConfig.RateLimitIntervalMs;

        var db = _redis.GetDatabase();
        var key = string.Format(RedisKeys.RateLimit, userId);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var wasSet = await db.StringSetAsync(key,
            now,
            TimeSpan.FromMilliseconds(intervalMs),
            when: When.NotExists);

        if (!wasSet)
        {
            _logger.LogInformation("Rate limit exceeded for userId {userId} (limit: {intervalMs})", userId, intervalMs);
            return false;
        }

        _logger.LogInformation("Rate limit check passed for userId {userId}", userId);
        return true;
    }

    public async Task<string[]> TryCreateMatchAsync(string userId, int requiredPlayers)
    {
        _logger.LogInformation("Attempting to add userId {userId} to pending pool (required: {requiredPlayers})", userId, requiredPlayers);

        var db = _redis.GetDatabase();

        var luaScript = @"
            redis.call('SADD', KEYS[1], ARGV[1])
            local count = redis.call('SCARD', KEYS[1])
            if count >= tonumber(ARGV[2]) then
                local members = redis.call('SMEMBERS', KEYS[1])
                local selected = {{}}
                for i = 1, tonumber(ARGV[2]) do
                    table.insert(selected, members[i])
                    redis.call('SREM', KEYS[1], members[i])
                end
                return selected
            end
            return nil";

        var result = await db.ScriptEvaluateAsync(luaScript,
            new RedisKey[] { RedisKeys.PendingUsers },
            new RedisValue[] { userId, requiredPlayers });

        if (result.IsNull)
        {
            _logger.LogInformation("User {userId} added to pending pool, not enough players yet.", userId);
            return [];
        }

        var users = ((RedisResult[])result!)
            .Select(r => r.ToString())
            .ToArray();

        _logger.LogInformation("Match ready with users: {0}", string.Join(", ", users));
        return users;
    }
}
