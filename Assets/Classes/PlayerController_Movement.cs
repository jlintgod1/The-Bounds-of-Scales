using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController_Movement : Controller
{
    //private AudioManager audioManager;
    LocalAudioManager audioManager;
    public ParticleSystem jumpParticles;
    protected InputActions inputActions;
    protected Vector2 moveControlVector;
    protected bool jumpPressed;
    
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
        audioManager = GetComponent<LocalAudioManager>();
    }

    float Jump()
    {
        bool notRotating = Mathf.Round(Mathf.Abs(transform.eulerAngles.z)) % 90 == 0;
        if (jumping == false && jumpPressed && notRotating && IsGrounded()) {
            jumping = true;
            //audioManager.Play("Jump");
            //jumpParticles.Play();
            //print("Jump!");
            return jumpPower;
        }else {
            jumping = !IsFalling();
        }
        return 0;
    }

    // Update is called once per frame
    protected void Update()
    {
        if (IsGrounded() && jumping == true) { jumping = false; }

        float SmoothTime = inputActions.Gameplay.Move.IsPressed() ? 0.15f : 0.05f;
        moveControlVector = Vector2.MoveTowards(moveControlVector, inputActions.Gameplay.Move.ReadValue<Vector2>(), ( 1 / SmoothTime ) * Time.deltaTime);
        jumpPressed = inputActions.Gameplay.Jump.IsPressed();

        Vector3 dir = -Physics2D.gravity;
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 15);

        ManipulateGraphics(transform.InverseTransformVector(moveControlVector).x);
    }

    
    void FixedUpdate()
    {
        base.FixedUpdate();
        Vector2 velocity = transform.InverseTransformVector(GetComponent<Rigidbody2D>().velocity);
        float jump = Jump();
        if (jump != 0) {
            velocity.y = jump;
        }
        GetComponent<Rigidbody2D>().velocity = transform.TransformVector(new Vector2(transform.InverseTransformVector(moveControlVector).x * speed, velocity.y));

        
    }
}
