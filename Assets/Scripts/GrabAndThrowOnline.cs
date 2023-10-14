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


    private List<Vector3> mousePositions = new List<Vector3>();
    private const int maxMousePositions = 20;

    private Rigidbody rb;
    private Vector3 startPosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }

    private void OnMouseDown()
    {
        Vector3 cursorWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));

        if (!isDraggingOnline.Value && cursorWorldPoint.x > -10)
        {
            //set owner
            // NetworkObject networkObject = GetComponent<NetworkObject>();
            // networkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);

            Debug.Log("OnMouseDownAndCanDrag");
            isCurrentClientDragging = true;
            PlayerGrabServerRpc();
        }
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


    private void OnMouseUp()
    {
        if (isCurrentClientDragging)
        {
            isCurrentClientDragging = false;
            PlayerThrowServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerThrowServerRpc()
    {
        isDraggingOnline.Value = false;
        Vector3 throwVelocity = CalculateThrowVelocity();
        rb.velocity = throwVelocity;
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

            SubmitPositionServerRpc(cursorWorldPoint);
        }
    }

    [ServerRpc(RequireOwnership = false)] //indique que cette méthode est exécutée sur le serveur
    void SubmitPositionServerRpc(Vector3 cursorWorldPoint)
    {
        transform.position = cursorWorldPoint;

        // Add the current mouse position to the list
        mousePositions.Add(cursorWorldPoint);

        // Remove old positions if the list exceeds the maximum count
        if (mousePositions.Count > maxMousePositions)
        {
            mousePositions.RemoveAt(0);
        }
    }
}
