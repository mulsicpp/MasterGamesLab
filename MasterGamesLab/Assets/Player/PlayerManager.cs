using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public List<PlayerData> players;

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
        PlayerData playerData = new PlayerData();

        playerData.clientid = request.ClientNetworkId;
        playerData.name = System.Text.Encoding.ASCII.GetString(request.Payload);

        response.Approved = true;
        response.CreatePlayerObject = false;

        RegisterPlayer(playerData);
    }

    public void OnClientConnected(ulong clientid)
    {
        UpdatePlayersClientRpc(players.ToArray());
    }

    public void OnClientDisconnect(ulong clientid)
    {
        UnregisterPlayer(clientid);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            UpdatePlayersClientRpc(players.ToArray());
        }
    }



    [Rpc(SendTo.ClientsAndHost)]
    public void UpdatePlayersClientRpc(PlayerData[] players)
    {
        this.players = new List<PlayerData>(players);
    }



    private void RegisterPlayer(PlayerData playerData)
    {
        players.Add(playerData);
    }

    private void UnregisterPlayer(ulong clientid)
    {
        for(int i = 0; i < players.Count; i++)
        {
            if (players[i].clientid == clientid)
            {
                players.RemoveAt(i);
                return;
            }
        }
    }

    public PlayerData? GetSelf()
    {
        foreach (PlayerData playerData in this.players)
        {
            if(playerData.clientid == NetworkManager.Singleton.LocalClientId)
            {
                return playerData;
            }
        }

        return null;
    }
}

[System.Serializable]
public struct PlayerData : INetworkSerializable
{
    public ulong clientid;
    public string name;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientid);
        serializer.SerializeValue(ref name);
    }
}
