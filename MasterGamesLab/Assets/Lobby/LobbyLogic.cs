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
    Lobby lobby;

    async void Awake()
    {
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
        return response.Results;
    }

    public async Task<Lobby> JoinLobbyById(string playerName, string lobbyId)
    {
        throw new System.NotImplementedException();
    }

    public async Task<Lobby> JoinLobbyByCode(string playerName, string lobbyCode)
    {
        throw new System.NotImplementedException();
    }

    public async Task<Lobby> CreateLobby(string playerName)
    {
        throw new System.NotImplementedException();
    }

}
