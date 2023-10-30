using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class MovingSphere : NetworkBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    bool onGround = false;
    bool desiredJump;
    float timerCanJump = 0;
    float delayCanJump = 0.5f;


    Vector2 velocity = Vector2.zero;
    Vector2 desiredVelocity = Vector2.zero;
    Vector2 contactNormal;


    Rigidbody2D body;

    float minGroundDotProduct;

    //Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        OnValidate();
    }

    void Start()
    {
        if (IsServer || !IsLocalPlayer)
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
            return;
        }

        Debug.Log("JE SUIS DE : " + PlayerPrefs.GetString("position"));
        if (PlayerPrefs.GetString("position") == "left")
        {
            transform.position = new Vector3(-4, 2, 0);
        }
        else
        {
            transform.position = new Vector3(4, 2, 0);
        }

    }

    void Update()
    {
        if (IsServer || !IsLocalPlayer)
        {
            return;
        }
        desiredJump |= Input.GetButtonDown("Jump");

        //Get player input
        Vector2 playerInput = new(Input.GetAxis("Horizontal"), 0);

        //On clamp au maximum � une magnitude de 1, mieux que Normalize car on garde les valeurs entre 0 et 1.
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        //On multiplie l'input du joueur par la vitesse max pour obtenir la vitesse (si il est � 1, il va � la vitesse max, si il est � 0, il ne bouge pas)
        desiredVelocity = new Vector2(playerInput.x, playerInput.y) * maxSpeed;

    }

    //The FixedUpdate method gets invoked at the start of each physics simulation step
    private void FixedUpdate()
    {
        if (IsServer || !IsLocalPlayer)
        {
            return;
        }

        timerCanJump += Time.deltaTime;

        //On r�cup�re la v�locit� actuelle du joueur (possiblement modifi� par le moteur physique)
        velocity = body.velocity;

        if (!onGround)
        {
            contactNormal = Vector3.up;
        }

        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        //Todo: manque d'une decceleration diff�rentes de l'acc�l�ration

        // https://docs.unity3d.com/ScriptReference/Mathf.MoveTowards.html
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        body.velocity = velocity;

        onGround = false;

    }

    void Jump()
    {
        if (onGround || timerCanJump < delayCanJump)
        {
            //voir la partie How is required velocity derived ? de https://catlikecoding.com/unity/tutorials/movement/physics/ (je ne comprend pas tout)
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

            float alignedSpeed = Vector3.Dot(velocity, contactNormal);
            //Limiting upward velocity
            if (alignedSpeed > 0f)
            {
                jumpSpeed -= Mathf.Max(jumpSpeed - velocity.y, 0f);
            }

            velocity += contactNormal * jumpSpeed;
        }
    }

    Vector2 direction;

    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluateCollision(collision);
        if (IsLocalPlayer && collision.gameObject.CompareTag("PhysicObject"))
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 center = collision.collider.bounds.center;
            direction = center - contactPoint;
            direction.Normalize();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluateCollision(collision);
        if (IsLocalPlayer && collision.gameObject.CompareTag("PhysicObject"))
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 center = collision.collider.bounds.center;
            direction = center - contactPoint;
            direction.Normalize();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        //if is local client and collision with physic object
        if (IsOwner && collision.gameObject.CompareTag("PhysicObject"))
        {
            collision.gameObject.GetComponent<VolleyBallManager>().LocalCollision(direction * body.velocity.magnitude * 3);
        }
    }

    private void EvaluateCollision(Collision2D collision)
    {
        foreach (var contact in collision.contacts)
        {
            Vector3 normal = contact.normal;

            if (normal.y >= minGroundDotProduct)
            {
                onGround = true;
                timerCanJump = 0;
                contactNormal = normal;
            }

            //On utilise l'op�rateur "Or Assignement" (|=) pour ne pas �craser la valeur de onGround si elle est d�j� � true (par un autre contact)
            //onGround |= normal.y >= minGroundDotProduct;
        }
    }
}
