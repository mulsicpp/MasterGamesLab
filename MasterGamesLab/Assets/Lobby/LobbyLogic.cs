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

public class LobbyLogic : MonoBehaviour
{
    public static LobbyLogic Instance { get; private set; }
    public Lobby Lobby { get; private set; }
    public List<Lobby> PublicLobbies { get; private set; }
    public string PlayerName;

    public bool ConnectingToGame { get; private set; } = false;

    private Coroutine lobbyHeartbeat;
    private Coroutine connectToIngame;

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
        connectToIngame = StartCoroutine(ConnectToIngame());
        startUI.Show();
        joinUI.Hide();
        lobbyUI.Hide();
    }

    private void OnDestroy()
    {
        if (lobbyHeartbeat != null)
            StopCoroutine(lobbyHeartbeat);
        if (connectToIngame != null)
            StopCoroutine(connectToIngame);
    }

    public async Task LoadPublicLobbies()
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

        int mapSeed = int.Parse(Lobby.Data["MapSeed"].Value);
        Map.Map.Instance.Generate(mapSeed);

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

        int mapSeed = Random.Range(int.MinValue, int.MaxValue);

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

        ShowLobbyUI();
    }

    public async Task StartHost()
    {
        if (!IsHost()) return;

        PlayerManager.Instance.SetPlayersFromLobby(Lobby);

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        UpdateLobbyOptions options = new UpdateLobbyOptions
        {
            IsPrivate = true,
            Data = new Dictionary<string, DataObject> {
            {
                "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
            }
            }
        };

        Lobby = await LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, options);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        SetConnectionData();
        NetworkManager.Singleton.StartHost();

        HideUI();
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

    private IEnumerator ConnectToIngame()
    {
        var retryDelay = new WaitForSeconds(1.0f);

        while (true)
        {
            yield return new WaitUntil (() => (Lobby?.Data?.ContainsKey("JoinCode") ?? false) && (!NetworkManager.Singleton?.IsListening ?? false) && !IsHost());
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
                HideUI();
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
            ShowStartUI();
        }
        else
        {
            changes.ApplyToLobby(Lobby);

            // if ((Lobby.Data?.ContainsKey("JoinCode") ?? false) && !NetworkManager.Singleton.IsListening && !IsHost())
            // {
            //     Debug.Log("Connecting to host");
            // 
            //     string relayJoinCode = Lobby.Data["JoinCode"].Value;
            //     Debug.Log("Relay code: " + relayJoinCode);
            //     
            //     JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            //     var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            //     transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            //     
            //     NetworkManager.Singleton.StartClient();
            // 
            //     HideUI();
            // 
            //     Debug.Log("Successfully connected to host");
            // }

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
        startUI.playerName.Value = PlayerName;
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

    private void SetConnectionData()
    {
        PlayerConnectData connectData = new PlayerConnectData
        {
            PlayerAuthId = AuthenticationService.Instance.PlayerId,
            MapSyncData = Map.Map.Instance.GetSyncData(),
        };

        byte[] rawData = new byte[Marshal.SizeOf<PlayerConnectData>()];
        MemoryMarshal.Write(rawData, ref connectData);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = rawData;
    }
}
