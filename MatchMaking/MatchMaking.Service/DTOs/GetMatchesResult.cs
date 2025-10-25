using MatchMaking.Shared.Contracts;

namespace MatchMaking.Service.DTOs;

public record GetMatchesResult(int StatusCode, string ErrorMessage, MatchCompleteMessage? Result);