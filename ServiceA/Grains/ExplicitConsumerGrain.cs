using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using ServiceA.Interfaces;

namespace ServiceA.Grains;

public class ExplicitConsumerGrain : IGrainBase, IExplicitConsumerGrain, IAsyncObserver<TestMessage>
{
    public IGrainContext GrainContext { get; }
    
    private readonly ILogger<ExplicitConsumerGrain> _logger;
    private StreamSubscriptionHandle<TestMessage>? _streamHandle;

    public ExplicitConsumerGrain(IGrainContext grainContext, ILogger<ExplicitConsumerGrain> logger)
    {
        GrainContext = grainContext;
        _logger = logger;
    }

    public async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("activating grain ID {Id}, ensuring any existing stream handles are resumed", this.GetPrimaryKey());
        
        var streamProvider = this.GetStreamProvider(Constants.StreamProviderName);
        var stream = streamProvider.GetStream<TestMessage>(StreamId.Create(Constants.StreamNamespace, this.GetPrimaryKey()));
        var handles = await stream.GetAllSubscriptionHandles();

        if (handles is null || handles.Count == 0)
        {
            _logger.LogInformation("no handles found in grain ID {Id}, not resuming anything", this.GetPrimaryKey());
            return;
        }

        foreach (var handle in handles)
        {
            _logger.LogInformation("about to resume handle {Handle} in grain ID {Id}", handle, this.GetPrimaryKey());
            await handle.ResumeAsync(this);
            _logger.LogInformation("resumed handle {Handle} in grain ID {Id}", handle, this.GetPrimaryKey());
        }
    }
    
    public async Task StartConsumer()
    {
        if (_streamHandle is not null)
        {
            throw new InvalidOperationException("stream already set up");
        }
        
        var streamProvider = this.GetStreamProvider(Constants.StreamProviderName);
        var stream = streamProvider.GetStream<TestMessage>(StreamId.Create(Constants.StreamNamespace, this.GetPrimaryKey()));
        _streamHandle = await stream.SubscribeAsync(this);
    }

    public Task OnNextAsync(TestMessage item, StreamSequenceToken? token = null)
    {
        _logger.LogInformation("received message in grain ID {Id}: {Message}", this.GetPrimaryKey(), item.Message);
        return Task.CompletedTask;
    }

    public Task OnCompletedAsync() => Task.CompletedTask;
    public Task OnErrorAsync(Exception ex) => Task.CompletedTask;
}