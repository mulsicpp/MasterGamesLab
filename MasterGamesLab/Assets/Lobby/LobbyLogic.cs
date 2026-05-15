using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Collections.Generic;

public class LobbyLogic : MonoBehaviour
{
    public static LobbyLogic Instance { get; private set; }
    public Lobby Lobby { get; private set; }
    public List<Lobby> PublicLobbies { get; private set; }
    public string PlayerName;

    private Coroutine lobbyHeartbeat;

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

        lobbyHeartbeat = StartCoroutine(LobbyHeartbeat());
    }

    private void OnDestroy()
    {
        if (lobbyHeartbeat != null)
            StopCoroutine(lobbyHeartbeat);
    }

    public async Task<List<Lobby>> LoadPublicLobbies()
    {
        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
        return PublicLobbies = response.Results;
    }

    public async Task<Lobby> JoinLobbyById(string lobbyId)
    {
        JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
        {
            Player = new Player
            {
                Data = new Dictionary<string, PlayerDataObject> {
                {
                    "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName)
                }
            }
            }
        };

        var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
        return await JoinLobby(lobby);
    }

    public async Task<Lobby> JoinLobbyByCode(string lobbyCode)
    {
        JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
        {
            Player = new Player
            {
                Data = new Dictionary<string, PlayerDataObject> {
                {
                    "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName)
                }
            }
            }
        };

        var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
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

        Lobby = lobby;
        SubscribeToLobby();

        return Lobby;
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
            },
            Player = new Player
            {
                Data = new Dictionary<string, PlayerDataObject> {
                {
                    "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName)
                }
            }
            }
        };

        Lobby = await LobbyService.Instance.CreateLobbyAsync(PlayerName, 4, options);
        SubscribeToLobby();

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.StartHost();

        return Lobby;
    }

    private IEnumerator LobbyHeartbeat()
    {
        var delay = new WaitForSecondsRealtime(5f);

        while (true)
        {
            if (Lobby != null)
            {
                Debug.Log("Sending heartbeat");
                Task heartbeatTask = LobbyService.Instance.SendHeartbeatPingAsync(Lobby.Id);

                yield return new WaitUntil(() => heartbeatTask.IsCompleted);

                if (heartbeatTask.IsFaulted)
                {
                    System.Exception e = heartbeatTask.Exception?.InnerException ?? heartbeatTask.Exception;
                    Debug.LogWarning($"[Lobby] Heartbeat failed. The lobby might have timed out or been deleted: {e?.Message}");

                    Lobby = null;
                }
            }

            yield return delay;
        }
    }

    private async void SubscribeToLobby()
    {
        if (Lobby != null)
        {
            LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;

            try
            {
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(Lobby.Id, callbacks);
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        if (changes.LobbyDeleted)
        {
            
        }
        else
        {
            changes.ApplyToLobby(Lobby);
        }

        // TODO update UI
    }

}
