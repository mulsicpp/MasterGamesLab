using NUnit.Framework;
using System;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public PlayerData[] Players;
    public int SelfIndex = -1;

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
        var playerId = System.Text.Encoding.ASCII.GetString(request.Payload);

        int index = Array.FindIndex(Players, data => data.PlayerId == playerId);

        if(index == -1)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "PlayerID could not be found";

            return;
        }

        Players[index].ClientId = request.ClientNetworkId;

        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    public void OnClientConnected(ulong clientid)
    {
        if (NetworkManager.Singleton == null) return;
        if (!IsServer) return;
        UpdatePlayersClientRpc(Players);

        // TODO Synchronize game state
    }

    public void OnClientDisconnect(ulong clientid)
    {
        if (NetworkManager.Singleton == null) return;
        if (IsServer)
        {
            int index = Array.FindIndex(Players, data => data.ClientId == clientid);
            if (index != -1)
            {
                Players[index].ClientId = Constants.NO_CLIENT_ID;
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


    [Rpc(SendTo.ClientsAndHost)]
    public void UpdatePlayersClientRpc(PlayerData[] players)
    {
        this.Players = players;
        SelfIndex = Array.FindIndex(players, data => data.PlayerId == AuthenticationService.Instance.PlayerId);
    }

    public PlayerData? GetSelf()
    {
        return SelfIndex != -1 ? Players[SelfIndex] : null;
    }
}

[System.Serializable]
public struct PlayerData : INetworkSerializable
{
    public ulong ClientId;
    public string PlayerId;
    public string Name;

    public ulong Money;

    public PlayerData(Player player)
    {
        ClientId = Constants.NO_CLIENT_ID;
        PlayerId = player.Id;
        Name = player.Data?["Name"]?.Value;
        Money = Constants.PLAYER_START_MONEY;
    }

    public bool IsConnected()
    {
        return ClientId != Constants.NO_CLIENT_ID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Money);
    }
}
