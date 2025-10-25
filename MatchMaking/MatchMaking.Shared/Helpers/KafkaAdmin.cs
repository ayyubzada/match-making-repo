using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Shared.Helpers;

public class KafkaAdmin : IDisposable
{
    private readonly IAdminClient _adminClient;
    private readonly ILogger _logger;

    public KafkaAdmin(string bootstrapServers, ILogger logger)
    {
        _logger = logger;
        var config = new AdminClientConfig { BootstrapServers = bootstrapServers };
        _adminClient = new AdminClientBuilder(config).Build();
    }

    public async Task EnsureTopicExistsAsync(string topicName, int partitions = 1, short replicationFactor = 1)
    {
        try
        {
            _logger.LogInformation("Checking if topic '{TopicName}' exists...", topicName);
            var topics = _adminClient.GetMetadata(TimeSpan.FromSeconds(10)).Topics;

            if (topics.Any(t => t.Topic == topicName))
            {
                _logger.LogInformation("Topic '{TopicName}' already exists.", topicName);
                return;
            }

            _logger.LogWarning("Topic '{TopicName}' not found. Creating it now.", topicName);
            await _adminClient.CreateTopicsAsync(new TopicSpecification[]
            {
                new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = partitions,
                    ReplicationFactor = replicationFactor
                }
            });
            _logger.LogInformation("Topic '{TopicName}' created successfully.", topicName);
        }
        catch (CreateTopicsException ex)
        {
            // Ignore if the topic already exists due to a race condition with another service
            if (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                _logger.LogInformation("Topic '{TopicName}' was created by another process.", topicName);
            else
            {
                _logger.LogError(ex, "Failed to create topic '{TopicName}'.", topicName);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while managing topic '{TopicName}'.", topicName);
            throw;
        }
    }

    public void Dispose()
    {
        _adminClient?.Dispose();
    }
}
