namespace MatchMaking.Service.DTOs;

public record GetMatchesResult(int StatusCode, string ErrorMessage, MatchResponseDTO? Result);