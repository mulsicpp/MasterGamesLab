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

    [SerializeField]
    private StartUI startUI;
    [SerializeField]
    private JoinUI joinUI;
    [SerializeField]
    private LobbyUI lobbyUI;

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

    public async Task LoadPublicLobbies()
    {
        try
        {
            PublicLobbies = (await LobbyService.Instance.QueryLobbiesAsync())?.Results;
            joinUI.lobbyList.itemsSource = PublicLobbies;
            joinUI.lobbyList.RefreshItems();
        } catch (System.Exception) {}
    }

    public async Task GoToJoinMenu()
    {
        await LoadPublicLobbies();
        ShowJoinUI();
    }

    public async Task JoinLobbyById(string lobbyId)
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
        JoinLobby(lobby);
    }

    public async Task JoinLobbyByCode(string lobbyCode)
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

        JoinLobby(lobby);
    }

    private void JoinLobby(Lobby lobby)
    {
        Debug.Log("Joined the lobby" + lobby.Name);

        // string relayJoinCode = lobby.Data["JoinCode"].Value;
        // Debug.Log("Relay code: " + relayJoinCode);
        // 
        // JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
        // var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        // transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
        // 
        // NetworkManager.Singleton.StartClient();

        Lobby = lobby;
        SubscribeToLobby();

        ShowLobbyUI();
    }

    public async Task LeaveLobby()
    {
        await LobbyService.Instance.RemovePlayerAsync(Lobby.Id, AuthenticationService.Instance.PlayerId);
        Lobby = null;
        // NetworkManager.Singleton.Shutdown();
        ShowStartUI();
    }

    public async Task CreateLobby()
    {
        // Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        // string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            // Data = new Dictionary<string, DataObject> {
            // {
            //     "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
            // }
            // },
            Player = new Player
            {
                Data = new Dictionary<string, PlayerDataObject> {
                {
                    "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName)
                }
            }
            }
        };

        Lobby = await LobbyService.Instance.CreateLobbyAsync(PlayerName + "'s Lobby", 4, options);
        SubscribeToLobby();

        // var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        // transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
        // NetworkManager.Singleton.StartHost();

        ShowLobbyUI();
    }

    public async Task StartHost()
    {
        if (!IsHost()) return;

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        UpdateLobbyOptions options = new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject> {
            {
                "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
            }
            }
        };

        Lobby = await LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, options);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
        NetworkManager.Singleton.StartHost();
    }

    private IEnumerator LobbyHeartbeat()
    {
        var delay = new WaitForSecondsRealtime(5f);

        while (true)
        {
            if (IsHost())
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
        if (changes.LobbyDeleted || Lobby == null)
        {
            Lobby = null;
            ShowStartUI();
        }
        else
        {
            changes.ApplyToLobby(Lobby);
            lobbyUI.UpdateUI(Lobby);
        }

    }

    public void HideUI()
    {
        startUI.Hide();
        joinUI.Hide();
        lobbyUI.Hide();
    }

    public void ShowStartUI()
    {
        startUI.playerName.SetValueWithoutNotify(PlayerName);
        HideUI();
        startUI.Show();
    }

    public void ShowJoinUI()
    {
        HideUI();
        joinUI.Show();
    }

    public void ShowLobbyUI()
    {
        lobbyUI.UpdateUI(Lobby);
        HideUI();
        lobbyUI.Show();
    }

    public bool IsHost()
    {
        return Lobby != null && AuthenticationService.Instance.PlayerId == Lobby?.HostId;
    }
}
