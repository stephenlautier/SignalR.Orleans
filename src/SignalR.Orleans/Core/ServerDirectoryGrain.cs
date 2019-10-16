using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;

namespace SignalR.Orleans.Core
{
    public interface IServerDirectoryGrain : IGrainWithIntegerKey
    {
        Task Register(Guid serverId);
        Task HeartBeat(Guid serverId);
        Task Dispose();
    }

    public class ServerDirectoryState
    {
        public Dictionary<Guid, DateTime> Servers { get; set; } = new Dictionary<Guid, DateTime>();
    }

    public class ServerDirectoryGrain : Grain, IServerDirectoryGrain
    {
        private readonly ServerDirectoryState _state = new ServerDirectoryState();

        public override async Task OnActivateAsync()
        {
            RegisterTimer(
               ValidateAndCleanUp,
               _state,
               TimeSpan.FromMinutes(1),
               TimeSpan.FromMinutes(1));

            await base.OnActivateAsync();
        }

        public Task Register(Guid serverId)
        {
            if (!_state.Servers.ContainsKey(serverId))
                _state.Servers.Add(serverId, DateTime.UtcNow);

            return Task.CompletedTask;
        }

        public Task HeartBeat(Guid serverId)
        {
            _state.Servers[serverId] = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            foreach (var server in _state.Servers)
            {
               var serverDisconnectedStream = _streamProvider.GetStream<string>(Constants.SERVER_DISCONNECTED, server.Key);

                //todo: dispatch server disconnected
            }
            _state.Servers = new Dictionary<Guid, DateTime>();
            DeactivateOnIdle();
            return Task.CompletedTask;
        }

        private async Task ValidateAndCleanUp(object serverDirectory)
        {
            foreach (var server in _state.Servers.Where(server => server.Value < DateTime.UtcNow.AddMinutes(-1)))
            {
                //todo: dispatch server disconnected
                _state.Servers.Remove(server.Key);
            }

            await Task.FromResult(_state);
        }
    }
}