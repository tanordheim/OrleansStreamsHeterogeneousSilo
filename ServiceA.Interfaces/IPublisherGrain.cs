using Orleans;

namespace ServiceA.Interfaces;

public interface IPublisherGrain : IGrainWithGuidKey
{
    Task StartPublishing();
}