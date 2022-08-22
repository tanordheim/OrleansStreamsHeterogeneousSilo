using Orleans;

namespace ServiceA.Interfaces;

public interface IExplicitConsumerGrain : IGrainWithGuidKey
{
    Task StartConsumer();
}