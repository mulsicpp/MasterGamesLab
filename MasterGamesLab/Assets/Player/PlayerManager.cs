using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;

namespace Player
{

    public class PlayerManager : NetworkBehaviour
    {
        public static PlayerManager Instance;

        private Dictionary<ClientId, Map.Timestamp> clientTimestamps = new Dictionary<ClientId, Map.Timestamp>();

        public PlayerConnection[] PlayerConnections;
        public PlayerConnection? Self => PlayerConnections[Player.SelfId];

        public int ConnectedPlayerCount => PlayerConnections.Where(d => d.IsConnected).Count();

        public bool GameCanStart => (NetworkManager.Singleton?.IsListening ?? false) && PlayerConnections.Length > 0 && ConnectedPlayerCount == PlayerConnections.Length;

        public void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PlayerConnections = new PlayerConnection[0];
        }

        public void SetPlayersFromLobby(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            PlayerConnections = new PlayerConnection[lobby.Players.Count];
            for (int i = 0; i < lobby.Players.Count; i++)
            {
                PlayerConnections[i] = new PlayerConnection(lobby.Players[i]);
            }
        }

        public void Start()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApproveConnection;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }

        public void OnServerStarted()
        {
            if (NetworkManager.Singleton?.NetworkTickSystem == null) return;
            NetworkManager.Singleton.NetworkTickSystem.Tick += OnNetworkTick;
            NetworkManager.Singleton.NetworkTickSystem.Tick += Map.Map.Instance.Tick;
        }

        public void OnServerStopped(bool _)
        {
            if (NetworkManager.Singleton?.NetworkTickSystem == null) return;
            NetworkManager.Singleton.NetworkTickSystem.Tick -= OnNetworkTick;
            NetworkManager.Singleton.NetworkTickSystem.Tick -= Map.Map.Instance.Tick;
        }


        public void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var data = System.Runtime.InteropServices.MemoryMarshal.Read<PlayerConnectData>(request.Payload);
            string playerAuthId = data.PlayerAuthId.ToString();

            Debug.Log("Incoming connection PlayerId: '" + playerAuthId + "'");

            int index = Array.FindIndex(PlayerConnections, playerData => playerData.PlayerAuthId == playerAuthId);

            if (index == -1)
            {
                response.Approved = false;
                response.CreatePlayerObject = false;
                response.Reason = "PlayerID could not be found";

                Debug.Log(response.Reason);

                return;
            }
            var clientId = new ClientId(request.ClientNetworkId);
            PlayerConnections[index].ClientId = clientId;

            clientTimestamps[clientId] = data.Timestamp;

            response.Approved = true;
            response.CreatePlayerObject = false;
        }

        public void OnClientConnected(ulong clientid)
        {
            if (NetworkManager.Singleton == null) return;
            if (!IsServer) return;

            UpdatePlayersClientRpc(PlayerConnections);

            if (clientid == NetworkManager.Singleton.LocalClientId) return;

            var clientId = new ClientId(clientid);
            Map.Map.Instance.SyncClientMap(clientTimestamps[clientId], clientId);
            clientTimestamps.Remove(clientId);
        }

        public void OnClientDisconnect(ulong clientid)
        {
            if (NetworkManager.Singleton == null) return;
            if (IsServer)
            {
                int index = Array.FindIndex(PlayerConnections, data => data.ClientId == clientid);
                if (index != -1)
                {
                    PlayerConnections[index].ClientId = ClientId.NONE;
                }

                if (NetworkManager.Singleton.IsListening)
                {
                    UpdatePlayersClientRpc(PlayerConnections);
                }
            }
            else if (clientid == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("Client disconnected");
                NetworkManager.Singleton.Shutdown();
            }
        }

        public void OnNetworkTick()
        {
            UpdatePlayersClientRpc(PlayerConnections);
        }


        // TODO Maybe create two different RPCs:
        // - UpdatePlayerStatus (on Connect/Disconnect)
        // - UpdatePlayerData (every server tick)
        [Rpc(SendTo.ClientsAndHost)]
        public void UpdatePlayersClientRpc(PlayerConnection[] players)
        {
            this.PlayerConnections = players;
            var index = Array.FindIndex(players, data => data.PlayerAuthId == AuthenticationService.Instance.PlayerId);
            Player.selfId = index == -1 ? PlayerId.NONE : new PlayerId((byte)index);
        }

        public Player GetPlayerFromClientId(ClientId clientId)
        {
            int index = Array.FindIndex(PlayerConnections, data => data.ClientId == clientId);
            if(index != -1 && index < Map.Map.Instance.Players.Count)
                return Map.Map.Instance.Players[index];
            return null;
        }

        public Color GetPlayerColor(PlayerId playerId)
        {
            return Constants.PLAYER_COLORS[playerId % Constants.MAX_PLAYER_COUNT];
        }
    }

    [System.Serializable]
    public struct PlayerConnection : INetworkSerializable
    {
        public ClientId ClientId;
        public string PlayerAuthId;
        public string Name;

        public PlayerConnection(Unity.Services.Lobbies.Models.Player player)
        {
            ClientId = ClientId.NONE;
            PlayerAuthId = player.Id;
            Name = player.Data?["Name"]?.Value;
        }

        public bool IsConnected => ClientId != ClientId.NONE;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerAuthId);
            serializer.SerializeValue(ref Name);
        }
    }

    public struct PlayerConnectData
    {
        public FixedString64Bytes PlayerAuthId;
        public Map.Timestamp Timestamp;
    }
}
