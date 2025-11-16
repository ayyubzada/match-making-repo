namespace MatchMaking.Shared.Constants;

public static class RedisKeys
{
    // match:{matchId}
    public const string Match = "match:{0}";

    // match:user:{userId}
    public const string MatchUser = "match:user:{0}";

    // rl:user:{userId}
    public const string RateLimit = "rl:user:{0}";

    // pending:users
    public const string PendingUsers = "pending:users";
}
