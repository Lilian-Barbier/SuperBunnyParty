using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Mouse : NetworkBehaviour
{
    private void Start()
    {
        Cursor.visible = false;
    }

    public override void OnNetworkSpawn()
    {

    }

    void Update()
    {
        if (IsOwner)
        {
            //change sprite color to red
            GetComponent<SpriteRenderer>().color = Color.red;

            //Get mouse position in world space and set to x and y of the transform
            Vector3 newPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z - 1));

            //set offset of the size of the sprite2D 
            newPos.x = Math.Clamp(newPos.x + 0.17f, 0, 100);
            newPos.y -= 0.5f;

            transform.position = newPos;
        }

        //Todo : ajouter un raycast pour que la position z soit sur l'objet le plus proche, ou une limite de profondeur

    }
}