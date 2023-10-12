using Unity.Netcode;
using UnityEngine;

public class HelloWorldPlayer : NetworkBehaviour
{
    //NetworkVariable, which means the server player can move immediately, but the client player must request a movement from the server, wait for the server to update the position NetworkVariable, then replicate the change locally.
    //Exemple sur Stardew Valley : c'est l'host qui gére tout les mouvement, si l'host bug on bug aussi
    public NetworkVariable<Vector3> Position = new();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Move();
        }
    }

    public void Move()
    {
        //Si le mouvement est demandé par le serveur (HelloWorldManager.cs)
        //On entre également dans cette condition dans le cas ou le client = le serveur (host)
        if (NetworkManager.Singleton.IsServer)
        {
            var randomPosition = GetRandomPositionOnPlane();
            transform.position = randomPosition;

            //Si on est le serveur on peut directement changer la position du joueur (vue que Position est de type NetworkVariable<Vector3> elle est stocké sur le serveur)
            Position.Value = randomPosition;
        }
        //Si le mouvement est demandé par le client
        else
        {
            //On appelle une méthode du serveur pour changer la position
            SubmitPositionRequestServerRpc();
        }
    }


    [ServerRpc] //indique que cette méthode est exécutée sur le serveur
    void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        Position.Value = GetRandomPositionOnPlane();
    }

    static Vector3 GetRandomPositionOnPlane()
    {
        return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
    }

    void Update()
    {
        transform.position = Position.Value;
    }
}