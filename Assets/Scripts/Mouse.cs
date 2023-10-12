using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Mouse : NetworkBehaviour
{
    //NetworkVariable, which means the server player can move immediately, but the client player must request a movement from the server, wait for the server to update the position NetworkVariable, then replicate the change locally.
    //Exemple sur Stardew Valley : c'est l'host qui gére tout les mouvement, si l'host bug on bug aussi
    public NetworkVariable<Vector3> Position = new();

    private void Start()
    {
        Cursor.visible = false;
    }

    public override void OnNetworkSpawn()
    {

    }


    [ServerRpc] //indique que cette méthode est exécutée sur le serveur
    void SubmitPositionRequestServerRpc(Vector3 mousePosition)
    {
        Position.Value = mousePosition;
    }

    void Update()
    {
        if (IsOwner)
        {
            //Get mouse position in world space and set to x and y of the transform
            Vector3 newPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z - 1));

            //set offset of the size of the sprite2D 
            newPos.x += 0.17f;
            newPos.y -= 0.5f;

            transform.position = newPos;
            SubmitPositionRequestServerRpc(newPos);
        }
        else
        {
            transform.position = Position.Value;
        }


        //Todo : ajouter un raycast pour que la position z soit sur l'objet le plus proche, ou une limite de profondeur

    }
}