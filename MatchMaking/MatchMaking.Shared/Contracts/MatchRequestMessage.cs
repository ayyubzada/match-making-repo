namespace MatchMaking.Shared.Contracts;

public record MatchRequestMessage(string UserId, DateTimeOffset Timestamp);