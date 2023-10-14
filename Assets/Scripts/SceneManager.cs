using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SceneManager : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    [ServerRpc(RequireOwnership = false)]
    void RestartSceneServerRpc()
    {
        NetworkManager.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void RestartScene()
    {
        RestartSceneServerRpc();
    }
}
