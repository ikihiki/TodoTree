using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Server;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MagicOnion.Server.Hubs;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace TodoTree.PluginInfra
{
    // Server -> Client definition
    public interface IGamingHubReceiver
    {
        void Import(string token, IEnumerable<TodoData> todo);

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
        Task<IEnumerable<TodoData>> Import(IEnumerable<TodoData> todo);
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


        public static ValueTask RunPulgin<T>(string address, CancellationToken cancellationToken = default)
            where T : new()
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
    public class TimeRecordData
    {
        [Key(0)] public DateTime Start { get; set; }
        [Key(1)] public DateTime? End { get; set; }
    }

    [MessagePackObject]
    public class TodoData
    {
        [Key(0)] public string Id { get; set; }
        [Key(1)] public string Name { get; set; }
        [Key(2)] public TimeSpan EstimateTime { get; set; }
        [Key(3)] public string Parent { get; set; }
        [Key(4)] public Dictionary<string, string> Attributes { get; set; }
        [Key(5)] public IEnumerable<TimeRecordData> TimeRecords { get; set; }
        [Key(6)] public bool Completed { get; set; }
    }

    public static class TodoConvert
    {
        public static IEnumerable<TodoData> Convert(IEnumerable<Todo> todos)
        {
            return todos == null
                ? Enumerable.Empty<TodoData>()
                : todos.SelectMany(todo => todo == null ? Enumerable.Empty<TodoData>() : Convert(todo)).ToArray();
        }

        public static IEnumerable<TodoData> Convert(Todo todo)
        {
            return Convert(todo.Children)
                .Concat(new[]
                {
                    new TodoData
                    {
                        Id = todo.Id,
                        Name = todo.Name,
                        EstimateTime = todo.EstimateTime,
                        Attributes = todo.Attribute,
                        Parent = todo.Parent?.Id,
                        Completed = todo.Compleated,
                        TimeRecords = todo.TimeRecords == null? Enumerable.Empty<TimeRecordData>(): todo.TimeRecords.Select(time=> new TimeRecordData(){Start = time.StartDateTime, End = time.EndDateTime}).ToArray()
                    }
                }).ToArray();
        }
    }

    public class TodoManager
    {
        private readonly Dictionary<string, Todo> todoDictionary = new Dictionary<string, Todo>();
        private readonly List<Todo> topTodo = new List<Todo>();

        public IEnumerable<Todo> TopTodo => topTodo;

        public TodoManager()
        {
        }

        public TodoManager(IEnumerable<Todo> todos)
        {
            foreach (var todo in todos)
            {
                todoDictionary.Add(todo.Id, todo);
                if (!todo.IsChild)
                {
                    topTodo.Add(todo);
                }
            }
        }

        public TodoManager(IEnumerable<TodoData> data)
        {
            UpsertTodoRange(data);
        }

        public IEnumerable<string> UpsertTodo(TodoData data)
        {
            if (todoDictionary.ContainsKey(data.Id))
            {
                return UpdateTodo(data);
            }
            else
            {
                return AddTodo(data);
            }
        }

        public IEnumerable<string> UpsertTodoRange(IEnumerable<TodoData> data)
        {
            var result = new List<string>();
            foreach (var todo in data)
            {
                result.AddRange(UpsertTodo(todo));
            }
            return result;
        }

        private IEnumerable<string> AddTodo(TodoData data)
        {
            var result = new List<string>();
            var timeRecords = data.TimeRecords.Select(time => new TimeRecord(time.Start, time.End)).ToArray();
            var todo = new Todo(data.Name, data.EstimateTime, timeRecords, data.Attributes);
            todo.Id = data.Id;
            todo.Compleated = data.Completed;
            todoDictionary.Add(data.Id, todo);
            if (data.Parent is null)
            {
                topTodo.Add(todo);
            }
            else
            {
                todo.IsChild = true;
                if (!todoDictionary.ContainsKey(data.Parent))
                {
                    AddTodo(new TodoData
                    {
                        Id = data.Parent,
                        Parent = null,
                        Attributes = new Dictionary<string, string>(),
                        Completed = false,
                        EstimateTime = TimeSpan.Zero,
                        Name = $"temp parent :{data.Parent}",
                        TimeRecords = Enumerable.Empty<TimeRecordData>()
                    });
                    
                }

                var parent = todoDictionary[data.Parent];
                parent.AddChild(todo);
                result.Add(data.Parent);
            }
            result.Add(data.Id);
            return result;
        }

        private IEnumerable<string> UpdateTodo(TodoData data)
        {
            var result = false;
            var list = new List<string>();

            var todo = todoDictionary[data.Id];
            if (todo.Attribute?
                .OrderBy((o) => o.Key)
                .SequenceEqual(data.Attributes?
                    .OrderBy((o) => o.Key) ?? Enumerable.Empty<KeyValuePair<string,string>>()
                    ) 
                is not true)
            {
                todo.Attribute = data.Attributes;
                result = true;
            }

            if (todo.Compleated != data.Completed)
            {
                todo.Compleated = data.Completed;
                result = true;
            }

            if (todo.Name != data.Name)
            {
                todo.Name = data.Name;
                result = true;
            }

            if (todo.Parent?.Id != data.Parent)
            {  
                if (todo.Parent != null)
                { 
                    list.Add(todo.Parent.Id);
                    todo.Parent?.DeleteChild(todo);
                }
                

                todo.IsChild = true;
                if (!todoDictionary.ContainsKey(data.Parent))
                {
                    AddTodo(new TodoData
                    {
                        Id = data.Parent,
                        Parent = null,
                        Attributes = new Dictionary<string, string>(),
                        Completed = false,
                        EstimateTime = TimeSpan.Zero,
                        Name = $"temp parent :{data.Parent}",
                        TimeRecords = Enumerable.Empty<TimeRecordData>()
                    });
                    
                }

                var parent = todoDictionary[data.Parent];
                parent.AddChild(todo);

                list.Add(data.Parent);
                result = true;
            }

            if (result)
            {
                list.Add(data.Id);
            }
            return list;
        }

        public void DeleteTodo(string id)
        {
            var todo = todoDictionary[id];
            todoDictionary.Remove(id);
            topTodo.Remove(todo);
        }

        public Todo GetTodo(string id)
        {
            return todoDictionary[id];
        }
    }

    public interface ITodoService : IService<ITodoService>
    {
        UnaryResult<IEnumerable<TodoData>> Get();
        UnaryResult<IEnumerable<TodoData>> Upsert(IEnumerable<TodoData> data);
        UnaryResult<IEnumerable<TodoData>> Delete(string id);
        UnaryResult<TodoData> Start(string id);
        UnaryResult<TodoData> Stop(string id);
        UnaryResult<TodoData> Complete(string id);
        UnaryResult<TodoData> UnComplete(string id);
    }




    public interface ITodoNotifyReceiver
    {
        void OnUpdate(IEnumerable<TodoData> data);
        void OnDelete(IEnumerable<TodoData> data);
    }

    public interface ITodoNotify : IStreamingHub<ITodoNotify, ITodoNotifyReceiver>
    {
        Task Join();
    }




    public class TodoServiceClient : ITodoNotifyReceiver
    {
        private readonly string address;

        GrpcChannel channel;
        ITodoNotify notifyClient;
        ITodoService serviceClient;
        private TodoManager manager;

        public event Action ChangeTodo;


        public TodoServiceClient(string address)
        {
            this.address = address;
        }

        public async Task Connect()
        {
            channel = GrpcChannel.ForAddress(address);
            serviceClient = MagicOnionClient.Create<ITodoService>(channel);
            notifyClient = await StreamingHubClient.ConnectAsync<ITodoNotify, ITodoNotifyReceiver>(channel, this);
            await notifyClient.Join();
            manager = new TodoManager(await serviceClient.Get());
        }

        public async Task CheckAndConnect()
        {
            if (channel == null)
            {
                await Connect();
            }
        }

        public void OnUpdate(IEnumerable<TodoData> data)
        {
            manager.UpsertTodoRange(data);
            ChangeTodo?.Invoke();
        }

        public IEnumerable<Todo> GetTopTodos()
        {
            return manager.TopTodo;
        }

        public async Task Upsert(Todo todo)
        {
            await CheckAndConnect();
            await serviceClient.Upsert(TodoConvert.Convert(todo));
        }

        public async Task Delete(Todo todo)
        {
            await CheckAndConnect();
            await serviceClient.Delete(todo.Id);
        }

        public async Task Start(Todo todo)
        {
            await CheckAndConnect();
            await serviceClient.Start(todo.Id);
        }

        public async Task Stop(Todo todo)
        {
            await CheckAndConnect();
            await serviceClient.Stop(todo.Id);
        }

        public async Task Complete(Todo todo)
        {
            await CheckAndConnect();
            await serviceClient.Complete(todo.Id);
        }

        public async Task Unomplete(Todo todo)
        {
            await CheckAndConnect();
            await serviceClient.UnComplete(todo.Id);
        }

        public void OnDelete(IEnumerable<TodoData> data)
        {
            foreach (var todoData in data)
            {
                manager.DeleteTodo(todoData.Id);
            }
            ChangeTodo?.Invoke();

        }
    }

}
