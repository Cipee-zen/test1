using Sandbox.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sandbox;

[Title("Network System")]
[Category("Networking")]
[Icon("electrical_services")]
public sealed class NetworkSystem : Component, Component.INetworkListener
{
    [Property] public bool StartServer { get; set; } = true;

    [Property] public GameObject PlayerPrefab { get; set; }

    [Property] public List<GameObject> SpawnPoints { get; set; }

    protected override async Task OnLoad()
    {
        if (Scene.IsEditor)
            return;

        if (StartServer && !GameNetworkSystem.IsActive)
        {
            LoadingScreen.Title = "Creating Lobby";
            await Task.DelayRealtimeSeconds(0.1f);
            GameNetworkSystem.CreateLobby();
        }
    }

    public void OnConnected(Connection connection)
    {
        // return;
    }

    /// <summary>
    /// A client is fully connected to the server. This is called on the host.
    /// </summary>
    public void OnActive(Connection channel)
    {
        Log.Info($"Player '{channel.DisplayName}' has joined the game");

        if (PlayerPrefab is null)
            return;
        
        var startLocation = FindSpawnLocation().WithScale(1);
        var player = PlayerPrefab.Clone(startLocation, name: $"Player - {channel.DisplayName}");
        player.NetworkSpawn(channel);
    }

    Transform FindSpawnLocation()
    {
        if (SpawnPoints is not null && SpawnPoints.Count > 0)
        {
            return Random.Shared.FromList(SpawnPoints, default).Transform.World;
        }

        var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
        if (spawnPoints.Length > 0) 
        {
            return Random.Shared.FromArray(spawnPoints).Transform.World;
        }

        return Transform.World;
    }
}



