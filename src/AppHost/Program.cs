var builder = DistributedApplication.CreateBuilder(args);

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
