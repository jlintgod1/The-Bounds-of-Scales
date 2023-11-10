using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class FlyingTurretEnemy : Controller
{
    Vector3 initialPosition;
    float shootTimer;
    float currentAngle;

    public SpriteRenderer face;
    public SpriteRenderer jetPack;
    public GameObject bulletTemplate;
    public float shootInterval = 2f;
    public int bulletCount = 4;
    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        shootTimer += Time.deltaTime;
        transform.eulerAngles = new(0, 0, Mathf.LerpAngle(currentAngle, currentAngle + 45, Mathf.Clamp01(shootTimer * 2)));
        transform.position = initialPosition + new Vector3(0, 0.125f, 0) * Mathf.Cos(Mathf.PI / 3 + Time.time);
        face.transform.eulerAngles = new Vector3(0, 0, 0);
        face.transform.position = transform.position + new Vector3(0, 0.0625f * Mathf.Sin(Mathf.PI / 1 + Time.time), -0.01f);
        jetPack.transform.eulerAngles = new Vector3(0, 0, 0);
        jetPack.transform.position = transform.position + new Vector3(0, -0.125f, 0.01f);

        if (shootTimer > shootInterval) 
        {
            shootTimer = 0;
            currentAngle += 45;
            ShootBullets();
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
        // Technique brought to you by a random Garry's Mod tutorial in that Garry's Mod wiki that I randomly had bookmarked
        for (int i = 0; i < bulletCount; i++)
        {
            float bulletAngle = (i * (360f / bulletCount) + transform.eulerAngles.z);
            SpawnBullet(new Vector2(Mathf.Sin(Mathf.Deg2Rad * bulletAngle), Mathf.Cos(Mathf.Deg2Rad * bulletAngle)), bulletAngle);
        }
    }
}
