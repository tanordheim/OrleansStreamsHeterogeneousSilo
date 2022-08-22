using Orleans;

namespace ServiceA.Interfaces;

[GenerateSerializer]
[Immutable]
public class TestMessage
{
    [Id(0)]
    public string Message { get; set; }
}