using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class FlyingTurretEnemy : Controller
{
    Vector3 initialPosition;
    Vector3 initialJetPackPosition;
    float shootTimer;
    float currentAngle;

    public SpriteRenderer face;
    public SpriteRenderer jetPack;
    public GameObject bulletTemplate;
    public float shootInterval = 2f;
    public int bulletCount = 4;

    public Sound ShootSound;
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        initialPosition = transform.position;
        initialJetPackPosition = jetPack.transform.position;
        currentAngle = 45 * Random.Range(0, 8);

        shootInterval /= (1 + GameManager.Instance.GlobalDifficulty / 3);
    }

    // Update is called once per frame
    void Update()
    {
        if (Dead) return;
        if (Mathf.Abs(transform.position.y - Camera.main.transform.position.y) > 15) return;
        shootTimer += Time.deltaTime;
        if (speed <= 0)
        {
            transform.eulerAngles = new(0, 0, Mathf.LerpAngle(currentAngle, currentAngle + 45, Mathf.Clamp01(shootTimer * 2)));
            transform.position = initialPosition + new Vector3(0, 0.125f, 0) * Mathf.Cos(Mathf.PI / 3 + Time.time);
            jetPack.transform.eulerAngles = new Vector3(0, 0, 0);
            jetPack.transform.position = initialJetPackPosition;
        }
        face.transform.eulerAngles = new Vector3(0, 0, 0);
        face.transform.position = transform.position + new Vector3(0, 0.0625f * Mathf.Sin(Mathf.PI / 1 + Time.time), -0.01f);

        if (shootTimer > shootInterval) 
        {
            shootTimer = 0;
            currentAngle += 45;
            ShootBullets();
        }
    }

    private void FixedUpdate()
    {
        base.FixedUpdate();

        if (speed > 0)
        {
            RaycastHit2D raycastHit2D = Physics2D.Raycast(transform.position, Vector2.right * direction, 1.5f, LayerMask.GetMask("Ground"));
            rigidbody2D.MovePosition(rigidbody2D.position + Vector2.right * speed * direction * Time.fixedDeltaTime);
            if (raycastHit2D.collider != null)
            {
                ManipulateGraphics(direction * -1);
            }
        }
        
    }

    void SpawnBullet(Vector3 direction, float angle)
    {
        if (bulletTemplate == null) return;
        GameObject newBullet = Instantiate(bulletTemplate, transform.position - new Vector3(0,0,transform.position.z), Quaternion.Euler(0,0,angle));
        Destroy(newBullet, 5);
    }
    void ShootBullets()
    {
        if (Mathf.Abs(GameManager.Instance.Player.gameObject.transform.position.y - transform.position.y) > 24) return;
        if (bulletCount <= 0) return;
        // Technique brought to you by a random Garry's Mod tutorial in that Garry's Mod wiki I randomly had bookmarked
        for (int i = 0; i < bulletCount; i++)
        {
            float bulletAngle = (i * (360f / bulletCount) + transform.eulerAngles.z);
            SpawnBullet(new Vector2(Mathf.Sin(Mathf.Deg2Rad * bulletAngle), Mathf.Cos(Mathf.Deg2Rad * bulletAngle)), bulletAngle);
        }
        GameManager.PlaySoundAtPoint(ShootSound, transform.position);
    }


    protected override void ManipulateGraphics(float value)
    {
        if (value != 0)
        {
            spriteRenderer.flipX = value < 0;
            jetPack.flipX = value < 0;
            face.flipX = value < 0;

            jetPack.transform.position = transform.position + new Vector3(
                (initialJetPackPosition.x - initialPosition.x) * value,
                (initialJetPackPosition.y - initialPosition.y),
                (initialJetPackPosition.z - initialPosition.z));
        }
        base.ManipulateGraphics(value);
    }

    public override void TakeDamage(GameObject Instigator, int Damage)
    {
        if (Instigator.GetComponent<PlayerController_Logic>() != null 
            && (Instigator.GetComponent<PlayerController_Logic>().InJumpPanelAbility || Instigator.GetComponent<PlayerController_Logic>().FireTimer > 0))
            base.TakeDamage(Instigator, Damage);
    }
}
