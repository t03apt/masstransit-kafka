using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Consumer;
using Contracts;
using Contracts.Serializers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

var kafkaBroker = Environment.GetEnvironmentVariable("ConnectionStrings__kafka");
var topic = "sample-topic";

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(Instrumentation.ActivitySource.Name);
    });

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();

    x.AddRider(rider =>
    {
        rider.AddConsumer<ConsumerWorker>();

        rider.UsingKafka((context, k) =>
        {
            k.ClientId = "Consumer";
            k.Host(kafkaBroker);

            k.TopicEndpoint<SampleMessage>(topic, "my-consumer-group", e =>
            {
                e.SetValueDeserializer(new MyAvroDeserializer<SampleMessage>());
                e.ConfigureConsumer<ConsumerWorker>(context);
            });
        });
    });
});

await EnsureTopicExists(kafkaBroker, topic);

await builder.Build().RunAsync();

static async Task<IAdminClient> EnsureTopicExists(string? kafkaBroker, string topic)
{
    using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = kafkaBroker }).Build();
    var spec = new TopicSpecification { Name = topic, ReplicationFactor = 1, NumPartitions = 1 };
    try
    {
        await adminClient.CreateTopicsAsync([spec]);
    }
    catch (CreateTopicsException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
    }

    return adminClient;
}

sealed class ConsumerWorker(ILogger<ConsumerWorker> logger) : IConsumer<SampleMessage>
{
    private readonly Random random = new();

    public async Task Consume(ConsumeContext<SampleMessage> context)
    {
        logger.LogInformation("Received message: {Message}", context.Message.Message);

        using var activity = Instrumentation.ActivitySource.StartActivity("SomeWork");
        if (context.TryGetHeader<string>(CustomHeaders.TenantId, out var tenantId))
        {
            activity?.SetTag("custom.tenantid", tenantId);
        }

        // wait like we were doing something with the message
        await Task.Delay(random.Next(100, 1000));
    }
}

class MyAvroDeserializer<T> : IDeserializer<T>
    where T : ISpecificRecord, new()
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return AvroSerializer.Deserialize<T>(data.ToArray());
    }
}


