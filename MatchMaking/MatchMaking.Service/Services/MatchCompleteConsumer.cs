using Confluent.Kafka;
using MatchMaking.Shared.Configurations;
using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Helpers;
using MatchMaking.Shared.Repositories.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MatchMaking.Service.Services;

public class MatchCompleteConsumer(
    IOptions<KafkaConfig> kafkaOptions,
    IRedisRepository repo,
    ILogger<MatchCompleteConsumer> logger) : BackgroundService
{
    private readonly ILogger<MatchCompleteConsumer> _logger = logger;
    private readonly IRedisRepository _repo = repo;
    private readonly KafkaConfig _kafkaConfig = kafkaOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (KafkaAdmin admin = new(_kafkaConfig.BootstrapServers, _logger))
        {
            await admin.EnsureTopicExistsAsync(_kafkaConfig.RequestTopic);
            await admin.EnsureTopicExistsAsync(_kafkaConfig.CompleteTopic);
        }

        var consumer = new ConsumerBuilder<Ignore, string>(
            new ConsumerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                GroupId = _kafkaConfig.ServiceGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).Build();

        consumer.Subscribe(_kafkaConfig.CompleteTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Waiting for match completions...");
            try
            {
                var cr = consumer.Consume(stoppingToken);
                var match = JsonSerializer.Deserialize<MatchCompleteMessage>(cr.Message.Value);
                if (match is not null)
                    await _repo.SaveMatchAsync(match);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming match.complete");
            }
        }
    }
}
