# Orleans Streaming heterogeneous silo issue demo

Showcases some issues I'm having with getting an Orleans cluster up and running composed of several different services with different capabilities. It consists of two services:

### Service A

This service uses:

- DynamoDB clustering
- DynamoDB pub sub storage
- In memory reminders
- In memory streams

It has one publisher grain that, when called, will call the subscriber grain to ask it to subscribe to the stream, then every time the reminder ticks it will send a message on that stream.

### Service B

This service uses:

- DynamoDB clustering

It has has no grain storage, reminders or streams configured. It has a dummy grain but its never activated.

## The problem

When starting Service B first, then Service A, Service A crashes on startup with the following exception:

```
fail: Microsoft.Extensions.Hosting.Internal.Host[9]
      BackgroundService failed
      System.IO.FileNotFoundException: Could not load file or assembly 'Orleans.Reminders, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
      
         at System.Reflection.RuntimeAssembly.InternalLoad(ObjectHandleOnStack assemblyName, ObjectHandleOnStack requestingAssembly, StackCrawlMarkHandle stackMark, Boolean throwOnFileNotFound, ObjectHandleOnStack assemblyLoadContext, ObjectHandleOnStack retAssembly)
         at System.Reflection.RuntimeAssembly.InternalLoad(AssemblyName assemblyName, RuntimeAssembly requestingAssembly, StackCrawlMark& stackMark, Boolean throwOnFileNotFound, AssemblyLoadContext assemblyLoadContext)
         at System.Reflection.Assembly.Load(AssemblyName assemblyRef)
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.<TryPerformUncachedTypeResolution>g__ResolveAssembly|7_0(AssemblyName assemblyName) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 136
         at System.TypeNameParser.ResolveAssembly(String asmName, Func`2 assemblyResolver, Boolean throwOnError, StackCrawlMark& stackMark)
         at System.TypeNameParser.ConstructType(Func`2 assemblyResolver, Func`4 typeResolver, Boolean throwOnError, Boolean ignoreCase, StackCrawlMark& stackMark)
         at System.TypeNameParser.GetType(String typeName, Func`2 assemblyResolver, Func`4 typeResolver, Boolean throwOnError, Boolean ignoreCase, StackCrawlMark& stackMark)
         at System.Type.GetType(String typeName, Func`2 assemblyResolver, Func`4 typeResolver, Boolean throwOnError)
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.TryPerformUncachedTypeResolution(String fullName, Type& type, Assembly[] assemblies) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 107
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.TryPerformUncachedTypeResolution(String name, Type& type) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 51
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.TryResolveType(String name, Type& type) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 39
         at Orleans.Serialization.TypeSystem.TypeConverter.ParseInternal(String formatted, Type& type) in /_/src/Orleans.Serialization/TypeSystem/TypeConverter.cs:line 329
         at Orleans.Serialization.TypeSystem.TypeCodec.TryRead[TInput](Reader`1& reader, Type& type) in /_/src/Orleans.Serialization/TypeSystem/TypeCodec.cs:line 113
         at Orleans.Serialization.Codecs.FieldHeaderCodec.ReadType[TInput](Reader`1& reader, SchemaType schemaType) in /_/src/Orleans.Serialization/Codecs/FieldHeaderCodec.cs:line 216
         at Orleans.Serialization.Codecs.FieldHeaderCodec.ReadExtendedFieldHeader[TInput](Reader`1& reader, Field& field) in /_/src/Orleans.Serialization/Codecs/FieldHeaderCodec.cs:line 197
         at Orleans.Serialization.Serializer`1.Deserialize(ReadOnlySpan`1 source, SerializerSession session) in /_/src/Orleans.Serialization/Serializer.cs:line 814
         at Orleans.Runtime.Messaging.MessageSerializer.TryRead(ReadOnlySequence`1& input, Message& message) in /_/src/Orleans.Core/Messaging/MessageSerializer.cs:line 120
         at Orleans.Runtime.Messaging.Connection.ProcessIncoming() in /_/src/Orleans.Core/Networking/Connection.cs:line 304
      --- End of stack trace from previous location ---
         at Orleans.Serialization.Invocation.ResponseCompletionSource`1.GetResult(Int16 token) in /_/src/Orleans.Serialization/Invocation/ResponseCompletionSource.cs:line 230
         at System.Threading.Tasks.ValueTask`1.ValueTaskSourceAsTask.<>c.<.cctor>b__4_0(Object state)
      --- End of stack trace from previous location ---
         at ServiceA.Grains.PublisherGrain.StartPublishing() in /Users/trond/Desktop/OrleansStreamsHeterogeneousSilo/ServiceA/Grains/PublisherGrain.cs:line 34
         at Orleans.Runtime.TaskRequest.CompleteInvokeAsync(Task resultTask) in /_/src/Orleans.Core.Abstractions/Runtime/GrainReference.cs:line 728
         at Orleans.Serialization.Invocation.ResponseCompletionSource.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token) in /_/src/Orleans.Serialization/Invocation/ResponseCompletionSource.cs:line 98
         at System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c.<.cctor>b__4_0(Object state)
      --- End of stack trace from previous location ---
         at StartPublisherHostedService.ExecuteAsync(CancellationToken stoppingToken) in /Users/trond/Desktop/OrleansStreamsHeterogeneousSilo/ServiceA/Program.cs:line 70
         at Microsoft.Extensions.Hosting.Internal.Host.TryExecuteBackgroundServiceAsync(BackgroundService backgroundService)
```

Service A crashes completely while Service B keeps running. Service B logs the following error when Service A starts up:

```
info: Orleans.Runtime.MembershipService.MembershipTableManager[0]
      Received cluster membership snapshot via gossip: [Version: 27, 8 silos, SiloAddress=S192.168.69.129:11111:398866315 SiloName=Silo_148f9 Status=Dead, SiloAddress=S127.0.0.1:11112:398866295 SiloName=Silo_b3acb Status=Dead, SiloAddress=S127.0.0.1:11112:398866358 SiloName=Silo_2da8f Status=Dead, SiloAddress=S127.0.0.1:11112:398866395 SiloName=Silo_4f998 Status=Active, SiloAddress=S127.0.0.1:11112:398865749 SiloName=Silo_b1d1b Status=Dead, SiloAddress=S192.168.69.237:11111:398866398 SiloName=Silo_a765e Status=Active, SiloAddress=S192.168.69.237:11111:398866368 SiloName=Silo_8f965 Status=Dead, SiloAddress=S192.168.69.237:11111:398865689 SiloName=Silo_146da Status=Dead]
info: Orleans.Runtime.MembershipService.ClusterHealthMonitor[100612]
      Will watch (actively ping) 1 silos: [S192.168.69.237:11111:398866398]
warn: Orleans.Runtime.Messaging.NetworkingTrace[0]
      Exception reading message Request [S192.168.69.237:11111:398866398 publisher/00000000000000000000000000000000]->[S127.0.0.1:11112:398866395 sys.svc.user.30C002EB/127.0.0.1:11112@398866395] #30 from remote endpoint 127.0.0.1:53062 to local endpoint 127.0.0.1:11112
      System.IO.FileNotFoundException: Could not load file or assembly 'Orleans.Reminders, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
      
      File name: 'Orleans.Reminders, Culture=neutral, PublicKeyToken=null'
         at System.Reflection.RuntimeAssembly.InternalLoad(ObjectHandleOnStack assemblyName, ObjectHandleOnStack requestingAssembly, StackCrawlMarkHandle stackMark, Boolean throwOnFileNotFound, ObjectHandleOnStack assemblyLoadContext, ObjectHandleOnStack retAssembly)
         at System.Reflection.RuntimeAssembly.InternalLoad(AssemblyName assemblyName, RuntimeAssembly requestingAssembly, StackCrawlMark& stackMark, Boolean throwOnFileNotFound, AssemblyLoadContext assemblyLoadContext)
         at System.Reflection.Assembly.Load(AssemblyName assemblyRef)
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.<TryPerformUncachedTypeResolution>g__ResolveAssembly|7_0(AssemblyName assemblyName) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 136
         at System.TypeNameParser.ResolveAssembly(String asmName, Func`2 assemblyResolver, Boolean throwOnError, StackCrawlMark& stackMark)
         at System.TypeNameParser.ConstructType(Func`2 assemblyResolver, Func`4 typeResolver, Boolean throwOnError, Boolean ignoreCase, StackCrawlMark& stackMark)
         at System.TypeNameParser.GetType(String typeName, Func`2 assemblyResolver, Func`4 typeResolver, Boolean throwOnError, Boolean ignoreCase, StackCrawlMark& stackMark)
         at System.Type.GetType(String typeName, Func`2 assemblyResolver, Func`4 typeResolver, Boolean throwOnError)
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.TryPerformUncachedTypeResolution(String fullName, Type& type, Assembly[] assemblies) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 107
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.TryPerformUncachedTypeResolution(String name, Type& type) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 51
         at Orleans.Serialization.TypeSystem.CachedTypeResolver.TryResolveType(String name, Type& type) in /_/src/Orleans.Serialization/TypeSystem/CachedTypeResolver.cs:line 39
         at Orleans.Serialization.TypeSystem.TypeConverter.ParseInternal(String formatted, Type& type) in /_/src/Orleans.Serialization/TypeSystem/TypeConverter.cs:line 329
         at Orleans.Serialization.TypeSystem.TypeCodec.TryRead[TInput](Reader`1& reader, Type& type) in /_/src/Orleans.Serialization/TypeSystem/TypeCodec.cs:line 113
         at Orleans.Serialization.Codecs.FieldHeaderCodec.ReadType[TInput](Reader`1& reader, SchemaType schemaType) in /_/src/Orleans.Serialization/Codecs/FieldHeaderCodec.cs:line 216
         at Orleans.Serialization.Codecs.FieldHeaderCodec.ReadExtendedFieldHeader[TInput](Reader`1& reader, Field& field) in /_/src/Orleans.Serialization/Codecs/FieldHeaderCodec.cs:line 197
         at Orleans.Serialization.Serializer`1.Deserialize(ReadOnlySpan`1 source, SerializerSession session) in /_/src/Orleans.Serialization/Serializer.cs:line 814
         at Orleans.Runtime.Messaging.MessageSerializer.TryRead(ReadOnlySequence`1& input, Message& message) in /_/src/Orleans.Core/Messaging/MessageSerializer.cs:line 120
         at Orleans.Runtime.Messaging.Connection.ProcessIncoming() in /_/src/Orleans.Core/Networking/Connection.cs:line 304
```

To showcase this issue locally, first spin up the necessary dependencies (DynamoDB):

```shell
docker compose up -d
```

Then start Service B:

```
dotnet run --project ServiceB/ServiceB.csproj
```

Then start Service A:

```
dotnet run --project ServiceA/ServiceA.csproj
```
