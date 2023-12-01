using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WormEnemy : Controller
{
    public Collider2D headCollider;
    public SpriteRenderer eyeRenderer;
    public float JumpInterval;
    private float JumpTimer;

    public Sound JumpSound;

    protected void Start()
    {
        base.Start();

        speed *= 1 + GameManager.Instance.GlobalDifficulty / 3;
        jumpPower *= 1 + GameManager.Instance.GlobalDifficulty / 2f + UnityEngine.Random.Range(0, GameManager.Instance.GlobalDifficulty / 5f);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Dead) return;
        if (Mathf.Abs(transform.position.y - Camera.main.transform.position.y) > 15) return;
        base.FixedUpdate();

        rigidbody2D.velocity = rigidbody2D.velocity * Vector2.up + Vector2.right * direction * speed;

        RaycastHit2D raycastHit2D = Physics2D.Raycast(transform.position, Vector2.right * direction, 1f, LayerMask.GetMask("Ground"));
        RaycastHit2D floorRaycastHit2D = Physics2D.Raycast(transform.position + Vector3.right * direction * 0.5f, Vector2.down, 1f, LayerMask.GetMask("Ground"));
        if (raycastHit2D.collider != null || (floorRaycastHit2D.collider == null && rigidbody2D.velocity.y >= 0 && (IsGrounded() || jumpPower > 0)))
        {
            ManipulateGraphics(direction * -1);
        }
        else
        {
            ManipulateGraphics(0);
        }

        JumpTimer -= Time.fixedDeltaTime;
        if (IsGrounded() && jumpPower > 0 && JumpTimer <= 0)
        {
            JumpTimer = JumpInterval;
            rigidbody2D.velocity = rigidbody2D.velocity * Vector2.right + Vector2.up * jumpPower;
            GameManager.PlaySoundAtPoint(JumpSound, transform.position);
        }
    }

    protected override void ManipulateGraphics(float value)
    {
        if (value != 0)
        {
            spriteRenderer.flipX = value < 0;
            if (eyeRenderer != null)
                eyeRenderer.flipX = value < 0;
        }
        if (eyeRenderer != null)
            eyeRenderer.transform.localPosition = new(eyeRenderer.transform.localPosition.x, 0.125f * Mathf.Clamp(rigidbody2D.velocity.y / 2, -1, 1), -0.01f);

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
