using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using System.ComponentModel.Design.Serialization;

public class Connector : MonoBehaviour
{
    ConnectionData data;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        // You also need to sign in (Anonymous is easiest for testing)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public void OnGUI()
    {
        data.name = GUI.TextField(new Rect(20, 20, 300, 30), data.name);
        data.joinCode = GUI.TextField(new Rect(20, 60, 300, 30), data.joinCode);

        if (GUI.Button(new Rect(20, 100, 140, 30), "Host"))
        {
            _ = StartHost(data);
        };

        if (GUI.Button(new Rect(180, 100, 140, 30), "Join"))
        {
            _ = Join(data);
        };
    }

    public async Task StartHost(ConnectionData data)
    {
        // 1. Setup Relay and get the Join Code
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // 2. Set up Lobby Data (This is where the magic happens)
        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject> {
            {
                "JoinCode", // The key name
                new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
            }
        }
        };

        // 3. Create the Lobby
        var lobby = await LobbyService.Instance.CreateLobbyAsync("My Awesome Room", 4, options);

        // 4. Start the actual Netcode Host
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.ConnectionData,
            false
        );

        NetworkManager.Singleton.StartHost();
    }

    public async Task Join(ConnectionData data)
    {
        Debug.Log("Joining");

        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();

        Debug.Log("Result count: " + response.Results.Count);
        var foundLobby = response.Results[0]; // Just grabbing the first one for this example

        // 2. Join the Lobby
        var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(foundLobby.Id);
        Debug.Log("Joined the lobby" + lobby);
        Debug.Log("Lobby data is null: " + (lobby.Data == null));

        // 3. Extract the Relay Join Code from the Lobby's data
        string relayJoinCode = lobby.Data["JoinCode"].Value;
        Debug.Log("Relay code: " + relayJoinCode);

        // 4. Join the Relay
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            false
        );

        NetworkManager.Singleton.StartClient();
    }
}

public struct ConnectionData
{
    public string name;
    public string joinCode;
}
