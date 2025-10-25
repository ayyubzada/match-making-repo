using MatchMaking.Shared.Contracts;

namespace MatchMaking.Service.Services.Abstractions;

public interface IMatchService
{
    Task<bool> RequestMatchAsync(string userId);
    Task<MatchCompleteMessage?> GetMatchAsync(string userId);
}
