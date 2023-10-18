using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GrabAndThrowOnline : NetworkBehaviour
{
    private Vector3 offset;

    private bool isCurrentClientDragging = false;
    private NetworkVariable<bool> isDraggingOnline = new(false);

    public NetworkVariable<Vector3> Position = new();
    public NetworkVariable<Vector3> Velocity = new();


    private List<Vector3> mousePositions = new List<Vector3>();
    private const int maxMousePositions = 20;

    private Rigidbody rb;
    private Vector3 startPosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }


    public override void OnNetworkSpawn()
    {

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Start");
            InvokeRepeating("SynchronizeGameObject", 0.3f, 0.3f);  //1s delay, repeat every 1s
        }

        base.OnNetworkSpawn();
    }

    void SynchronizeGameObject()
    {
        if (!isDraggingOnline.Value)
        {
            Debug.Log("SynchronizeGameObject");
            Position.Value = transform.position;
            Velocity.Value = rb.velocity;
            SetPositionAndVelocityClientRpc();
        }
    }

    private void OnMouseDown()
    {
        Vector3 cursorWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));

        if (!isDraggingOnline.Value && cursorWorldPoint.x > -10)
        {
            Debug.Log("OnMouseDownAndCanDrag");
            isCurrentClientDragging = true;

            // Reset the position and velocity of the local instance
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            PlayerGrabServerRpc();
        }
    }

    private void OnMouseUp()
    {
        if (isCurrentClientDragging)
        {
            isCurrentClientDragging = false;
            Vector3 throwVelocity = CalculateThrowVelocity();
            rb.velocity = throwVelocity;
            PlayerThrowServerRpc(throwVelocity);
        }
    }

    [ClientRpc]
    void SetPositionAndVelocityClientRpc()
    {
        transform.position = Position.Value;
        rb.velocity = Velocity.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerGrabServerRpc()
    {
        isDraggingOnline.Value = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Clear the mousePositions list when dragging starts
        mousePositions.Clear();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerThrowServerRpc(Vector3 velocity)
    {
        isDraggingOnline.Value = false;
        rb.velocity = velocity;
    }

    private Vector3 CalculateThrowVelocity()
    {
        if (mousePositions.Count < 2)
        {
            return Vector3.zero;
        }

        Vector3 dragVector = Vector3.zero;

        for (int i = 1; i < mousePositions.Count; i++)
        {
            dragVector += mousePositions[i] - mousePositions[i - 1];
        }

        // Calculate the average dragVector over the last 10 positions
        dragVector /= mousePositions.Count - 1;

        dragVector = dragVector.normalized * 15f;

        return dragVector;
    }


    private void Update()
    {
        if (isCurrentClientDragging)
        {
            Vector3 cursorWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));

            if (cursorWorldPoint.x < 0)
            {
                cursorWorldPoint.x = 0;
            }
            transform.position = cursorWorldPoint;

            //todo : ajouter une limite pour n'envoyer un update que tout les 0.2s
            SubmitPositionServerRpc(cursorWorldPoint);

            // Add the current mouse position to the list
            mousePositions.Add(cursorWorldPoint);

            // Remove old positions if the list exceeds the maximum count
            if (mousePositions.Count > maxMousePositions)
            {
                mousePositions.RemoveAt(0);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)] //indique que cette méthode est exécutée sur le serveur
    void SubmitPositionServerRpc(Vector3 cursorWorldPoint, ServerRpcParams serverRpcParams = default)
    {
        Position.Value = cursorWorldPoint;
        Velocity.Value = Vector3.zero;

        // Send the position to all clients except the client who sent the command
        // (the client who sent the command already knows the position)
        ulong[] sendToClients = new ulong[NetworkManager.Singleton.ConnectedClientsList.Count - 1];
        int index = 0;
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id != serverRpcParams.Receive.SenderClientId)
            {
                sendToClients[index] = id;
                index++;
            }
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = sendToClients
            }
        };

        SynchronizePositionClientRpc(clientRpcParams);
    }

    [ClientRpc]
    void SynchronizePositionClientRpc(ClientRpcParams clientRpcParams = default)
    {
        transform.position = Position.Value;
        rb.velocity = Velocity.Value;
    }

}
