using Orleans;
using Orleans.Runtime;
using ServiceB.Interfaces;

namespace ServiceB.Grains;

public class FooGrain : IGrainBase, IFooGrain
{
    public IGrainContext GrainContext { get; }

    public FooGrain(IGrainContext grainContext)
    {
        GrainContext = grainContext;
    }
}