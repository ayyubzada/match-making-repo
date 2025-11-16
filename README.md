# MatchMaking System

A distributed matchmaking system built with .NET 9 that groups users into matches. The system uses Kafka for communication between services and Redis for data storage and rate limiting.

## Architecture

The solution consists of three projects:

**MatchMaking.Service**
An HTTP API that handles match requests and provides match information to users. It enforces rate limiting and consumes completed matches from Kafka.

**MatchMaking.Worker**
A background service that processes match requests from Kafka. It maintains a pending users pool in Redis and creates matches when the required number of users is available. Multiple worker instances can run concurrently. Currently we have set it for 2 instances.

**MatchMaking.Shared**
A shared library containing contracts, configurations, and repository implementations used by both services.

### Technology Stack

- .NET 9
- ASP.NET Core
- Apache Kafka
- Redis
- Docker and Docker Compose

## API Endpoints

**Request Match**
```
POST /api/match/request
Header: UserId: {userId}
```

Response: 204 No Content (success), 400 Bad Request, 429 Too Many Requests or 500 with Unexpected Internal Error

**Get Match Information**
```
GET /api/match/matches
Header: UserId: {userId}
```

Response: 200 OK with match data, 400 Bad Request, 404 Not Found or 500 with Unexpected Internal Error

Response Body:
```json
{
  "matchId": "45ae548e-d72f-438d-bf1a-f1692a699a81",
  "userIds": ["user1", "user2", "user3"]
}
```

## Quick Start

### Prerequisites

- Docker and Docker Compose
- Ports 5000, 6379, and 9092 available

### Running with Docker Compose

Build and start all services:
```bash
docker-compose up --build
```

The API will be available at http://localhost:5000

Stop the system:
```bash
docker-compose down
```

## Configuration

Configuration can be modified in appsettings.json files or via environment variables.

**Key Configuration Parameters**

RedisConfig.RateLimitIntervalMs
- Rate limit interval in milliseconds (default: 100)
- Users can make 1 request per interval

MatchSettings.PlayersPerMatch
- Number of users required per match (default: 3)

**Environment Variable Override Example**

Edit docker-compose.yml:
```yaml
matchmaking.service:
  environment:
    - RedisConfig__RateLimitIntervalMs=150
```

Or edit appsettings.json:
```json
{
  "RedisConfig": {
    "RateLimitIntervalMs": 150
  },
  "MatchSettings": {
    "PlayersPerMatch": 5
  }
}
```

## Testing

The project includes a MatchMaking.Service.http file with pre-configured requests for testing endpoints. This file works with Visual Studio 2022+ or VS Code with the REST Client extension.

## System Flow

1. User sends match request to Service API
2. Service validates and checks rate limit
3. Service publishes request to Kafka
4. Worker consumes request and adds user to pending pool
5. When enough users available, Worker creates match
6. Worker publishes completion to Kafka
7. Service consumes completion and stores in Redis
8. User retrieves match information via API

## Redis Data Structure

- match:{matchId} - Match data by ID
- match:user:{userId} - Match data for user lookup
- rl:user:{userId} - Rate limiting key
- pending:users - Set of users waiting in pool

## Rate Limiting

The system enforces per-user rate limiting using Redis atomic operations. The rate limit can be adjusted in configuration without code changes. Default is 100ms (10 requests per second per user).

## Repository Pattern

**IRedisRepository**
Handles match storage and rate limiting for the Service. Manages pending user pool for the Worker using Lua scripts for atomic operations.

## Error Handling

The Service includes global exception handling middleware that catches unhandled exceptions and returns consistent error responses.

## Scalability

The system supports horizontal scaling:
- Multiple Service instances can run behind a load balancer
- Multiple Worker instances use Kafka consumer groups and Redis atomic operations to prevent race conditions
- Redis connection pooling via IConnectionMultiplexer singleton