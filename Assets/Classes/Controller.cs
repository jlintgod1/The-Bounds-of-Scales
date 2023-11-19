using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Base class for anything that runs around, jumps, attacks, etc., like the player and enemies 
public class Controller : MonoBehaviour
{
    protected new Rigidbody2D rigidbody2D;
    public new Collider2D collider;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;

    public int Health;
    public int MaxHealth;
    public bool Dead { get; protected set; }

    // Movement
    public float speed;
    public float jumpPower;
    public float direction = 1;
    public bool jumping {get; protected set;}
    public bool falling {get; protected set;}
    public Collider2D groundCheck;

    public bool IsGrounded()
    {
        return groundCheck.IsTouchingLayers(groundCheck.includeLayers);
    }

    public bool IsFalling()
    {
        return transform.InverseTransformVector(rigidbody2D.velocity).y < -0.1f && !IsGrounded();
    }
    // -1: Appear on Left, 0: Don't wrap, 1: Appear on Right
    public virtual int CanWrapAround()
    {
        if (collider.IsTouchingLayers(LayerMask.NameToLayer("WrapAroundOverride"))) return 0;
        if (transform.position.x < (GameManager.CONST_ScreenDimensions.x / -32.0) - 0.625) return 1;
        if (transform.position.x > (GameManager.CONST_ScreenDimensions.x / 32.0) + 0.625) return -1;

        return 0;
    }

    protected void OnEnable()
    {
        //rigidbody2D.simulated = true;
        //if (animator != null)
        //    animator.enabled = true;
    }

    protected void OnDisable()
    {
        //rigidbody2D.simulated = false;
        //if (animator != null)
        //    animator.enabled = false;
    }
    protected void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    protected void FixedUpdate()
    {
        if (Dead) return;
        int WrapAroundStatus = CanWrapAround();
        if (WrapAroundStatus != 0)
            transform.position = new((GameManager.CONST_ScreenDimensions.x / 32.0f + 0.5f) * WrapAroundStatus, rigidbody2D.transform.position.y);
    }

    protected virtual void ManipulateGraphics(float value)
    {
        if (value < -0.01 || value > 0.01)
            direction = Mathf.Abs(value) / value;

        if (animator != null)
        {
            animator.SetBool("Walking", (value < -0.01 || value > 0.01));
            animator.SetBool("Falling", IsFalling());
            animator.SetBool("Jumping", jumping);
        }
    }

    protected virtual void Die(GameObject Instigator)
    {
        if (Dead) return; // You can't die twice!!
        Health = 0;
        Dead = true;

        foreach (var collider in GetComponents<Collider2D>())
        {
            collider.enabled = false;
        }

        rigidbody2D.velocity = new(Mathf.Sign((transform.position - Instigator.transform.position).x) * 6, 8);
        rigidbody2D.freezeRotation = false;
        rigidbody2D.angularVelocity = 180;

        GameManager.Instance.OnControllerDeath(this, Instigator);

        Destroy(gameObject, 5);

        enabled = false;
    }

    // From Real Engine!!!
    public virtual void TakeDamage(GameObject Instigator, int Damage)
    { 
        Health = Mathf.Clamp(Health - Damage, 0, MaxHealth);

        if (Health <= 0)
        {
            Die(Instigator);
        }
    }
}
