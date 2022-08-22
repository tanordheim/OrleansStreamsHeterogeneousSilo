using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;

var app = Host.CreateDefaultBuilder(args)
    .UseOrleans((ctx, siloBuilder) =>
    {
        siloBuilder
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
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "test-cluster";
                options.ServiceId = "service-b";
            })
            .Configure<EndpointOptions>(options =>
            {
                options.AdvertisedIPAddress = IPAddress.Loopback;
                options.SiloPort = 11112;
                options.GatewayPort = 30001;
            });
    })
    .Build();

await app.RunAsync();