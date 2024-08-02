namespace AppHost;

public static class CollectorExtensions
{
    public static IResourceBuilder<ContainerResource> AddPrometheus(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new ContainerResource(name);
        return builder.AddResource(resource)
            .WithImage("prom/prometheus")
            .WithEndpoint(port: 9090, targetPort: 9090, name: "http", scheme: "http")
            .WithBindMount("prometheus.yaml", "/etc/prometheus/prometheus.yaml")
            .WithArgs("--config.file=/etc/prometheus/prometheus.yaml");
    }

    public static IResourceBuilder<ContainerResource> AddGrafana(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new ContainerResource(name);
        return builder.AddResource(resource)
            .WithImage("grafana/grafana")
            .WithEndpoint(port: 3000, targetPort: 3000, name: "http", scheme: "http")
            .WithBindMount("./grafana/data/", "/var/lib/grafana")
            .WithBindMount("./grafana/provisioning/", "/etc/grafana/provisioning/")
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
            .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false");
    }
}

