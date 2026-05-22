using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    private Dictionary<ClientId, Map.Map.SyncData> mapSyncData = new Dictionary<ClientId, Map.Map.SyncData>();

    public PlayerData[] Players;
    public PlayerId SelfId = PlayerId.NONE;

    public void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayersFromLobby(Lobby lobby)
    {
        Players = new PlayerData[lobby.Players.Count];
        for(int i = 0; i < lobby.Players.Count; i++)
        {
            Players[i] = new PlayerData(lobby.Players[i]);
        }
    }

    public void Start()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.ConnectionApprovalCallback -= ApproveConnection;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
    }

    public void OnServerStarted()
    {
        if (NetworkManager.Singleton?.NetworkTickSystem == null) return;
        NetworkManager.Singleton.NetworkTickSystem.Tick += OnNetworkTick;
    }

    public void OnServerStopped(bool _)
    {
        if (NetworkManager.Singleton?.NetworkTickSystem == null) return;
        NetworkManager.Singleton.NetworkTickSystem.Tick -= OnNetworkTick;
    }


    public void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var data = System.Runtime.InteropServices.MemoryMarshal.Read<PlayerConnectData>(request.Payload);
        string playerAuthId = data.PlayerAuthId.ToString();

        Debug.Log("Incoming connection PlayerId: '" + playerAuthId + "'");

        int index = Array.FindIndex(Players, playerData => playerData.PlayerAuthId == playerAuthId);

        if(index == -1)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "PlayerID could not be found";

            Debug.Log(response.Reason);

            return;
        }
        var clientId = new ClientId(request.ClientNetworkId);
        Players[index].ClientId = clientId;

        mapSyncData[clientId] = data.MapSyncData;

        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    public void OnClientConnected(ulong clientid)
    {
        if (NetworkManager.Singleton == null) return;
        if (!IsServer) return;

        UpdatePlayersClientRpc(Players);

        if (clientid == NetworkManager.Singleton.LocalClientId) return;

        var clientId = new ClientId(clientid);
        Map.Map.Instance.SyncClientMap(mapSyncData[clientId], clientId);
        mapSyncData.Remove(clientId);
    }

    public void OnClientDisconnect(ulong clientid)
    {
        if (NetworkManager.Singleton == null) return;
        if (IsServer)
        {
            int index = Array.FindIndex(Players, data => data.ClientId == clientid);
            if (index != -1)
            {
                Players[index].ClientId = ClientId.NONE;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                UpdatePlayersClientRpc(Players);
            }
        } else if(clientid == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client disconnected");
            NetworkManager.Singleton.Shutdown();
        }
    }

    public void OnNetworkTick()
    {
        UpdatePlayersClientRpc(Players);
    }


    // TODO Maybe create two different RPCs:
    // - UpdatePlayerStatus (on Connect/Disconnect)
    // - UpdatePlayerData (every server tick)
    [Rpc(SendTo.ClientsAndHost)]
    public void UpdatePlayersClientRpc(PlayerData[] players)
    {
        this.Players = players;
        var index = Array.FindIndex(players, data => data.PlayerAuthId == AuthenticationService.Instance.PlayerId);
        SelfId = index == -1 ? PlayerId.NONE : new PlayerId((byte)index);
    }

    public PlayerData? GetSelf()
    {
        return SelfId != PlayerId.NONE ? Players[SelfId] : null;
    }

    public PlayerId GetPlayerIdFromClientId(ClientId clientId)
    {
        int index = Array.FindIndex(Players, data => data.ClientId == clientId);
        return index == -1 ? PlayerId.NONE : new PlayerId((byte)index);
    }
}

[System.Serializable]
public struct PlayerData : INetworkSerializable
{
    public ClientId ClientId;
    public string PlayerAuthId;
    public string Name;

    public ulong Money;

    public PlayerData(Player player)
    {
        ClientId = ClientId.NONE;
        PlayerAuthId = player.Id;
        Name = player.Data?["Name"]?.Value;
        Money = Constants.PLAYER_START_MONEY;
    }

    public bool IsConnected()
    {
        return ClientId != ClientId.NONE;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerAuthId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Money);
    }
}

public struct PlayerConnectData
{
    public FixedString64Bytes PlayerAuthId;
    public Map.Map.SyncData MapSyncData;
}
