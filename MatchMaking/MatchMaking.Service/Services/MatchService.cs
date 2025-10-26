using Confluent.Kafka;
using MatchMaking.Service.DTOs;
using MatchMaking.Service.Services.Abstractions;
using MatchMaking.Shared.Configurations;
using MatchMaking.Shared.Contracts;
using MatchMaking.Shared.Persistance.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MatchMaking.Service.Services;

public class MatchService(
    ILogger<MatchService> logger,
    IOptions<KafkaConfig> kafkaOptions,
    IProducer<Null, string> producer,
    IRedisRepository repo) : IMatchService
{
    private readonly ILogger<MatchService> _logger = logger;
    private readonly IProducer<Null, string> _producer = producer;
    private readonly IRedisRepository _repo = repo;
    private readonly KafkaConfig _kafkaConfig = kafkaOptions.Value;

    public async Task<RequestMatchResult> RequestMatchAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("RequestMatchAsync called with null or empty userId");
            return RequestMatchResult.BadRequest;
        }

        if (!await _repo.CheckRateLimitAsync(userId))
            return RequestMatchResult.TooManyRequests;

        var msg = new MatchRequestMessage(userId, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(msg);
        await _producer.ProduceAsync(_kafkaConfig.RequestTopic,
            new Message<Null, string> { Value = json });
        return RequestMatchResult.Success;
    }

    public async Task<GetMatchesResult> GetMatchAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetMatchAsync called with null or empty userId");
            return new GetMatchesResult(400, "Invalid user ID", null);
        }

        var match = await _repo.GetMatchForUserAsync(userId);

        if (match == null)
        {
            _logger.LogInformation("No match found for userId: {UserId}", userId);
            return new GetMatchesResult(404, "No match found", null);
        }

        _logger.LogInformation("Match found for userId: {UserId}, MatchId: {MatchId}", userId, match.MatchId);
        var responseDto = new MatchResponseDTO(match.MatchId, match.UserIds);
        return new GetMatchesResult(200, string.Empty, responseDto);
    }
}
