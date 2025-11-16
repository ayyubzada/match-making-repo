namespace MatchMaking.Shared.Configurations;

public record KafkaConfig
{
    public string BootstrapServers { get; init; } = null!;
    public string ServiceGroupId { get; init; } = null!;
    public string WorkerGroupId { get; init; } = null!;
    public string RequestTopic { get; init; } = null!;
    public string CompleteTopic { get; init; } = null!;
};