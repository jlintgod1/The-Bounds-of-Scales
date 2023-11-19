using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WormEnemy : Controller
{
    public Collider2D headCollider;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Dead) return;
        if (Mathf.Abs(transform.position.y - Camera.main.transform.position.y) > 15) return;
        base.FixedUpdate();

        rigidbody2D.velocity = rigidbody2D.velocity * Vector2.up + Vector2.right * direction * speed;

        RaycastHit2D raycastHit2D = Physics2D.Raycast(transform.position, Vector2.right * direction, 1f, LayerMask.GetMask("Ground"));
        RaycastHit2D floorRaycastHit2D = Physics2D.Raycast(transform.position + Vector3.right * direction * 0.5f, Vector2.down, 1f, LayerMask.GetMask("Ground"));
        if (raycastHit2D.collider != null || (floorRaycastHit2D.collider == null && rigidbody2D.velocity.y >= 0 && IsGrounded()))
        {
            ManipulateGraphics(direction * -1);
        }
        else
        {
            ManipulateGraphics(0);
        }

        if (IsGrounded() && jumpPower > 0)
        {
            rigidbody2D.velocity = rigidbody2D.velocity * Vector2.right + Vector2.up * jumpPower;
        }
    }

    protected override void ManipulateGraphics(float value)
    {
        if (value != 0)
            spriteRenderer.flipX = value < 0;
        base.ManipulateGraphics(value);
    }

    protected override void Die(GameObject Instigator)
    {
        animator.SetBool("Falling", true);
        base.Die(Instigator);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Snake"))
        {
            TakeDamage(collision.gameObject, 1);
        }
    }
}
