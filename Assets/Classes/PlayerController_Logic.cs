using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerController_Logic : PlayerController_Movement
{
    public float snakeBounceHeight;
    public float snakeJumpPanelHeight;
    public SpriteRenderer Face;
    public SpriteRenderer Shading;
    public TrailRenderer JumpPanelTrail;
    public VisualEffect JumpAbilityEffect;
    public VisualEffect FireAbilityEffect;
    public SpriteRenderer FireAbilityTimer;

    public float InvincibilityFrames { get; private set; }
    public bool InJumpPanelAbility { get; private set; }
    public float FireTimer { get; private set; }
    public bool InFreeRise { get; private set; }

    public void UpdateUpgrades()
    {
        speed *= 1 + (0.05f * GameManager.Instance.GetUpgradeCount("PlayerSpeed"));
        jumpPower *= 1 + (0.05f * GameManager.Instance.GetUpgradeCount("PlayerJump"));
        MaxHealth = 3 + GameManager.Instance.GetUpgradeCount("ExtraHealth");
        Health = MaxHealth;
        GameManager.Instance.UI.UpdateHealth(Health, MaxHealth);
    }

    public void InitiateFreeRise(float finalHeight, float time)
    {
        InFreeRise = true;

        collider.excludeLayers = LayerMask.GetMask("Ground", "Enemy");
        // https://forum.unity.com/threads/calculating-projectile-velocity-needed-to-hit-a-target.1205383/
        rigidbody2D.velocity = new(rigidbody2D.velocity.x, (finalHeight - 0.5f * Physics2D.gravity.y * Mathf.Pow(time, 2)) / time);
    }

    // Start is called before the first frame update
    protected void Start()
    {
        base.Start();

        //FireAbilityEffect.Stop();
    }

    void UpdateAutoTargeting()
    {
        Collider2D[] snakePanels = Physics2D.OverlapBoxAll(transform.position + new Vector3(0, -2, 0), new(6, 4), 0);

        float closestDistance = 999999;
        GameObject closestPanel = null;
        foreach (var item in snakePanels)
        {
            if (item.gameObject.CompareTag("JumpPanel") || item.gameObject.CompareTag("FirePanel"))
            {
                if (closestPanel != null && closestDistance < Vector2.Distance(transform.position, item.transform.position)) continue;
                closestPanel = item.gameObject;
                closestDistance = Vector2.Distance(transform.position, item.transform.position);
            }
        }

        if (closestPanel != null)
            rigidbody2D.velocity += (closestPanel.transform.position - transform.position).normalized * new Vector2(4f, 0);
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

        if (InJumpPanelAbility && rigidbody2D.velocity.y < 0)
        {
            InJumpPanelAbility = false;
            JumpAbilityEffect.Stop();
            if (InvincibilityFrames < 0.1f)
                InvincibilityFrames = 0.1f;
        }

        FireTimer -= Time.deltaTime;
        FireAbilityTimer.material.SetFloat("_Alpha", FireTimer / 3);
        if (FireTimer <= 0 && FireTimer > -999)
        {
            FireTimer = -999;
            FireAbilityEffect.Stop();
            FireAbilityTimer.gameObject.SetActive(false);
            if (InvincibilityFrames < 0.1f)
                InvincibilityFrames = 0.1f;
        }

        RaycastHit2D SnakeRaycast = Physics2D.Raycast(transform.position, Vector2.down, 999, LayerMask.GetMask("Snake"));
        if (SnakeRaycast.collider == null && !InFreeRise && transform.position.y < Camera.main.transform.position.y - 9f)
        {
            TakeDamage(gameObject, 1);
            InvincibilityFrames += 1;
            if (Health <= 0)
                transform.position = new(0, Camera.main.transform.position.y - 8f, transform.position.z);
            InitiateFreeRise(8, 1);

        }

        RaycastHit2D FreeRiseRaycast = Physics2D.Raycast(transform.position, Vector2.up, 1.5f, LayerMask.GetMask("Ground"));
        if (InFreeRise && rigidbody2D.velocity.y < 0 && FreeRiseRaycast.collider == null)
        {
            InFreeRise = false;
            collider.excludeLayers = 0;
        }
    }

    protected void FixedUpdate()
    {
        base.FixedUpdate();
        if (IsFalling() && Mathf.Abs(transform.InverseTransformVector(moveControlVector).x) > 0.01)
            UpdateAutoTargeting();
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
        if (InvincibilityFrames > 0 && Damage > 0) return;
        if (GameManager.Instance.GameState != 1 && Damage > 0) return;

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
            if (!InJumpPanelAbility)
            {
                rigidbody2D.velocity = new(rigidbody2D.velocity.x, snakeBounceHeight);
            }
            if (InvincibilityFrames < 0.1f)
                InvincibilityFrames = 0.1f;
            if (collision.gameObject.CompareTag("Enemy"))
                collision.gameObject.GetComponent<Controller>().TakeDamage(gameObject, 1);
        }
        else if (collision.gameObject.CompareTag("JumpPanel"))
        {
            rigidbody2D.velocity = new(rigidbody2D.velocity.x, snakeJumpPanelHeight);
            JumpAbilityEffect.Play();
            InJumpPanelAbility = true;
        }
        else if (collision.gameObject.CompareTag("FirePanel"))
        {
            FireAbilityEffect.Play();
            FireAbilityTimer.gameObject.SetActive(true);
            FireTimer = 3.1f;
        }
        else if (collision.gameObject.CompareTag("EnemyBullet") && !InJumpPanelAbility && FireTimer <= 0)
        {
            TakeDamage(collision.gameObject, 1);
            Destroy(collision.gameObject);
        }
        else if ((collision.gameObject.CompareTag("Spikes") && GameManager.Instance.GetUpgradeCount("SpikeResistance") <= 0) || collision.gameObject.CompareTag("SpikesDangerous"))
        {
            TakeDamage(collision.gameObject, 1);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (InJumpPanelAbility || FireTimer > 0)
                collision.gameObject.GetComponent<Controller>().TakeDamage(gameObject, 999);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (InJumpPanelAbility || FireTimer > 0)
                collision.gameObject.GetComponent<Controller>().TakeDamage(gameObject, 999);
            else
                TakeDamage(collision.gameObject, 1);
        }
        else if (collision.gameObject.CompareTag("Snake"))
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
    }
}
