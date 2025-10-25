namespace MatchMaking.Shared.Configurations;

public record KafkaConfig(
    string BootstrapServers,
    string ServiceGroupId,
    string WorkerGroupId,
    string RequestTopic,
    string CompleteTopic);