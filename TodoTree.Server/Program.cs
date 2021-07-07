using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using MagicOnion.Server.Hubs;
using Microsoft.Extensions.DependencyInjection;
using TodoTree.Server;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using TodoTree;
using TodoTree.PluginInfra;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Plungin>();
builder.Services.AddSingleton<TodoRepository>(_ => new TodoRepository());
builder.Services.AddHostedService<PluginService>();
builder.Services.AddGrpc();
builder.Services.AddMagicOnion();
builder.WebHost.UseUrls("http://*:5000;https://*:5001;");
await using var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.MapMagicOnionService();

app.MapGet("/", (Func<string>)(() => "Hello World!"));

await app.RunAsync();


namespace TodoTree.Server
{
    public class PluginService : BackgroundService
    {
        private const string csproj = @"
<Project Sdk=""Microsoft.NET.Sdk"">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <OutputType>Exe</OutputType>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include = ""MagicOnion"" Version=""4.3.1"" />
        <Reference Include=""TodoTree.PluginInfra"">
            <HintPath>TodoTree.PluginInfra.dll</HintPath>
        </Reference>
    </ItemGroup>

</Project>
";

        private const string program = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoTree.PluginInfra;


await  PluginBody<Sample>.RunPulgin<Sample>(""https://localhost:5001"");

public class Sample : ImportPlugin
{
    public Task<IEnumerable<TodoData>> Import(IEnumerable<TodoData> todo)
    {
        return Task.FromResult(Enumerable.Empty<TodoData>());
    }
}

";


        private readonly string pluginPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "plugin");

        private Plungin plungin;

        private TodoRepository repository;

        public PluginService(Plungin plungin, TodoRepository repository)
        {
            this.plungin = plungin;
            this.repository = repository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!Directory.Exists(pluginPath))
            {
                Directory.CreateDirectory(pluginPath);
                await File.WriteAllTextAsync(Path.Combine(pluginPath, "Plugin.csproj"), csproj, stoppingToken);
                await File.WriteAllTextAsync(Path.Combine(pluginPath, "Program.cs"), program);
            }
            var infraDll = new FileInfo(typeof(IGamingHub).Assembly.Location);
            infraDll.CopyTo(Path.Combine(pluginPath, infraDll.Name), true);
            var modelDll = new FileInfo(typeof(TodoTree.Todo).Assembly.Location);
            modelDll.CopyTo(Path.Combine(pluginPath, modelDll.Name), true);

            await Task.Delay(TimeSpan.FromSeconds(10));
            var t = repository.GetTopTodo().ToArray();
            var td = TodoConvert.Convert(t).ToArray();
            await plungin.Import(td);
        }
    }


    public class Plungin
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<GamingHub, byte>> plugins = new ConcurrentDictionary<string, ConcurrentDictionary<GamingHub, byte>>();

        public void Register(Capability capability, GamingHub plugin)
        {
            foreach (var c in capability.Capabilities)
            {

                var bag = plugins.GetOrAdd(c, _ => new ConcurrentDictionary<GamingHub, byte>());
                bag.TryAdd(plugin, 0);
            }
        }


        public void UnRegister(GamingHub plugin)
        {
            foreach (var plugin1 in plugins)
            {
                plugin1.Value.TryRemove(new KeyValuePair<GamingHub, byte>(plugin, 0));

            }
        }



        public async Task<IEnumerable<TodoData>> Import(IEnumerable<TodoData> todo)
        {
            var list = new List<TodoData>();
            var requestId = Guid.NewGuid().ToString();
            var t = todo.ToArray();
            if (plugins.ContainsKey(typeof(ImportPlugin).FullName))
            {
                foreach (var p in plugins[typeof(ImportPlugin).FullName])
                {
                    try
                    {
                        var r = await p.Key.Import(requestId, t);
                       list.AddRange(r);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                }
            }

            return list;
        }
    }


    // Server implementation
    // implements : StreamingHubBase<THub, TReceiver>, THub
    public class GamingHub : StreamingHubBase<IGamingHub, IGamingHubReceiver>, IGamingHub
    {
        private Plungin manager;
        private IGroup group;
        private ConcurrentDictionary<string, object> requests = new ConcurrentDictionary<string, object>();


        public async Task<string> RegisterPlugin(Capability capability)
        {
            Console.WriteLine($"Register: {this.ConnectionId.ToString()}");
            manager = this.Context.ServiceProvider.GetService<Plungin>();
            manager.Register(capability, this);
            group = await Group.AddAsync(this.ConnectionId.ToString());

            return this.Context.ContextId.ToString();
        }


        public Task<IEnumerable<TodoData>> Import(string requestId, IEnumerable<TodoData> todo)
        {
            Console.WriteLine($"Import: {this.ConnectionId.ToString()} {requestId}");
            var comp = new TaskCompletionSource<IEnumerable<TodoData>>();

            if (requests.TryAdd(requestId, comp))
            {
                this.BroadcastToSelf(group).Import(requestId, todo);
                return comp.Task;
            }

            return Task.FromResult(Enumerable.Empty<TodoData>());
        }

        public async Task ReturnImportResult(string token, string instance, IEnumerable<TodoData> todo)
        {
            Console.WriteLine($"EndImport: {this.ConnectionId.ToString()} {token}");
            if (requests.TryGetValue(token, out var obj) && obj is TaskCompletionSource<IEnumerable<TodoData>> comp)
            {
                comp.SetResult(todo);
                requests.TryRemove(new KeyValuePair<string, object>(token, comp));

            }else if (requests.TryRemove(token, out var obj2))
            {
                Cancel(obj2);
            }
        }

        private void Cancel(object obj)
        {
            var d = obj as dynamic;
            d.SetCanceled();
        }

    // You can hook OnConnecting/OnDisconnected by override.
        protected override ValueTask OnDisconnected()
        {
            Console.WriteLine($"UnRegister: {this.ConnectionId.ToString()}");
            // on disconnecting, if automatically removed this connection from group.
            foreach (var request in requests)
            {
                Cancel(request.Value);
            }
            manager.UnRegister(this);
            return CompletedTask;
        }
    }
}