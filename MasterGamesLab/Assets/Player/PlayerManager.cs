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

    public PlayerData[] players;
    public int selfIndex = -1;

    public void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayersFromLobby(Lobby lobby)
    {
        players = new PlayerData[lobby.Players.Count];
        for(int i = 0; i < lobby.Players.Count; i++)
        {
            players[i] = new PlayerData(lobby.Players[i]);
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

        int index = Array.FindIndex(players, data => data.playerId == playerId);

        if(index == -1)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "PlayerID could not be found";

            return;
        }

        players[index].clientId = request.ClientNetworkId;

        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    public void OnClientConnected(ulong clientid)
    {
        UpdatePlayersClientRpc(players);
    }

    public void OnClientDisconnect(ulong clientid)
    {
        int index = Array.FindIndex(players, data => data.clientId == clientid);
        if (index != -1)
        {
            players[index].clientId = Constants.NO_CLIENT_ID;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            UpdatePlayersClientRpc(players);
        }
    }



    [Rpc(SendTo.ClientsAndHost)]
    public void UpdatePlayersClientRpc(PlayerData[] players)
    {
        this.players = players;
        selfIndex = Array.FindIndex(players, data => data.playerId == AuthenticationService.Instance.PlayerId);
    }

    public PlayerData? GetSelf()
    {
        return selfIndex != -1 ? players[selfIndex] : null;
    }
}

[System.Serializable]
public struct PlayerData : INetworkSerializable
{
    public ulong clientId;
    public string playerId;
    public string name;

    public ulong money;

    public PlayerData(Player player)
    {
        clientId = Constants.NO_CLIENT_ID;
        playerId = player.Id;
        name = player.Data?["Name"]?.Value;
        money = Constants.PLAYER_START_MONEY;
    }

    public bool IsConnected()
    {
        return clientId != Constants.NO_CLIENT_ID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref money);
    }
}
