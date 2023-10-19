using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private NetworkManager netManager;
    UnityTransport unityTransport;


    private const string _internalServerIp = "0.0.0.0";
    private ushort _serverPort = 7777;

    // Start is called before the first frame update
    void Start()
    {
        netManager = NetworkManager.Singleton;
        unityTransport = netManager.GetComponent<UnityTransport>();

        int isSpectator = PlayerPrefs.GetInt("IsSpectator", 0);
        if(isSpectator == 1)
        {
            GameObject.FindWithTag("ControlPanel").SetActive(false);

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            unityTransport.SetServerSecrets(SecureParameters.MyGameServerCertificate, SecureParameters.MyGameServerPrivateKey);
            netManager.StartServer();
        }
        else {
            if (PlayerPrefs.HasKey("IpAdress"))
            {
                unityTransport.ConnectionData.Address = PlayerPrefs.GetString("IpAdress");
            }

            unityTransport.SetClientSecrets(SecureParameters.ServerCommonName, SecureParameters.MyGameClientCA);
            netManager.StartClient();
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
