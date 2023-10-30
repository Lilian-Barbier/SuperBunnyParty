using Unity.Netcode;

public class GameSelectionManager : NetworkBehaviour
{

    public void LoadLevel(string levelName)
    {
        LoadLevelServerRpc(levelName);
    }

    [ServerRpc(RequireOwnership = false)]
    void LoadLevelServerRpc(string levelName)
    {
        NetworkManager.SceneManager.LoadScene(levelName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
