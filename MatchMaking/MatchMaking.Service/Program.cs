using Confluent.Kafka;
using MatchMaking.Service.Middlewares;
using MatchMaking.Service.Persistance;
using MatchMaking.Service.Services;
using MatchMaking.Service.Services.Abstractions;
using MatchMaking.Shared.Configurations;
using MatchMaking.Shared.Repositories.Abstractions;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<KafkaConfig>(builder.Configuration.GetSection(nameof(KafkaConfig)));
builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection(nameof(RedisConfig)));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(builder.Configuration["RedisConfig:ConnectionString"]!)
);
builder.Services.AddSingleton<IRedisRepository, RedisRepository>();
builder.Services.AddSingleton<IProducer<Null, string>>(
    _ => new ProducerBuilder<Null, string>(
        new ProducerConfig { BootstrapServers = builder.Configuration["KafkaConfig:BootstrapServers"]! }
    ).Build()
);
builder.Services.AddHostedService<MatchCompleteConsumer>();
builder.Services.AddScoped<IMatchService, MatchService>();

var app = builder.Build();


app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
