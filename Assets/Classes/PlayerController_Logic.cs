using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController_Logic : PlayerController_Movement
{
    public float snakeBounceHeight;
    public float snakeJumpPanelHeight;
    public SpriteRenderer Face;
    public SpriteRenderer Shading;

    public float InvincibilityFrames { get; private set; }

    public void UpdateUpgrades()
    {
        speed *= 1 + (0.05f * GameManager.Instance.GetUpgradeCount("PlayerSpeed"));
        jumpPower *= 1 + (0.05f * GameManager.Instance.GetUpgradeCount("PlayerJump"));
        MaxHealth = 3 + GameManager.Instance.GetUpgradeCount("ExtraHealth");
        Health = MaxHealth;
        GameManager.Instance.UI.UpdateHealth(Health, MaxHealth);
    }

    // Start is called before the first frame update
    protected void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected void Update()
    {
        base.Update();

        Face.transform.localPosition = new(Face.transform.localPosition.x, 0.125f * Mathf.Clamp(rigidbody2D.velocity.y / 2, -1, 1), -0.01f);

        if (InvincibilityFrames > 0) 
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            InvincibilityFrames -= Time.deltaTime;
            if (InvincibilityFrames <= 0.1f) 
            {
                spriteRenderer.enabled = true;
            }
            Face.enabled = spriteRenderer.enabled;
            Shading.enabled = spriteRenderer.enabled;
        }
        else
            InvincibilityFrames = 0;
    }
    protected override void ManipulateGraphics(float value)
    {
        if (value < -0.01 || value > 0.01)
        {
            Face.flipX = value < 0;
            spriteRenderer.flipX = value < 0;
            animator.SetBool("FlipX", value < 0);
        }
        base.ManipulateGraphics(value);
    }

    public override int CanWrapAround()
    {
        //if (transform.position.y < GameManager.Instance.Snake.transform.position.y) return -base.CanWrapAround();
        return base.CanWrapAround();
    }

    protected override void Die(GameObject Instigator)
    {
        base.Die(Instigator);
    }

    public override void TakeDamage(GameObject Instigator, int Damage)
    {
        if (InvincibilityFrames > 0 && Damage > 0) { return; }

        int DodgeChanceLevel = GameManager.Instance.GetUpgradeCount("DodgeChance");
        int BetterDodgeChanceLevel = GameManager.Instance.GetUpgradeCount("BetterDodge_Chance");
        if (DodgeChanceLevel > 0)
        {
            if (Random.value <= (DodgeChanceLevel * 0.01) + (BetterDodgeChanceLevel * 0.03) + GameManager.Instance.CurrentDodgeChance
                || (BetterDodgeChanceLevel > 0 && GameManager.Instance.CurrentDodgeChance >= 0.15))
            {
                GameManager.Instance.CurrentDodgeChance = 0;
                return;
            }
            else
            {
                GameManager.Instance.CurrentDodgeChance += 0.01f;
            }
        }

        base.TakeDamage(Instigator, Damage);

        if (Damage > 0)
            InvincibilityFrames = 2;
        GameManager.Instance.UI.UpdateHealth(Health, MaxHealth);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Snake") || collision.gameObject.CompareTag("Enemy"))
        {
            Vector2 velocity = transform.InverseTransformVector(GetComponent<Rigidbody2D>().velocity);
            if (velocity.y < snakeJumpPanelHeight)
            {
                velocity.y = snakeBounceHeight;
                GetComponent<Rigidbody2D>().velocity = transform.TransformVector(velocity);
            }
            if (InvincibilityFrames < 0.1f)
                InvincibilityFrames = 0.1f;
        }
        else if (collision.gameObject.CompareTag("JumpPanel"))
        {
            Vector2 velocity = transform.InverseTransformVector(GetComponent<Rigidbody2D>().velocity);
            velocity.y = snakeJumpPanelHeight;
            GetComponent<Rigidbody2D>().velocity = transform.TransformVector(velocity);
        }
        else if (collision.gameObject.CompareTag("EnemyBullet"))
        {
            TakeDamage(collision.gameObject, 1);
            Destroy(collision.gameObject);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(collision.gameObject, 1);
        }
    }
}
