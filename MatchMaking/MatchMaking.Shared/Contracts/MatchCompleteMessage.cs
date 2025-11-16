namespace MatchMaking.Shared.Contracts;

public record MatchCompleteMessage(Guid MatchId, string[] UserIds, DateTimeOffset CreatedAt);