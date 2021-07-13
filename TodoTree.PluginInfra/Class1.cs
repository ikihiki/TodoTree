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
    public class TodoData
    {
        [Key(0)] public string Id { get; set; }
        [Key(1)] public string Name { get; set; }
        [Key(2)] public TimeSpan EstimateTime { get; set; }
        [Key(3)] public string Parent { get; set; }
        [Key(4)] public Dictionary<string, string> Attributes { get; set; }
        [Key(5)] public IEnumerable<TimeRecord> TimeRecords { get; set; }
        [Key(6)] public bool Completed { get; set; }
    }

    public static class TodoConvert
    {
        public static IEnumerable<TodoData> Convert(IEnumerable<Todo> todos)
        {
            return todos == null
                ? Enumerable.Empty<TodoData>()
                : todos.SelectMany(todo => todo == null ? Enumerable.Empty<TodoData>() : Convert(todo));
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
                        Parent = todo.Parent?.Id
                    }
                });
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

        public void UpsertTodo(TodoData data)
        {
            if (todoDictionary.ContainsKey(data.Id))
            {
                UpdateTodo(data);
            }
            else
            {
                AddTodo(data);
            }
        }

        public void UpsertTodoRange(IEnumerable<TodoData> data)
        {
            foreach (var todo in data)
            {
                UpsertTodo(todo);
            }
        }

        private void AddTodo(TodoData data)
        {
            var todo = new Todo(data.Name, data.EstimateTime, data.TimeRecords, data.Attributes);
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
                        TimeRecords = Enumerable.Empty<TimeRecord>()
                    });
                }

                var parent = todoDictionary[data.Parent];
                parent.AddChild(todo);
            }

        }

        private void UpdateTodo(TodoData data)
        {
            var todo = todoDictionary[data.Id];
            todo.Attribute = data.Attributes;
            todo.Compleated = data.Completed;
            todo.Name = data.Name;
            if (todo.Parent?.Id != data.Parent)
            {
                todo.Parent?.DeleteChild(todo);

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
                        TimeRecords = Enumerable.Empty<TimeRecord>()
                    });
                }

                var parent = todoDictionary[data.Parent];
                parent.AddChild(todo);
            }
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
        UnaryResult<TodoData> Upsert(TodoData data);
        UnaryResult<IEnumerable<TodoData>> Delete(string id);
        UnaryResult<TodoData> Start(string id);
        UnaryResult<TodoData> Stop(string id);
        UnaryResult<TodoData> Complete(string id);
        UnaryResult<TodoData> Unomplete(string id);
    }


    public class TodoServer : ServiceBase<ITodoService>, ITodoService
    {
        private readonly TodoRepository repository;
        private readonly TodoManager manager;
        private readonly IPublisher<IEnumerable<TodoData>> publisher;

        public TodoServer(TodoRepository repository, IPublisher<IEnumerable<TodoData>> publisher)
        {
            this.repository = repository;
            this.publisher = publisher;
            manager = new TodoManager(repository.GetAllTodo());
        }

        public async UnaryResult<IEnumerable<TodoData>> Get()
        {
            return TodoConvert.Convert(repository.GetTopTodo());
        }

        public async UnaryResult<TodoData> Upsert(TodoData data)
        {
            manager.UpsertTodo(data);
            var result = manager.GetTodo(data.Id);
            repository.AddOrUpdate(result);
            var changed = TodoConvert.Convert(result).First();
            publisher.Publish(new[] { changed });
            return changed;
        }

        public async UnaryResult<IEnumerable<TodoData>> Delete(string id)
        {
            var todo = manager.GetTodo(id);
            manager.DeleteTodo(id);
            repository.Delete(todo);
            return TodoConvert.Convert(manager.TopTodo);
        }

        public async UnaryResult<TodoData> Start(string id)
        {
            var todo = manager.GetTodo(id);
            todo.Start();
            manager.UpsertTodo(TodoConvert.Convert(todo).First());
            repository.AddOrUpdate(todo);
            return TodoConvert.Convert(todo).First();
        }

        public async UnaryResult<TodoData> Stop(string id)
        {
            var todo = manager.GetTodo(id);
            todo.Stop();
            var changed = TodoConvert.Convert(todo).First();
            manager.UpsertTodo(changed);
            repository.AddOrUpdate(todo);
            publisher.Publish(new[] { changed });
            return changed;
        }

        public async UnaryResult<TodoData> Complete(string id)
        {
            var todo = manager.GetTodo(id);
            todo.Complete();
            var changed = TodoConvert.Convert(todo).First();
            manager.UpsertTodo(changed);
            repository.AddOrUpdate(todo);
            publisher.Publish(new[] { changed });
            return changed;
        }

        public async UnaryResult<TodoData> Unomplete(string id)
        {
            var todo = manager.GetTodo(id);
            todo.UnComplete();
            var changed = TodoConvert.Convert(todo).First();
            manager.UpsertTodo(changed);
            repository.AddOrUpdate(todo);
            publisher.Publish(new[] { changed });
            return changed;
        }
    }

    public interface ITodoNotifyReceiver
    {
        void OnUpdate(IEnumerable<TodoData> data);
    }

    public interface ITodoNotify : IStreamingHub<ITodoNotify, ITodoNotifyReceiver>
    {
        Task Join();
    }

    public class TodoNotifyHub : StreamingHubBase<ITodoNotify, ITodoNotifyReceiver>, ITodoNotify
    {
        IDisposable disposable;
        IGroup room;

        public async Task Join()
        {
            room = await Group.AddAsync(this.ConnectionId.ToString());
            ISubscriber<IEnumerable<TodoData>> subscriber =
                this.Context.ServiceProvider.GetService<ISubscriber<IEnumerable<TodoData>>>();
            disposable = subscriber.Subscribe(data => this.BroadcastToSelf(room).OnUpdate(data));
        }

        protected override async ValueTask OnDisconnected()
        {
            disposable?.Dispose();
        }
    }


    public class TodoServiceClient : ITodoNotifyReceiver
    {
        private readonly string address;

        GrpcChannel channel; 
        ITodoNotify notifyClient;
        ITodoService serviceClient;
        private TodoManager manager;

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


        public void OnUpdate(IEnumerable<TodoData> data)
        {

            manager.UpsertTodoRange(data);
        }

        public IEnumerable<Todo> GetTopTodos()
        {
            return manager.TopTodo;
        }

        public async Task Upsert(Todo todo)
        {
            await serviceClient.Upsert(TodoConvert.Convert(todo).First());
        }

        public async Task Delete(Todo todo)
        {
            await serviceClient.Delete(todo.Id);
        }

        public async Task Start(Todo todo)
        {
            await serviceClient.Start(todo.Id);
        }

        public async Task Stop(Todo todo)
        {
            await serviceClient.Stop(todo.Id);
        }

        public async Task Complete(Todo todo)
        {
            await serviceClient.Complete(todo.Id);
        }

        public async Task Unomplete(Todo todo)
        {
            await serviceClient.Unomplete(todo.Id);
        }
    }

}
