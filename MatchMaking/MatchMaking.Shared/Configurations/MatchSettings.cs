namespace MatchMaking.Shared.Configurations;

public record MatchSettings
{
    public int PlayersPerMatch { get; init; } = 3;
}