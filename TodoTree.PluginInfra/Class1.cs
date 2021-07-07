using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MessagePack;

namespace TodoTree.PluginInfra
{
    // Server -> Client definition
    public interface IGamingHubReceiver
    {
        void Import(string token,IEnumerable<TodoData> todo);

    }

    // Client -> Server definition
    // implements `IStreamingHub<TSelf, TReceiver>`  and share this type between server and client.
    public interface IGamingHub : IStreamingHub<IGamingHub, IGamingHubReceiver>
    {
        Task<string> RegisterPlugin(Capability capability);
        Task ReturnImportResult(string token, string instance, IEnumerable<TodoData> todo);

    }


    public interface ImportPlugin
    {
        Task<IEnumerable<TodoData>> Import( IEnumerable<TodoData> todo);
    }

    public class PluginBody<T> : IGamingHubReceiver where T : new()
    {
        private readonly T value;
        private readonly string address;
        private string instanceId;
        private IGamingHub client;

        public PluginBody(string address)
        {
            this.address = address;
            this.value = new T();
        }

        public async ValueTask RunAsync(CancellationToken cancellationToken = default)
        {
            var capabilitis = new List<string>();
            var interfaces = typeof(T).GetInterfaces().Select(type => type.FullName).ToHashSet();
            if (interfaces.Contains(typeof(ImportPlugin).FullName))
            {
                capabilitis.Add(typeof(ImportPlugin).FullName);
            }

            var capability = new Capability
            {
                Capabilities = capabilitis
            };

            var grpcChannel = GrpcChannel.ForAddress(address);
            client = await StreamingHubClient.ConnectAsync<IGamingHub, IGamingHubReceiver>(grpcChannel, this);
            instanceId = await client.RegisterPlugin(capability);
            await Task.Delay(TimeSpan.FromMilliseconds(-1), cancellationToken);

        }

        public async void Import(string token, IEnumerable<TodoData> todo)
        {
            if (value is ImportPlugin import)
            {
                var data = await import.Import(todo);
                await client.ReturnImportResult(token, instanceId, data);
            }
        }


        public static ValueTask RunPulgin<T>(string address, CancellationToken cancellationToken = default) where T : new()
        {
            return new PluginBody<T>(address).RunAsync(cancellationToken);

        }
    }



    // for example, request object by MessagePack.
    [MessagePackObject]
    public class Player
    {
        [Key(0)] public string Name { get; set; }
        [Key(1)] public int Position { get; set; }
        [Key(2)] public int Rotation { get; set; }
    }

    [MessagePackObject]
    public class Capability
    {
        [Key(0)] public IEnumerable<string> Capabilities { get; set; }
    }

    [MessagePackObject]
    public class TodoData
    {
        [Key(0)] public string Id { get; set; }
        [Key(1)] public string Name { get; set; }
        [Key(2)] public TimeSpan EstimateTime { get; set; }
        [Key(3)] public string Parent { get; set; }
        [Key(4)] public Dictionary<string,string> Attributes { get; set; }
    }

    public static class TodoConvert
    {
        public static IEnumerable<TodoData> Convert(IEnumerable<Todo> todos)
        {
            return todos == null
                ? Enumerable.Empty<TodoData>()
                : todos.SelectMany(todo => todo == null ? Enumerable.Empty<TodoData>() : Convert(todo, null));
        }

        public static IEnumerable<TodoData> Convert(Todo todo, string parent)
        {
            return Convert(todo.Children)
                .Concat(new[]{
                new TodoData
            {
                Id = todo.Id,
                Name = todo.Name,
                EstimateTime = todo.EstimateTime,
                Attributes = todo.Attribute,
                Parent = parent
            }});
        }
    }


}
