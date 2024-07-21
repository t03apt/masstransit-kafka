using Avro.Specific;
using Confluent.Kafka;
using Contracts;
using Contracts.Serializers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

var kafkaBroker = Environment.GetEnvironmentVariable("ConnectionStrings__kafka");

builder.AddServiceDefaults();

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();
    x.AddRider(rider =>
    {
        rider.UsingKafka((context, k) =>
        {
            k.ClientId = "Producer";
            k.Host(kafkaBroker);
        });

        rider.AddProducer<SampleMessage>("sample-topic", (context, cfg) =>
        {
            cfg.SetValueSerializer(new MyAvroSerializer<SampleMessage>());
        });
    });
});

builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();

class Worker(IServiceProvider serviceProvider, ILogger<Worker> logger) : BackgroundService
{
    private static readonly string TenantId = "e7ac3c97-5f3f-4bef-b077-d0c2a6daec45";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var producer = serviceProvider.GetRequiredService<ITopicProducer<SampleMessage>>();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        long i = 0;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var message = new SampleMessage
            {
                Message = $"Hello, World! {i}"
            };

            await producer.Produce(message,
                Pipe.Execute<KafkaSendContext<SampleMessage>>(context =>
                {
                    context.Headers.Set(CustomHeaders.TenantId, TenantId);
                }));
            logger.LogInformation("Producer sent message: {Message}", message.Message);
            i++;
        }
    }
}

class MyAvroSerializer<T> : IAsyncSerializer<T>
    where T : ISpecificRecord
{
    public Task<byte[]> SerializeAsync(T data, SerializationContext context)
    {
        return Task.FromResult(AvroSerializer.Serialize(data));
    }
}