using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddOpenTelemetryCollector("collector", "config.yaml")
    .WithEndpoint(8889, 8889)
    .WithAppForwarding();

builder.AddPrometheus("prometheus");
builder.AddGrafana("grafana");

var kafka = builder
    .AddKafka("kafka")
    .WithKafkaUI();

builder.AddProject<Projects.Producer>("producer")
    .WithReference(kafka)
    .WithArgs(kafka.Resource.Name);

builder.AddProject<Projects.Consumer>("consumer")
    .WithReference(kafka)
    .WithArgs(kafka.Resource.Name);

builder.Build().Run();

