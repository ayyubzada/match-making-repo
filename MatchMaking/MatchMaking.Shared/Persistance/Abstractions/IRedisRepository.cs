using MatchMaking.Shared.Contracts;

namespace MatchMaking.Shared.Persistance.Abstractions;

public interface IRedisRepository
{
    Task SaveMatchAsync(MatchCompleteMessage match);
    Task<MatchCompleteMessage?> GetMatchForUserAsync(string userId);
    Task<bool> CheckRateLimitAsync(string userId);
    Task<string[]> TryCreateMatchAsync(string userId, int requiredPlayers);
}
