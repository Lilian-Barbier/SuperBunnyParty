using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class VolleyBallManager : NetworkBehaviour
{
    private NetworkVariable<int> scoreLeft = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> scoreRight = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> serverFrame = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int localFrame = 0;

    TextMeshProUGUI scoreLeftText;
    TextMeshProUGUI scoreRightText;

    // Start is called before the first frame update
    void Start()
    {
        scoreLeftText = GameObject.FindWithTag("ScoreLeftText").GetComponent<TextMeshProUGUI>();
        scoreRightText = GameObject.FindWithTag("ScoreRightText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            serverFrame.Value += 1;
        }
        if (IsClient)
        {
            localFrame += 1;
        }

        scoreLeftText.text = scoreLeft.Value.ToString();
        scoreRightText.text = scoreRight.Value.ToString();
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        // Debug.Log("POSITION DE MERDER :" + PlayerPrefs.GetString("position"));
        //Debug.Log(collider.gameObject.tag);
        if (PlayerPrefs.GetString("position") == "left" && collider.gameObject.CompareTag("AreaLeft"))
        {
            // Debug.Log("lose left");
            LeftLooseServerRpc();
        }
        if (PlayerPrefs.GetString("position") == "right" && collider.gameObject.CompareTag("AreaRight"))
        {
            // Debug.Log("lose right");
            RightLooseServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void LeftLooseServerRpc()
    {
        // Debug.Log("server test left");
        scoreRight.Value += 1;
        transform.position = new Vector3(-4, 6, 0);
        var rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.up;
        SynchronizePositionEndClientRpc(transform.position, rb.velocity, rb.position, rb.rotation, serverFrame.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    void RightLooseServerRpc()
    {
        // Debug.Log("server test  Right");
        scoreLeft.Value += 1;
        transform.position = new Vector3(-4, 6, 0);
        var rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.up;
        SynchronizePositionEndClientRpc(transform.position, rb.velocity, rb.position, rb.rotation, serverFrame.Value);
    }

    public void LocalCollision(Vector2 velocity)
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.velocity = velocity;
        CollisionServerRpc(transform.position, rb.velocity, rb.position, rb.rotation, localFrame);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CollisionServerRpc(Vector2 position, Vector2 velocity, Vector2 rbPosition, float rotation, int localFrame, ServerRpcParams serverRpcParams = default)
    {
        //Debug.Log("Client want to synch with server. With parameters: " + position + " " + velocity + " " + localFrame);

        //Change position and velocity of the ball on the server based on difference between serverFrame and localFrame number
        // if (serverFrame.Value - localFrame > 1)
        // {
        //     position += velocity * (serverFrame.Value - localFrame);
        // }

        transform.position = position;
        GetComponent<Rigidbody2D>().velocity = velocity;
        GetComponent<Rigidbody2D>().position = rbPosition;
        GetComponent<Rigidbody2D>().rotation = rotation;

        // Send the position to all clients except the client who sent the command
        // (the client who sent the command already knows the position)
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds.Where(c => c != serverRpcParams.Receive.SenderClientId).ToArray()
            }
        };

        SynchronizePositionClientRpc(position, velocity, rbPosition, rotation, serverFrame.Value, clientRpcParams);
    }

    [ClientRpc]
    private void SynchronizePositionClientRpc(Vector2 position, Vector2 velocity, Vector2 rbPosition, float rotation, int serverFrame, ClientRpcParams clientRpcParams = default)
    {
        // Debug.Log("je suis synchronisé et je suis une merde");
        //Change position and velocity of the ball on the server based on difference between serverFrame and localFrame number
        // if (serverFrame - localFrame > 1)
        // {
        //     position += velocity * (serverFrame - localFrame);
        // }


        transform.position = position;
        var rb = GetComponent<Rigidbody2D>();
        rb.velocity = velocity;
        rb.position = rbPosition;
        rb.rotation = rotation;
    }

    [ClientRpc]
    private void SynchronizePositionEndClientRpc(Vector2 position, Vector2 velocity, Vector2 rbPosition, float rotation, int serverFrame, ClientRpcParams clientRpcParams = default)
    {
        //Change position and velocity of the ball on the server based on difference between serverFrame and localFrame number
        // if (serverFrame - localFrame > 1)
        // {
        //     position += velocity * (serverFrame - localFrame);
        // }

        // Debug.Log("end game synch");

        transform.position = position;
        var rb = GetComponent<Rigidbody2D>();
        rb.velocity = velocity;
        rb.position = rbPosition;
        rb.rotation = rotation;
    }
}
