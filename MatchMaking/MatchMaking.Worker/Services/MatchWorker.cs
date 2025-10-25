using Confluent.Kafka;
using MatchMaking.Shared.Configurations;
using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Helpers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace MatchMaking.Worker.Services;

public class MatchWorker(
    IOptions<KafkaConfig> kafkaOptions,
    IOptions<MatchSettings> matchSettingsOptions,
    IConnectionMultiplexer redis,
    IProducer<Null, string> producer,
    ILogger<MatchWorker> logger) : BackgroundService
{
    private readonly ILogger<MatchWorker> _logger = logger;
    private readonly IConnectionMultiplexer _redis = redis;
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
            var db = _redis.GetDatabase();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Waiting for match requests...");
                    var cr = consumer.Consume(stoppingToken);
                    var request = JsonSerializer.Deserialize<MatchRequestMessage>(cr.Message.Value);
                    if (request == null) continue;

                    await db.SetAddAsync("pending:users", request.UserId);
                    var count = await db.SetLengthAsync("pending:users");
                    if (count >= required)
                    {
                        var users = (await db.SetMembersAsync("pending:users"))
                            .Take(required)
                            .Select(x => x.ToString())
                            .ToArray();

                        foreach (var u in users)
                            await db.SetRemoveAsync("pending:users", u);

                        var match = new MatchCompleteMessage(Guid.NewGuid(), users, DateTimeOffset.UtcNow);
                        var json = JsonSerializer.Serialize(match);
                        await _producer.ProduceAsync(_kafkaConfig.CompleteTopic,
                            new Message<Null, string> { Value = json });

                        _logger.LogInformation("Created match {MatchId} for [{Users}]", match.MatchId, string.Join(",", users));
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