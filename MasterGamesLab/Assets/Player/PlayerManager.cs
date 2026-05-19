using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
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
        NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    public void OnServerStarted()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
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
        UpdatePlayersClientRpc(Players);
    }

    public void OnClientDisconnect(ulong clientid)
    {
        int index = Array.FindIndex(Players, data => data.ClientId == clientid);
        if (index != -1)
        {
            Players[index].ClientId = Constants.NO_CLIENT_ID;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            UpdatePlayersClientRpc(Players);
        }
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
