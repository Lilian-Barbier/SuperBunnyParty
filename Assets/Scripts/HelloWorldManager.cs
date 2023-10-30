using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;


public class HelloWorldManager : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            //nothing started, show the buttons to start server / client / host
            StartButtons();
        }
        else
        {
            StatusLabels();
            //SubmitNewPosition();
        }

        GUILayout.EndArea();
    }

    static void StartButtons()
    {
        // if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
        if (GUILayout.Button("Client Right"))
        {
            PlayerPrefs.SetString("position", "right");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientSecrets(SecureParameters.ServerCommonName, SecureParameters.MyGameClientCA);
            NetworkManager.Singleton.StartClient();
        }
        if (GUILayout.Button("Client Left"))
        {
            PlayerPrefs.SetString("position", "left");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientSecrets(SecureParameters.ServerCommonName, SecureParameters.MyGameClientCA);
            NetworkManager.Singleton.StartClient();
        }
        // if (GUILayout.Button("Server"))
        // {
        //     //get UnityTransport from NetworkManager
        //     var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        //     transport.SetServerSecrets(SecureParameters.MyGameServerCertificate, SecureParameters.MyGameServerPrivateKey);


        //     NetworkManager.Singleton.StartServer();
        // }
    }

    static void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }

    static void SubmitNewPosition()
    {
        if (GUILayout.Button(NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change"))
        {
            if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
            {
                foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                    NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<HelloWorldPlayer>().Move();
            }

            else
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<HelloWorldPlayer>();
                player.Move();
            }
        }
    }
}
