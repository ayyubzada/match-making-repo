using MatchMaking.Shared.Contracts;

namespace MatchMaking.Shared.Repositories.Abstractions;

public interface IRedisRepository
{
    Task SaveMatchAsync(MatchCompleteMessage match);
    Task<MatchCompleteMessage?> GetMatchForUserAsync(string userId);
    Task<bool> CheckRateLimitAsync(string userId, int intervalMs = 100);
}
