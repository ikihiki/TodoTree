using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using MagicOnion.Server.Hubs;
using Microsoft.Extensions.DependencyInjection;
using TodoTree.Server;
using System.Linq;
using TodoTree.PluginInfra;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<PluginService>();
builder.Services.AddGrpc();
builder.Services.AddMagicOnion();
await using var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


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
        <PackageReference Include = ""MagicOnion.Client"" Version=""4.3.1"" />
        <Reference Include=""TodoTree.PluginInfra"">
            <HintPath>TodoTree.PluginInfra.dll</HintPath>
        </Reference>
    </ItemGroup>

</Project>
";

        private const string program = @"
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion.Client;
using TodoTree.PluginInfra;


var channel = GrpcChannel.ForAddress(""https://localhost:5001"");

var hub = new GamingHubClient();
var connectAsync = await hub.ConnectAsync(channel, ""aaa"", ""bb"");
await hub.MoveAsync(1, 2);

public class GamingHubClient : IGamingHubReceiver
{
    Dictionary<string, string> players = new Dictionary<string, string>();
 
    IGamingHub client;
 
    public async Task<string> ConnectAsync(GrpcChannel grpcChannel, string roomName, string playerName)
    {
        client = await StreamingHubClient.ConnectAsync<IGamingHub, IGamingHubReceiver>(grpcChannel, this);
 
        var roomPlayers = await client.JoinAsync(roomName, playerName, 0, 0);
        foreach (var player in roomPlayers)
        {
            (this as IGamingHubReceiver).OnJoin(player);
        }
 
        return players[playerName];
    }
 
    // methods send to server.
 
    public Task LeaveAsync()
    {
        return client.LeaveAsync();
    }
 
    public Task MoveAsync(int position, int rotation)
    {
        return client.MoveAsync(position, rotation);
    }
 
    // dispose client-connection before channel.ShutDownAsync is important!
    public Task DisposeAsync()
    {
        return client.DisposeAsync();
    }
 
    // You can watch connection state, use this for retry etc.
    public Task WaitForDisconnect()
    {
        return client.WaitForDisconnect();
    }
 
    // Receivers of message from server.
 
    void IGamingHubReceiver.OnJoin(Player player)
    {
        Console.WriteLine(""Join Player:"" + player.Name);

        players[player.Name] = player.Name;
    }

    void IGamingHubReceiver.OnLeave(Player player)
    {
        Console.WriteLine(""Leave Player:"" + player.Name);

        if (players.TryGetValue(player.Name, out var cube))
        {
            
        }
    }

    void IGamingHubReceiver.OnMove(Player player)
    {
        Console.WriteLine(""Move Player:"" + player.Name);

        if (players.TryGetValue(player.Name, out var cube))
        {
            
        }
    }
}
";


        private readonly string pluginPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "plugin");



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!Directory.Exists(pluginPath))
            {
                Directory.CreateDirectory(pluginPath);
                await File.WriteAllTextAsync(Path.Combine(pluginPath, "Plugin.csproj"), csproj, stoppingToken);
                await File.WriteAllTextAsync(Path.Combine(pluginPath, "Program.cs"), program);
                var infraDll = new FileInfo(typeof(IGamingHub).Assembly.Location);
                infraDll.CopyTo(Path.Combine(pluginPath, infraDll.Name));
                var modelDll = new FileInfo(typeof(TodoTree.Todo).Assembly.Location);
                modelDll.CopyTo(Path.Combine(pluginPath, modelDll.Name));
            }
        }
    }


    public class Plungin
    {
        private Dictionary<string, List<GamingHub>> plugins;

        public void Register(Capability capability, GamingHub plugin)
        {
            foreach (var c in capability.Capabilities)
            {
                if (!plugins.ContainsKey(c))
                {
                    plugins.Add(c, new List<GamingHub>());
                }

                plugins[c].Add(plugin);
            }
        }


        public void UnRegister(GamingHub plugin)
        {
            foreach (var plugin1 in plugins)
            {
                if (plugin1.Value.Contains(plugin))
                {
                    plugin1.Value.Remove(plugin);
                }
            }
        }



        public void Import(IEnumerable<Todo> todo)
        {
            if (plugins.ContainsKey(typeof(ImportPlugin).FullName))
            {
                foreach (var p in plugins[typeof(ImportPlugin).FullName])
                {
                    
                }

                
            }
        }
    }


// Server implementation
    // implements : StreamingHubBase<THub, TReceiver>, THub
    public class GamingHub : StreamingHubBase<IGamingHub, IGamingHubReceiver>, IGamingHub
    {
        private Plungin manager;
        private string id;

        public GamingHub(Plungin manager)
        {
            this.manager = manager;
            id = Guid.NewGuid().ToString();
        }

        public async Task<string> RegisterPlugin(Capability capability)
        {
            manager.Register(capability, this);
            return id;
        }

        public Task ReturnImportResult(string token, string instance, IEnumerable<TodoData> todo)
        {
            throw new NotImplementedException();
        }

        // You can hook OnConnecting/OnDisconnected by override.
        protected override ValueTask OnDisconnected()
        {
            manager.UnRegister(this);
            // on disconnecting, if automatically removed this connection from group.
            return CompletedTask;
        }
    }
}