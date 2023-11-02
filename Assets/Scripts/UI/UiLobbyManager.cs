using UnityEngine;
using TMPro;

public class UILobbyManager : MonoBehaviour
{
    private string username;

    [SerializeField] CanvasGroup createJoinRoomGroup;
    [SerializeField] CanvasGroup roomGroup;

    // Start is called before the first frame update
    void Start()
    {
        PlayerWrapper.Instance.InitPlayer();
    }

    public void ChangedUsername(string newUsername)
    {
        username = newUsername;
    }

    public void CreateLobby()
    {
        if (username != null && username.Length > 0)
        {
            LobbyWrapper.Instance.CreateLobby(username, 4);
            ShowRoomUi();
        }
    }


    public void JoinLobby(TextMeshProUGUI code)
    {
        if (username != null && username.Length > 0 && code.text != null && code.text.Length > 5)
        {
            //get 6 first characters of the text because text mesh pro add invisible characters at the end
            LobbyWrapper.Instance.JoinLobby(code.text[..6], username);
            ShowRoomUi();
        }
    }

    void ShowRoomUi()
    {
        createJoinRoomGroup.alpha = 0;
        createJoinRoomGroup.interactable = false;
        createJoinRoomGroup.blocksRaycasts = false;

        roomGroup.alpha = 1;
        roomGroup.interactable = true;
        roomGroup.blocksRaycasts = true;

        //Get object name RoomCode in the roomGroup
        var roomCode = roomGroup.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        var playerList = roomGroup.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        roomCode.text = "Room code : " + LobbyWrapper.Instance.GetLocalLobby().LobbyCode.Value;
        playerList.text = "Player 1 : " + PlayerWrapper.Instance.localUser.DisplayName.Value + "\n";

        LobbyWrapper.Instance.GetLocalLobby().onUserJoined += OnUserJoined;
        LobbyWrapper.Instance.GetLocalLobby().onUserLeft += OnUserLeft;
    }

    void OnUserJoined(LocalPlayer localPlayer)
    {
        SynchPlayerUI();
    }

    void OnUserLeft(int i)
    {
        SynchPlayerUI();
    }

    void SynchPlayerUI()
    {
        string playerList = "";
        for (int i = 0; i < LobbyWrapper.Instance.GetLocalLobby().PlayerCount; i++)
        {
            var player = LobbyWrapper.Instance.GetLocalLobby().GetLocalPlayer(i);
            if (player == null)
                continue;

            playerList += "Player" + i + " : " + player.DisplayName + "\n";
        }
    }

}
