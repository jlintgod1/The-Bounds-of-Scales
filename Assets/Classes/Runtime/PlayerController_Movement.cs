using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController_Movement : Controller
{
    //private AudioManager audioManager;
    public ParticleSystem jumpParticles;
    protected InputActions inputActions;
    protected Vector2 moveControlVector;
    protected float jumpPressed;

    public Sound JumpSound;

    float CoyoteTime = 0;
    
    void OnEnable()
    {
        base.OnEnable();
        inputActions.Enable();
    }
    void OnDisable()
    {
        base.OnDisable();
        inputActions.Disable();
    }
    void Awake()
    {
        inputActions = new InputActions();
        inputActions.Enable();
    }
    
    // Start is called before the first frame update
    protected void Start()
    {
        base.Start();
    }
    float Jump()
    {
        bool notRotating = Mathf.Round(Mathf.Abs(transform.eulerAngles.z)) % 90 == 0;
        if (!jumping && jumpPressed > 0 && notRotating && CoyoteTime > 0) {
            jumping = true;
            jumpPressed = 0;
            GameManager.PlaySoundAtPoint(JumpSound, transform.position);
            //jumpParticles.Play();
            //print("Jump!");
            return jumpPower;
        }else { 
            jumping = !IsFalling() && !IsGrounded();
        }
        return 0;
    }

    // Update is called once per frame
    protected void Update()
    {
        //if (IsGrounded() && jumping == true) { jumping = false; }

        float SmoothTime = inputActions.Gameplay.Move.IsPressed() ? 0.075f : 0.2f;
        moveControlVector = Vector2.MoveTowards(moveControlVector, inputActions.Gameplay.Move.ReadValue<Vector2>(), ( 1 / SmoothTime ) * Time.deltaTime);
        jumpPressed = inputActions.Gameplay.Jump.WasPressedThisFrame() ? 0.15f : jumpPressed - Time.deltaTime;

        if (IsGrounded())
            CoyoteTime = 0.2f;
        else
            CoyoteTime -= Time.deltaTime;

        Vector3 dir = -Physics2D.gravity;
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 15);

        ManipulateGraphics(transform.InverseTransformVector(moveControlVector).x);
    }

    
    protected void FixedUpdate()
    {
        base.FixedUpdate();
        Vector2 velocity = transform.InverseTransformVector(rigidbody2D.velocity);
        float jump = Jump();
        if (jump != 0) {
            velocity.y = jump;
        }
        rigidbody2D.velocity = transform.TransformVector(new Vector2(transform.InverseTransformVector(moveControlVector).x * speed, velocity.y));

        rigidbody2D.gravityScale = IsFalling() ? 2.33f : 1.15f;
    }
}
