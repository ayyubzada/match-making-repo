using Confluent.Kafka;
using MatchMaking.Shared.Configurations;
using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Helpers;
using MatchMaking.Shared.Persistance.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MatchMaking.Worker.Services;

public class MatchWorker(
    IOptions<KafkaConfig> kafkaOptions,
    IOptions<MatchSettings> matchSettingsOptions,
    IRedisRepository repo,
    IProducer<Null, string> producer,
    ILogger<MatchWorker> logger) : BackgroundService
{
    private readonly ILogger<MatchWorker> _logger = logger;
    private readonly IRedisRepository _repo = repo;
    private readonly IProducer<Null, string> _producer = producer;

    private readonly KafkaConfig _kafkaConfig = kafkaOptions.Value;
    private readonly MatchSettings _matchSettings = matchSettingsOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MatchWorker starting...");
        try
        {
            using (KafkaAdmin admin = new(_kafkaConfig.BootstrapServers, _logger))
            {
                await admin.EnsureTopicExistsAsync(_kafkaConfig.RequestTopic);
                await admin.EnsureTopicExistsAsync(_kafkaConfig.CompleteTopic);
            }

            var required = _matchSettings.PlayersPerMatch;
            var consumer = new ConsumerBuilder<Ignore, string>(
                new ConsumerConfig
                {
                    BootstrapServers = _kafkaConfig.BootstrapServers,
                    GroupId = _kafkaConfig.WorkerGroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }).Build();

            consumer.Subscribe(_kafkaConfig.RequestTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Waiting for match requests...");

                    var cr = consumer.Consume(stoppingToken);
                    var request = JsonSerializer.Deserialize<MatchRequestMessage>(cr.Message.Value);
                    if (request == null)
                    {
                        _logger.LogWarning("Received null or invalid match request message");
                        continue;
                    }

                    _logger.LogInformation("Processing match request for UserId: {UserId}", request.UserId);

                    var matchedUsers = await _repo.TryCreateMatchAsync(request.UserId, required);
                    if (matchedUsers?.Any() ?? false)
                    {
                        var match = new MatchCompleteMessage(Guid.NewGuid(), matchedUsers, DateTimeOffset.UtcNow);
                        var json = JsonSerializer.Serialize(match);

                        await _producer.ProduceAsync(_kafkaConfig.CompleteTopic,
                            new Message<Null, string> { Value = json });

                        _logger.LogInformation("Created match {MatchId} for [{matchedUsers}]", match.MatchId, string.Join(",", matchedUsers));
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("MatchWorker is cancelling...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected Error while processing match requests");
                }
            }

            _logger.LogInformation("MatchWorker is stopping...");
            consumer.Close();
            consumer.Dispose();
            _logger.LogInformation("MatchWorker has stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal Error in MatchWorker");
            throw;
        }

    }
}