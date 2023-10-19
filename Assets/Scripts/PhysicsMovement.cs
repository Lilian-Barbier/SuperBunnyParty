using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class PhysicsMovement : NetworkBehaviour
{
    public float speed;
    public FixedJoystick fixedJoystick;
    private Rigidbody rb;

    public NetworkVariable<Vector3> currentPosition = new();
    public NetworkVariable<Vector3> currentVelocity = new();

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (!IsServer)
        {
            fixedJoystick = GameObject.FindWithTag("Joystick").GetComponent<FixedJoystick>();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void FixedUpdate()
    {
        if (IsOwner && !IsServer)
        {
            Vector3 direction = Vector3.forward * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal;
            rb.AddForce(direction * speed * Time.fixedDeltaTime, ForceMode.VelocityChange);

            SubmitPositionServerRpc(transform.position, rb.velocity);
        }
        else
        {
            transform.position = currentPosition.Value;
            rb.velocity = currentVelocity.Value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitPositionServerRpc(Vector3 position, Vector3 velocity, ServerRpcParams serverRpcParams = default)
    {
        currentPosition.Value = position;
        currentVelocity.Value = velocity;
    }
}
