using Confluent.Kafka;
using MatchMaking.Service.Services.Abstractions;
using MatchMaking.Shared.Configurations;
using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Repositories.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MatchMaking.Service.Services;

public class MatchService(
    IOptions<KafkaConfig> kafkaOptions,
    IProducer<Null, string> producer,
    IRedisRepository repo) : IMatchService
{
    private readonly IProducer<Null, string> _producer = producer;
    private readonly IRedisRepository _repo = repo;
    private readonly KafkaConfig _kafkaConfig = kafkaOptions.Value;

    public async Task<bool> RequestMatchAsync(string userId)
    {
        if (!await _repo.CheckRateLimitAsync(userId))
            return false;

        var msg = new MatchRequestMessage(userId, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(msg);
        await _producer.ProduceAsync(_kafkaConfig.RequestTopic,
            new Message<Null, string> { Value = json });
        return true;
    }

    public Task<MatchCompleteMessage?> GetMatchAsync(string userId) =>
        _repo.GetMatchForUserAsync(userId);
}
