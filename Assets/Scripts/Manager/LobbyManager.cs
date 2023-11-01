using System;
using System.Collections.Generic;
using System.Text;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;


#if UNITY_EDITOR
using System.Security.Cryptography;
#endif

public class LobbyManager : MonoBehaviour
{

    LobbyEventCallbacks lobbyEventCallbacks = new LobbyEventCallbacks();
    public Lobby CurrentLobby { get; private set; }
    private Task heartBeatTask;


    // Start is called before the first frame update
    void Start()
    {
        if (string.IsNullOrEmpty(Application.cloudProjectId))
        {
            OnSignInFailed();
            return;
        }

        TrySignIn();
    }

    private async void TrySignIn()
    {
        try
        {
            var unityAuthenticationInitOptions = GenerateAuthenticationOptions(GetProfile());
            await InitializeAndSignInAsync(unityAuthenticationInitOptions);
            OnAuthSignIn();
            //m_ProfileManager.onProfileChanged += OnProfileChanged;
        }

        catch (Exception)
        {
            OnSignInFailed();
        }
    }
    private void OnAuthSignIn()
    {

        Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

        //m_LocalUser.ID = AuthenticationService.Instance.PlayerId;


    }

    private void OnSignInFailed()
    {
        Debug.Log("Sign in failed");
    }


    static string GetProfile()
    {
        var arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == "-AuthProfile")
            {
                var profileId = arguments[i + 1];
                return profileId;
            }
        }

#if UNITY_EDITOR

        // When running in the Editor make a unique ID from the Application.dataPath.
        // This will work for cloning projects manually, or with Virtual Projects.
        // Since only a single instance of the Editor can be open for a specific
        // dataPath, uniqueness is ensured.
        var hashedBytes = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
        Array.Resize(ref hashedBytes, 16);
        // Authentication service only allows profile names of maximum 30 characters. We're generating a GUID based
        // on the project's path. Truncating the first 30 characters of said GUID string suffices for uniqueness.
        return new Guid(hashedBytes).ToString("N")[..30];
#else
            return "";
#endif
    }


    public InitializationOptions GenerateAuthenticationOptions(string profile)
    {
        try
        {
            var unityAuthenticationInitOptions = new InitializationOptions();
            if (profile.Length > 0)
            {
                unityAuthenticationInitOptions.SetProfile(profile);
            }

            return unityAuthenticationInitOptions;
        }
        catch (Exception e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})";
            //m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
            throw;
        }
    }

    public async Task InitializeAndSignInAsync(InitializationOptions initializationOptions)
    {
        try
        {
            await UnityServices.InitializeAsync(initializationOptions);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})";
            //m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
            throw;
        }
    }

    public async void CreateLobby()
    {
        CurrentLobby = await CreateLobbyAsync("test", 2, false, "Joueur1", null);
        await BindLocalLobbyToRemote(CurrentLobby.Id, new LocalLobby());
        Debug.Log($"Lobby created. Lobby ID {CurrentLobby.Id} lobby code {CurrentLobby.LobbyCode}");

    }

    public void JoinLobby(TextMeshProUGUI text)
    {
        //get 6 first characters of the text because text mesh pro add invisible characters at the end
        string lobbyCode = text.text[..6];
        JoinLobby(lobbyCode);
    }

    public async void JoinLobby(string lobbyCode)
    {
        try
        {
            //get lobbyCode from ui field
            Debug.Log($"Joining lobby {lobbyCode.Trim()}");
            var lobby = await JoinLobbyAsync(lobbyCode.Trim(), "joueur2");

            //LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);

        }
        catch (LobbyServiceException exception)
        {
            Debug.LogError("Failed to join lobby: " + exception.Message);
        }
    }

    public async Task<Lobby> JoinLobbyAsync(string lobbyCode, string userName)
    {
        string uasId = AuthenticationService.Instance.PlayerId;
        var playerData = CreateInitialPlayerData(userName);

        JoinLobbyByCodeOptions joinOptions = new() { Player = new Player(id: uasId, data: playerData) };
        var joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);

        return joinedLobby;
    }

    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, string userName, string password)
    {
        string uasId = AuthenticationService.Instance.PlayerId;

        CreateLobbyOptions createOptions = new CreateLobbyOptions
        {
            IsPrivate = isPrivate,
            Player = new Player(id: uasId, data: CreateInitialPlayerData(userName)),
            Password = password
        };
        var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
        StartHeartBeat();

        return lobby;
    }

    #region HeartBeat

    //Since the LobbyManager maintains the "connection" to the lobby, we will continue to heartbeat until host leaves.
    async Task SendHeartbeatPingAsync()
    {
        if (CurrentLobby == null)
            return;

        await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
    }

    void StartHeartBeat()
    {
        heartBeatTask = HeartBeatLoop();
    }

    async Task HeartBeatLoop()
    {
        while (CurrentLobby != null)
        {
            await SendHeartbeatPingAsync();
            await Task.Delay(8000);
        }
    }

    #endregion

    Dictionary<string, PlayerDataObject> CreateInitialPlayerData(string userName)
    {
        Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

        var displayNameObject = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, userName);
        data.Add("DisplayName", displayNameObject);
        return data;
    }


    const string key_RelayCode = nameof(LocalLobby.RelayCode);
    const string key_LobbyState = nameof(LocalLobby.LocalLobbyState);
    const string key_LobbyColor = nameof(LocalLobby.LocalLobbyColor);
    const string key_LastEdit = nameof(LocalLobby.LastUpdated);

    const string key_Displayname = nameof(LocalPlayer.DisplayName);
    const string key_Userstatus = nameof(LocalPlayer.UserStatus);


    public async Task BindLocalLobbyToRemote(string lobbyID, LocalLobby localLobby)
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

                Debug.Log($"Player {id} joined the lobby");
                localLobby.AddPlayer(index, newPlayer);
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
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, lobbyEventCallbacks);
    }

}
