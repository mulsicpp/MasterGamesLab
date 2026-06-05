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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

using MenuId = UI.Menu.MenuId;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public Lobby Lobby { get; private set; }
    public List<Lobby> PublicLobbies { get; private set; }
    public string PlayerName;

    public bool ConnectingToGame { get; private set; } = false;
    public bool IsStartingHost { get; private set; } = false;

    private Coroutine lobbyHeartbeat;
    private Coroutine connectToIngame;

    [SerializeField]
    private UI.JoinUI joinUI;
    [SerializeField]
    private UI.LobbyUI lobbyUI;

    private UI.Menu[] menus;

    private MenuId currentMenu;
    public MenuId CurrentMenu
    {
        get => currentMenu;
        set
        {
            currentMenu = value;
            foreach (var menu in menus)
            {
                menu?.Hide();
            }
            menus[(int)currentMenu]?.Show();
        }
    }

    [SerializeField]
    private bool suppressReconnect = false;

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
        connectToIngame = StartCoroutine(ConnectToIngame());
    }

    public void Start()
    {
        menus = new UI.Menu[Enum.GetValues(typeof(MenuId)).Length];
        var foundMenus = gameObject.GetComponentsInChildren<UI.Menu>();

        Debug.Log("Menu count: " + foundMenus.Length);

        foreach (var menu in foundMenus)
        {
            menus[(int)menu.Id] = menu;
        }

        CurrentMenu = MenuId.Start;
    }

    private void OnDestroy()
    {
        if (lobbyHeartbeat != null)
            StopCoroutine(lobbyHeartbeat);
        if (connectToIngame != null)
            StopCoroutine(connectToIngame);
    }

    public async Task LoadPublicLobbiesAsync()
    {
        try
        {
            PublicLobbies = (await LobbyService.Instance.QueryLobbiesAsync())?.Results;
            joinUI.lobbyList.itemsSource = PublicLobbies;
            joinUI.lobbyList.RefreshItems();
        }
        catch (System.Exception) { }
    }

    public async Task GoToJoinMenu()
    {
        await LoadPublicLobbiesAsync();
        CurrentMenu = MenuId.Join;
    }

    public async Task JoinLobbyByIdAsync(string lobbyId)
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

    public async Task JoinLobbyByCodeAsync(string lobbyCode)
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

        int mapSeed = int.Parse(Lobby.Data["MapSeed"].Value);
        Map.Map.Instance.Generate(mapSeed);

        CurrentMenu = MenuId.Lobby;
    }

    public async Task LeaveLobbyAsync()
    {
        if (Lobby != null)
        {
            await LobbyService.Instance.RemovePlayerAsync(Lobby.Id, AuthenticationService.Instance.PlayerId);
            Lobby = null;
        }
        CurrentMenu = MenuId.Start;
    }

    public async Task CreateLobbyAsync()
    {
        // Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        // string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        int mapSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject> {
            {
                "MapSeed", new DataObject(DataObject.VisibilityOptions.Member, mapSeed.ToString())
            }
            },
            Player = new Player
            {
                Data = new Dictionary<string, PlayerDataObject> {
                {
                    "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName)
                },
            }
            }
        };

        Lobby = await LobbyService.Instance.CreateLobbyAsync(PlayerName + "'s Lobby", 4, options);
        SubscribeToLobby();

        Map.Map.Instance.Generate(mapSeed);

        CurrentMenu = MenuId.Lobby;
    }

    public IEnumerator StartHost()
    {
        if (!IsHost()) yield break;

        IsStartingHost = true;

        try
        {
            UpdateLobbyOptions lockOptions = new UpdateLobbyOptions
            {
                IsPrivate = true,
                IsLocked = true,
            };

            var updateTask = LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, lockOptions);
            yield return new WaitUntil(() => updateTask.IsCompleted);
            if (updateTask.IsCanceled || updateTask.IsFaulted) yield break;

            StartCoroutine(LoadingScreen());

            PlayerManager.Instance.SetPlayersFromLobby(Lobby);

            var allocationTask = RelayService.Instance.CreateAllocationAsync(4);
            yield return new WaitUntil(() => allocationTask.IsCompleted);

            var allocation = allocationTask.Result;
            var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            yield return new WaitUntil(() => joinCodeTask.IsCompleted);

            var relayJoinCode = joinCodeTask.Result;

            UpdateLobbyOptions joinCodeOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                {
                    "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                }
                }
            };

            updateTask = LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, joinCodeOptions);
            yield return new WaitUntil(() => updateTask.IsCompleted);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            SetConnectionData();
            NetworkManager.Singleton.StartHost();
        }
        finally { IsStartingHost = false; }
    }

    private IEnumerator LobbyHeartbeat()
    {
        var delay = new WaitForSecondsRealtime(5f);

        while (true)
        {
            if (IsHost())
            {
                // Debug.Log("Sending heartbeat");
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

    private IEnumerator LoadingScreen()
    {
        CurrentMenu = MenuId.Loading;
        yield return new WaitUntil(() => PlayerManager.Instance.GameCanStart);

        Debug.Log("Game can start");

        Map.Map.Instance.Running = true;
        CurrentMenu = MenuId.Ingame;

        yield break;
    }

    public IEnumerator FinishGame()
    {
        int mapSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        UpdateLobbyOptions options = new UpdateLobbyOptions
        {
            IsPrivate = false,
            IsLocked = false,
            Data = new Dictionary<string, DataObject> {
                {
                    "MapSeed", new DataObject(DataObject.VisibilityOptions.Member, mapSeed.ToString())
                },
                {
                    "JoinCode", null
                }
            }
        };

        var updateTask = LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, options);
        yield return new WaitUntil(() => updateTask.IsCompleted);

        Map.Map.Instance.GameFinishedClientRpc();
        yield break;
    }

    private IEnumerator ConnectToIngame()
    {
        var retryDelay = new WaitForSeconds(1.0f);

        while (true)
        {
            yield return new WaitUntil(() => !suppressReconnect && (Lobby?.Data?.ContainsKey("JoinCode") ?? false) && (!NetworkManager.Singleton?.IsListening ?? false) && !IsHost());
            ConnectingToGame = true;

            Debug.Log("Connecting to host...");

            string relayJoinCode = Lobby.Data["JoinCode"].Value;
            Debug.Log("Relay code: " + relayJoinCode);

            var allocationTask = RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            yield return new WaitUntil(() => allocationTask.IsCompleted);

            if (allocationTask.IsFaulted || allocationTask.IsCanceled)
            {
                Debug.LogError($"Relay Allocation Failed: {allocationTask.Exception?.GetBaseException().Message}");
                yield return retryDelay;
                continue;
            }

            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocationTask.Result, "dtls"));

            SetConnectionData();

            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("StartClient failed to initialize network driver.");
                yield return retryDelay;
                continue;
            }


            float timeoutTimer = 0f;
            float maxTimeout = 8.0f;
            bool connectionVerified = false;

            while (timeoutTimer < maxTimeout)
            {
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    connectionVerified = true;
                    break;
                }

                if (!NetworkManager.Singleton.IsListening)
                {
                    break;
                }

                timeoutTimer += Time.deltaTime;
                yield return null;
            }

            if (connectionVerified)
            {
                Debug.Log("VERIFIED: Successfully connected to host!");
                ConnectingToGame = false;
            }
            else
            {
                Debug.LogError("Connection timed out or was rejected by the host.");
                NetworkManager.Singleton.Shutdown(); // Clean up the failed socket
            }
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

            CurrentMenu = MenuId.Start;
        }
        else
        {
            if (changes.IsLocked.Changed && changes.IsLocked.Value == true)
            {
                StartCoroutine(LoadingScreen());
            }

            changes.ApplyToLobby(Lobby);

            try
            {
                int mapSeed = int.Parse(Lobby.Data?["MapSeed"]?.Value);
                if (Map.Map.Instance.GenerationSeed != mapSeed)
                    Map.Map.Instance.Generate(mapSeed);
            } catch (Exception)
            {
                Map.Map.Instance.GenerateEmpty();
            }

            lobbyUI.UpdateUI(Lobby);
        }

    }

    public bool IsHost()
    {
        return Lobby != null && AuthenticationService.Instance.PlayerId == Lobby?.HostId;
    }

    private void SetConnectionData()
    {
        PlayerConnectData connectData = new PlayerConnectData
        {
            PlayerAuthId = AuthenticationService.Instance.PlayerId,
            Timestamp = Map.Map.Instance.Timestamp,
        };

        byte[] rawData = new byte[Marshal.SizeOf<PlayerConnectData>()];
        MemoryMarshal.Write(rawData, ref connectData);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = rawData;
    }
}
