using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    new Rigidbody2D rigidbody2D;
    public float Speed;
    public GameObject SplitBullet;
    public int SplitCount;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        rigidbody2D.velocity = transform.TransformDirection(Vector2.right) * Speed;
    }

    private void OnDestroy()
    {
        if (SplitBullet != null && SplitCount > 0)
        {
            for (int i = 0; i < 360; i += 360 / SplitCount)
            {
                Instantiate(SplitBullet, transform.position, Quaternion.Euler(0, 0, i));
            }
        }
    }
}
