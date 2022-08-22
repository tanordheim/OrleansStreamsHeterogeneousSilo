using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using ServiceA.Interfaces;

namespace ServiceA.Grains;

public class PublisherGrain : IGrainBase, IPublisherGrain, IRemindable
{
    public IGrainContext GrainContext { get; }

    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<PublisherGrain> _logger;
    private IAsyncStream<TestMessage>? _stream;

    public PublisherGrain(IGrainContext grainContext, IGrainFactory grainFactory, ILogger<PublisherGrain> logger)
    {
        GrainContext = grainContext;
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider(Constants.StreamProviderName);
        _stream = streamProvider.GetStream<TestMessage>(this.GetPrimaryKey(), Constants.StreamNamespace);
        return Task.CompletedTask;
    }
    
    public async Task StartPublishing()
    {
        _logger.LogInformation("starting to publish for grain ID {Id}", this.GetPrimaryKey());
        await this.RegisterOrUpdateReminder("PublishMessage", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        var subscriberGrain = _grainFactory.GetGrain<IExplicitConsumerGrain>(this.GetPrimaryKey());
        await subscriberGrain.StartConsumer();
        _logger.LogInformation("asked consumer grain to start consuming for grain ID {Id}", this.GetPrimaryKey());

        await SendMessage();
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        await SendMessage();
    }

    private async Task SendMessage()
    {
        _logger.LogInformation("publishing new message for grain ID {Id}", this.GetPrimaryKey());
        await _stream!.OnNextAsync(new TestMessage
        {
            Message = $"Hello from grain {this.GetPrimaryKey()} at {DateTime.Now.ToString("s")}"
        });
    }
}