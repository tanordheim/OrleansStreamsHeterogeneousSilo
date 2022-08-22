using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using ServiceA;
using ServiceA.Interfaces;

var app = Host.CreateDefaultBuilder(args)
    .UseOrleans((ctx, siloBuilder) =>
    {
        siloBuilder
            .UseInMemoryReminderService()
            .UseDynamoDBClustering(options =>
            {
                options.TableName = "test-cluster-state";
                options.Service = ctx.Configuration.GetConnectionString("dynamodb") ?? "http://localhost:8000";
            })
            .AddDynamoDBGrainStorage("PubSubStore", options =>
            {
                options.TableName = "test-pub-sub-state";
                options.Service = ctx.Configuration.GetConnectionString("dynamodb") ?? "http://localhost:8000";
            })
            .AddMemoryStreams<DefaultMemoryMessageBodySerializer>(Constants.StreamProviderName)
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "test-cluster";
                options.ServiceId = "service-a";
            })
            .Configure<EndpointOptions>(options =>
            {
                var ipAddress = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipAddress is null)
                    throw new Exception("unable to find public ipv4 address");
                options.AdvertisedIPAddress = ipAddress;
                options.SiloPort = 11111;
                options.GatewayPort = 30000;
            });
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IHostedService, StartPublisherHostedService>();
    })
    .Build();

await app.RunAsync();

internal class StartPublisherHostedService : BackgroundService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<StartPublisherHostedService> _logger;

    public StartPublisherHostedService(IClusterClient clusterClient, ILogger<StartPublisherHostedService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("preparing to start publisher grain");
        var publisherGrain = _clusterClient.GetGrain<IPublisherGrain>(Guid.Empty);
        await publisherGrain.StartPublishing();
        _logger.LogInformation("started publisher grain");
    }
}