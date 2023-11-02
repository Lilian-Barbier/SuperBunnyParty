

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;

public class LobbyWrapper : MonoBehaviour
{
    private static LobbyWrapper instance;

    // Static singleton property
    public static LobbyWrapper Instance
    {
        // ajout ET création du composant à un GameObject nommé "SingletonHolder" 
        get { return instance != null ? instance : (instance = new GameObject("SingletonHolder").AddComponent<LobbyWrapper>()); }
        private set { instance = value; }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);//le GameObject qui porte ce script ne sera pas détruit
    }

    Lobby lobby;
    Task heartBeatTask;

    public LocalLobby GetLocalLobby()
    {
        return localLobby;
    }

    LocalLobby localLobby = new();
    LobbyEventCallbacks lobbyEventCallbacks = new();

    public async void JoinLobby(string lobbyCode, string username)
    {
        try
        {
            //get lobbyCode from ui field
            Debug.Log($"Joining lobby {lobbyCode.Trim()}");

            string uasId = AuthenticationService.Instance.PlayerId;
            var playerData = CreateInitialPlayerData(username);

            JoinLobbyByCodeOptions joinOptions = new() { Player = new Player(id: uasId, data: playerData) };
            lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);

            await SubscribeToLobbyEvent();
            //LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);

        }
        catch (LobbyServiceException exception)
        {
            Debug.LogError("Failed to join lobby: " + exception.Message);
        }
    }

    //create player data object, used for shared DisplayName property with other player in lobby
    Dictionary<string, PlayerDataObject> CreateInitialPlayerData(string userName)
    {
        Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

        var displayNameObject = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, userName);
        data.Add("DisplayName", displayNameObject);
        return data;
    }

    public async void CreateLobby(string userName, int numberOfPlayers = 4)
    {
        //generate random lobby name
        string lobbyName = Path.GetRandomFileName().Replace(".", "").Substring(0, 4);

        string uasId = AuthenticationService.Instance.PlayerId;


        CreateLobbyOptions createOptions = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = new Player(id: uasId, data: CreateInitialPlayerData(userName)),
        };
        lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, numberOfPlayers, createOptions);

        StartHeartBeat();

        await SubscribeToLobbyEvent();
        Debug.Log($"Lobby created. Lobby ID {lobby.Id} lobby code {lobby.LobbyCode}");
    }

    #region HeartBeat


    // We need to call the lobby periodically to keep it alive : https://docs.unity.com/ugs/en-us/manual/lobby/manual/heartbeat-a-lobby
    void StartHeartBeat()
    {
        heartBeatTask = HeartBeatLoop();
    }

    async Task HeartBeatLoop()
    {
        while (lobby != null)
        {
            await SendHeartbeatPingAsync();
            await Task.Delay(8000);
        }
    }

    async Task SendHeartbeatPingAsync()
    {
        if (lobby == null)
            return;

        await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
    }

    #endregion

    const string key_RelayCode = nameof(LocalLobby.RelayCode);
    const string key_LobbyState = nameof(LocalLobby.LocalLobbyState);
    const string key_LobbyColor = nameof(LocalLobby.LocalLobbyColor);
    const string key_LastEdit = nameof(LocalLobby.LastUpdated);

    const string key_Displayname = nameof(LocalPlayer.DisplayName);
    const string key_Userstatus = nameof(LocalPlayer.UserStatus);


    public async Task SubscribeToLobbyEvent()
    {
        lobbyEventCallbacks.LobbyDeleted += async () =>
        {
            //await LeaveLobbyAsync();
        };

        lobbyEventCallbacks.DataChanged += changes =>
        {
            foreach (var change in changes)
            {
                var changedValue = change.Value;
                var changedKey = change.Key;

                if (changedKey == key_RelayCode)
                    localLobby.RelayCode.Value = changedValue.Value.Value;

                if (changedKey == key_LobbyState)
                    localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(changedValue.Value.Value);

                if (changedKey == key_LobbyColor)
                    localLobby.LocalLobbyColor.Value = (LobbyColor)int.Parse(changedValue.Value.Value);
            }
        };

        lobbyEventCallbacks.DataAdded += changes =>
        {
            foreach (var change in changes)
            {
                var changedValue = change.Value;
                var changedKey = change.Key;

                if (changedKey == key_RelayCode)
                    localLobby.RelayCode.Value = changedValue.Value.Value;

                if (changedKey == key_LobbyState)
                    localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(changedValue.Value.Value);

                if (changedKey == key_LobbyColor)
                    localLobby.LocalLobbyColor.Value = (LobbyColor)int.Parse(changedValue.Value.Value);
            }
        };

        lobbyEventCallbacks.DataRemoved += changes =>
        {
            foreach (var change in changes)
            {
                var changedKey = change.Key;
                if (changedKey == key_RelayCode)
                    localLobby.RelayCode.Value = "";
            }
        };

        lobbyEventCallbacks.PlayerLeft += players =>
        {
            foreach (var leftPlayerIndex in players)
            {
                localLobby.RemovePlayer(leftPlayerIndex);
            }
        };

        lobbyEventCallbacks.PlayerJoined += players =>
        {
            foreach (var playerChanges in players)
            {
                Player joinedPlayer = playerChanges.Player;

                var id = joinedPlayer.Id;
                var index = playerChanges.PlayerIndex;
                var isHost = localLobby.HostID.Value == id;


                var newPlayer = new LocalPlayer(id, index, isHost);

                foreach (var dataEntry in joinedPlayer.Data)
                {
                    var dataObject = dataEntry.Value;
                    //ParseCustomPlayerData(newPlayer, dataEntry.Key, dataObject.Value);
                }

                //debug player username 
                var displayName = joinedPlayer.Data["DisplayName"].Value;
                Debug.Log($"Player {index} joined the lobby with username {displayName}");
                localLobby.AddPlayer(newPlayer);
            }
        };

        lobbyEventCallbacks.PlayerDataChanged += changes =>
        {
            foreach (var lobbyPlayerChanges in changes)
            {
                var playerIndex = lobbyPlayerChanges.Key;
                var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                if (localPlayer == null)
                    continue;
                var playerChanges = lobbyPlayerChanges.Value;

                //There are changes on the Player
                foreach (var playerChange in playerChanges)
                {
                    var changedValue = playerChange.Value;

                    //There are changes on some of the changes in the player list of changes
                    var playerDataObject = changedValue.Value;
                    //ParseCustomPlayerData(localPlayer, playerChange.Key, playerDataObject.Value);
                    Debug.Log($"Player {playerIndex} changed {playerChange.Key} to {playerDataObject.Value}");

                }
            }
        };

        lobbyEventCallbacks.PlayerDataAdded += changes =>
        {
            foreach (var lobbyPlayerChanges in changes)
            {
                var playerIndex = lobbyPlayerChanges.Key;
                var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                if (localPlayer == null)
                    continue;
                var playerChanges = lobbyPlayerChanges.Value;

                //There are changes on the Player
                foreach (var playerChange in playerChanges)
                {
                    var changedValue = playerChange.Value;

                    //There are changes on some of the changes in the player list of changes
                    var playerDataObject = changedValue.Value;
                    //ParseCustomPlayerData(localPlayer, playerChange.Key, playerDataObject.Value);
                    Debug.Log($"Player {playerIndex} changed {playerChange.Key} to {playerDataObject.Value}");
                }
            }
        };

        lobbyEventCallbacks.PlayerDataRemoved += changes =>
        {
            foreach (var lobbyPlayerChanges in changes)
            {
                var playerIndex = lobbyPlayerChanges.Key;
                var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                if (localPlayer == null)
                    continue;
                var playerChanges = lobbyPlayerChanges.Value;

                //There are changes on the Player
                if (playerChanges == null)
                    continue;

                foreach (var playerChange in playerChanges.Values)
                {
                    //There are changes on some of the changes in the player list of changes
                    Debug.LogWarning("This Sample does not remove Player Values currently.");
                }
            }
        };

        lobbyEventCallbacks.LobbyChanged += async changes =>
        {
            //Lobby Fields
            if (changes.Name.Changed)
                localLobby.LobbyName.Value = changes.Name.Value;
            if (changes.HostId.Changed)
                localLobby.HostID.Value = changes.HostId.Value;
            if (changes.IsPrivate.Changed)
                localLobby.Private.Value = changes.IsPrivate.Value;
            if (changes.IsLocked.Changed)
                localLobby.Locked.Value = changes.IsLocked.Value;
            if (changes.AvailableSlots.Changed)
                localLobby.AvailableSlots.Value = changes.AvailableSlots.Value;
            if (changes.MaxPlayers.Changed)
                localLobby.MaxPlayerCount.Value = changes.MaxPlayers.Value;

            if (changes.LastUpdated.Changed)
                localLobby.LastUpdated.Value = changes.LastUpdated.Value.ToFileTimeUtc();

            //Custom Lobby Fields

            if (changes.PlayerData.Changed)
                PlayerDataChanged();

            void PlayerDataChanged()
            {
                foreach (var lobbyPlayerChanges in changes.PlayerData.Value)
                {
                    var playerIndex = lobbyPlayerChanges.Key;
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;
                    var playerChanges = lobbyPlayerChanges.Value;
                    if (playerChanges.ConnectionInfoChanged.Changed)
                    {
                        var connectionInfo = playerChanges.ConnectionInfoChanged.Value;
                        Debug.Log(
                            $"ConnectionInfo for player {playerIndex} changed to {connectionInfo}");
                    }

                    if (playerChanges.LastUpdatedChanged.Changed) { }
                }
            }
        };

        lobbyEventCallbacks.LobbyEventConnectionStateChanged += lobbyEventConnectionState =>
        {
            Debug.Log($"Lobby ConnectionState Changed to {lobbyEventConnectionState}");
        };

        lobbyEventCallbacks.KickedFromLobby += () =>
        {
            Debug.Log("Left Lobby");
            //Dispose();
        };

        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, lobbyEventCallbacks);
    }


    public void LeaveLobby()
    {
        throw new System.NotImplementedException();
    }

    public void OnGameStarted(UnityAction callback)
    {
        throw new System.NotImplementedException();
    }

    public void OnLobbyJoined(UnityAction callback)
    {
        throw new System.NotImplementedException();
    }

    public void OnLobbyLeft(UnityAction callback)
    {
        throw new System.NotImplementedException();
    }

    public void OnLobbyNameChanged(UnityAction<string> callback)
    {
        throw new System.NotImplementedException();
    }

    public void OnMaxPlayersChanged(UnityAction<int> callback)
    {
        throw new System.NotImplementedException();
    }

    public void OnPlayerNameChanged(UnityAction<string> callback)
    {
        throw new System.NotImplementedException();
    }

    public void OnPlayerReady(UnityAction<bool> callback)
    {
        throw new System.NotImplementedException();
    }

    public void SetLobbyName(string lobbyName)
    {
        throw new System.NotImplementedException();
    }

    public void SetMaxPlayers(int maxPlayers)
    {
        throw new System.NotImplementedException();
    }

    public void SetPlayerName(string playerName)
    {
        throw new System.NotImplementedException();
    }

    public void SetPlayerReady(bool isReady)
    {
        throw new System.NotImplementedException();
    }

    public void StartGame()
    {
        throw new System.NotImplementedException();
    }

    public string GetLobbyCode()
    {
        return lobby.LobbyCode;
    }

}