using Confluent.Kafka;
using MatchMaking.Shared.Configurations;
using MatchMaking.Worker.Services;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaConfig>(builder.Configuration.GetSection(nameof(KafkaConfig)));
builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection(nameof(RedisConfig)));
builder.Services.Configure<MatchSettings>(builder.Configuration.GetSection(nameof(MatchSettings)));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(builder.Configuration["RedisConfig:ConnectionString"]!)
);
builder.Services.AddSingleton<IProducer<Null, string>>(
    _ => new ProducerBuilder<Null, string>(
        new ProducerConfig { BootstrapServers = builder.Configuration["KafkaConfig:BootstrapServers"]! }
    ).Build()
);
builder.Services.AddHostedService<MatchWorker>();

var host = builder.Build();
host.Run();
