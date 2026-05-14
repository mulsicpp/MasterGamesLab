using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using System.ComponentModel.Design.Serialization;

public class LobbyLogic : MonoBehaviour
{
    public static LobbyLogic Instance { get; private set; }
    public Lobby Lobby { get; private set; }
    public List<Lobby> PublicLobbies { get; private set; }
    public string PlayerName;

    async void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async Task<List<Lobby>> LoadPublicLobbies()
    {
        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
        return PublicLobbies =  response.Results;
    }

    public async Task<Lobby> JoinLobbyById(string lobbyId)
    {
        var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        return await JoinLobby(lobby);
    }

    public async Task<Lobby> JoinLobbyByCode(string lobbyCode)
    {
        var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
        return await JoinLobby(lobby);
    }

    private async Task<Lobby> JoinLobby(Lobby lobby)
    {
        Debug.Log("Joined the lobby" + lobby.Name);

        string relayJoinCode = lobby.Data["JoinCode"].Value;
        Debug.Log("Relay code: " + relayJoinCode);

        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.StartClient();

        return Lobby = lobby;
    }

    public async Task<Lobby> CreateLobby()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject> {
            {
                "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
            }
        }
        };

        Lobby = await LobbyService.Instance.CreateLobbyAsync(PlayerName, 4, options);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.StartHost();

        return Lobby;
    }

}
