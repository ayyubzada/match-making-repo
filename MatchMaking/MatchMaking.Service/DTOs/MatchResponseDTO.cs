namespace MatchMaking.Service.DTOs;

public record MatchResponseDTO(
    Guid MatchId,
    string[] UserIds
);