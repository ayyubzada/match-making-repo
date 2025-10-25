using MatchMaking.Service.DTOs;

namespace MatchMaking.Service.Services.Abstractions;

public interface IMatchService
{
    Task<RequestMatchResult> RequestMatchAsync(string userId);
    Task<GetMatchesResult> GetMatchAsync(string userId);
}
