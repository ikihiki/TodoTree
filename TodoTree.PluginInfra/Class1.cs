using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using MagicOnion;
using MessagePack;

namespace TodoTree.PluginInfra
{
    // Server -> Client definition
    public interface IGamingHubReceiver
    {
        // return type shuold be `void` or `Task`, parameters are free.
        void OnJoin(Player player);
        void OnLeave(Player player);
        void OnMove(Player player);


        void OnRefresh();
    }

    // Client -> Server definition
    // implements `IStreamingHub<TSelf, TReceiver>`  and share this type between server and client.
    public interface IGamingHub : IStreamingHub<IGamingHub, IGamingHubReceiver>
    {
        // return type shuold be `Task` or `Task<T>`, parameters are free.
        Task<Player[]> JoinAsync(string roomName, string userName, int position, int rotation);
        Task LeaveAsync();
        Task MoveAsync(int position, int rotation);

        Task RegisterPlugin(Capability capability);



    }

    // for example, request object by MessagePack.
    [MessagePackObject]
    public class Player
    {
        [Key(0)]
        public string Name { get; set; }
        [Key(1)]
        public int Position { get; set; }
        [Key(2)]
        public int Rotation { get; set; }
    }

    [MessagePackObject]
    public class Capability
    {
        public IEnumerable<string> Capabilities { get; set; }
    }



}
